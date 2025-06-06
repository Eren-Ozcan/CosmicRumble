// Assets/Scripts/PlanetClickExploder.cs
using UnityEngine;

/// <summary>
/// PlanetClickExploder:
/// - 4 tuşuna basıldığında “patlama modu” aktif hale gelir.
/// - Sonraki sol fare tıklamasında DestructiblePlanet.ExplodeWithForce() çağrılır.
/// - Patlama yarıçapı içindeki tüm Rigidbody2D’ler itilir ve IDamageable objelere hasar uygulanır.
/// - Ardından kırmızı bir LineRenderer ile sınır çizilir.
/// </summary>
public class PlanetClickExploder : MonoBehaviour
{
    [Header("Patlama Ayarları")]
    [Tooltip("Patlamanın etki yarıçapı (dünya birimi)")]
    public float explosionRadius = 1f;
    [Tooltip("Patlamanın kuvveti (Impulse mag)")]
    public float explosionForce = 10f;
    [Tooltip("Patlama anında uygulanacak hasar (yarıçapa göre falloff uygulanacak)")]
    public float maxDamage = 50f;

    private bool awaitingClick = false;
    private GameObject boundaryObj;

    private void Update()
    {
        // 1) "E" tuşuna basıldığında patlama modunu aktif et
        if (Input.GetKeyDown(KeyCode.E))
        {
            awaitingClick = true;
        }

        // 2) Patlama modu açıksa ve sol fare tıklaması yapılırsa patlamayı gerçekleştir
        if (awaitingClick && Input.GetMouseButtonDown(0))
        {
            Vector3 mouseWorld3D = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 clickPos = new Vector2(mouseWorld3D.x, mouseWorld3D.y);

            PerformExplosion(clickPos);
            awaitingClick = false;
        }
    }

    /// <summary>
    /// Belirtilen dünya koordinatında patlama uygular:
    /// 1) DestructiblePlanet.ExplodeWithForce()
    /// 2) Tüm IDamageable objelere hasar ver, Rigidbody2D’leri iter
    /// 3) Kırmızı boundary çemberini çizer
    /// </summary>
    private void PerformExplosion(Vector2 center)
    {
        // 1) Patlama bölgesindeki tüm Collider2D’leri al
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, explosionRadius);
        foreach (Collider2D hit in hits)
        {
            // 1.a) Eğer DestructiblePlanet ise patlat
            if (hit.TryGetComponent<DestructiblePlanet>(out var dp))
            {
                dp.ExplodeWithForce(center, explosionRadius, explosionForce);
            }

            // 1.b) Fiziksel itme (Impulse)
            Rigidbody2D rbHit = hit.attachedRigidbody;
            if (rbHit != null)
            {
                Vector2 dir = (rbHit.position - center).normalized;
                float dist = Vector2.Distance(rbHit.position, center);
                float falloff = 1f - Mathf.Clamp01(dist / explosionRadius);
                rbHit.AddForce(dir * explosionForce * falloff, ForceMode2D.Impulse);
            }

            // 1.c) IDamageable objelere hasar uygula
            if (hit.TryGetComponent<IDamageable>(out var dmgTarget))
            {
                float dist = Vector2.Distance(hit.transform.position, center);
                float falloff = 1f - Mathf.Clamp01(dist / explosionRadius);
                float damageAmount = maxDamage * falloff;
                dmgTarget.TakeDamage(damageAmount);
            }
        }

        // 2) Önceki boundary objesi varsa sil
        if (boundaryObj != null)
        {
            Destroy(boundaryObj);
        }

        // 3) Yeni kırmızı çember çiz
        DrawBoundaryCircle(center, explosionRadius);
    }

    private void DrawBoundaryCircle(Vector2 center, float radius)
    {
        int segments = 60;
        boundaryObj = new GameObject("ExplosionBoundary");
        LineRenderer lr = boundaryObj.AddComponent<LineRenderer>();

        lr.positionCount = segments + 1;
        lr.loop = true;
        lr.startWidth = 0.02f;
        lr.endWidth = 0.02f;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = Color.red;
        lr.endColor = Color.red;
        lr.sortingOrder = 1000;

        for (int i = 0; i <= segments; i++)
        {
            float angle = (float)i / segments * Mathf.PI * 2f;
            float x = center.x + Mathf.Cos(angle) * radius;
            float y = center.y + Mathf.Sin(angle) * radius;
            lr.SetPosition(i, new Vector3(x, y, 0f));
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Scene görünümde bu nesne seçiliyken patlama yarıçapını göstermek isterseniz
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
