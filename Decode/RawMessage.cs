namespace LaMetrum {
  enum OpCode : ushort {
    PKTDeathNotify = LaMetrum.PKTDeathNotify.OpCode,
    PKTInitEnv = LaMetrum.PKTInitEnv.OpCode,
    PKTInitPC = LaMetrum.PKTInitPC.OpCode,
    PKTNewNpcSummon = LaMetrum.PKTNewNpcSummon.OpCode,
    PKTNewPC = LaMetrum.PKTNewPC.OpCode,
    PKTNewProjectile = LaMetrum.PKTNewProjectile.OpCode,
    PKTRaidResult = LaMetrum.PKTRaidResult.OpCode,
    PKTSkillDamageAbnormalMoveNotify = LaMetrum.PKTSkillDamageAbnormalMoveNotify.OpCode,
    PKTSkillDamageNotify = LaMetrum.PKTSkillDamageNotify.OpCode,
    PKTSkillStartNotify = LaMetrum.PKTSkillStartNotify.OpCode,
    PKTTriggerBossBattleStatus = LaMetrum.PKTTriggerBossBattleStatus.OpCode,
    PKTTriggerStartNotify = LaMetrum.PKTTriggerStartNotify.OpCode,
  }

  readonly struct RawMessage {
    public RawMessage(int unprocessedBytes, OpCode op, in ArraySegment<byte> data) {
      UnprocessedBytes = unprocessedBytes;
      OpCode = op;
      Data = data;
    }

    public int UnprocessedBytes { get; }
    public OpCode OpCode { get; }
    public ArraySegment<byte> Data { get; }

    public override string ToString() => new Printer()
        .Field(OpCode)
        .Field(Data)
        .Finish();
  }
}
