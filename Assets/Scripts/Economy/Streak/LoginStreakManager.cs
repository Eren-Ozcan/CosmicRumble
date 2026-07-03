using System;
using System.IO;
using UnityEngine;
using CosmicRumble.Cloud;

namespace CosmicRumble.Economy
{
    public class LoginStreakManager : MonoBehaviour
    {
        public static LoginStreakManager Instance { get; private set; }

        public event Action<int>               OnStreakUpdated;        // currentStreak
        public event Action<int, long, long, long> OnStreakRewardGranted; // streak, xp, gold, gem

        // ─── Streak reward table ──────────────────────────────────────────────
        // Ara günler: eşit veya küçük en yakın milestone ödülünü alır
        private static readonly (int day, long xp, long gold, long gem)[] RewardTable =
        {
            (  1,   10,    25,   0),
            (  3,   50,    75,   0),
            (  7,  150,   200,   5),
            ( 14,  300,   400,  15),
            ( 30,  500,   750,  30),
            (100, 1000,  2000, 100),
        };

        // ─── Save data ────────────────────────────────────────────────────────
        [Serializable]
        private class SaveData
        {
            public int    currentStreak = 0;
            public string lastLoginDate = "";
        }

        private SaveData _data = new SaveData();
        private string SavePath => Path.Combine(Application.persistentDataPath, "streak.json");

        // ─── Lifecycle ────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Load();
        }

        private void Start()
        {
            ProcessLogin();
        }

        // ─── Public API ───────────────────────────────────────────────────────
        public int GetCurrentStreak() => _data.currentStreak;

        // ─── Login logic ──────────────────────────────────────────────────────
        private void ProcessLogin()
        {
            DateTime today = DateTime.UtcNow.Date;

            if (!DateTime.TryParse(_data.lastLoginDate, out DateTime lastLogin))
                lastLogin = DateTime.MinValue;

            int daysDiff = (today - lastLogin.Date).Days;

            if (daysDiff == 0)
            {
                // Already counted today
                return;
            }
            else if (daysDiff == 1)
            {
                _data.currentStreak++;
            }
            else
            {
                // Missed a day — reset
                _data.currentStreak = 1;
            }

            _data.lastLoginDate = today.ToString("o");
            Save();

            OnStreakUpdated?.Invoke(_data.currentStreak);
            GrantReward(_data.currentStreak);

#if UNITY_EDITOR
            Debug.Log($"[LoginStreakManager] Streak: {_data.currentStreak}");
#endif
        }

        private void GrantReward(int streak)
        {
            // Find the highest milestone <= streak
            long xp = 0, gold = 0, gem = 0;
            foreach (var (day, rx, rg, rgem) in RewardTable)
            {
                if (streak >= day) { xp = rx; gold = rg; gem = rgem; }
                else break;
            }

            if (xp == 0 && gold == 0 && gem == 0) return;

            if (CurrencyManager.Instance != null)
            {
                if (xp   > 0) CurrencyManager.Instance.Add(CurrencyType.XP,   xp);
                if (gold > 0) CurrencyManager.Instance.Add(CurrencyType.Gold, gold);
                if (gem  > 0) CurrencyManager.Instance.Add(CurrencyType.Gem,  gem);
            }

            OnStreakRewardGranted?.Invoke(streak, xp, gold, gem);
#if UNITY_EDITOR
            Debug.Log($"[LoginStreakManager] Reward: XP:{xp} Gold:{gold} Gem:{gem}");
#endif
        }

        // ─── Save/Load ────────────────────────────────────────────────────────
        private void Save()
        {
            File.WriteAllText(SavePath, JsonUtility.ToJson(_data, true));
            CloudSaveManager.Instance?.QueuePush("streak", SavePath);
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
                Debug.LogWarning($"[LoginStreakManager] Save load failed, using defaults. {e.Message}");
#endif
                _data = new SaveData();
            }
        }
    }
}
