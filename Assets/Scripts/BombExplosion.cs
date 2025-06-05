// BombExplosion
using UnityEngine;

public class BombExplosion : MonoBehaviour
{
    public float explosionRadius = 1f;       // Dünya birimi cinsinden yarıçap
    public float explosionForce = 10f;       // Kuvvet büyüklüğü
    public GameObject explosionEffectPrefab; // Opsiyonel: particle efekti

    // Patlamaya izin vermek için kısa bir gecikme ekliyoruz
    private bool launched = false;

    void Start()
    {
        // Oluştuğunda hemen çarpışma kontrolü yapmasın
        Invoke(nameof(EnableLaunch), 0.1f);
    }

    void EnableLaunch()
    {
        launched = true;
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        // Eğer henüz ‘launched’ false ise (0.1 sn geçmediyse), patlamayı engelle
        if (!launched) return;

        // 1) Patlama merkezini belirle
        Vector2 patlamaPos = transform.position;

        // 2) Çarpılan nesnenin “DestructiblePlanet” script’i var mı kontrol et
        DestructiblePlanet dp = col.gameObject.GetComponent<DestructiblePlanet>();
        if (dp != null)
        {
            // 3) Patlamayı tetikle
            dp.ExplodeWithForce(patlamaPos, explosionRadius, explosionForce);
        }

        // 4) Opsiyonel: Partikül efekti yarat
        if (explosionEffectPrefab != null)
        {
            Instantiate(explosionEffectPrefab, patlamaPos, Quaternion.identity);
        }

        // 5) Bomb objesini yok et
        Destroy(gameObject);
    }
}
