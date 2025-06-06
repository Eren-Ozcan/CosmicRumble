using UnityEngine;
using System.Collections;

public class Bomb : WeaponBase
{
    public float fuseTime = 2f;
    public GameObject explosionPrefab;

    void Start()
    {
        ActivationKey = KeyCode.Alpha4;
    }

    protected override void FireProjectile(Vector2 direction, float power)
    {
        if (projectilePrefab == null || firePoint == null) return;
        GameObject bombObj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        Rigidbody2D rb = bombObj.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.linearVelocity = direction * power;

        BombBehaviour bb = bombObj.AddComponent<BombBehaviour>();
        bb.Init(fuseTime, explosionPrefab);
    }
}

public class BombBehaviour : MonoBehaviour
{
    private float fuse;
    private GameObject explosionPrefab;
    private bool hasExploded = false;

    public void Init(float fuseTime, GameObject expPrefab)
    {
        fuse = fuseTime;
        explosionPrefab = expPrefab;
        StartCoroutine(FuseCoroutine());
    }

    IEnumerator FuseCoroutine()
    {
        yield return new WaitForSeconds(fuse);
        Explode();
    }

    void Explode()
    {
        if (hasExploded) return;
        hasExploded = true;
        if (explosionPrefab != null)
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        Explode();
    }
}
