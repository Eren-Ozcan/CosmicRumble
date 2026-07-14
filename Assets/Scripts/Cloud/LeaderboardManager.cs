// Assets/Scripts/Cloud/LeaderboardManager.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Models;

namespace CosmicRumble.Cloud
{
    /// <summary>
    /// UGS Leaderboards sarmalayıcısı — Clash Royale tarzı KUPA (trophy) sistemi.
    ///
    /// Online maç kazanınca +<see cref="TrophiesPerWin"/>, kaybedince -<see cref="TrophiesPerLoss"/>
    /// kupa (0'ın altına düşmez); beraberlikte değişim yok. Güncel kupa toplamı her maç sonunda
    /// leaderboard'a gönderilir ve sıralama kupa sayısına göredir.
    ///
    /// ── UGS Dashboard kurulumu (kod tarafı hazır, bu adım manuel) ─────────────────────────────
    ///   cloud.unity.com → projeniz (eren-zcan org, Auth/Cloud Save/Relay'in bağlı olduğu proje)
    ///   → Leaderboards → Add leaderboard:
    ///     ID          : cosmic_trophies   (LeaderboardId sabitiyle birebir aynı olmalı)
    ///     Sort order  : High to low
    ///     Update type : Latest submission   (kupa DÜŞEBİLİR — "Keep best" KULLANMAYIN,
    ///                                        mağlubiyet kaybı tabloya hiç yansımaz)
    ///   Leaderboard dashboard'da yokken tüm çağrılar sessizce no-op/boş liste döner — oyun kırılmaz.
    /// ───────────────────────────────────────────────────────────────────────────────────────────
    ///
    /// UGS init/sign-in CloudSaveManager.InitializeAndPull (menü bootstrap'i) tarafından yapılır;
    /// burada yalnızca "hazır mı" kontrol edilir, ikinci bir init başlatılmaz (NetworkBootstrap
    /// ile aynı yaklaşım).
    /// </summary>
    public class LeaderboardManager : MonoBehaviour
    {
        public static LeaderboardManager Instance { get; private set; }

        /// <summary>UGS Dashboard'da oluşturulması gereken leaderboard ID'si.</summary>
        public const string LeaderboardId = "cosmic_trophies";

        public const int TrophiesPerWin  = 30;
        public const int TrophiesPerLoss = 20;

        private const string TrophyPrefKey    = "leaderboard_trophies";
        private const string TrophySigPrefKey = "leaderboard_trophies_sig";

        /// <summary>
        /// Yerelde bilinen güncel kupa sayısı. Değer, cihaza bağlı bir HMAC ile imzalanır
        /// (buluta gitmeyen saf yerel önbellek): PlayerPrefs/regedit'te elle değiştirilen kupa
        /// imzayı tutturamaz ve 0 sayılır — böylece şişirilmiş değer bir sonraki maç sonunda
        /// leaderboard'a da gönderilmez. NOT: bu yalnızca kolay kurcalamaya karşı bir bariyer;
        /// gerçek sunucu-taraflı kupa otoritesi Cloud Code işi (TODO.md madde 22).
        /// </summary>
        public int Trophies
        {
            get
            {
                int value = PlayerPrefs.GetInt(TrophyPrefKey, 0);
                if (value == 0) return 0;

                string sig = PlayerPrefs.GetString(TrophySigPrefKey, "");
                if (sig == CosmicRumble.Utilities.SaveIntegrity.SignDeviceBound("trophies:" + value))
                    return value;

                // Geçiş: bu güncellemeden önce yazılmış (hiç imzalanmamış) değer bir kez kabul
                // edilip imzalanır — mevcut oyuncuların kupası silinmesin.
                if (!PlayerPrefs.HasKey(TrophySigPrefKey))
                {
                    StoreTrophies(value);
                    return value;
                }
                return 0;
            }
        }

        private static void StoreTrophies(int total)
        {
            PlayerPrefs.SetInt(TrophyPrefKey, total);
            PlayerPrefs.SetString(TrophySigPrefKey,
                CosmicRumble.Utilities.SaveIntegrity.SignDeviceBound("trophies:" + total));
            PlayerPrefs.Save();
        }

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private static bool ServicesReady =>
            UnityServices.State == ServicesInitializationState.Initialized &&
            AuthenticationService.Instance.IsSignedIn;

        /// <summary>
        /// Leaderboards servisine güvenilir erişim. Statik LeaderboardsService.Instance'a
        /// GÜVENİLMEZ: core, paketleri instance-tabanlı yolla (IInitializablePackageV2
        /// .InitializeInstanceAsync) başlattığında o static hiç set edilmiyor (bu projede canlı
        /// olarak doğrulandı — servis CoreRegistry'de kayıtlıyken static null kalıp
        /// ServicesInitializationException atıyordu). Resmî erişim yolu paketin
        /// UnityServices.Instance.GetLeaderboardsService() extension'ı; static'e yalnızca
        /// yedek olarak düşülür.
        /// </summary>
        private static ILeaderboardsService Service
        {
            get
            {
                try
                {
                    var svc = UnityServices.Instance?.GetLeaderboardsService();
                    if (svc != null) return svc;
                }
                catch { /* kayıt yoksa aşağıdaki yedeğe düş */ }

                try { return LeaderboardsService.Instance; }
                catch { return null; }
            }
        }

        // ── Kupa güncelleme ──────────────────────────────────────────────────

        /// <summary>
        /// TurnManager.AnnounceMatchResultClientRpc tarafından her online maç sonunda, her makinede
        /// kendi yerel sonucuyla çağrılır (beraberlikte hiç çağrılmaz — kupa değişimi yok).
        /// </summary>
        public void ReportOnlineMatchResult(bool localPlayerWon)
        {
            int delta = localPlayerWon ? +TrophiesPerWin : -TrophiesPerLoss;
            int total = Mathf.Max(0, Trophies + delta);

            StoreTrophies(total);

            _ = SubmitScoreAsync(total);
        }

        private async Task SubmitScoreAsync(int trophies)
        {
            if (!ServicesReady) return;
            var svc = Service;
            if (svc == null) return;
            try
            {
                await svc.AddPlayerScoreAsync(LeaderboardId, trophies);
            }
            catch (Exception e)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"[LeaderboardManager] Score submit failed: {e.Message}");
#endif
                return;
            }

            // BIR_NUMARA/KOZMIK_AVCI: skor gönderildikten sonra güncel sıralamayı öğren — achievement
            // kontrolünün "anında" olması gerekmiyor, maç-sonu ekranı zaten bunu beklemiyor, bu yüzden
            // ayrı bir fire-and-forget adım olarak yeterli (senkron bir API'ye ihtiyaç yoktu).
            try
            {
                var own = await FetchOwnEntryAsync();
                if (own != null) CosmicRumble.Achievements.AchievementEvents.FireLeaderboardRankKnown(own.Rank);
            }
            catch (Exception e)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"[LeaderboardManager] Rank check failed: {e.Message}");
#endif
            }
        }

        // ── Okuma (panel için) ───────────────────────────────────────────────

        /// <summary>İlk <paramref name="limit"/> girişi döner; hata/servis yoksa boş liste.</summary>
        public async Task<List<LeaderboardEntry>> FetchTopAsync(int limit = 50)
        {
            var svc = Service;
            if (!ServicesReady || svc == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"[LeaderboardManager] Fetch skipped — ready={ServicesReady} service={(svc != null)}");
#endif
                return new List<LeaderboardEntry>();
            }
            try
            {
                var page = await svc.GetScoresAsync(
                    LeaderboardId, new GetScoresOptions { Offset = 0, Limit = limit });
                return page.Results ?? new List<LeaderboardEntry>();
            }
            catch (Exception e)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"[LeaderboardManager] Fetch failed: {e.Message}");
#endif
                return new List<LeaderboardEntry>();
            }
        }

        /// <summary>Yerel oyuncunun kendi girişini döner; skoru hiç yoksa/hata olursa null.</summary>
        public async Task<LeaderboardEntry> FetchOwnEntryAsync()
        {
            var svc = Service;
            if (!ServicesReady || svc == null) return null;
            try
            {
                return await svc.GetPlayerScoreAsync(LeaderboardId);
            }
            catch
            {
                return null; // skor yok (henüz hiç online maç oynanmamış) — normal durum
            }
        }

        /// <summary>
        /// Leaderboard'da anonim "Player#1234" yerine oyuncunun görünen adının çıkması için UGS
        /// player name'i eşitler. Bağlı hesapta kullanıcı adı, değilse PlayerIdentity'nin ürettiği
        /// kalıcı takma ad gönderilir ("Misafir/Guest" hiçbir zaman görünmez).
        /// </summary>
        public async Task SyncPlayerNameAsync()
        {
            if (!ServicesReady) return;

            string name = PlayerIdentity.Get();
            if (string.IsNullOrWhiteSpace(name)) return;

            // UGS player name kuralları: boşluk yok, max 50 karakter.
            name = name.Replace(" ", "_");
            if (name.Length > 50) name = name.Substring(0, 50);

            try
            {
                await AuthenticationService.Instance.UpdatePlayerNameAsync(name);
            }
            catch (Exception e)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"[LeaderboardManager] Player name sync failed: {e.Message}");
#endif
            }
        }

        // ── Lig/Arena (Clash Royale arena karşılığı, kupa aralığına göre) ────

        public static string GetLeagueName(int trophies) => trophies switch
        {
            < 300  => "Asteroid League",
            < 600  => "Moon League",
            < 1000 => "Planet League",
            < 1500 => "Star League",
            < 2200 => "Nebula League",
            _      => "Galaxy League"
        };
    }
}
