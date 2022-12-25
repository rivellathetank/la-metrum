using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaMetrum {
  class StatusEffectData {
    public StatusEffectData(FieldReader r) {
      TsReader reader = new(r);
      reader.u64();
      reader.u32();
      reader.u8();
      reader.LostArkDateTime();
      reader.u8();
      reader.u32();
      reader.bytes(reader.u16(), 8, 7);
      reader.u64();
      if (reader.bl()) reader.bytes(16);
      if (reader.bl()) reader.u64();
      reader.u32();
    }
  }
}
