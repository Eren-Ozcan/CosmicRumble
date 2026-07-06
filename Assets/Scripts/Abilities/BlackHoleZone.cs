using UnityEngine;
using System.Collections.Generic;
using CosmicRumble.Achievements;

[RequireComponent(typeof(Rigidbody2D))]
public class BlackHoleZone : MonoBehaviour
{
    private static BlackHoleZone active;

    [Header("Colliders (isTrigger=true)")]
    [SerializeField] private CircleCollider2D coreCollider;   // küçük (x)
    [SerializeField] private CircleCollider2D fieldCollider;  // büyük (x+y)

    [Header("Effects")]
    public float pullForce = 20f;          // çekim alanı kuvveti (x+y)
    public float damagePerSecond = 5f;     // çekirdek DoT (x)
    public float duration = 5f;            // zone yaşam süresi

    readonly List<Collider2D> _hits = new List<Collider2D>(32);
    ContactFilter2D _filter;

    // Skill/Projectile runtime’dan dolduruyor
    public void Configure(CircleCollider2D core, CircleCollider2D field, float pull, float dps, float dur)
    {
        coreCollider = core;
        fieldCollider = field;
        pullForce = pull;
        damagePerSecond = dps;
        duration = dur;
    }

    void Awake()
    {
        if (active != null && active != this) { Destroy(gameObject); return; }
        active = this;

        var rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.simulated = true;

        _filter = new ContactFilter2D { useTriggers = true, useLayerMask = false };
    }

    void Start()
    {
        if (coreCollider != null) coreCollider.isTrigger = true;
        if (fieldCollider != null) fieldCollider.isTrigger = true;

        if (duration > 0f) Destroy(gameObject, duration);
    }

    void OnDestroy()
    {
        if (active == this) active = null;
    }

    void FixedUpdate()
    {
        // 1) Core: DoT
        if (coreCollider != null)
        {
            _hits.Clear();
            Physics2D.OverlapCollider(coreCollider, _filter, _hits);
            foreach (var c in _hits)
            {
                if (c == null) continue;
                if (c.TryGetComponent<IDamageable>(out var dmg))
                {
                    float dmgAmount = damagePerSecond * Time.fixedDeltaTime;
                    dmg.TakeDamage(dmgAmount);
                    CombatEventReporter.ReportDamage(dmgAmount);
                }
            }
        }

        // 2) Field: çekim
        if (fieldCollider != null)
        {
            _hits.Clear();
            Physics2D.OverlapCollider(fieldCollider, _filter, _hits);
            Vector2 center = transform.position;

            foreach (var c in _hits)
            {
                var rb = c.attachedRigidbody;
                if (rb == null) continue;

                // GravityBody.ApplyForce, sahibi bu makine değilse (networked modda bu zone sadece
                // server'da çalışır) doğru sahibin makinesine ClientRpc ile yönlendirir — doğrudan
                // rb.AddForce çağrısı, hedef server'da kinematic (non-owner) ise sessizce no-op
                // olurdu, offline'da veya sahip bizsek (host kendi karakterini etkiliyorsa) hâlâ
                // doğrudan uygulanır.
                var gravityBody = c.attachedRigidbody.GetComponent<GravityBody>();
                Vector2 dir = (center - rb.position).normalized;
                Vector2 force = dir * pullForce;

                if (gravityBody != null)
                {
                    gravityBody.ApplyForce(force, ForceMode2D.Force);
                }
                else
                {
                    if (rb.bodyType != RigidbodyType2D.Dynamic) continue;
                    rb.AddForce(force, ForceMode2D.Force);
                }
            }
        }
    }
}
