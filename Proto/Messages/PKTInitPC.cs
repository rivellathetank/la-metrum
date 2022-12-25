using System.Runtime.CompilerServices;

namespace LaMetrum {
  class PKTInitPC : IMessage {
    public PKTInitPC(FieldReader r) {
      TsReader reader = new(r);
      reader.bytes(25);
      reader.u16();
      reader.u32();
      ClassId = reader.u16();
      reader.u64();
      reader.u32();
      reader.u8();
      reader.u16();
      reader.u16();
      reader.skip(66);
      Level = reader.u16();
      reader.skip(44);
      reader.array(
        reader.u16(),
        () => {
          reader.ReadNBytesInt64();
          reader.u8();
        },
        152);
      reader.u32();
      reader.u8();
      reader.u16();
      reader.u32();
      reader.u8();
      reader.array(reader.u16(), () => new StatusEffectData(r), 80);
      reader.u8();
      reader.bytes(reader.u16(), 104, 30);
      reader.u64();
      reader.u32();
      reader.u8();
      reader.u8();
      reader.u32();
      reader.u8();
      reader.u32();
      reader.u8();
      reader.u8();
      PlayerId = reader.u64();
      reader.u16();
      Name = reader.str();
      reader.u8();
      reader.u32();
      reader.u8();
      reader.array(
        reader.u16(),
        () => {
          reader.u64();
          reader.ReadNBytesInt64();
          reader.u8();
          reader.u16();
          reader.ReadNBytesInt64();
          reader.u8();
          reader.u8();
        },
        5);
      reader.u8();
      if (reader.bl()) reader.u32();
      reader.u32();
      reader.u8();
      reader.u64();
      reader.u64();
      reader.str(7);
      reader.u8();
      reader.u8();
      reader.u32();
      reader.bytes(reader.u16(), 3, 17);
      reader.u64();
      reader.u8();
      reader.u8();
      reader.u32();
      GearLevel = reader.u32();
      reader.u8();
      reader.bytes(reader.u16(), 57);
      reader.bytes(35);
      reader.u8();
      reader.u8();
      reader.u32();
    }

    public const ushort OpCode = 44217;

    public void Validate() {
      Check(PlayerId <= (ulong.MaxValue >> 16), PlayerId);
      Check(ItemLevel >= 0 && ItemLevel <= 2000, ItemLevel);
      Check(Enum.IsDefined(Class), Class);
      Check(Level > 0 && Level <= 60, Level);
    }

    public Class Class => (Class)ClassId;

    public float ItemLevel {
      get {
        uint res = GearLevel;
        return Unsafe.As<uint, float>(ref res);
      }
    }

    public R Visit<R, A>(IMessageVisitor<R, A> v, A arg) => v.Visit(this, arg);

    public override string ToString() => new Printer()
        .Field(PlayerId)
        .Field(Name)
        .Field(Class)
        .Field(Level)
        .Field((int)ItemLevel)
        .Finish();

    public ushort Level { get; }
    public uint GearLevel { get; }
    public ushort ClassId { get; }
    public string Name { get; }
    public ulong PlayerId { get; }
  }
}
