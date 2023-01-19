using NLog;
using System.Runtime.InteropServices;

namespace LaMetrum {
  public class GUI : Form {
    static readonly Logger _log = LogManager.GetCurrentClassLogger();

    enum Scope {
      Session,
      Battle,
      Player
    }

    [StructLayout(LayoutKind.Sequential)]
    struct RECT {
      public int Left;
      public int Top;
      public int Right;
      public int Bottom;

      public Rectangle ToRectangle() => new Rectangle(Left, Top, Right - Left, Bottom - Top);

      public static RECT FromRectangle(in Rectangle r) => new() {
        Left = r.Left,
        Top = r.Top,
        Right = r.Right,
        Bottom = r.Bottom,
      };
    }

    const uint SWP_NOSIZE = 0x01;
    const uint SWP_NOMOVE = 0x02;
    const uint SWP_NOACTIVATE = 0x10;

    const int WS_EX_TOPMOST = 0x08;
    const int WM_NCLBUTTONDOWN = 0xA1;
    const int HT_CAPTION = 0x02;
    const int WM_NCHITTEST = 0x84;
    const int WM_SIZING = 0x0214;
    const int HTBOTTOMLEFT = 16;
    const int HTBOTTOMRIGHT = 17;

    const int InitialRowCount = 13;
    const int FontSize = 10;

    [DllImport("user32.dll")] static extern int SendMessage(IntPtr hWnd, uint msg, int wParam, int lParam);
    [DllImport("user32.dll")] static extern int ReleaseCapture();

    Scope _scope = Scope.Battle;
    int _battle = -1;
    Entity _player = null;
    List<Battle> _battles = null;
    BattleTracker _tracker = null;
    long _battleOffset = 0;

    int _offset;
    int _rowHeight;

    readonly Font _regular = new(FontFamily.GenericMonospace, FontSize);
    readonly Font _bold = new(FontFamily.GenericMonospace, FontSize, FontStyle.Bold);
    readonly Brush _text = new SolidBrush(Color.Black);

    // From https://colorhunt.co/palette/f2d7d9d3cedf9cb4cc748da6.
    readonly Brush _title = new SolidBrush(ColorTranslator.FromHtml("#748DA6"));
    readonly Brush _header = new SolidBrush(ColorTranslator.FromHtml("#9CB4CC"));
    readonly Brush[] _rows = BrightAndDim("#D3CEDF").Concat(BrightAndDim("#F2D7D9"))
        .Select(c => new SolidBrush(c)).ToArray();

    readonly System.Windows.Forms.Timer _timer = new();

    static Color[] BrightAndDim(string color) {
      Color bright = ColorTranslator.FromHtml(color);
      return new Color[] { bright, Color.FromArgb(bright.A, Dimmer(bright.R), Dimmer(bright.G), Dimmer(bright.B)) };

      static int Dimmer(int c) => c * 3 / 4;
    }

    public GUI() {
      DoubleBuffered = true;
      BackColor = Color.Black;
      Text = Application.ProductName;
      FormBorderStyle = FormBorderStyle.None;

      SetStyle(ControlStyles.ResizeRedraw, true);

      MouseDown += new MouseEventHandler(OnMouseDown);
      KeyPress += new KeyPressEventHandler(OnKeyPress);

      _timer.Tick += delegate { Invalidate(); };
      _timer.Interval = 1000;
      _timer.Start();
    }

    Brush Bright(int i) => _rows[2 * (i % (_rows.Length / 2))];
    Brush Dim(int i) => _rows[2 * (i % (_rows.Length / 2)) + 1];

    bool DrawRow(PaintEventArgs e, Font font, Brush brightBrush, int index, string text,
                 Brush dimBrush = null, double brightFrac = 0) {
      int y = index * _rowHeight;
      if (y >= Size.Height) return false;
      if (dimBrush is not null) {
        e.Graphics.FillRectangle(dimBrush, 0, y, Size.Width, _rowHeight);
        e.Graphics.FillRectangle(brightBrush, 0, y, (int)Math.Ceiling(Size.Width * brightFrac), _rowHeight);
      } else {
        e.Graphics.FillRectangle(brightBrush, 0, y, Size.Width, _rowHeight);
      }
      e.Graphics.DrawString(text, font, _text, _offset, y + _offset);
      return true;
    }

    static Battle Latest(List<Battle> x) => x.LastOrDefault(b => !b.IsEmpty(), x[^1]);

    static string Align<T>(int w, T obj) => string.Format($"{{0,{w}}}", obj);

    Battle FocusedBattle() => _battle < 0 ? Latest(_battles) : _battles[_battle];

    void DrawSession(PaintEventArgs e) {
      DateTime now = DateTime.UtcNow;
      int w = Math.Max(2, (_battles.Count + _battleOffset).ToString().Length);
      DrawRow(e, _bold, _title, 0, Align(w, "ID") + " T0         T DAMAGE DEATH START      END");
      DrawRow(e, _bold, _header, 1, Align(w + 1, "") + Summary(Latest(_battles)));
      for (int i = 0; i != _battles.Count; ++i) {
        string text = Align(w, _battles.Count + _battleOffset - i) + " " + Summary(_battles[^(i + 1)]);
        if (!DrawRow(e, _regular, Bright(i), i + 2, text)) break;
      }

      string Summary(Battle b) {
        long dmg = 0;
        long deaths = 0;

        foreach (Entity x in b.Players.Values) {
          dmg += x.TotalStats.Total.TotalDamage;
          deaths += x.Deaths.Count;
        }

        return string.Format(
            "{0:HH:mm} {1,6} {2,6} {3,5} {4,-10} {5,-10}",
            b.StartTime,
            Fmt.MinSec((b.EndTime ?? now) - b.StartTime),
            Fmt.SI(dmg),
            deaths,
            b.StartReason,
            b.EndReason?.ToString() ?? "-");
      }
    }

    const string BattleHeader = "PLAYER           CLASS        ILVL BUILD              DMG   DPS   DMG% CRIT POS% ALIVE DIE KILL";
    const string PlayerHeader = "SKILL                            LV TRI TLV  HITS   DMG   DPS   DMG% CRIT POS%";

    void DrawBattle(PaintEventArgs e) {
      int i = 0;
      Battle b = FocusedBattle();
      DateTime end = b.EndTime ?? DateTime.UtcNow;
      TimeSpan battleDuration = end - b.StartTime;
      long totalDamage = b.Players.Values.Sum(x => x.TotalStats.Total.TotalDamage);
      string title = _battle < 0 ? "Latest" : $"Battle #{_battle + 1}";
      title += $": {b.StartTime:HH:mm} + {Fmt.MinSec(battleDuration)}";
      title += $", {Fmt.SI(totalDamage)} dmg";
      title += $", {Fmt.SI(totalDamage / Math.Max(1, battleDuration.TotalSeconds))} dps";
      title += $", {b.StartReason} - {b.EndReason}";
      DrawRow(e, _bold, _title, 0, title);
      DrawRow(e, _bold, _header, 1, BattleHeader);
      IEnumerable<Entity> players = b.Players.Values
          .Where(x => x.TotalStats.Total.TotalDamage > 0 || x.Deaths.Count > 0)
          .OrderByDescending(x => x.TotalStats.Total.TotalDamage);
      long max = players.FirstOrDefault()?.TotalStats.Total.TotalDamage ?? 1;
      foreach (Entity x in players) {
        TimeSpan alive = x.Deaths.Count == 0 ? battleDuration : x.Deaths[^1] - b.StartTime;
        string text = string.Format(
            "{0,-16} {1,-12} {2,4} {3,-16} {4,5} {5,5} {6,5:F1}% {7,3:F0}% {8,3:F0}% {9,6} {10,3} {11,4}",
            x.Name,
            x.Class,
            x.ItemLevel.HasValue ? ((int)x.ItemLevel.Value).ToString() : "-",
            Build.Guess(x),
            Fmt.SI(x.TotalStats.Total.TotalDamage),
            Fmt.SI(x.TotalStats.Total.TotalDamage / Math.Max(1, alive.TotalSeconds)),
            100.0 * x.TotalStats.Total.TotalDamage / totalDamage,
            100 * x.TotalStats.Total.CritRate,
            100 * x.TotalStats.PosRate,
            Fmt.MinSec(alive),
            x.Deaths.Count > 99 ? "99+" : x.Deaths.Count > 0 ? x.Deaths.Count.ToString() : "",
            x.Kills > 999 ? "999+" : x.Kills > 0 ? x.Kills.ToString() : "");
        if (!DrawRow(e, _regular, Bright(i), i + 2, text, Dim(i), 1.0 * x.TotalStats.Total.TotalDamage / max)) break;
        ++i;
      }
    }

    void DrawPlayer(PaintEventArgs e, Entity player) {
      int i = 0;
      Battle b = FocusedBattle();
      TimeSpan alive = (player.Deaths.Count == 0 ? b.EndTime ?? DateTime.UtcNow : player.Deaths[^1]) - b.StartTime;
      string title = _battle < 0 ? "Latest" : $"Battle #{_battle + 1}";
      title += $" => {player.Name}";
      title += $": {Fmt.SI(player.TotalStats.Total.TotalDamage / Math.Max(1, alive.TotalSeconds))} dps";
      title += $", {100 * player.TotalStats.Total.CritRate:F0}% crit";
      DrawRow(e, _bold, _title, 0, title);
      DrawRow(e, _bold, _header, 1, PlayerHeader);
      IEnumerable<KeyValuePair<string, SkillStats>> skills = player.SkillStats
          .OrderByDescending(kv => kv.Value.Stats.Total.TotalDamage);
      long max = skills.FirstOrDefault().Value?.Stats.Total.TotalDamage ?? 1;
      foreach ((string name, SkillStats stats) in skills) {
        if (stats.Stats.Total.NumHits == 0) continue;
        string text = string.Format(
            "{0,-32} {1,2} {2,3} {3,3} {4,5} {5,5} {6,5} {7,5:F1}% {8,3:F0}% {9,3:F0}%",
            name.Length > 32 ? name[..32] : name,
            stats.Build?.Level,
            stats.Build?.Level > 1 && stats.Build?.Tripods is not null ?
                string.Join(null, stats.Build.Tripods) : "",
            stats.Build?.Level > 1 && stats.Build?.TripodsLevels is not null ?
                string.Join(null, stats.Build.TripodsLevels) : "",
            Fmt.SI(stats.Stats.Total.NumHits),
            Fmt.SI(stats.Stats.Total.TotalDamage),
            Fmt.SI(stats.Stats.Total.TotalDamage / Math.Max(1, alive.TotalSeconds)),
            100.0 * stats.Stats.Total.TotalDamage / Math.Max(1, player.TotalStats.Total.TotalDamage),
            100 * stats.Stats.Total.CritRate,
            100 * stats.Stats.PosRate);
        if (!DrawRow(e, _regular, Bright(i), i + 2, text, Dim(i), 1.0 * stats.Stats.Total.TotalDamage / max)) break;
        ++i;
      }
    }

    void InitGraphics(PaintEventArgs e) {
      e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

      if (_tracker is not null) return;

      _tracker = new();

      SizeF glyph = e.Graphics.MeasureString(
          BattleHeader.Length > PlayerHeader.Length ? BattleHeader : PlayerHeader,
          _regular);
      _rowHeight = (int)(5 * glyph.Height / 4);
      _offset = (int)((_rowHeight - glyph.Height + 1) / 2);

      Size = new Size((int)glyph.Width + 2 * _offset, _rowHeight * InitialRowCount);
      Location = new Point(Screen.PrimaryScreen.Bounds.Width - Size.Width - 5, 0);
      MinimumSize = new Size(_rowHeight, _rowHeight);
    }

    protected override void OnPaint(PaintEventArgs e) {
      try {
        InitGraphics(e);
        base.OnPaint(e);

        _battles = _tracker.GetBattles(out _battleOffset);

        Entity player = null;
        if (_player is not null) {
          if (FocusedBattle().Players.TryGetValue(_player.Name, out Entity x)) {
            player = _player = x;
          } else {
            player = new Entity() {
              Id = _player.Id,
              Type = _player.Type,
              Name = _player.Name,
              OwnerId = _player.OwnerId,
              Class = _player.Class,
              ItemLevel = _player.ItemLevel,
              Deaths = new(),
              Kills = 0,
              ExpiresAt = null,
              SkillStats = new(),
              TotalStats = new(),
            };
          }
        }

        switch (_scope) {
          case Scope.Session:
            DrawSession(e);
            break;
          case Scope.Battle:
            DrawBattle(e);
            break;
          case Scope.Player:
            DrawPlayer(e, player);
            break;
        }

        ControlPaint.DrawSizeGrip(
            e.Graphics, Color.White,
            ClientSize.Width - SizeGrip,
            ClientSize.Height - SizeGrip,
            SizeGrip,
            SizeGrip);
      } catch (Exception ex) {
        _log.Fatal(ex, "Internal error in {0}:", nameof(OnPaint));
      }
    }

    int SizeGrip => _rowHeight * 2 / 3;

    void OnKeyPress(object sender, KeyPressEventArgs e) {
      try {
        switch (e.KeyChar) {
        case 'q':
        case 'Q':
          Close();
          break;
        case 'r':
        case 'R':
          if (_tracker is not null) {
            _tracker.Rotate();
            Invoke(Invalidate);
          }
          break;
      }
      } catch (Exception ex) {
        _log.Fatal(ex, "Internal error in {0}:", nameof(OnKeyPress));
      }
    }

    void OnMouseDown(object sender, MouseEventArgs e) {
      try {
        if (e.Button == MouseButtons.Right) {
          switch (_scope) {
            case Scope.Session:
              return;
            case Scope.Battle:
              _scope = Scope.Session;
              _battle = -1;
              break;
            case Scope.Player:
              _scope = Scope.Battle;
              _player = null;
              break;
          }

          Invalidate();
          return;
        }

        if (e.Button != MouseButtons.Left) return;

        Check(ReleaseCapture() != 0);
        Check(SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0) >= 0);

        if (_battles is null) return;

        int index = e.Location.Y / _rowHeight;

        switch (_scope) {
          case Scope.Session:
            index -= 2;
            if (index == -1) {
              _battle = -1;
            } else if (index >= 0 && index < _battles.Count) {
              _battle = _battles.Count - index - 1;
            } else {
              return;
            }
            _scope = Scope.Battle;
            Invalidate();
            break;
          case Scope.Battle:
            index -= 2;
            if (index < 0) return;
            Battle b = FocusedBattle();
            IEnumerable<Entity> players = b.Players.Values
                .Where(x => x.TotalStats.Total.TotalDamage > 0 || x.Deaths.Count > 0)
                .OrderByDescending(x => x.TotalStats.Total.TotalDamage);
            players = players.Skip(index);
            Entity p = players.FirstOrDefault();
            if (p is null) return;
            _scope = Scope.Player;
            _player = p;
            Invalidate();
            break;
        }
      } catch (Exception ex) {
        _log.Fatal(ex, "Internal error in {0}:", nameof(OnMouseDown));
      }
    }

    protected override void WndProc(ref Message m) {
      switch (m.Msg) {
        case WM_NCHITTEST:
          Point p = PointToClient(new Point((ushort)m.LParam.ToInt64(), (ushort)(m.LParam.ToInt64() >> 16)));
          if (p.X >= ClientSize.Width - SizeGrip && p.Y >= ClientSize.Height - SizeGrip) {
            m.Result = (IntPtr)(IsMirrored ? HTBOTTOMLEFT : HTBOTTOMRIGHT);
            return;
          }
          break;
        case WM_SIZING:
          if (_rowHeight != 0) {
            Rectangle rect = RectangleToClient(Marshal.PtrToStructure<RECT>(m.LParam).ToRectangle());
            rect.Height = (rect.Height + _rowHeight / 2) / _rowHeight * _rowHeight;
            Marshal.StructureToPtr(RECT.FromRectangle(RectangleToScreen(rect)), m.LParam, fDeleteOld: false);
          }
          break;
      }
      base.WndProc(ref m);
    }

    protected override void OnClosed(EventArgs e) {
      _timer.Dispose();
      _tracker?.Dispose();
      base.OnClosed(e);
    }

    protected override CreateParams CreateParams {
      get {
        CreateParams res = base.CreateParams;
        res.ExStyle |= WS_EX_TOPMOST;
        return res;
      }
    }
  }
}
