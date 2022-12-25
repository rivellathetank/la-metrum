namespace LaMetrum.Stats {
  enum PlayerBuild : uint {
    Sorceress_CastingReflux = Class.Sorceress << 8 | 1,
    Sorceress_InstantReflux = Class.Sorceress << 8 | 2,
    Sorceress_Igniter = Class.Sorceress << 8 | 3,
  }

  record Death(DateTime Timestamp, string SourcePlayerName);

  class Player : ISheet, IScope {
    const string NullSkill = "OTHER";

    public Player(NewPlayer p, IScope scope) {
      Scope = scope;
      Name = p.Name;
      Class = p.Class;
      ItemLevel = p.ItemLevel;
      You = p.You;
      Total = new(scope);
      Build = CommonBuilds.Infer(this);
    }

    public IScope Scope { get; }

    public string Name { get; }
    public Class Class { get; }
    public float? ItemLevel { get; }
    public bool You { get; }

    public PlayerBuild? Build { get; private set; }

    public List<Death> Deaths { get; private set; } = new();
    public int KillCount { get; private set; }

    public Dictionary<string, Skill> Skills { get; private set; } = new();
    public Damage Total { get; }

    public void AddHit(PlayerHit hit) {
      Total.AddHit(hit);
      if (!Skills.TryGetValue(hit.HitSource.SkillName, out Skill stats)) {
        stats = new Skill(hit.HitSource.SkillName, this);
        Skills.Add(hit.HitSource.SkillName, stats);
      }
      stats.AddHit(hit);
    }

    public void UpdateSkill(PlayerSkillStart skill) {
      if (Skills.TryGetValue(skill.SkillName, out Skill stats)) {
        if (stats.Build == skill.SkillBuild) return;
        stats.SetBuild(skill.SkillBuild);
      } else {
        stats = new Skill(skill.SkillName, this);
        stats.SetBuild(skill.SkillBuild);
        Skills.Add(skill.SkillName, stats);
      }
      Build ??= CommonBuilds.Infer(this);
    }

    public void RecordDeath(PlayerKill kill) {
      Check(kill.TargetPlayerName == Name);
      Deaths.Add(new Death(DateTime.UtcNow, kill.SourcePlayerName));
    }

    public void RecordKill(PlayerKill kill) {
      Check(kill.SourcePlayerName == Name);
      ++KillCount;
    }

    public long TotalDamage => Total.TotalDamage;

    public long NumHits => Total.NumHits;

    // TODO: this is time alive.
    public TimeSpan Duration { get; private set; }

    public string KeyColumn => "SKILL";

    public ITable ToTable() {
      Table<Skill> t = new(KeyColumn);
      t.AddScalar(CLASS);
      t.AddScalar(ILVL);
      t.AddScalar(BUILD);

      t.AddCol(Align.Right, x => x.LV);
      t.AddCol(Align.Right, x => x.TRI);
      t.AddCol(Align.Right, x => x.TLV);

      t.AddCol(Align.Right, x => x.HITS);
      t.AddCol(Align.Right, x => x.DMG);
      t.AddCol(Align.Right, x => x.DPS);
      t.AddCol(Align.Right, x => x.DMG_PCT);
      t.AddCol(Align.Right, x => x.CRIT_HIT_PCT);
      t.AddCol(Align.Right, x => x.CRIT_DMG_PCT);
      t.AddCol(Align.Right, x => x.POS_HIT_PCT);
      t.AddCol(Align.Right, x => x.POS_DMG_PCT);

      foreach ((string k, Skill v) in Skills.OrderBy(kv => -kv.Value.Total.Total.TotalDamage).ThenBy(kv => kv.Key)) {
        t.AddRow(k.Length == 0 ? NullSkill : k, v, v.Total.Total.TotalDamage);
      }

      return t;
    }

    public ISheet Nested(string id) => Skills[id == NullSkill ? "" : id];

    public string CLASS => Class.ToString();
    public string ILVL => ItemLevel?.ToString();
    public string BUILD => Build?.ToString();

    public string HITS => Total.HITS;
    public string DMG => Total.DMG;
    public string DMG_PCT => Total.DMG_PCT;
    public string CRIT_HIT_PCT => Total.CRIT_HIT_PCT;
    public string CRIT_DMG_PCT => Total.CRIT_DMG_PCT;
    public string POS_HIT_PCT => Total.POS_HIT_PCT;
    public string POS_DMG_PCT => Total.POS_DMG_PCT;

    public string DPS => Total.DPS;

    public string DEATHS => Fmt.SI(Deaths.Count);
    public string KILLS => Fmt.SI(KillCount);

    public string ALIVE => Fmt.MinSec(Duration);
  }
}
