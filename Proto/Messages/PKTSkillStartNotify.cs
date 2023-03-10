namespace LaMetrum {
  class PKTSkillStartNotify : IMessage {
    public PKTSkillStartNotify(FieldReader r) {
      TsReader reader = new(r);
      Unk.read21(reader);
      SourceId = reader.u64();
      SkillLevel = reader.u8();
      Unk.read22(reader);
      if (reader.bl()) Unk.read21(reader);
      Unk.read22(reader);
      Unk.read22(reader);
      SkillOptionData = reader.ReadFlagBytes();
      Unk.read21(reader);
      SkillId = reader.u32();
      if (reader.bl()) reader.i32();
      if (reader.bl()) reader.u32();

      if (SkillOptionData[6] is not null) {
        Check(SkillOptionData[6].Length == 6);
        TripodLevels = new[] {
          SkillOptionData[6][0],
          SkillOptionData[6][2],
          SkillOptionData[6][4]
        };
      }
    }

    public const ushort OpCode = 38704;

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
