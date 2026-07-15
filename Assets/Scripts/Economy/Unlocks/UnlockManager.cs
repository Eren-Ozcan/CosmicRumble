using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using CosmicRumble.Achievements;
using CosmicRumble.Cloud;

namespace CosmicRumble.Economy
{
    public class UnlockManager : MonoBehaviour
    {
        public static UnlockManager Instance { get; private set; }

        public event Action<UnlockableItem> OnItemUnlocked;

        [Serializable]
        private class SaveData
        {
            public List<string> unlockedIds = new List<string>();
        }

        private SaveData     _data = new SaveData();
        [SerializeField] private UnlockDatabase _db;
        private string SavePath => Path.Combine(Application.persistentDataPath, "unlocks.json");

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            var loadedDb = Resources.Load<UnlockDatabase>("Economy/UnlockDatabase");
            if (loadedDb != null) _db = loadedDb;
            if (_db == null)
            {
#if UNITY_EDITOR
                Debug.LogError("[UnlockManager] UnlockDatabase not found at Resources/Economy/UnlockDatabase — assign it in the Inspector as fallback");
#endif
            }

            Load();
            UnlockDefaults();
        }

        private void Start()
        {
            if (PlayerLevelManager.Instance != null)
            {
                PlayerLevelManager.Instance.OnLevelUp += OnLevelUp;
                // Seviye OnLevelUp tetiklenmeden de yükselmiş olabilir (cloud restore ile
                // progress.json'ın yeni cihaza inmesi, ya da UnlockManager'ın henüz var
                // olmadığı bir dönemde kazanılan seviyeler). OnLevelUp yalnızca artış
                // ANINDA çalıştığı için burada bir kez mevcut seviyeye kadar tarama yapılır.
                CatchUpToLevel(PlayerLevelManager.Instance.GetProgress().currentLevel);
            }
        }

        private void OnDestroy()
        {
            if (PlayerLevelManager.Instance != null)
                PlayerLevelManager.Instance.OnLevelUp -= OnLevelUp;
        }

        private void UnlockDefaults()
        {
            if (_db == null) return;
            foreach (var item in _db.allItems)
            {
                if (item != null && item.isDefault && !_data.unlockedIds.Contains(item.itemId))
                    ForceUnlock(item);
            }
        }

        private void OnLevelUp(int oldLevel, int newLevel)
        {
            if (_db == null) return;
            for (int lv = oldLevel + 1; lv <= newLevel; lv++)
            {
                foreach (var item in _db.GetUnlockedAtLevel(lv))
                {
                    if (!_data.unlockedIds.Contains(item.itemId))
                        ForceUnlock(item);
                }
            }
        }

        private void CatchUpToLevel(int level)
        {
            if (_db == null) return;
            foreach (var item in _db.allItems)
            {
                if (item != null && item.unlockMethod == UnlockMethod.ByLevel
                    && item.requiredLevel <= level && !_data.unlockedIds.Contains(item.itemId))
                    ForceUnlock(item);
            }
        }

        public bool IsUnlocked(string itemId) => _data.unlockedIds.Contains(itemId);

        /// <summary>Veritabanındaki item tanımına salt-okunur erişim (UI'da koşul göstermek için).</summary>
        public UnlockableItem GetItemById(string itemId) => _db != null ? _db.GetById(itemId) : null;

        public UnlockCheckResult CanUnlock(string itemId)
        {
            var result = new UnlockCheckResult();
            if (_db == null) return result;

            var item = _db.GetById(itemId);
            if (item == null) return result;

            int currentLevel = PlayerLevelManager.Instance != null
                ? PlayerLevelManager.Instance.GetProgress().currentLevel : 1;

            // Level check
            result.isLevelMet = currentLevel >= item.requiredLevel || item.requiredLevel <= 0;
            result.missingLevel = result.isLevelMet ? 0 : item.requiredLevel - currentLevel;

            // Currency check
            long gold = CurrencyManager.Instance != null ? CurrencyManager.Instance.Get(CurrencyType.Gold) : 0;
            long gem  = CurrencyManager.Instance != null ? CurrencyManager.Instance.Get(CurrencyType.Gem)  : 0;

            bool goldOk = gold >= item.goldCost;
            bool gemOk  = gem  >= item.gemCost;
            result.isCurrencyMet = goldOk && gemOk;
            result.missingGold   = goldOk ? 0 : item.goldCost - gold;
            result.missingGem    = gemOk  ? 0 : item.gemCost  - gem;

            // Achievement check
            if (!string.IsNullOrEmpty(item.requiredAchievementId) && AchievementManager.Instance != null)
                result.isAchievementMet = AchievementManager.Instance.IsUnlocked(item.requiredAchievementId);
            else
                result.isAchievementMet = string.IsNullOrEmpty(item.requiredAchievementId);

            result.canUnlock = result.isLevelMet && result.isCurrencyMet && result.isAchievementMet;
            return result;
        }

        public bool TryUnlock(string itemId)
        {
            if (IsUnlocked(itemId)) return false;
            var check = CanUnlock(itemId);
            if (!check.canUnlock) return false;

            var item = _db.GetById(itemId);
            if (item == null) return false;

            // Deduct currency
            if (item.goldCost > 0) CurrencyManager.Instance.Spend(CurrencyType.Gold, item.goldCost);
            if (item.gemCost  > 0) CurrencyManager.Instance.Spend(CurrencyType.Gem,  item.gemCost);

            ForceUnlock(item);
            return true;
        }

        public List<UnlockableItem> GetAllUnlocked()
        {
            var result = new List<UnlockableItem>();
            if (_db == null) return result;
            foreach (var item in _db.allItems)
                if (item != null && _data.unlockedIds.Contains(item.itemId))
                    result.Add(item);
            return result;
        }

        private void ForceUnlock(UnlockableItem item)
        {
            if (!_data.unlockedIds.Contains(item.itemId))
                _data.unlockedIds.Add(item.itemId);
            Save();
            OnItemUnlocked?.Invoke(item);
        }

        private void Save()
        {
            File.WriteAllText(SavePath, JsonUtility.ToJson(_data, true));
            CloudSaveManager.Instance?.QueuePush("unlocks", SavePath);
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
                Debug.LogWarning($"[UnlockManager] Save load failed, using defaults. {e.Message}");
#endif
                _data = new SaveData();
            }
        }
    }
}
