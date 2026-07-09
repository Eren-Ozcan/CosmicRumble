using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CosmicRumble.Economy;
using CosmicRumble.Localization;

/// <summary>
/// Ana menüdeki GÖREVLER butonu → günlük/haftalık/aylık görev paneli.
/// UiKit stiliyle (yuvarlatık kart + pill sekmeler + pop animasyonu + köşe X) programatik Canvas kurar.
/// </summary>
public class QuestsPanelUI : MonoBehaviour
{
    public static QuestsPanelUI Instance { get; private set; }

    // ── Renk paleti (UiKit mobil teması) ──────────────────────────────────
    static readonly Color CardBg       = new Color(0.07f,  0.07f,  0.16f,  0.97f);
    static readonly Color PrimaryBtn   = new Color(0.12f,  0.68f,  0.22f,  1f);
    static readonly Color TabOff       = new Color(0.15f,  0.15f,  0.27f,  1f);
    static readonly Color RowBg        = new Color(0.11f,  0.11f,  0.21f,  1f);
    static readonly Color RowBgAlt     = new Color(0.13f,  0.13f,  0.25f,  1f);
    static readonly Color CompletedGr  = new Color(0.30f,  0.85f,  0.40f,  1f);
    static readonly Color TextSec      = new Color(0.533f, 0.533f, 0.667f, 1f);
    static readonly Color StrokeCol    = new Color(1f, 1f, 1f, 0.09f);

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
            new Vector2(820, 600), new Vector2(0.5f, 0.5f));
        UiKit.Round(card.GetComponent<Image>());
        UiKit.Shadow(card, 8f, 0.55f);
        UiKit.Stroke(card, StrokeCol);
        UiKit.Pop(card);

        var title = MakeTxt(card, "Title", Loc.T("QUESTS"), 30, CompletedGr,
            new Vector2(0.5f, 0.925f), new Vector2(680, 46));
        title.fontStyle = FontStyles.Bold;

        UiKit.CloseButton(card, Hide);

        BuildTabs(card);

        _resetText = MakeTxt(card, "ResetInfo", "", 13, TextSec,
            new Vector2(0.5f, 0.765f), new Vector2(680, 20));

        BuildScrollView(card);

        _panelRoot.AddComponent<EscapeListener>().OnEscape = Hide;
        _panelRoot.SetActive(false);
    }

    void BuildTabs(GameObject parent)
    {
        float y = 0.845f;
        _tabDailyBtn   = MakeTabBtn(parent, "tab_daily",   Loc.T("DAILY"),   new Vector2(0.29f, y), () => SwitchTab(Tab.Daily));
        _tabWeeklyBtn  = MakeTabBtn(parent, "tab_weekly",  Loc.T("WEEKLY"), new Vector2(0.50f, y), () => SwitchTab(Tab.Weekly));
        _tabMonthlyBtn = MakeTabBtn(parent, "tab_monthly", Loc.T("MONTHLY"),    new Vector2(0.71f, y), () => SwitchTab(Tab.Monthly));
    }

    Button MakeTabBtn(GameObject parent, string name, string label, Vector2 anchor, UnityEngine.Events.UnityAction cb)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var img = go.AddComponent<Image>();
        UiKit.Round(img, 1.1f); // pill görünüm
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(cb);
        UiKit.Press(go);
        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.sizeDelta        = new Vector2(164, 44);
        rt.anchoredPosition = Vector2.zero;

        var lbl = MakeTxt(go, "Lbl", label, 15, Color.white, new Vector2(0.5f, 0.5f), Vector2.zero);
        lbl.fontStyle = FontStyles.Bold;
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
        scrollRt.sizeDelta        = new Vector2(770, 400);
        scrollRt.anchoredPosition = new Vector2(0, -70);

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

    void PopulateQuests()
    {
        foreach (Transform child in _contentParent.transform)
            Destroy(child.gameObject);

        RefreshTabHighlight();

        var mgr = QuestManager.Instance;
        if (mgr == null)
        {
            MakeEmptyRow(Loc.T("The quest system is currently unavailable."));
            if (_resetText) _resetText.text = "";
            return;
        }

        List<QuestDefinition> quests;
        switch (_currentTab)
        {
            case Tab.Weekly:
                quests = mgr.GetActiveWeeklyQuests();
                _resetText.text = string.Format(Loc.T("Resets in {0}"), FormatRemaining(NextWeeklyReset()));
                break;
            case Tab.Monthly:
                var monthly = mgr.GetActiveMonthlyQuest();
                quests = monthly != null ? new List<QuestDefinition> { monthly } : new List<QuestDefinition>();
                _resetText.text = string.Format(Loc.T("Resets in {0}"), FormatRemaining(NextMonthlyReset()));
                break;
            default:
                quests = mgr.GetActiveDailyQuests();
                _resetText.text = string.Format(Loc.T("Resets in {0}"), FormatRemaining(NextDailyReset()));
                break;
        }

        if (quests == null || quests.Count == 0)
        {
            MakeEmptyRow(Loc.T("No active quests right now."));
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
        Color c = active ? PrimaryBtn : TabOff;
        img.color  = c;
        btn.colors = UiKit.ButtonColors(c);
    }

    void BuildRow(QuestDefinition def, int progress, bool completed, bool alt)
    {
        var row = new GameObject($"Row_{def.questId}");
        row.transform.SetParent(_contentParent.transform, false);
        var rowImg = row.AddComponent<Image>();
        rowImg.color = alt ? RowBg : RowBgAlt;
        UiKit.Round(rowImg, 2f);

        var rowLE = row.AddComponent<LayoutElement>();
        rowLE.preferredHeight = 88;
        rowLE.minHeight       = 88;

        var rowVLG = row.AddComponent<VerticalLayoutGroup>();
        rowVLG.spacing              = 5;
        rowVLG.padding              = new RectOffset(16, 16, 12, 12);
        rowVLG.childControlWidth    = true;
        rowVLG.childControlHeight   = true;
        rowVLG.childForceExpandWidth  = true;
        rowVLG.childForceExpandHeight = false;

        // Üst satır: isim (sol) + ödül (sağ)
        var topRowGO = new GameObject("TopRow");
        topRowGO.transform.SetParent(row.transform, false);
        var topLE = topRowGO.AddComponent<LayoutElement>();
        topLE.preferredHeight = 24;
        var topHLG = topRowGO.AddComponent<HorizontalLayoutGroup>();
        topHLG.childControlWidth = true; topHLG.childControlHeight = true;
        topHLG.childForceExpandWidth = true; topHLG.childForceExpandHeight = false;

        var nameTxt = MakeTxtLE(topRowGO, "Name", Loc.T(def.displayName), 16,
            completed ? CompletedGr : Color.white, TextAlignmentOptions.Left);
        nameTxt.fontStyle = FontStyles.Bold;

        string rewardStr = BuildRewardString(def);
        MakeTxtLE(topRowGO, "Reward", rewardStr, 13, new Color(1f, 0.80f, 0.20f, 1f),
            TextAlignmentOptions.Right);

        // Açıklama
        MakeTxtLE(row, "Desc", Loc.T(def.description), 12, TextSec, TextAlignmentOptions.Left);

        // Progress bar + sayı
        var barRowGO = new GameObject("BarRow");
        barRowGO.transform.SetParent(row.transform, false);
        var barRowLE = barRowGO.AddComponent<LayoutElement>();
        barRowLE.preferredHeight = 16;
        var barHLG = barRowGO.AddComponent<HorizontalLayoutGroup>();
        barHLG.spacing = 10;
        barHLG.childControlWidth = true; barHLG.childControlHeight = true;
        barHLG.childForceExpandWidth = false; barHLG.childForceExpandHeight = true;

        var barBgGO = new GameObject("BarBg");
        barBgGO.transform.SetParent(barRowGO.transform, false);
        var barBgLE = barBgGO.AddComponent<LayoutElement>();
        barBgLE.flexibleWidth = 1;
        var barBg = barBgGO.AddComponent<Image>();
        barBg.color = new Color(0.2f, 0.2f, 0.32f);
        UiKit.Round(barBg, 4f);

        int clamped = Mathf.Clamp(progress, 0, Mathf.Max(1, def.targetValue));
        float fill = def.targetValue > 0 ? Mathf.Clamp01((float)clamped / def.targetValue) : 0f;
        if (fill > 0.001f)
        {
            var fillGO  = new GameObject("Fill");
            fillGO.transform.SetParent(barBgGO.transform, false);
            var fillImg = fillGO.AddComponent<Image>();
            fillImg.color = completed ? CompletedGr : PrimaryBtn;
            UiKit.Round(fillImg, 4f);
            var fillRt = fillImg.rectTransform;
            fillRt.anchorMin = new Vector2(0, 0);
            fillRt.anchorMax = new Vector2(fill, 1);
            fillRt.offsetMin = fillRt.offsetMax = Vector2.zero;
        }

        var countCol = ColFixed(barRowGO, "Count", 80);
        MakeTxt(countCol, "CountLbl",
            completed ? Loc.T("DONE") : $"{clamped}/{def.targetValue}",
            13, completed ? CompletedGr : TextSec,
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
            return string.Format(Loc.T("{0}d {1}h"), (int)span.TotalDays, span.Hours);
        return string.Format(Loc.T("{0}h {1}m"), span.Hours, span.Minutes);
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

    static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
