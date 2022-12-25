namespace LaMetrum {
  class PKTSkillDamageNotify : IMessage {
    public PKTSkillDamageNotify(FieldReader r) {
      TsReader reader = new(r);
      SkillDamageEvents = reader.array<SkillDamageEvent>();
      SkillId = reader.u32();
      SourceId = reader.u64();
      SkillLevel = reader.u8();
      SkillEffectId = reader.u32();
    }

    public const ushort OpCode = 1847;

    public void Validate() {
      Check(SourceId <= (ulong.MaxValue >> 16), SourceId);
      Check(SkillEffectId <= int.MaxValue, SkillEffectId);
      Check(SkillId <= int.MaxValue, SkillId);
      foreach (SkillDamageEvent x in Targets) x.Validate();
    }

    public R Visit<R, A>(IMessageVisitor<R, A> v, A arg) => v.Visit(this, arg);

    public IEnumerable<SkillDamageEvent> Targets => SkillDamageEvents;

    public override string ToString() => new Printer()
        .Field(SourceId)
        .Field(SkillId)
        .Field(SkillEffectId)
        .Field(Targets)
        .Finish();

    public uint SkillEffectId { get; }
    public ulong SourceId { get; }
    public SkillDamageEvent[] SkillDamageEvents { get; }
    public uint SkillId { get; }
    public byte SkillLevel { get; }
  }
}
