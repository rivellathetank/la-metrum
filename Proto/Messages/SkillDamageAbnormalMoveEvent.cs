using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaMetrum {
  class SkillDamageAbnormalMoveEvent {
    public SkillDamageAbnormalMoveEvent(FieldReader r) {
      TsReader reader = new(r);
      reader.ReadFlagBytes2();
      reader.u16();
      reader.u64();
      reader.Vector3F();
      reader.u16();
      skillDamageEvent = new SkillDamageEvent(r);
      reader.Vector3F();
      reader.u16();
      reader.u8();
    }

    public SkillDamageEvent skillDamageEvent { get; }
  }
}
