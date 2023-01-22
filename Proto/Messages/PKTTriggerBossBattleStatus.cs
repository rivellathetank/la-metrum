namespace LaMetrum {
  class PKTTriggerBossBattleStatus : IMessage {
    public PKTTriggerBossBattleStatus(FieldReader r) {
      r.Discard();
    }

    public const ushort OpCode = 51236;

    public void Validate() { }

    public R Visit<R, A>(IMessageVisitor<R, A> v, A arg) => v.Visit(this, arg);

    public override string ToString() => new Printer().Finish();
  }
}
