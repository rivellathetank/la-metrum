﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaMetrum {
  class ProjectileInfo {
    public ProjectileInfo(FieldReader r) {
      TsReader reader = new(r);
      reader.u8();
      if (reader.bl()) reader.u32();
      reader.u64();
      tripodIndex = reader.bytes(3);
      reader.u16();
      SkillId = reader.u32();
      OwnerId = reader.u64();
      reader.u32();
      reader.u32();
      reader.u64();
      if (reader.bl()) reader.bytes(reader.u16(), 11, 9);
      if (reader.bl()) reader.u64();
      SkillLevel = reader.u8();
      reader.u32();
      reader.u16();
      reader.u32();
      SkillEffect = reader.u32();
      reader.u64();
      Unk.read30(reader);
      ProjectileId = reader.u64();
      reader.u8();
    }

    public ulong ProjectileId { get; }
    public byte SkillLevel { get; }
    public byte[] tripodIndex { get; }
    public uint SkillId { get; }
    public ulong OwnerId { get; }
    public uint SkillEffect { get; }
  }
}
