using System.Runtime.CompilerServices;

namespace LaMetrum {
  class PKTInitPC : IMessage {
    public PKTInitPC(FieldReader r) {
      TsReader reader = new(r);
      reader.array(reader.u16(), () => Unk.read14(reader), 5);
      reader.bytes(25);
      reader.u8();
      reader.u8();
      reader.u32();
      reader.bytes(reader.u16(), 57);
      reader.u16();
      reader.u16();
      reader.u32();
      reader.u8();
      reader.u8();
      reader.array(
        reader.u16(),
        () => {
          Unk.read13(reader);
          reader.u8();
        },
        152);
      reader.u8();
      reader.u8();
      reader.u32();
      reader.u32();
      reader.u64();
      reader.u64();
      reader.bytes(35);
      reader.u32();
      reader.u8();
      reader.u8();
      reader.u16();
      reader.u8();
      ClassId = reader.u16();
      reader.u8();
      reader.u8();
      reader.u8();
      reader.u32();
      reader.skip(2 * reader.u16());
      reader.u32();
      reader.u64();
      reader.u32();
      reader.u8();
      GearLevel = reader.u32();
      reader.u8();
      reader.bytes(116);
      PlayerId = reader.u64();
      reader.u32();
      reader.u32();
      reader.array(reader.u16(), () => Unk.read15(reader), 80);
      reader.u64();
      reader.u8();
      reader.u32();
      reader.u32();
      reader.u8();
      reader.u8();
      Level = reader.u16();
      reader.u8();
      if (reader.bl()) reader.u32();
      reader.u8();
      reader.u16();
      Name = reader.str(20);
      reader.bytes(reader.u16(), 104, 30);
      reader.u64();
      reader.bytes(reader.u16(), 3, 17);
      reader.u8();
    }

    public const ushort OpCode = 20790;

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
