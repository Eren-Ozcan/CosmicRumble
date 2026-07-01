using System;
using System.IO;
using UnityEngine;

namespace CosmicRumble.Economy
{
    public class CurrencyManager : MonoBehaviour
    {
        public static CurrencyManager Instance { get; private set; }

        public event Action<CurrencyType, long> OnCurrencyChanged;

        [Serializable]
        private class SaveData
        {
            public long xp;
            public long gold;
            public long gem;
        }

        private SaveData _data = new SaveData();
        private string SavePath => Path.Combine(Application.persistentDataPath, "currency.json");

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Load();
        }

        public void Add(CurrencyType type, long amount)
        {
            if (amount <= 0) return;
#if UNITY_EDITOR
            if (type == CurrencyType.Gem)
                Debug.Log($"[CurrencyManager][GEM_AUDIT] Add {amount} Gem — total before: {_data.gem}");
#endif

            SetBalance(type, GetBalance(type) + amount);
        }

        public bool Spend(CurrencyType type, long amount)
        {
            if (amount <= 0) return false;
            long balance = GetBalance(type);
            if (balance < amount) return false;
            SetBalance(type, balance - amount);
            return true;
        }

        public long Get(CurrencyType type) => GetBalance(type);

        private long GetBalance(CurrencyType type)
        {
            return type switch
            {
                CurrencyType.XP   => _data.xp,
                CurrencyType.Gold => _data.gold,
                CurrencyType.Gem  => _data.gem,
                _                 => 0
            };
        }

        private void SetBalance(CurrencyType type, long value)
        {
            switch (type)
            {
                case CurrencyType.XP:   _data.xp   = value; break;
                case CurrencyType.Gold: _data.gold = value; break;
                case CurrencyType.Gem:  _data.gem  = value; break;
            }
            Save();
            OnCurrencyChanged?.Invoke(type, value);
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
                Debug.LogWarning($"[CurrencyManager] Save load failed, using defaults. {e.Message}");
#endif
                _data = new SaveData();
            }
        }
    }
}
