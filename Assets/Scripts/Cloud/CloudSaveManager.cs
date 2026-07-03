using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;

namespace CosmicRumble.Cloud
{
    /// <summary>
    /// UGS Authentication + Cloud Save sarmalayıcısı. Kullanıcı adına bağlı olmayan yerel "oyuncu
    /// ilerlemesi" JSON dosyalarını (currency.json, progress.json, vb. — bkz. SyncedFiles) Cloud
    /// Save'e senkronize eder. `achievements_&lt;username&gt;.json` kasıtlı olarak kapsam dışı: yerel
    /// kullanıcı adı ile UGS oyuncu kimliği arasındaki ilişki netleşmeden (ayrı bir karar, bkz.
    /// TODO.md) güvenle senkronlanamaz.
    ///
    /// Unity Cloud Project bağlı değilse veya ağ yoksa her işlem sessizce no-op olur — oyun her
    /// zaman local-only da tam çalışmaya devam eder; bulut senkronu saf bir ek katman.
    /// </summary>
    public class CloudSaveManager : MonoBehaviour
    {
        public static CloudSaveManager Instance { get; private set; }

        public bool IsReady { get; private set; }

        private bool _unavailable;

        // Cloud Save anahtarı -> yerel dosya adı.
        private static readonly Dictionary<string, string> SyncedFiles = new Dictionary<string, string>
        {
            { "currency", "currency.json" },
            { "progress", "progress.json" },
            { "unlocks",  "unlocks.json"  },
            { "quests",   "quests.json"   },
            { "chests",   "chests.json"   },
            { "streak",   "streak.json"   },
            { "costumes", "costumes.json" },
        };

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Tüm bilinen anahtarları buluttan çekip ilgili yerel dosyaların üzerine yazar. Diğer
        /// progress manager'ları (CurrencyManager vb.) oluşturulmadan ÖNCE bir kere çağrılmalı —
        /// böylece onların kendi Awake/Load'u zaten senkronlanmış veriyi okur. timeoutSeconds
        /// içinde bitmezse (ağ yok / UGS yapılandırılmamış) local-only devam eder.
        /// </summary>
        public IEnumerator InitializeAndPull(float timeoutSeconds = 6f)
        {
            var task = InitializeAndPullAsync();
            float elapsed = 0f;
            while (!task.IsCompleted && elapsed < timeoutSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            if (!task.IsCompleted)
            {
#if UNITY_EDITOR
                Debug.LogWarning("[CloudSaveManager] Init/pull timed out — continuing local-only.");
#endif
                _unavailable = true;
            }
        }

        // Unity Cloud Project'e yeni bağlanıldığında ya da Editor'ün ilk Play girişinde UGS'nin
        // dahili servis kaydı henüz hazır olmayabiliyor (UnityProjectNotLinkedException, config
        // gerçekte doğruyken bile) — bu geçici bir yarış durumu, kalıcı bir yapılandırma hatası
        // değil. Bir kere yeniden deneme, tüm oturumu gereksiz yere local-only'ye düşürmeyi önler.
        private const int MaxInitAttempts = 2;
        private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(1);

        private async Task InitializeAndPullAsync()
        {
            for (int attempt = 1; attempt <= MaxInitAttempts; attempt++)
            {
                try
                {
                    if (UnityServices.State == ServicesInitializationState.Uninitialized)
                        await UnityServices.InitializeAsync();

                    if (!AuthenticationService.Instance.IsSignedIn)
                        await AuthenticationService.Instance.SignInAnonymouslyAsync();

                    var keys = new HashSet<string>(SyncedFiles.Keys);
                    var remote = await CloudSaveService.Instance.Data.Player.LoadAsync(keys);

                    foreach (var kvp in remote)
                    {
                        if (!SyncedFiles.TryGetValue(kvp.Key, out var fileName)) continue;
                        string localPath = Path.Combine(Application.persistentDataPath, fileName);
                        File.WriteAllText(localPath, kvp.Value.Value.GetAsString());
                    }

                    IsReady = true;
                    return;
                }
                catch (Exception e)
                {
                    bool willRetry = attempt < MaxInitAttempts;
#if UNITY_EDITOR
                    Debug.LogWarning($"[CloudSaveManager] UGS init attempt {attempt}/{MaxInitAttempts} failed" +
                        $"{(willRetry ? ", retrying" : ", continuing local-only")}. {e.Message}");
#endif
                    if (!willRetry)
                    {
                        _unavailable = true;
                        return;
                    }
                    await Task.Delay(RetryDelay);
                }
            }
        }

        /// <summary>
        /// Yerel bir save dosyasının güncel içeriğini buluta gönderir (fire-and-forget, hata
        /// sessizce yutulur). fileKey, SyncedFiles içindeki bilinen anahtarlardan biri olmalı.
        /// </summary>
        public void QueuePush(string fileKey, string localFilePath)
        {
            if (_unavailable) return;
            _ = PushAsync(fileKey, localFilePath);
        }

        private async Task PushAsync(string fileKey, string localFilePath)
        {
            try
            {
                if (UnityServices.State != ServicesInitializationState.Initialized) return;
                if (!AuthenticationService.Instance.IsSignedIn) return;
                if (!File.Exists(localFilePath)) return;

                string content = File.ReadAllText(localFilePath);
                var data = new Dictionary<string, object> { { fileKey, content } };
                await CloudSaveService.Instance.Data.Player.SaveAsync(data);
            }
            catch (Exception e)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"[CloudSaveManager] Push failed for '{fileKey}': {e.Message}");
#endif
            }
        }
    }
}
