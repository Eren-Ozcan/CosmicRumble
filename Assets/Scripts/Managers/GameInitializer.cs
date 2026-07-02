using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// GameScene yüklenince çalışır.
/// Tüm GravitySource'ları bulur, açısal dağılım + Raycast ile
/// insan + bot spawn pozisyonlarını hesaplar, TurnManager'a kaydeder.
///
/// Inspector atamaları:
///   Human Prefab : Assets/Art/Sprites/Prefabs/Player.prefab
///   Bot Spawner  : BotSpawner component'ı olan herhangi bir GO
///
/// [DefaultExecutionOrder(-100)] → TurnManager.Start()'tan önce çalışır.
/// </summary>
// +10: DestructiblePlanet (0) önce Start() yaparak polygon'ı solid + doğru boyuta getirir,
// sonra GameInitializer çalışır → BotSpawner doğru yüzey noktasını bulur.
// TurnManager [+100] ise RegisterPlayers'tan sonra çalışır.
[DefaultExecutionOrder(10)]
public class GameInitializer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] GameObject _humanPrefab;
    [SerializeField] BotSpawner _botSpawner;

    void Start()
    {
        // ── 0. Gezegenleri bul, sırala ───────────────────────────────────
        var sortedPlanets = BotSpawner.GetSortedPlanets();
        if (sortedPlanets.Count == 0)
            #if UNITY_EDITOR
            Debug.LogWarning("[GameInitializer] Sahnede hiç GravitySource bulunamadı!");
            #endif

        // ── 1. Bot sayısını LobbyData'dan al ─────────────────────────────
        int botCount = 1;
        if (_botSpawner != null)
        {
            _botSpawner.botCount = LobbyData.BotCount;
            botCount = _botSpawner.botCount;
        }

        int totalPlayers = 1 + botCount; // 1 insan + N bot

        // ── 2. Tüm spawn slotlarını hesapla ──────────────────────────────
        var slots = BotSpawner.CalculateSpawnPositions(totalPlayers, sortedPlanets);

        // slot sayısı yetersizse padding ekle (kenar durum)
        while (slots.Count < totalPlayers)
            slots.Add(new BotSpawner.SpawnSlot
            {
                position = Vector3.up * 3f,
                upDir    = Vector3.up
            });

        var allPlayers = new List<GravityBody>();

        // ── 3. İnsan oyuncuyu spawn et (slot 0) ──────────────────────────
        string humanName = (AuthManager.Instance != null && AuthManager.Instance.IsLoggedIn)
            ? AuthManager.Instance.CurrentUsername
            : "Oyuncu";

        if (_humanPrefab != null)
        {
            var s       = slots[0];
            var humanGO = Instantiate(_humanPrefab, s.position, Quaternion.identity);
            humanGO.name         = humanName;
            humanGO.transform.up = s.upDir;
            AddNameTag(humanGO, humanName);
            AddHealthBar(humanGO);

            var gb = humanGO.GetComponent<GravityBody>();
            if (gb != null) allPlayers.Add(gb);

            #if UNITY_EDITOR
            Debug.Log($"[GameInitializer] {humanName} → {s.position}");
            #endif
        }
        else
        {
            // Geri uyumluluk: sahnede zaten insan varsa onu kullan
            var existing = FindFirstObjectByType<PlayerController2D>();
            if (existing != null)
            {
                existing.gameObject.name = humanName;
                AddNameTag(existing.gameObject, humanName);
                AddHealthBar(existing.gameObject);
                var gb = existing.GetComponent<GravityBody>();
                if (gb != null) allPlayers.Add(gb);
                #if UNITY_EDITOR
                Debug.LogWarning("[GameInitializer] humanPrefab atanmamış — sahnedeki oyuncu kullanıldı.");
                #endif
            }
        }

        // ── 4. Botları spawn et (slot 1..n) ──────────────────────────────
        if (_botSpawner != null)
        {
            var spawnedBots = _botSpawner.SpawnBots(slots, humanSlotIndex: 0);
            foreach (var botGO in spawnedBots)
            {
                if (botGO == null) continue;
                AddNameTag(botGO, botGO.name);
                AddHealthBar(botGO);
                var gb = botGO.GetComponent<GravityBody>();
                if (gb != null) allPlayers.Add(gb);
            }
        }

        // ── 5. TurnManager'a kaydet ───────────────────────────────────────
        var turnManager = FindFirstObjectByType<TurnManager>();
        if (turnManager != null)
        {
            turnManager.RegisterPlayers(allPlayers);
            #if UNITY_EDITOR
            Debug.Log($"[GameInitializer] TurnManager'a {allPlayers.Count} karakter kaydedildi.");
            #endif
        }
        else
        {
            #if UNITY_EDITOR
            Debug.LogWarning("[GameInitializer] TurnManager sahnede bulunamadı!");
            #endif
        }

        // ── 6. Profil güncelle (misafir için atla) ────────────────────────
        if (AuthManager.Instance != null
            && !AuthManager.Instance.IsGuest
            && AuthManager.Instance.CurrentProfile != null)
        {
            AuthManager.Instance.CurrentProfile.matchesPlayed++;
            AuthManager.Instance.CurrentProfile.Save();
        }
    }

    // ── Yardımcı ──────────────────────────────────────────────────────────

    static void AddNameTag(GameObject go, string characterName)
    {
        var tag = go.GetComponent<CharacterNameTag>();
        if (tag == null) tag = go.AddComponent<CharacterNameTag>();
        tag.SetName(characterName);
    }

    static void AddHealthBar(GameObject go)
    {
        var existing = go.GetComponent<HealthBarUI>();
        if (existing == null) go.AddComponent<HealthBarUI>();
    }
}
