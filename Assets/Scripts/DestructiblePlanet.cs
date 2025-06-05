using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(PolygonCollider2D))]
public class DestructiblePlanet : MonoBehaviour
{
    private SpriteRenderer sr;
    private Texture2D runtimeTex;
    private PolygonCollider2D poly;
    private float ppu;

    void Start()
    {
        // 1) SpriteRenderer ve orijinal sprite bilgisi
        sr = GetComponent<SpriteRenderer>();
        Sprite baseSprite = sr.sprite;
        Texture2D orig = baseSprite.texture;
        int w = orig.width, h = orig.height;

        // 2) Runtime Texture oluştur ve pikselleri kopyala
        runtimeTex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        runtimeTex.SetPixels(orig.GetPixels());
        runtimeTex.Apply();

        // 3) Yeni Sprite oluştur ve SpriteRenderer'a ata
        Sprite newSpr = Sprite.Create(
            runtimeTex,
            new Rect(0, 0, w, h),
            new Vector2(0.5f, 0.5f),
            baseSprite.pixelsPerUnit
        );
        sr.sprite = newSpr;

        // 4) PPU değerini kaydet
        ppu = baseSprite.pixelsPerUnit;

        // 5) Collider'ı ilk defa oluştur
        poly = GetComponent<PolygonCollider2D>();
        RebuildCollider();
    }

    // Explosion ile çağrılacak metot
    public void ExplodeWithForce(Vector2 worldPos, float radiusWorld, float forceStrength)
    {
        // a) Çevredeki nesnelere kuvvet uygula
        ApplyExplosionForce(worldPos, radiusWorld, forceStrength);

        // b) Terrain görselini parçalama işlemi
        ExplodeVisual(worldPos, radiusWorld);
    }

    // Terrain üzerindeki pikselleri sil, collider’ı güncelle
    private void ExplodeVisual(Vector2 worldPos, float radiusWorld)
    {
        // 1) World pos'u local koordinata çevir
        Vector2 local = transform.InverseTransformPoint(worldPos);

        // 2) Sprite rect ve pivot bilgilerini al
        Vector2 pivot = sr.sprite.pivot;

        // 3) Piksel koordinatına çevir
        int px = Mathf.FloorToInt(local.x * ppu + pivot.x);
        int py = Mathf.FloorToInt(local.y * ppu + pivot.y);

        // 4) Radius’u pixel cinsine çevir
        int radPx = Mathf.CeilToInt(radiusWorld * ppu);

        // 5) Daire içerisindeki piksellerin alpha'sini 0 yap
        for (int y = -radPx; y <= radPx; y++)
        {
            for (int x = -radPx; x <= radPx; x++)
            {
                if (x * x + y * y <= radPx * radPx)
                {
                    int tx = px + x;
                    int ty = py + y;
                    if (tx >= 0 && tx < runtimeTex.width && ty >= 0 && ty < runtimeTex.height)
                    {
                        Color c = runtimeTex.GetPixel(tx, ty);
                        if (c.a != 0f)
                        {
                            c.a = 0f;
                            runtimeTex.SetPixel(tx, ty, c);
                        }
                    }
                }
            }
        }

        // 6) Texture’i uygula
        runtimeTex.Apply();

        // 7) Collider’ı yeniden oluştur
        RebuildCollider();
    }

    // Collider’ı sil ve yeniden ekle
    private void RebuildCollider()
    {
        if (poly != null) Destroy(poly);
        poly = gameObject.AddComponent<PolygonCollider2D>();
        // Unity, son halindeki sprite’a göre otomatik cizer
    }

    // Çevredeki nesnelere patlama kuvveti uygular
    private void ApplyExplosionForce(Vector2 worldPos, float radiusWorld, float forceStrength)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(worldPos, radiusWorld);
        foreach (Collider2D hit in hits)
        {
            Rigidbody2D rb = hit.attachedRigidbody;
            if (rb == null) continue;

            Vector2 objPos = rb.worldCenterOfMass;
            Vector2 dir = (objPos - worldPos).normalized;
            float dist = Vector2.Distance(objPos, worldPos);
            float falloff = 1f - Mathf.Clamp01(dist / radiusWorld);
            Vector2 force = dir * forceStrength * falloff;
            rb.AddForce(force, ForceMode2D.Impulse);
        }
    }
}
