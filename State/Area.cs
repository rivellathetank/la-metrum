namespace LaMetrum {
  class Gate {
    public int Id { get; set; }
    public Battle FirstDeath { get; set; }
  }

  class Area {
    public List<Battle> Battles { get; } = new();

  }
}
