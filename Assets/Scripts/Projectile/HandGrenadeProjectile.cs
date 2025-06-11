using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class HandGrenadeProjectile : MonoBehaviour
{
    [Header("Zamanlayıcı ve Patlama")]
    public float delayBeforeExplosion = 6f;
    public GameObject explosionEffect;
    public float explosionRadius = 1f;
    public float explosionForce = 5f;
    public float maxDamage = 20f;

    [Header("Ekstra Kuvvet Ayarı")]
    [Tooltip("Çekim gücüne göre kuvvet çarpanı (örn: gezegenin gravityStrength değeri)")]
    public float gravityForceMultiplier = 1f;

    private Rigidbody2D rb;
    private Collider2D col;
    private GameObject owner;

    public void Init(Vector2 initialVelocity, GameObject ownerObj, float ignoreTime)
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        owner = ownerObj;

        rb.linearVelocity = initialVelocity;

        // Owner ile çarpışma engelle
        foreach (var oc in owner.GetComponentsInChildren<Collider2D>())
            Physics2D.IgnoreCollision(col, oc, true);

        Invoke(nameof(ReenableOwnerCollision), ignoreTime);
        Invoke(nameof(Explode), delayBeforeExplosion);
    }

    private void Explode()
    {
        Vector2 pos = transform.position;

        if (explosionEffect != null)
        {
            GameObject fx = Instantiate(explosionEffect, pos, Quaternion.identity);
            fx.transform.localScale = transform.localScale;
        }

        var hits = Physics2D.OverlapCircleAll(pos, explosionRadius);
        foreach (var hit in hits)
        {
            float distance = Vector2.Distance(hit.transform.position, pos);
            float falloff = 1f - Mathf.Clamp01(distance / explosionRadius);

            // ✅ Yakınlığa göre hasar (sahip dahil)
            if (hit.TryGetComponent<IDamageable>(out var dmg))
                dmg.TakeDamage(maxDamage * falloff);

            // ✅ Yakınlığa göre kuvvet + gezegen çekimi etkisi
            if (hit.attachedRigidbody != null)
            {
                Vector2 dir = (hit.attachedRigidbody.position - pos).normalized;
                float force = explosionForce * falloff * gravityForceMultiplier;
                hit.attachedRigidbody.AddForce(dir * force, ForceMode2D.Impulse);
            }

            if (hit.TryGetComponent<DestructiblePlanet>(out var dp))
                dp.ExplodeWithForce(pos, explosionRadius, explosionForce);
        }

        Debug.Log("💣 El bombası patladı!");
        Destroy(gameObject);
    }


    private void ReenableOwnerCollision()
    {
        if (owner == null || col == null) return;

        foreach (var oc in owner.GetComponentsInChildren<Collider2D>())
            Physics2D.IgnoreCollision(col, oc, false);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
