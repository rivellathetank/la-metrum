namespace LaMetrum {
  enum ZoneEventType : uint {
    GuardianRaidMask = 0x10,
    GuardianRaidEnd = 0x11,

    CoopMask = 0x20,
    CoopStart = 0x21,
    CoopSuccess = 0x23,
    CoopFail = 0x24,
    CoopCancel = 0x25,

    SubZoneMask = 0x40,
    SubZoneEnter = 0x41,

    GateEndMask = 0x400,
    GateSuccessMask = 0x500,
    Gate1Success = 0x501,
    Gate2Success = 0x502,
    Gate3Success = 0x503,
    Gate4Success = 0x504,
    Gate5Success = 0x505,
    GateFailMask = 0x600,
    GateFail1 = 0x601,
    GateFail2 = 0x602,
    GateFail3 = 0x603,
    GateFail4 = 0x604,
    GateFail5 = 0x605,
  }

  enum HitType {
    Regular,
    BackAttack,
    FrontAttack,
  }

  interface IEvent {
    R Visit<R, A>(IEventVisitor<R, A> v, A arg);
    void Validate();
  }

  interface IEventVisitor<R, A> {
    R Visit(NewZone x, A arg);
    R Visit(ZoneEvent x, A arg);
    R Visit(NewPlayer x, A arg);
    R Visit(PlayerSkillStart x, A arg);
    R Visit(PlayerHit x, A arg);
    R Visit(PlayerKill x, A arg);
  }

  record NewZone() : IEvent {
    public R Visit<R, A>(IEventVisitor<R, A> v, A arg) => v.Visit(this, arg);
    public void Validate() { }
  }

  record ZoneEvent(ZoneEventType Type) : IEvent {
    public R Visit<R, A>(IEventVisitor<R, A> v, A arg) => v.Visit(this, arg);

    public void Validate() {
      Check(Enum.IsDefined(Type));
      Check(((int)Type & 0x0F) == 0);
    }
  }

  record NewPlayer(string Name, Class Class, float? ItemLevel, bool You) : IEvent {
    public static bool IsSyntheticName(string name) => !string.IsNullOrEmpty(name) && (name == "YOU" || name[0] == '$');

    public R Visit<R, A>(IEventVisitor<R, A> v, A arg) => v.Visit(this, arg);

    public void Validate() {
      Check(!string.IsNullOrWhiteSpace(Name));
      Check(Enum.IsDefined(Class), Class);
      if (ItemLevel.HasValue) {
        Check(ItemLevel >= 0 && ItemLevel <= 2000);
        Check(!IsSyntheticName(Name));
      } else {
        Check(IsSyntheticName(Name));
        Check(You || Name != "YOU");
      }
    }
  }

  record SkillBuild(int SkillLevel, uint Tripods, uint TripodLevels) {
    public int Tripod(int row) {
      Check(row >= 1 && row <= 3, row);
      byte tripod = (byte)(Tripods >> (8 * (row - 1)));
      Check(tripod <= 3);
      return tripod;
    }

    public int TripodLevel(int row) {
      Check(row >= 1 && row <= 3, row);
      byte lvl = (byte)(Tripods >> (8 * (row - 1)));
      Check(lvl <= 5);
      return lvl;
    }

    public string FormatTripods() => SkillLevel >= 4 ? Tripods.ToString("D03") : null;
    public string FormatTripodLevels() => SkillLevel >= 4 ? TripodLevels.ToString("D03") : null;

    public void Validate() {
      if (SkillLevel == 0) {
        Check(Tripods == 0);
        Check(TripodLevels == 0);
      } else {
        Check(SkillLevel >= 1 && SkillLevel <= 12);
        Check((Tripods >> 24) == 0);
        Check((TripodLevels >> 24) == 0);
        for (int row = 1; row <= 3; ++row) {
          int tripod = Tripod(row);
          int lvl = TripodLevel(row);
          int max = row switch {
            1 => SkillLevel >= 4 ? 3 : 0,
            2 => SkillLevel >= 7 ? 3 : 0,
            3 => SkillLevel >= 10 ? 2 : 0,
            _ => throw new Exception("invalid tripod row: " + row),
          };
          Check(tripod >= 0 && tripod <= max);
          if (tripod == 0) {
            Check(lvl == 0, lvl);
          } else {
            Check(lvl >= 1 && lvl <= 5, lvl);
          }
        }
      }
    }
  }

  record PlayerSkillStart(string PlayerName, string SkillName, SkillBuild SkillBuild) : IEvent {
    public R Visit<R, A>(IEventVisitor<R, A> v, A arg) => v.Visit(this, arg);

    public void Validate() {
      Check(!string.IsNullOrWhiteSpace(PlayerName));
      Check(!string.IsNullOrWhiteSpace(SkillName));
      Check(SkillBuild is not null);
      SkillBuild.Validate();
    }
  }

  record HitSource(string SkillName, string EffectName) {
    public void Validate() {
      Check(SkillName is not null);
      Check(EffectName is not null);
      Check(!Strings.IsWhitespace(SkillName));
      Check(!Strings.IsWhitespace(EffectName));
      Check(SkillName.Length > 0 || EffectName.Length > 0);
    }
  }

  record PlayerHit(
      string SourcePlayerName,
      string TargetPlayerName,
      HitSource HitSource,
      long FullDamage,
      long OverkillDamage,
      bool Crit,
      HitType Type) : IEvent {
    public long EffectiveDamage => FullDamage - OverkillDamage;

    public R Visit<R, A>(IEventVisitor<R, A> v, A arg) => v.Visit(this, arg);

    public void Validate() {
      Check(!string.IsNullOrWhiteSpace(SourcePlayerName));
      Check(!Strings.IsWhitespace(TargetPlayerName));
      Check(FullDamage >= OverkillDamage);
      Check(OverkillDamage >= 0);
      Check(Enum.IsDefined(Type));
      Check(HitSource is not null);
      HitSource.Validate();
    }
  }

  record PlayerKill(string SourcePlayerName, string TargetPlayerName) : IEvent {
    public R Visit<R, A>(IEventVisitor<R, A> v, A arg) => v.Visit(this, arg);

    public void Validate() {
      Check(!Strings.IsWhitespace(SourcePlayerName));
      Check(!Strings.IsWhitespace(TargetPlayerName));
      Check(SourcePlayerName is not null || TargetPlayerName is not null);
    }
  }
}
