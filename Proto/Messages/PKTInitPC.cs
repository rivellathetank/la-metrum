using System.Runtime.CompilerServices;

namespace LaMetrum {
  class PKTInitPC : IMessage {
    public PKTInitPC(FieldReader r) {
      TsReader reader = new(r);
      reader.array<ushort>();
      if (reader.bl()) reader.u32();
      reader.u32();
      reader.array(
        reader.u16(),
        () => {
          reader.u8();
          Unk.read13(reader);
        },
        152);
      reader.u8();
      reader.u32();
      reader.u8();
      GearLevel = reader.u32();
      reader.u8();
      reader.u32();
      reader.u32();
      reader.u64();
      reader.u8();
      reader.u32();
      reader.array(reader.u16(), () => Unk.read14(reader), 80);
      reader.u8();
      reader.u8();
      reader.u8();
      PlayerId = reader.u64();
      reader.u64();
      reader.u8();
      reader.u8();
      reader.u8();
      reader.u16();
      reader.u32();
      reader.u8();
      reader.u8();
      reader.u16();
      reader.bytes(112);
      reader.array(reader.u16(), () => Unk.read15(reader), 5);
      reader.u16();
      Level = reader.u16();
      reader.u32();
      reader.u64();
      reader.u32();
      reader.u8();
      reader.u32();
      reader.bytes(25);
      reader.bytes(35);
      reader.u8();
      reader.u8();
      reader.u32();
      reader.u8();
      reader.u8();
      reader.u8();
      ClassId = reader.u16();
      reader.u8();
      Name = reader.str(20);
      reader.u64();
      reader.u32();
      reader.u64();
      reader.bytes(reader.u16(), 3, 17);
      reader.u8();
      reader.bytes(reader.u16(), 57);
      reader.u32();
      reader.u16();
      reader.bytes(reader.u16(), 104, 30);
    }

    public const ushort OpCode = 399;

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
