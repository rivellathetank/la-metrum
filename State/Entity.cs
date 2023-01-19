using NLog;

namespace LaMetrum {
  enum EntityType {
    Player,
    Projectile,
    Summon,
  }

  class HitStats {
    public long NumHits { get; set; }
    public long NumCrits { get; set; }
    public long TotalDamage { get; set; }
    public long CritDamage { get; set; }

    public double CritRate => 1.0 * NumCrits / Math.Max(1, NumHits);

    public HitStats Clone() => (HitStats)MemberwiseClone();

    public override string ToString() => new Printer()
        .Field(NumHits)
        .Field(NumCrits)
        .Field(TotalDamage)
        .Field(CritDamage)
        .Finish();
  }

  class DamageStats {
    // Invariant: Total is not null.
    // Invariant: Total.NumHits     == Regular.NumHits     + BackAttack.NumHits     + FrontAttack.NumHits.
    // Invariant: Total.NumCrits    == Regular.NumCrits    + BackAttack.NumCrits    + FrontAttack.NumCrits.
    // Invariant: Total.TotalDamage == Regular.TotalDamage + BackAttack.TotalDamage + FrontAttack.TotalDamage.
    // Invariant: Total.CritDamage  == Regular.CritDamage  + BackAttack.CritDamage  + FrontAttack.CritDamage.
    public HitStats Total { get; set; } = new();
    // Invariant: Regular is not null.
    public HitStats Regular { get; set; } = new();
    // Invariant: BackAttack is not null.
    public HitStats BackAttack { get; set; } = new();
    // Invariant: FrontAttack is not null.
    public HitStats FrontAttack { get; set; } = new();

    public double PosRate => 1.0 * (BackAttack.NumHits + FrontAttack.NumHits) / Math.Max(1, Total.NumHits);

    public DamageStats Clone() {
      DamageStats res = (DamageStats)MemberwiseClone();
      res.Total = res.Total?.Clone();
      res.Regular = res.Regular?.Clone();
      res.BackAttack = res.BackAttack?.Clone();
      res.FrontAttack = res.FrontAttack?.Clone();
      return res;
    }

    public HitStats Get(HitOption opt) => opt switch {
      HitOption.BACK_ATTACK => BackAttack,
      HitOption.FRONTAL_ATTACK => FrontAttack,
      _ => Regular,
    };
  }

  class SkillBuild_Old {
    // Invariant: Level >= 1 && Level <= 12.
    public byte Level { get; set; }
    // Invariant: Tripods is not null && Tripods.Length == 3 && Tripods.All(x => x <= 3).
    public byte[] Tripods { get; set; }
    // Invariant: TripodsLevels is not null && TripodsLevels.Length == 3 && Tripods.All(x => x >= 1 && x <= 5).
    public byte[] TripodsLevels { get; set; }

    public SkillBuild_Old Clone() {
      SkillBuild_Old res = (SkillBuild_Old)MemberwiseClone();
      Tripods = Tripods?.ToArray();
      TripodsLevels = TripodsLevels?.ToArray();
      return res;
    }
  }

  class SkillStats {
    // Can be null.
    public SkillBuild_Old Build { get; set; }

    // Invariant: Stats is not null.
    public DamageStats Stats { get; set; } = new();

    public SkillStats Clone() {
      SkillStats res = (SkillStats)MemberwiseClone();
      Build = Build?.Clone();
      Stats = Stats?.Clone();
      return res;
    }

    public override string ToString() => new Printer()
        .Field(Build)
        .Field(Stats)
        .Finish();
  }

  class Entity {
    public ulong Id { get; set; }
    public EntityType Type { get; set; }
    // Invariant: Name is null == (Type != Player).
    public string Name { get; set; }
    // Invariant: OwnerId.HasValue == (Type == Projectile || Type == Summon).
    public ulong? OwnerId { get; set; }
    // Invariant: !Class.HasValue || Type == Player.
    public Class? Class { get; set; }
    // Invariant: !ItemLevel.HasValue || Type == Player.
    public float? ItemLevel { get; set; }
    // Invariant: Deaths is null == (Type != Player).
    public List<DateTime> Deaths { get; set; }
    // Invariant: Deaths.HasValue == (Type == Player).
    public int? Kills { get; set; }

    // Invariant: ExpiresAt.HasValue == (Type != Player).
    public DateTime? ExpiresAt { get; set; }

    // Invariant: (SkillStats == null) == (Type != Player).
    public Dictionary<string, SkillStats> SkillStats { get; set; }
    // Invariant: (SkillStats == null) == (Type != Player).
    public DamageStats TotalStats { get; set; }

    public override string ToString() => new Printer()
        .Field(Id)
        .Field(Type)
        .Field(Name)
        .Field(OwnerId)
        .Field(Class)
        .Field(Deaths)
        .Field(Kills)
        .Field(ItemLevel)
        .Field(TotalStats)
        .Finish();

    public void CheckInvariants() {
      Check(Enum.IsDefined(Type));
      Check(OwnerId.HasValue == (Type == EntityType.Projectile || Type == EntityType.Summon));
      Check(!Class.HasValue || Type == EntityType.Player);
      Check(!ItemLevel.HasValue || Type == EntityType.Player);
      Check(ExpiresAt.HasValue == (Type != EntityType.Player));
      Check(SkillStats is null == (Type != EntityType.Player));
      Check(TotalStats is null == (Type != EntityType.Player));
      Check(Name is null == (Type != EntityType.Player));
      Check(Deaths is null == (Type != EntityType.Player));
      Check(Kills.HasValue == (Type == EntityType.Player));
    }

    public Entity Clone() {
      Entity res = (Entity)MemberwiseClone();
      res.TotalStats = res.TotalStats?.Clone();
      res.SkillStats = res.SkillStats?.ToDictionary(kv => kv.Key, kv => kv.Value.Clone());
      res.Deaths = res.Deaths?.ToList();
      return res;
    }
  }

  class ExpiryQueue {
    record Elem(DateTime ExpiresAt, Entity Entity);
    class ElemComparer : IComparer<Elem> {
      public int Compare(Elem x, Elem y) => y.ExpiresAt.CompareTo(x.ExpiresAt);
    }

    readonly MaxHeap<Elem> _queue = new(new ElemComparer());

    public bool TryPop(DateTime now, out Entity expired) {
      expired = null;
      while (true) {
        if (_queue.Count == 0) return false;
        Elem top = _queue.Top();
        if (top.ExpiresAt > now) return false;
        _queue.Pop();
        if (top.Entity.ExpiresAt <= now) {
          expired = top.Entity;
          return true;
        }
      }
    }

    public void Push(Entity x) {
      Check(x.ExpiresAt.HasValue);
      _queue.Push(new Elem(x.ExpiresAt.Value, x));
    }

    public void Clear() {
      _queue.Clear();
    }
  }

  class EntityTracker {
    static readonly TimeSpan SummonTtl = TimeSpan.FromSeconds(30);
    static readonly TimeSpan ProjectileTtl = TimeSpan.FromSeconds(10);

    static readonly Logger _log = LogManager.GetCurrentClassLogger();

    Entity _you = null;
    readonly ExpiryQueue _expiry = new();
    readonly Dictionary<ulong, Entity> _entities = new();
    readonly Dictionary<string, Entity> _players = new();
    readonly HashSet<ValueTuple<uint, uint>> _seenSkill = new();

    public Dictionary<string, Entity> Players => _players;

    public void ResetStats() {
      foreach (Entity x in _players.Values) {
        x.Kills = 0;
        x.TotalStats = new();
        x.SkillStats.Clear();
        x.Deaths.Clear();
      }
    }

    public void ProcessMessage(PKTInitPC msg) {
      RemoveExpired();
      InitYou();
      _you.Id = msg.PlayerId;
      _you.Name = msg.Name;
      _you.Class = msg.Class;
      _you.ItemLevel = msg.ItemLevel;
      AddYou();
    }

    public void ProcessMessage(PKTInitEnv msg) {
      RemoveExpired();
      InitYou();
      _you.Id = msg.PlayerId;
      _you.Name ??= "YOU";
      _you.Kills = 0;
      _you.TotalStats = new();
      _you.SkillStats.Clear();
      _you.Deaths.Clear();
      _you.CheckInvariants();
      _expiry.Clear();
      _entities.Clear();
      _players.Clear();
      AddYou();
    }

    public void ProcessMessage(PKTNewPC msg) {
      RemoveExpired();
      NewPc(msg.PlayerId, msg.Name, msg.Class, msg.ItemLevel);
    }

    public void ProcessMessage(PKTSkillStartNotify msg) {
      RemoveExpired();

      Skill.Db.TryGetValue(msg.SkillId, out Skill skill);

      if (!_entities.TryGetValue(msg.SourceId, out Entity src) || !Resolve(ref src)) {
        if (skill?.Class is null) return;
        ulong id = src is not null ? src.OwnerId.Value : msg.SourceId;
        src = NewPc(id, "$" + id, skill.Class.Value, ilvl: null);
      } else if (src.Type != EntityType.Player) {
        return;
      } else if (ReferenceEquals(src, _you)) {
        _you.Class ??= skill?.Class;
      }

      string name = SkillName(msg.SkillId, 10 * msg.SkillId);
      if (!src.SkillStats.TryGetValue(name, out SkillStats stats)) {
        stats = new();
        src.SkillStats.Add(name, stats);
      }

      stats.Build ??= new();
      stats.Build.Level = msg.SkillLevel;
      stats.Build.Tripods = msg.Tripods ?? new byte[] {0, 0, 0};
      stats.Build.TripodsLevels = msg.TripodLevels ?? new byte[] { 1, 1, 1 };
    }

    public void ProcessMessage(PKTNewProjectile msg) {
      RemoveExpired();
      if (_you is not null) Check(msg.projectileInfo.ProjectileId != _you.Id);

      if (_entities.TryGetValue(msg.projectileInfo.ProjectileId, out Entity x)) {
        SetName(x, null);
        x.OwnerId = msg.projectileInfo.OwnerId;
      } else {
        x = new Entity() {
          Type = EntityType.Projectile,
          Id = msg.projectileInfo.ProjectileId,
          OwnerId = msg.projectileInfo.OwnerId,
        };
        _entities.Add(x.Id, x);
      }

      if (x.Type != EntityType.Projectile) {
        x.Type = EntityType.Projectile;
        x.Name = null;
        x.Class = null;
        x.ItemLevel = null;
        x.SkillStats = null;
        x.TotalStats = null;
        x.Deaths = null;
        x.Kills = null;
      }

      Refresh(x);
      x.CheckInvariants();
    }

    public void ProcessMessage(PKTNewNpcSummon msg) {
      RemoveExpired();
      if (_you is not null) Check(msg.NpcId != _you.Id);

      if (_entities.TryGetValue(msg.NpcId, out Entity x)) {
        SetName(x, null);
        x.OwnerId = msg.OwnerId;
      } else {
        x = new Entity() {
          Type = EntityType.Summon,
          Id = msg.NpcId,
          OwnerId = msg.OwnerId,
        };
        _entities.Add(x.Id, x);
      }

      if (x.Type != EntityType.Summon) {
        x.Type = EntityType.Summon;
        x.Name = null;
        x.Class = null;
        x.ItemLevel = null;
        x.SkillStats = null;
        x.TotalStats = null;
        x.Deaths = null;
        x.Kills = null;
      }

      Refresh(x);
      x.CheckInvariants();
    }

    public void ProcessMessage(PKTSkillDamageAbnormalMoveNotify msg) {
      RemoveExpired();
      ProcessDamage(msg.SourceId, msg.SkillId, msg.SkillEffectId, msg.Targets);
    }

    public void ProcessMessage(PKTSkillDamageNotify msg) {
      RemoveExpired();
      ProcessDamage(msg.SourceId, msg.SkillId, msg.SkillEffectId, msg.Targets);
    }

    bool Resolve(ref Entity x) {
      Refresh(x);
      for (int i = 0; i != 3; ++i) {
        if (!x.OwnerId.HasValue) return true;
        if (!_entities.TryGetValue(x.OwnerId.Value, out Entity owner)) break;
        x = owner;
        Refresh(x);
      }
      return false;
    }

    public void ProcessMessage(PKTDeathNotify msg) {
      RemoveExpired();

      if (_entities.TryGetValue(msg.SourceId, out Entity src) &&
          Resolve(ref src) &&
          src.Type == EntityType.Player &&
          msg.SourceId != msg.TargetId) {
        ++src.Kills;
      }

      if (!_entities.TryGetValue(msg.TargetId, out Entity target)) return;
      Refresh(target);

      if (target.Type == EntityType.Player) {
        if (msg.SourceId == msg.TargetId) {
          _log.Info("Suicide: {0}", target.Name);
        } else {
          target.Deaths.Add(DateTime.UtcNow);
          _log.Info("Death: {0} <= {1} {2}{3}",
                    target.Name, Printer.Print(src?.Type), msg.SourceId,
                    src?.Name is null ? string.Empty : ' ' + src.Name);
        }
      } else {
        Check(_entities.Remove(target.Id));
      }
    }

    void Refresh(Entity x) {
      if (x.Type == EntityType.Player) return;
      x.ExpiresAt = DateTime.UtcNow + x.Type switch {
        EntityType.Summon => SummonTtl,
        EntityType.Projectile => ProjectileTtl,
        _ => throw new Exception($"unhandled {nameof(EntityType)}: {x.Type}"),
      };
      _expiry.Push(x);
    }

    void RemoveExpired() {
      DateTime now = DateTime.UtcNow;
      while (_expiry.TryPop(now, out Entity expired)) {
        if (_entities.TryGetValue(expired.Id, out Entity x) && ReferenceEquals(expired, x)) {
          _log.Debug("Expired: {0}", expired);
          Check(expired.Type != EntityType.Player);
          Check(_entities.Remove(expired.Id));
        }
      }
    }

    void ProcessDamage(ulong srcId, uint skillId, uint skillEffectId, IEnumerable<SkillDamageEvent> events) {
      if (!_entities.TryGetValue(srcId, out Entity src) || !Resolve(ref src) || src.Type != EntityType.Player) return;

      string skillName = SkillName(skillId, skillEffectId);

      foreach (SkillDamageEvent e in events) {
        if (_entities.TryGetValue(e.TargetId, out Entity target)) Refresh(target);
        if (e.HitFlag == HitFlag.DAMAGE_SHARE && skillId == 0 && skillEffectId == 0) return;

        long dmg = e.Damage;
        if (e.CurHp < 0) dmg += e.CurHp;

        if (dmg <= 0) {
          if (dmg < 0) {
            _log.Warn("Negative damage: {0} {1} {2} => {3} (CurHp {4}, MaxHp {5})",
                      src.Name, Strings.Quote(skillName), dmg, e.TargetId,
                      e.CurHp, e.MaxHp);
          }
          continue;
        }

        _log.Debug("Damage: {0} {1} {2} => {3} (CurHp {4}, MaxHp {5})",
                    src.Name, Strings.Quote(skillName), dmg, e.TargetId,
                    e.CurHp, e.MaxHp);

        if (!src.SkillStats.TryGetValue(skillName, out SkillStats skillStats)) {
          skillStats = new();
          src.SkillStats.Add(skillName, skillStats);
        }

        foreach (HitStats s in new[] { src.TotalStats.Total, src.TotalStats.Get(e.HitOption),
                                       skillStats.Stats.Total, skillStats.Stats.Get(e.HitOption) }) {
          ++s.NumHits;
          s.TotalDamage += dmg;
          if (e.HitFlag == HitFlag.CRITICAL || e.HitFlag == HitFlag.DOT_CRITICAL) {
            ++s.NumCrits;
            s.CritDamage += dmg;
          }
        }
      }
    }

    Entity NewPc(ulong id, string name, Class c, float? ilvl) {
      _log.Info("New player: {0} {1} {2} {3}", id, name, c, ilvl);

      if (_you is not null) {
        Check(id != _you.Id);
        Check(name != _you.Name);
      }

      if (_players.TryGetValue(name, out Entity x)) {
        SetId(x, id);
      } else if (_entities.TryGetValue(id, out x)) {
        Check(!ReferenceEquals(x, _you));
        SetName(x, name);
      } else {
        x = new Entity() {
          Type = EntityType.Player,
          Id = id,
          Name = name,
          ExpiresAt = null,
          SkillStats = new(),
          TotalStats = new(),
          Deaths = new(),
          Kills = 0,
        };
        _entities.Add(x.Id, x);
        _players.Add(x.Name, x);
      }

      if (x.Type != EntityType.Player) {
        x.Type = EntityType.Player;
        x.ExpiresAt = null;
        x.SkillStats = new();
        x.TotalStats = new();
        x.Deaths = new();
        x.Kills = 0;
      }

      x.Class = c;
      x.ItemLevel = ilvl;
      x.CheckInvariants();
      return x;
    }

    void InitYou() {
      if (_you is null) {
        _you = new Entity() {
          Type = EntityType.Player,
          SkillStats = new(),
          TotalStats = new(),
          Deaths = new(),
          Kills = 0,
        };
      } else {
        Check(_entities.Remove(_you.Id));
        Check(_players.Remove(_you.Name));
        Check(_you.Type == EntityType.Player);
      }
    }

    void AddYou() {
      Check(_you.Type == EntityType.Player);
      _you.CheckInvariants();
      if (_entities.TryGetValue(_you.Id, out Entity other)) Evict(other);
      if (_players.TryGetValue(_you.Name, out other)) Evict(other);
      _entities.Add(_you.Id, _you);
      _players.Add(_you.Name, _you);
    }

    void Evict(Entity x) {
      _log.Warn("Evicting an entity due to a clash: {0}", x);
      Check(_entities.Remove(x.Id));
      if (x.Type == EntityType.Player) Check(_players.Remove(x.Name));
    }

    void SetId(Entity x, ulong id) {
      if (x.Id == id) return;
      Check(_entities.Remove(x.Id));
      if (_entities.TryGetValue(id, out Entity other)) Evict(other);
      _entities.Add(id, x);
      x.Id = id;
    }

    void SetName(Entity x, string name) {
      if (x.Type == EntityType.Player) {
        Check(_players.Remove(x.Name));
        if (name is not null) {
          if (_players.TryGetValue(name, out Entity other)) Evict(other);
          _players.Add(name, x);
        }
      }
      x.Name = name;
    }

    string SkillName(uint id, uint effectId) {
      string res = Impl(id, effectId);
      if (_seenSkill.Add((id, effectId))) _log.Debug("Skill {0}/{1} => {2}", id, effectId, res);
      return res;

      static string Impl(uint id, uint effectId) {
        uint origId = id;
        if (id == 0) {
          if (SkillEffect.Db.TryGetValue(effectId, out string effect)) return effect;
          id = effectId / 10;
        }
        if (Skill.Db.TryGetValue(id, out Skill skill) && skill.Name is not null) return skill.Name;
        return origId == 0 ? "0/" + effectId : origId.ToString();
      }
    }
  }
}
