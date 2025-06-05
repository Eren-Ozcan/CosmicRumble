// DestructiblePlanet2.cs
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(PolygonCollider2D))]
public class DestructiblePlanet2 : MonoBehaviour
{
    private SpriteRenderer sr;
    private Texture2D runtimeTex;
    private PolygonCollider2D polyCollider;  // Bu objenin kendi PolygonCollider2D’si
    private float ppu;                        // Pixels Per Unit (sprite’dan alınacak)

    [Header("Merkez Koruma Ayarları")]
    [Tooltip("Dünya birimi cinsinden: İç maskenin (siyah daire) yarıçapı. " +
             "Bu değer, InnerMask (child)’ın CircleCollider2D.radius ile aynı olmalı.")]
    public float unbreakableInnerRadius = 1f;

    [Header("Kırılabilir Alan Collider’ı")]
    [Tooltip("Jagged kırmızı ring alanının sınırlarını belirleyen PolygonCollider2D.")]
    public PolygonCollider2D destructibleAreaCollider;

    [Header("İç Maske Collider’ı")]
    [Tooltip("İçteki korumalı daire: CircleCollider2D, isTrigger = true olmalı, radius = unbreakableInnerRadius.")]
    public CircleCollider2D innerMaskCollider;

    void Start()
    {
        // 1) SpriteRenderer ve orijinal sprite bilgisi
        sr = GetComponent<SpriteRenderer>();
        Sprite baseSprite = sr.sprite;
        Texture2D orig = baseSprite.texture;
        int w = orig.width, h = orig.height;

        // 2) Yeni runtime Texture oluşturup pikselleri kopyala
        runtimeTex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        runtimeTex.SetPixels(orig.GetPixels());
        runtimeTex.Apply();

        // 3) Oluşturulan Texture’dan yeni bir Sprite yarat ve SpriteRenderer’a ata
        Sprite newSprite = Sprite.Create(
            runtimeTex,
            new Rect(0, 0, w, h),
            new Vector2(0.5f, 0.5f),
            baseSprite.pixelsPerUnit
        );
        sr.sprite = newSprite;

        // 4) Pixels Per Unit değerini kaydet
        ppu = baseSprite.pixelsPerUnit;

        // 5) Mevcut PolygonCollider2D’yi (kendi collider’ını) referans al ve ilk kez oluştur
        polyCollider = GetComponent<PolygonCollider2D>();
        RebuildCollider();
    }

    /// <summary>
    /// Dışarıdan patlama tetiklemek için kullanacağınız metot.
    /// Önce kuvvet uygular, sonra görseli parçalar.
    /// </summary>
    /// <param name="worldPos">Patlama merkezinin dünya koordinatı</param>
    /// <param name="radiusWorld">Patlama yarıçapı (dünya birimi)</param>
    /// <param name="forceStrength">Patlama kuvveti (impulse büyüklüğü)</param>
    public void ExplodeWithForce(Vector2 worldPos, float radiusWorld, float forceStrength)
    {
        ApplyExplosionForce(worldPos, radiusWorld, forceStrength);
        ExplodeVisual(worldPos, radiusWorld);
    }

    /// <summary>
    /// Piksel bazlı silme: 
    ///  - İç maskede (içe korunan daire) kalan pikseller silinmez. 
    ///  - Jagged ring collider dışında kalan pikseller silinmez. 
    ///  - Geri kalan pikseller silinir.
    /// </summary>
    private void ExplodeVisual(Vector2 worldPos, float radiusWorld)
    {
        // 1) World konumunu sprite local koordinatına dönüştür
        Vector2 local = transform.InverseTransformPoint(worldPos);

        // 2) Sprite pivot ve PPU bilgilerini al
        Vector2 pivot = sr.sprite.pivot;
        int centerX = Mathf.FloorToInt(local.x * ppu + pivot.x);
        int centerY = Mathf.FloorToInt(local.y * ppu + pivot.y);

        // 3) Patlama yarıçapını pixel cinsine çevir
        int radPx = Mathf.CeilToInt(radiusWorld * ppu);

        // 4) İç maskenin yarıçapını pixel cinsine çevir
        int innerPx = Mathf.CeilToInt(unbreakableInnerRadius * ppu);

        // 5) Tüm potansiyel etki alanındaki pikselleri döngüyle kontrol et
        for (int dy = -radPx; dy <= radPx; dy++)
        {
            for (int dx = -radPx; dx <= radPx; dx++)
            {
                if (dx * dx + dy * dy <= radPx * radPx)
                {
                    int tx = centerX + dx;
                    int ty = centerY + dy;

                    // Piksel koordinatlarının texture sınırlarında olup olmadığını kontrol et
                    if (tx < 0 || tx >= runtimeTex.width || ty < 0 || ty >= runtimeTex.height)
                        continue;

                    // --- A) Inner Mask Kontrolü ---
                    // Bu pikselin dünya konumunu bul
                    float worldX = (tx - pivot.x) / ppu;
                    float worldY = (ty - pivot.y) / ppu;
                    Vector2 pixelWorldPos = transform.TransformPoint(new Vector2(worldX, worldY));

                    // Eğer bu nokta innerMaskCollider içinde ise, silme
                    if (innerMaskCollider != null && innerMaskCollider.OverlapPoint(pixelWorldPos))
                        continue;

                    // --- B) Kırılabilir Erik Alan Kontrolü ---
                    // Bu piksel, jagged ring’in sınırlarını belirleyen PolygonCollider2D içinde mi?
                    if (destructibleAreaCollider != null && !destructibleAreaCollider.OverlapPoint(pixelWorldPos))
                        continue;

                    // --- C) Pikseli sil ---
                    Color pixelColor = runtimeTex.GetPixel(tx, ty);
                    if (pixelColor.a != 0f)
                    {
                        pixelColor.a = 0f;
                        runtimeTex.SetPixel(tx, ty, pixelColor);
                    }
                }
            }
        }

        // 6) Değişiklikleri texture’a uygula
        runtimeTex.Apply();

        // 7) Collider’ı yeniden oluştur (yeni maskeye göre)
        RebuildCollider();
    }

    /// <summary>
    /// PolygonCollider2D’yi (runtime sprite’taki saydamlık değişikliklerine göre) sil ve yeniden ekle.
    /// </summary>
    private void RebuildCollider()
    {
        if (polyCollider != null)
            Destroy(polyCollider);

        polyCollider = gameObject.AddComponent<PolygonCollider2D>();
        // Unity otomatik olarak, sprite’taki alpha=0 pikselleri yok sayarak yeni patika çizecek
    }

    /// <summary>
    /// Çevredeki Rigidbody2D’lere patlama kuvveti uygular.
    /// </summary>
    private void ApplyExplosionForce(Vector2 worldPos, float radiusWorld, float forceStrength)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(worldPos, radiusWorld);
        foreach (Collider2D hit in hits)
        {
            Rigidbody2D rb2 = hit.attachedRigidbody;
            if (rb2 == null) continue;

            Vector2 objPos = rb2.worldCenterOfMass;
            Vector2 dir = (objPos - worldPos).normalized;
            float dist = Vector2.Distance(objPos, worldPos);
            float falloff = 1f - Mathf.Clamp01(dist / radiusWorld);
            Vector2 force = dir * forceStrength * falloff;

            rb2.AddForce(force, ForceMode2D.Impulse);
        }
    }
}
