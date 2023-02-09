namespace LaMetrum {
  class PKTNewProjectile : IMessage {
    public PKTNewProjectile(FieldReader r) {
      TsReader reader = new(r);
      projectileInfo = new ProjectileInfo(r);
    }

    public const ushort OpCode = 38002;

    public void Validate() {
      Check(OwnerId <= (ulong.MaxValue >> 16), OwnerId);
      Check(ProjectileId <= (ulong.MaxValue >> 16), ProjectileId);
      Check(SkillEffect <= int.MaxValue, SkillEffect);
      Check(SkillId <= int.MaxValue, SkillId);
      if (Tripods is not null) {
        Check(Tripods.Length == 3, Tripods.Length);
        foreach (byte tripod in Tripods) Check(tripod <= 3, tripod);
      }
      Check(SkillLevel > 0 && SkillLevel <= 12, SkillLevel);
    }

    public ulong OwnerId => projectileInfo.OwnerId;
    public ulong ProjectileId => projectileInfo.ProjectileId;
    public uint SkillId => projectileInfo.SkillId;
    public uint SkillEffect => projectileInfo.SkillEffect;
    public byte SkillLevel => projectileInfo.SkillLevel;
    public byte[] Tripods => projectileInfo.tripodIndex;

    public R Visit<R, A>(IMessageVisitor<R, A> v, A arg) => v.Visit(this, arg);

    public override string ToString() => new Printer()
        .Field(OwnerId)
        .Field(ProjectileId)
        .Field(SkillId)
        .Field(SkillEffect)
        .Field(SkillLevel)
        .Field(Tripods)
        .Finish();

    public ProjectileInfo projectileInfo { get; }
  }
}
