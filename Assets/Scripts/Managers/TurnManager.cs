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

    private int currentIndex = 0;
    private float turnTimer = 0f;
    private bool gameOver = false;

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

    private void Update()
    {
        if (IsSpawned && !IsServer) return;
        if (gameOver) return;

        // Yok edilmiş karakterleri her frame temizle (aynı tur içi ölümleri yakala)
        characters.RemoveAll(_isNull);
        if (CheckGameOver()) return;

        if (characters.Count < 2) return;

        // ── Manuel geçiş ─────────────────────────────────────────────────────
        if (Input.GetKeyDown(nextTurnKey))
        {
            if (ProjectileInFlight)
                _pendingNextTurn = true;   // mermi bitince geç
            else
                NextTurn();
        }

        // ── Otomatik zamanlayıcı (mermi uçuşu sırasında dondurulur) ──────────
        if (!ProjectileInFlight && turnTimer > 0f)
        {
            turnTimer -= Time.deltaTime;
            TurnTimerUI.Instance?.UpdateTimerDisplay(turnTimer, turnDuration);

            if (turnTimer <= 0f)
                NextTurn();
        }
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

            // UIManager’a bağlı abilities güncelle
            var abilities = newGb.GetComponent<CharacterAbilities>();
            if (abilities != null)
            {
                abilities.ResetTurnState();
                UIManager.Instance?.SetCharacter(abilities);
                UIManager.Instance?.ClearAllSkillFilters();
                UIManager.Instance?.ClearAllSkillSelections();
            }
        }

        // Yeni turn süresi başlat
        turnTimer = turnDuration;

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
