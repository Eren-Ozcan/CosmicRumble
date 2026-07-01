using UnityEngine;
using System.Collections.Generic;

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

    public void Init(Vector2 initialVelocity, GameObject ownerObj, float ignoreTime = 1f)
    {
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
            Destroy(gameObject);
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
        if (destroyOnInvisible)
        {
            CameraController.OnProjectileDestroyed();
            SettleOnce();
            Destroy(gameObject);
        }
    }

    void SettleOnce()
    {
        if (_settled) return;
        _settled = true;
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

        if (splashEffectPrefab != null)
            Instantiate(splashEffectPrefab, hitPos, Quaternion.identity);

        // Planet/destructible first
        if (collision.collider.TryGetComponent<DestructiblePlanet>(out var dp))
            dp.ExplodeWithForce(hitPos, explosionRadius, explosionForce);

        // Radial query (filtered)
        var hits = Physics2D.OverlapCircleAll(hitPos, explosionRadius, affectLayers);
        foreach (var hit in hits)
        {
            var go = hit.gameObject;

            if (go == owner) continue; // never affect owner

            // Damage falloff by distance
            float distance = Vector2.Distance(hit.transform.position, hitPos);
            float falloff = 1f - Mathf.Clamp01(distance / Mathf.Max(0.0001f, explosionRadius));

            // Deal damage (if any)
            if (hit.TryGetComponent<IDamageable>(out var dmg))
                dmg.TakeDamage(maxDamage * falloff);

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
        SettleOnce();
        Destroy(gameObject);
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
