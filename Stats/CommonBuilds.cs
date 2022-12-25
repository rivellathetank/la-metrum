namespace LaMetrum.Stats {
  static class CommonBuilds {
    public static PlayerBuild? Infer(Player p) => p.Class switch {
      Class.Sorceress => InferSorceress(p),
      _ => null,
    };

    static PlayerBuild? InferSorceress(Player p) {
      // Used Arcane Rupture => Igniter.
      if (p.Skills.ContainsKey("Arcane Rupture")) return PlayerBuild.Sorceress_Igniter;
      // Punishing Strike with Unavoidable Fate => Instant Reflux.
      if (Tripod(p, "Punishing Strike", 2) == 1) return PlayerBuild.Sorceress_InstantReflux;
      // Punishing Strike with Final Strike => Casting Reflux.
      if (Tripod(p, "Punishing Strike", 3) == 2) return PlayerBuild.Sorceress_CastingReflux;
      // Rime Arrow with Enlightnment => Igniter.
      if (Tripod(p, "Rime Arrow", 1) == 2) return PlayerBuild.Sorceress_Igniter;
      return null;
    }

    static int? Tripod(Player p, string skill, int row) =>
        p.Skills.TryGetValue(skill, out Skill s) ? s.Build?.Tripod(row) : null;
  }
}
