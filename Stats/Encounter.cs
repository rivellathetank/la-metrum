namespace LaMetrum.Stats {
  /*class Encounter : ITable {
    Battle _total = null;
    readonly List<Battle> _battles = new();

    public void Add(Battle battle) {
      Battles.Add(battle);
    }

    public void Rotate(BattleSeparator end) {
      if (Battles.Count > 0) {
        if (Battles[^1].IsEmpty) {
          Battles.RemoveAt(Battles.Count - 1);
        } else {
          Battles[^1].Seal(end);
        }
      }
      Battles.Add(new Battle(end));
    }

    public Battle Active() {
      Check(Battles.Count > 0);
      return Battles[^1];
    }

    public string KeyColumn => "BATTLE";

    public ITable Nested(string id) => id switch {
      "Deathless" => Deathless,
      "Full" => Full,
      _ => throw new Exception($"Invalid {nameof(id)}: {Strings.Quote(id)}"),
    };

    public void UpdateContext(Context ctx) {}

    public IEnumerable<Row> Print(Context ctx) {
      Table<Extent, Context> t = new(ctx, KeyColumn);
      t.AddCol(Align.Left, x => x.T0);
      t.AddCol(Align.Left, x => x.T);
      t.AddCol(Align.Left, x => x.START);
      t.AddCol(Align.Left, x => x.END);

      t.AddCol(Align.Right, x => x.DMG);
      t.AddCol(Align.Right, x => x.DPS);
      t.AddCol(Align.Right, x => x.DEATHS);
      t.AddCol(Align.Right, x => x.KILLS);

      Check(Deathless.Total.Total.TotalDamage <= Full.Total.Total.TotalDamage);

      t.AddRow("Full", Full, Next(Full.Total.Total.TotalDamage));
      t.AddRow("Deathless", Deathless, Deathless.Total.Total.TotalDamage);

      return t.Print();
    }

    static double Next(double x) => BitConverter.UInt64BitsToDouble(BitConverter.DoubleToUInt64Bits(x) + 1);

    public string START(Context ctx) => Full.START(ctx);
    public string END(Context ctx) => Full.END(ctx);
    public string T0(Context ctx) => Full.T0(ctx);
    public string T(Context ctx) => Full.T(ctx);
    public string DMG(Context ctx) => Full.DMG(ctx);
    public string DPS(Context ctx) => Full.DPS(ctx);
    public string DEATHS(Context ctx) => Full.DEATHS(ctx);
    public string KILLS(Context ctx) => Full.KILLS(ctx);
  }*/
}
