using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class BlackHoleProjectile : MonoBehaviour
{
    [Header("Lifecycle")]
    public float activationDelay = 3f;   // 3 sn sonra kara deliğe dönüşsün
    public float lifeTime = 10f;         // emniyet

    [Header("Zone Runtime Settings (no prefab)")]
    public float zoneCoreRadius = 1.2f;      // x (çekirdek)
    public float zoneFieldRadius = 3.5f;     // x+y (çekim alanı) -> core’dan büyük OLMALI
    public float zonePullForce = 20f;
    public float zoneDamagePerSecond = 5f;
    public float zoneDuration = 5f;

    [Header("Instant Deform (grenade gibi, tek sefer)")]
    public bool deformOnDeploy = true;
    public float deformRadiusOverride = 0f;  // 0 => core radius kullan
    public float deformForce = 5f;           // DestructiblePlanet.ExplodeWithForce force
    public LayerMask deformLayers;           // (opsiyonel) sadece belli layer’lar

    [Header("VFX")]
    public GameObject gifPrefab; // BlackHoleGIF.prefab (görsel efekt)

    Rigidbody2D rb;
    Collider2D col;
    GameObject owner;
    bool _settled;

    public void Init(Vector2 initialVelocity, GameObject ownerObj, float ignoreTime)
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        owner = ownerObj;

        // velocity düzeltme
        rb.linearVelocity = initialVelocity;

        // owner ile çarpışmayı kısa süre ignore et
        if (owner != null && col != null)
        {
            foreach (var oc in owner.GetComponentsInChildren<Collider2D>())
                Physics2D.IgnoreCollision(col, oc, true);
            if (ignoreTime > 0f) Invoke(nameof(ReenableOwnerCollision), ignoreTime);
        }

        CameraController.OnProjectileSpawned(transform);
        TurnManager.NotifyProjectileLaunched();
        if (lifeTime > 0f) Destroy(gameObject, lifeTime);
        StartCoroutine(ArmAndDeploy());
    }

    void ReenableOwnerCollision()
    {
        if (owner == null || col == null) return;
        foreach (var oc in owner.GetComponentsInChildren<Collider2D>())
            Physics2D.IgnoreCollision(col, oc, false);
    }

    IEnumerator ArmAndDeploy()
    {
        // zaman dolunca olduğun yerde kara deliğe dönüş
        yield return new WaitForSeconds(activationDelay);

        // bulunduğu noktayı sabitle
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }
        if (col != null) col.enabled = false;

        // Zone’u kur
        var (center, coreR) = BuildZoneRuntime(transform.position);

        // grenade gibi TEK SEFERDE del
        if (deformOnDeploy)
        {
            float useR = (deformRadiusOverride > 0f) ? deformRadiusOverride : coreR;
            DoInstantDeform(center, useR, deformForce, deformLayers);
        }

        CameraController.OnProjectileDestroyed();
        SettleOnce();
        Destroy(gameObject);
    }

    void SettleOnce()
    {
        if (_settled) return;
        _settled = true;
        TurnManager.NotifyProjectileSettled();
    }

    void OnDestroy() => SettleOnce(); // ensure settle if lifeTime Destroy fires before ArmAndDeploy completes

    (Vector2 center, float coreRadius) BuildZoneRuntime(Vector3 atPos)
    {
        var zoneGO = new GameObject("BlackHoleZone_Runtime");
        zoneGO.transform.position = atPos;

        var zrb = zoneGO.AddComponent<Rigidbody2D>();
        zrb.bodyType = RigidbodyType2D.Kinematic;
        zrb.simulated = true;

        // Core
        var coreGO = new GameObject("Core");
        coreGO.transform.SetParent(zoneGO.transform, false);
        var coreCol = coreGO.AddComponent<CircleCollider2D>();
        coreCol.isTrigger = true;
        coreCol.radius = Mathf.Max(0.01f, zoneCoreRadius);

        // Field
        var fieldGO = new GameObject("Field");
        fieldGO.transform.SetParent(zoneGO.transform, false);
        var fieldCol = fieldGO.AddComponent<CircleCollider2D>();
        fieldCol.isTrigger = true;
        fieldCol.radius = Mathf.Max(coreCol.radius + 0.01f, zoneFieldRadius);

        // Zone davranışı (çekim + core DoT)
        var zone = zoneGO.AddComponent<BlackHoleZone>();
        zone.Configure(coreCol, fieldCol, zonePullForce, zoneDamagePerSecond, zoneDuration);

        // --- GIF spawn (zone'a parent edilir, ölçek prefab'taki gibi kalır) ---
        if (gifPrefab != null)
        {
            Instantiate(gifPrefab, atPos, Quaternion.identity, zoneGO.transform);
        }

        return (atPos, coreCol.radius);
    }

    // Grenade gibi: çevredeki DestructiblePlanet’lere tek seferlik delik aç
    void DoInstantDeform(Vector2 pos, float radius, float force, LayerMask layers)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(pos, radius, layers);
        if (hits == null || hits.Length == 0)
            hits = Physics2D.OverlapCircleAll(pos, radius);

        foreach (var h in hits)
        {
            if (h != null && h.TryGetComponent<DestructiblePlanet>(out var dp))
                dp.ExplodeWithForce(pos, radius, force);
        }
    }
}
