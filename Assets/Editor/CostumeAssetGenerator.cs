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
            ("c001","Gri Asker",        CostumeType.Character,CostumeRarity.Common,  CostumeTheme.Other,  CostumeUnlock.Default,      0,    0,  0,""),
            ("c002","Standart Mavi",    CostumeType.Character,CostumeRarity.Common,  CostumeTheme.Other,  CostumeUnlock.Default,      0,    0,  0,""),
            ("c003","Kırmızı Savaşçı",  CostumeType.Character,CostumeRarity.Common,  CostumeTheme.Other,  CostumeUnlock.ByLevel,      3,    0,  0,""),
            ("c004","Yeşil Kamuflaj",   CostumeType.Character,CostumeRarity.Common,  CostumeTheme.Nature, CostumeUnlock.ByLevel,      5,    0,  0,""),
            ("c005","Sarı Fırtına",     CostumeType.Character,CostumeRarity.Common,  CostumeTheme.Other,  CostumeUnlock.ByGold,       0,  200,  0,""),
            ("c006","Turuncu Kor",      CostumeType.Character,CostumeRarity.Common,  CostumeTheme.Fire,   CostumeUnlock.ByGold,       0,  200,  0,""),
            ("c007","Mor Gece",         CostumeType.Character,CostumeRarity.Common,  CostumeTheme.Dark,   CostumeUnlock.ByGold,       0,  250,  0,""),
            ("c008","Beyaz Kar",        CostumeType.Character,CostumeRarity.Common,  CostumeTheme.Ice,    CostumeUnlock.ByGold,       0,  250,  0,""),
            ("c009","Kahve Toprak",     CostumeType.Character,CostumeRarity.Common,  CostumeTheme.Nature, CostumeUnlock.ByLevel,      7,    0,  0,""),
            ("c010","Gök Mavisi",       CostumeType.Character,CostumeRarity.Common,  CostumeTheme.Space,  CostumeUnlock.ByLevel,      9,    0,  0,""),
            ("c011","Çelik Gri",        CostumeType.Weapon,   CostumeRarity.Common,  CostumeTheme.Mech,   CostumeUnlock.ByGold,       0,  150,  0,""),
            ("c012","Pas Kahvesi",      CostumeType.Weapon,   CostumeRarity.Common,  CostumeTheme.Mech,   CostumeUnlock.ByGold,       0,  150,  0,""),
            ("c013","Orman Yeşili",     CostumeType.Weapon,   CostumeRarity.Common,  CostumeTheme.Nature, CostumeUnlock.ByLevel,      6,    0,  0,""),
            ("c014","Lav Kırmızısı",    CostumeType.Weapon,   CostumeRarity.Common,  CostumeTheme.Fire,   CostumeUnlock.ByGold,       0,  175,  0,""),
            ("c015","Buz Mavisi",       CostumeType.Weapon,   CostumeRarity.Common,  CostumeTheme.Ice,    CostumeUnlock.ByGold,       0,  175,  0,""),
            ("c016","Gece Siyahı",      CostumeType.Weapon,   CostumeRarity.Common,  CostumeTheme.Dark,   CostumeUnlock.ByLevel,      8,    0,  0,""),
            ("c017","Güneş Sarısı",     CostumeType.Weapon,   CostumeRarity.Common,  CostumeTheme.Other,  CostumeUnlock.ByGold,       0,  200,  0,""),
            ("c018","Mercan Pembesi",   CostumeType.Character,CostumeRarity.Common,  CostumeTheme.Other,  CostumeUnlock.ByGold,       0,  200,  0,""),
            ("c019","Deniz Tealı",      CostumeType.Character,CostumeRarity.Common,  CostumeTheme.Other,  CostumeUnlock.ByLevel,     11,    0,  0,""),
            ("c020","Lavanta",          CostumeType.Character,CostumeRarity.Common,  CostumeTheme.Other,  CostumeUnlock.ByGold,       0,  225,  0,""),
            ("c021","Bakır Parlak",     CostumeType.Weapon,   CostumeRarity.Common,  CostumeTheme.Mech,   CostumeUnlock.ByGold,       0,  175,  0,""),
            ("c022","Çöl Kumu",         CostumeType.Character,CostumeRarity.Common,  CostumeTheme.Nature, CostumeUnlock.ByLevel,     13,    0,  0,""),
            ("c023","Fıstık Yeşili",    CostumeType.Character,CostumeRarity.Common,  CostumeTheme.Nature, CostumeUnlock.ByGold,       0,  200,  0,""),
            ("c024","Deniz Köpüğü",     CostumeType.Weapon,   CostumeRarity.Common,  CostumeTheme.Ice,    CostumeUnlock.ByGold,       0,  150,  0,""),
            ("c025","Sis Grisi",        CostumeType.Character,CostumeRarity.Common,  CostumeTheme.Dark,   CostumeUnlock.ByLevel,     15,    0,  0,""),
            ("c026","Gün Batımı",       CostumeType.Character,CostumeRarity.Common,  CostumeTheme.Fire,   CostumeUnlock.ByGold,       0,  250,  0,""),
            ("c027","Yıldız Tozu",      CostumeType.Weapon,   CostumeRarity.Common,  CostumeTheme.Space,  CostumeUnlock.ByLevel,     12,    0,  0,""),
            ("c028","Okyanusun Dibi",   CostumeType.Weapon,   CostumeRarity.Common,  CostumeTheme.Other,  CostumeUnlock.ByChest,      0,    0,  0,""),
            ("c029","Kireç Beyazı",     CostumeType.Character,CostumeRarity.Common,  CostumeTheme.Other,  CostumeUnlock.ByChest,      0,    0,  0,""),
            ("c030","Antrasit",         CostumeType.Character,CostumeRarity.Common,  CostumeTheme.Dark,   CostumeUnlock.ByChest,      0,    0,  0,""),
            ("c031","Nane Yeşili",      CostumeType.Weapon,   CostumeRarity.Common,  CostumeTheme.Nature, CostumeUnlock.ByGold,       0,  175,  0,""),
            ("c032","Pembe Şeker",      CostumeType.Character,CostumeRarity.Common,  CostumeTheme.Other,  CostumeUnlock.ByGold,       0,  200,  0,""),
            ("c033","Gök Gürültüsü",    CostumeType.Weapon,   CostumeRarity.Common,  CostumeTheme.Other,  CostumeUnlock.ByLevel,     14,    0,  0,""),
            ("c034","Altın Sarısı",     CostumeType.Weapon,   CostumeRarity.Common,  CostumeTheme.Other,  CostumeUnlock.ByGold,       0,  225,  0,""),
            ("c035","Zümrüt",           CostumeType.Character,CostumeRarity.Common,  CostumeTheme.Nature, CostumeUnlock.ByLevel,     16,    0,  0,""),
            ("c036","Kirpi Kahvesi",    CostumeType.Character,CostumeRarity.Common,  CostumeTheme.Nature, CostumeUnlock.ByChest,      0,    0,  0,""),
            ("c037","Titan Grisi",      CostumeType.Weapon,   CostumeRarity.Common,  CostumeTheme.Mech,   CostumeUnlock.ByChest,      0,    0,  0,""),
            ("c038","Bordo",            CostumeType.Character,CostumeRarity.Common,  CostumeTheme.Dark,   CostumeUnlock.ByGold,       0,  200,  0,""),
            ("c039","Kobalt",           CostumeType.Weapon,   CostumeRarity.Common,  CostumeTheme.Space,  CostumeUnlock.ByGold,       0,  200,  0,""),
            ("c040","Çivit Mavisi",     CostumeType.Character,CostumeRarity.Common,  CostumeTheme.Other,  CostumeUnlock.ByLevel,     18,    0,  0,""),

            // ── UNCOMMON (35) ────────────────────────────────────────────────
            ("u001","Orman Savaşçısı",  CostumeType.Character,CostumeRarity.Uncommon,CostumeTheme.Nature, CostumeUnlock.ByLevel,     20,    0,  0,""),
            ("u002","Buz Büyücüsü",     CostumeType.Character,CostumeRarity.Uncommon,CostumeTheme.Ice,    CostumeUnlock.ByLevel,     22,    0,  0,""),
            ("u003","Alev Dansçısı",    CostumeType.Character,CostumeRarity.Uncommon,CostumeTheme.Fire,   CostumeUnlock.ByLevel,     24,    0,  0,""),
            ("u004","Gece Gözcüsü",     CostumeType.Character,CostumeRarity.Uncommon,CostumeTheme.Dark,   CostumeUnlock.ByGold,       0,  500,  0,""),
            ("u005","Şimşek Koşucusu",  CostumeType.Character,CostumeRarity.Uncommon,CostumeTheme.Other,  CostumeUnlock.ByGold,       0,  500,  0,""),
            ("u006","Kum Fırtınası",    CostumeType.Character,CostumeRarity.Uncommon,CostumeTheme.Nature, CostumeUnlock.ByLevel,     26,    0,  0,""),
            ("u007","Derin Uzay",       CostumeType.Character,CostumeRarity.Uncommon,CostumeTheme.Space,  CostumeUnlock.ByLevel,     28,    0,  0,""),
            ("u008","Demir Yumruk",     CostumeType.Character,CostumeRarity.Uncommon,CostumeTheme.Mech,   CostumeUnlock.ByGold,       0,  550,  0,""),
            ("u009","Rüzgar Ruhu",      CostumeType.Character,CostumeRarity.Uncommon,CostumeTheme.Nature, CostumeUnlock.ByGold,       0,  550,  0,""),
            ("u010","Kozmik Mor",       CostumeType.Character,CostumeRarity.Uncommon,CostumeTheme.Space,  CostumeUnlock.ByLevel,     30,    0,  0,""),
            ("u011","Ejder Dişi",       CostumeType.Weapon,   CostumeRarity.Uncommon,CostumeTheme.Fantasy,CostumeUnlock.ByGold,       0,  500,  0,""),
            ("u012","Uzay Tüfeği",      CostumeType.Weapon,   CostumeRarity.Uncommon,CostumeTheme.Space,  CostumeUnlock.ByLevel,     25,    0,  0,""),
            ("u013","Buz Kılıcı",       CostumeType.Weapon,   CostumeRarity.Uncommon,CostumeTheme.Ice,    CostumeUnlock.ByGold,       0,  500,  0,""),
            ("u014","Alev Mızrağı",     CostumeType.Weapon,   CostumeRarity.Uncommon,CostumeTheme.Fire,   CostumeUnlock.ByLevel,     27,    0,  0,""),
            ("u015","Gölge Bıçağı",     CostumeType.Weapon,   CostumeRarity.Uncommon,CostumeTheme.Dark,   CostumeUnlock.ByGold,       0,  525,  0,""),
            ("u016","Sis Tabancası",    CostumeType.Weapon,   CostumeRarity.Uncommon,CostumeTheme.Dark,   CostumeUnlock.ByChest,      0,    0,  0,""),
            ("u017","Plazma Tüp",       CostumeType.Weapon,   CostumeRarity.Uncommon,CostumeTheme.Cyber,  CostumeUnlock.ByLevel,     29,    0,  0,""),
            ("u018","Doğa Kalkanı",     CostumeType.Weapon,   CostumeRarity.Uncommon,CostumeTheme.Nature, CostumeUnlock.ByGold,       0,  500,  0,""),
            ("u019","Şimşek Topu",      CostumeType.Weapon,   CostumeRarity.Uncommon,CostumeTheme.Other,  CostumeUnlock.ByChest,      0,    0,  0,""),
            ("u020","Demir Kalkan",     CostumeType.Weapon,   CostumeRarity.Uncommon,CostumeTheme.Mech,   CostumeUnlock.ByGold,       0,  550,  0,""),
            ("u021","Kristal Savaşçı",  CostumeType.Character,CostumeRarity.Uncommon,CostumeTheme.Ice,    CostumeUnlock.ByAchievement,0,    0,  0,"achievement_ice_master"),
            ("u022","Volkan Adamı",     CostumeType.Character,CostumeRarity.Uncommon,CostumeTheme.Fire,   CostumeUnlock.ByAchievement,0,    0,  0,"PATLAMA_UZMANI"),
            ("u023","Siber Ninja",      CostumeType.Character,CostumeRarity.Uncommon,CostumeTheme.Cyber,  CostumeUnlock.ByLevel,     32,    0,  0,""),
            ("u024","Taş Golem",        CostumeType.Character,CostumeRarity.Uncommon,CostumeTheme.Nature, CostumeUnlock.ByLevel,     34,    0,  0,""),
            ("u025","Neon Ceket",       CostumeType.Character,CostumeRarity.Uncommon,CostumeTheme.Cyber,  CostumeUnlock.ByGold,       0,  600,  0,""),
            ("u026","Köpük Denizci",    CostumeType.Character,CostumeRarity.Uncommon,CostumeTheme.Other,  CostumeUnlock.ByChest,      0,    0,  0,""),
            ("u027","Bozkır Eri",       CostumeType.Character,CostumeRarity.Uncommon,CostumeTheme.Nature, CostumeUnlock.ByLevel,     36,    0,  0,""),
            ("u028","Gümüş Şövalye",    CostumeType.Character,CostumeRarity.Uncommon,CostumeTheme.Fantasy,CostumeUnlock.ByGold,       0,  600,  0,""),
            ("u029","Mavi Timsah",      CostumeType.Weapon,   CostumeRarity.Uncommon,CostumeTheme.Nature, CostumeUnlock.ByChest,      0,    0,  0,""),
            ("u030","Kor Bıçağı",       CostumeType.Weapon,   CostumeRarity.Uncommon,CostumeTheme.Fire,   CostumeUnlock.ByLevel,     31,    0,  0,""),
            ("u031","Hologram Silah",   CostumeType.Weapon,   CostumeRarity.Uncommon,CostumeTheme.Cyber,  CostumeUnlock.ByGold,       0,  575,  0,""),
            ("u032","Çelik Ejder",      CostumeType.Weapon,   CostumeRarity.Uncommon,CostumeTheme.Mech,   CostumeUnlock.ByLevel,     33,    0,  0,""),
            ("u033","Kristal Bomba",    CostumeType.Weapon,   CostumeRarity.Uncommon,CostumeTheme.Ice,    CostumeUnlock.ByGold,       0,  550,  0,""),
            ("u034","Kök Dokusu",       CostumeType.Weapon,   CostumeRarity.Uncommon,CostumeTheme.Nature, CostumeUnlock.ByAchievement,0,    0,  0,"GEZEGEN_KATILI"),
            ("u035","Fırtına Yelkeni",  CostumeType.Character,CostumeRarity.Uncommon,CostumeTheme.Other,  CostumeUnlock.ByGold,       0,  600,  0,""),

            // ── RARE (35) ────────────────────────────────────────────────────
            ("r001","Galaksi Gezgini",  CostumeType.Character,CostumeRarity.Rare,    CostumeTheme.Space,  CostumeUnlock.ByLevel,     40,    0,  0,""),
            ("r002","Kara Şövalye",     CostumeType.Character,CostumeRarity.Rare,    CostumeTheme.Dark,   CostumeUnlock.ByLevel,     42,    0,  0,""),
            ("r003","Neon Samuray",     CostumeType.Character,CostumeRarity.Rare,    CostumeTheme.Cyber,  CostumeUnlock.ByLevel,     44,    0,  0,""),
            ("r004","Ejder Avcısı",     CostumeType.Character,CostumeRarity.Rare,    CostumeTheme.Fantasy,CostumeUnlock.ByLevel,     45,    0,  0,""),
            ("r005","Buz Tanrısı",      CostumeType.Character,CostumeRarity.Rare,    CostumeTheme.Ice,    CostumeUnlock.ByGold,       0, 1200,  0,""),
            ("r006","Lav Devi",         CostumeType.Character,CostumeRarity.Rare,    CostumeTheme.Fire,   CostumeUnlock.ByGold,       0, 1200,  0,""),
            ("r007","Kuantum Zırhı",    CostumeType.Character,CostumeRarity.Rare,    CostumeTheme.Cyber,  CostumeUnlock.ByLevel,     48,    0,  0,""),
            ("r008","Orman Tanrısı",    CostumeType.Character,CostumeRarity.Rare,    CostumeTheme.Nature, CostumeUnlock.ByGold,       0, 1300,  0,""),
            ("r009","Karanlık Büyücü",  CostumeType.Character,CostumeRarity.Rare,    CostumeTheme.Dark,   CostumeUnlock.ByAchievement,0,    0,  0,"VETERAN_10"),
            ("r010","Meteor Savaşçısı", CostumeType.Character,CostumeRarity.Rare,    CostumeTheme.Space,  CostumeUnlock.ByLevel,     50,    0,  0,""),
            ("r011","Plazma Tüfek",     CostumeType.Weapon,   CostumeRarity.Rare,    CostumeTheme.Cyber,  CostumeUnlock.ByLevel,     41,    0,  0,""),
            ("r012","Ejder Alevi",      CostumeType.Weapon,   CostumeRarity.Rare,    CostumeTheme.Fantasy,CostumeUnlock.ByGold,       0, 1100,  0,""),
            ("r013","Kara Delik Topu",  CostumeType.Weapon,   CostumeRarity.Rare,    CostumeTheme.Space,  CostumeUnlock.ByLevel,     43,    0,  0,""),
            ("r014","Buz Kalkanı",      CostumeType.Weapon,   CostumeRarity.Rare,    CostumeTheme.Ice,    CostumeUnlock.ByGold,       0, 1100,  0,""),
            ("r015","Kor Bombası",      CostumeType.Weapon,   CostumeRarity.Rare,    CostumeTheme.Fire,   CostumeUnlock.ByAchievement,0,    0,  0,"PATLAMA_UZMANI"),
            ("r016","Nano Bıçak",       CostumeType.Weapon,   CostumeRarity.Rare,    CostumeTheme.Cyber,  CostumeUnlock.ByGold,       0, 1200,  0,""),
            ("r017","Rün Mızrağı",      CostumeType.Weapon,   CostumeRarity.Rare,    CostumeTheme.Fantasy,CostumeUnlock.ByLevel,     46,    0,  0,""),
            ("r018","Gölge Oku",        CostumeType.Weapon,   CostumeRarity.Rare,    CostumeTheme.Dark,   CostumeUnlock.ByChest,      0,    0,  0,""),
            ("r019","Zümrüt Ejder",     CostumeType.Weapon,   CostumeRarity.Rare,    CostumeTheme.Fantasy,CostumeUnlock.ByGold,       0, 1250,  0,""),
            ("r020","Yıldız Kılıcı",    CostumeType.Weapon,   CostumeRarity.Rare,    CostumeTheme.Space,  CostumeUnlock.ByLevel,     47,    0,  0,""),
            ("r021","Titanium Golem",   CostumeType.Character,CostumeRarity.Rare,    CostumeTheme.Mech,   CostumeUnlock.ByLevel,     52,    0,  0,""),
            ("r022","Işık Hızı",        CostumeType.Character,CostumeRarity.Rare,    CostumeTheme.Space,  CostumeUnlock.ByGold,       0, 1400,  0,""),
            ("r023","Deniz Canavarı",   CostumeType.Character,CostumeRarity.Rare,    CostumeTheme.Nature, CostumeUnlock.ByAchievement,0,    0,  0,"SOSYAL_KELEBEK"),
            ("r024","Fırtına Tanrısı",  CostumeType.Character,CostumeRarity.Rare,    CostumeTheme.Myth,   CostumeUnlock.ByLevel,     54,    0,  0,""),
            ("r025","Kızıl Şaman",      CostumeType.Character,CostumeRarity.Rare,    CostumeTheme.Myth,   CostumeUnlock.ByGold,       0, 1400,  0,""),
            ("r026","Siber Samurai",    CostumeType.Character,CostumeRarity.Rare,    CostumeTheme.Cyber,  CostumeUnlock.ByLevel,     56,    0,  0,""),
            ("r027","Biyonik Savaşçı",  CostumeType.Character,CostumeRarity.Rare,    CostumeTheme.Mech,   CostumeUnlock.ByGold,       0, 1500,  0,""),
            ("r028","Vorteks Tüfek",    CostumeType.Weapon,   CostumeRarity.Rare,    CostumeTheme.Space,  CostumeUnlock.ByChest,      0,    0,  0,""),
            ("r029","Şaman Asası",      CostumeType.Weapon,   CostumeRarity.Rare,    CostumeTheme.Myth,   CostumeUnlock.ByLevel,     49,    0,  0,""),
            ("r030","Titan Çekici",     CostumeType.Weapon,   CostumeRarity.Rare,    CostumeTheme.Mech,   CostumeUnlock.ByGold,       0, 1300,  0,""),
            ("r031","Rüzgar Bıçağı",    CostumeType.Weapon,   CostumeRarity.Rare,    CostumeTheme.Nature, CostumeUnlock.ByAchievement,0,    0,  0,"CEVRECI"),
            ("r032","Kristal Asa",      CostumeType.Weapon,   CostumeRarity.Rare,    CostumeTheme.Fantasy,CostumeUnlock.ByLevel,     51,    0,  0,""),
            ("r033","Lazer Tüfek",      CostumeType.Weapon,   CostumeRarity.Rare,    CostumeTheme.Cyber,  CostumeUnlock.ByGold,       0, 1350,  0,""),
            ("r034","Karanlık Rün",     CostumeType.Weapon,   CostumeRarity.Rare,    CostumeTheme.Dark,   CostumeUnlock.ByLevel,     53,    0,  0,""),
            ("r035","Mitoloji Okçusu",  CostumeType.Character,CostumeRarity.Rare,    CostumeTheme.Myth,   CostumeUnlock.ByAchievement,0,    0,  0,"SAVAS_MAKINESI"),

            // ── EPIC (25) ────────────────────────────────────────────────────
            ("e001","Nebula Savaşçısı", CostumeType.Character,CostumeRarity.Epic,    CostumeTheme.Space,  CostumeUnlock.ByLevel,     60,    0,  0,""),
            ("e002","Ejder Lordu",      CostumeType.Character,CostumeRarity.Epic,    CostumeTheme.Fantasy,CostumeUnlock.ByLevel,     63,    0,  0,""),
            ("e003","Siber Tanrı",      CostumeType.Character,CostumeRarity.Epic,    CostumeTheme.Cyber,  CostumeUnlock.ByGem,        0,    0, 50,""),
            ("e004","Ölüm Ruhu",        CostumeType.Character,CostumeRarity.Epic,    CostumeTheme.Dark,   CostumeUnlock.ByLevel,     66,    0,  0,""),
            ("e005","Volkan Tanrısı",   CostumeType.Character,CostumeRarity.Epic,    CostumeTheme.Fire,   CostumeUnlock.ByGem,        0,    0, 60,""),
            ("e006","Buz Fırtınası",    CostumeType.Character,CostumeRarity.Epic,    CostumeTheme.Ice,    CostumeUnlock.ByLevel,     70,    0,  0,""),
            ("e007","Ormantanrı",       CostumeType.Character,CostumeRarity.Epic,    CostumeTheme.Nature, CostumeUnlock.ByAchievement,0,    0,  0,"EFSANE"),
            ("e008","Titan Zırhı",      CostumeType.Character,CostumeRarity.Epic,    CostumeTheme.Mech,   CostumeUnlock.ByGem,        0,    0, 70,""),
            ("e009","Olimpos Tanrısı",  CostumeType.Character,CostumeRarity.Epic,    CostumeTheme.Myth,   CostumeUnlock.ByLevel,     73,    0,  0,""),
            ("e010","Kuantum Gölgesi",  CostumeType.Character,CostumeRarity.Epic,    CostumeTheme.Cyber,  CostumeUnlock.ByGem,        0,    0, 75,""),
            ("e011","Galaktik İmparator",CostumeType.Character,CostumeRarity.Epic,   CostumeTheme.Space,  CostumeUnlock.ByLevel,     76,    0,  0,""),
            ("e012","Kadim Ejder",      CostumeType.Character,CostumeRarity.Epic,    CostumeTheme.Fantasy,CostumeUnlock.ByAchievement,0,    0,  0,"KARA_DELIK_USTASI"),
            ("e013","Neon İblis",       CostumeType.Character,CostumeRarity.Epic,    CostumeTheme.Dark,   CostumeUnlock.ByGem,        0,    0, 80,""),
            ("e014","Plazma Tanrısı",   CostumeType.Weapon,   CostumeRarity.Epic,    CostumeTheme.Cyber,  CostumeUnlock.ByLevel,     61,    0,  0,""),
            ("e015","Ejder Nefesi",     CostumeType.Weapon,   CostumeRarity.Epic,    CostumeTheme.Fantasy,CostumeUnlock.ByGem,        0,    0, 50,""),
            ("e016","Karanlık Yıldız",  CostumeType.Weapon,   CostumeRarity.Epic,    CostumeTheme.Dark,   CostumeUnlock.ByLevel,     64,    0,  0,""),
            ("e017","Volkan Topu",      CostumeType.Weapon,   CostumeRarity.Epic,    CostumeTheme.Fire,   CostumeUnlock.ByGem,        0,    0, 55,""),
            ("e018","Buz Kristali",     CostumeType.Weapon,   CostumeRarity.Epic,    CostumeTheme.Ice,    CostumeUnlock.ByAchievement,0,    0,  0,"DOKUNULMAZ"),
            ("e019","Nano Swarm",       CostumeType.Weapon,   CostumeRarity.Epic,    CostumeTheme.Mech,   CostumeUnlock.ByLevel,     68,    0,  0,""),
            ("e020","Rün Patlaması",    CostumeType.Weapon,   CostumeRarity.Epic,    CostumeTheme.Fantasy,CostumeUnlock.ByGem,        0,    0, 65,""),
            ("e021","Nebula Bombası",   CostumeType.Weapon,   CostumeRarity.Epic,    CostumeTheme.Space,  CostumeUnlock.ByLevel,     71,    0,  0,""),
            ("e022","Titan Lazer",      CostumeType.Weapon,   CostumeRarity.Epic,    CostumeTheme.Mech,   CostumeUnlock.ByGem,        0,    0, 70,""),
            ("e023","Mitoloji Zırhı",   CostumeType.Character,CostumeRarity.Epic,    CostumeTheme.Myth,   CostumeUnlock.ByAchievement,0,    0,  0,"FIRTINA_TANRISI"),
            ("e024","Kristal Golem",    CostumeType.Character,CostumeRarity.Epic,    CostumeTheme.Ice,    CostumeUnlock.ByGem,        0,    0, 75,""),
            ("e025","Karga Kral",       CostumeType.Character,CostumeRarity.Epic,    CostumeTheme.Dark,   CostumeUnlock.ByLevel,     80,    0,  0,""),

            // ── LEGENDARY (15) ───────────────────────────────────────────────
            ("l001","Kozmik Efendi",    CostumeType.Character,CostumeRarity.Legendary,CostumeTheme.Space, CostumeUnlock.ByLevel,    100,    0,   0,""),
            ("l002","Ejder İmparatoru", CostumeType.Character,CostumeRarity.Legendary,CostumeTheme.Fantasy,CostumeUnlock.ByGem,      0,    0, 200,""),
            ("l003","Karanlık Tanrı",   CostumeType.Character,CostumeRarity.Legendary,CostumeTheme.Dark,  CostumeUnlock.ByAchievement,0,   0,   0,"COSMIC_100"),
            ("l004","Kıyamet Lordu",    CostumeType.Character,CostumeRarity.Legendary,CostumeTheme.Dark,  CostumeUnlock.ByGem,       0,    0, 250,""),
            ("l005","Zaman Efendisi",   CostumeType.Character,CostumeRarity.Legendary,CostumeTheme.Myth,  CostumeUnlock.ByLevel,    101,    0,   0,""),
            ("l006","Evren Savaşçısı",  CostumeType.Character,CostumeRarity.Legendary,CostumeTheme.Space, CostumeUnlock.ByGem,       0,    0, 300,""),
            ("l007","Kadim Dev",        CostumeType.Character,CostumeRarity.Legendary,CostumeTheme.Myth,  CostumeUnlock.ByAchievement,0,   0,   0,"GALAKSININ_EFSANESI"),
            ("l008","Biyonik Tanrı",    CostumeType.Character,CostumeRarity.Legendary,CostumeTheme.Mech,  CostumeUnlock.ByGem,       0,    0, 250,""),
            ("l009","Fenix Savaşçısı",  CostumeType.Character,CostumeRarity.Legendary,CostumeTheme.Fire,  CostumeUnlock.ByLevel,    102,    0,   0,""),
            ("l010","Kozmik Yıkıcı",    CostumeType.Weapon,   CostumeRarity.Legendary,CostumeTheme.Space, CostumeUnlock.ByLevel,    100,    0,   0,""),
            ("l011","Tanrı Kılıcı",     CostumeType.Weapon,   CostumeRarity.Legendary,CostumeTheme.Myth,  CostumeUnlock.ByGem,       0,    0, 200,""),
            ("l012","Ejder Kalbi",      CostumeType.Weapon,   CostumeRarity.Legendary,CostumeTheme.Fantasy,CostumeUnlock.ByAchievement,0,  0,   0,"EJDER_AVCI"),
            ("l013","Kara Delik Topu X",CostumeType.Weapon,   CostumeRarity.Legendary,CostumeTheme.Space, CostumeUnlock.ByGem,       0,    0, 250,""),
            ("l014","Kıyamet Çekici",   CostumeType.Weapon,   CostumeRarity.Legendary,CostumeTheme.Dark,  CostumeUnlock.ByAchievement,0,   0,   0,"HOME_RUN"),
            ("l015","Yaratıcı Gücü",    CostumeType.Weapon,   CostumeRarity.Legendary,CostumeTheme.Myth,  CostumeUnlock.ByLevel,    103,    0,   0,""),
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
            CostumeUnlock.Default      => "Başlangıçta açık",
            CostumeUnlock.ByLevel      => $"Seviye {lv} gerekli",
            CostumeUnlock.ByGold       => $"{gold} Gold",
            CostumeUnlock.ByGem        => $"{gem} Gem",
            CostumeUnlock.ByChest      => "Sandıktan düşer",
            CostumeUnlock.ByAchievement=> $"Başarım: {ach}",
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
