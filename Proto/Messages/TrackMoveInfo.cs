using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaMetrum {
  class TrackMoveInfo {
    public TrackMoveInfo(FieldReader r) {
      TsReader reader = new(r);
      reader.u32();
      reader.u32();
      reader.bytes(12);
      if (reader.bl()) reader.bytes(12);
    }
  }
}
