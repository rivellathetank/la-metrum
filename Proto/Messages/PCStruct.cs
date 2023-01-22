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
      reader.u64();
      reader.bytes(reader.u32(), 512);
      reader.u32();
      reader.u8();
      Level = reader.u16();
      reader.u32();
      reader.u8();
      reader.array(reader.u16(), () => Unk.read19(reader), 30);
      reader.array(reader.u16(), () => Unk.read14(reader), 80);
      PlayerId = reader.u64();
      reader.str(20);
      reader.bytes(25);
      reader.u32();
      reader.array(
        reader.u16(),
        () => {
          reader.array(reader.u16(), () => reader.u32(), 5);
          reader.u32();
        },
        200);
      ClassId = reader.u16();
      reader.u32();
      reader.u16();
      if (reader.bl()) reader.bytes(12);
      reader.u8();
      reader.u8();
      reader.u8();
      reader.u32();
      reader.array(reader.u16(), () => Unk.read19(reader), 9);
      Unk.read22(reader);
      reader.array(
        reader.u16(),
        () => {
          reader.u8();
          Unk.read13(reader);
        },
        152);
      reader.u16();
      reader.u8();
      reader.array(reader.u16(), () => Unk.read15(reader), 5);
      GearLevel = reader.u32();
      reader.u32();
      reader.u8();
      reader.u16();
      reader.u8();
      reader.u8();
      Name = reader.str(20);
      reader.u32();
      reader.u8();
      reader.u64();
      reader.u32();
      reader.bytes(5);
      reader.bytes(reader.u16(), 200, 4);
    }

    public string Name { get; }
    public ushort Level { get; }
    public ulong PlayerId { get; }
    public uint GearLevel { get; }
    public ushort ClassId { get; }
  }
}
