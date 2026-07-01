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
            // ── SAVAŞ (10) ──────────────────────────────────────────────────
            ("FIRST_BLOOD",     "İlk Kan",           "İlk galibiyetini kazan",                              AchievementRarity.Common,    AchievementTriggerType.SingleUnlock, 1,    false, ""),
            ("VETERAN_10",      "Veteran",            "10 maç kazan",                                        AchievementRarity.Rare,      AchievementTriggerType.Cumulative,   10,   false, "r009"),
            ("SAVAS_MAKINESI",  "Savaş Makinesi",     "25 maç kazan",                                        AchievementRarity.Rare,      AchievementTriggerType.Cumulative,   25,   false, "r035"),
            ("EFSANE",          "Efsane",             "50 maç kazan",                                        AchievementRarity.Epic,      AchievementTriggerType.Cumulative,   50,   false, "e007"),
            ("COSMIC_100",      "Kozmik Efendi",      "100 maç kazan",                                       AchievementRarity.Legendary, AchievementTriggerType.Cumulative,   100,  false, "l003"),
            ("FLAWLESS",        "Flawless",           "Hiç hasar almadan bir maç kazan",                     AchievementRarity.Epic,      AchievementTriggerType.SingleUnlock, 1,    false, ""),
            ("UNDERDOG",        "Underdog",           "Tüm düşmanlar fazla HP'deyken kazan",                 AchievementRarity.Rare,      AchievementTriggerType.SingleUnlock, 1,    false, ""),
            ("HIZLI_BITIR",     "Hızlı Bitir",        "5 turda bir maç kazan",                               AchievementRarity.Rare,      AchievementTriggerType.SingleUnlock, 1,    false, ""),
            ("SAMPIYONLAR",     "Şampiyonlar Ligi",   "8 kişilik lobbyde kazan",                             AchievementRarity.Epic,      AchievementTriggerType.SingleUnlock, 1,    false, ""),
            ("SON_NEFES",       "Son Nefes",          "1 HP'de maçı kazan",                                  AchievementRarity.Legendary, AchievementTriggerType.SingleUnlock, 1,    true,  ""),

            // ── İSTATİSTİK (10) ─────────────────────────────────────────────
            ("DAMAGE_1K",       "Hasarcı",            "Toplam 1.000 hasar ver",                              AchievementRarity.Common,    AchievementTriggerType.Cumulative,   1000,  false, ""),
            ("DAMAGE_50K",      "Yıkım Makinesi",     "Toplam 50.000 hasar ver",                             AchievementRarity.Rare,      AchievementTriggerType.Cumulative,   50000, false, ""),
            ("DAMAGE_250K",     "Atom Bombası",       "Toplam 250.000 hasar ver",                            AchievementRarity.Epic,      AchievementTriggerType.Cumulative,   250000,false, ""),
            ("SHOTS_100",       "Çılgın Atıcı",       "100 atış yap",                                        AchievementRarity.Common,    AchievementTriggerType.Cumulative,   100,   false, ""),
            ("SHOTS_1K",        "Mermi Fabrikası",    "1.000 atış yap",                                      AchievementRarity.Rare,      AchievementTriggerType.Cumulative,   1000,  false, ""),
            ("TETIKCI",         "Tetikçi",            "Tek maçta 30 atış yap",                               AchievementRarity.Rare,      AchievementTriggerType.Cumulative,   30,    false, ""),
            ("ISABETLI",        "İsabetli",           "%80 isabet oranıyla bitir (min 10 atış)",             AchievementRarity.Epic,      AchievementTriggerType.SingleUnlock, 1,    false, ""),
            ("SAGLAMDURUG",     "Sağlam Duruş",       "Toplam 10.000 hasar al hayatta kal",                  AchievementRarity.Rare,      AchievementTriggerType.Cumulative,   10000, false, ""),
            ("GEZEGEN_KATILI",  "Gezegen Katili",     "Toplamda 10 gezegen yok et",                          AchievementRarity.Epic,      AchievementTriggerType.Cumulative,   10,    false, "u034"),
            ("GALAKSI_TAMIRCISI","Galaksi Tamircisi", "Toplamda 100 maç oyna",                               AchievementRarity.Common,    AchievementTriggerType.Cumulative,   100,   false, ""),

            // ── SİLAH (10) ──────────────────────────────────────────────────
            ("TABANCALI",       "Tabancalı",          "Tabancayla 50 düşman vur",                            AchievementRarity.Common,    AchievementTriggerType.Cumulative,   50,   false, ""),
            ("KESKIN_NISANCI",  "Keskin Nişancı",     "Tabancayla 10 headshot yap",                          AchievementRarity.Rare,      AchievementTriggerType.Cumulative,   10,   false, ""),
            ("ROKETCI",         "Roketçi",            "RPG ile tek atışta 3+ düşmana hasar ver",             AchievementRarity.Rare,      AchievementTriggerType.SingleUnlock, 1,    false, "r015"),
            ("PATLAMA_UZMANI",  "Patlama Uzmanı",     "RPG ile toplam 100 atış yap",                         AchievementRarity.Rare,      AchievementTriggerType.Cumulative,   100,  false, "u022"),
            ("SACMA_YAGMURU",   "Saçma Yağmuru",      "Shotgun tüm pellet'leri isabet ettir",                AchievementRarity.Epic,      AchievementTriggerType.SingleUnlock, 1,    false, ""),
            ("POMPACI",         "Pompacı",            "Shotgun ile 5 düşmanı arka arkaya vur",               AchievementRarity.Rare,      AchievementTriggerType.SingleUnlock, 1,    false, ""),
            ("EL_BOMBACI",      "El Bombacı",         "El bombasıyla 2+ düşmanı tek vur",                    AchievementRarity.Rare,      AchievementTriggerType.SingleUnlock, 1,    false, ""),
            ("PIM_CEKICI",      "Pim Çekici",         "El bombası ile 25 atış yap",                          AchievementRarity.Common,    AchievementTriggerType.Cumulative,   25,   false, ""),
            ("BOMBA_IMHA",      "Bomba İmha",         "Bomba ile gezegen yüzeyini yok et",                   AchievementRarity.Epic,      AchievementTriggerType.SingleUnlock, 1,    false, ""),
            ("TAM_CEPHANE",     "Tam Cephane",        "Bir maçta 5 silahın tamamını kullan",                 AchievementRarity.Epic,      AchievementTriggerType.SingleUnlock, 1,    false, ""),

            // ── SKİLL (10) ──────────────────────────────────────────────────
            ("KARA_DELIK_USTASI","Kara Delik Ustası", "Black Hole ile 3 düşmanı tek çek",                   AchievementRarity.Epic,      AchievementTriggerType.SingleUnlock, 1,    false, "e012"),
            ("OLAY_UFKU",       "Olay Ufku",          "Black Hole ile 50 düşman çek",                        AchievementRarity.Rare,      AchievementTriggerType.Cumulative,   50,   false, ""),
            ("ISINLANAN",       "Işınlanan",          "Teleport ile düşmanın arkasına geç ve vur",           AchievementRarity.Rare,      AchievementTriggerType.SingleUnlock, 1,    false, ""),
            ("KUANTUM",         "Kuantum",            "Teleport'u tek maçta 5 kez kullan",                   AchievementRarity.Common,    AchievementTriggerType.Cumulative,   5,    false, ""),
            ("DOKUNULMAZ",      "Dokunulmaz",         "Shield ile 500 hasar blokla",                         AchievementRarity.Rare,      AchievementTriggerType.Cumulative,   500,  false, "e018"),
            ("KALKAN_DUVARI",   "Kalkan Duvarı",      "Shield ile 3 saldırıyı blokla",                       AchievementRarity.Rare,      AchievementTriggerType.SingleUnlock, 1,    false, ""),
            ("CEKIC_ZAMANI",    "Çekiç Zamanı",       "Bat Hammer ile düşmanı gezegen dışına fırlat",        AchievementRarity.Epic,      AchievementTriggerType.SingleUnlock, 1,    false, ""),
            ("HOME_RUN",        "Home Run",           "Bat Hammer vurduğun düşman başkasına çarpsın",        AchievementRarity.Legendary, AchievementTriggerType.SingleUnlock, 1,    false, "l014"),
            ("SUPER_KAHRAMAN",  "Süper Kahraman",     "Super Jump ile düşmanın üstüne inerek hasar ver",     AchievementRarity.Rare,      AchievementTriggerType.SingleUnlock, 1,    false, ""),
            ("YORUNGE",         "Yörünge",            "Super Jump ile gezegen değiştirerek atış yap",        AchievementRarity.Common,    AchievementTriggerType.SingleUnlock, 1,    false, ""),

            // ── SOSYAL (10) ─────────────────────────────────────────────────
            ("SOSYAL_KELEBEK",  "Sosyal Kelebek",     "8 farklı oyuncuyla maç oyna",                         AchievementRarity.Common,    AchievementTriggerType.Cumulative,   8,    false, "r023"),
            ("REKABETCI",       "Rekabetçi",          "Sıralama maçında ilk 3'e gir",                        AchievementRarity.Rare,      AchievementTriggerType.SingleUnlock, 1,    false, ""),
            ("KOZMIK_AVCI",     "Kozmik Avcı",        "Leaderboard top 10'a gir",                            AchievementRarity.Epic,      AchievementTriggerType.SingleUnlock, 1,    false, ""),
            ("BIR_NUMARA",      "Bir Numara",         "Leaderboard zirvesine çık",                           AchievementRarity.Legendary, AchievementTriggerType.SingleUnlock, 1,    true,  ""),
            ("DUELLO_SAMPIYONU","Düello Şampiyonu",   "1v1 modunda 10 galibiyet al",                         AchievementRarity.Rare,      AchievementTriggerType.Cumulative,   10,   false, ""),
            ("OGRETMEN",        "Öğretmen",           "Yeni oyuncuyu tutorial'dan geçir",                    AchievementRarity.Common,    AchievementTriggerType.SingleUnlock, 1,    false, ""),
            ("KOZMIK_EKIP",     "Kozmik Ekip",        "Aynı 3 kişiyle 5 maç oyna",                           AchievementRarity.Rare,      AchievementTriggerType.Cumulative,   5,    false, ""),
            ("INTIKAM",         "İntikam",            "Seni öldüreni bir sonraki maçta yenebilir",           AchievementRarity.Rare,      AchievementTriggerType.SingleUnlock, 1,    false, ""),
            ("HERKESE_MEYDAN",  "Herkese Meydan",     "Aynı maçta 7 farklı oyuncuya hasar ver",             AchievementRarity.Epic,      AchievementTriggerType.SingleUnlock, 1,    false, ""),
            ("GALAKSININ_EFSANESI","Galaksinin Efsanesi","Tüm 49 achievement'ı tamamla",                     AchievementRarity.Legendary, AchievementTriggerType.SingleUnlock, 1,    false, "l007"),
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
