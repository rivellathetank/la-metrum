using NLog;
using System.Text;

namespace LaMetrum {
  enum SessionTriger {
    Launch,
    Manual,
    ZoneChange,
    CoopStart,
    CoopWin,
    CoopFail,
    CoopCancel,
    PhaseStart,
    P1Win,
    P2Win,
    P3Win,
    P4Win,
    P5Win,
    P1Fail,
    P2Fail,
    P3Fail,
    P4Fail,
    P5Fail,
    RaidEnd,
  }

  class Battle {
    static readonly Logger _log = LogManager.GetCurrentClassLogger();

    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public SessionTriger StartReason { get; set; }
    public SessionTriger? EndReason { get; set; }
    public Dictionary<string, Entity> Players { get; set; }

    public bool IsEmpty() => Players.Values.All(x => x.TotalStats.Total.TotalDamage == 0 && x.Deaths.Count == 0);

    public Battle Clone() {
      Battle res = (Battle)MemberwiseClone();
      res.Players = res.Players?.ToDictionary(kv => kv.Key, kv => kv.Value.Clone());
      return res;
    }

    public string Print(string trigger, string ident) {
      StringBuilder buf = new();
      DateTime end = EndTime ?? DateTime.UtcNow;
      TimeSpan battleDuration = end - StartTime;
      long totalDamage = Players.Values.Sum(x => x.TotalStats.Total.TotalDamage);
      buf.AppendFormat("{0}----------------------------------------------------------", ident);
      buf.AppendFormat("\n{0}Trigger = {1}, T0 = {2:HH:mm}, T = {3}, DMG = {4}, DPS = {5}, Start = {6}",
                       ident, trigger, StartTime, Fmt.MinSec(battleDuration), Fmt.SI(totalDamage),
                       Fmt.SI(totalDamage / Math.Max(1, battleDuration.TotalSeconds)), StartReason);
      buf.AppendFormat("\n{0}------------------------------------------------------------------------------------------------", ident);
      buf.AppendFormat("\n{0}PLAYER           CLASS        ILVL BUILD                   DMG   DPS   DMG% CRIT  ALIVE DIE KILL", ident);
      buf.AppendFormat("\n{0}------------------------------------------------------------------------------------------------", ident);
      foreach (Entity x in Players.Values.OrderByDescending(x => (x.TotalStats.Total.TotalDamage, x.Deaths.Count))) {
        if (x.TotalStats.Total.TotalDamage == 0 && x.Deaths.Count == 0) break;
        TimeSpan alive = x.Deaths.Count == 0 ? battleDuration : x.Deaths[^1] - StartTime;
        buf.AppendFormat(
            "\n{0}{1,-16} {2,-12} {3,4} {4,-16} {5,10} {6,5} {7,5:F1}% {8,3:F0}% {9,6} {10,3} {11,4}",
            ident,
            x.Name,
            x.Class,
            x.ItemLevel.HasValue ? ((int)x.ItemLevel.Value).ToString() : "-",
            Build.Guess(x),
            x.TotalStats.Total.TotalDamage,
            Fmt.SI(x.TotalStats.Total.TotalDamage / Math.Max(1, alive.TotalSeconds)),
            100.0 * x.TotalStats.Total.TotalDamage / totalDamage,
            100 * x.TotalStats.Total.CritRate,
            Fmt.MinSec(alive),
            x.Deaths.Count > 99 ? "99+" : x.Deaths.Count > 0 ? x.Deaths.Count.ToString() : "",
            x.Kills > 999 ? "999+" : x.Kills > 0 ? x.Kills.ToString() : "");
      }
      return buf.ToString();
    }
  }

  class BattleTracker : IMessageVisitor<byte, byte>, IDisposable {
    const int MaxBattleCount = 128;

    static readonly Logger _log = LogManager.GetCurrentClassLogger();

    readonly object _monitor = new();
    readonly EntityTracker _entities = new();
    readonly List<Battle> _battles = new();
    readonly Task _loop;

    int _currentBattle = 0;
    int _droppedBattles = 0;

    CancellationTokenSource _dispose = new();

    public BattleTracker() {
      _battles.Add(new Battle() {
        StartTime = DateTime.UtcNow,
        Players = _entities.Players,
        StartReason = SessionTriger.Launch,
      });

      CancellationToken cancel = _dispose.Token;

      _loop = Task.Run(async () => {
        while (!cancel.IsCancellationRequested) {
          try {
            Decoder decoder = new();
            await foreach (ByteSegment bytes in Sniffer.Snif(cancel)) {
              try {
                if (bytes.Data.Count == 0) {
                  decoder.Reset();
                  continue;
                }
                foreach (RawMessage raw in decoder.Decode(bytes.Data)) {
                  IMessage msg;
                  try {
                    msg = Parser.Parse(raw).Data;
                  } catch (Exception e) {
                    _log.Error(e, "Failed to parse an incoming message: {0}:", raw);
                    decoder.Drop(raw.UnprocessedBytes, "failed to parse");
                    break;
                  }
                  if (msg is null) continue;
                  _log.Debug("Parsed: {0} {1}", msg.GetType().Name, msg);
                  try {
                    lock (_monitor) msg.Visit<byte, byte>(this, 0);
                  } catch (Exception e) {
                    _log.Error(e, "Internal error while processing {0} {1}:",
                                msg.GetType().Name, msg);
                  }
                }
              } catch (Exception e) {
                if (!cancel.IsCancellationRequested) _log.Error(e, "Internal error in the process loop:");
              }
            }
          } catch (Exception e) {
            if (!cancel.IsCancellationRequested) _log.Error(e, "Internal error in the process loop:");
          }
        }
      });
    }

    public void Dispose() {
      CancellationTokenSource dispose = Interlocked.Exchange(ref _dispose, null);
      if (dispose is not null) {
        dispose.Cancel();
        dispose.Dispose();
      }
      _loop.Wait();
    }

    public List<Battle> GetBattles(out long offset) {
      lock (_monitor) {
        List<Battle> res = new(_battles.Count);
        for (int i = 0; i < _battles.Count - 1; ++i) {
          res.Add(_battles[(_currentBattle + i + 1) % _battles.Count]);
        }
        res.Add(_battles[_currentBattle].Clone());
        offset = _droppedBattles;
        return res;
      }
    }

    public byte Visit(PKTDeathNotify x, byte _) {
      bool died = Died().Any();
      _entities.ProcessMessage(x);
      if (died) return 0;
      Entity who = Died().FirstOrDefault();
      if (who is null) return 0;
      _log.Info("Stats:\n{0}", _battles[_currentBattle].Print($"FirstDeath:{who.Name}", ident: "    "));
      return 0;

      IEnumerable<Entity> Died() => _battles[_currentBattle].Players.Values.Where(x => x.Deaths.Count > 0);
    }

    public byte Visit(PKTInitEnv x, byte _) {
      Rotate(SessionTriger.ZoneChange);
      _entities.ProcessMessage(x);
      return 0;
    }

    public byte Visit(PKTInitPC x, byte _) {
      _entities.ProcessMessage(x);
      return 0;
    }

    public byte Visit(PKTNewNpcSummon x, byte _) {
      _entities.ProcessMessage(x);
      return 0;
    }

    public byte Visit(PKTNewPC x, byte _) {
      _entities.ProcessMessage(x);
      return 0;
    }

    public byte Visit(PKTNewProjectile x, byte _) {
      _entities.ProcessMessage(x);
      return 0;
    }

    public byte Visit(PKTRaidResult x, byte _) {
      Rotate(SessionTriger.RaidEnd);
      return 0;
    }

    public byte Visit(PKTSkillDamageAbnormalMoveNotify x, byte _) {
      _entities.ProcessMessage(x);
      return 0;
    }

    public byte Visit(PKTSkillDamageNotify x, byte _) {
      _entities.ProcessMessage(x);
      return 0;
    }

    public byte Visit(PKTTriggerBossBattleStatus x, byte _) {
      Rotate(SessionTriger.PhaseStart);
      return 0;
    }

    public byte Visit(PKTTriggerStartNotify x, byte _) {
      switch (x.TriggerSignalType) {
        case TriggerSignalType.COOP_QUEST_START:
          Rotate(SessionTriger.CoopStart);
          break;
        case TriggerSignalType.COOP_QUEST_COMPLETE:
          Rotate(SessionTriger.CoopWin);
          break;
        case TriggerSignalType.COOP_QUEST_FAIL:
          Rotate(SessionTriger.CoopFail);
          break;
        case TriggerSignalType.COOP_QUEST_CANCEL:
          Rotate(SessionTriger.CoopCancel);
          break;
        case TriggerSignalType.DUNGEON_PHASE1_CLEAR:
          Rotate(SessionTriger.P1Win);
          break;
        case TriggerSignalType.DUNGEON_PHASE2_CLEAR:
          Rotate(SessionTriger.P2Win);
          break;
        case TriggerSignalType.DUNGEON_PHASE3_CLEAR:
          Rotate(SessionTriger.P3Win);
          break;
        case TriggerSignalType.DUNGEON_PHASE4_CLEAR:
          Rotate(SessionTriger.P4Win);
          break;
        case TriggerSignalType.DUNGEON_PHASE5_CLEAR:
          Rotate(SessionTriger.P5Win);
          break;
        case TriggerSignalType.DUNGEON_PHASE1_FAIL:
          Rotate(SessionTriger.P1Fail);
          break;
        case TriggerSignalType.DUNGEON_PHASE2_FAIL:
          Rotate(SessionTriger.P2Fail);
          break;
        case TriggerSignalType.DUNGEON_PHASE3_FAIL:
          Rotate(SessionTriger.P3Fail);
          break;
        case TriggerSignalType.DUNGEON_PHASE4_FAIL:
          Rotate(SessionTriger.P4Fail);
          break;
        case TriggerSignalType.DUNGEON_PHASE5_FAIL:
          Rotate(SessionTriger.P5Fail);
          break;
      }
      return 0;
    }

    public byte Visit(PKTSkillStartNotify x, byte _) {
      _entities.ProcessMessage(x);
      return 0;
    }

    public void Rotate() {
      lock (_monitor) Rotate(SessionTriger.Manual);
    }

    void Rotate(SessionTriger reason) {
      Battle x = _battles[_currentBattle];
      if (x.IsEmpty()) {
        _log.Info("Refreshing current battle: {0}", reason);
        x.StartTime = DateTime.UtcNow;
        x.StartReason = reason;
        return;
      }
      x.EndTime = DateTime.UtcNow;
      x.EndReason = reason;
      string ident = x.Players.Values.All(x => x.Deaths.Count == 0) &&
                     reason >= SessionTriger.P1Win &&
                     reason <= SessionTriger.P5Fail ? "    " : "  ";
      string dump = x.Print(reason.ToString(), ident: ident);
      _log.Info("Stats:\n{0}", dump);
      _battles[_currentBattle] = x.Clone();
      _entities.ResetStats();
      if (_battles.Count < MaxBattleCount) {
        _battles.Add(null);
      } else {
        ++_droppedBattles;
      }
      _currentBattle = (_currentBattle + 1) % _battles.Count;
      _battles[_currentBattle] = new Battle() {
        StartTime = DateTime.UtcNow,
        Players = _entities.Players,
        StartReason = reason,
      };
    }
  }
}
