using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gezegen yüzeyinde spawn pozisyonu hesabı (oyuncu + debug araçları için).
///
/// Yüzey tespiti:
///   1. gravity alanı dışından içe RaycastAll
///   2. Sıralı çarpmalarda ilk trigger (= GravityTrigger dış sınırı) atlanır
///   3. Sonraki çarpma = gezegen yüzeyi (solid veya trigger surface)
///   4. SpawnLift kadar yukarı kaydırılır
/// </summary>
public static class SpawnPositioning
{
    /// <summary>Yüzey noktasının dışına eklenen mesafe (pivot → ayak + buffer).</summary>
    public static float SpawnLift = 0.5f;

    public struct SpawnSlot
    {
        public Vector3 position;
        public Vector3 upDir;
    }

    // ── Ana Dağılım ───────────────────────────────────────────────────────

    public static List<SpawnSlot> CalculateSpawnPositions(int totalPlayers,
                                                          List<GravitySource> sortedPlanets)
    {
        var result = new List<SpawnSlot>();
        if (sortedPlanets == null || sortedPlanets.Count == 0 || totalPlayers <= 0)
            return result;

        int numPlanets = Mathf.Min(sortedPlanets.Count, totalPlayers);

        int[] perPlanet = new int[numPlanets];
        int   baseCount = totalPlayers / numPlanets;
        int   remainder = totalPlayers % numPlanets;
        for (int i = 0; i < numPlanets; i++)
            perPlanet[i] = baseCount + (i < remainder ? 1 : 0);

        for (int pi = 0; pi < numPlanets; pi++)
        {
            var   planet = sortedPlanets[pi];
            int   count  = perPlanet[pi];
            float step   = 360f / count;

            for (int i = 0; i < count; i++)
            {
                float   angleDeg    = i * step;
                Vector2 dir         = DirectionFromAngle(angleDeg);
                Vector3 spawnPos    = FindSurfacePoint(planet, angleDeg);
                Vector2 planetCenter = planet.transform.position;
                Vector2 upDir2D     = ((Vector2)spawnPos - planetCenter).normalized;

                result.Add(new SpawnSlot
                {
                    position = spawnPos,
                    upDir    = new Vector3(upDir2D.x, upDir2D.y, 0f)
                });

#if UNITY_EDITOR
                Debug.Log($"[SpawnPositioning] Slot {result.Count - 1}: {planet.name} {angleDeg:F0}° → {spawnPos}");
#endif
            }
        }

        return result;
    }

    // ── Yüzey Noktası ────────────────────────────────────────────────────────
    //
    //  Gezegen merkezinden dışa doğru raycast.
    //  İlk solid (isTrigger=false) çarpma = gezegen yüzeyi.
    //  Trigger collider'lar (gravity field, detection zone) atlanır.

    public static Vector3 FindSurfacePoint(GravitySource planet, float angleDeg)
    {
        Vector2 dir    = DirectionFromAngle(angleDeg);
        Vector2 center = planet.transform.position;
        float   gR     = planet.gravityRadius;

        // Dışarıdan içe: gR*1.5 uzaktan merkeze doğru at
        Vector2 origin   = center + dir * (gR * 1.5f);
        Vector2 inward   = -dir;
        float   castDist = gR * 1.5f;

        var hits = Physics2D.RaycastAll(origin, inward, castDist);

        // Mesafeye göre sırala — dıştan içe, yani en küçük dist önce
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var h in hits)
        {
            if (h.collider == null) continue;
            if (IsCircleTrigger(h.collider)) continue;         // gravity/core → atla
            if (h.collider.attachedRigidbody != null) continue; // karakter → atla

            Vector2 surface = h.point;
            Vector2 up      = (surface - center).normalized;

#if UNITY_EDITOR
            Debug.Log($"[SpawnPositioning] {planet.name} {angleDeg:F0}°: " +
                      $"yüzey={surface} collider={h.collider.name} ({h.collider.GetType().Name})");
#endif

            return new Vector3(surface.x + up.x * SpawnLift,
                               surface.y + up.y * SpawnLift,
                               0f);
        }

        // Fallback
#if UNITY_EDITOR
        Debug.LogWarning($"[SpawnPositioning] {planet.name} {angleDeg:F0}°: yüzey bulunamadı " +
                         $"(center={center} gR={gR:F1}). Tüm çarpmalar:");
        foreach (var h in hits)
            Debug.LogWarning($"  → {h.collider?.name} ({h.collider?.GetType().Name}) " +
                             $"isTrigger={h.collider?.isTrigger} dist={h.distance:F2}");
#endif

        Vector2 fb = center + dir * (gR * 0.9f);
        return new Vector3(fb.x, fb.y, 0f);
    }

    // ── Yardımcılar ───────────────────────────────────────────────────────

    public static List<GravitySource> GetSortedPlanets()
    {
        var found = Object.FindObjectsByType<GravitySource>(FindObjectsSortMode.None);
        var list  = new List<GravitySource>(found);
        list.RemoveAll(s => s == null || !s.gameObject.activeInHierarchy);
        list.Sort((a, b) => b.gravityRadius.CompareTo(a.gravityRadius));
        return list;
    }

    // Yalnızca CircleCollider2D trigger'ları atla (gravity field + inner core).
    // PolygonCollider2D trigger (Planet_External yüzeyi) geçerli yüzey kabul edilir.
    static bool IsCircleTrigger(Collider2D col) =>
        col.isTrigger && col is CircleCollider2D;

    public static GravitySource GetMainPlanet()
    {
        var planets = GetSortedPlanets();
        return planets.Count > 0 ? planets[0] : null;
    }

    static Vector2 DirectionFromAngle(float angleDeg)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        return new Vector2(Mathf.Sin(rad), Mathf.Cos(rad));
    }
}
