namespace LaMetrum {
  class PKTSkillDamageAbnormalMoveNotify : IMessage {
    public PKTSkillDamageAbnormalMoveNotify(FieldReader r) {
      TsReader reader = new(r);
      reader.u8();
      SourceId = reader.u64();
      reader.u32();
      SkillDamageAbnormalMoveEvents = reader.array<SkillDamageAbnormalMoveEvent>();
      SkillId = reader.u32();
      SkillEffectId = reader.u32();
    }

    public const ushort OpCode = 10555;

    public void Validate() {
      Check(SourceId <= (ulong.MaxValue >> 16), SourceId);
      Check(SkillEffectId <= int.MaxValue, SkillEffectId);
      Check(SkillId <= int.MaxValue, SkillId);
      foreach (SkillDamageEvent x in Targets) x.Validate();
    }

    public R Visit<R, A>(IMessageVisitor<R, A> v, A arg) => v.Visit(this, arg);

    public IEnumerable<SkillDamageEvent> Targets => SkillDamageAbnormalMoveEvents.Select(
        x => x.skillDamageEvent);

    public override string ToString() => new Printer()
        .Field(SourceId)
        .Field(SkillId)
        .Field(SkillEffectId)
        .Field(Targets)
        .Finish();

    public uint SkillEffectId { get; }
    public ulong SourceId { get; }
    public SkillDamageAbnormalMoveEvent[] SkillDamageAbnormalMoveEvents { get; }
    public uint SkillId { get; }
  }
}
