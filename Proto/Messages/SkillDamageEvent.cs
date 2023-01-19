namespace LaMetrum {
  class SkillDamageEvent {
    public SkillDamageEvent(FieldReader r) {
      TsReader reader = new(r);
      reader.i16();
      CurHp = reader.ReadNBytesInt64();
      Damage = reader.ReadNBytesInt64();
      if (reader.bl()) reader.u8();
      reader.u8();
      Modifier = reader.u8();
      TargetId = reader.u64();
      MaxHp = reader.ReadNBytesInt64();
    }

    public void Validate() {
      const long Max = 1L << 48;
      Check(CurHp >= -Max && CurHp <= Max, CurHp);
      Check(Damage >= 0 && Damage <= Max, Damage);
      Check(MaxHp >= 0 && MaxHp <= Max, MaxHp);
      Check(TargetId <= (ulong.MaxValue >> 16), TargetId);
    }

    public HitFlag HitFlag => (HitFlag)(Modifier & 0x0F);
    public HitOption HitOption => (HitOption)((Modifier >> 4) & 0x7);

    public override string ToString() => new Printer()
        .Field(TargetId)
        .Field(Damage)
        .Field(CurHp)
        .Field(MaxHp)
        .Field(HitFlag)
        .Field(HitOption)
        .Finish();

    public long Damage { get; }
    public long CurHp { get; }
    public long MaxHp { get; }
    public ulong TargetId { get; }
    byte Modifier { get; }
  }
}
