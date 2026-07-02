using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CosmicRumble.Achievements;

/// <summary>
/// Sol üst köşedeki küçük buton → achievement listesi paneli.
/// Programatik Canvas oluşturur. MenuScene'e boş GO ekleyip scripti yapıştır.
/// </summary>
public class AchievementsPanelUI : MonoBehaviour
{
    public static AchievementsPanelUI Instance { get; private set; }

    // ── Renk paleti ───────────────────────────────────────────────────────
    static readonly Color BgColor      = new Color(0.051f, 0.051f, 0.102f, 0.97f);
    static readonly Color CardBg       = new Color(0.09f,  0.09f,  0.18f,  1f);
    static readonly Color PrimaryBtn   = new Color(0.29f,  0.62f,  1.00f,  1f);
    static readonly Color PrimaryHover = new Color(0.42f,  0.71f,  1.00f,  1f);
    static readonly Color RowBg        = new Color(0.11f,  0.11f,  0.20f,  1f);
    static readonly Color RowBgAlt     = new Color(0.13f,  0.13f,  0.24f,  1f);
    static readonly Color UnlockedGold = new Color(1.00f,  0.722f, 0.00f,  1f);
    static readonly Color LockedGray   = new Color(0.35f,  0.35f,  0.45f,  1f);
    static readonly Color TextSec      = new Color(0.533f, 0.533f, 0.667f, 1f);

    static readonly Color RarityCommon    = new Color(0.67f, 0.67f, 0.67f, 1f);
    static readonly Color RarityRare      = new Color(0.29f, 0.62f, 1.00f, 1f);
    static readonly Color RarityEpic      = new Color(0.67f, 0.27f, 1.00f, 1f);
    static readonly Color RarityLegendary = new Color(1.00f, 0.72f, 0.00f, 1f);

    // ── Referanslar ───────────────────────────────────────────────────────
    GameObject      _panelRoot;
    GameObject      _contentParent;
    TextMeshProUGUI _statsText;

    // ─────────────────────────────────────────────────────────────────────

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
        PopulateAchievements();
        _panelRoot.SetActive(true);
    }

    public void Hide() => _panelRoot.SetActive(false);

    // ════════════════════════════════════════════════════════════════════
    //  UI BUILD
    // ════════════════════════════════════════════════════════════════════

    void BuildUI()
    {
        // Canvas
        var canvasGO = new GameObject("AchievementsCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 45;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // Sol üst buton
        BuildTopLeftButton(canvasGO);

        // Panel overlay (başta gizli)
        _panelRoot = new GameObject("AchievementsPanel");
        _panelRoot.transform.SetParent(canvasGO.transform, false);
        var overlay = _panelRoot.AddComponent<Image>();
        overlay.color = new Color(0, 0, 0, 0.65f);
        StretchFull(overlay.rectTransform);

        // Kart 760 × 580
        var card = MakePanel(_panelRoot, "Card", CardBg, Vector2.zero,
            new Vector2(760, 580), new Vector2(0.5f, 0.5f));

        // Title
        MakeTxt(card, "Title", "ACHIEVEMENTS", 28, Color.white,
            new Vector2(0.5f, 0.92f), new Vector2(680, 46));

        // Close button
        MakeBtn(card, "btn_close", "X CLOSE",
            new Vector2(0.88f, 0.92f), new Vector2(100, 34),
            new Color(0.25f, 0.25f, 0.4f), new Color(0.38f, 0.38f, 0.58f), Hide);

        // İstatistik
        _statsText = MakeTxt(card, "Stats", "", 13, TextSec,
            new Vector2(0.5f, 0.83f), new Vector2(680, 22));

        // Scroll
        BuildScrollView(card);

        _panelRoot.AddComponent<EscapeListener>().OnEscape = Hide;
        _panelRoot.SetActive(false);
    }

    void BuildTopLeftButton(GameObject canvasGO)
    {
        var go  = new GameObject("AchievementsBtn");
        go.transform.SetParent(canvasGO.transform, false);
        var img = go.AddComponent<Image>();
        img.color = PrimaryBtn;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.colors = ColorBlock(PrimaryBtn, PrimaryHover);
        btn.onClick.AddListener(Show);

        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.sizeDelta        = new Vector2(160, 40);
        rt.anchoredPosition = new Vector2(90, -30);

        var lbl = MakeTxt(go, "Lbl", "ACHIEVEMENTS", 14, Color.white,
            new Vector2(0.5f, 0.5f), Vector2.zero);
        lbl.rectTransform.anchorMin = Vector2.zero;
        lbl.rectTransform.anchorMax = Vector2.one;
        lbl.rectTransform.offsetMin = lbl.rectTransform.offsetMax = Vector2.zero;
    }

    void BuildScrollView(GameObject parent)
    {
        // ScrollRect GO
        var scrollGO = new GameObject("ScrollView");
        scrollGO.transform.SetParent(parent.transform, false);
        var scrollRect = scrollGO.AddComponent<ScrollRect>();
        scrollRect.horizontal        = false;
        scrollRect.vertical          = true;
        scrollRect.scrollSensitivity = 30f;

        var scrollRt = scrollGO.GetComponent<RectTransform>();
        scrollRt.anchorMin = new Vector2(0.5f, 0.5f);
        scrollRt.anchorMax = new Vector2(0.5f, 0.5f);
        scrollRt.sizeDelta        = new Vector2(720, 430);
        scrollRt.anchoredPosition = new Vector2(0, -28);

        // Viewport
        var vpGO  = new GameObject("Viewport");
        vpGO.transform.SetParent(scrollGO.transform, false);
        var vpImg = vpGO.AddComponent<Image>();
        vpImg.color = new Color(0, 0, 0, 0.01f);
        vpGO.AddComponent<Mask>().showMaskGraphic = false;
        var vpRt = vpImg.rectTransform;
        vpRt.anchorMin = Vector2.zero;
        vpRt.anchorMax = Vector2.one;
        vpRt.offsetMin = vpRt.offsetMax = Vector2.zero;
        scrollRect.viewport = vpRt;

        // Content — VerticalLayoutGroup
        var contentGO = new GameObject("Content");
        contentGO.transform.SetParent(vpGO.transform, false);

        var vlg = contentGO.AddComponent<VerticalLayoutGroup>();
        vlg.spacing              = 3;
        vlg.padding              = new RectOffset(0, 0, 0, 0);
        vlg.childControlWidth    = true;
        vlg.childControlHeight   = true;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;

        var csf = contentGO.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var cRt = contentGO.GetComponent<RectTransform>();
        cRt.anchorMin = new Vector2(0, 1);
        cRt.anchorMax = new Vector2(1, 1);
        cRt.pivot     = new Vector2(0.5f, 1f);
        cRt.offsetMin = cRt.offsetMax = Vector2.zero;
        scrollRect.content = cRt;

        _contentParent = contentGO;

        // Scrollbar
        var sbGO  = new GameObject("Scrollbar");
        sbGO.transform.SetParent(scrollGO.transform, false);
        var sbImg = sbGO.AddComponent<Image>();
        sbImg.color = new Color(0.12f, 0.12f, 0.25f);
        var sb  = sbGO.AddComponent<Scrollbar>();
        sb.direction = Scrollbar.Direction.BottomToTop;

        var sbRt = sbImg.rectTransform;
        sbRt.anchorMin = new Vector2(1, 0);
        sbRt.anchorMax = new Vector2(1, 1);
        sbRt.sizeDelta        = new Vector2(8, 0);
        sbRt.anchoredPosition = Vector2.zero;

        var haGO = new GameObject("Area"); haGO.transform.SetParent(sbGO.transform, false);
        var haRt = haGO.AddComponent<RectTransform>();
        haRt.anchorMin = Vector2.zero; haRt.anchorMax = Vector2.one;
        haRt.offsetMin = haRt.offsetMax = Vector2.zero;

        var hGO  = new GameObject("Handle"); hGO.transform.SetParent(haGO.transform, false);
        var hImg = hGO.AddComponent<Image>(); hImg.color = PrimaryBtn;
        var hRt  = hImg.rectTransform;
        hRt.anchorMin = Vector2.zero; hRt.anchorMax = Vector2.one;
        hRt.offsetMin = hRt.offsetMax = Vector2.zero;

        sb.handleRect    = hRt;
        sb.targetGraphic = hImg;
        scrollRect.verticalScrollbar = sb;
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
    }

    // ════════════════════════════════════════════════════════════════════
    //  POPULATE
    // ════════════════════════════════════════════════════════════════════

    void PopulateAchievements()
    {
        foreach (Transform child in _contentParent.transform)
            Destroy(child.gameObject);

        var db  = Resources.Load<AchievementDatabase>("Achievements/AchievementDatabase");
        var mgr = AchievementManager.Instance;

        if (db == null || db.allAchievements == null || db.allAchievements.Count == 0)
        {
            MakeEmptyRow("No achievements defined yet.");
            if (_statsText) _statsText.text = "0 / 0 completed";
            return;
        }

        int totalUnlocked = 0;
        var all = db.allAchievements;

        for (int i = 0; i < all.Count; i++)
        {
            var def = all[i];
            if (def == null) continue;

            bool unlocked = mgr != null && mgr.IsUnlocked(def.achievementId);
            int  progress = mgr != null ? mgr.GetProgress(def.achievementId) : 0;
            bool isSecret = def.isSecret && !unlocked;

            if (unlocked) totalUnlocked++;
            BuildRow(def, unlocked, progress, isSecret, i % 2 == 0);
        }

        if (_statsText)
            _statsText.text = $"{totalUnlocked} / {all.Count} achievements completed";
    }

    // ── Satır yapısı (HorizontalLayoutGroup sütunları) ──────────────────
    //
    //  [Stripe 6] [Icon 56] [Info flex] [Rarity 90] [Status 120]
    //
    void BuildRow(AchievementDefinition def, bool unlocked, int progress,
                  bool isSecret, bool alt)
    {
        Color rarityColor = GetRarityColor(def.rarity);

        // Row container
        var row = new GameObject($"Row_{def.achievementId}");
        row.transform.SetParent(_contentParent.transform, false);
        var rowImg = row.AddComponent<Image>();
        rowImg.color = alt ? RowBg : RowBgAlt;

        var rowLE = row.AddComponent<LayoutElement>();
        rowLE.preferredHeight = 68;
        rowLE.minHeight       = 68;

        var rowHLG = row.AddComponent<HorizontalLayoutGroup>();
        rowHLG.spacing              = 0;
        rowHLG.padding              = new RectOffset(0, 8, 0, 0);
        rowHLG.childControlWidth    = true;
        rowHLG.childControlHeight   = true;
        rowHLG.childForceExpandWidth  = false;
        rowHLG.childForceExpandHeight = true;

        // ── Sütun 1: Rarity stripe (6px) ─────────────────────────────────
        var stripe = ColFixed(row, "Stripe", 6);
        stripe.AddComponent<Image>().color = unlocked ? rarityColor : LockedGray;

        // ── Sütun 2: İkon (56px) ─────────────────────────────────────────
        var iconCol = ColFixed(row, "IconCol", 56);
        var iconBg  = iconCol.AddComponent<Image>();
        iconBg.color = unlocked
            ? new Color(rarityColor.r, rarityColor.g, rarityColor.b, 0.18f)
            : new Color(0.18f, 0.18f, 0.28f, 1f);

        var iconLbl = MakeTxt(iconCol, "IconLbl",
            unlocked ? GetRaritySymbol(def.rarity) : "?",
            20, unlocked ? rarityColor : LockedGray,
            new Vector2(0.5f, 0.5f), Vector2.zero);
        StretchFull(iconLbl.rectTransform);

        // ── Sütun 3: Ad + Açıklama (esnek) ───────────────────────────────
        var infoCol = ColFlex(row, "InfoCol", 1f);
        infoCol.AddComponent<Image>().color = new Color(0, 0, 0, 0); // şeffaf

        var infoVLG = infoCol.AddComponent<VerticalLayoutGroup>();
        infoVLG.spacing              = 2;
        infoVLG.padding              = new RectOffset(8, 4, 10, 10);
        infoVLG.childControlWidth    = true;
        infoVLG.childControlHeight   = true;
        infoVLG.childForceExpandWidth  = true;
        infoVLG.childForceExpandHeight = false;

        string displayName = isSecret ? "???" : def.displayName;
        string desc        = isSecret ? "Secret achievement" : def.description;

        var nameTxt = MakeTxtLE(infoCol, "Name", displayName, 15,
            unlocked ? Color.white : LockedGray, TextAlignmentOptions.Left);
        nameTxt.fontStyle = unlocked ? FontStyles.Bold : FontStyles.Normal;

        MakeTxtLE(infoCol, "Desc", desc, 11,
            unlocked ? TextSec : new Color(0.28f, 0.28f, 0.38f),
            TextAlignmentOptions.Left);

        // ── Sütun 4: Rarity etiketi (90px) ───────────────────────────────
        var rarityCol = ColFixed(row, "RarityCol", 90);
        rarityCol.AddComponent<Image>().color = new Color(0, 0, 0, 0);

        MakeTxt(rarityCol, "RarityLbl", def.rarity.ToString(), 11,
            unlocked ? rarityColor : LockedGray,
            new Vector2(0.5f, 0.5f), Vector2.zero)
            .rectTransform.Let(rt => StretchFull(rt));

        // ── Sütun 5: Durum (120px) ────────────────────────────────────────
        var statusCol = ColFixed(row, "StatusCol", 120);
        statusCol.AddComponent<Image>().color = new Color(0, 0, 0, 0);

        var statusVLG = statusCol.AddComponent<VerticalLayoutGroup>();
        statusVLG.spacing              = 3;
        statusVLG.padding              = new RectOffset(4, 4, 14, 14);
        statusVLG.childControlWidth    = true;
        statusVLG.childControlHeight   = true;
        statusVLG.childForceExpandWidth  = true;
        statusVLG.childForceExpandHeight = false;

        if (unlocked)
        {
            MakeTxtLE(statusCol, "StatusLbl", "UNLOCKED", 13,
                UnlockedGold, TextAlignmentOptions.Center);
        }
        else if (def.triggerType == AchievementTriggerType.Cumulative && def.targetValue > 0)
        {
            int clamped = Mathf.Clamp(progress, 0, def.targetValue);
            MakeTxtLE(statusCol, "StatusLbl", $"{clamped}/{def.targetValue}", 13,
                TextSec, TextAlignmentOptions.Center);

            // Mini progress bar
            var barLE = new GameObject("BarLE");
            barLE.transform.SetParent(statusCol.transform, false);
            var barLEComp = barLE.AddComponent<LayoutElement>();
            barLEComp.preferredHeight = 6;
            barLEComp.flexibleWidth   = 1;

            var barBg  = barLE.AddComponent<Image>();
            barBg.color = new Color(0.2f, 0.2f, 0.32f);

            float fill = def.targetValue > 0 ? (float)clamped / def.targetValue : 0f;
            var fillGO  = new GameObject("Fill");
            fillGO.transform.SetParent(barLE.transform, false);
            var fillImg = fillGO.AddComponent<Image>();
            fillImg.color = PrimaryBtn;
            var fillRt = fillImg.rectTransform;
            fillRt.anchorMin = new Vector2(0, 0);
            fillRt.anchorMax = new Vector2(fill, 1);
            fillRt.offsetMin = fillRt.offsetMax = Vector2.zero;
        }
        else
        {
            MakeTxtLE(statusCol, "StatusLbl", "Locked", 12,
                LockedGray, TextAlignmentOptions.Center);
        }
    }

    void MakeEmptyRow(string msg)
    {
        var go  = new GameObject("EmptyRow");
        go.transform.SetParent(_contentParent.transform, false);
        go.AddComponent<Image>().color = RowBg;
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 60;

        var txt = MakeTxt(go, "Msg", msg, 15, TextSec,
            new Vector2(0.5f, 0.5f), Vector2.zero);
        StretchFull(txt.rectTransform);
    }

    // ════════════════════════════════════════════════════════════════════
    //  LAYOUT HELPERS
    // ════════════════════════════════════════════════════════════════════

    /// <summary>HorizontalLayoutGroup içinde sabit genişlikli sütun.</summary>
    static GameObject ColFixed(GameObject parent, string name, float width)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var le = go.AddComponent<LayoutElement>();
        le.minWidth       = width;
        le.preferredWidth = width;
        le.flexibleWidth  = 0;
        return go;
    }

    /// <summary>HorizontalLayoutGroup içinde esnek genişlikli sütun.</summary>
    static GameObject ColFlex(GameObject parent, string name, float flex)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var le = go.AddComponent<LayoutElement>();
        le.flexibleWidth = flex;
        return go;
    }

    // ════════════════════════════════════════════════════════════════════
    //  UI BUILDER HELPERS
    // ════════════════════════════════════════════════════════════════════

    static GameObject MakePanel(GameObject parent, string name, Color color,
        Vector2 anchoredPos, Vector2 size, Vector2 anchor)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        var rt  = img.rectTransform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.sizeDelta        = size;
        rt.anchoredPosition = anchoredPos;
        return go;
    }

    static TextMeshProUGUI MakeTxt(GameObject parent, string name, string content,
        int size, Color color, Vector2 anchor, Vector2 sizeDelta)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var txt = go.AddComponent<TextMeshProUGUI>();
        txt.text            = content;
        txt.fontSize        = size;
        txt.color           = color;
        txt.alignment       = TextAlignmentOptions.Center;
        txt.overflowMode    = TextOverflowModes.Ellipsis;
        var rt = txt.rectTransform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.sizeDelta        = sizeDelta;
        rt.anchoredPosition = Vector2.zero;
        return txt;
    }

    /// <summary>VerticalLayoutGroup/HLG içinde LayoutElement'li metin.</summary>
    static TextMeshProUGUI MakeTxtLE(GameObject parent, string name, string content,
        int size, Color color, TextAlignmentOptions alignment)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var txt = go.AddComponent<TextMeshProUGUI>();
        txt.text         = content;
        txt.fontSize     = size;
        txt.color        = color;
        txt.alignment    = alignment;
        txt.overflowMode = TextOverflowModes.Ellipsis;
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = size * 1.6f;
        le.flexibleWidth   = 1;
        return txt;
    }

    static void MakeBtn(GameObject parent, string name, string label,
        Vector2 anchor, Vector2 size, Color normal, Color hover,
        UnityEngine.Events.UnityAction cb)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var img = go.AddComponent<Image>();
        img.color = normal;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.colors = ColorBlock(normal, hover);
        btn.onClick.AddListener(cb);
        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.sizeDelta        = size;
        rt.anchoredPosition = Vector2.zero;

        var lblGO = new GameObject("Lbl"); lblGO.transform.SetParent(go.transform, false);
        var lbl   = lblGO.AddComponent<TextMeshProUGUI>();
        lbl.text      = label; lbl.fontSize = 13; lbl.color = Color.white;
        lbl.alignment = TextAlignmentOptions.Center;
        var lrt = lbl.rectTransform;
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = lrt.offsetMax = Vector2.zero;
    }

    static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static ColorBlock ColorBlock(Color normal, Color hover) => new ColorBlock
    {
        normalColor      = normal,
        highlightedColor = hover,
        pressedColor     = new Color(normal.r * 0.7f, normal.g * 0.7f, normal.b * 0.7f),
        selectedColor    = hover,
        colorMultiplier  = 1f,
        fadeDuration     = 0.1f
    };

    // ════════════════════════════════════════════════════════════════════
    //  DATA HELPERS
    // ════════════════════════════════════════════════════════════════════

    static Color GetRarityColor(AchievementRarity r) => r switch
    {
        AchievementRarity.Common    => RarityCommon,
        AchievementRarity.Rare      => RarityRare,
        AchievementRarity.Epic      => RarityEpic,
        AchievementRarity.Legendary => RarityLegendary,
        _                           => RarityCommon
    };

    static string GetRaritySymbol(AchievementRarity r) => r switch
    {
        AchievementRarity.Common    => "C",
        AchievementRarity.Rare      => "R",
        AchievementRarity.Epic      => "E",
        AchievementRarity.Legendary => "L",
        _                           => "C"
    };
}

// ── RectTransform fluent helper ──────────────────────────────────────────
internal static class RectTransformExt
{
    internal static RectTransform Let(this RectTransform rt,
        System.Action<RectTransform> action) { action(rt); return rt; }
}
