using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class ShotgunPellet : MonoBehaviour
{
    [Header("Planet Effect")]
    public float planetHitRadius = 0.3f;
    public float planetHitForce = 2f;

    private Rigidbody2D rb;
    private GameObject owner;
    private float maxRange;
    private float falloffStart;
    private float maxDamage;
    private float falloffPercent;
    private float travelled;

    // planets we've already reduced range for
    private HashSet<Collider2D> piercedPlanets = new HashSet<Collider2D>();

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Init(Vector2 velocity, float range, float damage, float falloffStartPercent, GameObject ownerObj)
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();
        rb.linearVelocity = velocity;
        owner = ownerObj;
        maxRange = range;
        falloffPercent = falloffStartPercent;
        falloffStart = maxRange * falloffPercent;
        maxDamage = damage;
        travelled = 0f;
    }

    void Update()
    {
        travelled += rb.linearVelocity.magnitude * Time.deltaTime;
        if (travelled >= maxRange)
            Destroy(gameObject);
    }

    float GetDamage()
    {
        if (travelled <= falloffStart)
            return maxDamage;
        float t = Mathf.Clamp01((travelled - falloffStart) / (maxRange - falloffStart));
        return maxDamage * (1f - t);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject == owner)
            return;

        // planet piercing
        if (other.TryGetComponent<DestructiblePlanet>(out var planet))
        {
            planet.ExplodeWithForce(transform.position, planetHitRadius, planetHitForce);
            if (!piercedPlanets.Contains(other))
            {
                float remaining = maxRange - travelled;
                maxRange = travelled + remaining * 0.5f;
                falloffStart = maxRange * falloffPercent;
                piercedPlanets.Add(other);
            }
            return; // continue travelling
        }

        // damage other objects and destroy
        if (other.TryGetComponent<IDamageable>(out var dmg))
            dmg.TakeDamage(GetDamage());

        Destroy(gameObject);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (other.TryGetComponent<DestructiblePlanet>(out var planet))
            planet.ExplodeWithForce(transform.position, planetHitRadius, planetHitForce);
    }
}
