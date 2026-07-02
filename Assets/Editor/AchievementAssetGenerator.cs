using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using CosmicRumble.Achievements;

public class AchievementAssetGenerator
{
    [MenuItem("CosmicRumble/Economy/Generate Achievement Assets (Section 5)")]
    public static void GenerateAll()
    {
        var achievements = GenerateAchievementDefinitions();
        GenerateAchievementDatabase(achievements);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[AchievementAssetGenerator] Done — {achievements.Count} achievements generated.");
    }

    private static List<AchievementDefinition> GenerateAchievementDefinitions()
    {
        const string dir = "Assets/Resources/Achievements";
        EnsureDir(dir);

        // (id, name, desc, rarity, triggerType, targetValue, isSecret, rewardCostumeId)
        var defs = new List<(string id, string name, string desc, AchievementRarity r, AchievementTriggerType tt, int target, bool secret, string costume)>
        {
            // ── COMBAT (10) ──────────────────────────────────────────────────
            ("FIRST_BLOOD",     "First Blood",        "Win your first match",                                AchievementRarity.Common,    AchievementTriggerType.SingleUnlock, 1,    false, ""),
            ("VETERAN_10",      "Veteran",            "Win 10 matches",                                      AchievementRarity.Rare,      AchievementTriggerType.Cumulative,   10,   false, "r009"),
            ("SAVAS_MAKINESI",  "War Machine",        "Win 25 matches",                                      AchievementRarity.Rare,      AchievementTriggerType.Cumulative,   25,   false, "r035"),
            ("EFSANE",          "Legend",             "Win 50 matches",                                      AchievementRarity.Epic,      AchievementTriggerType.Cumulative,   50,   false, "e007"),
            ("COSMIC_100",      "Cosmic Master",      "Win 100 matches",                                     AchievementRarity.Legendary, AchievementTriggerType.Cumulative,   100,  false, "l003"),
            ("FLAWLESS",        "Flawless",           "Win a match without taking damage",                   AchievementRarity.Epic,      AchievementTriggerType.SingleUnlock, 1,    false, ""),
            ("UNDERDOG",        "Underdog",           "Win while every enemy has more HP than you",          AchievementRarity.Rare,      AchievementTriggerType.SingleUnlock, 1,    false, ""),
            ("HIZLI_BITIR",     "Speed Run",          "Win a match within 5 turns",                          AchievementRarity.Rare,      AchievementTriggerType.SingleUnlock, 1,    false, ""),
            ("SAMPIYONLAR",     "Champions League",   "Win in an 8-player lobby",                            AchievementRarity.Epic,      AchievementTriggerType.SingleUnlock, 1,    false, ""),
            ("SON_NEFES",       "Last Breath",        "Win a match at 1 HP",                                 AchievementRarity.Legendary, AchievementTriggerType.SingleUnlock, 1,    true,  ""),

            // ── STATISTICS (10) ─────────────────────────────────────────────
            ("DAMAGE_1K",       "Damage Dealer",      "Deal 1,000 total damage",                             AchievementRarity.Common,    AchievementTriggerType.Cumulative,   1000,  false, ""),
            ("DAMAGE_50K",      "Wrecking Machine",   "Deal 50,000 total damage",                            AchievementRarity.Rare,      AchievementTriggerType.Cumulative,   50000, false, ""),
            ("DAMAGE_250K",     "Atom Bomb",          "Deal 250,000 total damage",                           AchievementRarity.Epic,      AchievementTriggerType.Cumulative,   250000,false, ""),
            ("SHOTS_100",       "Trigger Happy",      "Fire 100 shots",                                      AchievementRarity.Common,    AchievementTriggerType.Cumulative,   100,   false, ""),
            ("SHOTS_1K",        "Bullet Factory",     "Fire 1,000 shots",                                    AchievementRarity.Rare,      AchievementTriggerType.Cumulative,   1000,  false, ""),
            ("TETIKCI",         "Gunslinger",         "Fire 30 shots in a single match",                     AchievementRarity.Rare,      AchievementTriggerType.Cumulative,   30,    false, ""),
            ("ISABETLI",        "Precision",          "Finish a match with 80% accuracy (min. 10 shots)",    AchievementRarity.Epic,      AchievementTriggerType.SingleUnlock, 1,    false, ""),
            ("SAGLAMDURUG",     "Tough Stance",       "Take 10,000 total damage and survive",                AchievementRarity.Rare,      AchievementTriggerType.Cumulative,   10000, false, ""),
            ("GEZEGEN_KATILI",  "Planet Killer",      "Destroy 10 planets in total",                         AchievementRarity.Epic,      AchievementTriggerType.Cumulative,   10,    false, "u034"),
            ("GALAKSI_TAMIRCISI","Galaxy Regular",    "Play 100 matches in total",                           AchievementRarity.Common,    AchievementTriggerType.Cumulative,   100,   false, ""),

            // ── WEAPONS (10) ──────────────────────────────────────────────────
            ("TABANCALI",       "Pistoleer",          "Hit 50 enemies with the pistol",                      AchievementRarity.Common,    AchievementTriggerType.Cumulative,   50,   false, ""),
            ("KESKIN_NISANCI",  "Sharpshooter",       "Land 10 headshots with the pistol",                   AchievementRarity.Rare,      AchievementTriggerType.Cumulative,   10,   false, ""),
            ("ROKETCI",         "Rocketeer",          "Deal damage to 3+ enemies with a single RPG shot",    AchievementRarity.Rare,      AchievementTriggerType.SingleUnlock, 1,    false, "r015"),
            ("PATLAMA_UZMANI",  "Demolitions Expert", "Fire 100 total shots with the RPG",                   AchievementRarity.Rare,      AchievementTriggerType.Cumulative,   100,  false, "u022"),
            ("SACMA_YAGMURU",   "Buckshot Storm",     "Land every pellet of a shotgun blast",                AchievementRarity.Epic,      AchievementTriggerType.SingleUnlock, 1,    false, ""),
            ("POMPACI",         "Pump Master",        "Hit 5 enemies in a row with the shotgun",             AchievementRarity.Rare,      AchievementTriggerType.SingleUnlock, 1,    false, ""),
            ("EL_BOMBACI",      "Grenadier",          "Hit 2+ enemies with a single grenade",                AchievementRarity.Rare,      AchievementTriggerType.SingleUnlock, 1,    false, ""),
            ("PIM_CEKICI",      "Pin Puller",         "Fire 25 shots with the grenade",                      AchievementRarity.Common,    AchievementTriggerType.Cumulative,   25,   false, ""),
            ("BOMBA_IMHA",      "Bomb Demolition",    "Destroy a planet's surface with a bomb",              AchievementRarity.Epic,      AchievementTriggerType.SingleUnlock, 1,    false, ""),
            ("TAM_CEPHANE",     "Full Arsenal",       "Use all 5 weapons in a single match",                 AchievementRarity.Epic,      AchievementTriggerType.SingleUnlock, 1,    false, ""),

            // ── SKILLS (10) ──────────────────────────────────────────────────
            ("KARA_DELIK_USTASI","Black Hole Master", "Pull in 3 enemies at once with Black Hole",          AchievementRarity.Epic,      AchievementTriggerType.SingleUnlock, 1,    false, "e012"),
            ("OLAY_UFKU",       "Event Horizon",      "Pull in 50 enemies in total with Black Hole",         AchievementRarity.Rare,      AchievementTriggerType.Cumulative,   50,   false, ""),
            ("ISINLANAN",       "Teleported",         "Teleport behind an enemy and land a hit",             AchievementRarity.Rare,      AchievementTriggerType.SingleUnlock, 1,    false, ""),
            ("KUANTUM",         "Quantum",            "Use Teleport 5 times in a single match",              AchievementRarity.Common,    AchievementTriggerType.Cumulative,   5,    false, ""),
            ("DOKUNULMAZ",      "Untouchable",        "Block 500 damage with Shield",                        AchievementRarity.Rare,      AchievementTriggerType.Cumulative,   500,  false, "e018"),
            ("KALKAN_DUVARI",   "Wall of Shields",    "Block 3 attacks with Shield",                         AchievementRarity.Rare,      AchievementTriggerType.SingleUnlock, 1,    false, ""),
            ("CEKIC_ZAMANI",    "Hammer Time",        "Knock an enemy off the planet with Bat Hammer",       AchievementRarity.Epic,      AchievementTriggerType.SingleUnlock, 1,    false, ""),
            ("HOME_RUN",        "Home Run",           "Send an enemy hit by Bat Hammer into another enemy",  AchievementRarity.Legendary, AchievementTriggerType.SingleUnlock, 1,    false, "l014"),
            ("SUPER_KAHRAMAN",  "Super Hero",         "Land on an enemy with Super Jump to deal damage",     AchievementRarity.Rare,      AchievementTriggerType.SingleUnlock, 1,    false, ""),
            ("YORUNGE",         "Orbit",              "Change planets with Super Jump and take a shot",      AchievementRarity.Common,    AchievementTriggerType.SingleUnlock, 1,    false, ""),

            // ── SOCIAL (10) ─────────────────────────────────────────────────
            ("SOSYAL_KELEBEK",  "Social Butterfly",   "Play a match with 8 different players",               AchievementRarity.Common,    AchievementTriggerType.Cumulative,   8,    false, "r023"),
            ("REKABETCI",       "Competitive",        "Finish top 3 in a ranked match",                      AchievementRarity.Rare,      AchievementTriggerType.SingleUnlock, 1,    false, ""),
            ("KOZMIK_AVCI",     "Cosmic Hunter",      "Reach the top 10 on the leaderboard",                 AchievementRarity.Epic,      AchievementTriggerType.SingleUnlock, 1,    false, ""),
            ("BIR_NUMARA",      "Number One",         "Reach the top of the leaderboard",                    AchievementRarity.Legendary, AchievementTriggerType.SingleUnlock, 1,    true,  ""),
            ("DUELLO_SAMPIYONU","Duel Champion",      "Get 10 wins in 1v1 mode",                             AchievementRarity.Rare,      AchievementTriggerType.Cumulative,   10,   false, ""),
            ("OGRETMEN",        "Mentor",             "Guide a new player through the tutorial",             AchievementRarity.Common,    AchievementTriggerType.SingleUnlock, 1,    false, ""),
            ("KOZMIK_EKIP",     "Cosmic Crew",        "Play 5 matches with the same 3 people",               AchievementRarity.Rare,      AchievementTriggerType.Cumulative,   5,    false, ""),
            ("INTIKAM",         "Revenge",            "Defeat whoever killed you in the next match",         AchievementRarity.Rare,      AchievementTriggerType.SingleUnlock, 1,    false, ""),
            ("HERKESE_MEYDAN",  "Challenge Everyone", "Deal damage to 7 different players in the same match",AchievementRarity.Epic,      AchievementTriggerType.SingleUnlock, 1,    false, ""),
            ("GALAKSININ_EFSANESI","Legend of the Galaxy","Complete all 49 other achievements",               AchievementRarity.Legendary, AchievementTriggerType.SingleUnlock, 1,    false, "l007"),
        };

        // Reward table by rarity
        long XpFor(AchievementRarity r)   => r switch { AchievementRarity.Common => 100, AchievementRarity.Rare => 300, AchievementRarity.Epic => 600, _ => 1500 };
        long GoldFor(AchievementRarity r) => r switch { AchievementRarity.Common => 50,  AchievementRarity.Rare => 150, AchievementRarity.Epic => 400, _ => 1000 };
        long GemFor(AchievementRarity r)  => r switch { AchievementRarity.Epic => 5, AchievementRarity.Legendary => 20, _ => 0 };

        var created = new List<AchievementDefinition>();
        foreach (var d in defs)
        {
            string path = $"{dir}/{d.id}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<AchievementDefinition>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<AchievementDefinition>();
                AssetDatabase.CreateAsset(asset, path);
            }
            asset.achievementId   = d.id;
            asset.displayName     = d.name;
            asset.description     = d.desc;
            asset.rarity          = d.r;
            asset.triggerType     = d.tt;
            asset.targetValue     = d.target;
            asset.isSecret        = d.secret;
            asset.rewardCostumeId = d.costume;
            asset.rewardXP        = XpFor(d.r);
            asset.rewardGold      = GoldFor(d.r);
            asset.rewardGem       = GemFor(d.r);
            EditorUtility.SetDirty(asset);
            created.Add(asset);
        }
        Debug.Log($"[AchievementAssetGenerator] {created.Count} AchievementDefinition assets ready.");
        return created;
    }

    private static void GenerateAchievementDatabase(List<AchievementDefinition> achievements)
    {
        const string dir  = "Assets/Resources/Achievements";
        const string path = dir + "/AchievementDatabase.asset";
        EnsureDir(dir);

        var db = AssetDatabase.LoadAssetAtPath<AchievementDatabase>(path);
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<AchievementDatabase>();
            AssetDatabase.CreateAsset(db, path);
        }
        db.allAchievements = achievements;
        EditorUtility.SetDirty(db);
        Debug.Log("[AchievementAssetGenerator] AchievementDatabase.asset ready.");
    }

    private static void EnsureDir(string assetPath)
    {
        string[] parts = assetPath.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
