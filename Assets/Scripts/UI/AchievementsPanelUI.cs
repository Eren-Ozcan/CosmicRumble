using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CosmicRumble.Achievements;
using CosmicRumble.Localization;

/// <summary>
/// Ana menüdeki BAŞARIMLAR butonu → başarım listesi paneli.
/// UiKit stiliyle (yuvarlatık kart + kontur + pop animasyonu + köşe X) programatik Canvas kurar.
/// </summary>
public class AchievementsPanelUI : MonoBehaviour
{
    public static AchievementsPanelUI Instance { get; private set; }

    // ── Renk paleti (UiKit mobil teması) ──────────────────────────────────
    static readonly Color CardBg       = new Color(0.07f,  0.07f,  0.16f,  0.97f);
    static readonly Color PrimaryBtn   = new Color(0.29f,  0.62f,  1.00f,  1f);
    static readonly Color RowBg        = new Color(0.11f,  0.11f,  0.21f,  1f);
    static readonly Color RowBgAlt     = new Color(0.13f,  0.13f,  0.25f,  1f);
    static readonly Color UnlockedGold = new Color(1.00f,  0.722f, 0.00f,  1f);
    static readonly Color LockedGray   = new Color(0.35f,  0.35f,  0.45f,  1f);
    static readonly Color TextSec      = new Color(0.533f, 0.533f, 0.667f, 1f);
    static readonly Color StrokeCol    = new Color(1f, 1f, 1f, 0.09f);

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

        // Panel overlay (başta gizli)
        _panelRoot = new GameObject("AchievementsPanel");
        _panelRoot.transform.SetParent(canvasGO.transform, false);
        var overlay = _panelRoot.AddComponent<Image>();
        overlay.color = new Color(0, 0, 0, 0.65f);
        StretchFull(overlay.rectTransform);

        // Kart 820 × 600
        var card = MakePanel(_panelRoot, "Card", CardBg, Vector2.zero,
            new Vector2(820, 600), new Vector2(0.5f, 0.5f));
        UiKit.Round(card.GetComponent<Image>());
        UiKit.Shadow(card, 8f, 0.55f);
        UiKit.Stroke(card, StrokeCol);
        UiKit.Pop(card);

        var title = MakeTxt(card, "Title", Loc.T("ACHIEVEMENTS"), 30, Color.white,
            new Vector2(0.5f, 0.925f), new Vector2(680, 46));
        title.fontStyle = FontStyles.Bold;
        title.color     = UnlockedGold;

        UiKit.CloseButton(card, Hide);

        // İstatistik
        _statsText = MakeTxt(card, "Stats", "", 14, TextSec,
            new Vector2(0.5f, 0.845f), new Vector2(680, 22));

        // Scroll
        BuildScrollView(card);

        _panelRoot.AddComponent<EscapeListener>().OnEscape = Hide;
        _panelRoot.SetActive(false);
    }

    void BuildScrollView(GameObject parent)
    {
        var scrollGO = new GameObject("ScrollView");
        scrollGO.transform.SetParent(parent.transform, false);
        var scrollRect = scrollGO.AddComponent<ScrollRect>();
        scrollRect.horizontal        = false;
        scrollRect.vertical          = true;
        scrollRect.scrollSensitivity = 30f;

        var scrollRt = scrollGO.GetComponent<RectTransform>();
        scrollRt.anchorMin = new Vector2(0.5f, 0.5f);
        scrollRt.anchorMax = new Vector2(0.5f, 0.5f);
        scrollRt.sizeDelta        = new Vector2(770, 450);
        scrollRt.anchoredPosition = new Vector2(0, -36);

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

        var contentGO = new GameObject("Content");
        contentGO.transform.SetParent(vpGO.transform, false);

        var vlg = contentGO.AddComponent<VerticalLayoutGroup>();
        vlg.spacing              = 8;
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
            MakeEmptyRow(Loc.T("No achievements defined yet."));
            if (_statsText) _statsText.text = string.Format(Loc.T("{0} / {1} completed"), 0, 0);
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
            _statsText.text = string.Format(Loc.T("{0} / {1} completed"), totalUnlocked, all.Count);
    }

    // ── Satır: [İkon dairesi 72] [Ad+Açıklama flex] [Nadirlik 96] [Durum 130] ──
    void BuildRow(AchievementDefinition def, bool unlocked, int progress,
                  bool isSecret, bool alt)
    {
        Color rarityColor = GetRarityColor(def.rarity);

        var row = new GameObject($"Row_{def.achievementId}");
        row.transform.SetParent(_contentParent.transform, false);
        var rowImg = row.AddComponent<Image>();
        rowImg.color = alt ? RowBg : RowBgAlt;
        UiKit.Round(rowImg, 2f);

        var rowLE = row.AddComponent<LayoutElement>();
        rowLE.preferredHeight = 76;
        rowLE.minHeight       = 76;

        var rowHLG = row.AddComponent<HorizontalLayoutGroup>();
        rowHLG.spacing              = 10;
        rowHLG.padding              = new RectOffset(12, 14, 0, 0);
        rowHLG.childControlWidth    = true;
        rowHLG.childControlHeight   = true;
        rowHLG.childForceExpandWidth  = false;
        rowHLG.childForceExpandHeight = true;

        // ── Sütun 1: Nadirlik renkli ikon dairesi ────────────────────────
        var iconCol = ColFixed(row, "IconCol", 60);

        var iconBgGO = new GameObject("IconBg");
        iconBgGO.transform.SetParent(iconCol.transform, false);
        var iconBg = iconBgGO.AddComponent<Image>();
        iconBg.sprite = UiKit.CircleSprite;
        iconBg.color  = unlocked
            ? new Color(rarityColor.r, rarityColor.g, rarityColor.b, 0.30f)
            : new Color(0.18f, 0.18f, 0.28f, 1f);
        var iconBgRt = iconBg.rectTransform;
        iconBgRt.anchorMin = iconBgRt.anchorMax = new Vector2(0.5f, 0.5f);
        iconBgRt.sizeDelta = new Vector2(52, 52);
        iconBgRt.anchoredPosition = Vector2.zero;

        var iconLbl = MakeTxt(iconBgGO, "IconLbl",
            unlocked ? GetRaritySymbol(def.rarity) : "?",
            22, unlocked ? rarityColor : LockedGray,
            new Vector2(0.5f, 0.5f), Vector2.zero);
        iconLbl.fontStyle = FontStyles.Bold;
        StretchFull(iconLbl.rectTransform);

        // ── Sütun 2: Ad + Açıklama (esnek) ───────────────────────────────
        var infoCol = ColFlex(row, "InfoCol", 1f);

        var infoVLG = infoCol.AddComponent<VerticalLayoutGroup>();
        infoVLG.spacing              = 2;
        infoVLG.padding              = new RectOffset(2, 4, 12, 12);
        infoVLG.childControlWidth    = true;
        infoVLG.childControlHeight   = true;
        infoVLG.childForceExpandWidth  = true;
        infoVLG.childForceExpandHeight = false;

        string displayName = isSecret ? "???" : def.displayName;
        string desc        = isSecret ? Loc.T("Secret achievement") : def.description;

        var nameTxt = MakeTxtLE(infoCol, "Name", displayName, 16,
            unlocked ? Color.white : LockedGray, TextAlignmentOptions.Left);
        nameTxt.fontStyle = unlocked ? FontStyles.Bold : FontStyles.Normal;

        MakeTxtLE(infoCol, "Desc", desc, 12,
            unlocked ? TextSec : new Color(0.28f, 0.28f, 0.38f),
            TextAlignmentOptions.Left);

        // ── Sütun 3: Nadirlik etiketi ────────────────────────────────────
        var rarityCol = ColFixed(row, "RarityCol", 96);
        var rarityTxt = MakeTxt(rarityCol, "RarityLbl", GetRarityLabel(def.rarity), 12,
            unlocked ? rarityColor : LockedGray,
            new Vector2(0.5f, 0.5f), Vector2.zero);
        rarityTxt.fontStyle = FontStyles.Bold;
        StretchFull(rarityTxt.rectTransform);

        // ── Sütun 4: Durum ───────────────────────────────────────────────
        var statusCol = ColFixed(row, "StatusCol", 130);

        var statusVLG = statusCol.AddComponent<VerticalLayoutGroup>();
        statusVLG.spacing              = 4;
        statusVLG.padding              = new RectOffset(4, 4, 16, 16);
        statusVLG.childControlWidth    = true;
        statusVLG.childControlHeight   = true;
        statusVLG.childForceExpandWidth  = true;
        statusVLG.childForceExpandHeight = false;

        if (unlocked)
        {
            MakeTxtLE(statusCol, "StatusLbl", Loc.T("UNLOCKED"), 14,
                UnlockedGold, TextAlignmentOptions.Center).fontStyle = FontStyles.Bold;
        }
        else if (def.triggerType == AchievementTriggerType.Cumulative && def.targetValue > 0)
        {
            int clamped = Mathf.Clamp(progress, 0, def.targetValue);
            MakeTxtLE(statusCol, "StatusLbl", $"{clamped}/{def.targetValue}", 13,
                TextSec, TextAlignmentOptions.Center);

            MakeProgressBar(statusCol, def.targetValue > 0 ? (float)clamped / def.targetValue : 0f,
                PrimaryBtn);
        }
        else
        {
            MakeTxtLE(statusCol, "StatusLbl", Loc.T("Locked"), 13,
                LockedGray, TextAlignmentOptions.Center);
        }
    }

    /// <summary>Yuvarlatık mini ilerleme çubuğu (layout içinde).</summary>
    static void MakeProgressBar(GameObject parent, float fill01, Color fillColor)
    {
        var barLE = new GameObject("BarLE");
        barLE.transform.SetParent(parent.transform, false);
        var barLEComp = barLE.AddComponent<LayoutElement>();
        barLEComp.preferredHeight = 8;
        barLEComp.flexibleWidth   = 1;

        var barBg = barLE.AddComponent<Image>();
        barBg.color = new Color(0.2f, 0.2f, 0.32f);
        UiKit.Round(barBg, 4f);

        float fill = Mathf.Clamp01(fill01);
        if (fill <= 0.001f) return;
        var fillGO  = new GameObject("Fill");
        fillGO.transform.SetParent(barLE.transform, false);
        var fillImg = fillGO.AddComponent<Image>();
        fillImg.color = fillColor;
        UiKit.Round(fillImg, 4f);
        var fillRt = fillImg.rectTransform;
        fillRt.anchorMin = new Vector2(0, 0);
        fillRt.anchorMax = new Vector2(fill, 1);
        fillRt.offsetMin = fillRt.offsetMax = Vector2.zero;
    }

    void MakeEmptyRow(string msg)
    {
        var go  = new GameObject("EmptyRow");
        go.transform.SetParent(_contentParent.transform, false);
        var img = go.AddComponent<Image>();
        img.color = RowBg;
        UiKit.Round(img, 2f);
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 64;

        var txt = MakeTxt(go, "Msg", msg, 15, TextSec,
            new Vector2(0.5f, 0.5f), Vector2.zero);
        StretchFull(txt.rectTransform);
    }

    // ════════════════════════════════════════════════════════════════════
    //  LAYOUT HELPERS
    // ════════════════════════════════════════════════════════════════════

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

    static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

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

    static string GetRarityLabel(AchievementRarity r) => r switch
    {
        AchievementRarity.Common    => Loc.T("COMMON"),
        AchievementRarity.Rare      => Loc.T("RARE"),
        AchievementRarity.Epic      => Loc.T("EPIC"),
        AchievementRarity.Legendary => Loc.T("LEGENDARY"),
        _                           => Loc.T("COMMON")
    };
}

// ── RectTransform fluent helper ──────────────────────────────────────────
internal static class RectTransformExt
{
    internal static RectTransform Let(this RectTransform rt,
        System.Action<RectTransform> action) { action(rt); return rt; }
}
