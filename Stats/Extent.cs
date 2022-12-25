namespace LaMetrum.Stats {
  enum BattleSeparator : uint {
    GuardianRaidMask = ZoneEventType.GuardianRaidMask,
    GuardianRaidEnd = ZoneEventType.GuardianRaidEnd,

    CoopMask = ZoneEventType.CoopMask,
    CoopStart = ZoneEventType.CoopStart,
    CoopSuccess = ZoneEventType.CoopSuccess,
    CoopFail = ZoneEventType.CoopFail,
    CoopCancel = ZoneEventType.CoopCancel,

    SubZoneMask = ZoneEventType.SubZoneMask,
    SubZoneEnter = ZoneEventType.SubZoneEnter,

    GateEndMask = ZoneEventType.GateEndMask,
    GateSuccessMask = ZoneEventType.GateSuccessMask,
    Gate1Success1 = ZoneEventType.Gate1Success,
    Gate2Success2 = ZoneEventType.Gate2Success,
    Gate3Success3 = ZoneEventType.Gate3Success,
    Gate4Success4 = ZoneEventType.Gate4Success,
    Gate5Success5 = ZoneEventType.Gate5Success,
    GateFailMask = ZoneEventType.GateFailMask,
    Gate1Fail = ZoneEventType.GateFail1,
    Gate2Fail = ZoneEventType.GateFail2,
    Gate3Fail = ZoneEventType.GateFail3,
    Gate4Fail = ZoneEventType.GateFail4,
    Gate5Fail = ZoneEventType.GateFail5,

    Launch = 0x80000001,
    ZoneChange = 0x80000002,
    Manual = 0x80000003,
    FirstDeath = 0x80000004,
  }

  class Extent : ISheet, IScope {
    static readonly TimeSpan CommonGrave = TimeSpan.FromSeconds(1);

    BattleSeparator? _endEvent = null;
    DateTime? _endTime = null;

    public Extent(BattleSeparator start, DateTime now, bool deathless, IScope scope) {
      Scope = scope;
      StartEvent = start;
      StartTime = now;
      Deathless = deathless;
      Total = new(scope);
    }

    public IScope Scope { get; }

    public Dictionary<string, Player> Players { get; private set; } = new();
    public Damage Total { get; }

    public BattleSeparator StartEvent { get; }
    public DateTime StartTime { get; }
    public bool Deathless { get; }

    public BattleSeparator? EndEvent(DateTime now) {
      if (_endEvent is not null) return _endEvent;
      if (!Deathless || FirstDeath is null) return null;
      if (now - FirstDeath.Value >= CommonGrave) return BattleSeparator.FirstDeath;
      return null;
    }

    public DateTime? EndTime(DateTime now) => EndEvent(now) == BattleSeparator.FirstDeath
        ? FirstDeath.Value + CommonGrave : _endTime;

    public DateTime? FirstDeath { get; private set; }

    public int KillCount { get; private set; }
    public int DeathCount { get; private set; }

    public void AddPlayer(NewPlayer player) {
      Check(_endEvent is null);
      if (EndEvent(DateTime.UtcNow) is not null) return;
      Check(!Players.ContainsKey(player.Name));
      Players.Add(player.Name, new Player(player, this));
    }

    public void AddHit(PlayerHit hit) {
      Check(_endEvent is null);
      if (EndEvent(DateTime.UtcNow) is not null) return;
      Players.GetValueOrDefault(hit.SourcePlayerName)?.AddHit(hit);
    }

    public void UpdateSkill(PlayerSkillStart skill) {
      Check(_endEvent is null);
      if (EndEvent(DateTime.UtcNow) is not null) return;
      Players.GetValueOrDefault(skill.PlayerName)?.UpdateSkill(skill);
    }

    public void RecordKilling(PlayerKill k) {
      Check(_endEvent is null);
      if (EndEvent(DateTime.UtcNow) is not null) return;
      if (k.SourcePlayerName is not null && Players.TryGetValue(k.TargetPlayerName, out Player source)) {
        source.RecordKill(k);
        ++KillCount;
      }
      if (k.TargetPlayerName is not null && Players.TryGetValue(k.TargetPlayerName, out Player target)) {
        FirstDeath ??= DateTime.UtcNow;
        target.RecordDeath(k);
        ++DeathCount;
      }
    }

    public void Seal(BattleSeparator end) {
      Check(_endEvent is null);
      _endTime = EndTime(DateTime.UtcNow);
      if (_endTime.HasValue) {
        _endEvent = BattleSeparator.FirstDeath;
      } else {
        _endEvent = end;
        _endTime = DateTime.UtcNow;
      }
    }

    // TODO: implement this.
    public TimeSpan Duration { get; private set; }
    public long TotalDamage => Total.TotalDamage;
    public long NumHits => Total.NumHits;

    public string KeyColumn => "PLAYER";

    public ISheet Nested(string id) => Players[id];

    public ITable ToTable() {
      Table<Player> t = new(KeyColumn);
      t.AddScalar(START);
      t.AddScalar(END);
      t.AddScalar(T0);
      t.AddScalar(T);
      t.AddScalar(DMG);
      t.AddScalar(DPS);

      t.AddCol(Align.Left, x => x.CLASS);
      t.AddCol(Align.Right, x => x.ILVL);
      t.AddCol(Align.Left, x => x.BUILD);

      t.AddCol(Align.Right, x => x.HITS);
      t.AddCol(Align.Right, x => x.DMG);
      t.AddCol(Align.Right, x => x.DPS);
      t.AddCol(Align.Right, x => x.DMG_PCT);
      t.AddCol(Align.Right, x => x.CRIT_HIT_PCT);
      t.AddCol(Align.Right, x => x.CRIT_DMG_PCT);
      t.AddCol(Align.Right, x => x.POS_HIT_PCT);
      t.AddCol(Align.Right, x => x.POS_DMG_PCT);
      t.AddCol(Align.Right, x => x.DEATHS);
      t.AddCol(Align.Right, x => x.KILLS);

      foreach ((string k, Player v) in Players.OrderBy(kv => -kv.Value.Total.Total.TotalDamage).ThenBy(kv => kv.Key)) {
        t.AddRow(k, v, v.Total.Total.TotalDamage);
      }

      return t;
    }

    public string START => StartEvent.ToString();
    // TODO: implement this.
    public string END => throw new NotImplementedException(nameof(END));
    public string T0 => Fmt.LocalTime(StartTime);
    public string T => Fmt.MinSec(Duration);

    public string DMG => Total.DMG;
    public string DPS => Total.DPS;
    public string DEATHS => Fmt.SI(DeathCount);
    public string KILLS => Fmt.SI(KillCount);
  }
}
