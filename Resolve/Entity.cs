namespace LaMetrum.Resolve {
  // Either player or player-created projectile/minion.
  class Entity {
    readonly TimeSpan _ttl;

    // Player.
    public Entity(ulong id, string name) {
      Check(name is not null);
      Id = id;
      Name = name;
    }

    // Player-created projectile/minion.
    public Entity(ulong id, ulong owner, TimeSpan ttl) {
      Id = id;
      OwnerId = owner;
      _ttl = ttl;
      ExpiresAt = DateTime.UtcNow + _ttl;
    }

    public ulong Id { get; }
    // Not null only for players.
    public string Name { get; }
    // Null only for players.
    public ulong? OwnerId { get; }
    // Null only for players.
    public DateTime? ExpiresAt { get; private set; }

    public bool IsPlayer => Name is not null;

    public void Refresh() {
      Check(!IsPlayer);
      ExpiresAt = DateTime.UtcNow + _ttl;
    }

    public override string ToString() => new Printer()
        .Field(Id)
        .Field(Name)
        .Field(OwnerId)
        .Finish();
  }
}
