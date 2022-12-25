using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaMetrum {
  class ProjectileInfo {
    public ProjectileInfo(FieldReader r) {
      TsReader reader = new(r);
      reader.u16();
      ProjectileId = reader.u64();
      reader.u32();
      reader.u8();
      if (reader.bl()) reader.u32();
      if (reader.bl()) reader.bytes(reader.u16(), 11, 9);
      reader.u32();
      reader.u32();
      SkillEffect = reader.u32();
      Tripods = reader.bytes(3);
      reader.u64();
      reader.u32();
      SkillLevel = reader.u8();
      reader.bytes(6);
      reader.u64();
      reader.u8();
      reader.u16();
      if (reader.bl()) reader.u64();
      reader.u64();
      SkillId = reader.u32();
      OwnerId = reader.u64();
    }

    public ulong ProjectileId { get; }
    public byte SkillLevel { get; }
    public byte[] Tripods { get; }
    public uint SkillId { get; }
    public ulong OwnerId { get; }
    public uint SkillEffect { get; }
  }
}
