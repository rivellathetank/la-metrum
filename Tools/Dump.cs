using System.IO.Compression;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace LaMetrum {
  static class Dump {
    public static void DumpAll(string srcDir, string dstDir) {
      Check(Directory.Exists(srcDir), srcDir);
      Check(Directory.Exists(dstDir), dstDir);

      Dictionary<string, string> gameMsg = Read<string, string>(srcDir, "GameMsg.gz");
      Dictionary<int, string[]> skill = Read<int, string[]>(srcDir, "Skill.gz");

      DumpSkills();
      DumpSkillEffects();

      void DumpSkills() {
        using TextWriter w = new StreamWriter(
            Path.Combine(dstDir, "Skill.cs"),
            append: false,
            encoding: new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        string[] prefixes = { "Yeon-Style Spear Technique: ", "Esoteric Skill: ", "Ultimate Skill: " };
        Dictionary<uint, Skill> db = new() {
          [16000] = new Skill(Class.Berserker, "Basic Attack"),
          [21000] = new Skill(Class.Bard, "Basic Attack"),
          [21222] = new Skill(Class.Bard, "Harp of Rhythm"),
          [25000] = new Skill(Class.Deathblade, "Basic Attack"),
          [25035] = new Skill(Class.Deathblade, "Surge"),
          [25036] = new Skill(Class.Deathblade, "Surge"),
          [25037] = new Skill(Class.Deathblade, "Surge"),
          [25038] = new Skill(Class.Deathblade, "Surge"),
          [30000] = new Skill(Class.Artillerist, "Basic Attack"),
          [30105] = new Skill(Class.Artillerist, "Summon Turret"),
          [30115] = new Skill(Class.Artillerist, "Summon Turret"),
          [36000] = new Skill(Class.Paladin, "Basic Attack"),
          [37000] = new Skill(Class.Sorceress, "Basic Attack"),
          [38000] = new Skill(Class.Gunslinger, "Basic Attack"),
          [55600] = new Skill(null, "Absorb Finest Medeia"),
          [55610] = new Skill(null, "Arcturus's Light"),
        };

        foreach (var kv in skill) {
          uint id = unchecked((uint)kv.Key);
          if (db.ContainsKey(id)) continue;
          Class? c = (Class)uint.Parse(kv.Value[2]);
          if (!Enum.IsDefined(c.Value)) c = null;

          string name = null;
          if (kv.Value[0].Length > 0) {
            if (gameMsg.TryGetValue(kv.Value[0], out name) && name.Length == 0) name = null;
          }

          if (name is not null) {
            foreach (string p in prefixes) {
              if (name.StartsWith(p)) {
                name = name[p.Length..];
                break;
              }
            }
          } else if (!c.HasValue) {
            continue;
          }

          db.Add(id, new Skill(c, name));
        }

        w.WriteLine("using System.Collections.Generic;");
        w.WriteLine("");
        w.WriteLine("namespace LaMetrum {");
        w.WriteLine("  class Skill {");
        w.WriteLine("    public Skill(Class? c, string name) {");
        w.WriteLine("      Class = c;");
        w.WriteLine("      Name = name;");
        w.WriteLine("    }");
        w.WriteLine("");
        w.WriteLine("    public Class? Class { get; }");
        w.WriteLine("    public string Name { get; }");
        w.WriteLine("");
        w.WriteLine("    public static IReadOnlyDictionary<uint, Skill> Db { get; } = new Dictionary<uint, Skill>() {");
        foreach (var kv in db.OrderBy(kv => kv.Key)) {
          w.WriteLine("      [{0}] = new Skill({1}, {2}),",
                      kv.Key,
                      kv.Value.Class is null ? "null" : "LaMetrum.Class." + kv.Value.Class,
                      kv.Value.Name is null ? "null" : Strings.Quote(kv.Value.Name));
        }
        w.WriteLine("    };");
        w.WriteLine("  }");
        w.WriteLine("}");
      }

      void DumpSkillEffects() {
        using TextWriter w = new StreamWriter(
            Path.Combine(dstDir, "SkillEffect.cs"),
            append: false,
            encoding: new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        string[] dots = { "Bleed", "Burn", "Poison", "Disease" };
        Dictionary<uint, string> db = new() {
          [0] = "Bleed",
          [23026] = "Splendid Frost Grenade",  // needs testing
          [32001] = "Flash Grenade",
          [32006] = "Splendid Flash Grenade",
          [32011] = "Flame Grenade",
          [32015] = "Flame Grenade",
          [32018] = "Splendid Flame Grenade",
          [32019] = "Splendid Flame Grenade",
          [32021] = "Frost Grenade",
          [32031] = "Electric Grenade",
          [32033] = "Electric Grenade",
          [32036] = "Splendid Lightening Grenade",  // needs testing
          [32141] = "Destruction Bomb",
          [32143] = "Splendid Destruction Bomb",
          [32241] = "Dark Grenade",
          [32245] = "Splendid Dark Grenade",
          [32311] = "Whirlwind Grenade",
          [32314] = "Splendid Whirlwind Grenade",
          [32321] = "Clay Grenade",
          [32326] = "Splendid Clay Grenade",
          [370014] = "Explosion/Burn",
          [370015] = "Doomsday/Burn",
          [373262] = "Seraphic Hail/Burn",
          [600020110] = "Betrayal: Electrode",
          [600030111] = "Betrayal: Thunderstroke",
          [600050109] = "Betrayal: Meteor",
          [610020110] = "Betrayal: Electrode",
          [610030111] = "Betrayal: Thunderstroke",
          [610050109] = "Betrayal: Meteor",
          [620020110] = "Betrayal: Electrode",
          [620030111] = "Betrayal: Thunderstroke",
          [620050109] = "Betrayal: Meteor",
        };

        foreach (var kv in gameMsg) {
          if (!kv.Key.StartsWith("tip.name.skillbuff_")) continue;
          if (!dots.Contains(kv.Value)) continue;
          string[] parts = kv.Key.Split('_');
          if (parts.Length != 2) continue;
          uint id = uint.Parse(parts[1]);
          if (db.ContainsKey(id)) continue;
          db.Add(id, kv.Value);
        }

        w.WriteLine("using System.Collections.Generic;");
        w.WriteLine("");
        w.WriteLine("namespace LaMetrum {");
        w.WriteLine("  static class SkillEffect {");
        w.WriteLine("    public static IReadOnlyDictionary<uint, string> Db { get; } = new Dictionary<uint, string>() {");
        foreach ((uint id, string name) in db.OrderBy(kv => kv.Key)) {
          w.WriteLine("      [{0}] = {1},", id, Strings.Quote(name));
        }
        w.WriteLine("    };");
        w.WriteLine("  }");
        w.WriteLine("}");
      }
    }

    static Dictionary<K, V> Read<K, V>(string dir, string file) =>
      Deserialize<K, V>(Decompress(File.ReadAllBytes(Path.Combine(dir, file))));

    static Dictionary<K, V> Deserialize<K, V>(this byte[] data) {
      Dictionary<K, V> res;
      using (MemoryStream strm = new()) {
        strm.Write(data, 0, data.Length);
        strm.Seek(0, SeekOrigin.Begin);
        res = (Dictionary<K, V>)new BinaryFormatter().Deserialize(strm);
      }

      using (MemoryStream strm = new()) {
        new BinaryFormatter().Serialize(strm, Clone(res));
        byte[] copy = strm.ToArray();
        Check(Enumerable.SequenceEqual(data, copy));
      }
      return res;
    }

    static Dictionary<K, V> Clone<K, V>(Dictionary<K, V> x) {
      object buckets = x
          .GetType()
          .GetField("_buckets", BindingFlags.NonPublic | BindingFlags.Instance)
          .GetValue(x);
      Dictionary<K, V> res = new(((int[])buckets).Length);
      foreach (var kv in x) res.Add(kv.Key, kv.Value);
      return res;
    }

    static byte[] Decompress(byte[] input) {
      byte[] decompressedData;
      using (var outputStream = new MemoryStream()) {
        using (var inputStream = new MemoryStream(input))
        using (var zip = new GZipStream(inputStream, CompressionMode.Decompress))
          zip.CopyTo(outputStream);
        decompressedData = outputStream.ToArray();
      }
      return decompressedData;
    }
  }
}
