﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace LaMetrum {
  class NpcData {
    public NpcData(FieldReader r) {
      TsReader reader = new(r);
      if (reader.bl()) reader.u32();
      if (reader.bl()) reader.u8();
      if (reader.bl()) reader.u8();
      if (reader.bl()) reader.bytes(reader.u16(), 11, 9);
      if (reader.bl()) reader.u8();
      reader.u8();
      if (reader.bl()) reader.u32();
      reader.u8();
      if (reader.bl()) reader.u16();
      if (reader.bl()) reader.u8();
      if (reader.bl()) reader.bytes(reader.u16(), 12, 12);
      reader.u16();
      reader.u8();
      if (reader.bl()) reader.u32();
      if (reader.bl()) reader.u16();
      reader.array(
        reader.u16(),
        () => {
          Unk.read13(reader);
          reader.u8();
        },
        152);
      if (reader.bl()) Unk.read20(reader);
      Unk.read21(reader);
      Unk.read22(reader);
      if (reader.bl()) reader.u64();
      if (reader.bl()) reader.u8();
      reader.u8();
      if (reader.bl()) reader.u32();
      reader.u32();
      if (reader.bl()) reader.u32();
      if (reader.bl()) reader.u8();
      reader.array(reader.u16(), () => Unk.read14(reader), 5);
      reader.i32();
      reader.u8();
      reader.u8();
      reader.array(reader.u16(), () => Unk.read15(reader), 80);
      if (reader.bl()) reader.u8();
      ObjectId = reader.u64();
    }

    public ulong ObjectId { get; }
  }
}
