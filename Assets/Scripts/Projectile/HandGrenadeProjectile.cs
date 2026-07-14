using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CosmicRumble.Achievements;

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
    private bool _settled;
    private float _lastBounceSfxTime = -999f;

    [Header("Sekme Sesi")]
    [Tooltip("Art arda sekmelerde sesin spam olmaması için minimum aralık (saniye)")]
    public float bounceSfxCooldown = 0.15f;
    [Tooltip("Bu hızın altındaki temaslar sekme sayılmaz (yavaş yuvarlanma sırasında spam'i önler)")]
    public float minBounceSpeed = 0.5f;

    private void Start()
    {
        // Uçuş loop'u server'da Init() içinde başlıyor — replike (non-server) kopyada Init hiç
        // çalışmadığı için client el bombasını sessiz izliyordu.
        var netObj = GetComponent<Unity.Netcode.NetworkObject>();
        var nm = Unity.Netcode.NetworkManager.Singleton;
        if (netObj != null && netObj.IsSpawned && (nm == null || !nm.IsServer))
            AudioManager.Instance?.PlayLoopingSfxOnObject(gameObject, "projectile_flight_grenade");
    }

    public void Init(Vector2 initialVelocity, GameObject ownerObj, float ignoreTime)
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        NetworkPhysicsGuard.EnsureDynamicWhenNotSpawned(rb);
        owner = ownerObj;

        // Temporarily ignore all characters for 0.3 s to prevent spawn-push
        StartCoroutine(TemporaryIgnoreCharacters());

        rb.linearVelocity = initialVelocity;
        CameraController.OnProjectileSpawned(transform);
        TurnManager.NotifyProjectileLaunched();
        AudioManager.Instance?.PlayLoopingSfxOnObject(gameObject, "projectile_flight_grenade");

        // Owner-specific ignore for the full ignoreTime duration
        foreach (var oc in owner.GetComponentsInChildren<Collider2D>())
            Physics2D.IgnoreCollision(col, oc, true);

        Invoke(nameof(ReenableOwnerCollision), ignoreTime);
        Invoke(nameof(Explode), delayBeforeExplosion);
    }

    private void Explode()
    {
        Vector2 pos = transform.position;

        AudioManager.Instance?.PlaySfx("explosion_small");

        if (explosionEffect != null)
        {
            GameObject fx = Instantiate(explosionEffect, pos, Quaternion.identity);
            fx.transform.localScale = transform.localScale;
        }

        var hits = Physics2D.OverlapCircleAll(pos, explosionRadius);
        bool hitAny = false;
        foreach (var hit in hits)
        {
            float distance = Vector2.Distance(hit.transform.position, pos);
            float falloff = 1f - Mathf.Clamp01(distance / explosionRadius);

            // ✅ Yakınlığa göre hasar (sahip dahil)
            if (hit.TryGetComponent<IDamageable>(out var dmg))
            {
                float dmgAmount = maxDamage * falloff;
                dmg.TakeDamage(dmgAmount);
                hitAny = true;
                CombatEventReporter.ReportHit(dmg, dmgAmount, pos);
            }

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

        #if UNITY_EDITOR
        Debug.Log("💣 El bombası patladı!");
        #endif
        CameraController.OnProjectileDestroyed();
        SettleOnce(hitAny);
        NetworkPhysicsGuard.DespawnOrDestroy(gameObject, this);
    }

    private void SettleOnce(bool isHit = false)
    {
        if (_settled) return;
        _settled = true;
        AchievementEvents.FireShotFired(isHit);
        TurnManager.NotifyProjectileSettled();
    }

    private void OnDestroy() => SettleOnce(); // ensure settle if destroyed before Explode fires

    // Zamanlayıcıyla patlıyor, temas anında değil — bu yüzden yere/gezegene her sekişinde
    // ayrı bir "bounce" sesi gerekiyor (patlamayla karıştırılmamalı). Hızlı art arda
    // sekmelerde spam olmasın diye cooldown + minimum hız eşiği var.
    private void OnCollisionEnter2D(Collision2D collision)
    {
        float impactSpeed = collision.relativeVelocity.magnitude;
        if (impactSpeed < minBounceSpeed) return;
        if (Time.time - _lastBounceSfxTime < bounceSfxCooldown) return;

        _lastBounceSfxTime = Time.time;
        AudioManager.Instance?.PlaySfx("grenade_bounce");
    }

    private IEnumerator TemporaryIgnoreCharacters()
    {
        var bodies = FindObjectsByType<GravityBody>(FindObjectsSortMode.None);
        var cols = new List<Collider2D>();
        foreach (var body in bodies)
            cols.AddRange(body.GetComponentsInChildren<Collider2D>());

        foreach (var c in cols)
            if (c != null) Physics2D.IgnoreCollision(col, c, true);

        yield return new WaitForSeconds(0.3f);

        if (this == null) yield break;   // grenade already exploded

        foreach (var c in cols)
        {
            if (c == null) continue;
            // Owner collision is managed separately by ReenableOwnerCollision — don't override it
            if (owner != null && c.transform.IsChildOf(owner.transform)) continue;
            Physics2D.IgnoreCollision(col, c, false);
        }
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
