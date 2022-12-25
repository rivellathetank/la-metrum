namespace LaMetrum {
  class PKTTriggerStartNotify : IMessage {
    public PKTTriggerStartNotify(FieldReader r) {
      TsReader reader = new(r);
      reader.u32();
      reader.skip(reader.u16() * 8);
      reader.u64();
      TriggerSignalType = (TriggerSignalType)reader.u32();
    }

    public const ushort OpCode = 50016;

    public void Validate() {}

    public R Visit<R, A>(IMessageVisitor<R, A> v, A arg) => v.Visit(this, arg);

    public override string ToString() => new Printer()
        .Field(TriggerSignalType)
        .Finish();

    public TriggerSignalType TriggerSignalType { get; }
  }
}
