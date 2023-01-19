using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaMetrum {
  class SkillDamageAbnormalMoveEvent {
    public SkillDamageAbnormalMoveEvent(FieldReader r) {
      TsReader reader = new(r);
      reader.u8();
      reader.u64();
      Unk.read47(reader);
      reader.u16();
      reader.u16();
      Unk.read21(reader);
      reader.u16();
      Unk.read21(reader);
      skillDamageEvent = new(r);
    }

    public SkillDamageEvent skillDamageEvent { get; }
  }
}
