using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CosmicRumble.Economy.IAP;

/// <summary>
/// Ana menüdeki "SHOP" butonu → Gem satın alma paneli. IAPManager.GemPacks'teki her paket için
/// bir satır: paket adı, verilecek Gem miktarı, mağazadan gelen fiyat (henüz mağaza bağlanmadıysa
/// "--"), ve bir Buy butonu. Programatik Canvas oluşturur, MenuScene'e boş GO ekleyip scripti
/// yapıştır.
/// </summary>
public class ShopPanelUI : MonoBehaviour
{
    public static ShopPanelUI Instance { get; private set; }

    static readonly Color BgColor      = new Color(0.051f, 0.051f, 0.102f, 0.97f);
    static readonly Color CardBg       = new Color(0.09f,  0.09f,  0.18f,  1f);
    static readonly Color RowBg        = new Color(0.11f,  0.11f,  0.20f,  1f);
    static readonly Color PrimaryBtn   = new Color(0.60f,  0.85f,  1.00f,  1f);
    static readonly Color PrimaryHover = new Color(0.72f,  0.92f,  1.00f,  1f);
    static readonly Color GemColor     = new Color(0.60f,  0.85f,  1.00f,  1f);
    static readonly Color TextSec      = new Color(0.533f, 0.533f, 0.667f, 1f);

    GameObject _panelRoot;
    TextMeshProUGUI[] _priceTexts;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        BuildUI();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ════════════════════════════════════════════════════════════════════
    //  PUBLIC API
    // ════════════════════════════════════════════════════════════════════

    public void Show()
    {
        _panelRoot.SetActive(true);
        StartCoroutine(RefreshPricesUntilReady());
    }

    public void Hide() => _panelRoot.SetActive(false);

    /// <summary>IAPManager fiyatları mağazadan asenkron çeker; hazır olana kadar birkaç kez yeniler.</summary>
    IEnumerator RefreshPricesUntilReady()
    {
        for (int i = 0; i < 20 && _panelRoot.activeSelf; i++)
        {
            RefreshPrices();
            if (IAPManager.Instance != null && IAPManager.Instance.IsInitialized) yield break;
            yield return new WaitForSecondsRealtime(0.5f);
        }
    }

    void RefreshPrices()
    {
        var packs = IAPManager.GemPacks;
        for (int i = 0; i < packs.Length && i < _priceTexts.Length; i++)
            _priceTexts[i].text = IAPManager.Instance != null
                ? IAPManager.Instance.GetLocalizedPrice(packs[i].productId)
                : "--";
    }

    // ════════════════════════════════════════════════════════════════════
    //  UI BUILD
    // ════════════════════════════════════════════════════════════════════

    void BuildUI()
    {
        var canvasGO = new GameObject("ShopCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 46;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        _panelRoot = new GameObject("ShopPanel");
        _panelRoot.transform.SetParent(canvasGO.transform, false);
        var overlay = _panelRoot.AddComponent<Image>();
        overlay.color = new Color(0, 0, 0, 0.65f);
        StretchFull(overlay.rectTransform);

        var card = MakeCard(_panelRoot, "Card", new Vector2(0.5f, 0.5f), new Vector2(560, 520));

        MakeText(card, "Title", "GEM SHOP", 28, new Vector2(0.5f, 0.93f), new Vector2(500, 46), Color.white);

        MakeSmallButton(card, "btn_close", "X CLOSE",
            new Vector2(0.85f, 0.93f), new Vector2(110, 34), Hide);

        var packs = IAPManager.GemPacks;
        _priceTexts = new TextMeshProUGUI[packs.Length];

        float top = 0.80f;
        float step = 0.145f;
        for (int i = 0; i < packs.Length; i++)
        {
            var pack = packs[i];
            var row = MakeCard(card, $"Row_{pack.productId}", new Vector2(0.5f, top - step * i), new Vector2(500, 84));
            row.GetComponent<Image>().color = RowBg;

            MakeText(row, "Name", pack.displayName, 16, new Vector2(0.30f, 0.65f), new Vector2(280, 24), Color.white);
            MakeText(row, "Amount", $"{pack.gemAmount} Gem", 18, new Vector2(0.30f, 0.30f), new Vector2(280, 26), GemColor);

            _priceTexts[i] = MakeText(row, "Price", "--", 14, new Vector2(0.72f, 0.5f), new Vector2(90, 24), TextSec);

            string productId = pack.productId;
            MakeSmallButton(row, "btn_buy", "BUY",
                new Vector2(0.90f, 0.5f), new Vector2(80, 44),
                () => IAPManager.Instance?.BuyGemPack(productId));
        }

        _panelRoot.AddComponent<EscapeListener>().OnEscape = Hide;
        _panelRoot.SetActive(false);
    }

    // ════════════════════════════════════════════════════════════════════
    //  UI HELPERS
    // ════════════════════════════════════════════════════════════════════

    static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static GameObject MakeCard(GameObject parent, string name, Vector2 anchor, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var img = go.AddComponent<Image>();
        img.color = CardBg;
        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.sizeDelta = size;
        rt.anchoredPosition = Vector2.zero;
        return go;
    }

    static TextMeshProUGUI MakeText(GameObject parent, string name, string content,
        int size, Vector2 anchor, Vector2 sizeDelta, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var txt = go.AddComponent<TextMeshProUGUI>();
        txt.text      = content;
        txt.fontSize  = size;
        txt.color     = color;
        txt.alignment = TextAlignmentOptions.Center;
        var rt = txt.rectTransform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.sizeDelta = sizeDelta;
        rt.anchoredPosition = Vector2.zero;
        return txt;
    }

    static void MakeSmallButton(GameObject parent, string name, string label,
        Vector2 anchor, Vector2 size, UnityEngine.Events.UnityAction callback)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var img = go.AddComponent<Image>();
        img.color = PrimaryBtn;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.colors = new ColorBlock
        {
            normalColor      = PrimaryBtn,
            highlightedColor = PrimaryHover,
            pressedColor     = new Color(PrimaryBtn.r * 0.7f, PrimaryBtn.g * 0.7f, PrimaryBtn.b * 0.7f),
            selectedColor    = PrimaryHover,
            colorMultiplier  = 1f,
            fadeDuration     = 0.1f
        };
        btn.onClick.AddListener(callback);
        var rt  = img.rectTransform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.sizeDelta = size;
        rt.anchoredPosition = Vector2.zero;

        var txtGO = new GameObject("Lbl");
        txtGO.transform.SetParent(go.transform, false);
        var txt   = txtGO.AddComponent<TextMeshProUGUI>();
        txt.text      = label;
        txt.fontSize  = 14;
        txt.color     = Color.black;
        txt.alignment = TextAlignmentOptions.Center;
        var trt = txt.rectTransform;
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;
    }
}
