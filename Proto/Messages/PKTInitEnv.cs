namespace LaMetrum {
  class PKTInitEnv : IMessage {
    public PKTInitEnv(FieldReader r) {
      TsReader reader = new(r);
      reader.array(
        reader.u16(),
        () => {
          reader.skip(2 * reader.u16());
          reader.skip(2 * reader.u16());
          reader.skip(2 * reader.u16());
        },
        64);
      reader.skip(2 * reader.u16());
      reader.u32();
      reader.u32();
      Unk.read11(reader);
      SamePlayer = reader.u8();
      PlayerId = reader.u64();
      reader.u64();
    }

    public const ushort OpCode = 57806;

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
