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
             "yalnızca fizik şekli). 8 => 1280px bir gezegen 160px'ten üretilir; Sprite.Create'in tam " +
             "çözünürlükte taraması yerine küçük texture üzerinde çalışır (bkz. RebuildColliderFromAlpha). " +
             "Karakter ölçeğinde collider hassasiyeti kaybı fark edilmez.")]
    [Range(1, 12)]
    public int physicsDownsampleFactor = 1;

    [Tooltip("Collider bir pikseli 'katı zemin' saymak için gereken minimum alfa (1-254). f16579c " +
             "'never smaller than visual' garantisi için bunu en toleranslı haline (fiilen != 0) " +
             "sabitlemişti — ama planet_with_hole_1280.png gibi bazı sprite'ların alfa kanalında " +
             "görünmez (render'da hiç görünmeyen) ama sıfırdan farklı geniş bir 'çöp halka' var " +
             "(ölçüldü: pikselin ~%6'sı alfa 1-63 arası, RGB'si renkli ama render'da görünmez). " +
             "Bu texture'ın histogramı BİMODAL: alfa ya 0, ya 1-63 (çöp), ya da neredeyse dosdoğru " +
             "255 — 64-254 arası pratikte boş (~%0.05). Yani 100-200 arası HERHANGİ bir eşik çöp " +
             "halkayı temiz şekilde dışlar, gerçek/görünür içeriği kesmez. 128 bu aralıkta güvenli " +
             "bir varsayılan. Yeni bir gezegen sprite'ı eklerken alfa histogramını kontrol etmeden " +
             "bu değeri değiştirme.")]
    [Range(1, 254)]
    public int groundAlphaThreshold = 128;

    private SpriteRenderer sr;
    private Texture2D runtimeTex;
    private Color32[] pixels;   // runtimeTex ile birebir aynalanan, GetPixel/SetPixel yerine dizi üzerinden mutasyona uğrayan buffer
    private PolygonCollider2D poly;
    private float ppu; // pixels per unit
    private Texture2D _physTexCache; // RebuildColliderFromAlpha'nın tekrar kullandığı downsample texture'ı

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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // 2026-08-01 vakası: bu değerler günlerce beklenenden farklıydı (Unity'nin prefab asset
        // cache'i, dosya dışarıdan düzenlendiğinde bunu yansıtmıyordu) ve hiçbir yerde görünür
        // olmadığı için fark edilemedi. Şimdi her Start()'ta konsola basılıyor — "değer ne
        // olmalıydı, oyun ne kullanıyor" sorusu artık bir Inspector/prefab arkeolojisi değil,
        // tek satırlık bir konsol kontrolü.
        Debug.Log($"[DestructiblePlanet] {name} Start(): physicsDownsampleFactor={physicsDownsampleFactor}, " +
                  $"groundAlphaThreshold={groundAlphaThreshold}, minDestructionRadius={minDestructionRadius}", this);
#endif

        // 4) İlk sefer polygon collider oluştur
        poly = GetComponent<PolygonCollider2D>();
        poly.isTrigger = false;  // solid yüzey: karakter üstünde yürür
        RebuildColliderFromAlpha();

        // 5) Çekirdek dışındaki yıkılabilir piksel sayısını hesapla (achievement/quest için)
        nonCorePixelsRemaining = CountNonCorePixels();
    }

    private void OnDestroy()
    {
        if (_physTexCache != null) Destroy(_physTexCache);
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
    /// yarıçapından bağımsız ~90-140ms), 8x küçültme (160x160) bunu belirgin şekilde düşürür.
    /// Küçültülmüş sprite'ın pixelsPerUnit'i de aynı oranda küçültülüyor (ppu/factor)
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

        // Blok içinde TEK pikseli örnekleyip (nearest-neighbor) o pikseli şeffaf yakalarsa,
        // downsample edilmiş collider görsel siluetin İÇİNE çöker — karakter fiziksel olarak
        // görsel yüzeyin biraz üstünde durur ("yere tam değmiyor"). Bunun yerine blok içindeki
        // pikselleri OR'layıp (herhangi biri opak ise sonuç opak) collider'ı asla görselden
        // küçük üretme: en kötü ihtimalle collider görselden biraz büyük olur (karakter yüzeye
        // tam basar), asla küçük olup boşlukta durmaz.
        var smallPixels = new Color32[smallW * smallH];
        for (int sy = 0; sy < smallH; sy++)
        {
            int destRowBase = sy * smallW;
            int srcYStart = sy * factor;
            int srcYEnd = Mathf.Min(srcYStart + factor, h);
            for (int sx = 0; sx < smallW; sx++)
            {
                int srcXStart = sx * factor;
                int srcXEnd = Mathf.Min(srcXStart + factor, w);

                byte maxAlpha = 0;
                for (int yy = srcYStart; yy < srcYEnd && maxAlpha == 0; yy++)
                {
                    int rowBase = yy * w;
                    for (int xx = srcXStart; xx < srcXEnd; xx++)
                    {
                        if (pixels[rowBase + xx].a >= groundAlphaThreshold)
                        {
                            maxAlpha = 255;
                            break;
                        }
                    }
                }

                smallPixels[destRowBase + sx] = new Color32(255, 255, 255, maxAlpha);
            }
        }

        // Texture2D her patlamada yeniden Instantiate/Destroy edilmek yerine boyutu sabit
        // olduğundan (factor runtime'da değişmiyor) tek sefer oluşturulup yeniden kullanılır —
        // GC baskısını azaltır.
        if (_physTexCache == null || _physTexCache.width != smallW || _physTexCache.height != smallH)
        {
            if (_physTexCache != null) Destroy(_physTexCache);
            _physTexCache = new Texture2D(smallW, smallH, TextureFormat.RGBA32, false);
        }
        var physTex = _physTexCache;
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

        // physTex artık _physTexCache üzerinden yeniden kullanılıyor — burada Destroy EDİLMEZ
        // (aksi halde bir sonraki patlamada boş bir texture referansı kalır).
        Destroy(physSprite);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        ValidateColliderAgainstAlpha();
#endif
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    /// <summary>
    /// 2026-08-01 vakası: collider görsel yüzeyden gözle görülür şekilde uzak duruyordu
    /// ("yeşil çizgi gezegen yüzeyine uzak") — bir kere f16579c ile çözülmüş, sonra farkında
    /// olmadan (groundAlphaThreshold yanlış kalibre edilerek + Unity'nin prefab asset cache'i
    /// dıştan yapılan dosya düzenlemesini yansıtmayarak) geri gelmişti; günlerce fark edilmedi
    /// çünkü hiçbir şey bunu bildirmiyordu. Bu kontrol, üretilen her collider köşesinin gerçekten
    /// opak bir pikselin (>= groundAlphaThreshold) makul bir mesafesinde olduğunu doğrular ve
    /// değilse konsola AÇIK bir uyarı basar — sorunun tekrar sessizce oturmasını engeller.
    /// </summary>
    private void ValidateColliderAgainstAlpha()
    {
        if (poly == null || runtimeTex == null || pixels == null) return;

        int w = runtimeTex.width, h = runtimeTex.height;
        Vector2 pivot = sr.sprite.pivot;
        int factor = Mathf.Max(1, physicsDownsampleFactor);
        // Beklenen en kötü sapma: bir downsample bloğu (factor px) + tracing/rounding payı.
        // Taban değer (10px) sağlıklı bir collider'da bile oluşabilecek küçük yuvarlama
        // sapmalarını (ölçüldü: factor=1'de ~4px) yanlış alarma çevirmemek için var — gerçek
        // vakalarda sapma 15-40+ piksel mertebesindeydi, bu eşik onları hâlâ yakalar.
        int allowedGapPx = Mathf.Max(factor * 4, 10);
        int searchLimitPx = allowedGapPx * 2;

        int worstGapPx = -1;
        Vector2 worstLocal = default;

        for (int p = 0; p < poly.pathCount; p++)
        {
            var path = poly.GetPath(p);
            // Tüm noktaları taramak yerine örnekle — köşe sayısı yüzlerce olabilir, bu sadece
            // hızlı bir "bir şeyler ters gitti mi" duman testi.
            int step = Mathf.Max(1, path.Length / 12);
            for (int i = 0; i < path.Length; i += step)
            {
                Vector2 local = path[i];
                int px = Mathf.RoundToInt(local.x * ppu + pivot.x);
                int py = Mathf.RoundToInt(local.y * ppu + pivot.y);

                int gap = NearestOpaquePixelDistance(px, py, w, h, searchLimitPx);
                if (gap > worstGapPx) { worstGapPx = gap; worstLocal = local; }
            }
        }

        if (worstGapPx > allowedGapPx)
        {
            float worldGap = worstGapPx / ppu;
            Debug.LogWarning(
                $"[DestructiblePlanet] {name}: collider en yakın opak pikselden {worstGapPx}px " +
                $"(~{worldGap:F2} birim, local={worstLocal}) uzakta — izin verilen ~{allowedGapPx}px. " +
                $"Karakterler görsel yüzeyin üstünde/uzağında yürüyor olabilir. Muhtemel nedenler: " +
                $"groundAlphaThreshold ({groundAlphaThreshold}) bu sprite için çok yüksek/düşük, VEYA " +
                $"prefab/script değişikliği Unity'nin asset cache'ine yansımamış olabilir (bkz. " +
                $"AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate) + sahneyi yeniden aç).",
                this);
        }
    }

    /// <summary>(px,py) merkezli genişleyen kare taramayla en yakın opak (alpha>=groundAlphaThreshold)
    /// pikselin Chebyshev mesafesini döner; searchLimitPx içinde bulunamazsa searchLimitPx döner.</summary>
    private int NearestOpaquePixelDistance(int px, int py, int w, int h, int searchLimitPx)
    {
        for (int radius = 0; radius <= searchLimitPx; radius++)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                int yy = py + dy;
                if (yy < 0 || yy >= h) continue;
                int rowBase = yy * w;
                for (int dx = -radius; dx <= radius; dx++)
                {
                    if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != radius) continue; // yalnız halka
                    int xx = px + dx;
                    if (xx < 0 || xx >= w) continue;
                    if (pixels[rowBase + xx].a >= groundAlphaThreshold) return radius;
                }
            }
        }
        return searchLimitPx;
    }
#endif

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
