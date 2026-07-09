// Assets/Scripts/UI/UiKit.cs
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Programatik UI'lar için paylaşılan görsel stil araçları (mobil görünüm yenilemesi).
/// - <see cref="Round"/>: runtime'da üretilen, antialias'lı, 9-slice yuvarlatılmış köşe sprite'ını
///   bir Image'a uygular — projede hiç UI sprite asset'i olmadığı için köşeler kod ile üretilir.
/// - <see cref="Shadow"/>: yumuşak alt gölge (uGUI Shadow efekti).
/// - <see cref="Stroke"/>: ince açık kontur (glass kenar parlaması), <see cref="Gradient"/>: dikey
///   vertex gradyanı, <see cref="Press"/>: dokunuşta küçülme, <see cref="Pop"/>: panel açılış
///   animasyonu, <see cref="CloseButton"/>: mobil oyun standardı köşe X butonu.
/// Tüm ana menü + panel UI'ları bu stili kullanır.
/// </summary>
public static class UiKit
{
    const int   TexSize      = 64;
    const int   CornerRadius = 18;

    static Sprite _rounded;

    /// <summary>9-slice kenarlıklı, antialias'lı yuvarlatılmış dikdörtgen sprite (lazy, cache'li).</summary>
    public static Sprite RoundedSprite
    {
        get
        {
            if (_rounded == null) _rounded = BuildRoundedSprite(TexSize, CornerRadius);
            return _rounded;
        }
    }

    /// <summary>
    /// Image'ı yuvarlatılmış köşeli hale getirir. <paramref name="cornerScale"/> büyüdükçe köşe
    /// yarıçapı KÜÇÜLÜR (pixelsPerUnitMultiplier) — küçük öğelerde (satırlar) 1.5–2 kullan.
    /// </summary>
    public static void Round(Image img, float cornerScale = 1f)
    {
        img.sprite = RoundedSprite;
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = cornerScale;
    }

    /// <summary>Yumuşak alt gölge — kartlar ve butonlara derinlik verir.</summary>
    public static void Shadow(GameObject go, float distance = 5f, float alpha = 0.45f)
    {
        var sh = go.AddComponent<UnityEngine.UI.Shadow>();
        sh.effectColor    = new Color(0f, 0f, 0f, alpha);
        sh.effectDistance = new Vector2(0f, -distance);
    }

    /// <summary>Butonun basılı/hover renklerini normal renginden türetir (tutarlı geri bildirim).</summary>
    public static ColorBlock ButtonColors(Color normal)
    {
        Color hover = Color.Lerp(normal, Color.white, 0.18f);
        Color press = new Color(normal.r * 0.65f, normal.g * 0.65f, normal.b * 0.65f, normal.a);
        return new ColorBlock
        {
            normalColor      = normal,
            highlightedColor = hover,
            pressedColor     = press,
            selectedColor    = hover,
            disabledColor    = new Color(normal.r, normal.g, normal.b, 0.35f),
            colorMultiplier  = 1f,
            fadeDuration     = 0.08f
        };
    }

    static Sprite _circle;

    /// <summary>Antialias'lı içi dolu daire sprite (dekoratif gezegen/ay, ikon zeminleri).</summary>
    public static Sprite CircleSprite
    {
        get
        {
            if (_circle == null) _circle = BuildCircleSprite(128);
            return _circle;
        }
    }

    static Sprite _glow;

    /// <summary>Merkezden kenara yumuşakça solan radyal glow sprite (nebula/ışık lekeleri).</summary>
    public static Sprite GlowSprite
    {
        get
        {
            if (_glow == null) _glow = BuildGlowSprite(128);
            return _glow;
        }
    }

    static Sprite BuildGlowSprite(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode   = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            hideFlags  = HideFlags.HideAndDontSave
        };
        var pixels = new Color32[size * size];
        float r = size * 0.5f;
        Vector2 c = new Vector2(r, r);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float t = Mathf.Clamp01(Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), c) / r);
                float a = (1f - t) * (1f - t); // kare falloff — merkez dolgun, kenar tamamen şeffaf
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        }
        tex.SetPixels32(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    static Sprite BuildCircleSprite(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode   = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            hideFlags  = HideFlags.HideAndDontSave
        };
        var pixels = new Color32[size * size];
        float r = size * 0.5f - 1f;
        Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), c) - r;
                byte a = (byte)(Mathf.Clamp01(0.5f - d) * 255f);
                pixels[y * size + x] = new Color32(255, 255, 255, a);
            }
        }
        tex.SetPixels32(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    static Sprite _roundedOutline;

    /// <summary>Yalnızca kenar çizgisi olan (içi boş) yuvarlatılmış dikdörtgen — glass kontur.</summary>
    public static Sprite RoundedOutlineSprite
    {
        get
        {
            if (_roundedOutline == null) _roundedOutline = BuildRoundedOutlineSprite(TexSize, CornerRadius, 2f);
            return _roundedOutline;
        }
    }

    /// <summary>
    /// Karta/butona ince açık kontur ekler (üst kenarda cam parlaması hissi).
    /// cornerScale, kartın kendi <see cref="Round"/> çağrısıyla aynı verilmeli.
    /// </summary>
    public static void Stroke(GameObject go, Color color, float cornerScale = 1f)
    {
        var strokeGO = new GameObject("Stroke");
        strokeGO.transform.SetParent(go.transform, false);
        var img = strokeGO.AddComponent<Image>();
        img.sprite = RoundedOutlineSprite;
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = cornerScale;
        img.color = color;
        img.raycastTarget = false;
        var rt = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    /// <summary>Grafiğe dikey iki-renk gradyan uygular (üst → alt). Birincil yüzeyler için.</summary>
    public static void Gradient(Graphic graphic, Color top, Color bottom)
    {
        var g = graphic.gameObject.AddComponent<UiVerticalGradient>();
        g.top = top;
        g.bottom = bottom;
        graphic.SetVerticesDirty();
    }

    /// <summary>Dokunuşta hafif küçülme mikro-etkileşimi (mobil "canlı" his).</summary>
    public static void Press(GameObject go, float scale = 0.94f)
    {
        var fx = go.AddComponent<UiPressScale>();
        fx.pressedScale = scale;
    }

    /// <summary>İmleç butonun üzerine geldiğinde `ui_button_hover` SFX'ini çalar (mouse/masaüstü test).</summary>
    public static void Hover(GameObject go)
    {
        go.AddComponent<UiHoverSound>();
    }

    /// <summary>Panel/kart açılışında ölçek+alfa pop animasyonu (her SetActive(true)'da oynar).</summary>
    public static void Pop(GameObject go, float duration = 0.16f)
    {
        var fx = go.AddComponent<UiPopIn>();
        fx.duration = duration;
    }

    /// <summary>Sürekli, çok hafif nefes alma ölçeği — tek birincil eylem butonu için (OYNA).</summary>
    public static void Pulse(GameObject go, float amount = 0.018f, float speed = 2.2f)
    {
        var fx = go.AddComponent<UiPulse>();
        fx.amount = amount;
        fx.speed  = speed;
    }

    /// <summary>
    /// Brawl Stars imzası: grafiği hafif paralelkenara yatırır (yatay shear).
    /// Bir butonun kenar + yüz Image'larının İKİSİNE de aynı miktarla uygulanmalı.
    /// </summary>
    public static void Skew(Graphic graphic, float shear = 0.10f)
    {
        var s = graphic.gameObject.AddComponent<UiSkew>();
        s.shear = shear;
        graphic.SetVerticesDirty();
    }

    /// <summary>
    /// Brawl Stars tarzı buton yazısı: beyaz dolgu + kalın koyu kontur, DÜZ (italik değil —
    /// eğiklik plakada, yazıda değil). Fake bold/italik kapatılır: varsayılan font (Titan One)
    /// zaten tek parça kalın display font, TMP'nin yapay kalınlaştırması bozuyor.
    /// </summary>
    public static void BrawlText(TMPro.TMP_Text txt)
    {
        txt.fontStyle    = TMPro.FontStyles.Normal;
        txt.outlineWidth = 0.24f;
        txt.outlineColor = new Color32(24, 20, 36, 255);
    }

    static readonly Color CloseRed    = new Color(0.86f, 0.22f, 0.25f, 1f);

    /// <summary>
    /// Mobil oyun standardı kapatma butonu: kartın sağ-üst köşesinden hafif taşan kırmızı
    /// yuvarlatılmış kare, beyaz X. Kartın child'ı olarak eklenir.
    /// </summary>
    public static void CloseButton(GameObject card, UnityEngine.Events.UnityAction onClose, float size = 58f)
    {
        var go = new GameObject("btn_close");
        go.transform.SetParent(card.transform, false);
        var img = go.AddComponent<Image>();
        img.color = CloseRed;
        Round(img, 1.4f);
        Shadow(go, 3f, 0.4f);
        Stroke(go, new Color(1f, 1f, 1f, 0.22f), 1.4f);

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.colors = ButtonColors(CloseRed);
        btn.onClick.AddListener(onClose);
        Press(go);
        Hover(go);

        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
        rt.sizeDelta = new Vector2(size, size);
        rt.anchoredPosition = new Vector2(6f, 6f); // köşeden taşsın

        var lblGO = new GameObject("Lbl");
        lblGO.transform.SetParent(go.transform, false);
        var lbl = lblGO.AddComponent<TMPro.TextMeshProUGUI>();
        lbl.text      = "X";
        lbl.fontSize  = size * 0.5f;
        lbl.fontStyle = TMPro.FontStyles.Bold;
        lbl.color     = Color.white;
        lbl.alignment = TMPro.TextAlignmentOptions.Center;
        var lrt = lbl.rectTransform;
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = lrt.offsetMax = Vector2.zero;
    }

    static Sprite BuildRoundedOutlineSprite(int size, int radius, float thickness)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode   = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            hideFlags  = HideFlags.HideAndDontSave
        };
        var pixels = new Color32[size * size];
        Vector2 half = new Vector2(size * 0.5f, size * 0.5f);
        Vector2 box  = half;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2(x + 0.5f - half.x, y + 0.5f - half.y);
                Vector2 q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - box + Vector2.one * radius;
                float outside = new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f)).magnitude;
                float inside  = Mathf.Min(Mathf.Max(q.x, q.y), 0f);
                float d = outside + inside - radius;

                // |d| < thickness bandında opak → içi boş çerçeve
                float band = thickness * 0.5f - Mathf.Abs(d + thickness * 0.5f);
                byte a = (byte)(Mathf.Clamp01(0.5f + band) * 255f);
                pixels[y * size + x] = new Color32(255, 255, 255, a);
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();

        float b = radius + 4;
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f),
                             100f, 0, SpriteMeshType.FullRect, new Vector4(b, b, b, b));
    }

    static Sprite BuildRoundedSprite(int size, int radius)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode   = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            hideFlags  = HideFlags.HideAndDontSave
        };

        var pixels = new Color32[size * size];
        Vector2 half = new Vector2(size * 0.5f, size * 0.5f);
        Vector2 box  = half; // yarı boyut

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Signed distance to rounded box (klasik SDF) → 1px antialias
                Vector2 p = new Vector2(x + 0.5f - half.x, y + 0.5f - half.y);
                Vector2 q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - box + Vector2.one * radius;
                float outside = new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f)).magnitude;
                float inside  = Mathf.Min(Mathf.Max(q.x, q.y), 0f);
                float d = outside + inside - radius;

                byte a = (byte)(Mathf.Clamp01(0.5f - d) * 255f);
                pixels[y * size + x] = new Color32(255, 255, 255, a);
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();

        float b = radius + 4; // 9-slice kenarlığı köşeden biraz geniş
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f),
                             100f, 0, SpriteMeshType.FullRect, new Vector4(b, b, b, b));
    }
}

/// <summary>Dikey iki-renk vertex gradyanı (üstten alta). UiKit.Gradient ile eklenir.</summary>
public class UiVerticalGradient : BaseMeshEffect
{
    public Color top    = Color.white;
    public Color bottom = Color.white;

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive() || vh.currentVertCount == 0) return;

        var vert = new UIVertex();
        float minY = float.MaxValue, maxY = float.MinValue;
        for (int i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref vert, i);
            minY = Mathf.Min(minY, vert.position.y);
            maxY = Mathf.Max(maxY, vert.position.y);
        }
        float range = Mathf.Max(0.0001f, maxY - minY);

        for (int i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref vert, i);
            float t = (vert.position.y - minY) / range;
            vert.color *= Color.Lerp(bottom, top, t);
            vh.SetUIVertex(vert, i);
        }
    }
}

/// <summary>Yatay shear (paralelkenar) mesh efekti. UiKit.Skew ile eklenir.</summary>
public class UiSkew : BaseMeshEffect
{
    public float shear = 0.10f;

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive() || vh.currentVertCount == 0) return;

        float centerY = graphic.rectTransform.rect.center.y;
        var vert = new UIVertex();
        for (int i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref vert, i);
            vert.position.x += (vert.position.y - centerY) * shear;
            vh.SetUIVertex(vert, i);
        }
    }
}

/// <summary>Dokunuşta hafif küçülme; bırakınca geri. UiKit.Press ile eklenir.</summary>
public class UiPressScale : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public float pressedScale = 0.94f;
    Vector3 _baseScale = Vector3.one;
    bool _captured;

    public void OnPointerDown(PointerEventData e)
    {
        if (!_captured) { _baseScale = transform.localScale; _captured = true; }
        transform.localScale = _baseScale * pressedScale;
    }

    public void OnPointerUp(PointerEventData e)   => Restore();
    public void OnPointerExit(PointerEventData e) => Restore();
    void OnDisable()                              => Restore();

    void Restore()
    {
        if (_captured) transform.localScale = _baseScale;
    }
}

/// <summary>Hover'da `ui_button_hover` SFX'i çalar; devre dışı (Selectable.interactable == false) butonlarda sessiz kalır. UiKit.Hover ile eklenir.</summary>
public class UiHoverSound : MonoBehaviour, IPointerEnterHandler
{
    Selectable _selectable;

    void Awake() => _selectable = GetComponent<Selectable>();

    public void OnPointerEnter(PointerEventData e)
    {
        if (_selectable != null && !_selectable.IsInteractable()) return;
        AudioManager.Instance?.PlayHover();
    }
}

/// <summary>Aktifleşince ölçek+alfa pop animasyonu. UiKit.Pop ile eklenir.</summary>
public class UiPopIn : MonoBehaviour
{
    public float duration = 0.16f;
    CanvasGroup _cg;

    void OnEnable()
    {
        // Dikkat: Unity objelerinde `??` kullanılmaz — GetComponent'in "fake null" dönüşünü
        // gerçek null sanmaz ve MissingComponentException'a yol açar. Açık == null şart.
        if (_cg == null)
        {
            _cg = gameObject.GetComponent<CanvasGroup>();
            if (_cg == null) _cg = gameObject.AddComponent<CanvasGroup>();
        }
        StartCoroutine(Animate());
    }

    System.Collections.IEnumerator Animate()
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duration);
            // ease-out-back: hafif taşma ile oturur
            float s = 1f + 1.4f * Mathf.Pow(k - 1f, 3) + 0.4f * Mathf.Pow(k - 1f, 2);
            transform.localScale = Vector3.one * Mathf.LerpUnclamped(0.92f, 1f, s);
            _cg.alpha = k;
            yield return null;
        }
        transform.localScale = Vector3.one;
        _cg.alpha = 1f;
    }
}

/// <summary>Sürekli çok hafif nefes ölçeği (birincil eylem vurgusu). UiKit.Pulse ile eklenir.</summary>
public class UiPulse : MonoBehaviour
{
    public float amount = 0.018f;
    public float speed  = 2.2f;
    Vector3 _baseScale;

    void OnEnable()  => _baseScale = transform.localScale;
    void OnDisable() => transform.localScale = _baseScale;

    void Update()
    {
        float s = 1f + Mathf.Sin(Time.unscaledTime * speed) * amount;
        transform.localScale = _baseScale * s;
    }
}
