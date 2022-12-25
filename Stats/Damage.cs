namespace LaMetrum.Stats {
  class Damage : ISheet, IScope {
    readonly Hit _regular;
    readonly Hit _frontAttack;
    readonly Hit _backAttack;

    public Damage(IScope scope) {
      _regular = new(this);
      _frontAttack = new(this);
      _backAttack = new(this);
      Total = new(scope);
      Scope = scope;
    }

    public IScope Scope { get; }

    public Hit Total { get; }

    public SkillBuild Skill { get; set; }

    public Hit Get(HitType type) => type switch {
      HitType.Regular => _regular,
      HitType.BackAttack => _backAttack,
      HitType.FrontAttack => _frontAttack,
      _ => throw new Exception($"invalid {nameof(HitType)}: {type}"),
    };

    public void AddHit(PlayerHit hit) {
      Get(hit.Type).AddHit(hit);
      Total.AddHit(hit);
    }

    public string KeyColumn => "ATTACK";

    public long TotalDamage => Total.TotalDamage;

    public long NumHits => Total.NumHits;

    public TimeSpan Duration => Scope.Duration;

    public ITable ToTable() {
      Table<Hit> t = new(KeyColumn);
      if (Skill is not null) {
        t.AddScalar("LV", Skill.SkillLevel.ToString());
        t.AddScalar("TRI", Skill.FormatTripods());
        t.AddScalar("TLV", Skill.FormatTripodLevels());
      }

      t.AddCol(Align.Right, x => x.HITS);
      t.AddCol(Align.Right, x => x.DMG);
      t.AddCol(Align.Right, x => x.DPS);
      t.AddCol(Align.Right, x => x.DMG_PCT);
      t.AddCol(Align.Right, x => x.HIT_PCT);
      t.AddCol(Align.Right, x => x.CRIT_HIT_PCT);
      t.AddCol(Align.Right, x => x.CRIT_DMG_PCT);

      t.AddRow("Regular", _regular, _regular.TotalDamage);
      t.AddRow("Front Attack", _frontAttack, _frontAttack.TotalDamage);
      t.AddRow("Back Attack", _backAttack, _backAttack.TotalDamage);

      return t;
    }

    public ISheet Nested(string id) => null;

    public string HITS => Total.HITS;
    public string DMG => Total.DMG;
    public string DPS => Total.DPS;
    public string DMG_PCT => Total.DMG_PCT;
    public string CRIT_HIT_PCT => Total.CRIT_HIT_PCT;
    public string CRIT_DMG_PCT => Total.CRIT_DMG_PCT;

    public string POS_HIT_PCT =>
        Fmt.Pct(_frontAttack.NumHits + _backAttack.NumHits, Total.NumHits);
    public string POS_DMG_PCT =>
        Fmt.Pct(_frontAttack.TotalDamage + _backAttack.TotalDamage, Total.TotalDamage);
  }
}
