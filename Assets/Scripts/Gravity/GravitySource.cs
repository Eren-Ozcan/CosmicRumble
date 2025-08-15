// Assets/Scripts/Gravity/GravitySource.cs
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public class GravitySource : MonoBehaviour
{
    [Header("Base Settings (Scale = 1 iken)")]
    [Tooltip("Çekim alanının yarıçapı (Unity birimi)")]
    public float baseRadius = 5f;
    [Tooltip("Çekim kuvvetinin büyüklüğü")]
    public float baseGravityForce = 9.81f;
    public float mass = 10f;
    [Header("Optional Multiplier")]
    [Tooltip("Ekstra kuvvet çarpanı (1 bırakabilirsin)")]
    public float forceMultiplier = 1f;


    public static List<GravitySource> AllSources = new List<GravitySource>();

    void OnEnable() { AllSources.Add(this); }
    void OnDisable() { AllSources.Remove(this); }

    // Runtime’da hesaplanan değerler (read-only inspector’da görmek istersen [ReadOnly] attribute ekleyebilirsiniz)
    [HideInInspector] public float scaledRadius;
    [HideInInspector] public float scaledGravityForce;

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
        // 1) Transform scale ile skala etkileşimi:
        float scaleFactor = transform.localScale.x;
        scaledRadius = baseRadius * scaleFactor;
        scaledGravityForce = baseGravityForce * scaleFactor * forceMultiplier;

        // 2) Collider radius’u runtime’da ayarla:
        gravityCollider.radius = scaledRadius;
    }

    // Force application moved to GravityBody. This trigger now only serves
    // to detect bodies for gameplay (e.g., grounding) via their own logic.

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        float drawRadius = baseRadius * transform.localScale.x;
        Gizmos.color = new Color(0f, 1f, 1f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, drawRadius);
    }
#endif
}
