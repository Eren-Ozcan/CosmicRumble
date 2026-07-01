# CosmicRumble — Master Implementation Prompt
# Achievement + Economy + Level + Costume Systems
# Bu prompt Claude Code'a direkt verilecek şekilde hazırlanmıştır.

---

## BAŞLAMADAN ÖNCE

1. CLAUDE.md dosyasını oku ve mevcut proje yapısını anla
2. Mevcut dosyaları incele: TurnManager, ProjectileBase, IAbility implementasyonları,
   DestructiblePlanet — gerçek dosya yollarını tespit et
3. Aşağıdaki sistemleri sırayla implement et; her bölüm bir öncekine bağımlıdır
4. Her sistem tamamlandıktan sonra CLAUDE.md'i güncelle

---

# BÖLÜM 1 — PARA BİRİMLERİ VE CURRENCY SİSTEMİ

## 1.1 CurrencyType.cs
`Scripts/Economy/Core/CurrencyType.cs`

```csharp
public enum CurrencyType { XP, Gold, Gem }
```

## 1.2 CurrencyManager.cs
`Scripts/Economy/Core/CurrencyManager.cs`
Singleton, DontDestroyOnLoad

- `Add(CurrencyType type, long amount)` — ekler, event fırlatır
- `Spend(CurrencyType type, long amount) → bool` — yeterliyse düşer
- `Get(CurrencyType type) → long` — mevcut bakiye
- `OnCurrencyChanged` event: `Action<CurrencyType, long>` (type, newBalance)
- Save: `Application.persistentDataPath/currency.json`
- Gem log kuralı: her Gem.Add çağrısı Debug.Log ile kaydedilir
  (IAP audit trail için)

---

# BÖLÜM 2 — LEVEL & PRESTIGE SİSTEMİ

## 2.1 LevelConfig.cs
`Scripts/Economy/Core/LevelConfig.cs`
ScriptableObject — `Resources/Economy/LevelConfig`

XP eşikleri (level başına gereken XP):
- Lv   1–10  → 100 XP   (kümülatif toplam: 1.000)
- Lv  11–50  → 500 XP   (kümülatif toplam: 21.000)
- Lv  51–100 → 1.000 XP (kümülatif toplam: 71.000)
- Lv 101+    → 2.000 XP (prestige dahil, sonsuz)

```csharp
public int GetXPForLevel(int level)
{
    if (level <= 10)  return 100;
    if (level <= 50)  return 500;
    if (level <= 100) return 1000;
    return 2000;
}
public long GetTotalXPForLevel(int level) { /* kümülatif */ }
public int  GetLevelFromTotalXP(long totalXP) { /* ters hesap */ }
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

- CurrencyManager.OnCurrencyChanged(XP) subscribe eder
- `CheckLevelUp()` — birden fazla level atlayabilir
- `OnLevelUp` event: `Action<int, int>` (oldLevel, newLevel)
- `OnPrestige` event: `Action<int>` (newPrestigeRank)
- Level 100 tamamlanınca bir sonraki XP'de otomatik prestige başlar
- Prestige'de level 101, 102... diye devam eder (sıfırlanmaz)
- `GetProgress() → PlayerProgressData`
- Save: `Application.persistentDataPath/progress.json`

---

# BÖLÜM 3 — UNLOCK SİSTEMİ

## 3.1 UnlockableItem.cs
`Scripts/Economy/Unlocks/UnlockableItem.cs`
ScriptableObject

```csharp
[CreateAssetMenu(menuName = "CosmicRumble/Economy/UnlockableItem")]
public class UnlockableItem : ScriptableObject
{
    public string        itemId;
    public string        displayName;
    public Sprite        icon;          // null olabilir, UI buna göre placeholder gösterir
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
    public bool canUnlock;        // tüm şartlar tamam mı
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

Aşağıdaki item'ları ScriptableObject olarak oluştur ve database'e ekle:

### Silahlar (Weapons):
| itemId           | displayName      | unlockMethod | requiredLevel | isDefault |
|------------------|------------------|--------------|---------------|-----------|
| weapon_pistol    | Tabanca          | Default      | —             | true      |
| weapon_shotgun   | Pompalı Tüfek    | Default      | —             | true      |
| weapon_rpg       | Roket Fırlatıcı  | Default      | —             | true      |
| weapon_bomb      | Bomba            | ByLevel      | 2             | false     |
| weapon_grenade   | El Bombası       | ByLevel      | 6             | false     |

### Skill'ler:
| itemId           | displayName      | unlockMethod | requiredLevel |
|------------------|------------------|--------------|---------------|
| skill_superjump  | Super Jump       | ByLevel      | 4             |
| skill_shield     | Shield           | ByLevel      | 8             |
| skill_blackhole  | Black Hole       | ByLevel      | 10            |
| skill_teleport   | Teleport         | ByLevel      | 10            |
| skill_bathammer  | Bat Hammer       | ByLevel      | 10            |

### Kozmetikler (level + gold):
| itemId                | displayName       | requiredLevel | goldCost |
|-----------------------|-------------------|---------------|----------|
| skin_cosmic_blue      | Kozmik Mavi       | 15            | 500      |
| skin_fire_red         | Ateş Kırmızısı    | 20            | 800      |
| skin_void_dark        | Void Karası       | 30            | 1200     |
| skin_golden_legend    | Altın Efsane      | 45            | 2000     |
| skin_neon_pulse       | Neon Nabzı        | 60            | 3000     |
| skin_prestige_shadow  | Prestij Gölgesi   | 80            | 5000     |
| skin_cosmic_master    | Kozmik Usta       | 100           | 0 (100 Gem) |

## 3.4 UnlockManager.cs
`Scripts/Economy/Unlocks/UnlockManager.cs`
Singleton, DontDestroyOnLoad

- `IsUnlocked(string itemId) → bool`
- `CanUnlock(string itemId) → UnlockCheckResult`
- `TryUnlock(string itemId) → bool` (currency düşer, kaydeder)
- `GetAllUnlocked() → List<UnlockableItem>`
- `OnItemUnlocked` event: `Action<UnlockableItem>`
- PlayerLevelManager.OnLevelUp subscribe → level unlock'larını otomatik kontrol et
- Başlangıçta tüm isDefault==true item'ları unlock et
- Save: `Application.persistentDataPath/unlocks.json`

---

# BÖLÜM 4 — KOSTÜM SİSTEMİ (150 KOSTÜM)

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
    public Sprite        previewSprite;   // null-safe: UI placeholder gösterir
    public CostumeType   costumeType;
    public CostumeRarity rarity;
    public CostumeTheme  theme;
    public CostumeUnlock unlockMethod;

    // Unlock koşulları (unlockMethod'a göre dolu olan kullanılır)
    public int    requiredLevel;
    public long   goldCost;
    public long   gemCost;
    public string requiredAchievementId;
    public string unlockDescription;     // UI'da gösterilecek koşul metni
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

Aşağıdaki 150 kostümü ScriptableObject olarak oluştur ve CostumeDatabase'e ekle.
`previewSprite` alanı null bırakılacak — UI sistemi otomatik placeholder gösterir.

### COMMON (40 kostüm)
| costumeId | displayName | costumeType | theme | unlockMethod | Koşul |
|---|---|---|---|---|---|
| c001 | Gri Asker | Character | Other | Default | Başlangıç |
| c002 | Standart Mavi | Character | Other | Default | Başlangıç |
| c003 | Kırmızı Savaşçı | Character | Other | ByLevel | Lv 3 |
| c004 | Yeşil Kamuflaj | Character | Nature | ByLevel | Lv 5 |
| c005 | Sarı Fırtına | Character | Other | ByGold | 200 Gold |
| c006 | Turuncu Kor | Character | Fire | ByGold | 200 Gold |
| c007 | Mor Gece | Character | Dark | ByGold | 250 Gold |
| c008 | Beyaz Kar | Character | Ice | ByGold | 250 Gold |
| c009 | Kahve Toprak | Character | Nature | ByLevel | Lv 7 |
| c010 | Gök Mavisi | Character | Space | ByLevel | Lv 9 |
| c011 | Çelik Gri | Weapon | Mech | ByGold | 150 Gold |
| c012 | Pas Kahvesi | Weapon | Mech | ByGold | 150 Gold |
| c013 | Orman Yeşili | Weapon | Nature | ByLevel | Lv 6 |
| c014 | Lav Kırmızısı | Weapon | Fire | ByGold | 175 Gold |
| c015 | Buz Mavisi | Weapon | Ice | ByGold | 175 Gold |
| c016 | Gece Siyahı | Weapon | Dark | ByLevel | Lv 8 |
| c017 | Güneş Sarısı | Weapon | Other | ByGold | 200 Gold |
| c018 | Mercan Pembesi | Character | Other | ByGold | 200 Gold |
| c019 | Deniz Tealı | Character | Other | ByLevel | Lv 11 |
| c020 | Lavanta | Character | Other | ByGold | 225 Gold |
| c021 | Bakır Parlak | Weapon | Mech | ByGold | 175 Gold |
| c022 | Çöl Kumu | Character | Nature | ByLevel | Lv 13 |
| c023 | Fıstık Yeşili | Character | Nature | ByGold | 200 Gold |
| c024 | Deniz Köpüğü | Weapon | Ice | ByGold | 150 Gold |
| c025 | Sis Grisi | Character | Dark | ByLevel | Lv 15 |
| c026 | Gün Batımı | Character | Fire | ByGold | 250 Gold |
| c027 | Yıldız Tozu | Weapon | Space | ByLevel | Lv 12 |
| c028 | Okyanusun Dibi | Weapon | Other | ByChest | Common Sandık |
| c029 | Kireç Beyazı | Character | Other | ByChest | Common Sandık |
| c030 | Antrasit | Character | Dark | ByChest | Common Sandık |
| c031 | Nane Yeşili | Weapon | Nature | ByGold | 175 Gold |
| c032 | Pembe Şeker | Character | Other | ByGold | 200 Gold |
| c033 | Gök Gürültüsü | Weapon | Other | ByLevel | Lv 14 |
| c034 | Altın Sarısı | Weapon | Other | ByGold | 225 Gold |
| c035 | Zümrüt | Character | Nature | ByLevel | Lv 16 |
| c036 | Kirpi Kahvesi | Character | Nature | ByChest | Common Sandık |
| c037 | Titan Grisi | Weapon | Mech | ByChest | Common Sandık |
| c038 | Bordo | Character | Dark | ByGold | 200 Gold |
| c039 | Kobalt | Weapon | Space | ByGold | 200 Gold |
| c040 | Çivit Mavisi | Character | Other | ByLevel | Lv 18 |

### UNCOMMON (35 kostüm)
| costumeId | displayName | costumeType | theme | unlockMethod | Koşul |
|---|---|---|---|---|---|
| u001 | Orman Savaşçısı | Character | Nature | ByLevel | Lv 20 |
| u002 | Buz Büyücüsü | Character | Ice | ByLevel | Lv 22 |
| u003 | Alev Dansçısı | Character | Fire | ByLevel | Lv 24 |
| u004 | Gece Gözcüsü | Character | Dark | ByGold | 500 Gold |
| u005 | Şimşek Koşucusu | Character | Other | ByGold | 500 Gold |
| u006 | Kum Fırtınası | Character | Nature | ByLevel | Lv 26 |
| u007 | Derin Uzay | Character | Space | ByLevel | Lv 28 |
| u008 | Demir Yumruk | Character | Mech | ByGold | 550 Gold |
| u009 | Rüzgar Ruhu | Character | Nature | ByGold | 550 Gold |
| u010 | Kozmik Mor | Character | Space | ByLevel | Lv 30 |
| u011 | Ejder Dişi | Weapon | Fantasy | ByGold | 500 Gold |
| u012 | Uzay Tüfeği | Weapon | Space | ByLevel | Lv 25 |
| u013 | Buz Kılıcı | Weapon | Ice | ByGold | 500 Gold |
| u014 | Alev Mızrağı | Weapon | Fire | ByLevel | Lv 27 |
| u015 | Gölge Bıçağı | Weapon | Dark | ByGold | 525 Gold |
| u016 | Sis Tabancası | Weapon | Dark | ByChest | Rare Sandık |
| u017 | Plazma Tüp | Weapon | Cyber | ByLevel | Lv 29 |
| u018 | Doğa Kalkanı | Weapon | Nature | ByGold | 500 Gold |
| u019 | Şimşek Topu | Weapon | Other | ByChest | Rare Sandık |
| u020 | Demir Kalkan | Weapon | Mech | ByGold | 550 Gold |
| u021 | Kristal Savaşçı | Character | Ice | ByAchievement | achievement_ice_master |
| u022 | Volkan Adamı | Character | Fire | ByAchievement | achievement_patlama_uzmani |
| u023 | Siber Ninja | Character | Cyber | ByLevel | Lv 32 |
| u024 | Taş Golem | Character | Nature | ByLevel | Lv 34 |
| u025 | Neon Ceket | Character | Cyber | ByGold | 600 Gold |
| u026 | Köpük Denizci | Character | Other | ByChest | Rare Sandık |
| u027 | Bozkır Eri | Character | Nature | ByLevel | Lv 36 |
| u028 | Gümüş Şövalye | Character | Fantasy | ByGold | 600 Gold |
| u029 | Mavi Timsah | Weapon | Nature | ByChest | Rare Sandık |
| u030 | Kor Bıçağı | Weapon | Fire | ByLevel | Lv 31 |
| u031 | Hologram Silah | Weapon | Cyber | ByGold | 575 Gold |
| u032 | Çelik Ejder | Weapon | Mech | ByLevel | Lv 33 |
| u033 | Kristal Bomba | Weapon | Ice | ByGold | 550 Gold |
| u034 | Kök Dokusu | Weapon | Nature | ByAchievement | PLANET_KILLER |
| u035 | Fırtına Yelkeni | Character | Other | ByGold | 600 Gold |

### RARE (35 kostüm)
| costumeId | displayName | costumeType | theme | unlockMethod | Koşul |
|---|---|---|---|---|---|
| r001 | Galaksi Gezgini | Character | Space | ByLevel | Lv 40 |
| r002 | Kara Şövalye | Character | Dark | ByLevel | Lv 42 |
| r003 | Neon Samuray | Character | Cyber | ByLevel | Lv 44 |
| r004 | Ejder Avcısı | Character | Fantasy | ByLevel | Lv 45 |
| r005 | Buz Tanrısı | Character | Ice | ByGold | 1200 Gold |
| r006 | Lav Devi | Character | Fire | ByGold | 1200 Gold |
| r007 | Kuantum Zırhı | Character | Cyber | ByLevel | Lv 48 |
| r008 | Orman Tanrısı | Character | Nature | ByGold | 1300 Gold |
| r009 | Karanlık Büyücü | Character | Dark | ByAchievement | VETERAN_10 |
| r010 | Meteor Savaşçısı | Character | Space | ByLevel | Lv 50 |
| r011 | Plazma Tüfek | Weapon | Cyber | ByLevel | Lv 41 |
| r012 | Ejder Alevi | Weapon | Fantasy | ByGold | 1100 Gold |
| r013 | Kara Delik Topu | Weapon | Space | ByLevel | Lv 43 |
| r014 | Buz Kalkanı | Weapon | Ice | ByGold | 1100 Gold |
| r015 | Kor Bombası | Weapon | Fire | ByAchievement | PATLAMA_UZMANI |
| r016 | Nano Bıçak | Weapon | Cyber | ByGold | 1200 Gold |
| r017 | Rün Mızrağı | Weapon | Fantasy | ByLevel | Lv 46 |
| r018 | Gölge Oku | Weapon | Dark | ByChest | Epic Sandık |
| r019 | Zümrüt Ejder | Weapon | Fantasy | ByGold | 1250 Gold |
| r020 | Yıldız Kılıcı | Weapon | Space | ByLevel | Lv 47 |
| r021 | Titanium Golem | Character | Mech | ByLevel | Lv 52 |
| r022 | Işık Hızı | Character | Space | ByGold | 1400 Gold |
| r023 | Deniz Canavarı | Character | Nature | ByAchievement | SOSYAL_KELEBEK |
| r024 | Fırtına Tanrısı | Character | Myth | ByLevel | Lv 54 |
| r025 | Kızıl Şaman | Character | Myth | ByGold | 1400 Gold |
| r026 | Siber Samurai | Character | Cyber | ByLevel | Lv 56 |
| r027 | Biyonik Savaşçı | Character | Mech | ByGold | 1500 Gold |
| r028 | Vorteks Tüfek | Weapon | Space | ByChest | Epic Sandık |
| r029 | Şaman Asası | Weapon | Myth | ByLevel | Lv 49 |
| r030 | Titan Çekici | Weapon | Mech | ByGold | 1300 Gold |
| r031 | Rüzgar Bıçağı | Weapon | Nature | ByAchievement | CEVRECI |
| r032 | Kristal Asa | Weapon | Fantasy | ByLevel | Lv 51 |
| r033 | Lazer Tüfek | Weapon | Cyber | ByGold | 1350 Gold |
| r034 | Karanlık Rün | Weapon | Dark | ByLevel | Lv 53 |
| r035 | Mitoloji Okçusu | Character | Myth | ByAchievement | SAVAS_MAKINESI |

### EPIC (25 kostüm)
| costumeId | displayName | costumeType | theme | unlockMethod | Koşul |
|---|---|---|---|---|---|
| e001 | Nebula Savaşçısı | Character | Space | ByLevel | Lv 60 |
| e002 | Ejder Lordu | Character | Fantasy | ByLevel | Lv 63 |
| e003 | Siber Tanrı | Character | Cyber | ByGem | 50 Gem |
| e004 | Ölüm Ruhu | Character | Dark | ByLevel | Lv 66 |
| e005 | Volkan Tanrısı | Character | Fire | ByGem | 60 Gem |
| e006 | Buz Fırtınası | Character | Ice | ByLevel | Lv 70 |
| e007 | Ormantanrı | Character | Nature | ByAchievement | EFSANE |
| e008 | Titan Zırhı | Character | Mech | ByGem | 70 Gem |
| e009 | Olimpos Tanrısı | Character | Myth | ByLevel | Lv 73 |
| e010 | Kuantum Gölgesi | Character | Cyber | ByGem | 75 Gem |
| e011 | Galaktik İmparator | Character | Space | ByLevel | Lv 76 |
| e012 | Kadim Ejder | Character | Fantasy | ByAchievement | KARA_DELIK_USTASI |
| e013 | Neon İblis | Character | Dark | ByGem | 80 Gem |
| e014 | Plazma Tanrısı | Weapon | Cyber | ByLevel | Lv 61 |
| e015 | Ejder Nefesi | Weapon | Fantasy | ByGem | 50 Gem |
| e016 | Karanlık Yıldız | Weapon | Dark | ByLevel | Lv 64 |
| e017 | Volkan Topu | Weapon | Fire | ByGem | 55 Gem |
| e018 | Buz Kristali | Weapon | Ice | ByAchievement | DOKUNULMAZ |
| e019 | Nano Swarm | Weapon | Mech | ByLevel | Lv 68 |
| e020 | Rün Patlaması | Weapon | Fantasy | ByGem | 65 Gem |
| e021 | Nebula Bombası | Weapon | Space | ByLevel | Lv 71 |
| e022 | Titan Lazer | Weapon | Mech | ByGem | 70 Gem |
| e023 | Mitoloji Zırhı | Character | Myth | ByAchievement | FIRTINA_TANRISI |
| e024 | Kristal Golem | Character | Ice | ByGem | 75 Gem |
| e025 | Karga Kral | Character | Dark | ByLevel | Lv 80 |

### LEGENDARY (15 kostüm)
| costumeId | displayName | costumeType | theme | unlockMethod | Koşul |
|---|---|---|---|---|---|
| l001 | Kozmik Efendi | Character | Space | ByLevel | Lv 100 |
| l002 | Ejder İmparatoru | Character | Fantasy | ByGem | 200 Gem |
| l003 | Karanlık Tanrı | Character | Dark | ByAchievement | COSMIC_100 |
| l004 | Kıyamet Lordu | Character | Dark | ByGem | 250 Gem |
| l005 | Zaman Efendisi | Character | Myth | ByLevel | Prestige 1 (Lv 101) |
| l006 | Evren Savaşçısı | Character | Space | ByGem | 300 Gem |
| l007 | Kadim Dev | Character | Myth | ByAchievement | GALAKSININ_EFSANESI |
| l008 | Biyonik Tanrı | Character | Mech | ByGem | 250 Gem |
| l009 | Fenix Savaşçısı | Character | Fire | ByLevel | Prestige 2 (Lv 102) |
| l010 | Kozmik Yıkıcı | Weapon | Space | ByLevel | Lv 100 |
| l011 | Tanrı Kılıcı | Weapon | Myth | ByGem | 200 Gem |
| l012 | Ejder Kalbi | Weapon | Fantasy | ByAchievement | EJDER_AVCI |
| l013 | Kara Delik Topu X | Weapon | Space | ByGem | 250 Gem |
| l014 | Kıyamet Çekici | Weapon | Dark | ByAchievement | HOME_RUN |
| l015 | Yaratıcı Gücü | Weapon | Myth | ByLevel | Prestige 3 (Lv 103) |

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
- Achievement unlock'larını AchievementManager.OnAchievementUnlocked'tan dinle
- Save: `Application.persistentDataPath/costumes.json`

---

# BÖLÜM 5 — ACHIEVEMENT SİSTEMİ

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

    // Ödüller
    public long rewardXP;
    public long rewardGold;
    public long rewardGem;

    // Kostüm ödülü (opsiyonel)
    public string rewardCostumeId;  // boşsa kostüm ödülü yok
}

public enum AchievementRarity      { Common, Rare, Epic, Legendary }
public enum AchievementTriggerType { SingleUnlock, Cumulative, SpecialAction }
```

Ödül tablosu rarity'e göre:
| Rarity    | XP    | Gold  | Gem |
|-----------|-------|-------|-----|
| Common    | 100   | 50    | 0   |
| Rare      | 300   | 150   | 0   |
| Epic      | 600   | 400   | 5   |
| Legendary | 1500  | 1000  | 20  |

## 5.2 AchievementDatabase.cs
`Scripts/Achievements/Core/AchievementDatabase.cs`
ScriptableObject — `Resources/Achievements/AchievementDatabase`

Aşağıdaki 50 achievement'ı ScriptableObject olarak oluştur ve database'e ekle:

### SAVAŞ (10)
| achievementId | displayName | description | rarity | triggerType | targetValue | rewardCostumeId |
|---|---|---|---|---|---|---|
| FIRST_BLOOD | İlk Kan | İlk galibiyetini kazan | Common | SingleUnlock | 1 | — |
| VETERAN_10 | Veteran | 10 maç kazan | Rare | Cumulative | 10 | r009 |
| SAVAS_MAKINESI | Savaş Makinesi | 25 maç kazan | Rare | Cumulative | 25 | r035 |
| EFSANE | Efsane | 50 maç kazan | Epic | Cumulative | 50 | e007 |
| COSMIC_100 | Kozmik Efendi | 100 maç kazan | Legendary | Cumulative | 100 | l003 |
| FLAWLESS | Flawless | Hiç hasar almadan bir maç kazan | Epic | SingleUnlock | 1 | — |
| UNDERDOG | Underdog | Tüm düşmanlar fazla HP'deyken kazan | Rare | SingleUnlock | 1 | — |
| HIZLI_BITIR | Hızlı Bitir | 5 turda bir maç kazan | Rare | SingleUnlock | 1 | — |
| SAMPIYONLAR | Şampiyonlar Ligi | 8 kişilik lobbyde kazan | Epic | SingleUnlock | 1 | — |
| SON_NEFES | Son Nefes | 1 HP'de maçı kazan | Legendary | SingleUnlock | 1 | — |

### İSTATİSTİK (10)
| achievementId | displayName | description | rarity | triggerType | targetValue | rewardCostumeId |
|---|---|---|---|---|---|---|
| DAMAGE_1K | Hasarcı | Toplam 1.000 hasar ver | Common | Cumulative | 1000 | — |
| DAMAGE_50K | Yıkım Makinesi | Toplam 50.000 hasar ver | Rare | Cumulative | 50000 | — |
| DAMAGE_250K | Atom Bombası | Toplam 250.000 hasar ver | Epic | Cumulative | 250000 | — |
| SHOTS_100 | Çılgın Atıcı | 100 atış yap | Common | Cumulative | 100 | — |
| SHOTS_1K | Mermi Fabrikası | 1.000 atış yap | Rare | Cumulative | 1000 | — |
| TETIKCI | Tetikçi | Tek maçta 30 atış yap | Rare | Cumulative | 30 | — |
| ISABETLI | İsabetli | %80 isabet oranıyla bitir (min 10 atış) | Epic | SingleUnlock | 1 | — |
| SAGLAMDURUG | Sağlam Duruş | Toplam 10.000 hasar al hayatta kal | Rare | Cumulative | 10000 | — |
| GEZEGEN_KATILI | Gezegen Katili | Toplamda 10 gezegen yok et | Epic | Cumulative | 10 | u034 |
| GALAKSI_TAMIRCISI | Galaksi Tamircisi | Toplamda 100 maç oyna | Common | Cumulative | 100 | — |

### SİLAH (10)
| achievementId | displayName | description | rarity | triggerType | targetValue | rewardCostumeId |
|---|---|---|---|---|---|---|
| TABANCALI | Tabancalı | Tabancayla 50 düşman vur | Common | Cumulative | 50 | — |
| KESKIN_NISANCI | Keskin Nişancı | Tabancayla 10 headshot yap | Rare | Cumulative | 10 | — |
| ROKETCI | Roketçi | RPG ile tek atışta 3+ düşmana hasar ver | Rare | SingleUnlock | 1 | r015 |
| PATLAMA_UZMANI | Patlama Uzmanı | RPG ile toplam 100 atış yap | Rare | Cumulative | 100 | u022 |
| SAÇMA_YAGMURU | Saçma Yağmuru | Shotgun tüm pellet'leri isabet ettir | Epic | SingleUnlock | 1 | — |
| POMPACI | Pompacı | Shotgun ile 5 düşmanı arka arkaya vur | Rare | SingleUnlock | 1 | — |
| EL_BOMBACI | El Bombacı | El bombasıyla 2+ düşmanı tek vur | Rare | SingleUnlock | 1 | — |
| PIM_CEKICI | Pim Çekici | El bombası ile 25 atış yap | Common | Cumulative | 25 | — |
| BOMBA_IMHA | Bomba İmha | Bomba ile gezegen yüzeyini yok et | Epic | SingleUnlock | 1 | — |
| TAM_CEPHANE | Tam Cephane | Bir maçta 5 silahın tamamını kullan | Epic | SingleUnlock | 1 | — |

### SKİLL (10)
| achievementId | displayName | description | rarity | triggerType | targetValue | rewardCostumeId |
|---|---|---|---|---|---|---|
| KARA_DELIK_USTASI | Kara Delik Ustası | Black Hole ile 3 düşmanı tek çek | Epic | SingleUnlock | 1 | e012 |
| OLAY_UFKU | Olay Ufku | Black Hole ile 50 düşman çek | Rare | Cumulative | 50 | — |
| ISINLANAN | Işınlanan | Teleport ile düşmanın arkasına geç ve vur | Rare | SingleUnlock | 1 | — |
| KUANTUM | Kuantum | Teleport'u tek maçta 5 kez kullan | Common | Cumulative | 5 | — |
| DOKUNULMAZ | Dokunulmaz | Shield ile 500 hasar blokla | Rare | Cumulative | 500 | e018 |
| KALKAN_DUVARI | Kalkan Duvarı | Shield ile 3 saldırıyı blokla | Rare | SingleUnlock | 1 | — |
| CEKIC_ZAMANI | Çekiç Zamanı | Bat Hammer ile düşmanı gezegen dışına fırlat | Epic | SingleUnlock | 1 | — |
| HOME_RUN | Home Run | Bat Hammer vurduğun düşman başkasına çarpsın | Legendary | SingleUnlock | 1 | l014 |
| SUPER_KAHRAMAN | Süper Kahraman | Super Jump ile düşmanın üstüne inerek hasar ver | Rare | SingleUnlock | 1 | — |
| YÖRÜNGE | Yörünge | Super Jump ile gezegen değiştirerek atış yap | Common | SingleUnlock | 1 | — |

### SOSYAL (10)
| achievementId | displayName | description | rarity | triggerType | targetValue | rewardCostumeId |
|---|---|---|---|---|---|---|
| SOSYAL_KELEBEK | Sosyal Kelebek | 8 farklı oyuncuyla maç oyna | Common | Cumulative | 8 | r023 |
| REKABETCI | Rekabetçi | Sıralama maçında ilk 3'e gir | Rare | SingleUnlock | 1 | — |
| KOZMIK_AVCI | Kozmik Avcı | Leaderboard top 10'a gir | Epic | SingleUnlock | 1 | — |
| BIR_NUMARA | Bir Numara | Leaderboard zirvesine çık | Legendary | SingleUnlock | 1 | — |
| DUELLO_SAMPIYONU | Düello Şampiyonu | 1v1 modunda 10 galibiyet al | Rare | Cumulative | 10 | — |
| OGRETMEN | Öğretmen | Yeni oyuncuyu tutorial'dan geçir | Common | SingleUnlock | 1 | — |
| KOZMIK_EKIP | Kozmik Ekip | Aynı 3 kişiyle 5 maç oyna | Rare | Cumulative | 5 | — |
| INTIKAM | İntikam | Seni öldüreni bir sonraki maçta yenebilir | Rare | SingleUnlock | 1 | — |
| HERKESE_MEYDAN | Herkese Meydan | Aynı maçta 7 farklı oyuncuya hasar ver | Epic | SingleUnlock | 1 | — |
| GALAKSININ_EFSANESI | Galaksinin Efsanesi | Tüm 49 achievement'ı tamamla | Legendary | SingleUnlock | 1 | l007 |

## 5.3 AchievementEvents.cs
`Scripts/Achievements/Core/AchievementEvents.cs`
Static event bus — hiçbir sistem başka sisteme doğrudan referans vermez

```csharp
public static class AchievementEvents
{
    public static event Action OnMatchWon;
    public static event Action OnMatchLost;
    public static event Action<int>    OnDamageDealt;       // hasar miktarı
    public static event Action<int>    OnDamageTaken;
    public static event Action<string> OnWeaponUsed;        // weapon itemId
    public static event Action<string> OnAbilityUsed;       // skill itemId
    public static event Action         OnHeadshotLanded;
    public static event Action<int>    OnMatchCompleted;    // toplam atış sayısı
    public static event Action         OnPlanetDestroyed;
    public static event Action<bool>   OnShotFired;         // bool: isHit
    public static event Action<int>    OnTurnCompleted;     // tur sayısı
    public static event Action<int>    OnPlayerCountInMatch;// lobbydeki oyuncu sayısı
    public static event Action<string> OnPlayerDefeated;    // yenilen oyuncunun id'si

    // Fire metodları (null-safe)
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
Singleton, DontDestroyOnLoad — event'leri dinler, sayaçları tutar

Takip edilecek kümülatif sayaçlar:
- totalMatchesWon, totalMatchesPlayed
- totalDamageDealt, totalDamageTaken
- totalShotsFired, totalShotsHit
- totalHeadshots, totalPlanetsDestroyed
- weaponsUsedInCurrentMatch (HashSet) → TAM_CEPHANE için
- uniqueOpponentsPlayed (HashSet)
- blackHolePullsInCurrentAbility
- shieldBlockedDamage
- consecutiveShotgunVictims

OnDestroy'da tüm event aboneliklerini temizle.

## 5.5 AchievementManager.cs
`Scripts/Achievements/Core/AchievementManager.cs`
Singleton, DontDestroyOnLoad

- `UnlockAchievement(string id)` — tek public unlock noktası
  - Daha önce unlock edilmişse çık
  - Ödülleri ver: CurrencyManager.Add(XP, Gold, Gem)
  - Kostüm ödülü varsa CostumeManager'a bildir
  - `OnAchievementUnlocked` event: `Action<AchievementDefinition>` fırlat
- `UpdateProgress(string id, int value)` — cumulative için
- `IsUnlocked(string id) → bool`
- Save: `Application.persistentDataPath/achievements.json`

## 5.6 Platform Provider'lar
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
  - Facepunch.Steamworks stub (gerçek entegrasyon ayrı görev)
**GooglePlayAchievementProvider.cs** — `#if UNITY_ANDROID` (placeholder)
**AppStoreAchievementProvider.cs** — `#if UNITY_IOS` (placeholder)

Platform detection AchievementManager.Awake():
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

# BÖLÜM 6 — MAÇ SONU XP SİSTEMİ

## 6.1 MatchRewardCalculator.cs
`Scripts/Economy/Match/MatchRewardCalculator.cs`
Static utility

```csharp
public static class MatchRewardCalculator
{
    // Galibiyet: 50 base + (süre/60)*10, max 150 XP
    // Mağlubiyet: 20 base + (süre/60)*5, max 50 XP
    public static long CalculateMatchXP(bool isWinner, float matchDurationSeconds)
    {
        if (isWinner)
            return Mathf.Min(50 + Mathf.FloorToInt(matchDurationSeconds / 60f) * 10, 150);
        else
            return Mathf.Min(20 + Mathf.FloorToInt(matchDurationSeconds / 60f) * 5, 50);
    }
}
```

Mevcut TurnManager.cs içinde maç bitişine ekle:
```csharp
long xp = MatchRewardCalculator.CalculateMatchXP(isLocalPlayerWinner, matchDuration);
CurrencyManager.Instance.Add(CurrencyType.XP, xp);
AchievementEvents.FireMatchWon(); // veya FireMatchLost()
ChestManager.Instance.TryGrantChest(isLocalPlayerWinner);
```

---

# BÖLÜM 7 — GÖREV SİSTEMİ

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
    public string      trackedEventKey; // AchievementEvents metoduyla eşleşir
    public int         targetValue;
    public long        rewardXP;
    public long        rewardGold;
    public long        rewardGem;
}
public enum QuestPeriod { Daily, Weekly, Monthly }
```

Görev havuzu ScriptableObject'leri `Resources/Economy/Quests/` altında oluştur:

**Günlük görevler (havuz — her gün 3 tanesi rastgele seçilir):**
| questId | displayName | description | target | XP | Gold |
|---|---|---|---|---|---|
| daily_win_1 | Bugün Kazan | Bugün 1 maç kazan | 1 | 200 | 100 |
| daily_shots_5 | Atış Antrenmanı | Bugün 5 atış yap | 5 | 150 | 75 |
| daily_blackhole | Kara Güç | Bugün Black Hole kullan | 1 | 200 | 100 |
| daily_damage_500 | Hasar Ver | Bugün 500 hasar ver | 500 | 200 | 100 |
| daily_headshot | Nişancı | Bugün 1 headshot yap | 1 | 150 | 75 |
| daily_play_2 | Oyun Zamanı | Bugün 2 maç oyna | 2 | 150 | 75 |
| daily_ability | Yetenekli | Bugün herhangi bir ability kullan | 1 | 150 | 75 |
| daily_planet | Gezegen Avcısı | Bugün 1 gezegen yok et | 1 | 200 | 100 |

**Haftalık görevler (havuz — haftada 2 tanesi seçilir):**
| questId | displayName | description | target | XP | Gold | Gem |
|---|---|---|---|---|---|---|
| weekly_win_10 | Haftalık Şampiyon | Bu hafta 10 maç kazan | 10 | 800 | 400 | 10 |
| weekly_weapons | Silah Ustası | Bu hafta tüm silahları kullan | 5 | 600 | 300 | 5 |
| weekly_damage_5k | Yıkıcı | Bu hafta 5.000 hasar ver | 5000 | 700 | 350 | 5 |
| weekly_abilities | Ability Koleksiyonu | Bu hafta 3 farklı ability kullan | 3 | 600 | 300 | 5 |

**Aylık görevler (havuz — ayda 1 tanesi seçilir):**
| questId | displayName | description | target | XP | Gold | Gem |
|---|---|---|---|---|---|---|
| monthly_play_50 | Aylık Savaşçı | Bu ay 50 maç oyna | 50 | 3000 | 1500 | 50 |
| monthly_damage_50k | Yıkım Efendisi | Bu ay 50.000 hasar ver | 50000 | 2500 | 1200 | 30 |

## 7.2 QuestManager.cs
`Scripts/Economy/Quests/QuestManager.cs`
Singleton, DontDestroyOnLoad

- Period başında görev havuzundan rastgele seç (Daily:3, Weekly:2, Monthly:1)
- AchievementEvents'i dinle → görev sayaçlarını artır
- Tamamlanınca CurrencyManager'a ödül ver
- Gece yarısı/Pazartesi/ay başı reset (DateTime ile)
- `GetActiveDailyQuests()`, `GetActiveWeeklyQuests()`, `GetActiveMonthlyQuest()`
- Save: `Application.persistentDataPath/quests.json`

---

# BÖLÜM 8 — LOGIN STREAK SİSTEMİ

## 8.1 LoginStreakManager.cs
`Scripts/Economy/Streak/LoginStreakManager.cs`
Singleton, DontDestroyOnLoad

Oyun açılışında:
- Son login tarihi == bugün → zaten sayıldı, çık
- Son login tarihi == dün → streak++
- 2+ gün önce → streak = 1 (sıfırla)

Streak ödül tablosu:
| Streak Günü | XP | Gold | Gem |
|---|---|---|---|
| 1 | 10 | 25 | 0 |
| 3 | 50 | 75 | 0 |
| 7 | 150 | 200 | 5 |
| 14 | 300 | 400 | 15 |
| 30 | 500 | 750 | 30 |
| 100 | 1000 | 2000 | 100 |

Ara günler: eşit veya küçük en yakın milestone ödülünü ver.

Events:
- `OnStreakUpdated`: `Action<int>` (currentStreak)
- `OnStreakRewardGranted`: `Action<int, long, long, long>` (streak, xp, gold, gem)

Save: `Application.persistentDataPath/streak.json`

---

# BÖLÜM 9 — SANDIK SİSTEMİ

## 9.1 ChestType.cs & ChestConfig.cs
`Scripts/Economy/Chest/`

```csharp
public enum ChestType { Common, Rare, Epic }
```

ChestConfig ScriptableObject — `Resources/Economy/ChestConfig`:
- dailyChestLimit = 3
- Drop oranları: Common %65, Rare %25, Epic %10
- Gold aralıkları: Common 50–150, Rare 200–400, Epic 500–800
- Gem: Rare +5, Epic +15
- Kostüm drop şansı: Common %0, Rare %5, Epic %15
  (Sadece sahip olunmayan Common/Uncommon kostümler düşer)

## 9.2 ChestManager.cs
`Scripts/Economy/Chest/ChestManager.cs`
Singleton, DontDestroyOnLoad

- `TryGrantChest(bool isWinner)` — sadece galibiyet sayılır
  - Gün içi limit kontrol (max 3)
  - Ağırlıklı random ile ChestType seç
  - Gold + Gem + opsiyonel kostüm ödülünü ver
  - `OnChestGranted` event: `Action<ChestType, long, long, string>` 
    (type, gold, gem, costumeId — costume boşsa "")
- `GetTodaysChestCount() → int`
- `GetRemainingChests() → int`
- Gece yarısı sayacı sıfırla
- Save: `Application.persistentDataPath/chests.json`

---

# BÖLÜM 10 — MEVCUT SİSTEMLERE ENTEGRASYON

TurnManager.cs, ProjectileBase.cs, IAbility implementasyonları ve
DestructiblePlanet.cs'e AchievementEvents çağrılarını ekle.
Gerçek dosya yollarını CLAUDE.md'den al.

**TurnManager.cs — maç/tur bitişi:**
```csharp
// Maç bitişinde:
bool isWinner = /* local player kazandı mı */;
float duration = /* maç süresi saniye */;
int playerCount = /* lobbydeki oyuncu sayısı */;
int totalShots = /* maç boyunca toplam atış */;
int currentHP = /* local player'ın HP'si */;

AchievementEvents.FirePlayerCountInMatch(playerCount);
if (isWinner) AchievementEvents.FireMatchWon();
else          AchievementEvents.FireMatchLost();
AchievementEvents.FireMatchCompleted(totalShots);
if (isWinner && currentHP == 1) { /* SON_NEFES için AchievementTracker yakalar */ }

long xp = MatchRewardCalculator.CalculateMatchXP(isWinner, duration);
CurrencyManager.Instance.Add(CurrencyType.XP, xp);
ChestManager.Instance.TryGrantChest(isWinner);
```

**ProjectileBase.cs — hasar uygulanınca:**
```csharp
AchievementEvents.FireDamageDealt(damageAmount);
AchievementEvents.FireShotFired(isHit: true);
// headshot tespiti varsa:
AchievementEvents.FireHeadshotLanded();
```

**Her IAbility implementasyonu — ability kullanılınca:**
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

**Her silah ateşlenince:**
```csharp
AchievementEvents.FireWeaponUsed("weapon_pistol"); // vb.
AchievementEvents.FireShotFired(isHit: false); // atış anında
```

**DestructiblePlanet.cs — gezegen tamamen yok olunca:**
```csharp
AchievementEvents.FirePlanetDestroyed();
```

---

# BÖLÜM 11 — UI BİLEŞENLERİ

## Prefabs/UI/Economy/ klasörüne oluştur:

### CurrencyHUD.prefab
- DontDestroyOnLoad Canvas (Sort Order: 50) üzerinde yaşar
- XP progress bar (mevcut level / sonraki level)
- Level badge (sayı + Prestige ikonu varsa)
- Gold sayacı
- Gem sayacı
- CurrencyManager.OnCurrencyChanged → animasyonlu counter
- PlayerLevelManager.OnLevelUp → level-up efekti

### AchievementPopup.prefab
- Canvas Sort Order: 100
- Sağ alt köşeden slide-in
- Queue sistemi: aynı anda birden fazla gelirse sırayla göster
- 3 saniye görünür, slide-out
- Rarity'e göre border rengi
- Achievement icon (null ise placeholder)
- "Achievement Kazanıldı!" başlığı + isim
- Ödül özeti: "+300 XP · +150 Gold"
- Kostüm ödülü varsa: "Yeni Kostüm: [isim]" satırı

### LevelUpPopup.prefab
- Ekran ortası, büyük format
- "Level X!" animasyonlu
- Bu levelde açılan item'lar listesi (silah, skill, kostüm)
- "Devam" butonu

### ChestPopup.prefab
- Sandık tipi görseli (placeholder)
- Açılış animasyonu
- Gold + Gem + Kostüm ödülleri
- "Bugün X/3 sandık" göstergesi

### StreakPopup.prefab
- Login streak bildirimi
- Ateş ikonu + streak sayısı
- Ödül detayı

### AchievementListPanel.prefab
- Fullscreen overlay
- Tab'lar: Savaş / İstatistik / Silah / Skill / Sosyal
- Her satır: icon (placeholder) + isim + açıklama + ödül + tamamlanma tarihi
- Secret olanlar unlock edilene kadar "???" 
- Progress bar: Cumulative achievement'lar için
- Üst özet: "X/50 tamamlandı"

### QuestPanel.prefab
- Günlük / Haftalık / Aylık sekmeleri
- Her görev: isim + progress bar + ödül + kalan süre
- Tamamlananlar yeşil, tamamlanmayanlar normal

### CostumeShopPanel.prefab
- Grid layout: 4 sütun
- Her kart: placeholder görsel alanı (128x128 gri rect + "Görsel Yakında" metni)
- Rarity border rengi
- Kilit ikonu + koşul metni (kilitliyse)
- Gold/Gem fiyatı (sahip olunmuyorsa)
- "Giy" butonu (sahip olunuyorsa)
- Filtre: Rarity / Tip / Tema / Açılış Yöntemi

### MainMenuEconomyWidget.cs
Ana menü Canvas'ına eklenecek persistent widget:
- Üst bar: [Level Badge] [━━━━XP BAR━━━━] [Gold] [Gem]
- Butonlar: "Sandık (X/3)" → ChestPanel | "Görevler" → QuestPanel |
  "Kostümler" → CostumeShopPanel | "Başarımlar" → AchievementListPanel

---

# BÖLÜM 12 — SAVE/LOAD MİMARİSİ

Her manager kendi JSON dosyasını yönetir:
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

Ortak save pattern her manager için:
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

# BÖLÜM 13 — SCRIPT EXECUTION ORDER

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

# BÖLÜM 14 — SCENE SETUP

**MainMenu scene:**
- EconomyCore GameObject: CurrencyManager + PlayerLevelManager +
  UnlockManager + CostumeManager (hepsi DontDestroyOnLoad)
- ProgressionServices GameObject: LoginStreakManager + ChestManager +
  QuestManager (DontDestroyOnLoad)
- AchievementServices GameObject: AchievementManager + AchievementTracker
  (DontDestroyOnLoad)
- PersistentCanvas (DontDestroyOnLoad, Sort Order 50): CurrencyHUD
- PopupCanvas (DontDestroyOnLoad, Sort Order 100): AchievementPopup +
  LevelUpPopup + ChestPopup + StreakPopup (başlangıçta inactive)
- MainMenuCanvas: MainMenuEconomyWidget + tüm panel prefab'ları

**Resources/ klasör yapısı:**
```
Resources/
  Economy/
    LevelConfig.asset
    UnlockDatabase.asset
    CostumeDatabase.asset
    ChestConfig.asset
    Quests/
      (tüm QuestDefinition asset'leri)
    Unlocks/
      (tüm UnlockableItem asset'leri)
  Achievements/
    AchievementDatabase.asset
    (tüm AchievementDefinition asset'leri)
  Costumes/
    (tüm CostumeDefinition asset'leri)
```

---

# BÖLÜM 15 — CLAUDE.md GÜNCELLEMESİ

Implementasyon bittikten sonra CLAUDE.md'e ekle:

```markdown
## Economy & Progression System

### Para Birimleri
- XP: Level atlatır, satın alınamaz
- Gold: Kozmetik alımı, oynanışla kazanılır
- Gem: Premium kozmetik, IAP ile alınır

### Level Eşikleri
- Lv  1-10  → 100 XP/level  (toplam 1.000)
- Lv 11-50  → 500 XP/level  (toplam 21.000)
- Lv 51-100 → 1.000 XP/level (toplam 71.000)
- Lv 101+   → 2.000 XP/level (prestige, sonsuz)

### Unlock Sırası (silah + skill)
Default: Tabanca, Pompalı, RPG
Lv 2: Bomba | Lv 4: Super Jump | Lv 6: El Bombası
Lv 8: Shield | Lv 10: Black Hole, Teleport, Bat Hammer

### Kostüm Sistemi
- 150 kostüm: 40 Common, 35 Uncommon, 35 Rare, 25 Epic, 15 Legendary
- Tip: Character skin + Weapon skin (bağımsız)
- previewSprite null olabilir — UI otomatik placeholder gösterir
- Açılış: Level / Gold / Gem / Chest / Achievement

### Gelir Kaynakları
- Maç sonu: sadece XP (galibiyet 50-150, mağlubiyet 20-50)
- Achievement: XP + Gold + Gem (rarity'e göre) + opsiyonel kostüm
- Günlük görev (3): XP + Gold
- Haftalık görev (2): XP + Gold + Gem
- Aylık görev (1): XP + Gold + Gem
- Login streak: gün sayısına göre kademeli (milestone'larda büyür)
- Günlük sandık (max 3, sadece galibiyet): Gold + Gem + %5-15 kostüm

### Script Execution Order
CurrencyManager(-100) → PlayerLevelManager(-90) → UnlockManager(-80)
→ CostumeManager(-75) → LoginStreakManager(-70) → ChestManager(-60)
→ QuestManager(-50) → AchievementManager(-40) → AchievementTracker(-30)

### Save Dosyaları
persistentDataPath: currency, progress, unlocks, costumes,
achievements, quests, streak, chests (.json)

### Yeni Achievement Ekleme
1. AchievementDefinition ScriptableObject oluştur
2. AchievementDatabase'e ekle
3. AchievementTracker'a trigger logic ekle
4. Platform provider'larda ID mapping yap

### Yeni Kostüm Ekleme
1. CostumeDefinition ScriptableObject oluştur (previewSprite null ok)
2. CostumeDatabase'e ekle
3. Unlock koşulunu doldur
```

---

# BÖLÜM 16 — KONTROL LİSTESİ

Implementasyon bittikten sonra sırayla doğrula:

**Economy Core:**
- [ ] Script execution order ayarlandı
- [ ] CurrencyManager Gem.Add çağrılarını Debug.Log ile kaydediyor
- [ ] Level 100 sonrası prestige otomatik devreye giriyor
- [ ] Prestige'de level sıfırlanmıyor (101, 102... devam ediyor)

**Unlock:**
- [ ] Default silahlar (Tabanca/Pompalı/RPG) ilk açılışta unlock ediliyor
- [ ] Lv 10'da Black Hole, Teleport, Bat Hammer açılıyor
- [ ] Kostüm alımında hem level hem currency şartı kontrol ediliyor
- [ ] Prestige kostümleri (l005, l009, l015) doğru level'da açılıyor

**Achievement:**
- [ ] Daha önce unlock edilmiş achievement tekrar ödül vermiyor
- [ ] Secret achievement'lar listede "???" gösteriyor
- [ ] Kostüm ödüllü achievement'lar (bkz. rewardCostumeId) CostumeManager'a bildiriyor
- [ ] AchievementTracker OnDestroy'da tüm event'lerden ayrılıyor

**Sandık:**
- [ ] Günlük 3 sandık limiti gece yarısı sıfırlanıyor
- [ ] Sadece galibiyet sayılıyor (mağlubiyet sandık düşürmüyor)
- [ ] Kostüm drop'u sadece sahip olunmayan kostümleri veriyor

**Login Streak:**
- [ ] 2+ gün boşlukta streak sıfırlanıyor
- [ ] Milestone ödülleri doğru hesaplanıyor (ara günler en yakın milestone)
- [ ] Aynı gün tekrar açılışta ödül verilmiyor

**Görev:**
- [ ] Period bitiminde sayaçlar sıfırlanıyor
- [ ] Günlük 3 / haftalık 2 / aylık 1 görev rastgele seçiliyor
- [ ] Tamamlanan görev tekrar tetiklenmiyor

**UI:**
- [ ] previewSprite null olan kostümler placeholder gösteriyor (hata vermiyor)
- [ ] AchievementPopup queue birden fazla achievement'ı sırayla gösteriyor
- [ ] CurrencyHUD tüm sahnelerde görünür (DontDestroyOnLoad)
- [ ] LevelUpPopup açılan item'ların listesini doğru gösteriyor
- [ ] CostumeShop filtreler çalışıyor (rarity, tip, tema, açılış yöntemi)

**Save/Load:**
- [ ] Tüm JSON dosyaları ilk çalıştırmada default değerlerle oluşuyor
- [ ] Uygulama kapatılıp açıldığında tüm veriler korunuyor
- [ ] Bozuk JSON dosyası default'a düşüyor (try-catch)
```
