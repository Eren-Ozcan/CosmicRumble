using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using CosmicRumble.Economy;

public class CostumeAssetGenerator
{
    [MenuItem("CosmicRumble/Economy/Generate Costume Assets (Section 4)")]
    public static void GenerateAll()
    {
        var costumes = GenerateCostumeDefinitions();
        GenerateCostumeDatabase(costumes);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[CostumeAssetGenerator] Done — {costumes.Count} costumes generated.");
    }

    private static List<CostumeDefinition> GenerateCostumeDefinitions()
    {
        const string dir = "Assets/Resources/Costumes";
        EnsureDir(dir);

        // (id, name, type, rarity, theme, unlock, level, gold, gem, achievementId)
        var defs = new List<(string id, string name, CostumeType ct, CostumeRarity r, CostumeTheme th, CostumeUnlock ul, int lv, long gold, long gem, string ach)>
        {
            // ── COMMON (40) ──────────────────────────────────────────────────
            ("c001","Gray Soldier",     CostumeType.Character,CostumeRarity.Common,  CostumeTheme.Other,  CostumeUnlock.Default,      0,    0,  0,""),
            ("c002","Standard Blue",    CostumeType.Character,CostumeRarity.Common,  CostumeTheme.Other,  CostumeUnlock.Default,      0,    0,  0,""),
            ("c003","Red Warrior",      CostumeType.Character,CostumeRarity.Common,  CostumeTheme.Other,  CostumeUnlock.ByLevel,      3,    0,  0,""),
            ("c004","Green Camo",       CostumeType.Character,CostumeRarity.Common,  CostumeTheme.Nature, CostumeUnlock.ByLevel,      5,    0,  0,""),
            ("c005","Yellow Storm",     CostumeType.Character,CostumeRarity.Common,  CostumeTheme.Other,  CostumeUnlock.ByGold,       0,  200,  0,""),
            ("c006","Orange Ember",     CostumeType.Character,CostumeRarity.Common,  CostumeTheme.Fire,   CostumeUnlock.ByGold,       0,  200,  0,""),
            ("c007","Purple Night",     CostumeType.Character,CostumeRarity.Common,  CostumeTheme.Dark,   CostumeUnlock.ByGold,       0,  250,  0,""),
            ("c008","White Snow",       CostumeType.Character,CostumeRarity.Common,  CostumeTheme.Ice,    CostumeUnlock.ByGold,       0,  250,  0,""),
            ("c009","Brown Earth",      CostumeType.Character,CostumeRarity.Common,  CostumeTheme.Nature, CostumeUnlock.ByLevel,      7,    0,  0,""),
            ("c010","Sky Blue",         CostumeType.Character,CostumeRarity.Common,  CostumeTheme.Space,  CostumeUnlock.ByLevel,      9,    0,  0,""),
            ("c011","Steel Gray",       CostumeType.Weapon,   CostumeRarity.Common,  CostumeTheme.Mech,   CostumeUnlock.ByGold,       0,  150,  0,""),
            ("c012","Rust Brown",       CostumeType.Weapon,   CostumeRarity.Common,  CostumeTheme.Mech,   CostumeUnlock.ByGold,       0,  150,  0,""),
            ("c013","Forest Green",     CostumeType.Weapon,   CostumeRarity.Common,  CostumeTheme.Nature, CostumeUnlock.ByLevel,      6,    0,  0,""),
            ("c014","Lava Red",         CostumeType.Weapon,   CostumeRarity.Common,  CostumeTheme.Fire,   CostumeUnlock.ByGold,       0,  175,  0,""),
            ("c015","Ice Blue",         CostumeType.Weapon,   CostumeRarity.Common,  CostumeTheme.Ice,    CostumeUnlock.ByGold,       0,  175,  0,""),
            ("c016","Night Black",      CostumeType.Weapon,   CostumeRarity.Common,  CostumeTheme.Dark,   CostumeUnlock.ByLevel,      8,    0,  0,""),
            ("c017","Sun Yellow",       CostumeType.Weapon,   CostumeRarity.Common,  CostumeTheme.Other,  CostumeUnlock.ByGold,       0,  200,  0,""),
            ("c018","Coral Pink",       CostumeType.Character,CostumeRarity.Common,  CostumeTheme.Other,  CostumeUnlock.ByGold,       0,  200,  0,""),
            ("c019","Sea Teal",         CostumeType.Character,CostumeRarity.Common,  CostumeTheme.Other,  CostumeUnlock.ByLevel,     11,    0,  0,""),
            ("c020","Lavender",         CostumeType.Character,CostumeRarity.Common,  CostumeTheme.Other,  CostumeUnlock.ByGold,       0,  225,  0,""),
            ("c021","Bright Copper",    CostumeType.Weapon,   CostumeRarity.Common,  CostumeTheme.Mech,   CostumeUnlock.ByGold,       0,  175,  0,""),
            ("c022","Desert Sand",      CostumeType.Character,CostumeRarity.Common,  CostumeTheme.Nature, CostumeUnlock.ByLevel,     13,    0,  0,""),
            ("c023","Pistachio Green",  CostumeType.Character,CostumeRarity.Common,  CostumeTheme.Nature, CostumeUnlock.ByGold,       0,  200,  0,""),
            ("c024","Sea Foam",         CostumeType.Weapon,   CostumeRarity.Common,  CostumeTheme.Ice,    CostumeUnlock.ByGold,       0,  150,  0,""),
            ("c025","Fog Gray",         CostumeType.Character,CostumeRarity.Common,  CostumeTheme.Dark,   CostumeUnlock.ByLevel,     15,    0,  0,""),
            ("c026","Sunset",           CostumeType.Character,CostumeRarity.Common,  CostumeTheme.Fire,   CostumeUnlock.ByGold,       0,  250,  0,""),
            ("c027","Stardust",         CostumeType.Weapon,   CostumeRarity.Common,  CostumeTheme.Space,  CostumeUnlock.ByLevel,     12,    0,  0,""),
            ("c028","Ocean Depths",     CostumeType.Weapon,   CostumeRarity.Common,  CostumeTheme.Other,  CostumeUnlock.ByChest,      0,    0,  0,""),
            ("c029","Chalk White",      CostumeType.Character,CostumeRarity.Common,  CostumeTheme.Other,  CostumeUnlock.ByChest,      0,    0,  0,""),
            ("c030","Anthracite",       CostumeType.Character,CostumeRarity.Common,  CostumeTheme.Dark,   CostumeUnlock.ByChest,      0,    0,  0,""),
            ("c031","Mint Green",       CostumeType.Weapon,   CostumeRarity.Common,  CostumeTheme.Nature, CostumeUnlock.ByGold,       0,  175,  0,""),
            ("c032","Candy Pink",       CostumeType.Character,CostumeRarity.Common,  CostumeTheme.Other,  CostumeUnlock.ByGold,       0,  200,  0,""),
            ("c033","Thunder",          CostumeType.Weapon,   CostumeRarity.Common,  CostumeTheme.Other,  CostumeUnlock.ByLevel,     14,    0,  0,""),
            ("c034","Golden Yellow",    CostumeType.Weapon,   CostumeRarity.Common,  CostumeTheme.Other,  CostumeUnlock.ByGold,       0,  225,  0,""),
            ("c035","Emerald",          CostumeType.Character,CostumeRarity.Common,  CostumeTheme.Nature, CostumeUnlock.ByLevel,     16,    0,  0,""),
            ("c036","Hedgehog Brown",   CostumeType.Character,CostumeRarity.Common,  CostumeTheme.Nature, CostumeUnlock.ByChest,      0,    0,  0,""),
            ("c037","Titan Gray",       CostumeType.Weapon,   CostumeRarity.Common,  CostumeTheme.Mech,   CostumeUnlock.ByChest,      0,    0,  0,""),
            ("c038","Maroon",           CostumeType.Character,CostumeRarity.Common,  CostumeTheme.Dark,   CostumeUnlock.ByGold,       0,  200,  0,""),
            ("c039","Cobalt",           CostumeType.Weapon,   CostumeRarity.Common,  CostumeTheme.Space,  CostumeUnlock.ByGold,       0,  200,  0,""),
            ("c040","Indigo Blue",      CostumeType.Character,CostumeRarity.Common,  CostumeTheme.Other,  CostumeUnlock.ByLevel,     18,    0,  0,""),

            // ── UNCOMMON (35) ────────────────────────────────────────────────
            ("u001","Forest Warrior",   CostumeType.Character,CostumeRarity.Uncommon,CostumeTheme.Nature, CostumeUnlock.ByLevel,     20,    0,  0,""),
            ("u002","Ice Mage",         CostumeType.Character,CostumeRarity.Uncommon,CostumeTheme.Ice,    CostumeUnlock.ByLevel,     22,    0,  0,""),
            ("u003","Flame Dancer",     CostumeType.Character,CostumeRarity.Uncommon,CostumeTheme.Fire,   CostumeUnlock.ByLevel,     24,    0,  0,""),
            ("u004","Night Watcher",    CostumeType.Character,CostumeRarity.Uncommon,CostumeTheme.Dark,   CostumeUnlock.ByGold,       0,  500,  0,""),
            ("u005","Lightning Runner", CostumeType.Character,CostumeRarity.Uncommon,CostumeTheme.Other,  CostumeUnlock.ByGold,       0,  500,  0,""),
            ("u006","Sandstorm",        CostumeType.Character,CostumeRarity.Uncommon,CostumeTheme.Nature, CostumeUnlock.ByLevel,     26,    0,  0,""),
            ("u007","Deep Space",       CostumeType.Character,CostumeRarity.Uncommon,CostumeTheme.Space,  CostumeUnlock.ByLevel,     28,    0,  0,""),
            ("u008","Iron Fist",        CostumeType.Character,CostumeRarity.Uncommon,CostumeTheme.Mech,   CostumeUnlock.ByGold,       0,  550,  0,""),
            ("u009","Wind Spirit",      CostumeType.Character,CostumeRarity.Uncommon,CostumeTheme.Nature, CostumeUnlock.ByGold,       0,  550,  0,""),
            ("u010","Cosmic Purple",    CostumeType.Character,CostumeRarity.Uncommon,CostumeTheme.Space,  CostumeUnlock.ByLevel,     30,    0,  0,""),
            ("u011","Dragon Fang",      CostumeType.Weapon,   CostumeRarity.Uncommon,CostumeTheme.Fantasy,CostumeUnlock.ByGold,       0,  500,  0,""),
            ("u012","Space Rifle",      CostumeType.Weapon,   CostumeRarity.Uncommon,CostumeTheme.Space,  CostumeUnlock.ByLevel,     25,    0,  0,""),
            ("u013","Ice Sword",        CostumeType.Weapon,   CostumeRarity.Uncommon,CostumeTheme.Ice,    CostumeUnlock.ByGold,       0,  500,  0,""),
            ("u014","Flame Spear",      CostumeType.Weapon,   CostumeRarity.Uncommon,CostumeTheme.Fire,   CostumeUnlock.ByLevel,     27,    0,  0,""),
            ("u015","Shadow Blade",     CostumeType.Weapon,   CostumeRarity.Uncommon,CostumeTheme.Dark,   CostumeUnlock.ByGold,       0,  525,  0,""),
            ("u016","Fog Pistol",       CostumeType.Weapon,   CostumeRarity.Uncommon,CostumeTheme.Dark,   CostumeUnlock.ByChest,      0,    0,  0,""),
            ("u017","Plasma Tube",      CostumeType.Weapon,   CostumeRarity.Uncommon,CostumeTheme.Cyber,  CostumeUnlock.ByLevel,     29,    0,  0,""),
            ("u018","Nature Shield",    CostumeType.Weapon,   CostumeRarity.Uncommon,CostumeTheme.Nature, CostumeUnlock.ByGold,       0,  500,  0,""),
            ("u019","Lightning Orb",    CostumeType.Weapon,   CostumeRarity.Uncommon,CostumeTheme.Other,  CostumeUnlock.ByChest,      0,    0,  0,""),
            ("u020","Iron Shield",      CostumeType.Weapon,   CostumeRarity.Uncommon,CostumeTheme.Mech,   CostumeUnlock.ByGold,       0,  550,  0,""),
            ("u021","Crystal Warrior",  CostumeType.Character,CostumeRarity.Uncommon,CostumeTheme.Ice,    CostumeUnlock.ByAchievement,0,    0,  0,"achievement_ice_master"),
            ("u022","Volcano Man",      CostumeType.Character,CostumeRarity.Uncommon,CostumeTheme.Fire,   CostumeUnlock.ByAchievement,0,    0,  0,"PATLAMA_UZMANI"),
            ("u023","Cyber Ninja",      CostumeType.Character,CostumeRarity.Uncommon,CostumeTheme.Cyber,  CostumeUnlock.ByLevel,     32,    0,  0,""),
            ("u024","Stone Golem",      CostumeType.Character,CostumeRarity.Uncommon,CostumeTheme.Nature, CostumeUnlock.ByLevel,     34,    0,  0,""),
            ("u025","Neon Jacket",      CostumeType.Character,CostumeRarity.Uncommon,CostumeTheme.Cyber,  CostumeUnlock.ByGold,       0,  600,  0,""),
            ("u026","Foam Sailor",      CostumeType.Character,CostumeRarity.Uncommon,CostumeTheme.Other,  CostumeUnlock.ByChest,      0,    0,  0,""),
            ("u027","Steppe Soldier",   CostumeType.Character,CostumeRarity.Uncommon,CostumeTheme.Nature, CostumeUnlock.ByLevel,     36,    0,  0,""),
            ("u028","Silver Knight",    CostumeType.Character,CostumeRarity.Uncommon,CostumeTheme.Fantasy,CostumeUnlock.ByGold,       0,  600,  0,""),
            ("u029","Blue Crocodile",   CostumeType.Weapon,   CostumeRarity.Uncommon,CostumeTheme.Nature, CostumeUnlock.ByChest,      0,    0,  0,""),
            ("u030","Ember Blade",      CostumeType.Weapon,   CostumeRarity.Uncommon,CostumeTheme.Fire,   CostumeUnlock.ByLevel,     31,    0,  0,""),
            ("u031","Hologram Weapon",  CostumeType.Weapon,   CostumeRarity.Uncommon,CostumeTheme.Cyber,  CostumeUnlock.ByGold,       0,  575,  0,""),
            ("u032","Steel Dragon",     CostumeType.Weapon,   CostumeRarity.Uncommon,CostumeTheme.Mech,   CostumeUnlock.ByLevel,     33,    0,  0,""),
            ("u033","Crystal Bomb",     CostumeType.Weapon,   CostumeRarity.Uncommon,CostumeTheme.Ice,    CostumeUnlock.ByGold,       0,  550,  0,""),
            ("u034","Root Texture",     CostumeType.Weapon,   CostumeRarity.Uncommon,CostumeTheme.Nature, CostumeUnlock.ByAchievement,0,    0,  0,"GEZEGEN_KATILI"),
            ("u035","Storm Sail",       CostumeType.Character,CostumeRarity.Uncommon,CostumeTheme.Other,  CostumeUnlock.ByGold,       0,  600,  0,""),

            // ── RARE (35) ────────────────────────────────────────────────────
            ("r001","Galaxy Wanderer",  CostumeType.Character,CostumeRarity.Rare,    CostumeTheme.Space,  CostumeUnlock.ByLevel,     40,    0,  0,""),
            ("r002","Black Knight",     CostumeType.Character,CostumeRarity.Rare,    CostumeTheme.Dark,   CostumeUnlock.ByLevel,     42,    0,  0,""),
            ("r003","Neon Samurai",     CostumeType.Character,CostumeRarity.Rare,    CostumeTheme.Cyber,  CostumeUnlock.ByLevel,     44,    0,  0,""),
            ("r004","Dragon Hunter",    CostumeType.Character,CostumeRarity.Rare,    CostumeTheme.Fantasy,CostumeUnlock.ByLevel,     45,    0,  0,""),
            ("r005","Ice God",          CostumeType.Character,CostumeRarity.Rare,    CostumeTheme.Ice,    CostumeUnlock.ByGold,       0, 1200,  0,""),
            ("r006","Lava Giant",       CostumeType.Character,CostumeRarity.Rare,    CostumeTheme.Fire,   CostumeUnlock.ByGold,       0, 1200,  0,""),
            ("r007","Quantum Armor",    CostumeType.Character,CostumeRarity.Rare,    CostumeTheme.Cyber,  CostumeUnlock.ByLevel,     48,    0,  0,""),
            ("r008","Forest God",       CostumeType.Character,CostumeRarity.Rare,    CostumeTheme.Nature, CostumeUnlock.ByGold,       0, 1300,  0,""),
            ("r009","Dark Sorcerer",    CostumeType.Character,CostumeRarity.Rare,    CostumeTheme.Dark,   CostumeUnlock.ByAchievement,0,    0,  0,"VETERAN_10"),
            ("r010","Meteor Warrior",   CostumeType.Character,CostumeRarity.Rare,    CostumeTheme.Space,  CostumeUnlock.ByLevel,     50,    0,  0,""),
            ("r011","Plasma Rifle",     CostumeType.Weapon,   CostumeRarity.Rare,    CostumeTheme.Cyber,  CostumeUnlock.ByLevel,     41,    0,  0,""),
            ("r012","Dragon Flame",     CostumeType.Weapon,   CostumeRarity.Rare,    CostumeTheme.Fantasy,CostumeUnlock.ByGold,       0, 1100,  0,""),
            ("r013","Black Hole Cannon",CostumeType.Weapon,   CostumeRarity.Rare,    CostumeTheme.Space,  CostumeUnlock.ByLevel,     43,    0,  0,""),
            ("r014","Ice Shield",       CostumeType.Weapon,   CostumeRarity.Rare,    CostumeTheme.Ice,    CostumeUnlock.ByGold,       0, 1100,  0,""),
            ("r015","Ember Bomb",       CostumeType.Weapon,   CostumeRarity.Rare,    CostumeTheme.Fire,   CostumeUnlock.ByAchievement,0,    0,  0,"PATLAMA_UZMANI"),
            ("r016","Nano Blade",       CostumeType.Weapon,   CostumeRarity.Rare,    CostumeTheme.Cyber,  CostumeUnlock.ByGold,       0, 1200,  0,""),
            ("r017","Rune Spear",       CostumeType.Weapon,   CostumeRarity.Rare,    CostumeTheme.Fantasy,CostumeUnlock.ByLevel,     46,    0,  0,""),
            ("r018","Shadow Arrow",     CostumeType.Weapon,   CostumeRarity.Rare,    CostumeTheme.Dark,   CostumeUnlock.ByChest,      0,    0,  0,""),
            ("r019","Emerald Dragon",   CostumeType.Weapon,   CostumeRarity.Rare,    CostumeTheme.Fantasy,CostumeUnlock.ByGold,       0, 1250,  0,""),
            ("r020","Star Sword",       CostumeType.Weapon,   CostumeRarity.Rare,    CostumeTheme.Space,  CostumeUnlock.ByLevel,     47,    0,  0,""),
            ("r021","Titanium Golem",   CostumeType.Character,CostumeRarity.Rare,    CostumeTheme.Mech,   CostumeUnlock.ByLevel,     52,    0,  0,""),
            ("r022","Light Speed",      CostumeType.Character,CostumeRarity.Rare,    CostumeTheme.Space,  CostumeUnlock.ByGold,       0, 1400,  0,""),
            ("r023","Sea Monster",      CostumeType.Character,CostumeRarity.Rare,    CostumeTheme.Nature, CostumeUnlock.ByAchievement,0,    0,  0,"SOSYAL_KELEBEK"),
            ("r024","Storm God",        CostumeType.Character,CostumeRarity.Rare,    CostumeTheme.Myth,   CostumeUnlock.ByLevel,     54,    0,  0,""),
            ("r025","Crimson Shaman",   CostumeType.Character,CostumeRarity.Rare,    CostumeTheme.Myth,   CostumeUnlock.ByGold,       0, 1400,  0,""),
            ("r026","Cyber Samurai",    CostumeType.Character,CostumeRarity.Rare,    CostumeTheme.Cyber,  CostumeUnlock.ByLevel,     56,    0,  0,""),
            ("r027","Bionic Warrior",   CostumeType.Character,CostumeRarity.Rare,    CostumeTheme.Mech,   CostumeUnlock.ByGold,       0, 1500,  0,""),
            ("r028","Vortex Rifle",     CostumeType.Weapon,   CostumeRarity.Rare,    CostumeTheme.Space,  CostumeUnlock.ByChest,      0,    0,  0,""),
            ("r029","Shaman Staff",     CostumeType.Weapon,   CostumeRarity.Rare,    CostumeTheme.Myth,   CostumeUnlock.ByLevel,     49,    0,  0,""),
            ("r030","Titan Hammer",     CostumeType.Weapon,   CostumeRarity.Rare,    CostumeTheme.Mech,   CostumeUnlock.ByGold,       0, 1300,  0,""),
            ("r031","Wind Blade",       CostumeType.Weapon,   CostumeRarity.Rare,    CostumeTheme.Nature, CostumeUnlock.ByAchievement,0,    0,  0,"CEVRECI"),
            ("r032","Crystal Staff",    CostumeType.Weapon,   CostumeRarity.Rare,    CostumeTheme.Fantasy,CostumeUnlock.ByLevel,     51,    0,  0,""),
            ("r033","Laser Rifle",      CostumeType.Weapon,   CostumeRarity.Rare,    CostumeTheme.Cyber,  CostumeUnlock.ByGold,       0, 1350,  0,""),
            ("r034","Dark Rune",        CostumeType.Weapon,   CostumeRarity.Rare,    CostumeTheme.Dark,   CostumeUnlock.ByLevel,     53,    0,  0,""),
            ("r035","Mythic Archer",    CostumeType.Character,CostumeRarity.Rare,    CostumeTheme.Myth,   CostumeUnlock.ByAchievement,0,    0,  0,"SAVAS_MAKINESI"),

            // ── EPIC (25) ────────────────────────────────────────────────────
            ("e001","Nebula Warrior",   CostumeType.Character,CostumeRarity.Epic,    CostumeTheme.Space,  CostumeUnlock.ByLevel,     60,    0,  0,""),
            ("e002","Dragon Lord",      CostumeType.Character,CostumeRarity.Epic,    CostumeTheme.Fantasy,CostumeUnlock.ByLevel,     63,    0,  0,""),
            ("e003","Cyber God",        CostumeType.Character,CostumeRarity.Epic,    CostumeTheme.Cyber,  CostumeUnlock.ByGem,        0,    0, 50,""),
            ("e004","Death Spirit",     CostumeType.Character,CostumeRarity.Epic,    CostumeTheme.Dark,   CostumeUnlock.ByLevel,     66,    0,  0,""),
            ("e005","Volcano God",      CostumeType.Character,CostumeRarity.Epic,    CostumeTheme.Fire,   CostumeUnlock.ByGem,        0,    0, 60,""),
            ("e006","Ice Storm",        CostumeType.Character,CostumeRarity.Epic,    CostumeTheme.Ice,    CostumeUnlock.ByLevel,     70,    0,  0,""),
            ("e007","Forest Deity",     CostumeType.Character,CostumeRarity.Epic,    CostumeTheme.Nature, CostumeUnlock.ByAchievement,0,    0,  0,"EFSANE"),
            ("e008","Titan Armor",      CostumeType.Character,CostumeRarity.Epic,    CostumeTheme.Mech,   CostumeUnlock.ByGem,        0,    0, 70,""),
            ("e009","Olympian God",     CostumeType.Character,CostumeRarity.Epic,    CostumeTheme.Myth,   CostumeUnlock.ByLevel,     73,    0,  0,""),
            ("e010","Quantum Shadow",   CostumeType.Character,CostumeRarity.Epic,    CostumeTheme.Cyber,  CostumeUnlock.ByGem,        0,    0, 75,""),
            ("e011","Galactic Emperor", CostumeType.Character,CostumeRarity.Epic,    CostumeTheme.Space,  CostumeUnlock.ByLevel,     76,    0,  0,""),
            ("e012","Ancient Dragon",   CostumeType.Character,CostumeRarity.Epic,    CostumeTheme.Fantasy,CostumeUnlock.ByAchievement,0,    0,  0,"KARA_DELIK_USTASI"),
            ("e013","Neon Demon",       CostumeType.Character,CostumeRarity.Epic,    CostumeTheme.Dark,   CostumeUnlock.ByGem,        0,    0, 80,""),
            ("e014","Plasma God",       CostumeType.Weapon,   CostumeRarity.Epic,    CostumeTheme.Cyber,  CostumeUnlock.ByLevel,     61,    0,  0,""),
            ("e015","Dragon Breath",    CostumeType.Weapon,   CostumeRarity.Epic,    CostumeTheme.Fantasy,CostumeUnlock.ByGem,        0,    0, 50,""),
            ("e016","Dark Star",        CostumeType.Weapon,   CostumeRarity.Epic,    CostumeTheme.Dark,   CostumeUnlock.ByLevel,     64,    0,  0,""),
            ("e017","Volcano Cannon",   CostumeType.Weapon,   CostumeRarity.Epic,    CostumeTheme.Fire,   CostumeUnlock.ByGem,        0,    0, 55,""),
            ("e018","Ice Crystal",      CostumeType.Weapon,   CostumeRarity.Epic,    CostumeTheme.Ice,    CostumeUnlock.ByAchievement,0,    0,  0,"DOKUNULMAZ"),
            ("e019","Nano Swarm",       CostumeType.Weapon,   CostumeRarity.Epic,    CostumeTheme.Mech,   CostumeUnlock.ByLevel,     68,    0,  0,""),
            ("e020","Rune Burst",       CostumeType.Weapon,   CostumeRarity.Epic,    CostumeTheme.Fantasy,CostumeUnlock.ByGem,        0,    0, 65,""),
            ("e021","Nebula Bomb",      CostumeType.Weapon,   CostumeRarity.Epic,    CostumeTheme.Space,  CostumeUnlock.ByLevel,     71,    0,  0,""),
            ("e022","Titan Laser",      CostumeType.Weapon,   CostumeRarity.Epic,    CostumeTheme.Mech,   CostumeUnlock.ByGem,        0,    0, 70,""),
            ("e023","Mythic Armor",     CostumeType.Character,CostumeRarity.Epic,    CostumeTheme.Myth,   CostumeUnlock.ByAchievement,0,    0,  0,"FIRTINA_TANRISI"),
            ("e024","Crystal Golem",    CostumeType.Character,CostumeRarity.Epic,    CostumeTheme.Ice,    CostumeUnlock.ByGem,        0,    0, 75,""),
            ("e025","Crow King",        CostumeType.Character,CostumeRarity.Epic,    CostumeTheme.Dark,   CostumeUnlock.ByLevel,     80,    0,  0,""),

            // ── LEGENDARY (15) ───────────────────────────────────────────────
            ("l001","Cosmic Master",    CostumeType.Character,CostumeRarity.Legendary,CostumeTheme.Space, CostumeUnlock.ByLevel,    100,    0,   0,""),
            ("l002","Dragon Emperor",   CostumeType.Character,CostumeRarity.Legendary,CostumeTheme.Fantasy,CostumeUnlock.ByGem,      0,    0, 200,""),
            ("l003","Dark God",         CostumeType.Character,CostumeRarity.Legendary,CostumeTheme.Dark,  CostumeUnlock.ByAchievement,0,   0,   0,"COSMIC_100"),
            ("l004","Doom Lord",        CostumeType.Character,CostumeRarity.Legendary,CostumeTheme.Dark,  CostumeUnlock.ByGem,       0,    0, 250,""),
            ("l005","Time Master",      CostumeType.Character,CostumeRarity.Legendary,CostumeTheme.Myth,  CostumeUnlock.ByLevel,    101,    0,   0,""),
            ("l006","Universe Warrior", CostumeType.Character,CostumeRarity.Legendary,CostumeTheme.Space, CostumeUnlock.ByGem,       0,    0, 300,""),
            ("l007","Ancient Giant",    CostumeType.Character,CostumeRarity.Legendary,CostumeTheme.Myth,  CostumeUnlock.ByAchievement,0,   0,   0,"GALAKSININ_EFSANESI"),
            ("l008","Bionic God",       CostumeType.Character,CostumeRarity.Legendary,CostumeTheme.Mech,  CostumeUnlock.ByGem,       0,    0, 250,""),
            ("l009","Phoenix Warrior",  CostumeType.Character,CostumeRarity.Legendary,CostumeTheme.Fire,  CostumeUnlock.ByLevel,    102,    0,   0,""),
            ("l010","Cosmic Destroyer", CostumeType.Weapon,   CostumeRarity.Legendary,CostumeTheme.Space, CostumeUnlock.ByLevel,    100,    0,   0,""),
            ("l011","God Sword",        CostumeType.Weapon,   CostumeRarity.Legendary,CostumeTheme.Myth,  CostumeUnlock.ByGem,       0,    0, 200,""),
            ("l012","Dragon Heart",     CostumeType.Weapon,   CostumeRarity.Legendary,CostumeTheme.Fantasy,CostumeUnlock.ByAchievement,0,  0,   0,"EJDER_AVCI"),
            ("l013","Black Hole Cannon X",CostumeType.Weapon, CostumeRarity.Legendary,CostumeTheme.Space, CostumeUnlock.ByGem,       0,    0, 250,""),
            ("l014","Doom Hammer",      CostumeType.Weapon,   CostumeRarity.Legendary,CostumeTheme.Dark,  CostumeUnlock.ByAchievement,0,   0,   0,"HOME_RUN"),
            ("l015","Creator's Power",  CostumeType.Weapon,   CostumeRarity.Legendary,CostumeTheme.Myth,  CostumeUnlock.ByLevel,    103,    0,   0,""),
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
            asset.costumeId              = d.id;
            asset.displayName            = d.name;
            asset.costumeType            = d.ct;
            asset.rarity                 = d.r;
            asset.theme                  = d.th;
            asset.unlockMethod           = d.ul;
            asset.requiredLevel          = d.lv;
            asset.goldCost               = d.gold;
            asset.gemCost                = d.gem;
            asset.requiredAchievementId  = d.ach;
            asset.previewSprite          = null;
            asset.unlockDescription      = BuildUnlockDescription(d.ul, d.lv, d.gold, d.gem, d.ach);
            EditorUtility.SetDirty(asset);
            created.Add(asset);
        }
        Debug.Log($"[CostumeAssetGenerator] {created.Count} CostumeDefinition assets ready.");
        return created;
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
            CostumeUnlock.Default      => "Unlocked from start",
            CostumeUnlock.ByLevel      => $"Requires level {lv}",
            CostumeUnlock.ByGold       => $"{gold} Gold",
            CostumeUnlock.ByGem        => $"{gem} Gem",
            CostumeUnlock.ByChest      => "Drops from chests",
            CostumeUnlock.ByAchievement=> $"Achievement: {ach}",
            _                          => ""
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
