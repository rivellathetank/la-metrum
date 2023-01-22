namespace LaMetrum {
  class PKTInitEnv : IMessage {
    public PKTInitEnv(FieldReader r) {
      TsReader reader = new(r);
      Unk.read11(reader);
      reader.u8();
      PlayerId = reader.u64();
      reader.u64();
      reader.u32();
      reader.array<ushort>();
      reader.u32();
      reader.array(
        reader.u16(),
        () => {
          reader.array<ushort>();
          reader.array<ushort>();
          reader.array<ushort>();
        },
        64);
    }

    public const ushort OpCode = 43353;

    public void Validate() {
      Check(PlayerId <= (ulong.MaxValue >> 16), PlayerId);
      Check(SamePlayer <= 1, SamePlayer);
    }

    public R Visit<R, A>(IMessageVisitor<R, A> v, A arg) => v.Visit(this, arg);

    public override string ToString() => new Printer()
        .Field(PlayerId)
        .Field(SamePlayer)
        .Finish();

    public byte SamePlayer { get; }
    public ulong PlayerId { get; }
  }
}
