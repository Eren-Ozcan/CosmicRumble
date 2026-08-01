# CosmicRumble — Master Implementation Prompt
# Achievement + Economy + Level + Costume Systems
# This prompt is written to be handed directly to Claude Code.

---

## BEFORE YOU START

1. Read CLAUDE.md and understand the existing project structure
2. Review the existing files: TurnManager, ProjectileBase, IAbility implementations,
   DestructiblePlanet — identify the real file paths
3. Implement the systems below in order; each section depends on the previous one
4. Update CLAUDE.md after each system is completed

---

# SECTION 1 — CURRENCIES AND CURRENCY SYSTEM

## 1.1 CurrencyType.cs
`Scripts/Economy/Core/CurrencyType.cs`

```csharp
public enum CurrencyType { XP, Gold, Gem }
```

## 1.2 CurrencyManager.cs
`Scripts/Economy/Core/CurrencyManager.cs`
Singleton, DontDestroyOnLoad

- `Add(CurrencyType type, long amount)` — adds, raises an event
- `Spend(CurrencyType type, long amount) → bool` — deducts if the balance is sufficient
- `Get(CurrencyType type) → long` — current balance
- `OnCurrencyChanged` event: `Action<CurrencyType, long>` (type, newBalance)
- Save: `Application.persistentDataPath/currency.json`
- Gem logging rule: every Gem.Add call is recorded via Debug.Log
  (for the IAP audit trail)

---

# SECTION 2 — LEVEL & PRESTIGE SYSTEM

## 2.1 LevelConfig.cs
`Scripts/Economy/Core/LevelConfig.cs`
ScriptableObject — `Resources/Economy/LevelConfig`

XP thresholds (XP required per level):
- Lv   1–10  → 100 XP   (cumulative total: 1,000)
- Lv  11–50  → 500 XP   (cumulative total: 21,000)
- Lv  51–100 → 1,000 XP (cumulative total: 71,000)
- Lv 101+    → 2,000 XP (including prestige, unlimited)

```csharp
public int GetXPForLevel(int level)
{
    if (level <= 10)  return 100;
    if (level <= 50)  return 500;
    if (level <= 100) return 1000;
    return 2000;
}
public long GetTotalXPForLevel(int level) { /* cumulative */ }
public int  GetLevelFromTotalXP(long totalXP) { /* inverse calculation */ }
public const int MaxLevelBeforePrestige = 100;
```

## 2.2 PlayerProgressData.cs
`Scripts/Economy/Core/PlayerProgressData.cs`

```csharp
[Serializable]
public class PlayerProgressData
{
    public long  totalXP;
    public int   currentLevel;
    public int   prestigeRank;        // 0 = normal, 1+ = prestige
    public long  xpInCurrentLevel;
    public long  xpNeededForNextLevel;
    public float levelProgress;       // 0.0–1.0 (progress bar)
}
```

## 2.3 PlayerLevelManager.cs
`Scripts/Economy/Core/PlayerLevelManager.cs`
Singleton, DontDestroyOnLoad

- Subscribes to CurrencyManager.OnCurrencyChanged(XP)
- `CheckLevelUp()` — can skip multiple levels at once
- `OnLevelUp` event: `Action<int, int>` (oldLevel, newLevel)
- `OnPrestige` event: `Action<int>` (newPrestigeRank)
- Once level 100 is completed, prestige starts automatically on the next XP gain
- With prestige, levels continue as 101, 102... (they are not reset)
- `GetProgress() → PlayerProgressData`
- Save: `Application.persistentDataPath/progress.json`

---

# SECTION 3 — UNLOCK SYSTEM

## 3.1 UnlockableItem.cs
`Scripts/Economy/Unlocks/UnlockableItem.cs`
ScriptableObject

```csharp
[CreateAssetMenu(menuName = "CosmicRumble/Economy/UnlockableItem")]
public class UnlockableItem : ScriptableObject
{
    public string        itemId;
    public string        displayName;
    public Sprite        icon;          // may be null, the UI shows a placeholder accordingly
    public UnlockableType  type;        // Weapon, Skill, Cosmetic
    public UnlockMethod    unlockMethod;// Default, ByLevel, ByGold, ByGem, ByAchievement
    public int   requiredLevel;
    public long  goldCost;
    public long  gemCost;
    public string requiredAchievementId;
    public bool  isDefault;
}

public enum UnlockableType { Weapon, Skill, Cosmetic }
public enum UnlockMethod   { Default, ByLevel, ByGold, ByGem, ByAchievement }
```

## 3.2 UnlockCheckResult.cs
`Scripts/Economy/Unlocks/UnlockCheckResult.cs`

```csharp
public struct UnlockCheckResult
{
    public bool isLevelMet;
    public bool isCurrencyMet;
    public bool isAchievementMet;
    public bool canUnlock;        // are all conditions satisfied
    public long missingGold;
    public long missingGem;
    public int  missingLevel;
}
```

## 3.3 UnlockDatabase.cs
`Scripts/Economy/Unlocks/UnlockDatabase.cs`
ScriptableObject — `Resources/Economy/UnlockDatabase`

```csharp
[CreateAssetMenu(menuName = "CosmicRumble/Economy/UnlockDatabase")]
public class UnlockDatabase : ScriptableObject
{
    public List<UnlockableItem> allItems;
    public UnlockableItem GetById(string id) { ... }
    public List<UnlockableItem> GetByType(UnlockableType type) { ... }
    public List<UnlockableItem> GetUnlockedAtLevel(int level) { ... }
}
```

Create the following items as ScriptableObjects and add them to the database:

### Weapons:
| itemId           | displayName      | unlockMethod | requiredLevel | isDefault |
|------------------|------------------|--------------|---------------|-----------|
| weapon_pistol    | Pistol           | Default      | —             | true      |
| weapon_shotgun   | Shotgun          | Default      | —             | true      |
| weapon_rpg       | Rocket Launcher  | Default      | —             | true      |
| weapon_bomb      | Bomb             | ByLevel      | 2             | false     |
| weapon_grenade   | Grenade          | ByLevel      | 6             | false     |

### Skills:
| itemId           | displayName      | unlockMethod | requiredLevel |
|------------------|------------------|--------------|---------------|
| skill_superjump  | Super Jump       | ByLevel      | 4             |
| skill_shield     | Shield           | ByLevel      | 8             |
| skill_blackhole  | Black Hole       | ByLevel      | 10            |
| skill_teleport   | Teleport         | ByLevel      | 10            |
| skill_bathammer  | Bat Hammer       | ByLevel      | 10            |

### Cosmetics (level + gold):
| itemId                | displayName       | requiredLevel | goldCost |
|-----------------------|-------------------|---------------|----------|
| skin_cosmic_blue      | Cosmic Blue       | 15            | 500      |
| skin_fire_red         | Fire Red          | 20            | 800      |
| skin_void_dark        | Void Dark         | 30            | 1200     |
| skin_golden_legend    | Golden Legend     | 45            | 2000     |
| skin_neon_pulse       | Neon Pulse        | 60            | 3000     |
| skin_prestige_shadow  | Prestige Shadow   | 80            | 5000     |
| skin_cosmic_master    | Cosmic Master     | 100           | 0 (100 Gem) |

## 3.4 UnlockManager.cs
`Scripts/Economy/Unlocks/UnlockManager.cs`
Singleton, DontDestroyOnLoad

- `IsUnlocked(string itemId) → bool`
- `CanUnlock(string itemId) → UnlockCheckResult`
- `TryUnlock(string itemId) → bool` (deducts currency, saves)
- `GetAllUnlocked() → List<UnlockableItem>`
- `OnItemUnlocked` event: `Action<UnlockableItem>`
- Subscribe to PlayerLevelManager.OnLevelUp → automatically check level unlocks
- Unlock all items with isDefault==true at startup
- Save: `Application.persistentDataPath/unlocks.json`

---

# SECTION 4 — COSTUME SYSTEM (150 COSTUMES)

## 4.1 CostumeRarity.cs
`Scripts/Economy/Costumes/CostumeRarity.cs`

```csharp
public enum CostumeRarity  { Common, Uncommon, Rare, Epic, Legendary }
public enum CostumeType    { Character, Weapon }
public enum CostumeTheme   { Space, Fantasy, Cyber, Nature, Dark, Fire, Ice, Mech, Myth, Other }
public enum CostumeUnlock  { Default, ByLevel, ByGold, ByGem, ByChest, ByAchievement }
```

## 4.2 CostumeDefinition.cs
`Scripts/Economy/Costumes/CostumeDefinition.cs`
ScriptableObject

```csharp
[CreateAssetMenu(menuName = "CosmicRumble/Economy/CostumeDefinition")]
public class CostumeDefinition : ScriptableObject
{
    public string        costumeId;
    public string        displayName;
    public Sprite        previewSprite;   // null-safe: the UI shows a placeholder
    public CostumeType   costumeType;
    public CostumeRarity rarity;
    public CostumeTheme  theme;
    public CostumeUnlock unlockMethod;

    // Unlock conditions (whichever is populated for the given unlockMethod is used)
    public int    requiredLevel;
    public long   goldCost;
    public long   gemCost;
    public string requiredAchievementId;
    public string unlockDescription;     // condition text to display in the UI
}
```

## 4.3 CostumeDatabase.cs
`Scripts/Economy/Costumes/CostumeDatabase.cs`
ScriptableObject — `Resources/Economy/CostumeDatabase`

```csharp
[CreateAssetMenu(menuName = "CosmicRumble/Economy/CostumeDatabase")]
public class CostumeDatabase : ScriptableObject
{
    public List<CostumeDefinition> allCostumes;
    public CostumeDefinition GetById(string id) { ... }
    public List<CostumeDefinition> GetByRarity(CostumeRarity r) { ... }
    public List<CostumeDefinition> GetByType(CostumeType t) { ... }
    public List<CostumeDefinition> GetByTheme(CostumeTheme t) { ... }
}
```

Create the following 150 costumes as ScriptableObjects and add them to CostumeDatabase.
The `previewSprite` field is to be left null — the UI system shows a placeholder automatically.

### COMMON (40 costumes)
| costumeId | displayName | costumeType | theme | unlockMethod | Condition |
|---|---|---|---|---|---|
| c001 | Gray Soldier | Character | Other | Default | Starter |
| c002 | Standard Blue | Character | Other | Default | Starter |
| c003 | Red Warrior | Character | Other | ByLevel | Lv 3 |
| c004 | Green Camo | Character | Nature | ByLevel | Lv 5 |
| c005 | Yellow Storm | Character | Other | ByGold | 200 Gold |
| c006 | Orange Ember | Character | Fire | ByGold | 200 Gold |
| c007 | Purple Night | Character | Dark | ByGold | 250 Gold |
| c008 | White Snow | Character | Ice | ByGold | 250 Gold |
| c009 | Brown Earth | Character | Nature | ByLevel | Lv 7 |
| c010 | Sky Blue | Character | Space | ByLevel | Lv 9 |
| c011 | Steel Gray | Weapon | Mech | ByGold | 150 Gold |
| c012 | Rust Brown | Weapon | Mech | ByGold | 150 Gold |
| c013 | Forest Green | Weapon | Nature | ByLevel | Lv 6 |
| c014 | Lava Red | Weapon | Fire | ByGold | 175 Gold |
| c015 | Ice Blue | Weapon | Ice | ByGold | 175 Gold |
| c016 | Night Black | Weapon | Dark | ByLevel | Lv 8 |
| c017 | Sun Yellow | Weapon | Other | ByGold | 200 Gold |
| c018 | Coral Pink | Character | Other | ByGold | 200 Gold |
| c019 | Sea Teal | Character | Other | ByLevel | Lv 11 |
| c020 | Lavender | Character | Other | ByGold | 225 Gold |
| c021 | Bright Copper | Weapon | Mech | ByGold | 175 Gold |
| c022 | Desert Sand | Character | Nature | ByLevel | Lv 13 |
| c023 | Pistachio Green | Character | Nature | ByGold | 200 Gold |
| c024 | Sea Foam | Weapon | Ice | ByGold | 150 Gold |
| c025 | Fog Gray | Character | Dark | ByLevel | Lv 15 |
| c026 | Sunset | Character | Fire | ByGold | 250 Gold |
| c027 | Stardust | Weapon | Space | ByLevel | Lv 12 |
| c028 | Ocean Depths | Weapon | Other | ByChest | Common Chest |
| c029 | Chalk White | Character | Other | ByChest | Common Chest |
| c030 | Anthracite | Character | Dark | ByChest | Common Chest |
| c031 | Mint Green | Weapon | Nature | ByGold | 175 Gold |
| c032 | Candy Pink | Character | Other | ByGold | 200 Gold |
| c033 | Thunder | Weapon | Other | ByLevel | Lv 14 |
| c034 | Golden Yellow | Weapon | Other | ByGold | 225 Gold |
| c035 | Emerald | Character | Nature | ByLevel | Lv 16 |
| c036 | Hedgehog Brown | Character | Nature | ByChest | Common Chest |
| c037 | Titan Gray | Weapon | Mech | ByChest | Common Chest |
| c038 | Maroon | Character | Dark | ByGold | 200 Gold |
| c039 | Cobalt | Weapon | Space | ByGold | 200 Gold |
| c040 | Indigo Blue | Character | Other | ByLevel | Lv 18 |

### UNCOMMON (35 costumes)
| costumeId | displayName | costumeType | theme | unlockMethod | Condition |
|---|---|---|---|---|---|
| u001 | Forest Warrior | Character | Nature | ByLevel | Lv 20 |
| u002 | Ice Mage | Character | Ice | ByLevel | Lv 22 |
| u003 | Flame Dancer | Character | Fire | ByLevel | Lv 24 |
| u004 | Night Watcher | Character | Dark | ByGold | 500 Gold |
| u005 | Lightning Runner | Character | Other | ByGold | 500 Gold |
| u006 | Sandstorm | Character | Nature | ByLevel | Lv 26 |
| u007 | Deep Space | Character | Space | ByLevel | Lv 28 |
| u008 | Iron Fist | Character | Mech | ByGold | 550 Gold |
| u009 | Wind Spirit | Character | Nature | ByGold | 550 Gold |
| u010 | Cosmic Purple | Character | Space | ByLevel | Lv 30 |
| u011 | Dragon Fang | Weapon | Fantasy | ByGold | 500 Gold |
| u012 | Space Rifle | Weapon | Space | ByLevel | Lv 25 |
| u013 | Ice Sword | Weapon | Ice | ByGold | 500 Gold |
| u014 | Flame Spear | Weapon | Fire | ByLevel | Lv 27 |
| u015 | Shadow Blade | Weapon | Dark | ByGold | 525 Gold |
| u016 | Fog Pistol | Weapon | Dark | ByChest | Rare Chest |
| u017 | Plasma Tube | Weapon | Cyber | ByLevel | Lv 29 |
| u018 | Nature Shield | Weapon | Nature | ByGold | 500 Gold |
| u019 | Lightning Orb | Weapon | Other | ByChest | Rare Chest |
| u020 | Iron Shield | Weapon | Mech | ByGold | 550 Gold |
| u021 | Crystal Warrior | Character | Ice | ByAchievement | achievement_ice_master |
| u022 | Volcano Man | Character | Fire | ByAchievement | achievement_patlama_uzmani |
| u023 | Cyber Ninja | Character | Cyber | ByLevel | Lv 32 |
| u024 | Stone Golem | Character | Nature | ByLevel | Lv 34 |
| u025 | Neon Jacket | Character | Cyber | ByGold | 600 Gold |
| u026 | Foam Sailor | Character | Other | ByChest | Rare Chest |
| u027 | Steppe Soldier | Character | Nature | ByLevel | Lv 36 |
| u028 | Silver Knight | Character | Fantasy | ByGold | 600 Gold |
| u029 | Blue Crocodile | Weapon | Nature | ByChest | Rare Chest |
| u030 | Ember Blade | Weapon | Fire | ByLevel | Lv 31 |
| u031 | Hologram Weapon | Weapon | Cyber | ByGold | 575 Gold |
| u032 | Steel Dragon | Weapon | Mech | ByLevel | Lv 33 |
| u033 | Crystal Bomb | Weapon | Ice | ByGold | 550 Gold |
| u034 | Root Texture | Weapon | Nature | ByAchievement | PLANET_KILLER |
| u035 | Storm Sail | Character | Other | ByGold | 600 Gold |

### RARE (35 costumes)
| costumeId | displayName | costumeType | theme | unlockMethod | Condition |
|---|---|---|---|---|---|
| r001 | Galaxy Wanderer | Character | Space | ByLevel | Lv 40 |
| r002 | Black Knight | Character | Dark | ByLevel | Lv 42 |
| r003 | Neon Samurai | Character | Cyber | ByLevel | Lv 44 |
| r004 | Dragon Hunter | Character | Fantasy | ByLevel | Lv 45 |
| r005 | Ice God | Character | Ice | ByGold | 1200 Gold |
| r006 | Lava Giant | Character | Fire | ByGold | 1200 Gold |
| r007 | Quantum Armor | Character | Cyber | ByLevel | Lv 48 |
| r008 | Forest God | Character | Nature | ByGold | 1300 Gold |
| r009 | Dark Sorcerer | Character | Dark | ByAchievement | VETERAN_10 |
| r010 | Meteor Warrior | Character | Space | ByLevel | Lv 50 |
| r011 | Plasma Rifle | Weapon | Cyber | ByLevel | Lv 41 |
| r012 | Dragon Flame | Weapon | Fantasy | ByGold | 1100 Gold |
| r013 | Black Hole Cannon | Weapon | Space | ByLevel | Lv 43 |
| r014 | Ice Shield | Weapon | Ice | ByGold | 1100 Gold |
| r015 | Ember Bomb | Weapon | Fire | ByAchievement | PATLAMA_UZMANI |
| r016 | Nano Blade | Weapon | Cyber | ByGold | 1200 Gold |
| r017 | Rune Spear | Weapon | Fantasy | ByLevel | Lv 46 |
| r018 | Shadow Arrow | Weapon | Dark | ByChest | Epic Chest |
| r019 | Emerald Dragon | Weapon | Fantasy | ByGold | 1250 Gold |
| r020 | Star Sword | Weapon | Space | ByLevel | Lv 47 |
| r021 | Titanium Golem | Character | Mech | ByLevel | Lv 52 |
| r022 | Light Speed | Character | Space | ByGold | 1400 Gold |
| r023 | Sea Monster | Character | Nature | ByAchievement | SOSYAL_KELEBEK |
| r024 | Storm God | Character | Myth | ByLevel | Lv 54 |
| r025 | Crimson Shaman | Character | Myth | ByGold | 1400 Gold |
| r026 | Cyber Samurai | Character | Cyber | ByLevel | Lv 56 |
| r027 | Bionic Warrior | Character | Mech | ByGold | 1500 Gold |
| r028 | Vortex Rifle | Weapon | Space | ByChest | Epic Chest |
| r029 | Shaman Staff | Weapon | Myth | ByLevel | Lv 49 |
| r030 | Titan Hammer | Weapon | Mech | ByGold | 1300 Gold |
| r031 | Wind Blade | Weapon | Nature | ByAchievement | CEVRECI |
| r032 | Crystal Staff | Weapon | Fantasy | ByLevel | Lv 51 |
| r033 | Laser Rifle | Weapon | Cyber | ByGold | 1350 Gold |
| r034 | Dark Rune | Weapon | Dark | ByLevel | Lv 53 |
| r035 | Mythic Archer | Character | Myth | ByAchievement | SAVAS_MAKINESI |

### EPIC (25 costumes)
| costumeId | displayName | costumeType | theme | unlockMethod | Condition |
|---|---|---|---|---|---|
| e001 | Nebula Warrior | Character | Space | ByLevel | Lv 60 |
| e002 | Dragon Lord | Character | Fantasy | ByLevel | Lv 63 |
| e003 | Cyber God | Character | Cyber | ByGem | 50 Gem |
| e004 | Death Spirit | Character | Dark | ByLevel | Lv 66 |
| e005 | Volcano God | Character | Fire | ByGem | 60 Gem |
| e006 | Ice Storm | Character | Ice | ByLevel | Lv 70 |
| e007 | Forest Deity | Character | Nature | ByAchievement | EFSANE |
| e008 | Titan Armor | Character | Mech | ByGem | 70 Gem |
| e009 | Olympian God | Character | Myth | ByLevel | Lv 73 |
| e010 | Quantum Shadow | Character | Cyber | ByGem | 75 Gem |
| e011 | Galactic Emperor | Character | Space | ByLevel | Lv 76 |
| e012 | Ancient Dragon | Character | Fantasy | ByAchievement | KARA_DELIK_USTASI |
| e013 | Neon Demon | Character | Dark | ByGem | 80 Gem |
| e014 | Plasma God | Weapon | Cyber | ByLevel | Lv 61 |
| e015 | Dragon Breath | Weapon | Fantasy | ByGem | 50 Gem |
| e016 | Dark Star | Weapon | Dark | ByLevel | Lv 64 |
| e017 | Volcano Cannon | Weapon | Fire | ByGem | 55 Gem |
| e018 | Ice Crystal | Weapon | Ice | ByAchievement | DOKUNULMAZ |
| e019 | Nano Swarm | Weapon | Mech | ByLevel | Lv 68 |
| e020 | Rune Burst | Weapon | Fantasy | ByGem | 65 Gem |
| e021 | Nebula Bomb | Weapon | Space | ByLevel | Lv 71 |
| e022 | Titan Laser | Weapon | Mech | ByGem | 70 Gem |
| e023 | Mythic Armor | Character | Myth | ByAchievement | FIRTINA_TANRISI |
| e024 | Crystal Golem | Character | Ice | ByGem | 75 Gem |
| e025 | Crow King | Character | Dark | ByLevel | Lv 80 |

### LEGENDARY (15 costumes)
| costumeId | displayName | costumeType | theme | unlockMethod | Condition |
|---|---|---|---|---|---|
| l001 | Cosmic Master | Character | Space | ByLevel | Lv 100 |
| l002 | Dragon Emperor | Character | Fantasy | ByGem | 200 Gem |
| l003 | Dark God | Character | Dark | ByAchievement | COSMIC_100 |
| l004 | Doom Lord | Character | Dark | ByGem | 250 Gem |
| l005 | Time Master | Character | Myth | ByLevel | Prestige 1 (Lv 101) |
| l006 | Universe Warrior | Character | Space | ByGem | 300 Gem |
| l007 | Ancient Giant | Character | Myth | ByAchievement | GALAKSININ_EFSANESI |
| l008 | Bionic God | Character | Mech | ByGem | 250 Gem |
| l009 | Phoenix Warrior | Character | Fire | ByLevel | Prestige 2 (Lv 102) |
| l010 | Cosmic Destroyer | Weapon | Space | ByLevel | Lv 100 |
| l011 | God Sword | Weapon | Myth | ByGem | 200 Gem |
| l012 | Dragon Heart | Weapon | Fantasy | ByAchievement | EJDER_AVCI |
| l013 | Black Hole Cannon X | Weapon | Space | ByGem | 250 Gem |
| l014 | Doom Hammer | Weapon | Dark | ByAchievement | HOME_RUN |
| l015 | Creator's Power | Weapon | Myth | ByLevel | Prestige 3 (Lv 103) |

## 4.4 CostumeManager.cs
`Scripts/Economy/Costumes/CostumeManager.cs`
Singleton, DontDestroyOnLoad

- `IsOwned(string costumeId) → bool`
- `CanPurchase(string costumeId) → UnlockCheckResult`
- `TryPurchase(string costumeId) → bool`
- `GetEquipped(CostumeType type) → CostumeDefinition`
- `Equip(string costumeId)`
- `OnCostumePurchased` event: `Action<CostumeDefinition>`
- `OnCostumeEquipped` event: `Action<CostumeDefinition>`
- Listen for achievement unlocks via AchievementManager.OnAchievementUnlocked
- Save: `Application.persistentDataPath/costumes.json`

---

# SECTION 5 — ACHIEVEMENT SYSTEM

## 5.1 AchievementDefinition.cs
`Scripts/Achievements/Core/AchievementDefinition.cs`
ScriptableObject

```csharp
[CreateAssetMenu(menuName = "CosmicRumble/Achievement")]
public class AchievementDefinition : ScriptableObject
{
    public string             achievementId;
    public string             displayName;
    public string             description;
    public Sprite             icon;           // null-safe
    public AchievementRarity  rarity;
    public AchievementTriggerType triggerType;
    public int                targetValue;
    public bool               isSecret;

    // Rewards
    public long rewardXP;
    public long rewardGold;
    public long rewardGem;

    // Costume reward (optional)
    public string rewardCostumeId;  // empty means no costume reward
}

public enum AchievementRarity      { Common, Rare, Epic, Legendary }
public enum AchievementTriggerType { SingleUnlock, Cumulative, SpecialAction }
```

Reward table by rarity:
| Rarity    | XP    | Gold  | Gem |
|-----------|-------|-------|-----|
| Common    | 100   | 50    | 0   |
| Rare      | 300   | 150   | 0   |
| Epic      | 600   | 400   | 5   |
| Legendary | 1500  | 1000  | 20  |

## 5.2 AchievementDatabase.cs
`Scripts/Achievements/Core/AchievementDatabase.cs`
ScriptableObject — `Resources/Achievements/AchievementDatabase`

Create the following 50 achievements as ScriptableObjects and add them to the database:

### COMBAT (10)
| achievementId | displayName | description | rarity | triggerType | targetValue | rewardCostumeId |
|---|---|---|---|---|---|---|
| FIRST_BLOOD | First Blood | Win your first match | Common | SingleUnlock | 1 | — |
| VETERAN_10 | Veteran | Win 10 matches | Rare | Cumulative | 10 | r009 |
| SAVAS_MAKINESI | War Machine | Win 25 matches | Rare | Cumulative | 25 | r035 |
| EFSANE | Legend | Win 50 matches | Epic | Cumulative | 50 | e007 |
| COSMIC_100 | Cosmic Master | Win 100 matches | Legendary | Cumulative | 100 | l003 |
| FLAWLESS | Flawless | Win a match without taking any damage | Epic | SingleUnlock | 1 | — |
| UNDERDOG | Underdog | Win while all enemies have more HP | Rare | SingleUnlock | 1 | — |
| HIZLI_BITIR | Quick Finish | Win a match in 5 turns | Rare | SingleUnlock | 1 | — |
| SAMPIYONLAR | Champions League | Win in an 8-player lobby | Epic | SingleUnlock | 1 | — |
| SON_NEFES | Last Breath | Win a match at 1 HP | Legendary | SingleUnlock | 1 | — |

### STATISTICS (10)
| achievementId | displayName | description | rarity | triggerType | targetValue | rewardCostumeId |
|---|---|---|---|---|---|---|
| DAMAGE_1K | Damage Dealer | Deal 1,000 total damage | Common | Cumulative | 1000 | — |
| DAMAGE_50K | Destruction Machine | Deal 50,000 total damage | Rare | Cumulative | 50000 | — |
| DAMAGE_250K | Atom Bomb | Deal 250,000 total damage | Epic | Cumulative | 250000 | — |
| SHOTS_100 | Trigger Happy | Fire 100 shots | Common | Cumulative | 100 | — |
| SHOTS_1K | Ammo Factory | Fire 1,000 shots | Rare | Cumulative | 1000 | — |
| TETIKCI | Gunslinger | Fire 30 shots in a single match | Rare | Cumulative | 30 | — |
| ISABETLI | Accurate | Finish with an 80% hit rate (min 10 shots) | Epic | SingleUnlock | 1 | — |
| SAGLAMDURUG | Solid Stance | Take 10,000 total damage and survive | Rare | Cumulative | 10000 | — |
| GEZEGEN_KATILI | Planet Killer | Destroy 10 planets in total | Epic | Cumulative | 10 | u034 |
| GALAKSI_TAMIRCISI | Galaxy Mechanic | Play 100 matches in total | Common | Cumulative | 100 | — |

### WEAPONS (10)
| achievementId | displayName | description | rarity | triggerType | targetValue | rewardCostumeId |
|---|---|---|---|---|---|---|
| TABANCALI | Pistolero | Hit 50 enemies with the pistol | Common | Cumulative | 50 | — |
| KESKIN_NISANCI | Sharpshooter | Land 10 headshots with the pistol | Rare | Cumulative | 10 | — |
| ROKETCI | Rocketeer | Damage 3+ enemies with a single RPG shot | Rare | SingleUnlock | 1 | r015 |
| PATLAMA_UZMANI | Explosion Expert | Fire 100 total shots with the RPG | Rare | Cumulative | 100 | u022 |
| SAÇMA_YAGMURU | Pellet Rain | Land every pellet of a shotgun blast | Epic | SingleUnlock | 1 | — |
| POMPACI | Pumper | Hit 5 enemies in a row with the shotgun | Rare | SingleUnlock | 1 | — |
| EL_BOMBACI | Grenadier | Hit 2+ enemies with a single grenade | Rare | SingleUnlock | 1 | — |
| PIM_CEKICI | Pin Puller | Throw 25 grenades | Common | Cumulative | 25 | — |
| BOMBA_IMHA | Demolition | Destroy the planet surface with a bomb | Epic | SingleUnlock | 1 | — |
| TAM_CEPHANE | Full Arsenal | Use all 5 weapons in one match | Epic | SingleUnlock | 1 | — |

### SKILLS (10)
| achievementId | displayName | description | rarity | triggerType | targetValue | rewardCostumeId |
|---|---|---|---|---|---|---|
| KARA_DELIK_USTASI | Black Hole Master | Pull 3 enemies with a single Black Hole | Epic | SingleUnlock | 1 | e012 |
| OLAY_UFKU | Event Horizon | Pull 50 enemies with Black Hole | Rare | Cumulative | 50 | — |
| ISINLANAN | Teleported | Teleport behind an enemy and hit them | Rare | SingleUnlock | 1 | — |
| KUANTUM | Quantum | Use Teleport 5 times in a single match | Common | Cumulative | 5 | — |
| DOKUNULMAZ | Untouchable | Block 500 damage with Shield | Rare | Cumulative | 500 | e018 |
| KALKAN_DUVARI | Shield Wall | Block 3 attacks with Shield | Rare | SingleUnlock | 1 | — |
| CEKIC_ZAMANI | Hammer Time | Knock an enemy off the planet with Bat Hammer | Epic | SingleUnlock | 1 | — |
| HOME_RUN | Home Run | Make a Bat Hammer-struck enemy collide with another | Legendary | SingleUnlock | 1 | l014 |
| SUPER_KAHRAMAN | Super Hero | Deal damage by landing on an enemy with Super Jump | Rare | SingleUnlock | 1 | — |
| YÖRÜNGE | Orbit | Change planets with Super Jump and take a shot | Common | SingleUnlock | 1 | — |

### SOCIAL (10)
| achievementId | displayName | description | rarity | triggerType | targetValue | rewardCostumeId |
|---|---|---|---|---|---|---|
| SOSYAL_KELEBEK | Social Butterfly | Play matches with 8 different players | Common | Cumulative | 8 | r023 |
| REKABETCI | Competitor | Finish top 3 in a ranked match | Rare | SingleUnlock | 1 | — |
| KOZMIK_AVCI | Cosmic Hunter | Reach the leaderboard top 10 | Epic | SingleUnlock | 1 | — |
| BIR_NUMARA | Number One | Reach the top of the leaderboard | Legendary | SingleUnlock | 1 | — |
| DUELLO_SAMPIYONU | Duel Champion | Win 10 matches in 1v1 mode | Rare | Cumulative | 10 | — |
| OGRETMEN | Teacher | Guide a new player through the tutorial | Common | SingleUnlock | 1 | — |
| KOZMIK_EKIP | Cosmic Squad | Play 5 matches with the same 3 people | Rare | Cumulative | 5 | — |
| INTIKAM | Revenge | Beat the player who killed you in the next match | Rare | SingleUnlock | 1 | — |
| HERKESE_MEYDAN | Challenge Everyone | Damage 7 different players in the same match | Epic | SingleUnlock | 1 | — |
| GALAKSININ_EFSANESI | Legend of the Galaxy | Complete all 49 achievements | Legendary | SingleUnlock | 1 | l007 |

## 5.3 AchievementEvents.cs
`Scripts/Achievements/Core/AchievementEvents.cs`
Static event bus — no system references another system directly

```csharp
public static class AchievementEvents
{
    public static event Action OnMatchWon;
    public static event Action OnMatchLost;
    public static event Action<int>    OnDamageDealt;       // damage amount
    public static event Action<int>    OnDamageTaken;
    public static event Action<string> OnWeaponUsed;        // weapon itemId
    public static event Action<string> OnAbilityUsed;       // skill itemId
    public static event Action         OnHeadshotLanded;
    public static event Action<int>    OnMatchCompleted;    // total shot count
    public static event Action         OnPlanetDestroyed;
    public static event Action<bool>   OnShotFired;         // bool: isHit
    public static event Action<int>    OnTurnCompleted;     // turn count
    public static event Action<int>    OnPlayerCountInMatch;// player count in the lobby
    public static event Action<string> OnPlayerDefeated;    // id of the defeated player

    // Fire methods (null-safe)
    public static void FireMatchWon()                      => OnMatchWon?.Invoke();
    public static void FireMatchLost()                     => OnMatchLost?.Invoke();
    public static void FireDamageDealt(int amount)         => OnDamageDealt?.Invoke(amount);
    public static void FireDamageTaken(int amount)         => OnDamageTaken?.Invoke(amount);
    public static void FireWeaponUsed(string weaponId)     => OnWeaponUsed?.Invoke(weaponId);
    public static void FireAbilityUsed(string abilityId)   => OnAbilityUsed?.Invoke(abilityId);
    public static void FireHeadshotLanded()                => OnHeadshotLanded?.Invoke();
    public static void FireMatchCompleted(int shots)       => OnMatchCompleted?.Invoke(shots);
    public static void FirePlanetDestroyed()               => OnPlanetDestroyed?.Invoke();
    public static void FireShotFired(bool isHit)           => OnShotFired?.Invoke(isHit);
    public static void FireTurnCompleted(int turnCount)    => OnTurnCompleted?.Invoke(turnCount);
    public static void FirePlayerCountInMatch(int count)   => OnPlayerCountInMatch?.Invoke(count);
    public static void FirePlayerDefeated(string id)       => OnPlayerDefeated?.Invoke(id);
}
```

## 5.4 AchievementTracker.cs
`Scripts/Achievements/Core/AchievementTracker.cs`
Singleton, DontDestroyOnLoad — listens to events, keeps the counters

Cumulative counters to track:
- totalMatchesWon, totalMatchesPlayed
- totalDamageDealt, totalDamageTaken
- totalShotsFired, totalShotsHit
- totalHeadshots, totalPlanetsDestroyed
- weaponsUsedInCurrentMatch (HashSet) → for TAM_CEPHANE
- uniqueOpponentsPlayed (HashSet)
- blackHolePullsInCurrentAbility
- shieldBlockedDamage
- consecutiveShotgunVictims

Clear all event subscriptions in OnDestroy.

## 5.5 AchievementManager.cs
`Scripts/Achievements/Core/AchievementManager.cs`
Singleton, DontDestroyOnLoad

- `UnlockAchievement(string id)` — the single public unlock entry point
  - Return early if already unlocked
  - Grant the rewards: CurrencyManager.Add(XP, Gold, Gem)
  - Notify CostumeManager if there is a costume reward
  - Raise the `OnAchievementUnlocked` event: `Action<AchievementDefinition>`
- `UpdateProgress(string id, int value)` — for cumulative achievements
- `IsUnlocked(string id) → bool`
- Save: `Application.persistentDataPath/achievements.json`

## 5.6 Platform Providers
`Scripts/Achievements/Providers/`

**IAchievementProvider.cs**
```csharp
public interface IAchievementProvider
{
    string ProviderName { get; }
    void Initialize(Action onReady);
    void UnlockAchievement(string id);
    void UpdateProgress(string id, int current, int max);
    bool IsUnlocked(string id);
}
```

**LocalAchievementProvider.cs** — editor/fallback
**SteamAchievementProvider.cs** — `#if UNITY_STANDALONE`
  - Facepunch.Steamworks stub (real integration is a separate task)
**GooglePlayAchievementProvider.cs** — `#if UNITY_ANDROID` (placeholder)
**AppStoreAchievementProvider.cs** — `#if UNITY_IOS` (placeholder)

Platform detection in AchievementManager.Awake():
```csharp
#if UNITY_STANDALONE && !UNITY_EDITOR
    _provider = new SteamAchievementProvider();
#elif UNITY_ANDROID
    _provider = new GooglePlayAchievementProvider();
#elif UNITY_IOS
    _provider = new AppStoreAchievementProvider();
#else
    _provider = new LocalAchievementProvider();
#endif
```

---

# SECTION 6 — END-OF-MATCH XP SYSTEM

## 6.1 MatchRewardCalculator.cs
`Scripts/Economy/Match/MatchRewardCalculator.cs`
Static utility

```csharp
public static class MatchRewardCalculator
{
    // Win:  50 base + (duration/60)*10, max 150 XP
    // Loss: 20 base + (duration/60)*5,  max 50 XP
    public static long CalculateMatchXP(bool isWinner, float matchDurationSeconds)
    {
        if (isWinner)
            return Mathf.Min(50 + Mathf.FloorToInt(matchDurationSeconds / 60f) * 10, 150);
        else
            return Mathf.Min(20 + Mathf.FloorToInt(matchDurationSeconds / 60f) * 5, 50);
    }
}
```

Add to the match-end path in the existing TurnManager.cs:
```csharp
long xp = MatchRewardCalculator.CalculateMatchXP(isLocalPlayerWinner, matchDuration);
CurrencyManager.Instance.Add(CurrencyType.XP, xp);
AchievementEvents.FireMatchWon(); // or FireMatchLost()
ChestManager.Instance.TryGrantChest(isLocalPlayerWinner);
```

---

# SECTION 7 — QUEST SYSTEM

## 7.1 QuestDefinition.cs
`Scripts/Economy/Quests/QuestDefinition.cs`
ScriptableObject

```csharp
[CreateAssetMenu(menuName = "CosmicRumble/Economy/QuestDefinition")]
public class QuestDefinition : ScriptableObject
{
    public string      questId;
    public string      displayName;
    public string      description;
    public QuestPeriod period;          // Daily, Weekly, Monthly
    public string      trackedEventKey; // matches an AchievementEvents method
    public int         targetValue;
    public long        rewardXP;
    public long        rewardGold;
    public long        rewardGem;
}
public enum QuestPeriod { Daily, Weekly, Monthly }
```

Create the quest pool ScriptableObjects under `Resources/Economy/Quests/`:

**Daily quests (pool — 3 are picked at random each day):**
| questId | displayName | description | target | XP | Gold |
|---|---|---|---|---|---|
| daily_win_1 | Win Today | Win 1 match today | 1 | 200 | 100 |
| daily_shots_5 | Shooting Practice | Fire 5 shots today | 5 | 150 | 75 |
| daily_blackhole | Black Hole Power | Use Black Hole today | 1 | 200 | 100 |
| daily_damage_500 | Deal Damage | Deal 500 damage today | 500 | 200 | 100 |
| daily_headshot | Sharpshooter | Land 1 headshot today | 1 | 150 | 75 |
| daily_play_2 | Playtime | Play 2 matches today | 2 | 150 | 75 |
| daily_ability | Skilled | Use any ability today | 1 | 150 | 75 |
| daily_planet | Planet Hunter | Destroy 1 planet today | 1 | 200 | 100 |

**Weekly quests (pool — 2 are picked per week):**
| questId | displayName | description | target | XP | Gold | Gem |
|---|---|---|---|---|---|---|
| weekly_win_10 | Weekly Champion | Win 10 matches this week | 10 | 800 | 400 | 10 |
| weekly_weapons | Weapon Master | Use every weapon this week | 5 | 600 | 300 | 5 |
| weekly_damage_5k | Destroyer | Deal 5,000 damage this week | 5000 | 700 | 350 | 5 |
| weekly_abilities | Ability Collection | Use 3 different abilities this week | 3 | 600 | 300 | 5 |

**Monthly quests (pool — 1 is picked per month):**
| questId | displayName | description | target | XP | Gold | Gem |
|---|---|---|---|---|---|---|
| monthly_play_50 | Monthly Warrior | Play 50 matches this month | 50 | 3000 | 1500 | 50 |
| monthly_damage_50k | Lord of Destruction | Deal 50,000 damage this month | 50000 | 2500 | 1200 | 30 |

## 7.2 QuestManager.cs
`Scripts/Economy/Quests/QuestManager.cs`
Singleton, DontDestroyOnLoad

- At the start of each period, pick randomly from the quest pool (Daily:3, Weekly:2, Monthly:1)
- Listen to AchievementEvents → increment the quest counters
- Grant the reward via CurrencyManager on completion
- Reset at midnight / Monday / start of month (using DateTime)
- `GetActiveDailyQuests()`, `GetActiveWeeklyQuests()`, `GetActiveMonthlyQuest()`
- Save: `Application.persistentDataPath/quests.json`

---

# SECTION 8 — LOGIN STREAK SYSTEM

## 8.1 LoginStreakManager.cs
`Scripts/Economy/Streak/LoginStreakManager.cs`
Singleton, DontDestroyOnLoad

On game launch:
- Last login date == today → already counted, return
- Last login date == yesterday → streak++
- 2+ days ago → streak = 1 (reset)

Streak reward table:
| Streak Day | XP | Gold | Gem |
|---|---|---|---|
| 1 | 10 | 25 | 0 |
| 3 | 50 | 75 | 0 |
| 7 | 150 | 200 | 5 |
| 14 | 300 | 400 | 15 |
| 30 | 500 | 750 | 30 |
| 100 | 1000 | 2000 | 100 |

Intermediate days: grant the reward of the nearest milestone that is equal or lower.

Events:
- `OnStreakUpdated`: `Action<int>` (currentStreak)
- `OnStreakRewardGranted`: `Action<int, long, long, long>` (streak, xp, gold, gem)

Save: `Application.persistentDataPath/streak.json`

---

# SECTION 9 — CHEST SYSTEM

## 9.1 ChestType.cs & ChestConfig.cs
`Scripts/Economy/Chest/`

```csharp
public enum ChestType { Common, Rare, Epic }
```

ChestConfig ScriptableObject — `Resources/Economy/ChestConfig`:
- dailyChestLimit = 3
- Drop rates: Common 65%, Rare 25%, Epic 10%
- Gold ranges: Common 50–150, Rare 200–400, Epic 500–800
- Gem: Rare +5, Epic +15
- Costume drop chance: Common 0%, Rare 5%, Epic 15%
  (Only Common/Uncommon costumes that are not already owned can drop)

## 9.2 ChestManager.cs
`Scripts/Economy/Chest/ChestManager.cs`
Singleton, DontDestroyOnLoad

- `TryGrantChest(bool isWinner)` — only wins count
  - Check the daily limit (max 3)
  - Pick the ChestType via weighted random
  - Grant the Gold + Gem + optional costume reward
  - `OnChestGranted` event: `Action<ChestType, long, long, string>` 
    (type, gold, gem, costumeId — "" if there is no costume)
- `GetTodaysChestCount() → int`
- `GetRemainingChests() → int`
- Reset the counter at midnight
- Save: `Application.persistentDataPath/chests.json`

---

# SECTION 10 — INTEGRATION WITH EXISTING SYSTEMS

Add the AchievementEvents calls to TurnManager.cs, ProjectileBase.cs, the IAbility
implementations and DestructiblePlanet.cs.
Take the real file paths from CLAUDE.md.

**TurnManager.cs — match/turn end:**
```csharp
// At match end:
bool isWinner = /* did the local player win */;
float duration = /* match duration in seconds */;
int playerCount = /* player count in the lobby */;
int totalShots = /* total shots over the match */;
int currentHP = /* the local player's HP */;

AchievementEvents.FirePlayerCountInMatch(playerCount);
if (isWinner) AchievementEvents.FireMatchWon();
else          AchievementEvents.FireMatchLost();
AchievementEvents.FireMatchCompleted(totalShots);
if (isWinner && currentHP == 1) { /* AchievementTracker catches this for SON_NEFES */ }

long xp = MatchRewardCalculator.CalculateMatchXP(isWinner, duration);
CurrencyManager.Instance.Add(CurrencyType.XP, xp);
ChestManager.Instance.TryGrantChest(isWinner);
```

**ProjectileBase.cs — when damage is applied:**
```csharp
AchievementEvents.FireDamageDealt(damageAmount);
AchievementEvents.FireShotFired(isHit: true);
// if headshot detection exists:
AchievementEvents.FireHeadshotLanded();
```

**Every IAbility implementation — when the ability is used:**
```csharp
// BlackHoleAbility.cs:
AchievementEvents.FireAbilityUsed("skill_blackhole");
// TeleportAbility.cs:
AchievementEvents.FireAbilityUsed("skill_teleport");
// ShieldAbility.cs:
AchievementEvents.FireAbilityUsed("skill_shield");
// BatHammerAbility.cs:
AchievementEvents.FireAbilityUsed("skill_bathammer");
// SuperJumpAbility.cs:
AchievementEvents.FireAbilityUsed("skill_superjump");
```

**When each weapon is fired:**
```csharp
AchievementEvents.FireWeaponUsed("weapon_pistol"); // etc.
AchievementEvents.FireShotFired(isHit: false); // at the moment of firing
```

**DestructiblePlanet.cs — when the planet is fully destroyed:**
```csharp
AchievementEvents.FirePlanetDestroyed();
```

---

# SECTION 11 — UI COMPONENTS

## Create under the Prefabs/UI/Economy/ folder:

### CurrencyHUD.prefab
- Lives on a DontDestroyOnLoad Canvas (Sort Order: 50)
- XP progress bar (current level / next level)
- Level badge (number + Prestige icon if any)
- Gold counter
- Gem counter
- CurrencyManager.OnCurrencyChanged → animated counter
- PlayerLevelManager.OnLevelUp → level-up effect

### AchievementPopup.prefab
- Canvas Sort Order: 100
- Slides in from the bottom-right corner
- Queue system: if several arrive at once, show them in sequence
- Visible for 3 seconds, then slides out
- Border color by rarity
- Achievement icon (placeholder if null)
- "Achievement Unlocked!" heading + name
- Reward summary: "+300 XP · +150 Gold"
- If there's a costume reward: a "New Costume: [name]" line

### LevelUpPopup.prefab
- Center of the screen, large format
- Animated "Level X!"
- List of items unlocked at this level (weapon, skill, costume)
- "Continue" button

### ChestPopup.prefab
- Chest type visual (placeholder)
- Opening animation
- Gold + Gem + Costume rewards
- "X/3 chests today" indicator

### StreakPopup.prefab
- Login streak notification
- Fire icon + streak count
- Reward details

### AchievementListPanel.prefab
- Fullscreen overlay
- Tabs: Combat / Statistics / Weapons / Skills / Social
- Each row: icon (placeholder) + name + description + reward + completion date
- Secret ones show "???" until unlocked
- Progress bar: for Cumulative achievements
- Summary at the top: "X/50 completed"

### QuestPanel.prefab
- Daily / Weekly / Monthly tabs
- Each quest: name + progress bar + reward + time remaining
- Completed ones green, incomplete ones normal

### CostumeShopPanel.prefab
- Grid layout: 4 columns
- Each card: placeholder art area (128x128 gray rect + "Art Coming Soon" text)
- Rarity border color
- Lock icon + condition text (if locked)
- Gold/Gem price (if not owned)
- "Equip" button (if owned)
- Filters: Rarity / Type / Theme / Unlock Method

### MainMenuEconomyWidget.cs
Persistent widget to add to the main menu Canvas:
- Top bar: [Level Badge] [━━━━XP BAR━━━━] [Gold] [Gem]
- Buttons: "Chests (X/3)" → ChestPanel | "Quests" → QuestPanel |
  "Costumes" → CostumeShopPanel | "Achievements" → AchievementListPanel

---

# SECTION 12 — SAVE/LOAD ARCHITECTURE

Each manager owns its own JSON file:
```
Application.persistentDataPath/
  currency.json      ← CurrencyManager
  progress.json      ← PlayerLevelManager
  unlocks.json       ← UnlockManager
  costumes.json      ← CostumeManager
  achievements.json  ← AchievementManager
  quests.json        ← QuestManager
  streak.json        ← LoginStreakManager
  chests.json        ← ChestManager
```

Shared save pattern for every manager:
```csharp
private void Save() =>
    File.WriteAllText(SavePath, JsonUtility.ToJson(_data, true));

private void Load()
{
    if (File.Exists(SavePath))
        _data = JsonUtility.FromJson<T>(File.ReadAllText(SavePath));
    else
        _data = new T();
}
```

---

# SECTION 13 — SCRIPT EXECUTION ORDER

Project Settings → Script Execution Order:
```
CurrencyManager      : -100
PlayerLevelManager   : -90
UnlockManager        : -80
CostumeManager       : -75
LoginStreakManager   : -70
ChestManager         : -60
QuestManager         : -50
AchievementManager   : -40
AchievementTracker   : -30
```

---

# SECTION 14 — SCENE SETUP

**MainMenu scene:**
- EconomyCore GameObject: CurrencyManager + PlayerLevelManager +
  UnlockManager + CostumeManager (all DontDestroyOnLoad)
- ProgressionServices GameObject: LoginStreakManager + ChestManager +
  QuestManager (DontDestroyOnLoad)
- AchievementServices GameObject: AchievementManager + AchievementTracker
  (DontDestroyOnLoad)
- PersistentCanvas (DontDestroyOnLoad, Sort Order 50): CurrencyHUD
- PopupCanvas (DontDestroyOnLoad, Sort Order 100): AchievementPopup +
  LevelUpPopup + ChestPopup + StreakPopup (inactive at start)
- MainMenuCanvas: MainMenuEconomyWidget + all panel prefabs

**Resources/ folder structure:**
```
Resources/
  Economy/
    LevelConfig.asset
    UnlockDatabase.asset
    CostumeDatabase.asset
    ChestConfig.asset
    Quests/
      (all QuestDefinition assets)
    Unlocks/
      (all UnlockableItem assets)
  Achievements/
    AchievementDatabase.asset
    (all AchievementDefinition assets)
  Costumes/
    (all CostumeDefinition assets)
```

---

# SECTION 15 — CLAUDE.md UPDATE

Add to CLAUDE.md once the implementation is finished:

```markdown
## Economy & Progression System

### Currencies
- XP: levels you up, cannot be purchased
- Gold: cosmetic purchases, earned through gameplay
- Gem: premium cosmetics, bought via IAP

### Level Thresholds
- Lv  1-10  → 100 XP/level  (total 1,000)
- Lv 11-50  → 500 XP/level  (total 21,000)
- Lv 51-100 → 1,000 XP/level (total 71,000)
- Lv 101+   → 2,000 XP/level (prestige, unlimited)

### Unlock Order (weapons + skills)
Default: Pistol, Shotgun, RPG
Lv 2: Bomb | Lv 4: Super Jump | Lv 6: Grenade
Lv 8: Shield | Lv 10: Black Hole, Teleport, Bat Hammer

### Costume System
- 150 costumes: 40 Common, 35 Uncommon, 35 Rare, 25 Epic, 15 Legendary
- Type: Character skin + Weapon skin (independent)
- previewSprite may be null — the UI shows a placeholder automatically
- Unlock: Level / Gold / Gem / Chest / Achievement

### Income Sources
- End of match: XP only (win 50-150, loss 20-50)
- Achievement: XP + Gold + Gem (by rarity) + optional costume
- Daily quests (3): XP + Gold
- Weekly quests (2): XP + Gold + Gem
- Monthly quest (1): XP + Gold + Gem
- Login streak: tiered by day count (grows at milestones)
- Daily chest (max 3, wins only): Gold + Gem + 5-15% costume

### Script Execution Order
CurrencyManager(-100) → PlayerLevelManager(-90) → UnlockManager(-80)
→ CostumeManager(-75) → LoginStreakManager(-70) → ChestManager(-60)
→ QuestManager(-50) → AchievementManager(-40) → AchievementTracker(-30)

### Save Files
persistentDataPath: currency, progress, unlocks, costumes,
achievements, quests, streak, chests (.json)

### Adding a New Achievement
1. Create an AchievementDefinition ScriptableObject
2. Add it to AchievementDatabase
3. Add trigger logic to AchievementTracker
4. Map the ID in the platform providers

### Adding a New Costume
1. Create a CostumeDefinition ScriptableObject (previewSprite null is ok)
2. Add it to CostumeDatabase
3. Fill in the unlock condition
```

---

# SECTION 16 — CHECKLIST

Verify in order once the implementation is finished:

**Economy Core:**
- [ ] Script execution order is configured
- [ ] CurrencyManager records Gem.Add calls via Debug.Log
- [ ] Prestige kicks in automatically after level 100
- [ ] Level is not reset on prestige (continues 101, 102...)

**Unlock:**
- [ ] Default weapons (Pistol/Shotgun/RPG) are unlocked on first launch
- [ ] Black Hole, Teleport and Bat Hammer unlock at Lv 10
- [ ] Costume purchases check both the level and the currency requirement
- [ ] Prestige costumes (l005, l009, l015) unlock at the correct level

**Achievement:**
- [ ] An already-unlocked achievement does not grant rewards again
- [ ] Secret achievements show "???" in the list
- [ ] Achievements with costume rewards (see rewardCostumeId) notify CostumeManager
- [ ] AchievementTracker unsubscribes from all events in OnDestroy

**Chest:**
- [ ] The daily 3-chest limit resets at midnight
- [ ] Only wins count (losses do not drop chests)
- [ ] Costume drops only grant costumes that are not already owned

**Login Streak:**
- [ ] The streak resets after a gap of 2+ days
- [ ] Milestone rewards are calculated correctly (intermediate days use the nearest milestone)
- [ ] No reward is granted when relaunching on the same day

**Quests:**
- [ ] Counters reset at the end of the period
- [ ] 3 daily / 2 weekly / 1 monthly quests are picked at random
- [ ] A completed quest is not triggered again

**UI:**
- [ ] Costumes with a null previewSprite show a placeholder (no errors)
- [ ] The AchievementPopup queue shows multiple achievements in sequence
- [ ] CurrencyHUD is visible in all scenes (DontDestroyOnLoad)
- [ ] LevelUpPopup correctly lists the items that were unlocked
- [ ] CostumeShop filters work (rarity, type, theme, unlock method)

**Save/Load:**
- [ ] All JSON files are created with default values on first run
- [ ] All data is preserved when the app is closed and reopened
- [ ] A corrupt JSON file falls back to defaults (try-catch)
```
