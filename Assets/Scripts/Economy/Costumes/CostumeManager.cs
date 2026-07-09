using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using CosmicRumble.Achievements;
using CosmicRumble.Cloud;

namespace CosmicRumble.Economy
{
    public class CostumeManager : MonoBehaviour
    {
        public static CostumeManager Instance { get; private set; }

        public event Action<CostumeDefinition> OnCostumePurchased;
        public event Action<CostumeDefinition> OnCostumeEquipped;

        [Serializable]
        private class SaveData
        {
            public List<string> ownedIds     = new List<string>();
            public string       equippedChar = "";
            public string       equippedWeapon= "";
        }

        private SaveData       _data = new SaveData();
        private CostumeDatabase _db;
        private string SavePath => Path.Combine(Application.persistentDataPath, "costumes.json");

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _db = Resources.Load<CostumeDatabase>("Economy/CostumeDatabase");
            if (_db == null)
            {
#if UNITY_EDITOR
                Debug.LogError("[CostumeManager] CostumeDatabase not found at Resources/Economy/CostumeDatabase");
#endif
            }

            Load();
            GrantDefaultCostumes();
        }

        /// <summary>
        /// unlockMethod == Default olan kostümler baştan sahiplidir ("Unlocked from start").
        /// Sessizce eklenir — açılışta ödül popup'ı tetiklememek için OnCostumePurchased ateşlenmez.
        /// </summary>
        private void GrantDefaultCostumes()
        {
            if (_db == null) return;
            bool changed = false;
            foreach (var c in _db.allCostumes)
            {
                if (c != null && c.unlockMethod == CostumeUnlock.Default
                    && !_data.ownedIds.Contains(c.costumeId))
                {
                    _data.ownedIds.Add(c.costumeId);
                    changed = true;
                }
            }
            if (changed) Save();
        }

        private void Start()
        {
            if (AchievementManager.Instance != null)
                AchievementManager.Instance.OnAchievementUnlocked += OnAchievementUnlocked;
        }

        private void OnDestroy()
        {
            if (AchievementManager.Instance != null)
                AchievementManager.Instance.OnAchievementUnlocked -= OnAchievementUnlocked;
        }

        // ─── Public API ──────────────────────────────────────────────────────

        public bool IsOwned(string costumeId) => _data.ownedIds.Contains(costumeId);

        public UnlockCheckResult CanPurchase(string costumeId)
        {
            var result = new UnlockCheckResult();
            if (_db == null) return result;

            var def = _db.GetById(costumeId);
            if (def == null) return result;

            int currentLevel = PlayerLevelManager.Instance != null
                ? PlayerLevelManager.Instance.GetProgress().currentLevel : 1;

            result.isLevelMet   = def.requiredLevel <= 0 || currentLevel >= def.requiredLevel;
            result.missingLevel = result.isLevelMet ? 0 : def.requiredLevel - currentLevel;

            long gold = CurrencyManager.Instance != null ? CurrencyManager.Instance.Get(CurrencyType.Gold) : 0;
            long gem  = CurrencyManager.Instance != null ? CurrencyManager.Instance.Get(CurrencyType.Gem)  : 0;

            bool goldOk = gold >= def.goldCost;
            bool gemOk  = gem  >= def.gemCost;
            result.isCurrencyMet = goldOk && gemOk;
            result.missingGold   = goldOk ? 0 : def.goldCost - gold;
            result.missingGem    = gemOk  ? 0 : def.gemCost  - gem;

            if (def.unlockMethod == CostumeUnlock.ByAchievement)
                result.isAchievementMet = AchievementManager.Instance != null
                    && AchievementManager.Instance.IsUnlocked(def.requiredAchievementId);
            else
                result.isAchievementMet = true;

            result.canUnlock = result.isLevelMet && result.isCurrencyMet && result.isAchievementMet;
            return result;
        }

        public bool TryPurchase(string costumeId)
        {
            if (IsOwned(costumeId)) return false;
            var check = CanPurchase(costumeId);
            if (!check.canUnlock) return false;

            var def = _db.GetById(costumeId);
            if (def == null) return false;

            if (def.goldCost > 0) CurrencyManager.Instance.Spend(CurrencyType.Gold, def.goldCost);
            if (def.gemCost  > 0) CurrencyManager.Instance.Spend(CurrencyType.Gem,  def.gemCost);

            GrantCostume(def);
            return true;
        }

        public CostumeDefinition GetEquipped(CostumeType type)
        {
            if (_db == null) return null;
            string id = type == CostumeType.Character ? _data.equippedChar : _data.equippedWeapon;
            return string.IsNullOrEmpty(id) ? null : _db.GetById(id);
        }

        public void Equip(string costumeId)
        {
            if (!IsOwned(costumeId)) return;
            var def = _db?.GetById(costumeId);
            if (def == null) return;

            if (def.costumeType == CostumeType.Character) _data.equippedChar   = costumeId;
            else                                           _data.equippedWeapon = costumeId;
            Save();
            OnCostumeEquipped?.Invoke(def);
        }

        // Called by AchievementManager when an achievement with rewardCostumeId is unlocked
        public void GrantCostumeById(string costumeId)
        {
            if (string.IsNullOrEmpty(costumeId) || _db == null) return;
            var def = _db.GetById(costumeId);
            if (def != null) GrantCostume(def);
        }

        // ─── Internals ───────────────────────────────────────────────────────

        private void OnAchievementUnlocked(Achievements.AchievementDefinition achievementDef)
        {
            // Grant costume reward if attached to this achievement
            if (!string.IsNullOrEmpty(achievementDef.rewardCostumeId))
                GrantCostumeById(achievementDef.rewardCostumeId);

            // Also auto-grant costumes whose unlock requirement is this achievement
            if (_db == null) return;
            foreach (var costume in _db.allCostumes)
            {
                if (costume == null) continue;
                if (costume.unlockMethod == CostumeUnlock.ByAchievement
                    && costume.requiredAchievementId == achievementDef.achievementId
                    && !IsOwned(costume.costumeId))
                {
                    GrantCostume(costume);
                }
            }
        }

        private void GrantCostume(CostumeDefinition def)
        {
            if (!_data.ownedIds.Contains(def.costumeId))
                _data.ownedIds.Add(def.costumeId);
            Save();
            OnCostumePurchased?.Invoke(def);
#if UNITY_EDITOR
            Debug.Log($"[CostumeManager] Costume granted: {def.displayName}");
#endif
        }

        private void Save()
        {
            File.WriteAllText(SavePath, JsonUtility.ToJson(_data, true));
            CloudSaveManager.Instance?.QueuePush("costumes", SavePath);
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
                Debug.LogWarning($"[CostumeManager] Save load failed, using defaults. {e.Message}");
#endif
                _data = new SaveData();
            }
        }
    }
}
