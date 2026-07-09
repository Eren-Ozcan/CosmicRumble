using System;
using System.IO;
using UnityEngine;
using CosmicRumble.Cloud;

namespace CosmicRumble.Economy
{
    /// <summary>
    /// Profil avatarı seçimi — kostümlerin aksine hepsi baştan açık (unlock/rarity yok), sadece
    /// "hangisi seçili" bilgisi kalıcı. Gerçek görsel yok (placeholderColor + baş harf), ikon
    /// sprite'ları eklenince AvatarDefinition.icon null-safe zaten UI'da öncelik alır.
    /// </summary>
    public class AvatarManager : MonoBehaviour
    {
        public static AvatarManager Instance { get; private set; }

        public event Action<AvatarDefinition> OnAvatarChanged;

        [Serializable]
        private class SaveData
        {
            public string selectedId = "";
        }

        private SaveData      _data = new SaveData();
        private AvatarDatabase _db;
        private string SavePath => Path.Combine(Application.persistentDataPath, "avatar.json");

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _db = Resources.Load<AvatarDatabase>("Economy/AvatarDatabase");
            if (_db == null)
            {
#if UNITY_EDITOR
                Debug.LogError("[AvatarManager] AvatarDatabase not found at Resources/Economy/AvatarDatabase");
#endif
            }

            Load();
        }

        public AvatarDefinition GetSelected()
        {
            if (_db == null || _db.allAvatars.Count == 0) return null;
            var def = string.IsNullOrEmpty(_data.selectedId) ? null : _db.GetById(_data.selectedId);
            return def ?? _db.allAvatars[0];
        }

        public void Select(string avatarId)
        {
            if (_db == null || _db.GetById(avatarId) == null) return;
            _data.selectedId = avatarId;
            Save();
            OnAvatarChanged?.Invoke(GetSelected());
        }

        private void Save()
        {
            File.WriteAllText(SavePath, JsonUtility.ToJson(_data, true));
            CloudSaveManager.Instance?.QueuePush("avatar", SavePath);
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
                Debug.LogWarning($"[AvatarManager] Save load failed, using defaults. {e.Message}");
#endif
                _data = new SaveData();
            }
        }
    }
}
