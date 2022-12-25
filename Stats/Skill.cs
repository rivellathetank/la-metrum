namespace LaMetrum.Stats {
  class Skill : ISheet, IScope {
    public Skill(string name, IScope scope) {
      Name = name;
      Scope = scope;
      Total = new(scope);
    }

    public string Name { get; }

    public IScope Scope { get; }

    public SkillBuild Build { get; private set; }

    public Dictionary<string, Damage> Effects { get; private set; } = new();
    public Damage Total { get; }

    public void SetBuild(SkillBuild build) {
      Build = build;
      foreach (Damage dmg in Effects.Values) dmg.Skill = build;
    }

    public void AddHit(PlayerHit hit) {
      Total.AddHit(hit);
      if (!Effects.TryGetValue(hit.HitSource.EffectName, out Damage stats)) {
        stats = new(this);
        Effects.Add(hit.HitSource.EffectName, stats);
      }
      stats.AddHit(hit);
    }

    public long TotalDamage => Total.TotalDamage;
    public long NumHits => Total.NumHits;
    public TimeSpan Duration => Scope.Duration;

    public string KeyColumn => "EFFECT";

    public ITable ToTable() {
      Table<Damage> t = new( KeyColumn);
      t.AddScalar(LV);
      t.AddScalar(TRI);
      t.AddScalar(TLV);

      t.AddCol(Align.Right, x => x.HITS);
      t.AddCol(Align.Right, x => x.DMG);
      t.AddCol(Align.Right, x => x.DPS);
      t.AddCol(Align.Right, x => x.DMG_PCT);
      t.AddCol(Align.Right, x => x.CRIT_HIT_PCT);
      t.AddCol(Align.Right, x => x.CRIT_DMG_PCT);
      t.AddCol(Align.Right, x => x.POS_HIT_PCT);
      t.AddCol(Align.Right, x => x.POS_DMG_PCT);

      foreach ((string k, Damage v) in Effects.OrderBy(kv => -kv.Value.Total.TotalDamage).ThenBy(kv => kv.Key)) {
        t.AddRow(k, v, v.Total.TotalDamage);
      }

      return t;
    }

    public ISheet Nested(string id) => Effects[id];

    public string LV => Build?.SkillLevel.ToString();
    public string TRI => Build?.FormatTripods();
    public string TLV => Build?.FormatTripodLevels();

    public string HITS => Total.HITS;
    public string DMG => Total.DMG;
    public string DPS => Total.DPS;
    public string DMG_PCT => Total.DMG_PCT;
    public string CRIT_HIT_PCT => Total.CRIT_HIT_PCT;
    public string CRIT_DMG_PCT => Total.CRIT_DMG_PCT;
    public string POS_HIT_PCT => Total.POS_HIT_PCT;
    public string POS_DMG_PCT => Total.POS_DMG_PCT;
  }
}
