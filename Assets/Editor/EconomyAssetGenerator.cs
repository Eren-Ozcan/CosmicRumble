using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using CosmicRumble.Economy;

public class EconomyAssetGenerator
{
    // ─── Menu Entry ──────────────────────────────────────────────────────────

    [MenuItem("CosmicRumble/Economy/Generate All Economy Assets (Sections 1-3)")]
    public static void GenerateAll()
    {
        GenerateLevelConfig();
        var unlockItems = GenerateUnlockableItems();
        GenerateUnlockDatabase(unlockItems);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[EconomyAssetGenerator] All Section 1-3 assets generated.");
    }

    // ─── LevelConfig ─────────────────────────────────────────────────────────

    private static void GenerateLevelConfig()
    {
        const string dir  = "Assets/Resources/Economy";
        const string path = dir + "/LevelConfig.asset";
        EnsureDir(dir);

        if (!File.Exists(Path.Combine(Application.dataPath, "../" + path).Replace('/', Path.DirectorySeparatorChar)))
        {
            var cfg = ScriptableObject.CreateInstance<LevelConfig>();
            AssetDatabase.CreateAsset(cfg, path);
            Debug.Log("[EconomyAssetGenerator] Created LevelConfig.asset");
        }
        else
        {
            Debug.Log("[EconomyAssetGenerator] LevelConfig.asset already exists, skipped.");
        }
    }

    // ─── UnlockableItems ─────────────────────────────────────────────────────

    private static List<UnlockableItem> GenerateUnlockableItems()
    {
        const string dir = "Assets/Resources/Economy/Unlocks";
        EnsureDir(dir);

        var defs = new List<(string id, string name, UnlockableType type, UnlockMethod method, int lvl, long gold, long gem, bool isDefault)>
        {
            // Weapons
            ("weapon_pistol",  "Tabanca",          UnlockableType.Weapon, UnlockMethod.Default, 0,  0,   0, true),
            ("weapon_shotgun", "Pompalı Tüfek",     UnlockableType.Weapon, UnlockMethod.Default, 0,  0,   0, true),
            ("weapon_rpg",     "Roket Fırlatıcı",   UnlockableType.Weapon, UnlockMethod.Default, 0,  0,   0, true),
            ("weapon_bomb",    "Bomba",              UnlockableType.Weapon, UnlockMethod.ByLevel, 2,  0,   0, false),
            ("weapon_grenade", "El Bombası",         UnlockableType.Weapon, UnlockMethod.ByLevel, 6,  0,   0, false),
            // Skills
            ("skill_superjump","Super Jump",         UnlockableType.Skill,  UnlockMethod.ByLevel, 4,  0,   0, false),
            ("skill_shield",   "Shield",             UnlockableType.Skill,  UnlockMethod.ByLevel, 8,  0,   0, false),
            ("skill_blackhole","Black Hole",         UnlockableType.Skill,  UnlockMethod.ByLevel, 10, 0,   0, false),
            ("skill_teleport", "Teleport",           UnlockableType.Skill,  UnlockMethod.ByLevel, 10, 0,   0, false),
            ("skill_bathammer","Bat Hammer",         UnlockableType.Skill,  UnlockMethod.ByLevel, 10, 0,   0, false),
            // Cosmetics
            ("skin_cosmic_blue",     "Kozmik Mavi",     UnlockableType.Cosmetic, UnlockMethod.ByLevel, 15,  500,  0, false),
            ("skin_fire_red",        "Ateş Kırmızısı",  UnlockableType.Cosmetic, UnlockMethod.ByLevel, 20,  800,  0, false),
            ("skin_void_dark",       "Void Karası",      UnlockableType.Cosmetic, UnlockMethod.ByLevel, 30, 1200,  0, false),
            ("skin_golden_legend",   "Altın Efsane",     UnlockableType.Cosmetic, UnlockMethod.ByLevel, 45, 2000,  0, false),
            ("skin_neon_pulse",      "Neon Nabzı",       UnlockableType.Cosmetic, UnlockMethod.ByLevel, 60, 3000,  0, false),
            ("skin_prestige_shadow", "Prestij Gölgesi",  UnlockableType.Cosmetic, UnlockMethod.ByLevel, 80, 5000,  0, false),
            ("skin_cosmic_master",   "Kozmik Usta",      UnlockableType.Cosmetic, UnlockMethod.ByGem,  100,    0, 100, false),
        };

        var created = new List<UnlockableItem>();
        foreach (var d in defs)
        {
            string path = $"{dir}/{d.id}.asset";
            UnlockableItem item = AssetDatabase.LoadAssetAtPath<UnlockableItem>(path);
            if (item == null)
            {
                item = ScriptableObject.CreateInstance<UnlockableItem>();
                AssetDatabase.CreateAsset(item, path);
            }
            item.itemId        = d.id;
            item.displayName   = d.name;
            item.type          = d.type;
            item.unlockMethod  = d.method;
            item.requiredLevel = d.lvl;
            item.goldCost      = d.gold;
            item.gemCost       = d.gem;
            item.isDefault     = d.isDefault;
            EditorUtility.SetDirty(item);
            created.Add(item);
        }
        Debug.Log($"[EconomyAssetGenerator] {created.Count} UnlockableItem assets ready.");
        return created;
    }

    // ─── UnlockDatabase ──────────────────────────────────────────────────────

    private static void GenerateUnlockDatabase(List<UnlockableItem> items)
    {
        const string dir  = "Assets/Resources/Economy";
        const string path = dir + "/UnlockDatabase.asset";
        EnsureDir(dir);

        UnlockDatabase db = AssetDatabase.LoadAssetAtPath<UnlockDatabase>(path);
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<UnlockDatabase>();
            AssetDatabase.CreateAsset(db, path);
        }
        db.allItems = items;
        EditorUtility.SetDirty(db);
        Debug.Log("[EconomyAssetGenerator] UnlockDatabase.asset ready.");
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static void EnsureDir(string assetPath)
    {
        string full = Path.Combine(Application.dataPath, "..", assetPath).Replace('/', Path.DirectorySeparatorChar);
        if (!Directory.Exists(full))
            Directory.CreateDirectory(full);

        // Also ensure via AssetDatabase
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
