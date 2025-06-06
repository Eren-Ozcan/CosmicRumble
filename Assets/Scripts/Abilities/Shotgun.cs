using UnityEngine;

public class Shotgun : WeaponBase
{
    public int pelletCount = 5;
    public float spreadAngle = 15f;

    void Start()
    {
        ActivationKey = KeyCode.Alpha2;
    }

    protected override void FireProjectile(Vector2 direction, float power)
    {
        for (int i = 0; i < pelletCount; i++)
        {
            float angleOffset = Random.Range(-spreadAngle, spreadAngle);
            Vector2 newDir = Quaternion.Euler(0, 0, angleOffset) * direction;
            GameObject proj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
            Rigidbody2D rb = proj.GetComponent<Rigidbody2D>();
            if (rb != null)
                rb.linearVelocity = newDir * power;
        }
    }
}
