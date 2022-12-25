namespace LaMetrum {
  class PKTSkillStartNotify : IMessage {
    public PKTSkillStartNotify(FieldReader r) {
      TsReader reader = new(r);
      reader.Vector3F();
      reader.Vector3F();
      reader.Angle();
      if (reader.bl()) reader.Angle();
      SkillId = reader.u32();
      reader.Vector3F();
      SourceId = reader.u64();
      if (reader.bl()) reader.i32();
      SkillOptionData = reader.ReadFlagBytes();
      if (reader.bl()) reader.u32();
      SkillLevel = reader.u8();
      reader.Angle();

      if (SkillOptionData[6] is not null) {
        Check(SkillOptionData[6].Length == 6);
        TripodLevels = new[] {
          SkillOptionData[6][0],
          SkillOptionData[6][2],
          SkillOptionData[6][4]
        };
      }
    }

    public const ushort OpCode = 45202;

    public void Validate() {
      Check(SkillId <= int.MaxValue);
      Check(SkillLevel > 0 && SkillLevel <= 12, SkillLevel);
      if (Tripods is not null) {
        foreach (byte tripod in Tripods) Check(tripod <= 3, tripod);
      }
      if (TripodLevels is not null) {
        foreach (byte lvl in TripodLevels) Check(lvl >= 1 && lvl <= 5, lvl);
      }
    }

    public byte[] Tripods => SkillOptionData[5];
    public byte[] TripodLevels { get; }

    public R Visit<R, A>(IMessageVisitor<R, A> v, A arg) => v.Visit(this, arg);

    public override string ToString() => new Printer()
        .Field(SourceId)
        .Field(SkillId)
        .Field(SkillLevel)
        .Field(Tripods)
        .Finish();

    public uint SkillId { get; }
    public ulong SourceId { get; }
    public byte SkillLevel { get; }
    byte[][] SkillOptionData { get; }
  }
}
