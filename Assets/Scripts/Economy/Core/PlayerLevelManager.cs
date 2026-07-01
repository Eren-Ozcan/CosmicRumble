using System;
using System.IO;
using UnityEngine;

namespace CosmicRumble.Economy
{
    public class PlayerLevelManager : MonoBehaviour
    {
        public static PlayerLevelManager Instance { get; private set; }

        public event Action<int, int> OnLevelUp;
        public event Action<int>      OnPrestige;

        [Serializable]
        private class SaveData
        {
            public long totalXP;
            public int  currentLevel = 1;
            public int  prestigeRank = 0;
        }

        private SaveData  _data = new SaveData();
        [SerializeField] private LevelConfig _config;
        private string SavePath => Path.Combine(Application.persistentDataPath, "progress.json");

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            var loadedConfig = Resources.Load<LevelConfig>("Economy/LevelConfig");
            if (loadedConfig != null) _config = loadedConfig;
            if (_config == null)
            {
#if UNITY_EDITOR
                Debug.LogError("[PlayerLevelManager] LevelConfig not found at Resources/Economy/LevelConfig — assign it in the Inspector as fallback");
#endif
            }

            Load();
        }

        private void Start()
        {
            if (CurrencyManager.Instance != null)
                CurrencyManager.Instance.OnCurrencyChanged += OnCurrencyChanged;
        }

        private void OnDestroy()
        {
            if (CurrencyManager.Instance != null)
                CurrencyManager.Instance.OnCurrencyChanged -= OnCurrencyChanged;
        }

        private void OnCurrencyChanged(CurrencyType type, long newBalance)
        {
            if (type != CurrencyType.XP) return;
            _data.totalXP = newBalance;
            CheckLevelUp();
        }

        public void CheckLevelUp()
        {
            if (_config == null) return;

            int oldLevel = _data.currentLevel;

            while (true)
            {
                long xpNeeded = _config.GetXPForLevel(_data.currentLevel);
                long xpInLevel = _data.totalXP - _config.GetTotalXPForLevel(_data.currentLevel);

                if (xpInLevel < xpNeeded) break;

                _data.currentLevel++;

                // Prestige check: when passing level 100
                if (_data.currentLevel > LevelConfig.MaxLevelBeforePrestige && _data.prestigeRank == 0)
                {
                    _data.prestigeRank = 1;
                    OnPrestige?.Invoke(_data.prestigeRank);
                }
                else if (_data.currentLevel > LevelConfig.MaxLevelBeforePrestige + _data.prestigeRank)
                {
                    _data.prestigeRank++;
                    OnPrestige?.Invoke(_data.prestigeRank);
                }
            }

            if (_data.currentLevel != oldLevel)
            {
                Save();
                OnLevelUp?.Invoke(oldLevel, _data.currentLevel);
            }
        }

        public PlayerProgressData GetProgress()
        {
            if (_config == null) return new PlayerProgressData();

            long xpForCurrent = _config.GetTotalXPForLevel(_data.currentLevel);
            long xpNeeded     = _config.GetXPForLevel(_data.currentLevel);
            long xpInLevel    = _data.totalXP - xpForCurrent;

            return new PlayerProgressData
            {
                totalXP             = _data.totalXP,
                currentLevel        = _data.currentLevel,
                prestigeRank        = _data.prestigeRank,
                xpInCurrentLevel    = xpInLevel,
                xpNeededForNextLevel = xpNeeded,
                levelProgress       = xpNeeded > 0 ? Mathf.Clamp01((float)xpInLevel / xpNeeded) : 1f
            };
        }

        private void Save() =>
            File.WriteAllText(SavePath, JsonUtility.ToJson(_data, true));

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
                Debug.LogWarning($"[PlayerLevelManager] Save load failed, using defaults. {e.Message}");
#endif
                _data = new SaveData();
            }
        }
    }
}
