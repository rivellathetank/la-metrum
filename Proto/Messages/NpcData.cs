using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaMetrum {
  class NpcData {
    public NpcData(FieldReader r) {
      TsReader reader = new(r);
      if (reader.bl()) {
        reader.u16();
        reader.u8();
        reader.u64();
        reader.bytes(reader.u32());
        reader.str();
        for (uint i = reader.u16(); i > 0; --i) {
          reader.u16();
          reader.u32();
          reader.u16();
          reader.LostArkDateTime();
          if (reader.bl()) reader.u8();
          reader.bytes(reader.u16() * 14);
        }
        reader.u8();
        reader.u8();
      }
      if (reader.bl()) reader.u32();
      reader.u8();
      if (reader.bl()) reader.bytes(reader.u16(), 11, 9);
      ObjectId = reader.u64();
      reader.i32();
      reader.array(reader.u16(), () => new StatusEffectData(r), 80);
      reader.u8();
      reader.Vector3F();
      if (reader.bl()) reader.u32();
      reader.u8();
      if (reader.bl()) reader.u32();
      if (reader.bl()) reader.bytes(reader.u16(), 12, 12);
      reader.u8();
      reader.u16();
      reader.u32();
      if (reader.bl()) reader.u16();
      if (reader.bl()) reader.u32();
      reader.Angle();
      if (reader.bl()) reader.u8();
      if (reader.bl()) reader.u8();
      if (reader.bl()) reader.u8();
      reader.u8();
      if (reader.bl()) reader.u8();
      reader.array(
        reader.u16(),
        () => {
          reader.ReadNBytesInt64();
          reader.u8();
        },
        152);
      if (reader.bl()) reader.u64();
      if (reader.bl()) reader.u16();
      if (reader.bl()) reader.u8();
      reader.u8();
      if (reader.bl()) reader.u32();
      if (reader.bl()) reader.u8();
      reader.array(
        reader.u16(), () => {
          reader.u64();
          reader.ReadNBytesInt64();
          reader.u8();
          reader.u16();
          reader.ReadNBytesInt64();
          reader.u8();
          reader.u8();
        },
        5);
      if (reader.bl()) reader.u8();
    }

    public ulong ObjectId { get; }
  }
}
