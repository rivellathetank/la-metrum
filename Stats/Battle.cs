namespace LaMetrum.Stats {
  class Battle : ISheet, IScope {
    public Battle(BattleSeparator start, IScope scope) {
      Scope = scope;
      Total = new(scope);
      DateTime now = DateTime.UtcNow;
      Deathless = new Extent(start, now, deathless: true, this);
      Full = new Extent(start, now, deathless: false, this);
    }

    public IScope Scope { get; }

    public Damage Total { get; set; }

    public Extent Deathless { get; }
    public Extent Full { get; }

    public bool IsEmpty => Full.Total.TotalDamage == 0 && Full.DeathCount == 0;

    public void AddPlayer(NewPlayer player) {
      Full.AddPlayer(player);
      Deathless.AddPlayer(player);
    }

    public void AddHit(PlayerHit hit) {
      Total.AddHit(hit);
      Full.AddHit(hit);
      Deathless.AddHit(hit);
    }

    public void UpdateSkill(PlayerSkillStart skill) {
      Full.UpdateSkill(skill);
      Deathless.UpdateSkill(skill);
    }

    public void RecordKilling(PlayerKill k) {
      Full.RecordKilling(k);
      Deathless.RecordKilling(k);
    }

    public void Seal(BattleSeparator end) {
      Full.Seal(end);
      Deathless.Seal(end);
    }

    public long TotalDamage => Total.TotalDamage;

    public long NumHits => Total.NumHits;

    public TimeSpan Duration => Full.Duration;

    public string KeyColumn => "EXTENT";

    public ISheet Nested(string id) => id switch {
      "Deathless" => Deathless,
      "Full" => Full,
      _ => throw new Exception($"Invalid {nameof(id)}: {Strings.Quote(id)}"),
    };

    public ITable ToTable() {
      Table<Extent> t = new(KeyColumn);
      t.AddCol(Align.Left, x => x.T0);
      t.AddCol(Align.Left, x => x.T);
      t.AddCol(Align.Left, x => x.START);
      t.AddCol(Align.Left, x => x.END);

      t.AddCol(Align.Right, x => x.DMG);
      t.AddCol(Align.Right, x => x.DPS);
      t.AddCol(Align.Right, x => x.DEATHS);
      t.AddCol(Align.Right, x => x.KILLS);

      Check(Deathless.Total.Total.TotalDamage <= Full.Total.Total.TotalDamage);

      t.AddRow("Full", Full, Full.TotalDamage, order: 0);
      t.AddRow("Deathless", Deathless, Deathless.TotalDamage, order: 1);

      return t;
    }

    public string START => Full.START;
    public string END => Full.END;
    public string T0 => Full.T0;
    public string T => Full.T;
    public string DMG => Full.DMG;
    public string DPS => Full.DPS;
    public string DEATHS => Full.DEATHS;
    public string KILLS => Full.KILLS;
  }
}
