// Assets/Scripts/Projectile/Projectile.cs
using UnityEngine;

/// <summary>
/// Projectile:
/// - Spawn olduğunda ileri yönde sabit hızla hareket eder.
/// - Çarptığında patlama efekti yaratır, DestructiblePlanet ve IDamageable hedeflere etki eder.
/// - Sahneden çıkınca veya TTL dolunca kendini yok eder.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class Projectile : MonoBehaviour
{
    [Header("Hareket & Yaşam Süresi")]
    [Tooltip("Mermi hızı (birim/saniye)")]
    public float speed = 10f;
    [Tooltip("Merminin sahneden çıktıktan sonra yok olma süresi (saniye)")]
    public float timeToLive = 5f;
    [Tooltip("Merminin Rigidbody2D gravityScale değeri (genellikle 0)")]
    public float gravityScale = 0f;

    [Header("Patlama & Hasar")]
    [Tooltip("Mermi çarptığında instantiate edilecek efekt prefab’ı")]
    public GameObject splashEffectPrefab;
    [Tooltip("Patlama yarıçapı (dünya birimi); 0 ise tek vuruş")]
    public float explosionRadius = 0f;
    [Tooltip("Patlama kuvveti (Impulse mag)")]
    public float explosionForce = 5f;
    [Tooltip("Maksimum hasar (mesafeye göre falloff yapabiliriz)")]
    public float maxDamage = 10f;

    private Rigidbody2D rb;
    private float spawnTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = gravityScale;
    }

    private void Start()
    {
        // Mermiyi ileri yöne doğru hareket ettir
        rb.linearVelocity = transform.right * speed;
        spawnTime = Time.time;
    }

    private void Update()
    {
        // TTL bittiğinde yok et
        if (Time.time - spawnTime >= timeToLive)
        {
            Destroy(gameObject);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Vector2 hitPos = transform.position;

        // 1) Splash/Patlama efekti
        if (splashEffectPrefab != null)
        {
            Instantiate(splashEffectPrefab, hitPos, Quaternion.identity);
        }

        // 2) DestructiblePlanet var mı?
        if (collision.collider.TryGetComponent<DestructiblePlanet>(out var dp))
        {
            dp.ExplodeWithForce(hitPos, explosionRadius, explosionForce);
        }

        // 3) Hasar uygulama (IDamageable interface’li objelere)
        if (explosionRadius <= 0f)
        {
            // Tek vuruş hasarı
            if (collision.collider.TryGetComponent<IDamageable>(out var dmgTarget))
            {
                dmgTarget.TakeDamage(maxDamage);
            }
        }
        else
        {
            // Alan hasarı: OverlapCircleAll içinde kalanlara
            Collider2D[] hits = Physics2D.OverlapCircleAll(hitPos, explosionRadius);
            foreach (var hit in hits)
            {
                if (hit.TryGetComponent<IDamageable>(out var dmg))
                {
                    float dist = Vector2.Distance(hit.transform.position, hitPos);
                    float falloff = 1f - Mathf.Clamp01(dist / explosionRadius);
                    float damage = maxDamage * falloff;
                    dmg.TakeDamage(damage);
                }

                if (hit.attachedRigidbody != null)
                {
                    Vector2 dir = (hit.attachedRigidbody.position - hitPos).normalized;
                    float dist = Vector2.Distance(hit.attachedRigidbody.position, hitPos);
                    float falloff = 1f - Mathf.Clamp01(dist / explosionRadius);
                    hit.attachedRigidbody.AddForce(dir * explosionForce * falloff, ForceMode2D.Impulse);
                }
            }
        }

        // 4) Mermiyi yok et
        Destroy(gameObject);
    }

    private void OnBecameInvisible()
    {
        // Kamera dışına tamamen çıktığında mermiyi yok et
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        // Inspector’da patlama yarıçapını göstermek için
        if (explosionRadius > 0f)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
        }
    }
}
