// Assets/Scripts/Planet/DestructiblePlanet.cs
using UnityEngine;

/// <summary>
/// DestructiblePlanet:
/// - SpriteRenderer'daki sprite'ı runtime’da Texture2D’ye kopyalar.
/// - ExplodeWithForce() ile etraftaki rigidbody’lere impulse uygular ve
///   Texture2D’deki pikselleri silerek collider’ı günceller.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(PolygonCollider2D))]
public class DestructiblePlanet : MonoBehaviour
{
    private SpriteRenderer sr;
    private Texture2D runtimeTex;
    private PolygonCollider2D poly;
    private float ppu; // pixels per unit

    private void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            Debug.LogError($"[DestructiblePlanet] {name} üzerinde SpriteRenderer bulunamadı!");
            enabled = false;
            return;
        }

        // Orijinal sprite ve texture bilgisi
        Sprite baseSprite = sr.sprite;
        Texture2D orig = baseSprite.texture;
        int w = orig.width, h = orig.height;

        // 1) Runtime Texture oluştur ve pikselleri kopyala
        runtimeTex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        runtimeTex.SetPixels(orig.GetPixels());
        runtimeTex.Apply();

        // 2) Yeni Sprite oluştur ve SpriteRenderer'a ata
        sr.sprite = Sprite.Create(
            runtimeTex,
            new Rect(0, 0, w, h),
            new Vector2(0.5f, 0.5f),
            baseSprite.pixelsPerUnit
        );

        // 3) Pixels per unit değerini kaydet
        ppu = baseSprite.pixelsPerUnit;

        // 4) İlk sefer polygon collider oluştur
        poly = GetComponent<PolygonCollider2D>();
        RebuildCollider();
    }

    /// <summary>
    /// Patlama geldiğinde çağrılır.
    /// Etraftaki Rigidbody’lere impulse uygular ve görseli parçalar.
    /// </summary>
    /// <param name="worldPos">Patlama merkezi (dünya koordinatı)</param>
    /// <param name="radiusWorld">Patlama yarıçapı (dünya birimi)</param>
    /// <param name="forceStrength">Patlama kuvveti (Impulse mag)</param>
    public void ExplodeWithForce(Vector2 worldPos, float radiusWorld, float forceStrength)
    {
        if (radiusWorld <= 0f) return;

        // 1) Patlama kuvvetini etraftaki objelere uygula
        ApplyExplosionForce(worldPos, radiusWorld, forceStrength);

        // 2) Görseli parçala ve collider’i güncelle
        ExplodeVisual(worldPos, radiusWorld);
    }

    private void ExplodeVisual(Vector2 worldPos, float radiusWorld)
    {
        // World -> Local koordinata dönüştür
        Vector2 local = transform.InverseTransformPoint(worldPos);

        // Sprite pivot ve ppu
        Vector2 pivot = sr.sprite.pivot;
        int px = Mathf.FloorToInt(local.x * ppu + pivot.x);
        int py = Mathf.FloorToInt(local.y * ppu + pivot.y);
        int radPx = Mathf.CeilToInt(radiusWorld * ppu);

        int w = runtimeTex.width;
        int h = runtimeTex.height;

        // Daire içindeki piksellerin alpha’sını sıfırla
        for (int y = -radPx; y <= radPx; y++)
        {
            int ty = py + y;
            if (ty < 0 || ty >= h) continue;

            int xLimit = Mathf.FloorToInt(Mathf.Sqrt(radPx * radPx - y * y));
            for (int x = -xLimit; x <= xLimit; x++)
            {
                int tx = px + x;
                if (tx < 0 || tx >= w) continue;

                Color c = runtimeTex.GetPixel(tx, ty);
                if (c.a != 0f)
                {
                    c.a = 0f;
                    runtimeTex.SetPixel(tx, ty, c);
                }
            }
        }

        runtimeTex.Apply();

        // Collider’ı yeniden oluştur
        RebuildCollider();
    }

    private void RebuildCollider()
    {
        if (poly != null) Destroy(poly);
        poly = gameObject.AddComponent<PolygonCollider2D>();
    }

    private void ApplyExplosionForce(Vector2 worldPos, float radiusWorld, float forceStrength)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(worldPos, radiusWorld);
        foreach (Collider2D hit in hits)
        {
            Rigidbody2D rbHit = hit.attachedRigidbody;
            if (rbHit == null) continue;

            Vector2 objPos = rbHit.worldCenterOfMass;
            Vector2 dir = (objPos - worldPos).normalized;
            float dist = Vector2.Distance(objPos, worldPos);
            float falloff = 1f - Mathf.Clamp01(dist / radiusWorld);
            Vector2 force = dir * forceStrength * falloff;
            rbHit.AddForce(force, ForceMode2D.Impulse);
        }
    }
}
