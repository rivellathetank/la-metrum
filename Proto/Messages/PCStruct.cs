using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaMetrum {
  class PCStruct {
    public PCStruct(FieldReader r) {
      TsReader reader = new(r);
      reader.str(20);
      reader.u16();
      reader.u8();
      reader.u32();
      reader.u32();
      if (reader.bl()) reader.bytes(12);
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
      reader.array(
        reader.u16(),
        () => {
          reader.u16();
          reader.u32();
          reader.u16();
          reader.LostArkDateTime();
          if (reader.bl()) reader.u8();
          reader.bytes(reader.u16(), 3, 14);
        },
        30);
      reader.u8();
      reader.u16();
      GearLevel = reader.u32();
      reader.array(
        reader.u16(),
        () => {
          reader.bytes(reader.u16(), 5, 4);
          reader.u32();
        },
        200);
      reader.u8();
      reader.u8();
      reader.u8();
      reader.u32();
      reader.u8();
      Level = reader.u16();
      reader.bytes(reader.u32(), 512);
      reader.u8();
      reader.u8();
      reader.u32();
      reader.Angle();
      reader.u64();
      reader.u8();
      reader.u64();
      reader.array(
        reader.u16(),
        () => {
          reader.u16();
          reader.u32();
          reader.u16();
          reader.LostArkDateTime();
          if (reader.bl()) reader.u8();
          reader.bytes(reader.u16(), 3, 14);
        },
        9);
      reader.u32();
      reader.u8();
      reader.bytes(reader.u16(), 200, 4);
      ClassId = reader.u16();
      reader.u32();
      reader.bytes(5);
      Name = reader.str();
      reader.u32();
      reader.u32();
      reader.array(
        reader.u16(),
        () => {
          reader.ReadNBytesInt64();
          reader.u8();
        },
        152);
      reader.bytes(25);
      reader.u8();
      reader.u16();
      reader.u32();
      reader.array(reader.u16(), () => new StatusEffectData(r), 80);
      PlayerId = reader.u64();
    }

    public string Name { get; }
    public ushort Level { get; }
    public ulong PlayerId { get; }
    public uint GearLevel { get; }
    public ushort ClassId { get; }
  }
}
