using UnityEngine;

 [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(OwnerCollisionIgnore))]
public class Projectile : MonoBehaviour
{
    [Header("Owner Ignoring")]
    public GameObject owner;
    public float ignoreOwnerTime = 1f;

    [Header("Movement & TTL")]
    public float timeToLive = 15f;
    public float gravityScale = 0f;

    [Header("Explosion & Damage")]
    public GameObject splashEffectPrefab;
    public float explosionRadius = 1f;
    public float explosionForce = 5f;
    public float maxDamage = 10f;

    [Tooltip("Patlamada etkilenecek çekim gücü (gezegenin gravityStrength gibi değerini temsil eder)")]
    public float gravityInfluence = 1f;

    Rigidbody2D rb;
    float spawnTime;
    OwnerCollisionIgnore ownerIgnore;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = gravityScale;
        spawnTime = Time.time;
        ownerIgnore = GetComponent<OwnerCollisionIgnore>();
    }

    public void Init(Vector2 initialVelocity, GameObject ownerObj, float ignoreTime = 1f)
    {
        owner = ownerObj;
        ignoreOwnerTime = ignoreTime;
        spawnTime = Time.time;
        ownerIgnore.Ignore(owner, ignoreOwnerTime); // FIX: use cached colliders & realtime coroutine

        rb.linearVelocity = initialVelocity;
    }

    void Update()
    {
        if (Time.time - spawnTime >= timeToLive)
        {
            Destroy(gameObject);
            return;
        }
    }

    void FixedUpdate()
    {
        Vector2 vel = rb.linearVelocity;
        if (vel.sqrMagnitude > 0.01f)
            transform.right = vel.normalized;
    }

    void OnBecameInvisible()
    {
        Destroy(gameObject);
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

        var hits = Physics2D.OverlapCircleAll(hitPos, explosionRadius);
        foreach (var hit in hits)
        {
            if (hit.gameObject == owner) continue;

            float distance = Vector2.Distance(hit.transform.position, hitPos);
            float falloff = 1f - Mathf.Clamp01(distance / explosionRadius);

            // Hasar sadece yakınlığa göre
            if (hit.TryGetComponent<IDamageable>(out var dmg))
                dmg.TakeDamage(maxDamage * falloff);

            // Kuvvet: hem yakınlığa hem gravity çarpanına göre
            if (hit.attachedRigidbody != null)
            {
                Vector2 dir = (hit.attachedRigidbody.position - hitPos).normalized;
                float finalForce = explosionForce * falloff * gravityInfluence;
                hit.attachedRigidbody.AddForce(dir * finalForce, ForceMode2D.Impulse);
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
