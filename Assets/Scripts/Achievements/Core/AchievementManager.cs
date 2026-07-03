using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using CosmicRumble.Economy;
using CosmicRumble.Cloud;

namespace CosmicRumble.Achievements
{
    public class AchievementManager : MonoBehaviour
    {
        public static AchievementManager Instance { get; private set; }

        public event Action<AchievementDefinition> OnAchievementUnlocked;

        [Serializable]
        private class AchievementProgress
        {
            public string achievementId;
            public bool   isUnlocked;
            public int    currentProgress;
        }

        [Serializable]
        private class SaveData
        {
            public List<AchievementProgress> achievements = new List<AchievementProgress>();
        }

        private SaveData          _data = new SaveData();
        [SerializeField] private AchievementDatabase _db;
        private IAchievementProvider _provider;
        private string _currentUsername; // null = misafir
        private string SavePath => _currentUsername != null
            ? Path.Combine(Application.persistentDataPath, $"achievements_{_currentUsername}.json")
            : Path.Combine(Application.persistentDataPath, "achievements_guest.json");

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            var loadedDb = Resources.Load<AchievementDatabase>("Achievements/AchievementDatabase");
            if (loadedDb != null) _db = loadedDb;
            if (_db == null)
            {
#if UNITY_EDITOR
                Debug.LogError("[AchievementManager] AchievementDatabase not found at Resources/Achievements/AchievementDatabase — assign it in the Inspector as fallback");
#endif
            }

            SelectProvider();
            Load();

            _provider.Initialize(() =>
            {
#if UNITY_EDITOR
                Debug.Log($"[AchievementManager] Provider ready: {_provider.ProviderName}");
#endif
            });
        }

        private void Update() => _provider?.Tick();

        private void OnApplicationQuit() => _provider?.Shutdown();

        private void SelectProvider()
        {
#if UNITY_STANDALONE && !UNITY_EDITOR
            _provider = new SteamAchievementProvider();
#elif UNITY_ANDROID
            _provider = new GooglePlayAchievementProvider();
#elif UNITY_IOS
            _provider = new AppStoreAchievementProvider();
#else
            _provider = new LocalAchievementProvider();
#endif
        }

        // ─── Public API ──────────────────────────────────────────────────────

        /// <summary>
        /// Kullanıcı girişi/çıkışında çağrılır.
        /// username == null → misafir modu.
        /// </summary>
        public void LoadForUser(string username)
        {
            _currentUsername = username;
            Load();
#if UNITY_EDITOR
            Debug.Log($"[AchievementManager] Kullanıcı başarımları yüklendi: {username ?? "misafir"}");
#endif
        }

        public void UnlockAchievement(string id)
        {
            if (IsUnlocked(id)) return;

            var def = _db != null ? _db.GetById(id) : null;
            if (def == null)
            {
    #if UNITY_EDITOR
            Debug.LogWarning($"[AchievementManager] Unknown achievement: {id}");
#endif
                return;
            }

            var entry = GetOrCreate(id);
            entry.isUnlocked = true;
            Save();

            _provider?.UnlockAchievement(id);

            // Grant rewards
            if (CurrencyManager.Instance != null)
            {
                if (def.rewardXP   > 0) CurrencyManager.Instance.Add(CurrencyType.XP,   def.rewardXP);
                if (def.rewardGold > 0) CurrencyManager.Instance.Add(CurrencyType.Gold, def.rewardGold);
                if (def.rewardGem  > 0) CurrencyManager.Instance.Add(CurrencyType.Gem,  def.rewardGem);
            }

            OnAchievementUnlocked?.Invoke(def);
#if UNITY_EDITOR
            Debug.Log($"[AchievementManager] Unlocked: {def.displayName}");
#endif
        }

        public void UpdateProgress(string id, int value)
        {
            if (IsUnlocked(id)) return;

            var def = _db != null ? _db.GetById(id) : null;
            if (def == null) return;

            var entry = GetOrCreate(id);
            entry.currentProgress = value;
            Save();

            _provider?.UpdateProgress(id, value, def.targetValue);

            if (def.triggerType == AchievementTriggerType.Cumulative && value >= def.targetValue)
                UnlockAchievement(id);
        }

        public bool IsUnlocked(string id)
        {
            foreach (var e in _data.achievements)
                if (e.achievementId == id) return e.isUnlocked;
            return false;
        }

        public int GetProgress(string id)
        {
            foreach (var e in _data.achievements)
                if (e.achievementId == id) return e.currentProgress;
            return 0;
        }

        // ─── Helpers ─────────────────────────────────────────────────────────

        private AchievementProgress GetOrCreate(string id)
        {
            foreach (var e in _data.achievements)
                if (e.achievementId == id) return e;
            var entry = new AchievementProgress { achievementId = id };
            _data.achievements.Add(entry);
            return entry;
        }

        private void Save()
        {
            File.WriteAllText(SavePath, JsonUtility.ToJson(_data, true));
            CloudSaveManager.Instance?.QueuePush("achievements", SavePath);
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
                Debug.LogWarning($"[AchievementManager] Save load failed, using defaults. {e.Message}");
#endif
                _data = new SaveData();
            }
        }
    }
}
