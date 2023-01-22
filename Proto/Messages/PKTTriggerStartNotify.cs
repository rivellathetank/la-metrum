namespace LaMetrum {
  class PKTTriggerStartNotify : IMessage {
    public PKTTriggerStartNotify(FieldReader r) {
      TsReader reader = new(r);
      reader.array(reader.u16(), () => reader.u64(), 40);
      reader.u32();
      TriggerSignalType = (TriggerSignalType)reader.u32();
      reader.u64();
    }

    public const ushort OpCode = 4099;

    public void Validate() {}

    public R Visit<R, A>(IMessageVisitor<R, A> v, A arg) => v.Visit(this, arg);

    public override string ToString() => new Printer()
        .Field(TriggerSignalType)
        .Finish();

    public TriggerSignalType TriggerSignalType { get; }
  }
}
