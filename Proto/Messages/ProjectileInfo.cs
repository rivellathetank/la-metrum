using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaMetrum {
  class ProjectileInfo {
    public ProjectileInfo(FieldReader r) {
      TsReader reader = new(r);
      SkillEffect = reader.u32();
      ProjectileId = reader.u64();
      if (reader.bl()) reader.u64();
      tripodIndex = reader.bytes(3);
      reader.u32();
      reader.u32();
      reader.u8();
      SkillLevel = reader.u8();
      reader.u64();
      reader.u8();
      OwnerId = reader.u64();
      reader.u64();
      reader.u64();
      Unk.read30(reader);
      reader.u16();
      if (reader.bl()) reader.u32();
      if (reader.bl()) reader.bytes(reader.u16(), 11, 9);
      reader.u32();
      SkillId = reader.u32();
      reader.u16();
      reader.u32();
    }

    public ulong ProjectileId { get; }
    public byte SkillLevel { get; }
    public byte[] tripodIndex { get; }
    public uint SkillId { get; }
    public ulong OwnerId { get; }
    public uint SkillEffect { get; }
  }
}
