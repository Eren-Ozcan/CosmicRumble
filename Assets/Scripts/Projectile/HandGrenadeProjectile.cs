using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(OwnerCollisionIgnore))]
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
    private GameObject owner;
    private OwnerCollisionIgnore ownerIgnore;

    public void Init(Vector2 initialVelocity, GameObject ownerObj, float ignoreTime)
    {
        rb = GetComponent<Rigidbody2D>();
        owner = ownerObj;
        ownerIgnore = GetComponent<OwnerCollisionIgnore>();

        rb.linearVelocity = initialVelocity;

        ownerIgnore.Ignore(owner, ignoreTime); // FIX: cache & coroutine based ignore

        StartCoroutine(ExplodeRoutine());
    }

    private IEnumerator ExplodeRoutine()
    {
        yield return new WaitForSecondsRealtime(delayBeforeExplosion);
        Explode();
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

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
