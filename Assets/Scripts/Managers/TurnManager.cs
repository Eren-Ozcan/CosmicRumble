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
            oldGb.ZeroHorizontalVelocity();
        }

        _weaponConfirmed = false;

        // 2) Yeni karakteri aktif et
        currentIndex = newIndex;
        GravityBody newGb = characters[currentIndex];
        if (newGb != null)
        {
            newGb.isActive.Value = true;
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
    /// </summary>
    private bool CheckGameOver()
    {
        if (characters.Count > 1) return false;

        GravityBody winner = characters.Count == 1 ? characters[0] : null;
        TriggerGameOver(winner);
        return true;
    }

    private void TriggerGameOver(GravityBody winner)
    {
        gameOver = true;
        turnTimer = 0f;

        // Aktif karakteri durdur
        foreach (var gb in characters)
            if (gb != null) gb.isActive.Value = false;

        string winnerName = winner != null ? winner.gameObject.name : null;
        #if UNITY_EDITOR
        Debug.Log($"[TurnManager] Game over — Winner: {winnerName ?? "None (Draw)"}");
        #endif

        // ── Economy & Achievement entegrasyonu ────────────────────────────────
        // Local player: characters listesinde kalan tek karakter = winner
        // Multiplayer entegrasyonu gelince isLocalPlayerWinner doğru kaynaktan alınacak
        bool isWinner = winner != null;
        float matchDuration = Time.time - _matchStartTime;

        AchievementEvents.FireMatchCompleted(_totalShots);
        if (isWinner) AchievementEvents.FireMatchWon();
        else          AchievementEvents.FireMatchLost();
        AudioManager.Instance?.PlaySfx(isWinner ? "match_win" : "match_lose");

        long xp   = MatchRewardCalculator.CalculateMatchXP(isWinner, matchDuration);
        long gold = MatchRewardCalculator.CalculateMatchGold(isWinner, matchDuration);
        CurrencyManager.Instance?.Add(CurrencyType.XP,   xp);
        CurrencyManager.Instance?.Add(CurrencyType.Gold, gold);

        ChestManager.Instance?.TryGrantChest(isWinner);
        // ─────────────────────────────────────────────────────────────────────

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
