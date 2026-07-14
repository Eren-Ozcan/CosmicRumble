using System;
using System.IO;
using UnityEngine;
using CosmicRumble.Cloud;
using CosmicRumble.Utilities;

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

        // Diskteki dosya artık {data, hmac} zarfı — currency.json elle (save-editor/hex-editor ile)
        // değiştirilirse Load() bunu tespit edip GÜVENLİ VARSAYILANA döner, sessizce hileli bakiyeyi
        // kabul etmez. NOT — dürüst sınır: anahtar client binary'sinde gömülü olduğu için bu,
        // reverse-engineering yapabilen gelişmiş bir saldırganı DURDURMAZ; amacı save-editor gibi
        // yaygın/kolay araçlarla yapılan kurcalamayı engellemektir, kriptografik bir garanti değildir.
        // Gerçek sunucu-taraflı doğrulama (Cloud Code) olmadan tam koruma mümkün değil (bkz. TODO.md).
        [Serializable]
        private class SaveEnvelope
        {
            public string data;
            public string hmac;
        }

        private SaveData _data = new SaveData();
        private string SavePath => Path.Combine(Application.persistentDataPath, "currency.json");

        // İmza CİHAZDAN BAĞIMSIZ (SaveIntegrity.Sign): bu dosya CloudSaveManager ile buluta itilip
        // başka bir cihaza aynen iniyor — eski, cihaz kimliği karılmış imza yeni cihazda HER ZAMAN
        // tutmuyor ve meşru bulut geri yüklemesi "kurcalama" sanılıp bakiye SIFIRLANIYOR, üstüne
        // sıfırlanmış hali buluta geri yazılıyordu (kalıcı veri kaybı). Cihaz bağlama, buluta giden
        // veriyle tanım gereği bağdaşmaz; kurcalama koruması imzanın kendisiyle devam ediyor.

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

        private void Save()
        {
            string json = JsonUtility.ToJson(_data);
            var envelope = new SaveEnvelope { data = json, hmac = SaveIntegrity.Sign(json) };
            File.WriteAllText(SavePath, JsonUtility.ToJson(envelope, true));
            CloudSaveManager.Instance?.QueuePush("currency", SavePath);
        }

        private void Load()
        {
            try
            {
                if (!File.Exists(SavePath))
                {
                    _data = new SaveData();
                    return;
                }

                string raw = File.ReadAllText(SavePath);
                var envelope = JsonUtility.FromJson<SaveEnvelope>(raw);

                if (envelope != null && !string.IsNullOrEmpty(envelope.hmac))
                {
                    // Zarflı format — bütünlük doğrulanmalı. Önce güncel (cihazdan bağımsız) imza;
                    // tutmazsa bu güncellemeden ÖNCE yazılmış cihaz-bağlı imza denenir (geçiş):
                    // eski imza yalnızca dosyayı yazan cihazda geçerlidir ama mevcut oyuncuların
                    // ilerlemesini silmemek için kabul edilip hemen yeni formatta yeniden imzalanır.
                    bool valid = SaveIntegrity.Sign(envelope.data) == envelope.hmac;
                    bool legacyValid = !valid && SaveIntegrity.SignDeviceBound(envelope.data) == envelope.hmac;
                    if (!valid && !legacyValid)
                    {
#if UNITY_EDITOR
                        Debug.LogWarning("[CurrencyManager] currency.json HMAC mismatch — muhtemel kurcalama tespit edildi, güvenli varsayılana dönülüyor.");
#endif
                        _data = new SaveData();
                        Save(); // kurcalanmış dosyanın üzerine temiz+imzalı bir kopya yaz
                        return;
                    }
                    _data = JsonUtility.FromJson<SaveData>(envelope.data) ?? new SaveData();
                    if (legacyValid)
                        Save(); // yeni (cihazdan bağımsız) imzayla yeniden yaz
                }
                else
                {
                    // Eski (zarfsız) format — bu güncellemeden önce yazılmış dosyalar için geriye
                    // dönük uyumluluk (mevcut oyuncuların ilerlemesi silinmesin). Okur okumaz yeni
                    // imzalı formatta yeniden yazılır.
                    _data = JsonUtility.FromJson<SaveData>(raw) ?? new SaveData();
                    Save();
                }
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
