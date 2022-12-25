namespace LaMetrum {
  static class Build {
    public static string Guess(Entity player) {
      Check(player.Type == EntityType.Player);
      return player.Class.Value switch {
        Class.Sorceress => GuessSorceress(player),
        _ => null,
      };
    }

    static string GuessSorceress(Entity player) {
      if (!player.SkillStats.TryGetValue("Punishing Strike", out SkillStats ps)) return null;
      if (ps.Build is null) return null;
      if (ps.Build.Tripods[1] == 0) return null;
      if (ps.Build.Tripods[1] == 1) return "Instant Reflux";  // Unavoidable Fate
      if (ps.Build.Tripods[2] == 0) return null;
      if (ps.Build.Tripods[2] == 2) return "Casting Reflux";  // Final Strike
      if (!player.SkillStats.TryGetValue("Rime Arrow", out SkillStats ra)) return null;
      if (ra.Build is null) return null;
      if (ra.Build.Tripods[0] == 2) return "Igniter";         // Enlightenment
      return null;
    }
  }
}
