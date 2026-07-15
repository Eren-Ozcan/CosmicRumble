using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using CosmicRumble.Cloud;
namespace CosmicRumble.Economy
{
    public class ChestManager : MonoBehaviour
    {
        public static ChestManager Instance { get; private set; }

        // type, gold, gem, costumeId (empty string = no costume)
        public event Action<ChestType, long, long, string> OnChestGranted;

        // ─── Save data ────────────────────────────────────────────────────────
        [Serializable]
        private class SaveData
        {
            public int    todaysCount    = 0;
            public string lastResetDate  = "";
        }

        private SaveData  _data = new SaveData();
        [SerializeField] private ChestConfig _cfg;
        private string SavePath => Path.Combine(Application.persistentDataPath, "chests.json");

        // ─── Lifecycle ────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            var loadedCfg = Resources.Load<ChestConfig>("Economy/ChestConfig");
            if (loadedCfg != null) _cfg = loadedCfg;
            if (_cfg == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning("[ChestManager] ChestConfig not found at Resources/Economy/ChestConfig — assign it in the Inspector as fallback");
#endif
            }

            Load();
        }

        private void Start()
        {
            CheckDailyReset();
        }

        // ─── Public API ───────────────────────────────────────────────────────
        public int GetTodaysChestCount()  => _data.todaysCount;
        public int GetRemainingChests()   => Mathf.Max(0, (_cfg?.dailyChestLimit ?? 3) - _data.todaysCount);

        /// <summary>
        /// Sadece galibiyette çağrılır. Günlük limiti aşıldıysa silent return.
        /// </summary>
        public void TryGrantChest(bool isWinner)
        {
            if (!isWinner) return;

            CheckDailyReset();

            int limit = _cfg != null ? _cfg.dailyChestLimit : 3;
            if (_data.todaysCount >= limit)
            {
#if UNITY_EDITOR
                Debug.Log("[ChestManager] Daily chest limit reached.");
#endif
                return;
            }

            ChestType type = RollChestType();

            _data.todaysCount++;
            Save();

            GrantChestContents(type);
        }

        // ─── Mağaza satın alma (Gold/Gem harcama yolu) ────────────────────────

        /// <summary>Sandığın mağaza Gold fiyatı (0 = Gold ile satılmaz).</summary>
        public long GetChestGoldPrice(ChestType type) =>
            type == ChestType.Rare ? (_cfg != null ? _cfg.rareChestGoldPrice : 800) : 0;

        /// <summary>Sandığın mağaza Gem fiyatı (0 = Gem ile satılmaz).</summary>
        public long GetChestGemPrice(ChestType type) =>
            type == ChestType.Epic ? (_cfg != null ? _cfg.epicChestGemPrice : 25) : 0;

        /// <summary>
        /// Mağazadan sandık satın alma — TryGrantChest'in aksine günlük galibiyet limitine
        /// dahil DEĞİLDİR ve onu saymaz (limit "maç kazanarak bedava sandık" hakkıdır,
        /// satın alma ayrı bir yol). Bedel peşin düşer; bakiye yetmiyorsa hiçbir şey olmaz.
        /// Kostüm mağazası gelene kadar ekonomideki tek Gold/Gem harcama noktası budur.
        /// </summary>
        public bool TryPurchaseChest(ChestType type)
        {
            if (CurrencyManager.Instance == null) return false;

            long goldPrice = GetChestGoldPrice(type);
            long gemPrice  = GetChestGemPrice(type);
            if (goldPrice <= 0 && gemPrice <= 0) return false; // bu tip satılık değil

            // Önce her iki bakiyeyi de doğrula, sonra düş — yarım harcama olmasın
            if (CurrencyManager.Instance.Get(CurrencyType.Gold) < goldPrice) return false;
            if (CurrencyManager.Instance.Get(CurrencyType.Gem)  < gemPrice)  return false;

            if (goldPrice > 0 && !CurrencyManager.Instance.Spend(CurrencyType.Gold, goldPrice)) return false;
            if (gemPrice  > 0 && !CurrencyManager.Instance.Spend(CurrencyType.Gem,  gemPrice))  return false;

            GrantChestContents(type);
            return true;
        }

        /// <summary>Sandık içeriğini üretip verir ve OnChestGranted'ı ateşler (limitten bağımsız).</summary>
        private void GrantChestContents(ChestType type)
        {
            long gold      = RollGold(type);
            long gem       = GetGem(type);
            string costume = TryRollCostume(type);

            if (CurrencyManager.Instance != null)
            {
                if (gold > 0) CurrencyManager.Instance.Add(CurrencyType.Gold, gold);
                if (gem  > 0) CurrencyManager.Instance.Add(CurrencyType.Gem,  gem);
            }

            if (!string.IsNullOrEmpty(costume) && CostumeManager.Instance != null)
                CostumeManager.Instance.GrantCostumeById(costume);

            OnChestGranted?.Invoke(type, gold, gem, costume ?? "");
#if UNITY_EDITOR
            Debug.Log($"[ChestManager] Chest granted: {type} — Gold:{gold} Gem:{gem} Costume:{costume ?? "none"}");
#endif
        }

        // ─── Internal helpers ─────────────────────────────────────────────────
        private ChestType RollChestType()
        {
            float common = _cfg != null ? _cfg.commonChance : 65f;
            float rare   = _cfg != null ? _cfg.rareChance   : 25f;
            // epic is remainder

            float roll = UnityEngine.Random.Range(0f, 100f);
            if (roll < common) return ChestType.Common;
            if (roll < common + rare) return ChestType.Rare;
            return ChestType.Epic;
        }

        private long RollGold(ChestType type)
        {
            if (_cfg == null) return 0;
            return type switch
            {
                ChestType.Common => UnityEngine.Random.Range(_cfg.commonGoldMin, _cfg.commonGoldMax + 1),
                ChestType.Rare   => UnityEngine.Random.Range(_cfg.rareGoldMin,   _cfg.rareGoldMax   + 1),
                ChestType.Epic   => UnityEngine.Random.Range(_cfg.epicGoldMin,   _cfg.epicGoldMax   + 1),
                _                => 0
            };
        }

        private long GetGem(ChestType type)
        {
            if (_cfg == null) return 0;
            return type switch
            {
                ChestType.Rare => _cfg.rareGem,
                ChestType.Epic => _cfg.epicGem,
                _              => 0
            };
        }

        private string TryRollCostume(ChestType type)
        {
            if (_cfg == null) return null;

            float prob = type switch
            {
                ChestType.Rare => _cfg.rareCosstumeProbability,
                ChestType.Epic => _cfg.epicCostumeProbability,
                _              => 0f
            };

            if (prob <= 0f || UnityEngine.Random.value > prob) return null;

            return PickUnownedCostume(type);
        }

        private string PickUnownedCostume(ChestType chestType)
        {
            var db = Resources.Load<CostumeDatabase>("Economy/CostumeDatabase");
            if (db == null || CostumeManager.Instance == null) return null;

            // Sadece ByChest unlock yöntemi + sahip olunmayan + Common/Uncommon rarity
            var candidates = new List<CostumeDefinition>();
            foreach (var c in db.allCostumes)
            {
                if (c == null) continue;
                if (c.unlockMethod != CostumeUnlock.ByChest) continue;
                if (CostumeManager.Instance.IsOwned(c.costumeId)) continue;
                if (c.rarity != CostumeRarity.Common && c.rarity != CostumeRarity.Uncommon) continue;
                candidates.Add(c);
            }

            if (candidates.Count == 0) return null;
            return candidates[UnityEngine.Random.Range(0, candidates.Count)].costumeId;
        }

        private void CheckDailyReset()
        {
            string today = DateTime.UtcNow.Date.ToString("o");
            if (_data.lastResetDate != today)
            {
                _data.todaysCount   = 0;
                _data.lastResetDate = today;
                Save();
            }
        }

        // ─── Save/Load ────────────────────────────────────────────────────────
        private void Save()
        {
            File.WriteAllText(SavePath, JsonUtility.ToJson(_data, true));
            CloudSaveManager.Instance?.QueuePush("chests", SavePath);
        }

        private void Load()
        {
            try
            {
                if (File.Exists(SavePath))
                    _data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
                else
                    _data = new SaveData();
            }
            catch (Exception e)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"[ChestManager] Save load failed, using defaults. {e.Message}");
#endif
                _data = new SaveData();
            }
        }
    }
}
