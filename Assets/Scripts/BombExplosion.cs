// Assets/Scripts/Planet/BombExplosion.cs
using UnityEngine;

/// <summary>
/// BombExplosion:
/// - Spawn olduktan 0.1s sonra çarpışmaya (ve patlamaya) izin verir.
/// - Çarptığı DestructiblePlanet objesini ExplodeWithForce() ile patlatır.
/// - Patlama yarıçapı içindeki tüm Rigidbody2D’ler itilir ve IDamageable objelere hasar uygulanır.
/// - Partikül efekti (varsa) instantiate edilir, bomba yok edilir.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class BombExplosion : MonoBehaviour
{
    [Header("Patlama Ayarları")]
    [Tooltip("Patlamanın etki yarıçapı (dünya birimi)")]
    public float explosionRadius = 1f;
    [Tooltip("Patlama kuvveti (Impulse büyüklüğü)")]
    public float explosionForce = 10f;
    [Tooltip("Patlama anında uygulanacak hasar (yarıçapa göre falloff uygulanacak)")]
    public float maxDamage = 50f;
    [Tooltip("Opsiyonel: Patlama efekti prefab’ı")]
    public GameObject explosionEffectPrefab;

    private bool launched = false;

    private void Start()
    {
        // Oluştuktan hemen sonra kendi collider’ına çarpmasını engellemek için kısa gecikme
        Invoke(nameof(EnableLaunch), 0.1f);
    }

    private void EnableLaunch()
    {
        launched = true;
    }

    private void OnCollisionEnter2D(Collision2D col)
    {
        if (!launched) return;

        Vector2 explosionPos = transform.position;

        // Çarptığı obje DestructiblePlanet ise patlat
        if (col.collider.TryGetComponent<DestructiblePlanet>(out var dp))
        {
            dp.ExplodeWithForce(explosionPos, explosionRadius, explosionForce);
        }

        // Patlama yarıçapındaki tüm objelere hem fiziksel itme hem hasar uygula
        Collider2D[] hits = Physics2D.OverlapCircleAll(explosionPos, explosionRadius);
        foreach (var hit in hits)
        {
            // Fiziksel itme (Impulse)
            Rigidbody2D rbHit = hit.attachedRigidbody;
            if (rbHit != null)
            {
                Vector2 dir = (rbHit.position - explosionPos).normalized;
                float dist = Vector2.Distance(rbHit.position, explosionPos);
                float falloff = 1f - Mathf.Clamp01(dist / explosionRadius);
                rbHit.AddForce(dir * explosionForce * falloff, ForceMode2D.Impulse);
            }

            // IDamageable objelere hasar uygula
            if (hit.TryGetComponent<IDamageable>(out var dmgTarget))
            {
                float dist = Vector2.Distance(hit.transform.position, explosionPos);
                float falloff = 1f - Mathf.Clamp01(dist / explosionRadius);
                float damageAmount = maxDamage * falloff;
                dmgTarget.TakeDamage(damageAmount);
            }
        }

        // Patlama efekti instantiate (varsa)
        if (explosionEffectPrefab != null)
        {
            Instantiate(explosionEffectPrefab, explosionPos, Quaternion.identity);
        }

        // Bombayı yok et
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        // Editörde patlama yarıçapını görebilmek için
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
