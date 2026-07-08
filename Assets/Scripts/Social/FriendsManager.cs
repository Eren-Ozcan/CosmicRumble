using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Services.Authentication;
using Unity.Services.Friends;
using Unity.Services.Friends.Models;
using Unity.Services.Friends.Notifications;

namespace CosmicRumble.Social
{
    /// <summary>Presence "activity" yükü — arkadaşlar listesinde durum + kupa gösterimi için.</summary>
    [Serializable]
    public class PresenceActivity
    {
        public string status;   // "Menüde" / "Maçta"
        public int    trophies;
    }

    /// <summary>MessageAsync ile arkadaşa gönderilen maç daveti (session kodu taşır).</summary>
    [Serializable]
    public class MatchInviteMessage
    {
        public string type = "match_invite";
        public string code;
        public string fromName;
    }

    /// <summary>
    /// UGS Friends servisi sarmalayıcısı. Kurallar:
    /// - InitializeAsync yalnızca oturum açıldıktan SONRA çağrılabilir; hesap değişiminde
    ///   (farklı PlayerId) yeniden init gerekir — _initializedForPlayerId guard'ı bunu yönetir.
    ///   Tek giriş noktası MainMenuUI.BootstrapSequence (hesap değişimi zaten sahneyi yeniliyor).
    /// - Başarısızlıkta IsAvailable=false kalır; SOSYAL panel "şu an kullanılamıyor" gösterir,
    ///   oyun etkilenmez.
    /// - MessageAsync yalnızca ONLINE kullanıcılara ulaşır (offline kutusu yok) — davet butonu
    ///   UI'da presence ile kilitlenir, gönderim fire-and-forget'tir.
    /// Arkadaş kodu = UGS PlayerName ("Nova731#1234") — bootstrap SyncPlayerNameAsync'i erken çağırır.
    /// </summary>
    public class FriendsManager : MonoBehaviour
    {
        public static FriendsManager Instance { get; private set; }

        /// <summary>Friends servisi bu oturum için kullanılabilir mi.</summary>
        public bool IsAvailable { get; private set; }

        string _initializedForPlayerId;
        bool   _eventsSubscribed;
        Task   _initTask;

        /// <summary>Arkadaş listesi veya istekler değişti (ekleme/silme/kabul).</summary>
        public event Action OnRelationshipsChanged;
        /// <summary>Bir arkadaşın çevrimiçi durumu/aktivitesi değişti.</summary>
        public event Action OnPresenceUpdated;
        /// <summary>Maç daveti geldi (mesaj, gönderenin PlayerId'si).</summary>
        public event Action<MatchInviteMessage, string> OnMatchInvite;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        // ── Init / lifecycle ─────────────────────────────────────────────

        /// <summary>Oturum açıldıktan sonra çağrılır; aynı PlayerId için tekrar çağrılması no-op.
        /// Hesap değişiminde (yeni PlayerId) yeniden init eder.</summary>
        public Task EnsureInitializedAsync()
        {
            if (!AuthenticationService.Instance.IsSignedIn)
                return Task.CompletedTask;

            string playerId = AuthenticationService.Instance.PlayerId;
            if (_initializedForPlayerId == playerId && IsAvailable)
                return Task.CompletedTask;
            if (_initTask != null && !_initTask.IsCompleted)
                return _initTask;

            _initTask = InitializeAsync(playerId);
            return _initTask;
        }

        async Task InitializeAsync(string playerId)
        {
            try
            {
                await FriendsService.Instance.InitializeAsync();
                _initializedForPlayerId = playerId;
                IsAvailable = true;
                SubscribeEvents();
                await SetPresenceAsync(Availability.Online, CurrentActivityStatus());
                OnRelationshipsChanged?.Invoke();
            }
            catch (Exception e)
            {
                IsAvailable = false;
#if UNITY_EDITOR
                Debug.LogWarning($"[FriendsManager] Initialize failed: {e.Message}");
#endif
            }
        }

        void SubscribeEvents()
        {
            if (_eventsSubscribed) return; // FriendsService kalıcı — çifte abonelik olmasın
            _eventsSubscribed = true;

            FriendsService.Instance.RelationshipAdded   += _ => OnRelationshipsChanged?.Invoke();
            FriendsService.Instance.RelationshipDeleted += _ => OnRelationshipsChanged?.Invoke();
            FriendsService.Instance.PresenceUpdated     += _ => OnPresenceUpdated?.Invoke();
            FriendsService.Instance.MessageReceived     += HandleMessageReceived;
        }

        void HandleMessageReceived(IMessageReceivedEvent e)
        {
            try
            {
                var invite = e.GetAs<MatchInviteMessage>();
                if (invite == null || invite.type != "match_invite" || string.IsNullOrEmpty(invite.code))
                    return;
                OnMatchInvite?.Invoke(invite, e.UserId);
            }
            catch (Exception ex)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"[FriendsManager] Unreadable message ignored: {ex.Message}");
#endif
            }
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!IsAvailable) return;
            _ = SetPresenceAsync(Availability.Online, CurrentActivityStatus());
        }

        void OnApplicationPause(bool paused)
        {
            if (!IsAvailable) return;
            _ = SetPresenceAsync(paused ? Availability.Away : Availability.Online, CurrentActivityStatus());
        }

        static string CurrentActivityStatus() =>
            SceneManager.GetActiveScene().name == SceneNames.Game ? "Maçta" : "Menüde";

        // ── Data access ──────────────────────────────────────────────────

        public IReadOnlyList<Relationship> Friends =>
            IsAvailable ? FriendsService.Instance.Friends : System.Array.Empty<Relationship>();

        public IReadOnlyList<Relationship> IncomingRequests =>
            IsAvailable ? FriendsService.Instance.IncomingFriendRequests : System.Array.Empty<Relationship>();

        /// <summary>Kendi arkadaş kodun ("Nova731#1234"). Bootstrap player name'i erken eşitler;
        /// yine de boşsa sunucudan çeker.</summary>
        public async Task<string> GetOwnFriendCodeAsync()
        {
            try
            {
                string name = AuthenticationService.Instance.PlayerName;
                if (string.IsNullOrEmpty(name))
                    name = await AuthenticationService.Instance.GetPlayerNameAsync();
                return name;
            }
            catch { return null; }
        }

        // ── Actions ──────────────────────────────────────────────────────

        /// <summary>"Nova731#1234" koduyla (veya '#' içermiyorsa ham PlayerId ile) arkadaşlık
        /// isteği gönderir.</summary>
        public async Task<(bool success, string error)> AddFriendByCodeAsync(string code)
        {
            if (!IsAvailable) return (false, "Arkadaş sistemi şu an kullanılamıyor.");
            code = code?.Trim();
            if (string.IsNullOrEmpty(code)) return (false, "Bir ID gir.");

            try
            {
                if (code.Contains("#")) await FriendsService.Instance.AddFriendByNameAsync(code);
                else                    await FriendsService.Instance.AddFriendAsync(code);
                OnRelationshipsChanged?.Invoke();
                return (true, null);
            }
            catch (Exception e) { return (false, FriendlyError(e)); }
        }

        public async Task<(bool success, string error)> AcceptRequestAsync(string memberId)
        {
            if (!IsAvailable) return (false, "Arkadaş sistemi şu an kullanılamıyor.");
            try
            {
                // Gelen isteği kabul = karşı tarafı arkadaş eklemek
                await FriendsService.Instance.AddFriendAsync(memberId);
                OnRelationshipsChanged?.Invoke();
                return (true, null);
            }
            catch (Exception e) { return (false, FriendlyError(e)); }
        }

        public async Task<(bool success, string error)> DeclineRequestAsync(string memberId)
        {
            if (!IsAvailable) return (false, "Arkadaş sistemi şu an kullanılamıyor.");
            try
            {
                await FriendsService.Instance.DeleteIncomingFriendRequestAsync(memberId);
                OnRelationshipsChanged?.Invoke();
                return (true, null);
            }
            catch (Exception e) { return (false, FriendlyError(e)); }
        }

        public async Task<(bool success, string error)> RemoveFriendAsync(string memberId)
        {
            if (!IsAvailable) return (false, "Arkadaş sistemi şu an kullanılamıyor.");
            try
            {
                await FriendsService.Instance.DeleteFriendAsync(memberId);
                OnRelationshipsChanged?.Invoke();
                return (true, null);
            }
            catch (Exception e) { return (false, FriendlyError(e)); }
        }

        /// <summary>Arkadaşa maç daveti yollar (yalnızca ONLINE arkadaşlara ulaşır).</summary>
        public async Task<(bool success, string error)> SendMatchInviteAsync(string memberId, string sessionCode)
        {
            if (!IsAvailable) return (false, "Arkadaş sistemi şu an kullanılamıyor.");
            try
            {
                var msg = new MatchInviteMessage
                {
                    code     = sessionCode,
                    fromName = PlayerIdentity.Get(),
                };
                await FriendsService.Instance.MessageAsync(memberId, msg);
                return (true, null);
            }
            catch (Exception e) { return (false, FriendlyError(e)); }
        }

        public async Task SetPresenceAsync(Availability availability, string status)
        {
            if (!IsAvailable) return;
            try
            {
                var activity = new PresenceActivity
                {
                    status   = status,
                    trophies = Cloud.LeaderboardManager.Instance != null
                        ? Cloud.LeaderboardManager.Instance.Trophies : 0,
                };
                await FriendsService.Instance.SetPresenceAsync(availability, activity);
            }
            catch (Exception e)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"[FriendsManager] SetPresence failed: {e.Message}");
#endif
            }
        }

        static string FriendlyError(Exception e)
        {
            string m = e.Message ?? "";
            if (m.Contains("not found") || m.Contains("NotFound"))
                return "Bu ID ile bir oyuncu bulunamadı.";
            if (m.Contains("already"))
                return "Bu oyuncu zaten arkadaşın veya istek zaten gönderilmiş.";
            return $"İşlem başarısız: {m}";
        }
    }
}
