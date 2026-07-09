using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CosmicRumble.Localization;

namespace CosmicRumble.Networking
{
    /// <summary>
    /// Unity Multiplayer Services (Session API, Relay üzerinden) + Netcode for GameObjects
    /// köprüsü. UGS init/sign-in'i CloudSaveManager'ın kurduğu aynı oturumu kullanır, tekrar
    /// başlatmaz. Gerçek satın alma/host/join akışı OnlineLobbyPanelUI'dan çağrılır.
    ///
    /// Ayrıca kalıcı (DontDestroyOnLoad, sahne geçişlerinde hayatta kalan) küçük bir durum
    /// banner'ı taşır — OnlineLobbyPanelUI'nin kendi "bağlantı kesildi" ekranı sadece MenuScene'de
    /// yaşadığı için maç sahnesine (SampleScene) geçildikten sonra asla tetiklenemiyordu; bu
    /// banner o boşluğu kapatır ve hem client'ın kendi yeniden-bağlanma denemesini hem de
    /// NetworkPlayerSpawner'ın "rakip koptu, bekleniyor" mesajını göstermek için kullanılır.
    /// </summary>
    public class NetworkBootstrap : MonoBehaviour
    {
        public static NetworkBootstrap Instance { get; private set; }

        public string LastJoinCode { get; private set; }
        public bool IsBusy { get; private set; }

        /// <summary>
        /// Bu oturum DERECELİ mi? Quick Match ile kurulan maçlar dereceli (kupa +30/−20),
        /// arkadaş koduyla (Host/Join) kurulanlar dostluk maçıdır — kupa değişmez
        /// (Clash Royale'deki friendly battle kuralı). TurnManager maç sonunda bunu okur.
        /// </summary>
        public bool IsRankedMatch { get; private set; }

        [Header("Reconnect (client-tarafı, kendi bağlantımız koparsa)")]
        [Tooltip("Beklenmedik kopuşta kaç kez yeniden katılma denenecek. Host taraflı " +
                 "NetworkPlayerSpawner artık disconnect anında RemoveDisconnectedPeerAsync ile " +
                 "UGS Session/Lobby üyeliğini de temizliyor, bu yüzden rejoin genelde saniyeler " +
                 "içinde başarılı olur -- yine de ağ gecikmesi/geçici hatalar için makul bir pay bırakıldı.")]
        public int reconnectAttempts = 6;
        [Tooltip("Denemeler arası bekleme (saniye)")]
        public float reconnectDelaySeconds = 5f;

        private ISession _session;
        private bool _wasClient;          // JoinSessionAsync ile bağlandık mı (host değil)
        private bool _intentionalLeave;   // LeaveSessionAsync bilinçli çağrıldıysa true

        GameObject      _statusRoot;
        TextMeshProUGUI _statusText;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            BuildStatusUI();
        }

        async Task EnsureUgsReadyAsync()
        {
            if (UnityServices.State == ServicesInitializationState.Uninitialized)
                await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        /// <summary>
        /// Bir Relay oturumu oluşturur, host olarak başlar. Başarılıysa katılım kodunu döner.
        /// Belirli bir arkadaşı davet etmek içindir — <c>IsPrivate = true</c>, yani
        /// <see cref="QuickMatchAsync"/>'in genel havuzunda hiç görünmez, sadece bu kodu bilen biri
        /// katılabilir.
        /// </summary>
        public async Task<string> HostSessionAsync()
        {
            IsBusy = true;
            try
            {
                await EnsureUgsReadyAsync();

                var options = new SessionOptions { MaxPlayers = 2, IsPrivate = true }.WithRelayNetwork();
                var session = await MultiplayerService.Instance.CreateSessionAsync(options);
                _session = session;

                LastJoinCode = session.Code;
                _wasClient = false;
                IsRankedMatch = false; // arkadaş daveti = dostluk maçı
                Debug.Log($"[NET] Hosted session, code={LastJoinCode}, IsHost={NetworkManager.Singleton.IsHost}");
                return LastJoinCode;
            }
            catch (Exception e)
            {
                Debug.LogError($"[NET] HostSessionAsync failed: {e}");
                return null;
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Hızlı Eşleşme: genel (public) havuzda bekleyen bir rakip varsa ona katılır; yoksa kendi
        /// genel oturumunu oluşturup rakip bekler. Tek bir SDK çağrısı (<c>MatchmakeSessionAsync</c>)
        /// hem "ara" hem "bulamazsan sen oluştur" akışını kapsıyor — ayrı bir lobby-tarama kodu
        /// yazmaya gerek yok. Steam/mobil ayrımı YOK (bilerek) — tek, birleşik havuz.
        /// </summary>
        public async Task<bool> QuickMatchAsync(float timeoutSeconds = 20f)
        {
            IsBusy = true;
            try
            {
                await EnsureUgsReadyAsync();

                var sessionOptions = new SessionOptions { MaxPlayers = 2 }.WithRelayNetwork();
                var quickJoinOptions = new QuickJoinOptions
                {
                    Timeout       = TimeSpan.FromSeconds(timeoutSeconds),
                    CreateSession = true // eşleşme bulunamazsa kendi genel oturumumuzu kur
                };

                var session = await MultiplayerService.Instance.MatchmakeSessionAsync(quickJoinOptions, sessionOptions);
                _session = session;
                LastJoinCode = session.Code;
                IsRankedMatch = true; // Quick Match = dereceli (kupa sistemi işler)

                bool becameHost = NetworkManager.Singleton.IsHost;
                _wasClient = !becameHost;
                if (_wasClient)
                {
                    _intentionalLeave = false;
                    NetworkManager.Singleton.OnClientDisconnectCallback -= OnUnexpectedDisconnect;
                    NetworkManager.Singleton.OnClientDisconnectCallback += OnUnexpectedDisconnect;
                }

                Debug.Log($"[NET] QuickMatch succeeded, becameHost={becameHost}, code={LastJoinCode}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[NET] QuickMatchAsync failed: {e}");
                return false;
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>Quick Match sonucunda biz mi host olduk, yoksa mevcut birine mi katıldık.</summary>
        public bool IsHostAfterQuickMatch => NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost;

        /// <summary>Verilen katılım koduyla mevcut bir oturuma bağlanır (client olarak).</summary>
        public async Task<bool> JoinSessionAsync(string code)
        {
            IsBusy = true;
            try
            {
                await EnsureUgsReadyAsync();

                var session = await MultiplayerService.Instance.JoinSessionByCodeAsync(code, new JoinSessionOptions());
                _session = session;
                LastJoinCode = code;
                _wasClient = true;
                _intentionalLeave = false;
                IsRankedMatch = false; // kodla katılma = dostluk maçı (reconnect bunu geri yükler, aşağıya bak)
                Debug.Log($"[NET] Joined session code={code}, IsClient={NetworkManager.Singleton.IsClient}");

                NetworkManager.Singleton.OnClientDisconnectCallback -= OnUnexpectedDisconnect;
                NetworkManager.Singleton.OnClientDisconnectCallback += OnUnexpectedDisconnect;
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[NET] JoinSessionAsync failed: {e}");
                return false;
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Aktif oturumdan ayrılır (host veya client fark etmez) ve NetworkManager'ı kapatır.
        /// Bağlantı denemesi sırasında (BACK/İptal) veya maç bittiğinde temiz bir şekilde çağrılır.
        /// </summary>
        public async Task LeaveSessionAsync()
        {
            _intentionalLeave = true;
            try
            {
                if (_session != null)
                {
                    await _session.LeaveAsync();
                    _session = null;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[NET] LeaveSessionAsync: session leave failed (continuing shutdown anyway): {e}");
            }
            finally
            {
                if (NetworkManager.Singleton != null)
                {
                    NetworkManager.Singleton.OnClientDisconnectCallback -= OnUnexpectedDisconnect;
                    if (NetworkManager.Singleton.IsListening)
                        NetworkManager.Singleton.Shutdown();
                }
                LastJoinCode = null;
                IsRankedMatch = false;
                HideStatus();
            }
        }

        /// <summary>
        /// Host-only: bir client mid-match beklenmedik şekilde koptuğunda UGS Session/Lobby
        /// seviyesindeki üyeliğini de temizler. NGO'nun kendi disconnect'i sadece transport
        /// bağlantısını koparır — Session/Lobby'nin kendi üyelik kaydı ayrı bir katman ve
        /// otomatik zaman aşımıyla silinmiyor (canlı testte 250s+ beklemeye rağmen hâlâ
        /// "player is already a member of the lobby" hatası alınıyordu) — bu yüzden aynı kimlikle
        /// gerçek bir rejoin'in çalışabilmesi için host'un bunu açıkça yapması gerekiyor.
        /// </summary>
        public async Task RemoveDisconnectedPeerAsync()
        {
            try
            {
                if (_session == null) return;
                var host = _session.AsHost();
                string myId = AuthenticationService.Instance.PlayerId;

                foreach (var p in host.Players)
                {
                    if (p.Id == myId) continue;
                    await host.RemovePlayerAsync(p.Id);
                    Debug.Log($"[NET] RemoveDisconnectedPeerAsync: removed stale session player {p.Id}");
                    return;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[NET] RemoveDisconnectedPeerAsync failed: {e}");
            }
        }

        // ════════════════════════════════════════════════════════════════════
        //  RECONNECT (sadece client tarafı — host'un kendisi kopunca oturumun
        //  tamamı zaten sona erer, host migration kapsam dışı)
        // ════════════════════════════════════════════════════════════════════

        async void OnUnexpectedDisconnect(ulong clientId)
        {
            if (!_wasClient) return;                 // biz host'tuk, bu bizim işimiz değil
            if (_intentionalLeave) return;            // kendi isteğimizle ayrıldık
            if (clientId != NetworkManager.Singleton.LocalClientId) return; // başkasının kopuşu

            string codeToRetry = LastJoinCode;
            bool wasRanked = IsRankedMatch; // JoinSessionAsync bayrağı sıfırlar; rejoin sonrası geri yüklenir
            if (string.IsNullOrEmpty(codeToRetry))
            {
                Debug.LogWarning("[NET] Unexpected disconnect but no LastJoinCode to retry with.");
                return;
            }

            for (int attempt = 1; attempt <= reconnectAttempts; attempt++)
            {
                ShowStatus(string.Format(Loc.T("Connection lost, reconnecting... (attempt {0}/{1})"), attempt, reconnectAttempts));
                Debug.Log($"[NET] Reconnect attempt {attempt}/{reconnectAttempts} with code={codeToRetry}");
                await Task.Delay(TimeSpan.FromSeconds(reconnectDelaySeconds));

                if (_intentionalLeave) return; // bu sırada kullanıcı kendi çıktıysa vazgeç

                bool ok = await JoinSessionAsync(codeToRetry);
                if (ok)
                {
                    IsRankedMatch = wasRanked; // dereceli maça rejoin, dereceli kalır
                    Debug.Log("[NET] Reconnect succeeded.");
                    HideStatus();
                    return;
                }
            }

            Debug.LogWarning("[NET] Reconnect failed after all attempts, giving up.");
            ShowStatus(Loc.T("Connection lost completely."));
            await Task.Delay(TimeSpan.FromSeconds(2f));
            HideStatus();
            UnityEngine.SceneManagement.SceneManager.LoadScene(SceneNames.Menu);
        }

        // ════════════════════════════════════════════════════════════════════
        //  KALICI DURUM BANNER'I (sahne geçişlerinde hayatta kalır)
        // ════════════════════════════════════════════════════════════════════

        public void ShowStatus(string message)
        {
            _statusText.text = message;
            _statusRoot.SetActive(true);
        }

        public void HideStatus() => _statusRoot.SetActive(false);

        void BuildStatusUI()
        {
            var canvasGO = new GameObject("ConnectionStatusCanvas");
            canvasGO.transform.SetParent(transform, false);
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100; // her şeyin üstünde
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight  = 0.5f;
            canvasGO.AddComponent<GraphicRaycaster>();

            _statusRoot = new GameObject("StatusBanner");
            _statusRoot.transform.SetParent(canvasGO.transform, false);
            var bg = _statusRoot.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.85f);
            var bgRt = bg.rectTransform;
            bgRt.anchorMin = new Vector2(0.5f, 0.92f);
            bgRt.anchorMax = new Vector2(0.5f, 0.92f);
            bgRt.sizeDelta = new Vector2(900, 70);

            var textGO = new GameObject("Text");
            textGO.transform.SetParent(_statusRoot.transform, false);
            _statusText = textGO.AddComponent<TextMeshProUGUI>();
            _statusText.fontSize  = 24;
            _statusText.color     = new Color(1f, 0.8f, 0.2f);
            _statusText.alignment = TextAlignmentOptions.Center;
            var trt = _statusText.rectTransform;
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = trt.offsetMax = Vector2.zero;

            _statusRoot.SetActive(false);
        }
    }
}
