namespace LaMetrum {
  class PKTNewNpcSummon : IMessage {
    public PKTNewNpcSummon(FieldReader r) {
      TsReader reader = new(r);
      reader.skip(5);
      OwnerId = reader.u64();
      reader.skip(26);
      reader.u8();
      NpcData = new(r);
    }

    public const ushort OpCode = 13729;

    public void Validate() {
      Check(OwnerId <= (ulong.MaxValue >> 16), OwnerId);
      Check(NpcId <= (ulong.MaxValue >> 16), NpcId);
    }

    public ulong NpcId => NpcData.ObjectId;

    public R Visit<R, A>(IMessageVisitor<R, A> v, A arg) => v.Visit(this, arg);

    public override string ToString() => new Printer()
        .Field(OwnerId)
        .Field(NpcId)
        .Finish();

    public ulong OwnerId { get; }
    NpcData NpcData { get; }
  }
}
