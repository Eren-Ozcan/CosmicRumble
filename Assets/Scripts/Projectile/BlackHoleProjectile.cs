using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class BlackHoleProjectile : MonoBehaviour
{
    public GameObject blackHolePrefab;
    public float lifeTime = 8f;

    private Rigidbody2D rb;
    private Collider2D col;
    private GameObject owner;

    public void Init(Vector2 initialVelocity, GameObject ownerObj, float ignoreTime)
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        owner = ownerObj;

        rb.linearVelocity = initialVelocity;

        foreach (var oc in owner.GetComponentsInChildren<Collider2D>())
            Physics2D.IgnoreCollision(col, oc, true);

        Invoke(nameof(ReenableOwnerCollision), ignoreTime);
        if (lifeTime > 0f)
            Destroy(gameObject, lifeTime);
    }

    private void ReenableOwnerCollision()
    {
        if (owner == null || col == null) return;
        foreach (var oc in owner.GetComponentsInChildren<Collider2D>())
            Physics2D.IgnoreCollision(col, oc, false);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        SpawnBlackHole();
    }

    private void SpawnBlackHole()
    {
        if (blackHolePrefab != null)
            Instantiate(blackHolePrefab, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }
}
