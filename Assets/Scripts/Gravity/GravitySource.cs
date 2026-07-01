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

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.attachedRigidbody == null) return;

        Rigidbody2D rb = other.attachedRigidbody;

        Vector2 direction = (Vector2)transform.position - rb.position;
        float distance = direction.magnitude;
        if (distance <= 0f) return;
        if (distance < 0.3f) return;

        // Gravity formulas — choose one:
        // 1/r² (realistic): force increases as character approaches center
        //   → causes extreme pull near core, unpredictable gameplay feel
        // float forceMag = gravityForce / (distance * distance);

        // Constant (arcade): same pull everywhere regardless of distance
        //   → consistent movement speed, recommended for turn-based gameplay
        float forceMag = gravityForce;

        rb.AddForce(direction.normalized * forceMag, ForceMode2D.Force);
    }

// Gizmo çizimi PlanetDebugVisualizer tarafından yapılır (ColliderDebugVisualizer.cs).
// Buradaki OnDrawGizmosSelected kaldırıldı — çift çizim önlendi.
}
