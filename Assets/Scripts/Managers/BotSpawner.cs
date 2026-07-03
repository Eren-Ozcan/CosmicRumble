using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Bot spawn — yüzey/pozisyon hesabı SpawnPositioning'de yaşıyor (GameInitializer
/// ve SpawnDebugger de aynı hesabı paylaşıyor), bu sınıf yalnızca "Bot_N" adıyla
/// Player prefabını spawn etmekle ilgileniyor.
///
/// Kasıtlı olarak PlayerController2D/ability'leri DEVRE DIŞI BIRAKMIYOR: botlar
/// test amaçlı, sırası geldiğinde aynı yerel oyuncu tarafından kontrol edilebilir
/// olsun diye normal bir oyuncuyla birebir aynı (hot-seat). TurnManager zaten
/// yalnızca aktif GravityBody'nin girişini kabul ediyor, o yüzden ekstra bir
/// kısıtlama gerekmiyor.
/// </summary>
public class BotSpawner : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Player prefab (Assets/Art/Sprites/Prefabs/Player.prefab)")]
    public GameObject botPrefab;

    [Header("Config")]
    [Tooltip("LobbyData.BotCount üzerinden GameInitializer tarafından set edilir.")]
    public int botCount = 0;

    public List<GameObject> SpawnBots(List<SpawnPositioning.SpawnSlot> slots, int humanSlotIndex = 0)
    {
        var spawned = new List<GameObject>();

        if (botPrefab == null)
        {
#if UNITY_EDITOR
            Debug.LogError("[BotSpawner] botPrefab atanmamış!");
#endif
            return spawned;
        }

        int botIndex = 0;
        for (int s = 0; s < slots.Count; s++)
        {
            if (s == humanSlotIndex) continue;

            var slot = slots[s];
            var bot  = Instantiate(botPrefab, slot.position, Quaternion.identity);
            bot.name         = $"Bot_{++botIndex}";
            bot.transform.up = slot.upDir;

            // Kasıtlı olarak "Player" tag'i korunuyor (prefab'tan miras) — "Bot" tag'i
            // BatHammerSkill.onlyAffectTaggedPlayers gibi CompareTag("Player") kontrollerini
            // atlatıp botları bazı silahlara karşı bağışık kılardı, "gerçek oyuncuyla birebir
            // aynı" hedefiyle çelişirdi.

            spawned.Add(bot);
#if UNITY_EDITOR
            Debug.Log($"[BotSpawner] {bot.name} → {slot.position}");
#endif
        }

        return spawned;
    }
}
