using UnityEngine;

public class BlackHoleZone : MonoBehaviour
{
    [Header("Colliders")]
    [SerializeField] private CircleCollider2D coreCollider;
    [SerializeField] private CircleCollider2D fieldCollider;

    [Header("Effects")]
    public float pullForce = 20f;
    public float damagePerSecond = 5f;
    public float duration = 5f;

    void Start()
    {
        if (coreCollider != null) coreCollider.isTrigger = true;
        if (fieldCollider != null) fieldCollider.isTrigger = true;
        Destroy(gameObject, duration);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        // Damage if inside core
        if (coreCollider != null && coreCollider.IsTouching(other))
        {
            if (other.TryGetComponent<IDamageable>(out var dmg))
                dmg.TakeDamage(damagePerSecond * Time.deltaTime);

            if (other.CompareTag("PlanetFragment"))
                Destroy(other.gameObject);
            return;
        }

        // Pull if inside gravitational field
        if (fieldCollider != null && fieldCollider.IsTouching(other))
        {
            Rigidbody2D rb = other.attachedRigidbody;
            if (rb != null)
            {
                Vector2 dir = ((Vector2)transform.position - rb.position).normalized;
                rb.AddForce(dir * pullForce * Time.deltaTime, ForceMode2D.Force);
            }
        }
    }
}
