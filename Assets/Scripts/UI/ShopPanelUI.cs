using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CosmicRumble.Economy;
using CosmicRumble.Economy.IAP;
using CosmicRumble.Localization;

/// <summary>
/// Ana menüdeki MARKET butonu → Gem satın alma paneli. Modern mobil mağaza düzeni:
/// yan yana dikey paket kartları (gem ikonu + miktar + fiyatlı satın al butonu), popüler
/// pakette rozet. IAPManager.GemPacks kataloğunu kullanır; mağaza bağlanmadıysa fiyat "--".
/// Alt şeritte Gold/Gem ile sandık satın alma teklifleri (ekonominin harcama yolu —
/// ChestManager.TryPurchaseChest). UiKit stiliyle programatik Canvas kurar.
/// </summary>
public class ShopPanelUI : MonoBehaviour
{
    public static ShopPanelUI Instance { get; private set; }

    static readonly Color CardBg     = new Color(0.07f,  0.07f,  0.16f,  0.97f);
    static readonly Color PackBg     = new Color(0.11f,  0.11f,  0.21f,  1f);
    static readonly Color PackBgTop  = new Color(0.16f,  0.16f,  0.30f,  1f);
    static readonly Color BuyGreen   = new Color(0.16f,  0.72f,  0.26f,  1f);
    static readonly Color GemColor   = new Color(0.60f,  0.85f,  1.00f,  1f);
    static readonly Color BadgeGold  = new Color(1.00f,  0.72f,  0.00f,  1f);
    static readonly Color TextSec    = new Color(0.533f, 0.533f, 0.667f, 1f);
    static readonly Color StrokeCol  = new Color(1f, 1f, 1f, 0.09f);

    /// <summary>Rozet gösterilecek paketler (indeks → etiket).</summary>
    const int PopularPackIndex   = 2; // gem_pack_1200
    const int BestValuePackIndex = 4; // gem_pack_6000

    GameObject _panelRoot;
    TextMeshProUGUI[] _priceTexts;
    Button _rareChestBtn, _epicChestBtn;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        BuildUI();
        if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.OnCurrencyChanged += HandleCurrencyChanged;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.OnCurrencyChanged -= HandleCurrencyChanged;
    }

    void HandleCurrencyChanged(CurrencyType type, long newBalance) => RefreshChestOffers();

    // ════════════════════════════════════════════════════════════════════
    //  PUBLIC API
    // ════════════════════════════════════════════════════════════════════

    public void Show()
    {
        _panelRoot.SetActive(true);
        RefreshChestOffers();
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
        {
            string price = IAPManager.Instance != null
                ? IAPManager.Instance.GetLocalizedPrice(packs[i].productId)
                : "--";
            _priceTexts[i].text = string.IsNullOrEmpty(price) ? "--" : price;
        }
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

        var packs = IAPManager.GemPacks;
        _priceTexts = new TextMeshProUGUI[packs.Length];

        // Kart: 5 paket yan yana rahat sığsın (yatay telefon ekranı)
        var card = new GameObject("Card");
        card.transform.SetParent(_panelRoot.transform, false);
        var cardImg = card.AddComponent<Image>();
        cardImg.color = CardBg;
        UiKit.Round(cardImg);
        UiKit.Shadow(card, 8f, 0.55f);
        UiKit.Stroke(card, StrokeCol);
        UiKit.Pop(card);
        var cardRt = cardImg.rectTransform;
        cardRt.anchorMin = cardRt.anchorMax = new Vector2(0.5f, 0.5f);
        cardRt.sizeDelta = new Vector2(1060, 700); // 560 → 700: altta sandık şeridi
        cardRt.anchoredPosition = Vector2.zero;

        var title = MakeText(card, "Title", Loc.T("SHOP"), 30,
            new Vector2(0.5f, 0.92f), new Vector2(500, 46), GemColor);
        title.fontStyle = FontStyles.Bold;

        MakeText(card, "Subtitle", Loc.T("Gem packs — for costumes and chests"), 14,
            new Vector2(0.5f, 0.845f), new Vector2(600, 24), TextSec);

        UiKit.CloseButton(card, Hide);

        // ── Paket kartları: yatay sıra ───────────────────────────────────
        float packW   = 188f;
        float gap     = 14f;
        float totalW  = packs.Length * packW + (packs.Length - 1) * gap;
        float startX  = -totalW * 0.5f + packW * 0.5f;

        for (int i = 0; i < packs.Length; i++)
        {
            BuildPackCard(card, packs[i], i,
                new Vector2(startX + i * (packW + gap), 30f), new Vector2(packW, 360f));
        }

        BuildChestStrip(card);

        _panelRoot.AddComponent<EscapeListener>().OnEscape = Hide;
        _panelRoot.SetActive(false);
    }

    // ── Sandık teklifleri (Gold/Gem harcama yolu) ─────────────────────────

    void BuildChestStrip(GameObject card)
    {
        var header = MakeText(card, "ChestHeader", Loc.T("CHESTS"), 20,
            new Vector2(0.5f, 0.5f), new Vector2(400, 30), BadgeGold);
        header.fontStyle = FontStyles.Bold;
        header.rectTransform.anchoredPosition = new Vector2(0, -190);

        long rareGold = ChestManager.Instance != null
            ? ChestManager.Instance.GetChestGoldPrice(ChestType.Rare) : 800;
        long epicGem = ChestManager.Instance != null
            ? ChestManager.Instance.GetChestGemPrice(ChestType.Epic) : 25;

        _rareChestBtn = BuildChestOffer(card, "btn_chest_rare",
            Loc.T("RARE CHEST"), string.Format(Loc.T("{0} Gold"), rareGold),
            BadgeGold, new Vector2(-215, -270),
            () => ChestManager.Instance?.TryPurchaseChest(ChestType.Rare));

        _epicChestBtn = BuildChestOffer(card, "btn_chest_epic",
            Loc.T("EPIC CHEST"), string.Format(Loc.T("{0} Gem"), epicGem),
            GemColor, new Vector2(215, -270),
            () => ChestManager.Instance?.TryPurchaseChest(ChestType.Epic));
    }

    Button BuildChestOffer(GameObject parent, string name, string title, string price,
        Color accent, Vector2 pos, System.Action onBuy)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var img = go.AddComponent<Image>();
        img.color = Color.white;
        UiKit.Round(img, 1.4f);
        UiKit.Gradient(img, PackBgTop, PackBg);
        UiKit.Shadow(go, 4f, 0.4f);
        UiKit.Stroke(go, new Color(accent.r, accent.g, accent.b, 0.45f), 1.4f);
        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(390, 96);
        rt.anchoredPosition = pos;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.colors = UiKit.ButtonColors(Color.white);
        btn.onClick.AddListener(() => onBuy?.Invoke());
        UiKit.Press(go);
        UiKit.Hover(go);

        var titleTxt = MakeText(go, "Title", title, 19,
            new Vector2(0.5f, 0.5f), new Vector2(360, 28), Color.white);
        titleTxt.fontStyle = FontStyles.Bold;
        titleTxt.rectTransform.anchoredPosition = new Vector2(0, 16);

        MakeText(go, "Price", price, 16,
            new Vector2(0.5f, 0.5f), new Vector2(360, 24), accent)
            .rectTransform.anchoredPosition = new Vector2(0, -18);

        return btn;
    }

    /// <summary>Bakiye yetmeyen teklifin butonunu pasifleştirir (yarım harcama zaten imkânsız, bu salt UX).</summary>
    void RefreshChestOffers()
    {
        var cur   = CurrencyManager.Instance;
        var chest = ChestManager.Instance;
        bool ready = cur != null && chest != null;

        if (_rareChestBtn != null)
            _rareChestBtn.interactable = ready
                && cur.Get(CurrencyType.Gold) >= chest.GetChestGoldPrice(ChestType.Rare);
        if (_epicChestBtn != null)
            _epicChestBtn.interactable = ready
                && cur.Get(CurrencyType.Gem) >= chest.GetChestGemPrice(ChestType.Epic);
    }

    /// <summary>Tek paket kartı: gradyanlı zemin, gem dairesi, miktar, ad, fiyatlı SATIN AL.</summary>
    void BuildPackCard(GameObject parent, GemPackDefinition pack, int index,
        Vector2 pos, Vector2 size)
    {
        var cardGO = new GameObject($"Pack_{pack.productId}");
        cardGO.transform.SetParent(parent.transform, false);
        var img = cardGO.AddComponent<Image>();
        img.color = Color.white;
        UiKit.Round(img, 1.2f);
        UiKit.Gradient(img, PackBgTop, PackBg);
        UiKit.Shadow(cardGO, 5f, 0.45f);
        UiKit.Stroke(cardGO, StrokeCol, 1.2f);
        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;

        // Rozet (popüler / en iyi değer)
        string badge = index == PopularPackIndex   ? Loc.T("POPULAR")
                     : index == BestValuePackIndex ? Loc.T("BEST VALUE") : null;
        if (badge != null)
        {
            var badgeGO = new GameObject("Badge");
            badgeGO.transform.SetParent(cardGO.transform, false);
            var badgeImg = badgeGO.AddComponent<Image>();
            badgeImg.color = BadgeGold;
            UiKit.Round(badgeImg, 2.2f);
            var brt = badgeImg.rectTransform;
            brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 1f);
            brt.sizeDelta = new Vector2(size.x - 34f, 30);
            brt.anchoredPosition = new Vector2(0, 4); // üst kenardan hafif taşar
            var bTxt = MakeText(badgeGO, "Lbl", badge, 13,
                new Vector2(0.5f, 0.5f), Vector2.zero, new Color(0.18f, 0.12f, 0.02f, 1f));
            bTxt.fontStyle = FontStyles.Bold;
            StretchFull(bTxt.rectTransform);
        }

        // Gem ikonu (renkli daire + iç parlama)
        var gemGO = new GameObject("GemIcon");
        gemGO.transform.SetParent(cardGO.transform, false);
        var gemImg = gemGO.AddComponent<Image>();
        gemImg.sprite = UiKit.CircleSprite;
        gemImg.color  = new Color(GemColor.r, GemColor.g, GemColor.b, 0.25f);
        var gemRt = gemImg.rectTransform;
        gemRt.anchorMin = gemRt.anchorMax = new Vector2(0.5f, 1f);
        gemRt.sizeDelta = new Vector2(92, 92);
        gemRt.anchoredPosition = new Vector2(0, -86);

        var gemInnerGO = new GameObject("GemInner");
        gemInnerGO.transform.SetParent(gemGO.transform, false);
        var gemInner = gemInnerGO.AddComponent<Image>();
        gemInner.sprite = UiKit.CircleSprite;
        gemInner.color  = GemColor;
        var giRt = gemInner.rectTransform;
        giRt.anchorMin = giRt.anchorMax = new Vector2(0.5f, 0.5f);
        giRt.sizeDelta = new Vector2(56, 56);
        giRt.anchoredPosition = Vector2.zero;

        // Miktar (büyük) + ad (küçük)
        var amount = MakeText(cardGO, "Amount", $"{pack.gemAmount}", 34,
            new Vector2(0.5f, 1f), new Vector2(160, 44), Color.white);
        amount.fontStyle = FontStyles.Bold;
        amount.rectTransform.anchoredPosition = new Vector2(0, -166);

        MakeText(cardGO, "GemLbl", "GEM", 14,
            new Vector2(0.5f, 1f), new Vector2(160, 22), GemColor)
            .rectTransform.anchoredPosition = new Vector2(0, -196);

        // Satın al butonu (fiyat butonun üstünde)
        var buyGO = new GameObject("btn_buy");
        buyGO.transform.SetParent(cardGO.transform, false);
        var buyImg = buyGO.AddComponent<Image>();
        buyImg.color = BuyGreen;
        UiKit.Round(buyImg, 1.5f);
        UiKit.Shadow(buyGO, 3f, 0.35f);
        var buyBtn = buyGO.AddComponent<Button>();
        buyBtn.targetGraphic = buyImg;
        buyBtn.colors = UiKit.ButtonColors(BuyGreen);
        string productId = pack.productId;
        buyBtn.onClick.AddListener(() => IAPManager.Instance?.BuyGemPack(productId));
        UiKit.Press(buyGO);
        UiKit.Hover(buyGO);
        var buyRt = buyImg.rectTransform;
        buyRt.anchorMin = buyRt.anchorMax = new Vector2(0.5f, 0f);
        buyRt.sizeDelta = new Vector2(size.x - 28f, 58);
        buyRt.anchoredPosition = new Vector2(0, 46);

        _priceTexts[index] = MakeText(buyGO, "Price", "--", 17,
            new Vector2(0.5f, 0.5f), Vector2.zero, Color.white);
        _priceTexts[index].fontStyle = FontStyles.Bold;
        StretchFull(_priceTexts[index].rectTransform);

        MakeText(cardGO, "BuyHint", Loc.T("BUY"), 12,
            new Vector2(0.5f, 0f), new Vector2(140, 20), TextSec)
            .rectTransform.anchoredPosition = new Vector2(0, 16);
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
        txt.overflowMode = TextOverflowModes.Ellipsis;
        var rt = txt.rectTransform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.sizeDelta = sizeDelta;
        rt.anchoredPosition = Vector2.zero;
        return txt;
    }
}
