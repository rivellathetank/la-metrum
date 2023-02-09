using System.Runtime.CompilerServices;

namespace LaMetrum {
  class PKTNewPC : IMessage {
    public PKTNewPC(FieldReader r) {
      TsReader reader = new(r);
      reader.u8();
      if (reader.bl()) reader.bytes(12);
      if (reader.bl()) reader.bytes(20);
      PCStruct = new(r);
      reader.u8();
      if (reader.bl()) reader.u32();
      if (reader.bl()) Unk.read27(reader);
    }

    public const ushort OpCode = 8067;

    public void Validate() {
      Check(ItemLevel >= 0 && ItemLevel <= 2000, ItemLevel);
      Check(PlayerId <= (ulong.MaxValue >> 16), PlayerId);
      Check(Enum.IsDefined(Class), Class);
    }

    public ulong PlayerId => PCStruct.PlayerId;
    public string Name => PCStruct.Name;
    public Class Class => (Class)PCStruct.ClassId;
    public float ItemLevel {
      get {
        uint res = PCStruct.GearLevel;
        return Unsafe.As<uint, float>(ref res);
      }
    }

    public R Visit<R, A>(IMessageVisitor<R, A> v, A arg) => v.Visit(this, arg);

    public override string ToString() => new Printer()
        .Field(PlayerId)
        .Field(Name)
        .Field(Class)
        .Field(ItemLevel)
        .Finish();

    PCStruct PCStruct { get; }
  }
}
