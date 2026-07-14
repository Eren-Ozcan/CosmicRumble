// Assets/Scripts/Planet/DestructiblePlanet.cs
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using CosmicRumble.Achievements;

/// <summary>
/// DestructiblePlanet:
/// - SpriteRenderer’daki sprite’ı runtime’da Texture2D’ye kopyalar.
/// - ExplodeWithForce() ile etraftaki rigidbody’lere impulse uygular ve
///   Texture2D’deki pikselleri silerek collider’ı günceller.
/// - minDestructionRadius içindeki pikseller korunur (gezegen çekirdeği).
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(PolygonCollider2D))]
public class DestructiblePlanet : MonoBehaviour
{
    [Header("Core Protection")]
    public float minDestructionRadius = 0.3f;

    [Header("Collider Performance")]
    [Tooltip("Collider'ın alfa hattı bu kadar küçültülmüş bir texture'dan üretilir (görsel etkilenmez, " +
             "yalnızca fizik şekli). 8 => 1280px bir gezegen 160px'ten üretilir; patlama başına maliyeti " +
             "~kare oranında düşürür (ölçüldü: 1280px'te 8x ile patlama başına ~4-7ms, bkz. " +
             "RebuildColliderFromAlpha). Karakter ölçeğinde collider hassasiyeti kaybı fark edilmez.")]
    [Range(1, 12)]
    public int physicsDownsampleFactor = 8;

    private SpriteRenderer sr;
    private Texture2D runtimeTex;
    private Color32[] pixels;   // runtimeTex ile birebir aynalanan, GetPixel/SetPixel yerine dizi üzerinden mutasyona uğrayan buffer
    private PolygonCollider2D poly;
    private float ppu; // pixels per unit

    // Çekirdek dışındaki (yıkılabilir) piksel sayısı — sıfıra inince gezegen "tamamen yok edilmiş" sayılır.
    private int nonCorePixelsRemaining = -1;
    private bool destroyedFired;

    private void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            #if UNITY_EDITOR
            Debug.LogError($"[DestructiblePlanet] {name} üzerinde SpriteRenderer bulunamadı!");
            #endif
            enabled = false;
            return;
        }

        // Orijinal sprite ve texture bilgisi
        Sprite baseSprite = sr.sprite;
        Texture2D orig = baseSprite.texture;
        int w = orig.width, h = orig.height;

        // 1) Runtime Texture oluştur ve pikselleri kopyala — pixels[] buffer'ı runtimeTex ile
        // birebir aynı veriyi tutar; sonraki tüm mutasyonlar (ExplodeVisual) GetPixel/SetPixel
        // yerine bu dizi üzerinden yapılır (bkz. RebuildColliderFromAlpha üstündeki not).
        pixels = orig.GetPixels32();
        runtimeTex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        runtimeTex.SetPixels32(pixels);
        runtimeTex.Apply();

        // 2) Pixels per unit değerini kaydet
        ppu = baseSprite.pixelsPerUnit;

        // 3) Yeni Sprite oluştur ve SpriteRenderer'a ata. generateFallbackPhysicsShape=false:
        // collider artık RebuildColliderFromAlpha() ile ayrı, küçültülmüş bir texture'dan
        // üretiliyor (aşağıda) — tam çözünürlükte otomatik physics shape üretimi (asıl darboğaz,
        // patlama başına ~90-140ms) burada gereksiz hale geldi. Görsel güncelleme artık bu Sprite'ı
        // hiç yeniden oluşturmadan runtimeTex.Apply() ile yapılıyor (bkz. ExplodeVisual).
        sr.sprite = Sprite.Create(
            runtimeTex,
            new Rect(0, 0, w, h),
            new Vector2(0.5f, 0.5f),
            ppu,
            0,
            SpriteMeshType.Tight,
            Vector4.zero,
            false
        );

        // 4) İlk sefer polygon collider oluştur
        poly = GetComponent<PolygonCollider2D>();
        poly.isTrigger = false;  // solid yüzey: karakter üstünde yürür
        RebuildColliderFromAlpha();

        // 5) Çekirdek dışındaki yıkılabilir piksel sayısını hesapla (achievement/quest için)
        nonCorePixelsRemaining = CountNonCorePixels();
    }

    /// <summary>
    /// Patlama geldiğinde çağrılır.
    /// Etraftaki Rigidbody’lere impulse uygular ve görseli parçalar.
    ///
    /// NETWORKED MODDA TAHRİBAT SERVER-AUTHORITATIVE: mermiler her makinede yerel fizikle de
    /// simüle edildiği için bu metod her makinede, makineye özgü temas noktalarıyla çağrılıyordu —
    /// host ve client'ın gezegenleri zamanla birbirinden ayrışıyordu (birinde zemin olan yer
    /// diğerinde delik). Artık yalnızca server'ın çağrısı işlenir; server parametreleri
    /// TurnManager üzerinden ClientRpc ile yayar ve delik HER makinede birebir aynı
    /// pos/yarıçap/kuvvetle açılır (ClientRpc host'ta da çalıştığı için server yerel uygulamayı
    /// ayrıca yapmaz — çift delik olmasın). Offline'da eski doğrudan yol aynen çalışır.
    /// </summary>
    /// <param name="worldPos">Patlama merkezi (dünya koordinatı)</param>
    /// <param name="radiusWorld">Patlama yarıçapı (dünya birimi)</param>
    /// <param name="forceStrength">Patlama kuvveti (Impulse mag)</param>
    public void ExplodeWithForce(Vector2 worldPos, float radiusWorld, float forceStrength)
    {
        if (radiusWorld <= 0f) return;

        var nm = NetworkManager.Singleton;
        bool networked = nm != null && nm.IsListening &&
                         TurnManager.Instance != null && TurnManager.Instance.IsSpawned;
        if (networked)
        {
            if (nm.IsServer)
                TurnManager.BroadcastPlanetExplosion(this, worldPos, radiusWorld, forceStrength);
            // client'ın kendi yerel simülasyonundan gelen çağrılar yok sayılır — uygulama
            // her makinede PlanetExplosionClientRpc → ApplyExplosionNow ile yapılır.
            return;
        }

        ApplyExplosionNow(worldPos, radiusWorld, forceStrength);
    }

    /// <summary>Deliği ve patlama kuvvetini bu makinede gerçekten uygular — offline'da doğrudan,
    /// online'da TurnManager.PlanetExplosionClientRpc tarafından çağrılır.</summary>
    public void ApplyExplosionNow(Vector2 worldPos, float radiusWorld, float forceStrength)
    {
        if (radiusWorld <= 0f) return;
        if (runtimeTex == null) return; // Start() henüz çalışmadıysa (savunma)

        // 1) Patlama kuvvetini etraftaki objelere uygula
        ApplyExplosionForce(worldPos, radiusWorld, forceStrength);

        // 2) Görseli parçala ve collider’i güncelle
        ExplodeVisual(worldPos, radiusWorld);
    }

    // ── Makineler arası stabil kimlik ────────────────────────────────────────
    // Gezegenler sahneye yerleştirilmiş sıradan objeler (NetworkObject yok) — RPC'de referans
    // taşınamaz. Her makine aynı sahneyi yüklediği için isim+konuma göre sıralanmış indeks her
    // makinede aynı gezegeni gösterir. Patlama düşük frekanslı olduğundan her çağrıda taze
    // FindObjectsByType maliyeti kabul edilebilir.

    public static DestructiblePlanet FindByStableIndex(int index)
    {
        var all = GetAllSorted();
        return (index >= 0 && index < all.Count) ? all[index] : null;
    }

    public int StableIndex => GetAllSorted().IndexOf(this);

    private static List<DestructiblePlanet> GetAllSorted()
    {
        var arr = FindObjectsByType<DestructiblePlanet>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        var list = new List<DestructiblePlanet>(arr);
        list.Sort((a, b) =>
        {
            int n = string.CompareOrdinal(a.name, b.name);
            if (n != 0) return n;
            // aynı isimli birden çok gezegen olabilir — statik sahne konumuna göre kır
            int c = a.transform.position.x.CompareTo(b.transform.position.x);
            return c != 0 ? c : a.transform.position.y.CompareTo(b.transform.position.y);
        });
        return list;
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

        // Daire içindeki piksellerin alpha'sını sıfırla — GetPixel/SetPixel (piksel başına native
        // çağrı, ölçüldü: ~60-150ms/patlama) yerine doğrudan dizi indeksleme kullanılır; texture'a
        // tek seferde SetPixels32 ile geri yazılır (bkz. döngü sonrası).
        for (int y = -radPx; y <= radPx; y++)
        {
            int ty = py + y;
            if (ty < 0 || ty >= h) continue;

            int xLimit = Mathf.FloorToInt(Mathf.Sqrt(radPx * radPx - y * y));
            int rowBase = ty * w;
            for (int x = -xLimit; x <= xLimit; x++)
            {
                int tx = px + x;
                if (tx < 0 || tx >= w) continue;

                // Protect the planet core: skip pixels within minDestructionRadius
                // of the planet center (pivot). Distances are in local sprite units.
                float dxLocal = (tx - pivot.x) / ppu;
                float dyLocal = (ty - pivot.y) / ppu;
                if (dxLocal * dxLocal + dyLocal * dyLocal < minDestructionRadius * minDestructionRadius)
                    continue;

                int idx = rowBase + tx;
                if (pixels[idx].a != 0)
                {
                    Color32 c = pixels[idx];
                    c.a = 0;
                    pixels[idx] = c;
                    nonCorePixelsRemaining--;
                }
            }
        }

        runtimeTex.SetPixels32(pixels);
        runtimeTex.Apply();

        if (!destroyedFired && nonCorePixelsRemaining <= 0)
        {
            destroyedFired = true;
            AchievementEvents.FirePlanetDestroyed();
            AudioManager.Instance?.PlaySfx("planet_destroyed");
        }

        // Not: sr.sprite BURADA yeniden oluşturulmuyor — runtimeTex.Apply() zaten aynı Sprite'ın
        // referans aldığı GPU texture'ı günceller, görsel değişim için yeni bir Sprite şart değil.
        // Eskiden burada her patlamada Sprite.Create(generateFallbackPhysicsShape:true) çağrılıyordu;
        // bu API tüm texture'ın alfa hattını yeniden tarıyordu (ölçüldü: ~90-140ms, patlama
        // yarıçapından bağımsız sabit maliyet) — collider artık RebuildColliderFromAlpha() ile ayrı,
        // küçültülmüş bir texture'dan üretildiği için bu maliyet tamamen ortadan kalktı.
        RebuildColliderFromAlpha();
    }

    /// <summary>Çekirdek yarıçapı (minDestructionRadius) dışında kalan, hâlâ opak olan piksel sayısını hesaplar.</summary>
    private int CountNonCorePixels()
    {
        int w = runtimeTex.width;
        int h = runtimeTex.height;
        Vector2 pivot = sr.sprite.pivot;

        int count = 0;
        for (int y = 0; y < h; y++)
        {
            float dyLocal = (y - pivot.y) / ppu;
            int rowBase = y * w;
            for (int x = 0; x < w; x++)
            {
                float dxLocal = (x - pivot.x) / ppu;
                if (dxLocal * dxLocal + dyLocal * dyLocal < minDestructionRadius * minDestructionRadius)
                    continue;

                if (pixels[rowBase + x].a != 0) count++;
            }
        }
        return count;
    }

    /// <summary>
    /// Collider'ı, tam çözünürlüklü runtimeTex yerine physicsDownsampleFactor kadar küçültülmüş
    /// tek seferlik bir yardımcı texture'dan üretir. Unity'nin generateFallbackPhysicsShape alfa-hattı
    /// taraması maliyeti texture piksel sayısıyla orantılı olduğundan (ölçüldü: 1280x1280'de patlama
    /// yarıçapından bağımsız ~90-140ms), 8x küçültme (160x160) bunu ~90-140ms'den ~4-7ms'ye düşürür
    /// (ölçüldü). Küçültülmüş sprite'ın pixelsPerUnit'i de aynı oranda küçültülüyor (ppu/factor)
    /// — bu sayede GetPhysicsShape'in döndürdüğü noktalar otomatik olarak tam çözünürlüklü sprite'ın
    /// üreteceğiyle AYNI local-unit uzayına düşer, elle ölçekleme gerekmez. Görsel kaliteyi etkilemez
    /// (sr.sprite/runtimeTex bu metoda hiç dokunulmaz) — yalnızca collider'ın köşe hassasiyeti
    /// düşer, ki bu karakterin metrelerce büyük gezegen üzerindeki fiziği için fark edilmez.
    /// </summary>
    private void RebuildColliderFromAlpha()
    {
        int factor = Mathf.Max(1, physicsDownsampleFactor);
        int w = runtimeTex.width;
        int h = runtimeTex.height;
        int smallW = Mathf.Max(1, w / factor);
        int smallH = Mathf.Max(1, h / factor);

        var smallPixels = new Color32[smallW * smallH];
        for (int sy = 0; sy < smallH; sy++)
        {
            int sourceY = Mathf.Min(sy * factor, h - 1);
            int sourceRowBase = sourceY * w;
            int destRowBase = sy * smallW;
            for (int sx = 0; sx < smallW; sx++)
            {
                int sourceX = Mathf.Min(sx * factor, w - 1);
                smallPixels[destRowBase + sx] = pixels[sourceRowBase + sourceX];
            }
        }

        var physTex = new Texture2D(smallW, smallH, TextureFormat.RGBA32, false);
        physTex.SetPixels32(smallPixels);
        physTex.Apply();

        Sprite physSprite = Sprite.Create(
            physTex,
            new Rect(0, 0, smallW, smallH),
            new Vector2(0.5f, 0.5f),
            ppu / factor,
            0,
            SpriteMeshType.Tight,
            Vector4.zero,
            true // generateFallbackPhysicsShape — küçük texture üzerinde, artık ucuz
        );

        if (poly == null) poly = GetComponent<PolygonCollider2D>();

        int pathCount = physSprite.GetPhysicsShapeCount();
        poly.pathCount = pathCount;

        var path = new System.Collections.Generic.List<Vector2>();
        for (int i = 0; i < pathCount; i++)
        {
            path.Clear();
            physSprite.GetPhysicsShape(i, path);
            poly.SetPath(i, path);
        }

        Destroy(physSprite);
        Destroy(physTex);
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
