// Assets/Scripts/Gravity/GravitySource.cs
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public class GravitySource : MonoBehaviour
{
    [Header("Gravity Settings")]
    [Tooltip("Gravitational force (constant value)")]
    public float gravityForce = 9.81f;
    [Tooltip("Distance (area) where gravity will act")]
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

    // Force application moved to GravityBody. This trigger now only serves
    // to detect bodies for gameplay (e.g., grounding) via their own logic.

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 1f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, gravityRadius);
    }
#endif
}

