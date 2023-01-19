using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaMetrum {
  class PCStruct {
    public PCStruct(FieldReader r) {
      TsReader reader = new(r);
      reader.u8();
      reader.u8();
      reader.u32();
      if (reader.bl()) reader.bytes(12);
      reader.u16();
      reader.array(reader.u16(), () => Unk.read19(reader), 30);
      reader.bytes(25);
      reader.array(
        reader.u16(),
        () => {
          reader.u8();
          Unk.read14(reader);
        },
        152);
      Name = reader.str(20);
      reader.u64();
      reader.u16();
      reader.bytes(5);
      reader.bytes(reader.u16(), 200, 4);
      Unk.read22(reader);
      reader.u8();
      reader.u32();
      reader.u8();
      reader.array(reader.u16(), () => Unk.read19(reader), 9);
      ClassId = reader.u16();
      reader.u32();
      reader.str(20);
      reader.u32();
      reader.u8();
      PlayerId = reader.u64();
      reader.u32();
      reader.u8();
      reader.array(
        reader.u16(),
        () => {
          reader.array(reader.u16(), () => reader.u32(), 5);
          reader.u32();
        },
        200);
      reader.u32();
      reader.u8();
      reader.array(reader.u16(), () => Unk.read13(reader), 80);
      reader.u8();
      reader.u32();
      reader.u8();
      GearLevel = reader.u32();
      reader.u64();
      reader.u8();
      reader.u16();
      reader.u8();
      reader.bytes(reader.u32(), 512);
      reader.array(reader.u16(), () => Unk.read15(reader), 5);
      Level = reader.u16();
      reader.u32();
      reader.u8();
      reader.u32();
    }

    public string Name { get; }
    public ushort Level { get; }
    public ulong PlayerId { get; }
    public uint GearLevel { get; }
    public ushort ClassId { get; }
  }
}
