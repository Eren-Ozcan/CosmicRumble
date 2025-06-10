using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class Projectile : MonoBehaviour
{
    [Header("Owner Ignoring")]
    public GameObject owner;
    public float ignoreOwnerTime = 1f;

    [Header("Movement & TTL")]
    [Tooltip("Merminin sahnede kalabileceği maksimum süre (saniye)")]
    public float timeToLive = 15f;      // ← 15 sn  
    public float gravityScale = 0f;

    [Header("Explosion & Damage")]
    public GameObject splashEffectPrefab;
    public float explosionRadius = 1f;
    public float explosionForce = 5f;
    public float maxDamage = 10f;

    Rigidbody2D rb;
    Collider2D col;
    float spawnTime;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        rb.gravityScale = gravityScale;

        // TTL sayacını başlat
        spawnTime = Time.time;
    }

    /// <summary>
    /// Atış anında çağrılır, hem hızı atar hem de TTL'yi sıfırlar.
    /// </summary>
    public void Init(Vector2 initialVelocity, GameObject ownerObj, float ignoreTime = 1f)
    {
        owner = ownerObj;
        ignoreOwnerTime = ignoreTime;
        // TTL'yi yeniden başlat
        spawnTime = Time.time;

        // Owner collision ignore
        foreach (var oc in owner.GetComponentsInChildren<Collider2D>())
            Physics2D.IgnoreCollision(col, oc, true);

        // İlk hızı ata
        rb.linearVelocity = initialVelocity;

        Invoke(nameof(ReenableOwnerCollision), ignoreOwnerTime);
    }

    void Update()
    {
        // TTL kontrolü: 15 saniye dolduysa yok et
        if (Time.time - spawnTime >= timeToLive)
        {
            Destroy(gameObject);
            return;
        }
    }

    void FixedUpdate()
    {
        // Hızı takip edip sprite'ı hizalar
        Vector2 vel = rb.linearVelocity;
        if (vel.sqrMagnitude > 0.01f)
            transform.right = vel.normalized;
    }

    // -----------------------------------
    // → EKLENDİ: Ekrandan çıktığında çağrılır
    void OnBecameInvisible()
    {
        Destroy(gameObject);
    }
    // -----------------------------------

    private void ReenableOwnerCollision()
    {
        if (owner == null) return;
        foreach (var oc in owner.GetComponentsInChildren<Collider2D>())
            Physics2D.IgnoreCollision(col, oc, false);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject == owner && Time.time - spawnTime < ignoreOwnerTime)
            return;

        Vector2 hitPos = transform.position;
        if (splashEffectPrefab != null)
            Instantiate(splashEffectPrefab, hitPos, Quaternion.identity);

        if (collision.collider.TryGetComponent<DestructiblePlanet>(out var dp))
            dp.ExplodeWithForce(hitPos, explosionRadius, explosionForce);

        if (explosionRadius <= 0f)
        {
            if (collision.collider.TryGetComponent<IDamageable>(out var dmg)
                && collision.gameObject != owner)
                dmg.TakeDamage(maxDamage);
        }
        else
        {
            var hits = Physics2D.OverlapCircleAll(hitPos, explosionRadius);
            foreach (var hit in hits)
            {
                if (hit.gameObject == owner) continue;
                if (hit.TryGetComponent<IDamageable>(out var dmg))
                {
                    float d = Vector2.Distance(hit.transform.position, hitPos);
                    float falloff = 1f - Mathf.Clamp01(d / explosionRadius);
                    dmg.TakeDamage(maxDamage * falloff);
                }
                if (hit.attachedRigidbody != null)
                {
                    Vector2 dir = (hit.attachedRigidbody.position - hitPos).normalized;
                    float d = Vector2.Distance(hit.attachedRigidbody.position, hitPos);
                    float falloff = 1f - Mathf.Clamp01(d / explosionRadius);
                    hit.attachedRigidbody.AddForce(dir * explosionForce * falloff, ForceMode2D.Impulse);
                }
            }
        }

        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        if (explosionRadius > 0f)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
        }
    }
}
