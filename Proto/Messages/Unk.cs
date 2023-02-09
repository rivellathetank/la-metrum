using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaMetrum {
  static class Unk {
    public static void read8(TsReader reader) {
      reader.u64();
      reader.u64();
      reader.skip(1);
      reader.u32();
    }
    public static void read11(TsReader reader) {
      reader.LostArkDateTime();
    }
    public static void read13(TsReader reader) {
      reader.ReadNBytesInt64();
    }
    public static void read14(TsReader reader) {
      reader.u8();
      reader.u16();
      Unk.read13(reader);
      reader.u64();
      reader.u8();
      reader.u8();
      Unk.read13(reader);
    }
    public static void read15(TsReader reader) {
      reader.u32();
      reader.u32();
      reader.u64();
      reader.u64();
      reader.u32();
      reader.u8();
      if (reader.bl()) reader.u64();
      if (reader.bl()) reader.bytes(16);
      reader.bytes(reader.u16(), 8, 7);
      reader.u8();
      Unk.read11(reader);
    }
    public static void read19(TsReader reader) {
      reader.u16();
      Unk.read11(reader);
      reader.bytes(reader.u16(), 3, 14);
      reader.u32();
      reader.u16();
      if (reader.bl()) reader.u8();
    }
    public static void read20(TsReader reader) {
      reader.u16();
      reader.array(reader.u16(), () => Unk.read19(reader), 30);
      reader.u64();
      reader.bytes(reader.u32(), 512);
      reader.u8();
      reader.skip(2 * reader.u16());
      reader.u8();
      reader.u8();
    }
    public static void read21(TsReader reader) {
      reader.Angle();
    }
    public static void read22(TsReader reader) {
      reader.Vector3F();
    }
    public static void read27(TsReader reader) {
      reader.u32();
      reader.u32();
      reader.bytes(12);
      if (reader.bl()) reader.bytes(12);
    }
    public static void read30(TsReader reader) {
      reader.u16();
      reader.u16();
      reader.u16();
    }
    public static void read49(TsReader reader) {
      reader.ReadFlagBytes2();
    }
  }
}
