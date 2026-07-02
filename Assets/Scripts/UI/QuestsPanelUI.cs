using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CosmicRumble.Economy;

/// <summary>
/// Ana menüdeki "QUESTS" butonu → günlük/haftalık/aylık görev paneli.
/// Programatik Canvas oluşturur. MenuScene'e boş GO ekleyip scripti yapıştır.
/// </summary>
public class QuestsPanelUI : MonoBehaviour
{
    public static QuestsPanelUI Instance { get; private set; }

    // ── Renk paleti ───────────────────────────────────────────────────────
    static readonly Color BgColor      = new Color(0.051f, 0.051f, 0.102f, 0.97f);
    static readonly Color CardBg       = new Color(0.09f,  0.09f,  0.18f,  1f);
    static readonly Color PrimaryBtn   = new Color(0.12f,  0.68f,  0.22f,  1f);
    static readonly Color PrimaryHover = new Color(0.18f,  0.82f,  0.30f,  1f);
    static readonly Color TabOff       = new Color(0.16f,  0.16f,  0.28f,  1f);
    static readonly Color TabOffHover  = new Color(0.22f,  0.22f,  0.36f,  1f);
    static readonly Color RowBg        = new Color(0.11f,  0.11f,  0.20f,  1f);
    static readonly Color RowBgAlt     = new Color(0.13f,  0.13f,  0.24f,  1f);
    static readonly Color CompletedGr  = new Color(0.30f,  0.85f,  0.40f,  1f);
    static readonly Color TextSec      = new Color(0.533f, 0.533f, 0.667f, 1f);

    enum Tab { Daily, Weekly, Monthly }
    Tab _currentTab = Tab.Daily;

    // ── Referanslar ───────────────────────────────────────────────────────
    GameObject      _panelRoot;
    GameObject      _contentParent;
    TextMeshProUGUI _resetText;
    Button          _tabDailyBtn, _tabWeeklyBtn, _tabMonthlyBtn;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        BuildUI();
    }

    void OnEnable()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestProgress  += HandleQuestChanged;
            QuestManager.Instance.OnQuestCompleted += HandleQuestCompleted;
        }
    }

    void OnDisable()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestProgress  -= HandleQuestChanged;
            QuestManager.Instance.OnQuestCompleted -= HandleQuestCompleted;
        }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void HandleQuestChanged(QuestDefinition def, int progress)
    {
        if (_panelRoot != null && _panelRoot.activeSelf) PopulateQuests();
    }

    void HandleQuestCompleted(QuestDefinition def)
    {
        if (_panelRoot != null && _panelRoot.activeSelf) PopulateQuests();
    }

    // ════════════════════════════════════════════════════════════════════
    //  PUBLIC API
    // ════════════════════════════════════════════════════════════════════

    public void Show()
    {
        PopulateQuests();
        _panelRoot.SetActive(true);
    }

    public void Hide() => _panelRoot.SetActive(false);

    // ════════════════════════════════════════════════════════════════════
    //  UI BUILD
    // ════════════════════════════════════════════════════════════════════

    void BuildUI()
    {
        var canvasGO = new GameObject("QuestsCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 46;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        _panelRoot = new GameObject("QuestsPanel");
        _panelRoot.transform.SetParent(canvasGO.transform, false);
        var overlay = _panelRoot.AddComponent<Image>();
        overlay.color = new Color(0, 0, 0, 0.65f);
        StretchFull(overlay.rectTransform);

        var card = MakePanel(_panelRoot, "Card", CardBg, Vector2.zero,
            new Vector2(760, 580), new Vector2(0.5f, 0.5f));

        MakeTxt(card, "Title", "QUESTS", 28, Color.white,
            new Vector2(0.5f, 0.92f), new Vector2(680, 46));

        MakeBtn(card, "btn_close", "X CLOSE",
            new Vector2(0.88f, 0.92f), new Vector2(100, 34),
            new Color(0.25f, 0.25f, 0.4f), new Color(0.38f, 0.38f, 0.58f), Hide);

        BuildTabs(card);

        _resetText = MakeTxt(card, "ResetInfo", "", 12, TextSec,
            new Vector2(0.5f, 0.775f), new Vector2(680, 20));

        BuildScrollView(card);

        _panelRoot.AddComponent<EscapeListener>().OnEscape = Hide;
        _panelRoot.SetActive(false);
    }

    void BuildTabs(GameObject parent)
    {
        float y = 0.845f;
        _tabDailyBtn   = MakeTabBtn(parent, "tab_daily",   "DAILY",   new Vector2(0.30f, y), () => SwitchTab(Tab.Daily));
        _tabWeeklyBtn  = MakeTabBtn(parent, "tab_weekly",  "WEEKLY",  new Vector2(0.50f, y), () => SwitchTab(Tab.Weekly));
        _tabMonthlyBtn = MakeTabBtn(parent, "tab_monthly", "MONTHLY", new Vector2(0.70f, y), () => SwitchTab(Tab.Monthly));
    }

    Button MakeTabBtn(GameObject parent, string name, string label, Vector2 anchor, UnityEngine.Events.UnityAction cb)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var img = go.AddComponent<Image>();
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(cb);
        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.sizeDelta        = new Vector2(160, 34);
        rt.anchoredPosition = Vector2.zero;

        var lbl = MakeTxt(go, "Lbl", label, 13, Color.white, new Vector2(0.5f, 0.5f), Vector2.zero);
        StretchFull(lbl.rectTransform);
        return btn;
    }

    void SwitchTab(Tab tab)
    {
        _currentTab = tab;
        PopulateQuests();
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
        scrollRt.sizeDelta        = new Vector2(720, 380);
        scrollRt.anchoredPosition = new Vector2(0, -60);

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
        vlg.spacing              = 6;
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

    void PopulateQuests()
    {
        foreach (Transform child in _contentParent.transform)
            Destroy(child.gameObject);

        RefreshTabHighlight();

        var mgr = QuestManager.Instance;
        if (mgr == null)
        {
            MakeEmptyRow("Quest system not available.");
            if (_resetText) _resetText.text = "";
            return;
        }

        List<QuestDefinition> quests;
        switch (_currentTab)
        {
            case Tab.Weekly:
                quests = mgr.GetActiveWeeklyQuests();
                _resetText.text = $"Resets in {FormatRemaining(NextWeeklyReset())}";
                break;
            case Tab.Monthly:
                var monthly = mgr.GetActiveMonthlyQuest();
                quests = monthly != null ? new List<QuestDefinition> { monthly } : new List<QuestDefinition>();
                _resetText.text = $"Resets in {FormatRemaining(NextMonthlyReset())}";
                break;
            default:
                quests = mgr.GetActiveDailyQuests();
                _resetText.text = $"Resets in {FormatRemaining(NextDailyReset())}";
                break;
        }

        if (quests == null || quests.Count == 0)
        {
            MakeEmptyRow("No quests active right now.");
            return;
        }

        for (int i = 0; i < quests.Count; i++)
        {
            var def = quests[i];
            if (def == null) continue;

            int  progress  = mgr.GetProgress(def.questId);
            bool completed = mgr.IsCompleted(def.questId);
            BuildRow(def, progress, completed, i % 2 == 0);
        }
    }

    void RefreshTabHighlight()
    {
        SetTabVisual(_tabDailyBtn,   _currentTab == Tab.Daily);
        SetTabVisual(_tabWeeklyBtn,  _currentTab == Tab.Weekly);
        SetTabVisual(_tabMonthlyBtn, _currentTab == Tab.Monthly);
    }

    static void SetTabVisual(Button btn, bool active)
    {
        if (btn == null) return;
        var img = btn.GetComponent<Image>();
        img.color = active ? PrimaryBtn : TabOff;
        btn.colors = ColorBlock(active ? PrimaryBtn : TabOff, active ? PrimaryHover : TabOffHover);
    }

    void BuildRow(QuestDefinition def, int progress, bool completed, bool alt)
    {
        var row = new GameObject($"Row_{def.questId}");
        row.transform.SetParent(_contentParent.transform, false);
        var rowImg = row.AddComponent<Image>();
        rowImg.color = alt ? RowBg : RowBgAlt;

        var rowLE = row.AddComponent<LayoutElement>();
        rowLE.preferredHeight = 78;
        rowLE.minHeight       = 78;

        var rowVLG = row.AddComponent<VerticalLayoutGroup>();
        rowVLG.spacing              = 4;
        rowVLG.padding              = new RectOffset(14, 14, 10, 10);
        rowVLG.childControlWidth    = true;
        rowVLG.childControlHeight   = true;
        rowVLG.childForceExpandWidth  = true;
        rowVLG.childForceExpandHeight = false;

        // Üst satır: isim (sol) + ödül/durum (sağ) — tek satırda iki metin, basitlik için ayrı GO'lar horizontal group ile
        var topRowGO = new GameObject("TopRow");
        topRowGO.transform.SetParent(row.transform, false);
        var topLE = topRowGO.AddComponent<LayoutElement>();
        topLE.preferredHeight = 22;
        var topHLG = topRowGO.AddComponent<HorizontalLayoutGroup>();
        topHLG.childControlWidth = true; topHLG.childControlHeight = true;
        topHLG.childForceExpandWidth = true; topHLG.childForceExpandHeight = false;

        var nameTxt = MakeTxtLE(topRowGO, "Name", def.displayName, 15,
            completed ? CompletedGr : Color.white, TextAlignmentOptions.Left);
        nameTxt.fontStyle = FontStyles.Bold;

        string rewardStr = BuildRewardString(def);
        MakeTxtLE(topRowGO, "Reward", rewardStr, 12, TextSec, TextAlignmentOptions.Right);

        // Açıklama
        MakeTxtLE(row, "Desc", def.description, 11, TextSec, TextAlignmentOptions.Left);

        // Progress bar + sayı
        var barRowGO = new GameObject("BarRow");
        barRowGO.transform.SetParent(row.transform, false);
        var barRowLE = barRowGO.AddComponent<LayoutElement>();
        barRowLE.preferredHeight = 16;
        var barHLG = barRowGO.AddComponent<HorizontalLayoutGroup>();
        barHLG.spacing = 8;
        barHLG.childControlWidth = true; barHLG.childControlHeight = true;
        barHLG.childForceExpandWidth = false; barHLG.childForceExpandHeight = true;

        var barBgGO = new GameObject("BarBg");
        barBgGO.transform.SetParent(barRowGO.transform, false);
        var barBgLE = barBgGO.AddComponent<LayoutElement>();
        barBgLE.flexibleWidth = 1;
        var barBg = barBgGO.AddComponent<Image>();
        barBg.color = new Color(0.2f, 0.2f, 0.32f);

        int clamped = Mathf.Clamp(progress, 0, Mathf.Max(1, def.targetValue));
        float fill = def.targetValue > 0 ? (float)clamped / def.targetValue : 0f;
        var fillGO  = new GameObject("Fill");
        fillGO.transform.SetParent(barBgGO.transform, false);
        var fillImg = fillGO.AddComponent<Image>();
        fillImg.color = completed ? CompletedGr : PrimaryBtn;
        var fillRt = fillImg.rectTransform;
        fillRt.anchorMin = new Vector2(0, 0);
        fillRt.anchorMax = new Vector2(Mathf.Clamp01(fill), 1);
        fillRt.offsetMin = fillRt.offsetMax = Vector2.zero;

        var countCol = ColFixed(barRowGO, "Count", 70);
        MakeTxt(countCol, "CountLbl",
            completed ? "DONE" : $"{clamped}/{def.targetValue}",
            12, completed ? CompletedGr : TextSec,
            new Vector2(0.5f, 0.5f), Vector2.zero)
            .rectTransform.Let(rt => StretchFull(rt));
    }

    static string BuildRewardString(QuestDefinition def)
    {
        var parts = new List<string>();
        if (def.rewardXP   > 0) parts.Add($"+{def.rewardXP} XP");
        if (def.rewardGold > 0) parts.Add($"+{def.rewardGold} Gold");
        if (def.rewardGem  > 0) parts.Add($"+{def.rewardGem} Gem");
        return string.Join(" · ", parts);
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
    //  RESET ZAMANI HESAPLAMA (QuestManager ile aynı UTC mantığı)
    // ════════════════════════════════════════════════════════════════════

    static TimeSpan NextDailyReset()
    {
        DateTime now = DateTime.UtcNow;
        DateTime nextMidnight = now.Date.AddDays(1);
        return nextMidnight - now;
    }

    static TimeSpan NextWeeklyReset()
    {
        DateTime now = DateTime.UtcNow;
        int daysUntilMonday = ((int)DayOfWeek.Monday - (int)now.DayOfWeek + 7) % 7;
        if (daysUntilMonday == 0) daysUntilMonday = 7;
        DateTime nextMonday = now.Date.AddDays(daysUntilMonday);
        return nextMonday - now;
    }

    static TimeSpan NextMonthlyReset()
    {
        DateTime now = DateTime.UtcNow;
        DateTime firstOfNextMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1);
        return firstOfNextMonth - now;
    }

    static string FormatRemaining(TimeSpan span)
    {
        if (span.TotalDays >= 1)
            return $"{(int)span.TotalDays}d {span.Hours}h";
        return $"{span.Hours}h {span.Minutes}m";
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
}
