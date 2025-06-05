using UnityEngine;

public class RPG : ProjectileBase
{
    [Header("RPG Ozel Ayarlari")]
    public Vector2 initialVelocity = new Vector2(5f, 5f);

    protected override void Start()
    {
        base.Start();
        // Eski: rb.velocity = initialVelocity;
        rb.linearVelocity = initialVelocity;
    }

    protected override void OnCollisionEnter2D(Collision2D collision)
    {
        CameraShake cs = Camera.main.GetComponent<CameraShake>();
        if (cs != null)
            cs.DoShake(0.2f, 0.1f);

        base.OnCollisionEnter2D(collision);
    }

    protected override void Explode(Vector2 center)
    {
        // Ekstra efekt eklemek isterseniz buraya
        base.Explode(center);
    }
}
