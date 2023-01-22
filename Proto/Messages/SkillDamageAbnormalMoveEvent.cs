using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaMetrum {
  class SkillDamageAbnormalMoveEvent {
    public SkillDamageAbnormalMoveEvent(FieldReader r) {
      TsReader reader = new(r);
      reader.u16();
      skillDamageEvent = new(r);
      Unk.read21(reader);
      reader.u16();
      reader.u8();
      Unk.read21(reader);
      Unk.read49(reader);
      reader.u16();
      reader.u64();
    }

    public SkillDamageEvent skillDamageEvent { get; }
  }
}
