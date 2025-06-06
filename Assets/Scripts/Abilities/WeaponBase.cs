using UnityEngine;

public abstract class WeaponBase : MonoBehaviour, IAbility
{
    public KeyCode ActivationKey { get; protected set; }
    public bool IsSelected { get; set; }

    public Transform firePoint;
    public float maxDragDistance = 3f;
    public float powerMultiplier = 10f;
    public GameObject projectilePrefab;

    protected Vector2 dragStartPos;
    protected bool isDragging = false;

    protected virtual void Awake() { IsSelected = false; }

    protected virtual void Update()
    {
        if (!IsSelected) return;
        HandleDragAndFire();
    }

    protected void HandleDragAndFire()
    {
        if (Input.GetMouseButtonDown(0))
        {
            dragStartPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            isDragging = true;
        }
        else if (Input.GetMouseButtonUp(0) && isDragging)
        {
            isDragging = false;
            Vector2 dragEndPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 dragVector = dragStartPos - dragEndPos;
            if (dragVector.magnitude > maxDragDistance)
                dragVector = dragVector.normalized * maxDragDistance;

            Vector2 direction = dragVector.normalized;
            float power = dragVector.magnitude * powerMultiplier;
            FireProjectile(direction, power);
        }
    }

    protected virtual void FireProjectile(Vector2 direction, float power)
    {
        if (projectilePrefab == null || firePoint == null) return;
        GameObject proj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        Rigidbody2D rb = proj.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.linearVelocity = direction * power;
    }

    public virtual void UseAbility() { }
}
