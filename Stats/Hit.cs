namespace LaMetrum.Stats {
  class Hit {
    public Hit(IScope scope) {
      Check(scope is not null);
      Scope = scope;
    }

    public IScope Scope { get; }

    public long NumHits { get; private set; }
    public long NumCrits { get; private set; }
    public long TotalDamage { get; private set; }
    public long CritDamage { get; private set; }

    public void AddHit(PlayerHit hit) {
      ++NumHits;
      TotalDamage += hit.EffectiveDamage;
      if (hit.Crit) {
        ++NumCrits;
        CritDamage += hit.EffectiveDamage;
      }
    }

    public string HITS => Fmt.SI(NumHits);
    public string DMG => Fmt.SI(TotalDamage);
    public string DPS => Fmt.SI(TotalDamage / Math.Max(1, Scope.Duration.TotalSeconds));
    public string DMG_PCT => Fmt.Pct(TotalDamage, Scope.TotalDamage);
    public string HIT_PCT => Fmt.Pct(NumHits, Scope.NumHits);
    public string CRIT_HIT_PCT => Fmt.Pct(NumCrits, NumHits);
    public string CRIT_DMG_PCT => Fmt.Pct(CritDamage, TotalDamage);
  }
}
