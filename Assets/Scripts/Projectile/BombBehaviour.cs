using UnityEngine;
using System.Collections;

public class BombBehaviour : MonoBehaviour
{
    private float fuse;
    private GameObject explosionPrefab;
    private bool hasExploded = false;
    private bool _settled = false;

    public void Init(float fuseTime, GameObject expPrefab)
    {
        fuse = fuseTime;
        explosionPrefab = expPrefab;
        CameraController.OnProjectileSpawned(transform);
        TurnManager.Instance?.RegisterShot();
        TurnManager.NotifyProjectileLaunched();
        AudioManager.Instance?.PlayLoopingSfxOnObject(gameObject, "projectile_flight_bomb");
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
        CameraController.OnProjectileDestroyed();
        SettleOnce();
        Destroy(gameObject);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        Explode();
    }

    void OnDestroy()
    {
        SettleOnce();
    }

    private void SettleOnce()
    {
        if (_settled) return;
        _settled = true;
        TurnManager.NotifyProjectileSettled();
    }
}
