// Assets/Scripts/Gravity/GravitySource.cs
using System.Collections.Generic;
using UnityEngine;

public class GravitySource : MonoBehaviour
{
    [Tooltip("Distance (Unity units) where gravity will act")]
    public float gravityRadius = 5f;

    [Tooltip("Gravitational force (constant value)")]
    public float gravityForce = 9.81f;

    public static List<GravitySource> AllSources = new List<GravitySource>();

    void OnEnable() { AllSources.Add(this); }
    void OnDisable() { AllSources.Remove(this); }

    private CircleCollider2D gravityCollider;

    private void Awake()
    {
        // Önce GravityTrigger adlı child'ı ara — solid surface collider'ı yanlışlıkla
        // trigger'a çevirmemek için kendimizi (transform) atla.
        var triggerChild = transform.Find("GravityTrigger");
        if (triggerChild != null)
            gravityCollider = triggerChild.GetComponent<CircleCollider2D>();

        // GravityTrigger yoksa eski davranış: herhangi bir child ColliderCollider2D
        if (gravityCollider == null)
        {
            foreach (Transform child in transform)
            {
                gravityCollider = child.GetComponent<CircleCollider2D>();
                if (gravityCollider != null) break;
            }
        }

        if (gravityCollider == null)
        {
#if UNITY_EDITOR
            Debug.LogError($"[GravitySource] {name} altında CircleCollider2D bulunamadı (GravityTrigger child bekleniyor)!");
#endif
            enabled = false;
            return;
        }
        gravityCollider.isTrigger = true;
    }

    private void Start()
    {
        if (gravityCollider == null) return;

        // gravityRadius is a world-space value set in the Inspector.
        // CircleCollider2D.radius is local-space, so divide by lossyScale
        // to keep the trigger zone consistent regardless of parent scale
        // (e.g. Planet_Interior under Planet_External with scale 10,10,1).
        float worldScale = Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));
        gravityCollider.radius = worldScale > 0f ? gravityRadius / worldScale : gravityRadius;
    }

    // ── Yerçekimi kuvveti uygulaması ────────────────────────────────────────
    // Eskiden OnTriggerStay2D ile yapılıyordu, ama bu yapı kırılgandı: trigger collider child
    // "GravityTrigger" objesinde ve hiyerarşide Rigidbody2D olmadığı için Unity o child'ın trigger
    // callback'lerini bu script'e HİÇ iletmiyordu — çekim yalnızca, sahnedeki Planet_Interior
    // collider'ının (script'le aynı objede) elle trigger yapılmış olması sayesinde çalışıyordu ve
    // yarıçapı gravityRadius ile eşleşmiyordu. Artık kuvvet FixedUpdate'te gravityRadius içindeki
    // tüm dinamik Rigidbody2D'lere doğrudan uygulanır: mermiler dahil her şey gezegen merkezine
    // çekilir, etki alanı tam olarak gravityRadius'tur ve TrajectoryDots'un Strategy tabanlı
    // tahminiyle birebir aynı formül kullanılır.
    private static readonly Collider2D[] _overlapBuffer = new Collider2D[64];
    private static readonly HashSet<Rigidbody2D> _seenBodies = new HashSet<Rigidbody2D>();
    private static ContactFilter2D _overlapFilter = CreateOverlapFilter();

    private static ContactFilter2D CreateOverlapFilter()
    {
        var f = new ContactFilter2D();
        f.NoFilter();
        f.useTriggers = true; // mermi collider'ları trigger olabilir (KineticProjectile)
        return f;
    }

    private void FixedUpdate()
    {
        Vector2 center = transform.position;
        int count = Physics2D.OverlapCircle(center, gravityRadius, _overlapFilter, _overlapBuffer);

        _seenBodies.Clear();
        for (int i = 0; i < count; i++)
        {
            Rigidbody2D rb = _overlapBuffer[i] != null ? _overlapBuffer[i].attachedRigidbody : null;
            if (rb == null) continue;
            if (!_seenBodies.Add(rb)) continue;                     // aynı body'ye çift kuvvet yok
            if (rb.bodyType != RigidbodyType2D.Dynamic) continue;   // kinematic/static: AddForce no-op
            if (rb.IsSleeping()) continue;                          // uyuyan body'leri uyandırma

            Vector2 direction = center - rb.position;
            float distance = direction.magnitude;
            if (distance < 0.3f) continue; // merkez tekilliği koruması

            // Gravity formulas — choose one:
            // 1/r² (realistic): force increases as character approaches center
            //   → causes extreme pull near core, unpredictable gameplay feel
            // float forceMag = gravityForce / (distance * distance);

            // Constant (arcade): same pull everywhere regardless of distance
            //   → consistent movement speed, recommended for turn-based gameplay
            // rb.mass ile çarpılır ki ivme kütleden bağımsız gravityForce olsun —
            // IGravityStrategy.CalculateAcceleration (yörünge tahmini) ile birebir tutarlı.
            float forceMag = gravityForce * rb.mass;

            rb.AddForce(direction.normalized * forceMag, ForceMode2D.Force);
        }
    }

// Gizmo çizimi PlanetDebugVisualizer tarafından yapılır (ColliderDebugVisualizer.cs).
// Buradaki OnDrawGizmosSelected kaldırıldı — çift çizim önlendi.
}
