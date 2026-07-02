using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using CosmicRumble.Achievements;

namespace CosmicRumble.Economy
{
    public class QuestManager : MonoBehaviour
    {
        public static QuestManager Instance { get; private set; }

        // ─── Save data ────────────────────────────────────────────────────────
        [Serializable]
        private class QuestProgress
        {
            public string       questId;
            public int          progress;
            public bool         completed;
            public List<string> distinctIds = new List<string>(); // distinctTracking quest'ler için
        }

        [Serializable]
        private class SaveData
        {
            public List<string>        activeDaily   = new List<string>();
            public List<string>        activeWeekly  = new List<string>();
            public string              activeMonthly = "";
            public List<QuestProgress> progressList  = new List<QuestProgress>();
            public string              lastDailyReset  = "";
            public string              lastWeeklyReset = "";
            public string              lastMonthlyReset = "";
        }

        private SaveData _data = new SaveData();
        private string SavePath => Path.Combine(Application.persistentDataPath, "quests.json");

        // All quest definitions loaded from Resources/Economy/Quests/
        private QuestDefinition[] _allQuests;

        // ─── Events ───────────────────────────────────────────────────────────
        public event Action<QuestDefinition, int>  OnQuestProgress;   // quest, newProgress
        public event Action<QuestDefinition>       OnQuestCompleted;

        // ─── Lifecycle ────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _allQuests = Resources.LoadAll<QuestDefinition>("Economy/Quests");
            Load();
        }

        private void Start()
        {
            CheckResets();
            SubscribeToEvents();
        }

        private void OnDestroy() => UnsubscribeFromEvents();

        // ─── Public API ───────────────────────────────────────────────────────
        public List<QuestDefinition> GetActiveDailyQuests()   => GetDefs(_data.activeDaily);
        public List<QuestDefinition> GetActiveWeeklyQuests()  => GetDefs(_data.activeWeekly);
        public QuestDefinition       GetActiveMonthlyQuest()  => GetDef(_data.activeMonthly);

        public int  GetProgress(string questId)  => GetProgressEntry(questId)?.progress ?? 0;
        public bool IsCompleted(string questId)  => GetProgressEntry(questId)?.completed ?? false;

        // ─── Reset logic ──────────────────────────────────────────────────────
        private void CheckResets()
        {
            DateTime now = DateTime.UtcNow;

            // Daily — midnight
            if (!IsSameDay(now, _data.lastDailyReset))
            {
                _data.activeDaily = PickRandom(QuestPeriod.Daily, 3);
                ClearProgress(QuestPeriod.Daily);
                _data.lastDailyReset = now.ToString("o");
            }

            // Weekly — Monday midnight
            if (!IsSameWeek(now, _data.lastWeeklyReset))
            {
                _data.activeWeekly = PickRandom(QuestPeriod.Weekly, 2);
                ClearProgress(QuestPeriod.Weekly);
                _data.lastWeeklyReset = now.ToString("o");
            }

            // Monthly — 1st of month
            if (!IsSameMonth(now, _data.lastMonthlyReset))
            {
                var monthly = PickRandom(QuestPeriod.Monthly, 1);
                _data.activeMonthly = monthly.Count > 0 ? monthly[0] : "";
                ClearProgress(QuestPeriod.Monthly);
                _data.lastMonthlyReset = now.ToString("o");
            }

            Save();
        }

        // ─── Event wiring ─────────────────────────────────────────────────────
        // Backing fields — SubscribeToEvents'te atanır, UnsubscribeFromEvents'te kullanılır
        private Action         _hMatchWon;
        private Action<int>    _hMatchCompleted;
        private Action<int>    _hDamageDealt;
        private Action         _hHeadshotLanded;
        private Action<string> _hAbilityUsed;
        private Action<bool>   _hShotFired;
        private Action         _hPlanetDestroyed;
        private Action<string> _hWeaponUsed;

        private void SubscribeToEvents()
        {
            _hMatchWon        = ()    => Advance("MatchWon", 1);
            _hMatchCompleted  = shots => Advance("MatchCompleted", 1);
            _hDamageDealt     = amt   => Advance("DamageDealt", amt);
            _hHeadshotLanded  = ()    => Advance("HeadshotLanded", 1);
            _hAbilityUsed     = id    => AdvanceById("AbilityUsed", id);
            _hShotFired       = hit   => Advance("ShotFired", 1);
            _hPlanetDestroyed = ()    => Advance("PlanetDestroyed", 1);
            _hWeaponUsed      = id    => AdvanceById("WeaponUsed", id);

            AchievementEvents.OnMatchWon        += _hMatchWon;
            AchievementEvents.OnMatchCompleted  += _hMatchCompleted;
            AchievementEvents.OnDamageDealt     += _hDamageDealt;
            AchievementEvents.OnHeadshotLanded  += _hHeadshotLanded;
            AchievementEvents.OnAbilityUsed     += _hAbilityUsed;
            AchievementEvents.OnShotFired       += _hShotFired;
            AchievementEvents.OnPlanetDestroyed += _hPlanetDestroyed;
            AchievementEvents.OnWeaponUsed      += _hWeaponUsed;
        }

        private void UnsubscribeFromEvents()
        {
            AchievementEvents.OnMatchWon        -= _hMatchWon;
            AchievementEvents.OnMatchCompleted  -= _hMatchCompleted;
            AchievementEvents.OnDamageDealt     -= _hDamageDealt;
            AchievementEvents.OnHeadshotLanded  -= _hHeadshotLanded;
            AchievementEvents.OnAbilityUsed     -= _hAbilityUsed;
            AchievementEvents.OnShotFired       -= _hShotFired;
            AchievementEvents.OnPlanetDestroyed -= _hPlanetDestroyed;
            AchievementEvents.OnWeaponUsed      -= _hWeaponUsed;
        }

        private void Advance(string eventKey, int amount)
        {
            bool dirty = false;
            foreach (var questId in AllActiveIds())
            {
                var def = GetDef(questId);
                if (def == null || def.trackedEventKey != eventKey) continue;

                var entry = GetOrCreateProgress(questId);
                if (entry.completed) continue;

                entry.progress += amount;
                OnQuestProgress?.Invoke(def, entry.progress);

                if (entry.progress >= def.targetValue)
                {
                    entry.progress  = def.targetValue;
                    entry.completed = true;
                    GrantReward(def);
                    OnQuestCompleted?.Invoke(def);
                }
                dirty = true;
            }
            if (dirty) Save();
        }

        /// <summary>AbilityUsed/WeaponUsed gibi id'li event'ler için: requiredId filtresi ve
        /// distinctTracking (farklı id sayısı) desteği ekler.</summary>
        private void AdvanceById(string eventKey, string id)
        {
            bool dirty = false;
            foreach (var questId in AllActiveIds())
            {
                var def = GetDef(questId);
                if (def == null || def.trackedEventKey != eventKey) continue;
                if (!string.IsNullOrEmpty(def.requiredId) && def.requiredId != id) continue;

                var entry = GetOrCreateProgress(questId);
                if (entry.completed) continue;

                if (def.distinctTracking)
                {
                    if (entry.distinctIds.Contains(id)) continue; // zaten sayıldı, ilerleme yok
                    entry.distinctIds.Add(id);
                    entry.progress = entry.distinctIds.Count;
                }
                else
                {
                    entry.progress += 1;
                }

                OnQuestProgress?.Invoke(def, entry.progress);

                if (entry.progress >= def.targetValue)
                {
                    entry.progress  = def.targetValue;
                    entry.completed = true;
                    GrantReward(def);
                    OnQuestCompleted?.Invoke(def);
                }
                dirty = true;
            }
            if (dirty) Save();
        }

        private void GrantReward(QuestDefinition def)
        {
            if (CurrencyManager.Instance == null) return;
            if (def.rewardXP   > 0) CurrencyManager.Instance.Add(CurrencyType.XP,   def.rewardXP);
            if (def.rewardGold > 0) CurrencyManager.Instance.Add(CurrencyType.Gold, def.rewardGold);
            if (def.rewardGem  > 0) CurrencyManager.Instance.Add(CurrencyType.Gem,  def.rewardGem);
#if UNITY_EDITOR
            Debug.Log($"[QuestManager] Quest completed: {def.displayName} — XP:{def.rewardXP} Gold:{def.rewardGold} Gem:{def.rewardGem}");
#endif
        }

        // ─── Helpers ──────────────────────────────────────────────────────────
        private List<string> PickRandom(QuestPeriod period, int count)
        {
            var pool = new List<QuestDefinition>();
            foreach (var q in _allQuests)
                if (q != null && q.period == period) pool.Add(q);

            var result = new List<string>();
            while (result.Count < count && pool.Count > 0)
            {
                int idx = UnityEngine.Random.Range(0, pool.Count);
                result.Add(pool[idx].questId);
                pool.RemoveAt(idx);
            }
            return result;
        }

        private void ClearProgress(QuestPeriod period)
        {
            _data.progressList.RemoveAll(p =>
            {
                var def = GetDef(p.questId);
                return def != null && def.period == period;
            });
        }

        private IEnumerable<string> AllActiveIds()
        {
            foreach (var id in _data.activeDaily)   yield return id;
            foreach (var id in _data.activeWeekly)  yield return id;
            if (!string.IsNullOrEmpty(_data.activeMonthly)) yield return _data.activeMonthly;
        }

        private QuestDefinition GetDef(string questId)
        {
            foreach (var q in _allQuests)
                if (q != null && q.questId == questId) return q;
            return null;
        }

        private List<QuestDefinition> GetDefs(List<string> ids)
        {
            var list = new List<QuestDefinition>();
            foreach (var id in ids)
            {
                var def = GetDef(id);
                if (def != null) list.Add(def);
            }
            return list;
        }

        private QuestProgress GetProgressEntry(string questId)
        {
            foreach (var p in _data.progressList)
                if (p.questId == questId) return p;
            return null;
        }

        private QuestProgress GetOrCreateProgress(string questId)
        {
            var entry = GetProgressEntry(questId);
            if (entry != null) return entry;
            entry = new QuestProgress { questId = questId };
            _data.progressList.Add(entry);
            return entry;
        }

        private static bool IsSameDay(DateTime now, string stored)
        {
            if (!DateTime.TryParse(stored, out DateTime prev)) return false;
            return now.Year == prev.Year && now.DayOfYear == prev.DayOfYear;
        }

        private static bool IsSameWeek(DateTime now, string stored)
        {
            if (!DateTime.TryParse(stored, out DateTime prev)) return false;
            // Same ISO week: Monday is start
            DateTime monday = now.AddDays(-(int)now.DayOfWeek == 0 ? 6 : (int)now.DayOfWeek - 1);
            return prev >= monday.Date;
        }

        private static bool IsSameMonth(DateTime now, string stored)
        {
            if (!DateTime.TryParse(stored, out DateTime prev)) return false;
            return now.Year == prev.Year && now.Month == prev.Month;
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
                Debug.LogWarning($"[QuestManager] Save load failed, using defaults. {e.Message}");
#endif
                _data = new SaveData();
            }
        }
    }
}
