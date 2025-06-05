using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public class GravitySource : MonoBehaviour
{
    [Header("Base Settings (Scale = 1 iken)")]
    [Tooltip("Çekim alanının yarıçapı (Unity birimi)")]
    public float baseRadius = 5f;
    [Tooltip("Çekim kuvvetinin büyüklüğü")]
    public float baseGravityForce = 9.81f;

    [Header("Optional Multiplier")]
    [Tooltip("Ekstra kuvvet çarpanı (1 bırakabilirsin)")]
    public float forceMultiplier = 1f;

    // Runtime’da hesaplanan değerler
    [HideInInspector] public float scaledRadius;
    [HideInInspector] public float scaledGravityForce;

    private CircleCollider2D gravityCollider;

    void Awake()
    {
        gravityCollider = GetComponent<CircleCollider2D>();
        gravityCollider.isTrigger = true;
    }

    void Start()
    {
        float scaleFactor = transform.localScale.x;
        scaledRadius = baseRadius * scaleFactor;
        scaledGravityForce = baseGravityForce * scaleFactor * forceMultiplier;
        gravityCollider.radius = scaledRadius;
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        float drawRadius = baseRadius * transform.localScale.x;
        Gizmos.color = new Color(0f, 1f, 1f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, drawRadius);
    }
#endif
}
