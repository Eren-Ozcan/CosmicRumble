// Assets/Scripts/Projectile/ProjectileBase.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CosmicRumble.Achievements;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class ProjectileBase : MonoBehaviour
{
    [Header("Fizik Ayarları")]
    public float mass = 1f;

    [Header("Patlama/Çarpma Ayarları")]
    public float explosionRadius = 0f;
    public float explosionForce = 0f;
    public GameObject impactEffectPrefab;

    [Header("Genel Ayarlar")]
    public bool destroyWhenOffScreen = true;
    [Tooltip("Ekran dışına çıkan mermi bu kadar saniye içinde tekrar görünmezse yok edilir — " +
             "uzun yörüngeli (gezegen arkasından dolanan) atışların anında ölmesini engeller.")]
    public float offscreenGraceTime = 3f;
    public float timeToLive = 10f;

    protected Rigidbody2D rb;
    protected Collider2D col2d;
    protected float spawnTime;

    // Guard: ensures TurnManager.NotifyProjectileSettled is called exactly once per projectile.
    private bool _settled = false;

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
        // Start, dinamik NGO spawn'larından sonra çalışır: offline'da IsSpawned=false kalır ve
        // NetworkRigidbody2D'nin Awake'te zorladığı Kinematic geri alınır (yoksa yerçekimi AddForce'u no-op olur).
        NetworkPhysicsGuard.EnsureDynamicWhenNotSpawned(rb);
        spawnTime = Time.time;
        StartCoroutine(TemporaryIgnoreCharacters());
        CameraController.OnProjectileSpawned(transform);
        TurnManager.NotifyProjectileLaunched();
    }

    // Ignores all character colliders for 0.3 s to prevent spawn-push,
    // then re-enables them so damage/knockback hits work normally.
    private IEnumerator TemporaryIgnoreCharacters()
    {
        var bodies = FindObjectsByType<GravityBody>(FindObjectsSortMode.None);
        var cols = new List<Collider2D>();
        foreach (var body in bodies)
            cols.AddRange(body.GetComponentsInChildren<Collider2D>());

        foreach (var c in cols)
            if (c != null) Physics2D.IgnoreCollision(col2d, c, true);

        yield return new WaitForSeconds(0.3f);

        if (this == null) yield break;   // projectile already destroyed

        foreach (var c in cols)
            if (c != null) Physics2D.IgnoreCollision(col2d, c, false);
    }

    protected virtual void FixedUpdate()
    {
        // Gravity GravitySource.FixedUpdate üzerinden uygulanır — Physics2D.gravity kapalı
    }

    /// <summary>Calls NotifyProjectileSettled exactly once, regardless of how many destroy paths fire.</summary>
    protected void SettleOnce()
    {
        if (_settled) return;
        _settled = true;
        TurnManager.NotifyProjectileSettled();
    }

    protected virtual void OnCollisionEnter2D(Collision2D collision)
    {
        Vector2 impactPoint = collision.GetContact(0).point;

        if (impactEffectPrefab != null)
            Instantiate(impactEffectPrefab, impactPoint, Quaternion.identity);

        if (explosionRadius > 0f && explosionForce > 0f)
            Explode(impactPoint);
        else
            ApplyDirectHit(collision, impactPoint);

        CameraController.OnProjectileDestroyed();
        SettleOnce();
        Destroy(gameObject);
    }

    protected virtual void Explode(Vector2 center)
    {
        AudioManager.Instance?.PlaySfx("explosion_small");

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
                CombatEventReporter.ReportHit(dmg, amount, center);
            }
        }
    }

    protected virtual void ApplyDirectHit(Collision2D collision, Vector2 impactPoint)
    {
        Rigidbody2D rbHit = collision.rigidbody;
        if (rbHit != null)
        {
            Vector2 dir = (rbHit.position - rb.position).normalized;
            rbHit.AddForce(dir * explosionForce, ForceMode2D.Impulse);
        }

        var dmg = collision.gameObject.GetComponent<IDamageable>();
        if (dmg != null)
        {
            dmg.TakeDamage(explosionForce);
            CombatEventReporter.ReportHit(dmg, explosionForce, impactPoint);
        }
    }

    protected virtual float CalculateDamageByDistance(Vector2 center, Vector2 targetPos)
    {
        float dist = Vector2.Distance(center, targetPos);
        float ratio = Mathf.Clamp01(1f - (dist / explosionRadius));
        return ratio * explosionForce;
    }

    protected virtual void OnBecameInvisible()
    {
        // Anında yok etme yerine tolerans süresi: yörüngedeki mermi ekrana geri dönerse yaşamaya
        // devam eder (OnBecameVisible iptal eder); dönmezse süre sonunda temizlenir.
        if (destroyWhenOffScreen)
            Invoke(nameof(OffscreenExpired), offscreenGraceTime);
    }

    protected virtual void OnBecameVisible()
    {
        CancelInvoke(nameof(OffscreenExpired));
    }

    private void OffscreenExpired()
    {
        CameraController.OnProjectileDestroyed();
        SettleOnce();
        Destroy(gameObject);
    }

    protected virtual void Update()
    {
        if (Time.time - spawnTime >= timeToLive)
        {
            CameraController.OnProjectileDestroyed();
            SettleOnce();
            Destroy(gameObject);
        }
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
