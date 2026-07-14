using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using CosmicRumble.Achievements;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class Projectile : MonoBehaviour
{
    [Header("Owner Ignoring")]
    public GameObject owner;
    public float ignoreOwnerTime = 1f;

    [Header("Movement & TTL")]
    public float timeToLive = 15f;
    public float gravityScale = 0f;
    public bool destroyOnInvisible = true;
    [Tooltip("Ekran dışına çıkan mermi bu kadar saniye içinde tekrar görünmezse yok edilir — " +
             "uzun yörüngeli atışların anında ölmesini engeller.")]
    public float offscreenGraceTime = 3f;

    [Header("Explosion & Damage")]
    public GameObject splashEffectPrefab;
    public float explosionRadius = 1f;
    public float explosionForce = 5f;
    public float maxDamage = 10f;

    [Tooltip("Scales the impulse applied to rigidbodies within the blast.")]
    public float gravityInfluence = 1f;

    [Header("Preview Color")]
    public Color minPowerColor = Color.green; // düşük güç
    public Color maxPowerColor = Color.red;   // yüksek güç

    [Header("Filtering")]
    [Tooltip("Only colliders on these layers will be affected by the explosion. (~0 = everything)")]
    public LayerMask affectLayers = ~0;

    // --- Internals ---
    Rigidbody2D rb;
    Collider2D col;
    float spawnTime;
    bool ownerReenabled;
    bool _settled;

    // We will re-enable these on timeout/destroy
    readonly List<Collider2D> ignoredOwnerColliders = new List<Collider2D>(8);

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        rb.gravityScale = gravityScale;
        spawnTime = Time.time;
    }

    void Start()
    {
        // Replike (non-server) kopyalarda Init() çağrılmaz — owner bilinmediği için roket namlu
        // ucunda atıcının collider'ına çarpıp yerelde anında "patlayabilir". Bu makinelerde
        // simülasyon saf görsel olduğundan kısa süre tüm karakterler yok sayılır
        // (KineticProjectile.Start'taki korumanın aynısı; server/offline etkilenmez).
        var netObj = GetComponent<NetworkObject>();
        var nm = NetworkManager.Singleton;
        if (netObj != null && netObj.IsSpawned && (nm == null || !nm.IsServer))
        {
            StartCoroutine(TemporaryIgnoreAllCharacters());
            // Uçuş loop'u server'da Init() içinde başlıyor — replike kopyada Init çalışmadığı
            // için client roketi sessiz uçuyordu.
            AudioManager.Instance?.PlayLoopingSfxOnObject(gameObject, "projectile_flight_rocket");
        }
    }

    private IEnumerator TemporaryIgnoreAllCharacters()
    {
        var bodies = FindObjectsByType<GravityBody>(FindObjectsSortMode.None);
        var allCols = new List<Collider2D>();
        foreach (var body in bodies)
            allCols.AddRange(body.GetComponentsInChildren<Collider2D>());

        foreach (var c in allCols)
            if (c != null) Physics2D.IgnoreCollision(col, c, true);

        yield return new WaitForSeconds(0.3f);

        if (this == null) yield break;
        if (col == null || !col.enabled) yield break; // mermi bu arada "emekliye ayrılmış" olabilir

        foreach (var c in allCols)
        {
            if (c == null || !c.enabled) continue;
            if (owner != null && c.transform.IsChildOf(owner.transform)) continue; // Init yönetiyor
            Physics2D.IgnoreCollision(col, c, false);
        }
    }

    public void Init(Vector2 initialVelocity, GameObject ownerObj, float ignoreTime = 1f)
    {
        NetworkPhysicsGuard.EnsureDynamicWhenNotSpawned(rb);
        owner = ownerObj;
        ignoreOwnerTime = ignoreTime;
        spawnTime = Time.time;

        // Ignore collisions with all owner's colliders
        ignoredOwnerColliders.Clear();
        if (owner != null)
        {
            var ownerCols = owner.GetComponentsInChildren<Collider2D>(true);
            foreach (var oc in ownerCols)
            {
                if (oc == null) continue;
                Physics2D.IgnoreCollision(col, oc, true);
                ignoredOwnerColliders.Add(oc);
            }
        }

        rb.linearVelocity = initialVelocity;
        CameraController.OnProjectileSpawned(transform);
        TurnManager.NotifyProjectileLaunched();
        AudioManager.Instance?.PlayLoopingSfxOnObject(gameObject, "projectile_flight_rocket");
        if (ignoreOwnerTime > 0f)
            Invoke(nameof(ReenableOwnerCollision), ignoreOwnerTime);
    }

    void Update()
    {
        // TTL
        if (timeToLive > 0f && Time.time - spawnTime >= timeToLive)
        {
            CameraController.OnProjectileDestroyed();
            SettleOnce();
            NetworkPhysicsGuard.DespawnOrDestroy(gameObject, this);
            return;
        }
    }

    void FixedUpdate()
    {
        // Align to velocity
        Vector2 vel = rb.linearVelocity;
        if (vel.sqrMagnitude > 0.0001f)
            transform.right = vel.normalized;
    }

    void OnBecameInvisible()
    {
        // Anında yok etme yerine tolerans süresi — bkz. ProjectileBase.OnBecameInvisible.
        if (destroyOnInvisible)
            Invoke(nameof(OffscreenExpired), offscreenGraceTime);
    }

    void OnBecameVisible()
    {
        CancelInvoke(nameof(OffscreenExpired));
    }

    void OffscreenExpired()
    {
        CameraController.OnProjectileDestroyed();
        SettleOnce();
        NetworkPhysicsGuard.DespawnOrDestroy(gameObject, this);
    }

    void SettleOnce(bool isHit = false)
    {
        if (_settled) return;
        _settled = true;
        AchievementEvents.FireShotFired(isHit);
        TurnManager.NotifyProjectileSettled();
    }

    void OnDestroy()
    {
        SettleOnce(); // ensure settle even if destroyed externally
        if (!ownerReenabled) ReenableOwnerCollision();
        CancelInvoke();
    }

    private void ReenableOwnerCollision()
    {
        if (ownerReenabled) return;
        if (ignoredOwnerColliders.Count > 0 && col != null)
        {
            foreach (var oc in ignoredOwnerColliders)
            {
                if (oc == null) continue;
                Physics2D.IgnoreCollision(col, oc, false);
            }
        }
        ownerReenabled = true;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // Still within initial ignore window?
        if (collision.gameObject == owner && Time.time - spawnTime < ignoreOwnerTime)
            return;

        // Impact position (more accurate than transform.position)
        Vector2 hitPos = collision.GetContact(0).point;

        AudioManager.Instance?.PlaySfx("explosion_large");

        if (splashEffectPrefab != null)
            Instantiate(splashEffectPrefab, hitPos, Quaternion.identity);

        // Planet/destructible first
        if (collision.collider.TryGetComponent<DestructiblePlanet>(out var dp))
            dp.ExplodeWithForce(hitPos, explosionRadius, explosionForce);

        // Radial query (filtered)
        var hits = Physics2D.OverlapCircleAll(hitPos, explosionRadius, affectLayers);
        bool hitAny = false;
        foreach (var hit in hits)
        {
            var go = hit.gameObject;

            if (go == owner) continue; // never affect owner

            // Damage falloff by distance
            float distance = Vector2.Distance(hit.transform.position, hitPos);
            float falloff = 1f - Mathf.Clamp01(distance / Mathf.Max(0.0001f, explosionRadius));

            // Deal damage (if any)
            if (hit.TryGetComponent<IDamageable>(out var dmg))
            {
                float dmgAmount = maxDamage * falloff;
                dmg.TakeDamage(dmgAmount);
                hitAny = true;
                CombatEventReporter.ReportHit(dmg, dmgAmount, hitPos);
            }

            // Apply impulse (physics)
            var targetRb = hit.attachedRigidbody;
            if (targetRb != null)
            {
                Vector2 dir = ((Vector2)targetRb.position - hitPos).normalized;
                float finalForce = explosionForce * falloff * gravityInfluence;
                targetRb.AddForce(dir * finalForce, ForceMode2D.Impulse);
            }
        }

        CameraController.OnProjectileDestroyed();
        SettleOnce(hitAny);
        NetworkPhysicsGuard.DespawnOrDestroy(gameObject, this);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (explosionRadius > 0f)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
        }
    }
#endif
}
