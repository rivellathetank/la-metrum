namespace LaMetrum {
  interface IMessage {
    R Visit<R, A>(IMessageVisitor<R, A> v, A arg);
    void Validate();
  }

  interface IMessageVisitor<R, A> {
    R Visit(PKTDeathNotify x, A arg);
    R Visit(PKTInitEnv x, A arg);
    R Visit(PKTInitPC x, A arg);
    R Visit(PKTNewNpcSummon x, A arg);
    R Visit(PKTNewPC x, A arg);
    R Visit(PKTNewProjectile x, A arg);
    R Visit(PKTRaidResult x, A arg);
    R Visit(PKTSkillDamageAbnormalMoveNotify x, A arg);
    R Visit(PKTSkillDamageNotify x, A arg);
    R Visit(PKTSkillStartNotify x, A arg);
    R Visit(PKTTriggerBossBattleStatus x, A arg);
    R Visit(PKTTriggerStartNotify x, A arg);
  }

  struct ParsedMessage {
    public ParsedMessage(OpCode op, IMessage data) {
      OpCode = op;
      Data = data;
    }

    public OpCode OpCode { get; }
    public IMessage Data { get; }
  }
}
