using UnityEngine;

/// <summary>
/// Karaktere ekle → dolaş → logları gör.
///
/// Her saniye otomatik log atar.
/// L tuşuna basınca anlık detaylı snapshot alır.
/// Bu scripti sadece debug için kullan, build'e alma.
/// </summary>
public class SpawnDebugger : MonoBehaviour
{
    [Header("Ayarlar")]
    [Tooltip("Kaç saniyede bir otomatik log atılsın (0 = sadece L ile)")]
    public float autoLogInterval = 1f;

    [Tooltip("Anlık snapshot için tuş")]
    public KeyCode snapshotKey = KeyCode.L;

    float _timer;

    void Update()
    {
        if (autoLogInterval > 0f)
        {
            _timer += Time.deltaTime;
            if (_timer >= autoLogInterval)
            {
                _timer = 0f;
                LogStatus("AUTO");
            }
        }

        if (Input.GetKeyDown(snapshotKey))
            LogDetailed();
    }

    // ── Kısa otomatik log ────────────────────────────────────────────────────

    void LogStatus(string tag)
    {
        var planet = GetClosestPlanet(out float dist, out float angleDeg, out Vector2 upDir);

        string planetName = planet != null ? planet.name : "YOK";
        #if UNITY_EDITOR
        Debug.Log($"[SpawnDebug|{tag}] pos={transform.position:F2}  " +
        #endif
                  $"planet={planetName}  açı={angleDeg:F1}°  " +
                  $"yüzey mesafe={dist:F2}  up={upDir:F2}");
    }

    // ── Detaylı snapshot (L tuşu) ────────────────────────────────────────────

    void LogDetailed()
    {
        #if UNITY_EDITOR
        Debug.Log("══════════════ SPAWN SNAPSHOT ══════════════");
        #endif

        var planet = GetClosestPlanet(out float distFromCenter, out float angleDeg, out Vector2 upDir);

        if (planet == null)
        {
            #if UNITY_EDITOR
            Debug.LogWarning("[SpawnDebug] Yakında hiç GravitySource yok!");
            #endif
            return;
        }

        // Gezegen yüzeyine olan mesafe (merkeze mesafe - gezegen yarıçapı değil, raycast ile)
        Vector3 surfacePoint = SpawnPositioning.FindSurfacePoint(planet, angleDeg);
        float distFromSurface = Vector3.Distance(transform.position, surfacePoint);
        float spawnLift       = SpawnPositioning.SpawnLift;

        #if UNITY_EDITOR
        Debug.Log($"  Karakter pozisyonu : {transform.position:F3}");
        #endif
        #if UNITY_EDITOR
        Debug.Log($"  En yakın gezegen   : {planet.name}  (merkez={planet.transform.position:F2})");
        #endif
        #if UNITY_EDITOR
        Debug.Log($"  Gezegene açı       : {angleDeg:F1}°  (SpawnPositioning DirectionFromAngle formatı)");
        #endif
        #if UNITY_EDITOR
        Debug.Log($"  Merkeze mesafe     : {distFromCenter:F2}");
        #endif
        #if UNITY_EDITOR
        Debug.Log($"  Yüzey noktası      : {surfacePoint:F3}");
        #endif
        #if UNITY_EDITOR
        Debug.Log($"  Yüzeyden mesafe    : {distFromSurface:F2}  (SpawnLift={spawnLift:F2})");
        #endif
        #if UNITY_EDITOR
        Debug.Log($"  transform.up       : {transform.up:F3}");
        #endif
        #if UNITY_EDITOR
        Debug.Log($"  Beklenen upDir     : {upDir:F3}");
        #endif

        // Açı farkı
        float upAngleDiff = Vector3.Angle(transform.up, upDir);
        #if UNITY_EDITOR
        Debug.Log($"  Yön farkı         : {upAngleDiff:F1}°  (0 = mükemmel hizalı)");
        #endif

        // SpawnLift önerisi
        float idealLift = distFromSurface;
        #if UNITY_EDITOR
        Debug.Log($"  SpawnLift önerisi  : SpawnPositioning.SpawnLift = {idealLift:F2}f  (şu an: {spawnLift:F2}f)");
        #endif

        // Tüm gezegenler
        var all = SpawnPositioning.GetSortedPlanets();
        #if UNITY_EDITOR
        Debug.Log($"  Sahnedeki geçerli gezegenler ({all.Count}):");
        #endif
        for (int i = 0; i < all.Count; i++)
            #if UNITY_EDITOR
            Debug.Log($"    [{i}] {all[i].name}  gravityRadius={all[i].gravityRadius:F1}");
            #endif

        #if UNITY_EDITOR
        Debug.Log("═══════════════════════════════════════════");
        #endif
    }

    // ── Yardımcılar ─────────────────────────────────────────────────────────

    GravitySource GetClosestPlanet(out float distFromCenter, out float angleDeg, out Vector2 upDir)
    {
        GravitySource closest = null;
        float minDist = float.MaxValue;

        foreach (var gs in GravitySource.AllSources)
        {
            if (gs == null) continue;
            float d = Vector3.Distance(transform.position, gs.transform.position);
            if (d < minDist) { minDist = d; closest = gs; }
        }

        distFromCenter = minDist;

        if (closest == null)
        {
            angleDeg = 0f;
            upDir    = Vector2.up;
            return null;
        }

        // Karakterden gezegen merkezine vektör → açı (SpawnPositioning.DirectionFromAngle ile uyumlu)
        Vector2 toChar  = (Vector2)(transform.position - closest.transform.position);
        upDir           = toChar.normalized;

        // atan2 yerine SpawnPositioning'in Sin/Cos formatıyla uyumlu açı:
        // DirectionFromAngle: x=Sin(angle), y=Cos(angle)  → angle = atan2(x, y)
        angleDeg = Mathf.Atan2(toChar.x, toChar.y) * Mathf.Rad2Deg;
        if (angleDeg < 0f) angleDeg += 360f;

        return closest;
    }
}
