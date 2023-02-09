using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaMetrum {
  class SkillDamageAbnormalMoveEvent {
    public SkillDamageAbnormalMoveEvent(FieldReader r) {
      TsReader reader = new(r);
      reader.u64();
      reader.u16();
      skillDamageEvent = new(r);
      Unk.read22(reader);
      reader.u8();
      Unk.read22(reader);
      reader.u16();
      Unk.read49(reader);
      reader.u16();
    }

    public SkillDamageEvent skillDamageEvent { get; }
  }
}
