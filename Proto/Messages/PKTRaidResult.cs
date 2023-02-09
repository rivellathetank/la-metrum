namespace LaMetrum {
  class PKTRaidResult : IMessage {
    public PKTRaidResult(FieldReader reader) {
      reader.Discard();
    }

    public const ushort OpCode = 6589;

    public void Validate() { }

    public R Visit<R, A>(IMessageVisitor<R, A> v, A arg) => v.Visit(this, arg);

    public override string ToString() => new Printer().Finish();
  }
}
