using UnityEngine;
using CosmicRumble.Achievements;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class KineticProjectile : MonoBehaviour
{
    [Header("Owner & Movement")]
    public GameObject owner;
    public float ignoreOwnerTime = 0.5f;
    public float gravityScale = 0f;

    [Header("Range & Damage")]
    public float maxRange = 12f;
    public float maxDamage = 8f;
    [Range(0f, 1f)] public float fullDamagePortion = 0.30f;
    public float minDamage = 0f;

    [Header("Planet Interaction")]
    public bool destroyOnPlanetHit = true;
    public bool applyPlanetDamage = true;
    public float planetDamageRadius = 0.6f;      // fallback tek damga
    public float planetDamageForce = 2f;        // toplam kuvvet (stamps’e bölünür)
    [Range(0f, 1f)] public float planetDamageMultiplier = 1f; // tek yerden kısma

    [Header("Elongated Planet Damage (capsule/oval)")]
    public bool useElongatedPlanetDamage = true;
    public float ovalLength = 6f;                // toplam uzunluk
    public float ovalWidth = 3f;                // çap
    public int ovalStamps = 7;                 // kaç damga
    public bool alignToVelocity = true;
    public bool anchorAtSurface = true;         // temas noktasından içeri tek yön

    [Header("Penetration (Chunks)")]
    public bool useTrigger = true;
    public LayerMask planetChunkLayer;
    public bool drain2xInsideChunk = true;
    [Tooltip("Range tüketim çarpanı chunk içinde (drain2xInsideChunk aktifken uygulanır)")]
    public float chunkDrainMultiplier = 2f;

    [Header("Raycast (planet temas noktası)")]
    [Tooltip("Gezegen collider’ının layer’ı. Doğru set edilmezse fallback çalışır ve merkez delinir.")]
    public LayerMask planetLayer = ~0;

    [Header("On Character Hit")]
    public bool stopOnDamageable = true;

    // --- runtime ---
    Rigidbody2D rb; Collider2D col;
    Vector2 lastPos;
    float traveled;
    bool insideChunk;
    bool planetDamageApplied; // ✅ aynı mermide tekrar delmeyi önle
    bool hitCharacter;        // isabet/miss raporlaması için (OnDestroy'da)

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        rb.gravityScale = gravityScale;
        if (useTrigger) col.isTrigger = true;
    }

    public void Init(Vector2 initialVelocity, GameObject ownerObj, float ignoreTime = 0.5f)
    {
        owner = ownerObj;
        ignoreOwnerTime = ignoreTime;
        lastPos = rb.position;

        if (owner != null)
        {
            foreach (var oc in owner.GetComponentsInChildren<Collider2D>())
                Physics2D.IgnoreCollision(col, oc, true);
            Invoke(nameof(ReenableOwnerCollision), ignoreOwnerTime);
        }

        rb.linearVelocity = initialVelocity;
        CameraController.OnProjectileSpawned(transform);
    }

    void OnDestroy()
    {
        CameraController.OnProjectileDestroyed();
        TurnManager.NotifyProjectileSettled();
        AchievementEvents.FireShotFired(hitCharacter);
    }

    void FixedUpdate()
    {
        // Menzil takibi FixedUpdate'te — rb.position burada tutarlı
        Vector2 p = rb.position;
        float frameDist = (p - lastPos).magnitude;
        traveled += frameDist * (insideChunk && drain2xInsideChunk ? chunkDrainMultiplier : 1f);
        lastPos = p;

        if (traveled >= maxRange)
        {
            Destroy(gameObject);
            return;
        }

        Vector2 vel = rb.linearVelocity;
        if (vel.sqrMagnitude > 0.01f)
            transform.right = vel.normalized;
    }

    void ReenableOwnerCollision()
    {
        if (!owner) return;
        foreach (var oc in owner.GetComponentsInChildren<Collider2D>())
            Physics2D.IgnoreCollision(col, oc, false);
    }

    // ---- Trigger flow ----
    void OnTriggerEnter2D(Collider2D other)
    {
        if (IsOwner(other)) return;

        if (other.TryGetComponent<IDamageable>(out var dmg))
        {
            float dmgAmount = ComputeDamage();
            dmg.TakeDamage(dmgAmount);
            hitCharacter = true;
            CombatEventReporter.ReportHit(dmg, dmgAmount, other.bounds.center);
            if (stopOnDamageable) Destroy(gameObject);
            return;
        }

        if (other.TryGetComponent<DestructiblePlanet>(out var dp))
        {
            if (planetDamageApplied) { if (destroyOnPlanetHit) Destroy(gameObject); return; } // ✅
            planetDamageApplied = true;

            if (applyPlanetDamage)
            {
                Vector2 hitPoint, hitNormal;
                FindPlanetContact(out hitPoint, out hitNormal); // ✅ gerçek temas
                if (useElongatedPlanetDamage)
                    ApplyElongatedDamage(dp, hitPoint, hitNormal);
                else if (planetDamageRadius > 0f)
                    dp.ExplodeWithForce(hitPoint,
                        planetDamageRadius * planetDamageMultiplier,
                        planetDamageForce * planetDamageMultiplier);
            }

            if (destroyOnPlanetHit) Destroy(gameObject);
            return;
        }

        if (IsPlanetChunk(other))
        {
            insideChunk = true;
            return;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (IsPlanetChunk(other))
            insideChunk = false;
    }

    // ---- Collision flow (trigger kapalıysa) ----
    void OnCollisionEnter2D(Collision2D c)
    {
        if (useTrigger) return;
        if (IsOwner(c.collider)) return;

        if (c.collider.TryGetComponent<IDamageable>(out var dmg))
        {
            float dmgAmount = ComputeDamage();
            dmg.TakeDamage(dmgAmount);
            hitCharacter = true;
            CombatEventReporter.ReportHit(dmg, dmgAmount, c.GetContact(0).point);
            if (stopOnDamageable) Destroy(gameObject);
            return;
        }

        if (IsPlanetChunk(c.collider))
        {
            Physics2D.IgnoreCollision(col, c.collider, true);
            return;
        }

        if (c.collider.TryGetComponent<DestructiblePlanet>(out var dp))
        {
            if (planetDamageApplied) { if (destroyOnPlanetHit) Destroy(gameObject); return; } // ✅
            planetDamageApplied = true;

            if (applyPlanetDamage)
            {
                var contact = c.GetContact(0);
                Vector2 hitPoint = contact.point;
                Vector2 hitNormal = contact.normal;
                if (useElongatedPlanetDamage)
                    ApplyElongatedDamage(dp, hitPoint, hitNormal);
                else if (planetDamageRadius > 0f)
                    dp.ExplodeWithForce(hitPoint,
                        planetDamageRadius * planetDamageMultiplier,
                        planetDamageForce * planetDamageMultiplier);
            }

            if (destroyOnPlanetHit) Destroy(gameObject);
            return;
        }

        Destroy(gameObject);
    }

    // ---- Helpers ----
    float ComputeDamage()
    {
        float t = traveled / Mathf.Max(0.0001f, maxRange);
        if (t <= fullDamagePortion) return Mathf.Max(minDamage, maxDamage);
        float k = Mathf.InverseLerp(fullDamagePortion, 1f, t);
        return Mathf.Max(minDamage, maxDamage * (1f - k));
    }

    bool IsOwner(Collider2D c2d)
    {
        if (!owner) return false;
        return c2d.transform.IsChildOf(owner.transform);
    }

    bool IsPlanetChunk(Collider2D c2d)
    {
        if (planetChunkLayer != 0)
        {
            int lm = 1 << c2d.gameObject.layer;
            if ((planetChunkLayer.value & lm) != 0) return true;
        }
        return false;
    }

    /// Gerçek temas noktasını bul: Raycast + CircleCast yedek
    void FindPlanetContact(out Vector2 hitPoint, out Vector2 hitNormal)
    {
        Vector2 from = lastPos;
        Vector2 move = (rb.position - lastPos);
        float dist = move.magnitude;
        Vector2 dir = (dist > 1e-6f) ? (move / dist)
                        : (rb.linearVelocity.sqrMagnitude > 0.001f ? rb.linearVelocity.normalized : Vector2.right);

        // Raycast
        var hit = Physics2D.Raycast(from, dir, dist + 0.5f, planetLayer);
        if (hit.collider != null)
        {
            hitPoint = hit.point;
            hitNormal = hit.normal;
            return;
        }

        // CircleCast (tünellemeyi yakala)
        float r = (col is CircleCollider2D cc) ? cc.radius * Mathf.Abs(transform.lossyScale.x) : 0.1f;
        var chit = Physics2D.CircleCast(from, r, dir, dist + r, planetLayer);
        if (chit.collider != null)
        {
            hitPoint = chit.point;
            hitNormal = chit.normal;
            return;
        }

        // Fallback (yanlış layer ayarlıysa)
        hitPoint = rb.position;
        hitNormal = -dir;
    }

    // Çoklu stamp ile kapsül/oval (normalize force, tek-yön opsiyonu)
    void ApplyElongatedDamage(DestructiblePlanet dp, Vector2 hitPoint, Vector2 surfaceNormal)
    {
        Vector2 mainDir = (alignToVelocity && rb.linearVelocity.sqrMagnitude > 0.001f)
            ? (Vector2)rb.linearVelocity.normalized
            : Vector2.Perpendicular(surfaceNormal).normalized;

        Vector2 start = hitPoint + (-surfaceNormal) * 0.05f;

        int stamps = Mathf.Max(1, ovalStamps);
        float totalForce = planetDamageForce * Mathf.Clamp01(planetDamageMultiplier);
        float perForce = (stamps > 0) ? totalForce / stamps : totalForce;

        float baseR = Mathf.Max(0.01f, ovalWidth * 0.5f) * Mathf.Clamp01(planetDamageMultiplier);
        float half = Mathf.Max(0.001f, ovalLength * 0.5f);

        for (int i = 0; i < stamps; i++)
        {
            if (anchorAtSurface)
            {
                // 0..1: temas noktasından içeri tek yönde
                float t = (stamps == 1) ? 0f : (float)i / (stamps - 1);
                Vector2 pos = start + mainDir * (t * ovalLength);
                float r = baseR * (1f - 0.2f * t);   // içeriye doğru hafif incel
                dp.ExplodeWithForce(pos, r, perForce);
            }
            else
            {
                // -1..+1: iki yöne simetrik
                float t = (stamps == 1) ? 0f : Mathf.Lerp(-1f, 1f, (float)i / (stamps - 1));
                Vector2 pos = start + mainDir * (half * t);
                float r = baseR * (1f - 0.3f * Mathf.Abs(t));
                dp.ExplodeWithForce(pos, r, perForce);
            }
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (planetDamageRadius > 0f)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, planetDamageRadius * Mathf.Clamp01(planetDamageMultiplier));
        }
    }
#endif
}
