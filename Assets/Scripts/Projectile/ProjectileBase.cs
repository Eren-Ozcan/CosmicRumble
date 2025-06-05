// Assets/Scripts/Projectile/ProjectileBase.cs
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class ProjectileBase : MonoBehaviour
{
    [Header("Fizik Ayarları")]
    public float mass = 1f;
    public float gravityScale = 1f;

    [Header("Patlama/Çarpma Ayarları")]
    public float explosionRadius = 0f;
    public float explosionForce = 0f;
    public GameObject impactEffectPrefab;

    [Header("Genel Ayarlar")]
    public bool destroyWhenOffScreen = true;
    public float timeToLive = 10f;

    protected Rigidbody2D rb;
    protected Collider2D col2d;
    protected float spawnTime;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col2d = GetComponent<Collider2D>();

        rb.mass = mass;
        rb.gravityScale = 0f; // Manuel gravity kullanacak
        col2d.isTrigger = false;
    }

    protected virtual void Start()
    {
        spawnTime = Time.time;
    }

    protected virtual void FixedUpdate()
    {
        Vector2 gravity = Physics2D.gravity * gravityScale * mass;
        rb.AddForce(gravity * Time.fixedDeltaTime, ForceMode2D.Force);
    }

    protected virtual void OnCollisionEnter2D(Collision2D collision)
    {
        Vector2 impactPoint = collision.GetContact(0).point;

        if (impactEffectPrefab != null)
            Instantiate(impactEffectPrefab, impactPoint, Quaternion.identity);

        if (explosionRadius > 0f && explosionForce > 0f)
            Explode(impactPoint);
        else
            ApplyDirectHit(collision);

        Destroy(gameObject);
    }

    protected virtual void Explode(Vector2 center)
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(center, explosionRadius);
        foreach (var hit in colliders)
        {
            Rigidbody2D rbHit = hit.attachedRigidbody;
            if (rbHit != null)
            {
                Vector2 dir = (rbHit.position - center).normalized;
                float dist = Vector2.Distance(rbHit.position, center);
                float forceMag = Mathf.Lerp(explosionForce, 0f, dist / explosionRadius);
                rbHit.AddForce(dir * forceMag, ForceMode2D.Impulse);
            }

            var dp = hit.GetComponent<DestructiblePlanet>();
            if (dp != null)
                dp.ExplodeWithForce(center, explosionRadius, explosionForce);

            var dmg = hit.GetComponent<IDamageable>();
            if (dmg != null)
            {
                float amount = CalculateDamageByDistance(center, hit.transform.position);
                dmg.TakeDamage(amount);
            }
        }
    }

    protected virtual void ApplyDirectHit(Collision2D collision)
    {
        Rigidbody2D rbHit = collision.rigidbody;
        if (rbHit != null)
        {
            Vector2 dir = (rbHit.position - rb.position).normalized;
            rbHit.AddForce(dir * explosionForce, ForceMode2D.Impulse);
        }

        var dmg = collision.gameObject.GetComponent<IDamageable>();
        if (dmg != null)
            dmg.TakeDamage(explosionForce);
    }

    protected virtual float CalculateDamageByDistance(Vector2 center, Vector2 targetPos)
    {
        float dist = Vector2.Distance(center, targetPos);
        float ratio = Mathf.Clamp01(1f - (dist / explosionRadius));
        return ratio * explosionForce;
    }

    protected virtual void OnBecameInvisible()
    {
        if (destroyWhenOffScreen)
            Destroy(gameObject);
    }

    protected virtual void Update()
    {
        if (Time.time - spawnTime >= timeToLive)
            Destroy(gameObject);
    }

    protected virtual void OnDrawGizmosSelected()
    {
        if (explosionRadius > 0f)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
        }
    }
}
