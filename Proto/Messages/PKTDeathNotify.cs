namespace LaMetrum {
  class PKTDeathNotify : IMessage {
    public PKTDeathNotify(FieldReader r) {
      TsReader reader = new(r);
      TargetId = reader.u64();
      reader.u32();
      reader.u32();
      reader.u64();
      reader.u8();
      SourceId = reader.u64();
      if (reader.bl()) reader.u8();
      reader.u16();
      if (reader.bl()) reader.u8();
      if (reader.bl()) reader.u8();
    }

    public const ushort OpCode = 21574;

    public void Validate() {
      Check(SourceId <= (ulong.MaxValue >> 16), SourceId);
      Check(TargetId <= (ulong.MaxValue >> 16), TargetId);
    }

    public R Visit<R, A>(IMessageVisitor<R, A> v, A arg) => v.Visit(this, arg);

    public override string ToString() => new Printer()
        .Field(SourceId)
        .Field(TargetId)
        .Finish();

    public ulong TargetId { get; }
    public ulong SourceId { get; }
  }
}
