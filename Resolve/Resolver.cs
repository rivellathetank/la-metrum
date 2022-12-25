using NLog;

namespace LaMetrum.Resolve {
  class Resolver : IMessageVisitor<IEnumerable<IEvent>, byte> {
    class You {
      public ulong Id { get; set; }
      public string Name { get; set; }
      public Class? Class { get; set; }
      public float? ItemLevel { get; set; }

      public IEvent MakeEvent() => new NewPlayer(Name, Class.Value, ItemLevel, You: true);
      public Entity MakeEntity() => new Entity(Id, Name);
    }

    static readonly TimeSpan SummonTTL = TimeSpan.FromSeconds(30);
    static readonly TimeSpan ProjectileTTL = TimeSpan.FromSeconds(10);

    static readonly Logger _log = LogManager.GetCurrentClassLogger();

    You _you = null;
    readonly ExpiryQueue _expiry = new();
    readonly Dictionary<ulong, Entity> _entities = new();
    readonly Dictionary<string, Entity> _players = new();

    public IEnumerable<IEvent> Resolve(IMessage msg) => msg.Visit<IEnumerable<IEvent>, byte>(this, 0);

    IEnumerable<IEvent> IMessageVisitor<IEnumerable<IEvent>, byte>.Visit(PKTInitPC msg, byte _) {
      if (_players.Count > 0) {
        _expiry.Clear();
        _entities.Clear();
        _players.Clear();
        yield return new NewZone();
      }
      _you = new You() {
        Id = msg.PlayerId,
        Name = msg.Name,
        Class = msg.Class,
        ItemLevel = msg.ItemLevel,
      };
      yield return AddYou(out Entity _);
    }

    IEnumerable<IEvent> IMessageVisitor<IEnumerable<IEvent>, byte>.Visit(PKTInitEnv msg, byte _) {
      _expiry.Clear();
      _entities.Clear();
      _players.Clear();

      yield return new NewZone();

      if (msg.SamePlayer == 0) {
        _you = null;
      } else if (_you is null) {
        _you = new You() {
          Id = msg.PlayerId,
          Name = "YOU",
          Class = null,
          ItemLevel = null,
        };
      } else {
        _you.Id = msg.PlayerId;
        if (_you.Class.HasValue) yield return AddYou(out Entity _);
      }
    }

    IEnumerable<IEvent> IMessageVisitor<IEnumerable<IEvent>, byte>.Visit(PKTNewPC msg, byte _) {
      yield return NewPc(
          msg.PlayerId,
          msg.Name,
          msg.Class,
          msg.ItemLevel,
          out Entity _);
    }

    IEnumerable<IEvent> IMessageVisitor<IEnumerable<IEvent>, byte>.Visit(PKTSkillStartNotify msg, byte _) {
      Skill.Db.TryGetValue(msg.SkillId, out Skill skill);

      if (!_entities.TryGetValue(msg.SourceId, out Entity src) || !GetAgent(ref src)) {
        if (skill?.Class is null) yield break;
        ulong id = src is not null ? src.OwnerId.Value : msg.SourceId;
        if (id == _you?.Id && _you.Class is null) {
          _you.Class = skill.Class;
          yield return AddYou(out src);
        } else {
          yield return NewPc(id, "$" + id, skill.Class.Value, ilvl: null, out src);
        }
      } else if (!src.IsPlayer) {
        yield break;
      }

      yield return new PlayerSkillStart(
          PlayerName: src.Name,
          SkillName: skill.Name ?? msg.SkillId.ToString(),
          SkillBuild: new SkillBuild(
              SkillLevel: msg.SkillLevel,
              Tripods: Pack(msg.SkillLevel, msg.Tripods),
              TripodLevels: Pack(msg.SkillLevel, msg.TripodLevels)));

      static uint Pack(int lvl, byte[] x) {
        uint res = x is null ? 0U : x[0] | (uint)x[1] << 8 | (uint)x[2] << 16;
        res &= lvl switch {
          (< 4) => 0x000000,
          (< 7) => 0x0000FF,
          (< 10) => 0x00FFFF,
          _ => uint.MaxValue,
        };
        return res;
      }
    }

    IEnumerable<IEvent> IMessageVisitor<IEnumerable<IEvent>, byte>.Visit(PKTNewProjectile msg, byte _) {
      Check(!_entities.ContainsKey(msg.ProjectileId));
      if (_entities.ContainsKey(msg.OwnerId)) {
        Entity x = new(msg.ProjectileId, msg.OwnerId, ProjectileTTL);
        _entities.Add(x.Id, x);
        Refresh(x);
      }
      return Array.Empty<IEvent>();
    }

    IEnumerable<IEvent> IMessageVisitor<IEnumerable<IEvent>, byte>.Visit(PKTNewNpcSummon msg, byte _) {
      Check(!_entities.ContainsKey(msg.NpcId));
      if (_entities.ContainsKey(msg.OwnerId)) {
        Entity x = new(msg.NpcId, msg.OwnerId, SummonTTL);
        _entities.Add(x.Id, x);
        Refresh(x);
      }
      return Array.Empty<IEvent>();
    }

    IEnumerable<IEvent> IMessageVisitor<IEnumerable<IEvent>, byte>.Visit(
        PKTSkillDamageAbnormalMoveNotify msg, byte _) =>
        ProcessDamage(msg.SourceId, msg.SkillId, msg.SkillEffectId, msg.Targets);

    IEnumerable<IEvent> IMessageVisitor<IEnumerable<IEvent>, byte>.Visit(
        PKTSkillDamageNotify msg, byte _) =>
        ProcessDamage(msg.SourceId, msg.SkillId, msg.SkillEffectId, msg.Targets);

    IEnumerable<IEvent> IMessageVisitor<IEnumerable<IEvent>, byte>.Visit(PKTDeathNotify msg, byte _) {
      string targetName = null;
      if (_entities.TryGetValue(msg.TargetId, out Entity target)) {
        if (target.IsPlayer) {
          targetName = target.Name;
        } else {
          Check(_entities.Remove(msg.TargetId));
        }
      }

      if (msg.SourceId == msg.TargetId) yield break;

      string srcName = null;
      if (_entities.TryGetValue(msg.SourceId, out Entity src) && GetAgent(ref src) && src.IsPlayer) {
        srcName = src.Name;
      } else if (targetName is null) {
        yield break;
      }

      yield return new PlayerKill(SourcePlayerName: srcName, TargetPlayerName: targetName);
    }

    IEnumerable<IEvent> IMessageVisitor<IEnumerable<IEvent>, byte>.Visit(PKTRaidResult x, byte _) {
      yield return new ZoneEvent(ZoneEventType.GuardianRaidEnd);
    }

    IEnumerable<IEvent> IMessageVisitor<IEnumerable<IEvent>, byte>.Visit(PKTTriggerBossBattleStatus x, byte _) {
      yield return new ZoneEvent(ZoneEventType.SubZoneEnter);
    }

    IEnumerable<IEvent> IMessageVisitor<IEnumerable<IEvent>, byte>.Visit(PKTTriggerStartNotify x, byte _) {
      ZoneEventType? t = x.TriggerSignalType switch {
        TriggerSignalType.COOP_QUEST_START => ZoneEventType.CoopStart,
        TriggerSignalType.COOP_QUEST_COMPLETE => ZoneEventType.CoopSuccess,
        TriggerSignalType.COOP_QUEST_FAIL => ZoneEventType.CoopFail,
        TriggerSignalType.COOP_QUEST_CANCEL => ZoneEventType.CoopCancel,
        TriggerSignalType.DUNGEON_PHASE1_CLEAR => ZoneEventType.Gate1Success,
        TriggerSignalType.DUNGEON_PHASE2_CLEAR => ZoneEventType.Gate2Success,
        TriggerSignalType.DUNGEON_PHASE3_CLEAR => ZoneEventType.Gate3Success,
        TriggerSignalType.DUNGEON_PHASE4_CLEAR => ZoneEventType.Gate4Success,
        TriggerSignalType.DUNGEON_PHASE5_CLEAR => ZoneEventType.Gate5Success,
        TriggerSignalType.DUNGEON_PHASE1_FAIL => ZoneEventType.GateFail1,
        TriggerSignalType.DUNGEON_PHASE2_FAIL => ZoneEventType.GateFail2,
        TriggerSignalType.DUNGEON_PHASE3_FAIL => ZoneEventType.GateFail3,
        TriggerSignalType.DUNGEON_PHASE4_FAIL => ZoneEventType.GateFail4,
        TriggerSignalType.DUNGEON_PHASE5_FAIL => ZoneEventType.GateFail5,
        _ => null,
      };
      if (t is not null) yield return new ZoneEvent(t.Value);
    }

    bool GetAgent(ref Entity x) {
      Refresh(x);
      for (int i = 0; i != 3; ++i) {
        if (!x.OwnerId.HasValue) return true;
        if (!_entities.TryGetValue(x.OwnerId.Value, out Entity owner)) break;
        x = owner;
        Refresh(x);
      }
      return false;
    }

    void Refresh(Entity x) {
      if (x.IsPlayer) return;
      x.Refresh();
      _expiry.Push(x);
    }

    void RemoveExpired() {
      DateTime now = DateTime.UtcNow;
      while (_expiry.TryPop(now, out Entity expired)) {
        if (_entities.TryGetValue(expired.Id, out Entity x) && ReferenceEquals(expired, x)) {
          _log.Debug("Expired: {0}", expired);
          Check(!expired.IsPlayer);
          Check(_entities.Remove(expired.Id));
        }
      }
    }

    IEnumerable<IEvent> ProcessDamage(ulong srcId, uint skillId, uint effectId, IEnumerable<SkillDamageEvent> msg) {
      if (!_entities.TryGetValue(srcId, out Entity src) || !GetAgent(ref src) || !src.IsPlayer) yield break;

      GetSkillName(skillId, effectId, out string skillName, out string skillEffect);

      foreach (SkillDamageEvent e in msg) {
        if (_entities.TryGetValue(e.TargetId, out Entity target)) Refresh(target);
        if (e.HitFlag == HitFlag.DAMAGE_SHARE && skillId == 0 && effectId == 0) continue;

        yield return new PlayerHit(
            SourcePlayerName: src.Name,
            TargetPlayerName: target.IsPlayer ? target.Name : null,
            HitSource: new HitSource(SkillName: skillName, EffectName: skillEffect),
            FullDamage: e.Damage,
            OverkillDamage: Math.Max(0, -e.CurHp),
            Crit: e.HitFlag == HitFlag.CRITICAL || e.HitFlag == HitFlag.DOT_CRITICAL,
            Type: e.HitOption switch {
              HitOption.BACK_ATTACK => HitType.BackAttack,
              HitOption.FRONTAL_ATTACK => HitType.FrontAttack,
              _ => HitType.Regular,
            });
      }
    }

    IEvent NewPc(ulong id, string name, Class c, float? ilvl, out Entity entity) {
      if (_you is not null) {
        Check(id != _you.Id);
        Check(name != _you.Name);
      }

      Check(!_entities.ContainsKey(id));
      Check(!_players.ContainsKey(name));
      entity = new Entity(id, name);
      _entities.Add(id, entity);
      _players.Add(name, entity);
      return new NewPlayer(Name: name, Class: c, ItemLevel: ilvl, You: false);
    }

    IEvent AddYou(out Entity entity) {
      Check(_you is not null);
      Check(_you.Class is not null);
      entity = _you.MakeEntity();
      _players.Add(entity.Name, entity);
      _entities.Add(entity.Id, entity);
      return _you.MakeEvent();
    }

    static void GetSkillName(uint id, uint effectId, out string name, out string effect) {
      uint origId = id;
      if (id == 0) {
        if (SkillEffect.Db.TryGetValue(effectId, out string s)) {
          int sep = s.IndexOf('/');
          if (sep < 0) {
            name = string.Empty;
            effect = s;
          } else {
            name = s[..sep];
            effect = s[(sep + 1)..];
          }
          return;
        }
        id = effectId / 10;
      }
      if (Skill.Db.TryGetValue(id, out Skill skill) && skill.Name is not null) {
        name = skill.Name;
        effect = string.Empty;
      } else {
        name = origId.ToString();
        effect = effectId.ToString();
      }
    }
  }
}
