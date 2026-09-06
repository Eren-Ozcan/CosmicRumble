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
using CosmicRumble.Data;

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

        [Header("Host migration (3+ oyunculu modlar)")]
        [Tooltip("Host koptuktan sonra SDK'nın kendi migration'ını (yeni host seçimi + yeni Relay " +
                 "allocation + otomatik rejoin) tamamlaması için beklenecek azami süre. Bu süre " +
                 "Lobby'nin host'u ölü sayma gecikmesini de kapsar — o gecikme ölçülmüş bir sayı " +
                 "değil (bkz. docs/TEST_PLAN.md HM-20), Photon'daki muadili ~10 saniye. Süre " +
                 "dolarsa kendi elle yeniden katılma döngümüze düşeriz.")]
        public float hostMigrationWaitSeconds = 30f;
        [Tooltip("Lobby uyeligi geri alindiktan sonra NGO tasima katmaninin gercekten baglanmasi " +
                 "icin beklenecek sure (saniye). Dolmasi 'bu deneme basarisiz' demektir — bkz. " +
                 "WaitForTransportAsync.")]
        public float transportWaitSeconds = 10f;
        [Tooltip("Kopustan sonra oyuncunun donmus bir maca bakmasina izin verilen azami toplam sure " +
                 "(saniye). Dolarsa migration/rejoin denemeleri birakilir ve menuye donulur.")]
        public float maxDowntimeSeconds = 60f;

        private ISession _session;
        private bool _wasClient;          // JoinSessionAsync ile bağlandık mı (host değil)
        private bool _intentionalLeave;   // LeaveSessionAsync bilinçli çağrıldıysa true

        // ── Host migration durumu ──────────────────────────────────────────
        private bool _hostMigrationEnabled;   // bu oturum migration açık kurulduysa true
        private bool _migrationInProgress;    // SessionHostChanged geldi, SessionMigrated henüz gelmedi
        private bool _migrationCompleted;     // SessionMigrated geldi (bekleme döngüsü bunu yoklar)

        // HM-20 ölçümü: kopuş → yeni host seçimi → migration tamam zaman damgaları.
        private DateTime? _tLocalDisconnectUtc;
        private DateTime? _tHostChangedUtc;

        /// <summary>Bu oturum host migration ile mi kuruldu (3+ oyunculu modlar). 1v1'de host'un
        /// çıkması migration değil, kalan oyuncunun hükmen galibiyetidir — bkz.
        /// docs/HOST_MIGRATION_PLAN.md, kural 1.</summary>
        public bool HostMigrationEnabled => _hostMigrationEnabled;

        /// <summary>Yeni host seçildi ama taşıma henüz bitmedi. UI/oynanış tarafı bu sırada
        /// girdi ve tur zamanlayıcısını dondurmalı (bkz. TEST_PLAN HM-22).</summary>
        public bool MigrationInProgress => _migrationInProgress;

        GameObject      _statusRoot;
        TextMeshProUGUI _statusText;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            BuildStatusUI();
            ApplyCommandLineOverrides();
        }

        /// <summary>
        /// Otomatik testlerin (bkz. AutoTestBot) migration bekleme suresini sahneyi degistirmeden
        /// ayarlayabilmesi icin: <c>-hmwait 120</c>. HM-20 (host koptuktan sonra Lobby'nin yeni host
        /// secme gecikmesi) olculmemis bir sayi oldugu icin sureyi kosudan kosuya degistirebilmek
        /// gerekiyor. Argüman verilmezse sahnedeki deger aynen kalir.
        /// </summary>
        void ApplyCommandLineOverrides()
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] != "-hmwait") continue;
                if (float.TryParse(args[i + 1], System.Globalization.NumberStyles.Float,
                                   System.Globalization.CultureInfo.InvariantCulture, out float v) && v > 0f)
                {
                    hostMigrationWaitSeconds = v;
                    Debug.Log($"[HM] hostMigrationWaitSeconds overridden from command line: {v}s");
                }
                break;
            }
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
        /// Arkadaş/parti daveti içindir — <c>IsPrivate = true</c>, yani <see cref="QuickMatchAsync"/>'in
        /// genel (dereceli, hep 1v1) havuzunda hiç görünmez, sadece bu kodu bilen biri katılabilir.
        /// MaxPlayers, host'un lobide seçtiği moda göre değişir (LobbyData.SelectedMode) — 1v1'den
        /// 3v3v3'e (9 oyuncu) kadar; host mod seçimini oturumu açmadan ÖNCE yapmış olmalı
        /// (bkz. PartyLobbyPanelUI).
        /// </summary>
        public async Task<string> HostSessionAsync()
        {
            IsBusy = true;
            try
            {
                await EnsureUgsReadyAsync();

                int totalPlayers = GameModeCatalog.ResolveTotalPlayers(LobbyData.SelectedMode, LobbyData.FfaPlayerCount);
                Debug.Log($"[NET] HostSessionAsync: mode={LobbyData.SelectedMode} ffaCount={LobbyData.FfaPlayerCount} totalPlayers={totalPlayers}");
                var options = new SessionOptions { MaxPlayers = totalPlayers, IsPrivate = true }.WithRelayNetwork();

                // Host migration yalnızca 3+ oyunculu modlarda anlamlı: 1v1'de host çıkınca geriye
                // tek oyuncu kalır, o da hükmen galibiyettir (SDK zaten PlayerCount<2 iken snapshot
                // yüklemiyor). Faz 1 kapsamı için bkz. docs/HOST_MIGRATION_PLAN.md.
                if (totalPlayers >= 3)
                {
                    options = options.WithHostMigration(new HostMigrationDataHandler());
                    _hostMigrationEnabled = true;
                }

                var session = await MultiplayerService.Instance.CreateSessionAsync(options);
                AttachSession(session);

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

                // Quick Match her zaman 1v1 — host migration bilerek kapalı, bkz. HostSessionAsync.
                var sessionOptions = new SessionOptions { MaxPlayers = 2 }.WithRelayNetwork();
                var quickJoinOptions = new QuickJoinOptions
                {
                    Timeout       = TimeSpan.FromSeconds(timeoutSeconds),
                    CreateSession = true // eşleşme bulunamazsa kendi genel oturumumuzu kur
                };

                var session = await MultiplayerService.Instance.MatchmakeSessionAsync(quickJoinOptions, sessionOptions);
                AttachSession(session);
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

                // KATILAN TARAF DA migration'i acmak ZORUNDA. SDK'nin NetworkModule'u host
                // degisince su kontrolu yapiyor: HostMigrationHandler == null ise
                // "Host migration is disabled" deyip donuyor — ve o handler YALNIZCA
                // WithHostMigration verilen options'tan kuruluyor. Yani yalnizca host tarafinda
                // acmak, secilen yeni host'un (bir client) hic re-host edememesi demek: canli
                // testte host cikinca SessionHostChanged geliyor ama SessionMigrated hic gelmiyordu
                // (bkz. docs/HOST_MIGRATION_PLAN.md, 2026-09-06 olcumleri).
                var joinOptions = new JoinSessionOptions().WithHostMigration(new HostMigrationDataHandler());
                var session = await MultiplayerService.Instance.JoinSessionByCodeAsync(code, joinOptions);
                AttachSession(session);
                LastJoinCode = code;
                _wasClient = true;
                _intentionalLeave = false;
                IsRankedMatch = false; // kodla katılma = dostluk maçı (reconnect bunu geri yükler, aşağıya bak)
                Debug.Log($"[NET] Joined session code={code}, maxPlayers={session.MaxPlayers} " +
                    $"playerCount={session.PlayerCount} IsClient={NetworkManager.Singleton.IsClient} " +
                    $"IsConnectedClient={NetworkManager.Singleton.IsConnectedClient}");

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

            // Dereceli forfeit (HM-15): oyuncu maç DOĞAL SONUÇLANMADAN kendi isteğiyle
            // ayrılıyorsa (ör. host çıkıyor ya da dereceli 1v1'de client çıkıyor) bunun kupa
            // karşılığı olmalı — aksi halde maçtan giderek erken çıkan biri hiç ceza görmez, geriye
            // kalanlar da (varsa) hiç ödül alamaz. Yalnızca AYRILAN taraf ceza görür (kural: kalan
            // oyuncular cezalandırılmaz); onlar için normal maç-sonu RPC yolu zaten kendi
            // sonucunu üretir (örn. rakip zaman aşımıyla düşerse hükmen galibiyet). Doğal maç sonu
            // sırasında (gameOver=true) burası tetiklenmez — o zaten AnnounceMatchResultClientRpc'den
            // geçmiştir, ikinci bir kupa değişimi burada olmaz.
            if (IsRankedMatch && TurnManager.Instance != null && TurnManager.Instance.IsMatchInProgress)
            {
                Debug.Log("[NET] Deliberate leave mid-ranked-match — reporting forfeit loss.");
                CosmicRumble.Cloud.LeaderboardManager.Instance?.ReportOnlineMatchResult(false);
            }

            try
            {
                if (_session != null)
                {
                    await _session.LeaveAsync();
                    DetachSession();
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
                DetachSession();
                LastJoinCode  = null;
                IsRankedMatch = false;

                _hostMigrationEnabled = false;
                _migrationInProgress  = false;
                _migrationCompleted   = false;
                _tLocalDisconnectUtc  = null;
                _tHostChangedUtc      = null;
                HostMigrationDataHandler.ConsumePending();

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
        /// <param name="playerId">
        /// UGS session player id of the client that disconnected (see
        /// NetworkIdentityRegistry.Get). When null/empty, falls back to removing the first
        /// non-self player — only correct for 2-player sessions.
        /// </param>
        public async Task RemoveDisconnectedPeerAsync(string playerId = null)
        {
            try
            {
                if (_session == null) return;

                // KENDIMIZ cikiyorsak burasi calismamali. Host LeaveSessionAsync cagirinca NGO
                // her client icin OnClientDisconnect uretiyor; her biri buraya dusup KALAN
                // oyunculari Lobby'den atiyordu — yani cikan host, arkasinda kalanlarin oturumunu
                // yok ediyordu ve host migration'a devredilecek bir lobi kalmiyordu (canli testte
                // "SessionNotFound: lobby not found" olarak goruldu). Kopan tek bir client icin bu
                // temizlik hala dogru; kapanan host icin degil.
                if (_intentionalLeave) return;

                var host = _session.AsHost();
                string myId = AuthenticationService.Instance.PlayerId;

                foreach (var p in host.Players)
                {
                    if (p.Id == myId) continue;
                    if (!string.IsNullOrEmpty(playerId) && p.Id != playerId) continue;
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
        //  HOST MIGRATION (3+ oyunculu modlar)
        //  Seçimi ve taşımayı SDK yapar: Lobby yeni host'u atar → NetworkModule
        //  yeni host'ta Relay'i yeniden tahsis edip NetworkManager'ı host olarak
        //  başlatır → diğer client'ları oraya kendisi taşır. Buradaki kod yalnızca
        //  (a) durumu kullanıcıya gösterir, (b) kendi elle rejoin döngümüzün
        //  SDK'nın migration'ıyla yarışmasını engeller, (c) HM-20 için süre ölçer.
        // ════════════════════════════════════════════════════════════════════

        void AttachSession(ISession session)
        {
            DetachSession();
            _session = session;

            // Katılan taraf, oturumun migration ile kurulup kurulmadığını doğrudan göremez;
            // kural kapasiteden okunur — 3+ kişilik her oturum migration'lıdır (bkz.
            // HostSessionAsync).
            if (session != null && session.MaxPlayers >= 3) _hostMigrationEnabled = true;

            if (session == null) return;
            session.SessionHostChanged += OnSessionHostChanged;
            session.SessionMigrated    += OnSessionMigrated;
        }

        void DetachSession()
        {
            if (_session == null) return;
            _session.SessionHostChanged -= OnSessionHostChanged;
            _session.SessionMigrated    -= OnSessionMigrated;
            _session = null;
        }

        void OnSessionHostChanged(string newHostId)
        {
            _tHostChangedUtc     = DateTime.UtcNow;
            _migrationInProgress = true;
            _migrationCompleted  = false;

            string gap = _tLocalDisconnectUtc.HasValue
                ? $"{(_tHostChangedUtc.Value - _tLocalDisconnectUtc.Value).TotalSeconds:F1}s after local disconnect"
                : "no local disconnect recorded";
            Debug.Log($"[HM] SessionHostChanged newHost={newHostId} ({gap})");

            ShowStatus(Loc.T("Host changed, reconnecting..."));
        }

        void OnSessionMigrated()
        {
            _migrationInProgress = false;
            _migrationCompleted  = true;

            // Migration sonrası bu makine host olmuş olabilir — elle rejoin döngüsü artık
            // bizim için geçersiz.
            bool nowHost = _session != null && _session.IsHost;
            _wasClient = !nowHost;

            double total = _tLocalDisconnectUtc.HasValue
                ? (DateTime.UtcNow - _tLocalDisconnectUtc.Value).TotalSeconds : -1;
            Debug.Log($"[HM] SessionMigrated: isHost={nowHost}, total visible downtime={total:F1}s");

            HideStatus();
        }

        /// <summary>SDK'nın migration'ını bekler. Tamamlanırsa true — bu durumda elle yeniden
        /// katılma denenmemeli, aksi halde SDK taşırken ikinci bir üyelik açıp yarışırız.</summary>
        async Task<bool> WaitForHostMigrationAsync()
        {
            var deadline = Time.realtimeSinceStartupAsDouble + hostMigrationWaitSeconds;
            while (Time.realtimeSinceStartupAsDouble < deadline)
            {
                if (_intentionalLeave) return true;   // kullanıcı çıktı, rejoin denemesi anlamsız
                if (_migrationCompleted) return true;
                await Task.Delay(250);
            }

            Debug.LogWarning($"[HM] Host migration did not complete within {hostMigrationWaitSeconds}s " +
                             $"(hostChanged={_tHostChangedUtc.HasValue}) — falling back to manual rejoin.");
            // Migration'ı beklemeyi bıraktık — durum artık "taşıma sürüyor" değil, ondan sonraki
            // elle rejoin döngüsü kendi banner'ını basar. Bayrağı burada bırakmak (kural 5: hiçbir
            // zaman frozen client bırakma) hiçbir yerin gözlemlemediği kalıcı yanlış bir durum
            // olurdu; ranked maçlar zaten hiç migration'lı kurulmuyor (bkz. HostSessionAsync/
            // QuickMatchAsync), yani bu yol hiçbir zaman bir kupa RPC'siyle çakışmaz.
            _migrationInProgress = false;
            return false;
        }

        // ════════════════════════════════════════════════════════════════════
        //  RECONNECT (client tarafı — kendi bağlantımız koptuğunda)
        // ════════════════════════════════════════════════════════════════════

        async void OnUnexpectedDisconnect(ulong clientId)
        {
            if (!_wasClient) return;                 // biz host'tuk, bu bizim işimiz değil
            if (_intentionalLeave) return;            // kendi isteğimizle ayrıldık
            if (clientId != NetworkManager.Singleton.LocalClientId) return; // başkasının kopuşu

            // Banner, Lobby'nin yeni host'u seçmesini BEKLEMEDEN burada açılır: aradaki tespit
            // boşluğu saniyeler sürebiliyor ve oyuncu o sırada donmuş bir maça bakıyor olur
            // (HM-21). Ölçüm için kopuş anı da burada damgalanır (HM-20).
            _tLocalDisconnectUtc = DateTime.UtcNow;
            _tHostChangedUtc     = null;
            _migrationCompleted  = false;
            ShowStatus(Loc.T("Connection lost, reconnecting..."));

            // Migration'lı bir oturumda önce SDK'ya şansı verilir; elle rejoin yalnızca o
            // başarısız olursa devreye girer.
            if (_hostMigrationEnabled && await WaitForHostMigrationAsync())
                return;

            string codeToRetry = LastJoinCode;
            bool wasRanked = IsRankedMatch; // JoinSessionAsync bayrağı sıfırlar; rejoin sonrası geri yüklenir
            if (string.IsNullOrEmpty(codeToRetry))
            {
                Debug.LogWarning("[NET] Unexpected disconnect but no LastJoinCode to retry with.");
                return;
            }

            for (int attempt = 1; attempt <= reconnectAttempts; attempt++)
            {
                // Toplam donma butcesi: migration beklemesi + rejoin denemeleri birlikte bu sureyi
                // asamaz. Asarsa oyuncuyu bir daha asla gelmeyecek bir host'u beklerken birakmak
                // yerine menuye dondururuz (bkz. TEST_PLAN HM-13).
                if (_tLocalDisconnectUtc.HasValue &&
                    (DateTime.UtcNow - _tLocalDisconnectUtc.Value).TotalSeconds > maxDowntimeSeconds)
                {
                    Debug.LogWarning($"[NET] Downtime budget of {maxDowntimeSeconds}s exceeded — " +
                                     "abandoning the match and returning to the menu.");
                    break;
                }

                ShowStatus(string.Format(Loc.T("Connection lost, reconnecting... (attempt {0}/{1})"), attempt, reconnectAttempts));
                Debug.Log($"[NET] Reconnect attempt {attempt}/{reconnectAttempts} with code={codeToRetry}");
                await Task.Delay(TimeSpan.FromSeconds(reconnectDelaySeconds));

                if (_intentionalLeave) return; // bu sırada kullanıcı kendi çıktıysa vazgeç

                bool ok = await ReconnectOrRejoinAsync(codeToRetry);
                if (ok)
                {
                    IsRankedMatch = wasRanked; // dereceli maça rejoin, dereceli kalır
                    Debug.Log("[NET] Reconnect succeeded.");
                    HideStatus();
                    return;
                }
            }

            Debug.LogWarning("[NET] Reconnect failed, giving up and returning to the menu.");
            ShowStatus(Loc.T("Connection lost completely."));
            await Task.Delay(TimeSpan.FromSeconds(2f));
            HideStatus();
            UnityEngine.SceneManagement.SceneManager.LoadScene(SceneNames.Menu);
        }

        /// <summary>
        /// Elle rejoin döngüsünün tek adımı. Biz host'tuk ve koptuk demek: karşı taraf bizi
        /// Lobby'den hiç ATMADI (RemoveDisconnectedPeerAsync yalnızca host'un normalde çağırdığı
        /// bir şey — burada host biziz, biz de koptuk, kimse temizlik yapmadı). Bu yüzden taze
        /// JoinSessionByCodeAsync "SessionConflict: player is already a member of the lobby" ile
        /// reddedilir (canlı testte ölçüldü — bkz. host migration test notları). Önce MEVCUT
        /// üyelikle ReconnectAsync denenir; yalnızca üyelik gerçekten kalmamışsa (host bizi ayrı
        /// bir yoldan temizlemişse) taze JoinSessionAsync'e düşülür.
        /// </summary>
        async Task<bool> ReconnectOrRejoinAsync(string code)
        {
            if (_session != null)
            {
                try
                {
                    await _session.ReconnectAsync();
                    Debug.Log("[NET] ReconnectAsync succeeded on existing session membership.");
                    return await WaitForTransportAsync();
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[NET] ReconnectAsync failed, falling back to fresh join: {e.Message}");
                }
            }

            if (!await JoinSessionAsync(code)) return false;
            return await WaitForTransportAsync();
        }

        /// <summary>
        /// Lobby uyeligini geri almak TEK BASINA yeterli degil: host'un process'i olduyse Lobby
        /// bizi uye olarak kabul etse bile bagalanacak bir NGO host'u yoktur. ReconnectAsync
        /// "basarili" doner, oyuncu ise sonsuza kadar donmus bir mac ekraninda kalir. Bu yuzden
        /// gercek olcut tasima katmani: NetworkManager yeniden baglandi mi.
        /// Baglanmadiysa cagiran taraf bunu basarisiz deneme sayar; tum denemeler bitince
        /// OnUnexpectedDisconnect MenuScene'e doner (kural: asla donmus client birakma).
        /// </summary>
        async Task<bool> WaitForTransportAsync()
        {
            var nm = NetworkManager.Singleton;
            if (nm == null) return false;

            var deadline = Time.realtimeSinceStartupAsDouble + transportWaitSeconds;
            while (Time.realtimeSinceStartupAsDouble < deadline)
            {
                if (_intentionalLeave) return false;
                if (nm.IsConnectedClient || nm.IsHost) return true;
                await Task.Delay(250);
            }

            Debug.LogWarning($"[NET] Session membership restored but the transport never reconnected " +
                             $"within {transportWaitSeconds}s (no live host) — treating as a failed attempt.");
            return false;
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
