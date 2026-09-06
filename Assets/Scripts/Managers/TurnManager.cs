using UnityEngine;
using System;
using System.Collections.Generic;
using Unity.Netcode;
using CosmicRumble.Achievements;
using CosmicRumble.Economy;

// +100: GameInitializer [+10] RegisterPlayers'ı tamamladıktan sonra çalışır.
[DefaultExecutionOrder(100)]
public class TurnManager : NetworkBehaviour
{
    public static TurnManager Instance { get; private set; }

    [Header("Sıra Tabanlı Oynanacak Karakterler")]
    [Tooltip("GravityBody içeren karakter objelerini buraya atayın.")]
    public List<GravityBody> characters;

    [Tooltip("Sıra değişim tuşu")]
    public KeyCode nextTurnKey = KeyCode.Tab;

    /// <summary>Antrenman modu: maç, karakter sayısı 1'e düştüğünde (botlar characters'a hiç
    /// eklenmediği için başlangıçtan itibaren 1'dir) bitmez — GameInitializer set eder.</summary>
    public bool isTrainingMode = false;

    [Header("Tur Süresi Ayarları")]
    [Tooltip("Her karakterin tur süresi (saniye cinsinden)")]
    public float turnDuration = 15f;

    [Tooltip("Host migration sonrası, tur zamanlayıcısı geri sayıma başlamadan önce bırakılan " +
             "ek pay (saniye). Snapshot'taki kalan süre zaten migration boyunca donmuş sayılır " +
             "(bkz. ResumeMatchAfterMigration) — bu pay üstüne, oyuncu yeni host'a bağlanıp " +
             "sahneyi/UI'ı toparlarken bir anlık de-facto dondurmadır (bkz. docs/HOST_MIGRATION_PLAN.md " +
             "madde 3, 'Freeze stays on through migration, plus a grace period after SessionMigrated').")]
    public float migrationGraceSeconds = 3f;
    private float _migrationGraceUntil = 0f;

    private int currentIndex = 0;
    private float turnTimer = 0f;
    private bool gameOver = false;

    // Bu TurnManager bir kez bile ag uzerinde spawn oldu mu? Host migration sirasinda NGO
    // client tarafinda ag'i tamamen kapatir ve IsSpawned false'a duser; bu bayrak olmadan
    // Update() asagida "offline hotseat" dalina girip, kuculmus yerel characters listesi
    // uzerinde CheckGameOver() calistirir ve client kendini yanlislikla kazanan ilan eder.
    // Bir kez true olduktan sonra asla geri alinmaz: "online baslamis mac" kalici bir gercektir.
    private bool _wasOnline = false;

    // Tur zamanlayıcısının client'lara yansıması: Update'teki tur mantığı yalnız server'da
    // çalıştığı için TurnTimerUI client makinede hiç güncellenmiyordu (donuk sayaç).
    // Server her frame kalan süreyi yazar, client'lar salt-okur gösterir.
    private readonly NetworkVariable<float> netTurnTimer =
        new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<float> netTurnDuration =
        new NetworkVariable<float>(15f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private float _matchStartTime;
    private int   _totalShots = 0;

    private static readonly Predicate<GravityBody> _isNull = gb => gb == null;

    // ── Projectile-wait state ─────────────────────────────────────────────
    // Havada mermi varken tur geçişi ve zamanlayıcı dondurulur.
    private int        _activeProjectiles = 0;   // uçuştaki mermi sayısı
    private bool       _pendingNextTurn   = false; // mermi bitince geç
    private GravityBody _currentShooter  = null;  // ateş eden karakter ref'i
    private bool       _weaponConfirmed  = false; // Enter sonrası iptal engeli

    /// <summary>Havada en az 1 mermi var mı?</summary>
    public bool ProjectileInFlight => _activeProjectiles > 0;

    /// <summary>Sırası olan karakterin <see cref="characters"/> içindeki indeksi — host migration
    /// snapshot'ı (HostMigrationDataHandler) sıra düzenini bununla kaydeder.</summary>
    public int CurrentTurnIndex => currentIndex;

    /// <summary>Maç başından beri oynanmış tur sayısı (ilk tur = 1). Host migration snapshot'ı
    /// bunu taşır ki maç yeni host'ta 1. turdan başlıyormuş gibi görünmesin.</summary>
    public int TurnNumber => _turnNumber;

    /// <summary>Aktif turun kalan süresi (saniye) — migration snapshot'ı için.</summary>
    public float RemainingTurnTime => turnTimer;

    /// <summary>Maç hâlâ sürüyor mu — ranked forfeit kararı için: oyuncu maç SONUÇLANMADAN
    /// (TriggerGameOver / AnnounceMatchResultClientRpc henüz gelmeden, yani gameOver hâlâ false
    /// iken) bilinçli ayrılırsa forfeit sayılır; ayrılış zaten doğal maç sonuysa sayılmaz — kupa
    /// değişimi normal RPC yolundan zaten yürüdü/yürüyecek. gameOver alanı host'ta server
    /// mantığıyla, client'larda AnnounceMatchResultClientRpc ile yazıldığı için her iki tarafta
    /// da yerel olarak doğru okunur. Bkz. NetworkBootstrap.LeaveSessionAsync, HM-15.</summary>
    public bool IsMatchInProgress => IsSpawned && !gameOver &&
                                      characters != null && characters.Count >= 2;

    private int _turnNumber = 0;

    /// <summary>Şu an havadaki mermi(ler)i ateşleyen karakter — NotifyProjectileLaunched'ta set
    /// edilir, ilgili mermi(ler) çözülene kadar geçerlidir (tur başına tek aktif karakter olduğu
    /// için bir sonraki atışa kadar değişmez). CombatEventReporter'ın dostane ateş filtresi için.</summary>
    public GravityBody CurrentShooter => _currentShooter;

    /// <summary>ProjectileBase.Start() tarafından çağrılır.</summary>
    public static void NotifyProjectileLaunched()
    {
        if (Instance == null) return;
        if (Instance.IsSpawned && !Instance.IsServer) return;
        Instance._activeProjectiles++;

        // Ateş eden karakteri dondur — turn switch'e kadar hareket edemez
        if (Instance.characters != null &&
            Instance.currentIndex < Instance.characters.Count)
        {
            var gb = Instance.characters[Instance.currentIndex];
            if (gb != null)
            {
                Instance._currentShooter = gb;
                gb.isActive.Value = false;
            }
        }
    }

    /// <summary>ProjectileBase'in tüm imha yolları tarafından çağrılır.</summary>
    public static void NotifyProjectileSettled()
    {
        if (Instance == null) return;
        if (Instance.IsSpawned && !Instance.IsServer) return;
        Instance._activeProjectiles = Mathf.Max(0, Instance._activeProjectiles - 1);
        if (Instance._activeProjectiles > 0) return;   // hâlâ havada mermi var

        if (Instance._pendingNextTurn)
        {
            Instance._pendingNextTurn = false;
            // Kilit burada da açılmalı: pending yolunda aşağıdaki else dalı hiç çalışmaz —
            // açılmazsa atıcının movementLocked'ı sonsuza dek true kalır (kalıcı hareket felci).
            if (Instance._currentShooter != null)
                Instance._currentShooter.movementLocked = false;
            Instance._currentShooter  = null;
            Instance.NextTurn();                        // ertelenmiş geçiş
        }
        else
        {
            // Tur geçişi yoktu — atıcıya kontrolü geri ver
            if (Instance._currentShooter != null)
            {
                Instance._currentShooter.movementLocked = false;
                Instance._currentShooter.isActive.Value = true;
                Instance._currentShooter = null;
            }
            // Mermi patladıktan sonra kalan süreyi 5 saniye ile sınırla
            Instance.turnTimer = Mathf.Min(Instance.turnTimer, 5f);
        }
    }

    /// <summary>Silah onaylandıktan (Enter) sonra ateş edilene kadar yürümeyi dondurur (ateşlemeye izin verir).</summary>
    public static void NotifyWeaponConfirmed()
    {
        if (Instance == null) return;
        if (Instance.characters == null || Instance.currentIndex >= Instance.characters.Count) return;
        var gb = Instance.characters[Instance.currentIndex];
        if (gb == null) return;
        Instance._weaponConfirmed = true;
        Instance._currentShooter = gb;
        gb.movementLocked = true;
    }

    /// <summary>Silah iptali sonrası hareket kilidini kaldırır; Enter ile onaylandıysa iptal edilemez.</summary>
    public static void NotifyWeaponCancelled()
    {
        if (Instance == null) return;
        if (Instance._weaponConfirmed) return;   // Enter sonrası geri alma yok
        if (Instance._activeProjectiles > 0) return;
        if (Instance._currentShooter == null) return;
        Instance._currentShooter.movementLocked = false;
        Instance._currentShooter = null;
    }

    /// <summary>
    /// Mermi atmayan "anlık" yeteneklerin (ör. SuperJump) OnFireUpdate'i başarıyla tükettikten
    /// hemen sonra çağırması gerekir. NotifyWeaponConfirmed hareketi kilitler ve bu kilidi normalde
    /// yalnızca NotifyProjectileSettled açar — ama bu tür ability'ler hiç mermi/NotifyProjectileLaunched
    /// üretmediği için o çağrı hiç gelmez ve movementLocked bir sonraki tur başlayana kadar (tüm tur
    /// boyunca) takılı kalır, karakter yürüyemez hale gelir. (Shield kasıtlı olarak bunu ÇAĞIRMAZ —
    /// kalkan açıkken hareketsiz kalmak tasarım gereği.)
    /// </summary>
    public static void NotifyInstantAbilityUsed()
    {
        if (Instance == null) return;
        if (Instance._currentShooter != null)
        {
            Instance._currentShooter.movementLocked = false;
            Instance._currentShooter = null;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    public override void OnDestroy()
    {
        if (Instance == this) Instance = null;
        base.OnDestroy();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        _wasOnline = true;

        // Reconnect kimlik doğrulaması: her client (ilk bağlantı + rejoin) game sahnesine
        // senkronize olunca kendi UGS PlayerId'sini server'a bildirir. NetworkPlayerSpawner,
        // kopan oyuncunun sahipsiz karakterini yalnızca aynı kimlikle dönen bağlantıya devreder
        // (bkz. NetworkIdentityRegistry).
        if (!IsServer)
        {
            string ugsId = null;
            try { ugsId = Unity.Services.Authentication.AuthenticationService.Instance.PlayerId; }
            catch { /* UGS oturumu yoksa (Editor testleri) kimlik bildirilmez */ }
            if (!string.IsNullOrEmpty(ugsId)) SubmitIdentityServerRpc(ugsId);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SubmitIdentityServerRpc(string ugsPlayerId, ServerRpcParams rpcParams = default)
    {
        CosmicRumble.Utilities.NetworkIdentityRegistry.Report(
            rpcParams.Receive.SenderClientId, ugsPlayerId);
    }

    private void Start()
    {
        // IsSpawned=false → offline hotseat, eski davranış (tek makine her şeyi yönetir).
        // IsSpawned=true  → networked, sadece server tur mantığını çalıştırır.
        if (IsSpawned && !IsServer) return;

        if (characters == null || characters.Count == 0)
        {
            // Offline: GameInitializer RegisterPlayers'ı henüz çağırmamış demektir — uyar.
            // Online (server): oyuncular henüz bağlanmamış olabilir — NetworkPlayerSpawner
            // ikisi de bağlanınca RegisterPlayers() + BeginMatch()'i kendisi çağıracak.
            #if UNITY_EDITOR
            Debug.LogWarning("[TurnManager] characters listesi boş!");
            #endif
            return;
        }

        BeginMatch();
    }

    /// <summary>
    /// Maçı başlatır: ilk oyuncu sayısını bildirir, ilk karakteri aktif eder. Offline'da
    /// Start() tarafından; online'da NetworkPlayerSpawner tarafından (RegisterPlayers'tan
    /// hemen sonra, 2 client de bağlandığında) çağrılır.
    /// </summary>
    public void BeginMatch()
    {
        if (GameConfig.Instance != null)
            turnDuration = GameConfig.Instance.TurnDuration;

        _matchStartTime = Time.time;
        AchievementEvents.FirePlayerCountInMatch(characters.Count);
        ActivateCharacter(0);
    }

    /// <summary>
    /// Host migration sonrası maçı yeni host'ta kaldığı yerden sürdürür: BeginMatch'in
    /// "sıfırdan başlat" yolunun yerine geçer. Sıra, snapshot'taki oyuncuya verilir ve turun
    /// kalan süresi korunur — yeni host'ta tur baştan başlamaz.
    ///
    /// Maç istatistiği sayaçları (AchievementEvents.FirePlayerCountInMatch) bilerek yeniden
    /// tetiklenmez: aynı maç için ikinci kez sayılmaları başarım ilerlemesini çift artırırdı.
    /// </summary>
    public void ResumeMatchAfterMigration(int activeIndex, float remainingTurnTime, int turnNumber)
    {
        if (characters == null || characters.Count == 0) return;

        if (GameConfig.Instance != null)
            turnDuration = GameConfig.Instance.TurnDuration;

        _matchStartTime = Time.time;
        gameOver        = false;

        ActivateCharacter(Mathf.Clamp(activeIndex, 0, characters.Count - 1));

        // ActivateCharacter turu tam süreyle başlatır; snapshot'taki kalan süre onun yerine geçer.
        // 0 veya negatif bir kalan süre turu anında bitirirdi — migration'ın kendisi yüzünden
        // tur kaybedilmesin diye asgari bir pay bırakılır.
        if (remainingTurnTime > 0f)
        {
            turnTimer = Mathf.Min(remainingTurnTime, turnDuration);
            if (IsSpawned && IsServer) netTurnTimer.Value = turnTimer;
            TurnTimerUI.Instance?.UpdateTimerDisplay(turnTimer, turnDuration);
        }

        _turnNumber = Mathf.Max(turnNumber, _turnNumber);

        // Zamanlayıcı bir süre daha donmuş kalır (bkz. yukarıdaki tooltip) — HM-22.
        _migrationGraceUntil = Time.time + Mathf.Max(0f, migrationGraceSeconds);

        Debug.Log($"[HM] Match resumed: activeIndex={currentIndex} remaining={turnTimer:F1}s turn={_turnNumber} grace={migrationGraceSeconds:F1}s");
    }

    private void Update()
    {
        // Client: tur mantığı çalışmaz ama zamanlayıcı server'ın yazdığı NetworkVariable'dan
        // GÖSTERİLMELİ (eskiden client'ta sayaç hiç güncellenmiyordu) ve oyuncu kendi turunu
        // pas geçebilmeli (istek server'da doğrulanır).
        if (IsSpawned && !IsServer)
        {
            if (gameOver) return;
            TurnTimerUI.Instance?.UpdateTimerDisplay(netTurnTimer.Value, Mathf.Max(0.01f, netTurnDuration.Value));

            if (Input.GetKeyDown(nextTurnKey))
                RequestEndTurnServerRpc();
            return;
        }
        if (gameOver) return;

        // Ag coktu (host migration / reconnect): IsSpawned false ama mac online baslamisti.
        // Bu durumda hotseat mantigini CALISTIRMA — asagidaki RemoveAll + CheckGameOver,
        // despawn yuzunden bosalan yerel listeyi "digerleri oldu" sanip sahte galibiyet
        // tetikliyordu. NetworkBootstrap ya yeniden baglanir (IsSpawned tekrar true olur)
        // ya da pes edip MenuScene'e doner; o ana kadar mac donmus kalir.
        if (!IsSpawned && _wasOnline)
        {
            TurnTimerUI.Instance?.UpdateTimerDisplay(turnTimer, Mathf.Max(0.01f, turnDuration));
            return;
        }

        // Yok edilmiş karakterleri her frame temizle (aynı tur içi ölümleri yakala)
        characters.RemoveAll(_isNull);
        if (CheckGameOver()) return;

        if (characters.Count < 2) return;

        // ── Manuel geçiş ─────────────────────────────────────────────────────
        if (Input.GetKeyDown(nextTurnKey))
        {
            // Online host yalnız KENDİ turunu pas geçebilir — eskiden Tab, sıra client'tayken de
            // turu atlıyordu (host rakibin turunu keyfî kesebiliyordu). Offline hotseat'te sıra
            // kimdeyse klavye zaten onda: eski davranış aynen korunur.
            bool allowed = true;
            if (IsSpawned)
            {
                var cur = characters[Mathf.Clamp(currentIndex, 0, characters.Count - 1)];
                allowed = cur != null && NetworkManager.Singleton != null &&
                          cur.OwnerClientId == NetworkManager.Singleton.LocalClientId;
            }
            if (allowed) EndTurnEarly();
        }

        // ── Otomatik zamanlayıcı (mermi uçuşu sırasında ve migration grace penceresinde dondurulur) ──
        if (!ProjectileInFlight && turnTimer > 0f && Time.time >= _migrationGraceUntil)
        {
            turnTimer -= Time.deltaTime;
            if (IsSpawned) netTurnTimer.Value = turnTimer;
            TurnTimerUI.Instance?.UpdateTimerDisplay(turnTimer, turnDuration);

            if (turnTimer <= 0f)
                NextTurn();
        }
    }

    /// <summary>Sıradaki oyuncunun turunu erken bitirir (mermi havadaysa çözülmesini bekler).</summary>
    private void EndTurnEarly()
    {
        if (ProjectileInFlight)
            _pendingNextTurn = true;   // mermi bitince geç
        else
            NextTurn();
    }

    /// <summary>
    /// UI (pas butonu) için ortak giriş noktası — offline'da doğrudan, online'da host/client
    /// ayrımını yaparak doğru yoldan tur bitirir. Sırası olmayanın isteği server'da reddedilir.
    /// </summary>
    public void RequestEndTurn()
    {
        if (gameOver) return;

        if (!IsSpawned)
        {
            if (characters != null && characters.Count >= 2) EndTurnEarly();
            return;
        }

        if (IsServer)
        {
            if (characters == null || characters.Count == 0) return;
            var cur = characters[Mathf.Clamp(currentIndex, 0, characters.Count - 1)];
            if (cur != null && NetworkManager.Singleton != null &&
                cur.OwnerClientId == NetworkManager.Singleton.LocalClientId)
                EndTurnEarly();
            return;
        }

        RequestEndTurnServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestEndTurnServerRpc(ServerRpcParams rpcParams = default)
    {
        if (gameOver || characters == null || characters.Count == 0) return;
        var cur = characters[Mathf.Clamp(currentIndex, 0, characters.Count - 1)];
        // Yalnızca SIRADAKİ karakterin sahibi kendi turunu pas geçebilir.
        if (cur == null || cur.OwnerClientId != rpcParams.Receive.SenderClientId) return;
        EndTurnEarly();
    }

    /// <summary>
    /// Belirlenen indeksteki karakteri aktif yap, eski karakteri pasif hale getir.
    /// Ayrıca UIManager'a yeni karakterin abilities bileşenini bildirir.
    /// </summary>
    /// <param name="newIndex">Yeni aktif karakter indeksi</param>
    private void ActivateCharacter(int newIndex)
    {
        // 1) Önceki karakteri pasif hale getir
        GravityBody oldGb = characters[currentIndex];
        if (oldGb != null)
        {
            var oldAb = oldGb.GetComponent<CharacterAbilities>();
            oldAb?.DeselectAll();
            oldGb.isActive.Value = false;
            // Onaylanmış (Enter) ama ateşlenmemiş silahın hareket kilidi turla birlikte düşmeli —
            // DeselectAll → NotifyWeaponCancelled bunu açAMAZ (_weaponConfirmed hâlâ true), bu
            // yüzden burada koşulsuz açılır.
            oldGb.movementLocked = false;
            oldGb.ZeroHorizontalVelocity();
        }

        _weaponConfirmed = false;

        // 2) Yeni karakteri aktif et
        currentIndex = newIndex;
        GravityBody newGb = characters[currentIndex];
        if (newGb != null)
        {
            newGb.isActive.Value = true;
            // Önceki turlardan sahipsiz kalmış bir kilit varsa temizle (savunmacı — normalde
            // yukarıdaki eski-karakter temizliği yeterli).
            newGb.movementLocked = false;
            newGb.OnTurnStart();
            CameraController.Instance?.SetActiveCharacter(newGb.transform);

            // UIManager’a bağlı abilities güncelle. Online'da UI bağlaması makine başına bir kez
            // CharacterAbilities.OnNetworkSpawn'da (kendi karakterine) yapılır — burada SetCharacter
            // çağrılsaydı host'un paneli rakibin turunda rakibin cephanesine geçerdi ve client'ta
            // hiç çağrılmadığı için panel tamamen ölü kalırdı (mobil client silah bile seçemiyordu).
            var abilities = newGb.GetComponent<CharacterAbilities>();
            if (abilities != null)
            {
                abilities.ResetTurnState();
                if (!IsSpawned)
                {
                    // Offline hotseat: tek makine sırayla tüm karakterleri oynatır — UI aktif karaktere bağlanır.
                    UIManager.Instance?.SetCharacter(abilities);
                    UIManager.Instance?.ClearAllSkillFilters();
                    UIManager.Instance?.ClearAllSkillSelections();
                }
                else if (newGb.IsOwner)
                {
                    // Online host'un kendi turu: bağlama sabit, yalnızca filtre/kilit tazelenir.
                    // (Client'ın tazelemesi netHasUsedSkill reset'inin replikasyonuyla gelir.)
                    UIManager.Instance?.ClearAllSkillFilters();
                    UIManager.Instance?.ClearAllSkillSelections();
                }
            }
        }

        _turnNumber++;

        // Yeni turn süresi başlat
        turnTimer = turnDuration;
        if (IsSpawned && IsServer)
        {
            netTurnTimer.Value = turnTimer;
            netTurnDuration.Value = turnDuration;
        }

        // ⏱ UI başlatma (ilk dolu gösterim)
        TurnTimerUI.Instance?.UpdateTimerDisplay(turnTimer, turnDuration);
    }

    /// <summary>
    /// Sıradaki karaktere geç. Ölü (null) karakterleri listeden temizler.
    /// </summary>
    private void NextTurn()
    {
        characters.RemoveAll(_isNull);
        if (CheckGameOver()) return;
        if (characters.Count < 2) return;

        // currentIndex sınır dışına çıkmış olabilir (ölüm sonrası)
        currentIndex = Mathf.Clamp(currentIndex, 0, characters.Count - 1);
        int nextIndex = (currentIndex + 1) % characters.Count;
        ActivateCharacter(nextIndex);
    }

    /// <summary>
    /// Oyun sona erdi mi kontrol eder. Bittiyse TriggerGameOver çağırır ve true döner.
    /// Takım-farkında: hayatta kalan karakterlerin FARKLI teamId sayısı baz alınır — takımsız
    /// modlarda (Duel1v1/Ffa) her oyuncunun kendi tekil teamId'si olduğu için bu, eski
    /// "characters.Count &lt;= 1" davranışıyla birebir aynı sonucu verir; takım modlarında
    /// (2v2, 3v3, 2v2v2v2, 3v3v3) aynı takımdaki hayatta kalanlar tek grup sayılır.
    /// </summary>
    private bool CheckGameOver()
    {
        if (isTrainingMode) return false; // botlar characters'a hiç eklenmez, tek karakterle bitmesin

        // Ag coktu (host migration) — Update() disindaki cagrilar icin de ayni koruma:
        // despawn sonrasi bosalan yerel liste "tek takim kaldi" gibi gorunur ve sahte
        // galibiyet uretir. Mac online basladiysa ve su an spawn degilse hic karar verme.
        if (!IsSpawned && _wasOnline) return false;

        var survivingTeams = new HashSet<int>();
        foreach (var gb in characters)
            if (gb != null) survivingTeams.Add(gb.teamId.Value);

        if (survivingTeams.Count > 1) return false;

        List<GravityBody> winningTeam = null;
        if (survivingTeams.Count == 1)
        {
            int winningTeamId = 0;
            foreach (int t in survivingTeams) winningTeamId = t; // tek eleman var
            winningTeam = characters.FindAll(gb => gb != null && gb.teamId.Value == winningTeamId);
        }

        TriggerGameOver(winningTeam);
        return true;
    }

    private void TriggerGameOver(List<GravityBody> winningTeam)
    {
        gameOver = true;
        turnTimer = 0f;

        // Aktif karakteri durdur
        foreach (var gb in characters)
            if (gb != null) gb.isActive.Value = false;

        bool hasWinner = winningTeam != null && winningTeam.Count > 0;
        string winnerName = hasWinner
            ? string.Join(", ", winningTeam.ConvertAll(gb => gb.gameObject.name))
            : null;
        #if UNITY_EDITOR
        Debug.Log($"[TurnManager] Game over — Winner: {winnerName ?? "None (Draw)"}");
        #endif

        float matchDuration = Time.time - _matchStartTime;

        if (IsSpawned && IsServer)
        {
            // Online: TriggerGameOver yalnızca server'da çalışır — game-over UI'ı, ödüller,
            // başarımlar ve kupa değişimi HER makinede kendi yerel sonucuna göre RPC içinde
            // işlenir (ClientRpc host'ta da çalışır, bu yüzden burada ayrıca YAPILMAZ; yoksa
            // host çift ödül alırdı, client ise hiçbir şey görmezdi). Takım modlarında kazanan
            // takımın TÜM üyelerinin OwnerClientId'si taşınır, her client kendi id'sinin listede
            // olup olmadığına bakarak "takımım kazandı mı"yı belirler.
            ulong[] winnerClientIds = hasWinner
                ? winningTeam.ConvertAll(gb => gb.OwnerClientId).ToArray()
                : Array.Empty<ulong>();
            AnnounceMatchResultClientRpc(winnerClientIds, winnerName ?? "", matchDuration, _totalShots);
        }
        else
        {
            // Offline hotseat: eski davranış — "biri/bir takım kazandıysa" kazanan akışı.
            // Hotseat hiçbir zaman dereceli değildir.
            FinishMatchLocally(hasWinner, winnerName, matchDuration, _totalShots, ranked: false);
        }
    }

    [ClientRpc]
    private void AnnounceMatchResultClientRpc(ulong[] winnerClientIds, string winnerName, float matchDuration, int totalShots)
    {
        gameOver = true;

        bool isDraw   = winnerClientIds == null || winnerClientIds.Length == 0;
        bool localWon = false;
        if (!isDraw && NetworkManager.Singleton != null)
        {
            ulong localId = NetworkManager.Singleton.LocalClientId;
            foreach (var id in winnerClientIds)
                if (id == localId) { localWon = true; break; }
        }

        // Kupa yalnızca DERECELİ (Quick Match) maçlarda değişir — arkadaş koduyla kurulan
        // maçlar dostluk maçıdır, beraberlikte de değişim yok (Clash Royale kuralları).
        bool ranked = CosmicRumble.Networking.NetworkBootstrap.Instance != null &&
                      CosmicRumble.Networking.NetworkBootstrap.Instance.IsRankedMatch;
        if (!isDraw && ranked)
            CosmicRumble.Cloud.LeaderboardManager.Instance?.ReportOnlineMatchResult(localWon);

        FinishMatchLocally(localWon, string.IsNullOrEmpty(winnerName) ? null : winnerName,
                           matchDuration, totalShots, ranked && !isDraw);
    }

    /// <summary>
    /// Maç sonu yerel işlemleri: başarım event'leri, ses, XP/Gold/sandık ödülleri ve game-over
    /// ekranı. Offline'da server yolundan, online'da her makinede kendi RPC'sinden çağrılır.
    /// </summary>
    private void FinishMatchLocally(bool isWinner, string winnerName, float matchDuration, int totalShots, bool ranked)
    {
        AchievementEvents.FireMatchCompleted(totalShots);
        if (isWinner) AchievementEvents.FireMatchWon();
        else          AchievementEvents.FireMatchLost(winnerName);
        AudioManager.Instance?.PlaySfx(isWinner ? "match_win" : "match_lose");

        if (ranked) AchievementEvents.FireRankedMatchCompleted();
        CosmicRumble.Analytics.AnalyticsManager.Instance?.RecordMatchCompleted(isWinner, ranked);

        // KOZMIK_EKIP: bu maç bir arkadaş daveti ile kuruldu mu (bkz. PartyLobbyPanelUI) —
        // maç türünden bağımsız (kazan/kaybet fark etmez), tek seferlik tüketilir.
        if (!string.IsNullOrEmpty(LobbyData.FriendOpponentId))
        {
            AchievementEvents.FireFriendMatchCompleted(LobbyData.FriendOpponentId);
            LobbyData.FriendOpponentId = null;
        }

        long xp   = MatchRewardCalculator.CalculateMatchXP(isWinner, matchDuration);
        long gold = MatchRewardCalculator.CalculateMatchGold(isWinner, matchDuration);
        CurrencyManager.Instance?.Add(CurrencyType.XP,   xp);
        CurrencyManager.Instance?.Add(CurrencyType.Gold, gold);

        ChestManager.Instance?.TryGrantChest(isWinner);

        UIManager.Instance?.ShowGameOver(winnerName, xp, gold);
    }

    /// <summary>
    /// Atış sayacını artırır — ProjectileBase spawn edildiğinde çağrılmalı.
    /// </summary>
    public void RegisterShot() => _totalShots++;

    // ── Gezegen tahribatı senkronu ────────────────────────────────────────
    // DestructiblePlanet sahneye yerleştirilmiş, NetworkObject'siz bir obje — kendi RPC'si
    // olamaz. TurnManager zaten sahnedeki spawned NetworkBehaviour olduğu için yayın buradan
    // yapılır: server ExplodeWithForce parametrelerini stabil gezegen indeksiyle tüm makinelere
    // iletir, delik her makinede birebir aynı açılır (bkz. DestructiblePlanet.ExplodeWithForce).

    // Host migration FAZ 3 + genel reconnect: sahnesini yeniden yükleyen her makine (yeni host
    // dahil) DestructiblePlanet'i bakir başlatır — o ana kadarki tüm patlamalar burada tutulup
    // (yalnız server'da anlamlı), ihtiyaç duyan her makineye tek seferde tekrar oynatılır.
    private readonly List<PlanetExplosionRecord> _explosionHistory = new List<PlanetExplosionRecord>();

    public static void BroadcastPlanetExplosion(DestructiblePlanet planet, Vector2 pos, float radius, float force)
    {
        if (Instance == null || !Instance.IsSpawned || !Instance.IsServer) return;
        int index = planet != null ? planet.StableIndex : -1;
        if (index < 0) return;
        Instance._explosionHistory.Add(new PlanetExplosionRecord(index, pos, radius));
        Instance.PlanetExplosionClientRpc(index, pos, radius, force);
    }

    [ClientRpc]
    private void PlanetExplosionClientRpc(int planetIndex, Vector2 pos, float radius, float force)
    {
        // ClientRpc host'ta da çalışır — server'ın kendi uygulaması da bu yoldan gelir,
        // böylece delik her makinede tam olarak bir kez açılır.
        DestructiblePlanet.FindByStableIndex(planetIndex)?.ApplyExplosionNow(pos, radius, force);
    }

    /// <summary>Host migration snapshot'ının taşıdığı geçmişi yeni host'ta yeniden kurar —
    /// snapshot'tan önceki patlamalar da hesapta kalsın diye Generate() bu listeyi kullanır.</summary>
    public void SeedExplosionHistory(List<PlanetExplosionRecord> events)
    {
        _explosionHistory.Clear();
        if (events != null) _explosionHistory.AddRange(events);
    }

    public List<PlanetExplosionRecord> ExplosionHistorySnapshot() => new List<PlanetExplosionRecord>(_explosionHistory);

    /// <summary>Tüm bağlı makinelere (yeni host dahil) o ana kadarki tüm patlamaları force=0 ile
    /// tekrar oynatır — migration/reconnect sonrası herkesin gezegeni sunucuyla eşleşsin.</summary>
    public static void ReplayExplosionHistoryToAll()
    {
        if (Instance == null || !Instance.IsSpawned || !Instance.IsServer) return;
        if (Instance._explosionHistory.Count == 0) return;
        Instance.ReplayExplosionHistoryClientRpc(Instance._explosionHistory.ToArray());
    }

    /// <summary>Aynı geçmişi TEK bir yeniden bağlanan client'a hedefli gönderir — o an bağlı
    /// olmayan bir client, ReplayExplosionHistoryToAll'un genel yayınını kaçırmış olabilir.</summary>
    public static void ReplayExplosionHistoryTo(ulong clientId)
    {
        if (Instance == null || !Instance.IsSpawned || !Instance.IsServer) return;
        if (Instance._explosionHistory.Count == 0) return;
        var rpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } }
        };
        Instance.ReplayExplosionHistoryClientRpc(Instance._explosionHistory.ToArray(), rpcParams);
    }

    [ClientRpc]
    private void ReplayExplosionHistoryClientRpc(PlanetExplosionRecord[] events, ClientRpcParams rpcParams = default)
    {
        foreach (var e in events)
            DestructiblePlanet.FindByStableIndex(e.PlanetIndex)?.ApplyExplosionNow(e.Pos, e.Radius, 0f, isReplay: true);
    }

    /// <summary>
    /// Tüm karakterleri (insan + bot) tek seferde kaydet.
    /// GameInitializer.Start() tarafından çağrılır — TurnManager.Start()'tan önce çalışır.
    /// </summary>
    public void RegisterPlayers(List<GravityBody> allPlayers)
    {
        if (allPlayers == null) return;
        characters = new List<GravityBody>(allPlayers);
    }

    /// <summary>RegisterPlayers için GameObject overload'u.</summary>
    public void RegisterPlayers(List<GameObject> allPlayers)
    {
        if (allPlayers == null) return;
        characters = new List<GravityBody>();
        foreach (var go in allPlayers)
        {
            if (go == null) continue;
            var gb = go.GetComponent<GravityBody>();
            if (gb != null) characters.Add(gb);
        }
    }
}

/// <summary>Tek bir gezegen patlamasının kaydı — host migration snapshot'ında ve
/// TurnManager.ReplayExplosionHistory*'de kullanılır. Tamamen blittable (int/Vector2/float) alanlar,
/// NGO'nun RPC serileştirmesi ekstra kod gerektirmeden diziyi taşır.</summary>
public struct PlanetExplosionRecord : Unity.Netcode.INetworkSerializeByMemcpy
{
    public int PlanetIndex;
    public Vector2 Pos;
    public float Radius;

    public PlanetExplosionRecord(int planetIndex, Vector2 pos, float radius)
    {
        PlanetIndex = planetIndex;
        Pos = pos;
        Radius = radius;
    }
}
