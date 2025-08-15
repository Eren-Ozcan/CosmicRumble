// Assets/Scripts/Gravity/GravitySource.cs
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public class GravitySource : MonoBehaviour
{
    [Header("Gravity Settings")]
    [Tooltip("Gravitational force magnitude")]
    public float gravityForce = 9.81f;
    [Tooltip("Radius where gravity affects objects")]
    public float gravityRadius = 5f;


    public static List<GravitySource> AllSources = new List<GravitySource>();

    void OnEnable() { AllSources.Add(this); }
    void OnDisable() { AllSources.Remove(this); }

    private CircleCollider2D gravityCollider;

    private void Awake()
    {
        gravityCollider = GetComponent<CircleCollider2D>();
        if (gravityCollider == null)
        {
            Debug.LogError($"[GravitySource] {name} üzerinde CircleCollider2D bulunamadı!");
            enabled = false;
            return;
        }
        gravityCollider.isTrigger = true;
    }

    private void Start()
    {
        gravityCollider.radius = gravityRadius;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        Rigidbody2D rb = other.attachedRigidbody;
        if (rb == null) return;

        Vector2 direction = ((Vector2)transform.position - rb.position);
        float distance = direction.magnitude;
        if (distance <= 0f) return;

        // Inverse square law: F = G / r^2
        float forceMag = gravityForce / (distance * distance);
        rb.AddForce(direction.normalized * forceMag, ForceMode2D.Force);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 1f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, gravityRadius);
    }
#endif
}
