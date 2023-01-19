namespace LaMetrum {
  class PKTInitEnv : IMessage {
    public PKTInitEnv(FieldReader r) {
      TsReader reader = new(r);
      reader.u32();
      reader.u64();
      reader.u32();
      reader.str(128);
      PlayerId = reader.u64();
      Unk.read11(reader);
      SamePlayer = reader.u8();
      reader.array(
        reader.u16(),
        () => {
          reader.str(32);
          reader.str(64);
          reader.str(128);
        },
        64);
    }

    public const ushort OpCode = 16275;

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
