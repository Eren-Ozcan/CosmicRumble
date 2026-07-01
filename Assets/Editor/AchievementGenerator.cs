using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using CosmicRumble.Achievements;

public class AchievementGenerator : EditorWindow
{
    private Vector2 _scroll;
    private string  _status = "Ready.";
    private bool    _lastSuccess;

    [MenuItem("CosmicRumble/Generate Achievements")]
    public static void ShowWindow()
    {
        var win = GetWindow<AchievementGenerator>("Achievement Generator");
        win.minSize = new Vector2(420, 280);
    }

    private void OnGUI()
    {
        GUILayout.Space(10);
        EditorGUILayout.LabelField("CosmicRumble — Achievement Generator", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Creates 50 ScriptableObject assets in Resources/Achievements/",
                                   EditorStyles.miniLabel);
        GUILayout.Space(6);

        if (GUILayout.Button("Generate All Achievements", GUILayout.Height(36)))
            RunGeneration();

        GUILayout.Space(8);

        var style = new GUIStyle(EditorStyles.helpBox)
        {
            wordWrap = true,
            richText = true,
            fontSize = 11
        };
        var colour = _lastSuccess ? new Color(0.2f, 0.8f, 0.3f) : Color.white;
        GUI.color = colour;
        EditorGUILayout.LabelField(_status, style, GUILayout.ExpandHeight(true));
        GUI.color = Color.white;
    }

    // ─── Main entry ───────────────────────────────────────────────────────────

    private void RunGeneration()
    {
        _lastSuccess = false;
        _status      = "Working…";
        Repaint();

        try
        {
            AssetDatabase.StartAssetEditing();
            EnsureDir("Assets/Resources/Achievements");

            var defs = BuildDefinitions();
            var created = new List<AchievementDefinition>();

            for (int i = 0; i < defs.Count; i++)
            {
                var d = defs[i];
                EditorUtility.DisplayProgressBar(
                    "Generating Achievements",
                    $"({i + 1}/{defs.Count})  {d.displayName}",
                    (float)(i + 1) / defs.Count);

                created.Add(WriteAsset(d));
            }

            EditorUtility.DisplayProgressBar("Generating Achievements", "Updating AchievementDatabase…", 1f);
            UpdateDatabase(created);

            _lastSuccess = true;
            _status      = $"<b>Done!</b>  {created.Count} achievements created / updated.\n" +
                           "Assets/Resources/Achievements/AchievementDatabase.asset refreshed.";
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Repaint();
        }
    }

    // ─── Asset writer ─────────────────────────────────────────────────────────

    private static AchievementDefinition WriteAsset(AchievementDefinition src)
    {
        string path  = $"Assets/Resources/Achievements/{src.achievementId}.asset";
        var    asset = AssetDatabase.LoadAssetAtPath<AchievementDefinition>(path);
        if (asset == null)
        {
            asset = CreateInstance<AchievementDefinition>();
            AssetDatabase.CreateAsset(asset, path);
        }

        asset.achievementId   = src.achievementId;
        asset.displayName     = src.displayName;
        asset.description     = src.description;
        asset.rarity          = src.rarity;
        asset.triggerType     = src.triggerType;
        asset.targetValue     = src.targetValue;
        asset.isSecret        = src.isSecret;
        asset.rewardXP        = src.rewardXP;
        asset.rewardGold      = src.rewardGold;
        asset.rewardGem       = src.rewardGem;
        asset.rewardCostumeId = src.rewardCostumeId;
        EditorUtility.SetDirty(asset);
        return asset;
    }

    // ─── Database updater ─────────────────────────────────────────────────────

    private static void UpdateDatabase(List<AchievementDefinition> achievements)
    {
        const string path = "Assets/Resources/Achievements/AchievementDatabase.asset";
        var db = AssetDatabase.LoadAssetAtPath<AchievementDatabase>(path);
        if (db == null)
        {
            db = CreateInstance<AchievementDatabase>();
            AssetDatabase.CreateAsset(db, path);
        }

        // Merge: keep existing entries not in our 50, then add/replace ours.
        var newIds = new HashSet<string>();
        foreach (var a in achievements) newIds.Add(a.achievementId);

        var merged = new List<AchievementDefinition>();
        foreach (var existing in db.allAchievements)
            if (existing != null && !newIds.Contains(existing.achievementId))
                merged.Add(existing);
        merged.AddRange(achievements);

        db.allAchievements = merged;
        EditorUtility.SetDirty(db);
    }

    // ─── Directory helper ─────────────────────────────────────────────────────

    private static void EnsureDir(string assetPath)
    {
        string[] parts   = assetPath.Split('/');
        string   current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    // ─── Achievement data ─────────────────────────────────────────────────────

    private static AchievementDefinition Make(
        string id, string name, string desc,
        AchievementRarity rarity, AchievementTriggerType trigger,
        int target, long xp, bool secret = false)
    {
        var a = CreateInstance<AchievementDefinition>();
        a.achievementId = id;
        a.displayName   = name;
        a.description   = desc;
        a.rarity        = rarity;
        a.triggerType   = trigger;
        a.targetValue   = target;
        a.rewardXP      = xp;
        a.isSecret      = secret;

        // Derived gold/gem from rarity
        a.rewardGold = rarity switch
        {
            AchievementRarity.Common    => 50,
            AchievementRarity.Rare      => 150,
            AchievementRarity.Epic      => 400,
            AchievementRarity.Legendary => 1000,
            _                           => 0
        };
        a.rewardGem = rarity switch
        {
            AchievementRarity.Epic      => 5,
            AchievementRarity.Legendary => 20,
            _                           => 0
        };
        return a;
    }

    private static List<AchievementDefinition> BuildDefinitions()
    {
        // Shorthand aliases
        const AchievementRarity C  = AchievementRarity.Common;
        const AchievementRarity R  = AchievementRarity.Rare;
        const AchievementRarity E  = AchievementRarity.Epic;
        const AchievementRarity L  = AchievementRarity.Legendary;
        const AchievementTriggerType SU = AchievementTriggerType.SingleUnlock;
        const AchievementTriggerType CU = AchievementTriggerType.Cumulative;
        const AchievementTriggerType SA = AchievementTriggerType.SpecialAction;

        return new List<AchievementDefinition>
        {
            // ── COMBAT (10) ───────────────────────────────────────────────────
            Make("FIRST_BLOOD",    "First Blood",        "Deal damage for first time",               C, SU, 1,     100),
            Make("HEADSHOT_MASTER","Headshot Master",    "10 headshots",                             R, CU, 10,    200),
            Make("DESTROYER",      "Destroyer",          "Destroy 5 planets",                        R, CU, 5,     300),
            Make("FLAWLESS",       "Flawless Victory",   "Win without taking damage",                E, SA, 1,     500),
            Make("SNIPER",         "Sniper",             "Hit target from max distance",             R, SA, 1,     200),
            Make("BERSERKER",      "Berserker",          "Deal 1000 damage in one match",            R, CU, 1000,  300),
            Make("SURVIVOR",       "Survivor",           "Win with 1 HP remaining",                  E, SA, 1,     400),
            Make("COMBO",          "Combo King",         "Hit 3 enemies with one shot",              E, SA, 1,     500),
            Make("PACIFIST",       "Pacifist",           "Win without using weapons",                L, SA, 1,    1000),
            Make("COMEBACK",       "Comeback",           "Win after being last alive",               E, SA, 1,     500),

            // ── STATISTICS (10) ───────────────────────────────────────────────
            Make("PLAY_10",        "Veteran",            "Play 10 matches",                          C, CU, 10,    100),
            Make("PLAY_50",        "Seasoned",           "Play 50 matches",                          R, CU, 50,    200),
            Make("PLAY_100",       "Elite",              "Play 100 matches",                         E, CU, 100,   500),
            Make("WIN_10",         "Winner",             "Win 10 matches",                           R, CU, 10,    200),
            Make("WIN_50",         "Champion",           "Win 50 matches",                           E, CU, 50,    500),
            Make("DAMAGE_10K",     "Damage Dealer",      "Deal 10000 total damage",                  R, CU, 10000, 200),
            Make("DAMAGE_100K",    "Destroyer Supreme",  "Deal 100000 damage",                       E, CU, 100000,500),
            Make("SHOTS_100",      "Trigger Happy",      "Fire 100 shots",                           C, CU, 100,   100),
            Make("SHOTS_1000",     "Marksman",           "Fire 1000 shots",                          R, CU, 1000,  300),
            Make("HIZLI_BITIR",    "Speed Run",          "Win in under 5 minutes",                   R, SA, 1,     300),

            // ── WEAPONS (10) ──────────────────────────────────────────────────
            Make("PISTOL_MASTER",  "Pistol Master",      "Kill 10 with pistol",                      R, CU, 10,    200),
            Make("SHOTGUN_MASTER", "Shotgun Master",     "Kill 10 with shotgun",                     R, CU, 10,    200),
            Make("RPG_MASTER",     "RPG Master",         "Kill 10 with RPG",                         R, CU, 10,    200),
            Make("GRENADE_MASTER", "Grenade Master",     "Kill 10 with grenade",                     R, CU, 10,    200),
            Make("BOMB_MASTER",    "Bomb Master",        "Kill 10 with bomb",                        R, CU, 10,    200),
            Make("BLACKHOLE_MASTER","Black Hole Master", "Use black hole 20 times",                  R, CU, 20,    300),
            Make("TELEPORT_MASTER","Teleport Master",    "Teleport 20 times",                        R, CU, 20,    300),
            Make("HAMMER_MASTER",  "Hammer Master",      "Kill 10 with hammer",                      R, CU, 10,    200),
            Make("SHIELD_MASTER",  "Shield Master",      "Block 10 hits with shield",                R, CU, 10,    300),
            Make("ALL_WEAPONS",    "Arsenal",            "Kill with every weapon",                   L, SA, 1,    1000),

            // ── SKILLS (10) ───────────────────────────────────────────────────
            Make("JUMPER",         "Jumper",             "Jump 100 times",                           C, CU, 100,   100),
            Make("SUPER_JUMPER",   "Super Jumper",       "Super jump 20 times",                      R, CU, 20,    200),
            Make("ABILITY_100",    "Skilled",            "Use abilities 100 times",                  R, CU, 100,   200),
            Make("COMBO_ABILITY",  "Combo Ability",      "Use 3 abilities in one turn",              R, SA, 1,     300),
            Make("PLANET_SURFER",  "Planet Surfer",      "Visit both planets in one match",          R, SA, 1,     200),
            Make("GRAVITY_MASTER", "Gravity Master",     "Use gravity to kill enemy",                R, SA, 1,     300),
            Make("ESCAPE_ARTIST",  "Escape Artist",      "Survive 5 near-death situations",          R, CU, 5,     300),
            Make("TACTICIAN",      "Tactician",          "Win using only 3 shots",                   E, SA, 1,     500),
            Make("MINIMALIST",     "Minimalist",         "Win using only 1 weapon type",             E, SA, 1,     400),
            Make("PERFECT_AIM",    "Perfect Aim",        "Hit 10 shots in a row",                    E, CU, 10,    500),

            // ── SOCIAL (10) ───────────────────────────────────────────────────
            Make("FIRST_MATCH",    "Welcome!",           "Complete first match",                     C, SU, 1,     50),
            Make("DAILY_1",        "Daily Player",       "Complete 1 daily quest",                   C, CU, 1,     100),
            Make("DAILY_10",       "Quest Hunter",       "Complete 10 daily quests",                 R, CU, 10,    300),
            Make("WEEKLY_1",       "Weekly Warrior",     "Complete 1 weekly quest",                  R, CU, 1,     200),
            Make("STREAK_3",       "On Fire",            "3 day login streak",                       C, CU, 3,     100),
            Make("STREAK_7",       "Dedicated",          "7 day login streak",                       R, CU, 7,     200),
            Make("STREAK_30",      "Addicted",           "30 day login streak",                      E, CU, 30,    500),
            Make("CHEST_10",       "Collector",          "Open 10 chests",                           R, CU, 10,    200),
            Make("LEVEL_10",       "Level 10",           "Reach level 10",                           R, CU, 10,    300),
            Make("LEVEL_50",       "Level 50",           "Reach level 50",                           L, CU, 50,   1000),
        };
    }
}
