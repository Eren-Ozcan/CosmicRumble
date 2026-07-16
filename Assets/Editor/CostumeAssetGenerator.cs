using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using CosmicRumble.Economy;

/// <summary>
/// Kostüm asset üretici — 2026-07-16 yeniden tasarımı: eski 150 serbest kostüm yerine
/// 5 karakter × 3 kademe = 15 kostüm (kullanıcı kararı; karakter isimleri şimdilik jenerik,
/// kostüm sanatı tasarlanırken adlandırılacak). Çalıştırıldığında yeni seti üretir/günceller,
/// Assets/Resources/Costumes altında yeni sete ait OLMAYAN tüm eski .asset'leri siler ve
/// CostumeDatabase.asset'i yeniden bağlar.
///
/// Kademe deseni (her karakterde):
///   _1 "Standard" — Common, baştan açık
///   _2 "Advanced" — orta kademe (Gold / Sandık / Level karışık)
///   _3 "Elite"    — üst kademe (Gem / Başarım / yüksek Level karışık)
/// Sandık dropu yalnız Common/Uncommon ByChest kostümleri seçebildiği için (ChestManager.
/// PickUnownedCostume) ByChest olanlar bilerek Uncommon.
/// </summary>
public class CostumeAssetGenerator
{
    [MenuItem("CosmicRumble/Economy/Generate Costume Assets (5x3)")]
    public static void GenerateAll()
    {
        var costumes = GenerateCostumeDefinitions();
        DeleteStaleAssets(costumes);
        GenerateCostumeDatabase(costumes);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[CostumeAssetGenerator] Done — {costumes.Count} costumes generated.");
    }

    private static List<CostumeDefinition> GenerateCostumeDefinitions()
    {
        const string dir = "Assets/Resources/Costumes";
        EnsureDir(dir);

        // (id, name, characterId, rarity, unlock, level, gold, gem, achievementId)
        var defs = new List<(string id, string name, int ch, CostumeRarity r, CostumeUnlock ul, int lv, long gold, long gem, string ach)>
        {
            // ── Karakter 1 ───────────────────────────────────────────────────
            ("c1_1", "Standard", 1, CostumeRarity.Common,    CostumeUnlock.Default,       0,    0,  0, ""),
            ("c1_2", "Advanced", 1, CostumeRarity.Rare,      CostumeUnlock.ByGold,        0,  800,  0, ""),
            ("c1_3", "Elite",    1, CostumeRarity.Epic,      CostumeUnlock.ByLevel,      20,    0,  0, ""),

            // ── Karakter 2 ───────────────────────────────────────────────────
            ("c2_1", "Standard", 2, CostumeRarity.Common,    CostumeUnlock.Default,       0,    0,  0, ""),
            ("c2_2", "Advanced", 2, CostumeRarity.Uncommon,  CostumeUnlock.ByChest,       0,    0,  0, ""),
            ("c2_3", "Elite",    2, CostumeRarity.Epic,      CostumeUnlock.ByGem,         0,    0, 50, ""),

            // ── Karakter 3 ───────────────────────────────────────────────────
            ("c3_1", "Standard", 3, CostumeRarity.Common,    CostumeUnlock.Default,       0,    0,  0, ""),
            ("c3_2", "Advanced", 3, CostumeRarity.Rare,      CostumeUnlock.ByLevel,      10,    0,  0, ""),
            ("c3_3", "Elite",    3, CostumeRarity.Legendary, CostumeUnlock.ByAchievement, 0,    0,  0, "EFSANE"),

            // ── Karakter 4 ───────────────────────────────────────────────────
            ("c4_1", "Standard", 4, CostumeRarity.Common,    CostumeUnlock.Default,       0,    0,  0, ""),
            ("c4_2", "Advanced", 4, CostumeRarity.Rare,      CostumeUnlock.ByGold,        0, 1200,  0, ""),
            ("c4_3", "Elite",    4, CostumeRarity.Epic,      CostumeUnlock.ByGem,         0,    0, 80, ""),

            // ── Karakter 5 ───────────────────────────────────────────────────
            ("c5_1", "Standard", 5, CostumeRarity.Common,    CostumeUnlock.Default,       0,    0,  0, ""),
            ("c5_2", "Advanced", 5, CostumeRarity.Uncommon,  CostumeUnlock.ByChest,       0,    0,  0, ""),
            ("c5_3", "Elite",    5, CostumeRarity.Epic,      CostumeUnlock.ByLevel,      35,    0,  0, ""),
        };

        var created = new List<CostumeDefinition>();
        foreach (var d in defs)
        {
            string path = $"{dir}/{d.id}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<CostumeDefinition>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<CostumeDefinition>();
                AssetDatabase.CreateAsset(asset, path);
            }
            asset.costumeId             = d.id;
            asset.displayName           = d.name;
            asset.characterId           = d.ch;
            asset.costumeType           = CostumeType.Character; // silah kostümü kalktı (5x3=15 toplam)
            asset.rarity                = d.r;
            asset.theme                 = CostumeTheme.Other;    // gerçek tema karakter sanatıyla gelecek
            asset.unlockMethod          = d.ul;
            asset.requiredLevel         = d.lv;
            asset.goldCost              = d.gold;
            asset.gemCost               = d.gem;
            asset.requiredAchievementId = d.ach;
            asset.previewSprite         = null;
            asset.unlockDescription     = BuildUnlockDescription(d.ul, d.lv, d.gold, d.gem, d.ach);
            EditorUtility.SetDirty(asset);
            created.Add(asset);
        }
        Debug.Log($"[CostumeAssetGenerator] {created.Count} CostumeDefinition assets ready.");
        return created;
    }

    /// <summary>Yeni sete ait olmayan tüm eski kostüm asset'lerini (150'lik set) siler.</summary>
    private static void DeleteStaleAssets(List<CostumeDefinition> keep)
    {
        var keepPaths = new HashSet<string>();
        foreach (var c in keep) keepPaths.Add(AssetDatabase.GetAssetPath(c));

        int deleted = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:CostumeDefinition", new[] { "Assets/Resources/Costumes" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!keepPaths.Contains(path) && AssetDatabase.DeleteAsset(path))
                deleted++;
        }
        Debug.Log($"[CostumeAssetGenerator] {deleted} stale costume assets deleted.");
    }

    private static void GenerateCostumeDatabase(List<CostumeDefinition> costumes)
    {
        const string dir  = "Assets/Resources/Economy";
        const string path = dir + "/CostumeDatabase.asset";
        EnsureDir(dir);

        var db = AssetDatabase.LoadAssetAtPath<CostumeDatabase>(path);
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<CostumeDatabase>();
            AssetDatabase.CreateAsset(db, path);
        }
        db.allCostumes = costumes;
        EditorUtility.SetDirty(db);
        Debug.Log("[CostumeAssetGenerator] CostumeDatabase.asset ready.");
    }

    private static string BuildUnlockDescription(CostumeUnlock ul, int lv, long gold, long gem, string ach)
    {
        return ul switch
        {
            CostumeUnlock.Default       => "Unlocked from start",
            CostumeUnlock.ByLevel       => $"Requires level {lv}",
            CostumeUnlock.ByGold        => $"{gold} Gold",
            CostumeUnlock.ByGem         => $"{gem} Gem",
            CostumeUnlock.ByChest       => "Drops from chests",
            CostumeUnlock.ByAchievement => $"Achievement: {ach}",
            _                           => ""
        };
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
