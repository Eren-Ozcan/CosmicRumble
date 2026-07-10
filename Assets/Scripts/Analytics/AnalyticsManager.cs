using UnityEngine;
using Unity.Services.Analytics;
using Unity.Services.Core;

namespace CosmicRumble.Analytics
{
    /// <summary>
    /// Unity Analytics (ücretsiz katman) sarmalayıcısı. UGS Core init/sign-in tamamlandıktan sonra
    /// (CloudSaveManager.InitializeAndPull sonrası, MainMenuUI.BootstrapSequence'ta) <see cref="EnsureStarted"/>
    /// çağrılmalı. SDK'nın kendi dokümantasyonu StartDataCollection'ı "kullanıcıdan onay alındığını
    /// veya gerekmediğini teyit eder" olarak tanımlıyor — gizlilik politikası (yol haritası madde 7)
    /// canlıya alınmadan gerçek son kullanıcı build'lerine dağıtılmamalı, Editor/iç test için sorun değil.
    ///
    /// Otomatik toplanan (dashboard'da şema gerektirmeyen) session/engagement event'leri dışında,
    /// custom event'ler (ör. match_completed) yalnızca UGS Dashboard'da aynı isimle bir şema
    /// TANIMLANMIŞSA kaydedilir — kod hazır, dashboard adımı ayrı (bkz. TODO.md).
    /// </summary>
    public class AnalyticsManager : MonoBehaviour
    {
        public static AnalyticsManager Instance { get; private set; }

        bool _started;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void EnsureStarted()
        {
            if (_started) return;
            if (UnityServices.State != ServicesInitializationState.Initialized) return;
            try
            {
                AnalyticsService.Instance.StartDataCollection();
                _started = true;
            }
            catch (System.Exception e)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"[AnalyticsManager] StartDataCollection failed: {e.Message}");
#endif
            }
        }

        /// <summary>Maç sonu — UGS Dashboard'da "match_completed" şeması tanımlanınca aktifleşir.</summary>
        public void RecordMatchCompleted(bool won, bool ranked)
        {
            if (!_started) return;
            try
            {
                var evt = new CustomEvent("match_completed")
                {
                    { "won", won },
                    { "ranked", ranked },
                };
                AnalyticsService.Instance.RecordEvent(evt);
            }
            catch (System.Exception e)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"[AnalyticsManager] RecordEvent failed: {e.Message}");
#endif
            }
        }
    }
}
