// Assets/Scripts/UI/LeaderboardPanelUI.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Leaderboards.Models;
using CosmicRumble.Cloud;
using CosmicRumble.Localization;

/// <summary>
/// Ana menüdeki LEADERBOARD butonu → global online galibiyet sıralaması paneli.
/// AchievementsPanelUI ile aynı programatik Canvas kalıbı. MainMenuUI bootstrap'te oluşturur.
/// Veri UGS Leaderboards'tan async çekilir; servis yoksa/boşsa bilgilendirici satır gösterilir.
/// </summary>
public class LeaderboardPanelUI : MonoBehaviour
{
    public static LeaderboardPanelUI Instance { get; private set; }

    // ── Renk paleti (AchievementsPanelUI ile uyumlu) ──────────────────────
    static readonly Color CardBg       = new Color(0.09f,  0.09f,  0.18f,  1f);
    static readonly Color PrimaryBtn   = new Color(0.29f,  0.62f,  1.00f,  1f);
    static readonly Color PrimaryHover = new Color(0.42f,  0.71f,  1.00f,  1f);
    static readonly Color RowBg        = new Color(0.11f,  0.11f,  0.20f,  1f);
    static readonly Color RowBgAlt     = new Color(0.13f,  0.13f,  0.24f,  1f);
    static readonly Color OwnRowBg     = new Color(0.16f,  0.28f,  0.16f,  1f);
    static readonly Color GoldRank     = new Color(1.00f,  0.722f, 0.00f,  1f);
    static readonly Color SilverRank   = new Color(0.75f,  0.75f,  0.80f,  1f);
    static readonly Color BronzeRank   = new Color(0.80f,  0.50f,  0.20f,  1f);
    static readonly Color TextSec      = new Color(0.533f, 0.533f, 0.667f, 1f);

    const int TopEntryCount = 50;

    GameObject      _panelRoot;
    GameObject      _contentParent;
    TextMeshProUGUI _statusText;
    bool            _loading;

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
        _ = PopulateAsync();
    }

    public void Hide() => _panelRoot.SetActive(false);

    // ════════════════════════════════════════════════════════════════════
    //  UI BUILD
    // ════════════════════════════════════════════════════════════════════

    void BuildUI()
    {
        var canvasGO = new GameObject("LeaderboardCanvas");
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 46;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // Panel overlay (başta gizli)
        _panelRoot = new GameObject("LeaderboardPanel");
        _panelRoot.transform.SetParent(canvasGO.transform, false);
        var overlay = _panelRoot.AddComponent<Image>();
        overlay.color = new Color(0, 0, 0, 0.65f);
        StretchFull(overlay.rectTransform);

        var card = MakePanel(_panelRoot, "Card", CardBg, Vector2.zero,
            new Vector2(820, 600), new Vector2(0.5f, 0.5f));
        UiKit.Round(card.GetComponent<Image>());
        UiKit.Shadow(card, 8f, 0.55f);
        UiKit.Stroke(card, new Color(1f, 1f, 1f, 0.09f));
        UiKit.Pop(card);

        var title = MakeTxt(card, "Title", Loc.T("LEADERBOARD"), 30, GoldRank,
            new Vector2(0.5f, 0.925f), new Vector2(600, 46));
        title.fontStyle = FontStyles.Bold;

        UiKit.CloseButton(card, Hide);

        MakeBtn(card, "btn_refresh", Loc.T("REFRESH"),
            new Vector2(0.11f, 0.925f), new Vector2(120, 46),
            PrimaryBtn, PrimaryHover, () => { if (!_loading) _ = PopulateAsync(); });

        _statusText = MakeTxt(card, "Status", "", 13, TextSec,
            new Vector2(0.5f, 0.83f), new Vector2(600, 22));

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
        scrollRt.sizeDelta        = new Vector2(770, 430);
        scrollRt.anchoredPosition = new Vector2(0, -44);

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

    async System.Threading.Tasks.Task PopulateAsync()
    {
        _loading = true;
        ClearRows();
        if (_statusText) _statusText.text = Loc.T("Loading...");

        var mgr = LeaderboardManager.Instance;
        if (mgr == null ||
            UnityServices.State != ServicesInitializationState.Initialized ||
            !AuthenticationService.Instance.IsSignedIn)
        {
            MakeEmptyRow(Loc.T("Can't reach online services — check your connection."));
            if (_statusText) _statusText.text = "";
            _loading = false;
            return;
        }

        // Gerçek kullanıcı adı leaderboard'a yansısın (misafirse no-op)
        await mgr.SyncPlayerNameAsync();

        var entries = await mgr.FetchTopAsync(TopEntryCount);
        var own     = await mgr.FetchOwnEntryAsync();

        // Panel bu arada kapatılmış/yok edilmiş olabilir
        if (this == null || _contentParent == null) return;

        ClearRows();

        if (entries.Count == 0)
        {
            MakeEmptyRow(Loc.T("No trophies yet — play an online match to show up here!"));
        }
        else
        {
            string ownId = AuthenticationService.Instance.PlayerId;
            for (int i = 0; i < entries.Count; i++)
                BuildRow(entries[i], entries[i].PlayerId == ownId, i % 2 == 0);
        }

        if (_statusText)
        {
            _statusText.text = own != null
                ? string.Format(Loc.T("Your rank: #{0}   •   {1} trophies   •   {2}"), own.Rank + 1, (int)own.Score, LeaderboardManager.GetLeagueName((int)own.Score))
                : string.Format(Loc.T("No ranked trophies yet   •   Local: {0} trophies"), mgr.Trophies);
        }

        _loading = false;
    }

    void ClearRows()
    {
        if (_contentParent == null) return;
        foreach (Transform child in _contentParent.transform)
            Destroy(child.gameObject);
    }

    // ── Satır: [Rank 70] [Name flex] [Wins 110] ──────────────────────────
    void BuildRow(LeaderboardEntry entry, bool isOwn, bool alt)
    {
        int rank = entry.Rank + 1; // UGS Rank 0 tabanlı

        var row = new GameObject($"Row_{rank}");
        row.transform.SetParent(_contentParent.transform, false);
        var rowImg = row.AddComponent<Image>();
        rowImg.color = isOwn ? OwnRowBg : (alt ? RowBg : RowBgAlt);
        UiKit.Round(rowImg, 2f);

        var rowLE = row.AddComponent<LayoutElement>();
        rowLE.preferredHeight = 52;
        rowLE.minHeight       = 52;

        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.padding              = new RectOffset(12, 12, 0, 0);
        hlg.childControlWidth    = true;
        hlg.childControlHeight   = true;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = true;

        Color rankColor = rank switch
        {
            1 => GoldRank,
            2 => SilverRank,
            3 => BronzeRank,
            _ => TextSec
        };

        var rankCol = ColFixed(row, "RankCol", 70);
        var rankTxt = MakeTxt(rankCol, "Rank", $"#{rank}", 17, rankColor,
            new Vector2(0.5f, 0.5f), Vector2.zero);
        rankTxt.fontStyle = rank <= 3 ? FontStyles.Bold : FontStyles.Normal;
        StretchFull(rankTxt.rectTransform);

        var nameCol = ColFlex(row, "NameCol", 1f);
        var nameTxt = MakeTxt(nameCol, "Name", CleanName(entry.PlayerName), 15,
            isOwn ? Color.white : new Color(0.85f, 0.85f, 0.95f, 1f),
            new Vector2(0.5f, 0.5f), Vector2.zero);
        nameTxt.alignment = TextAlignmentOptions.Left;
        nameTxt.fontStyle = isOwn ? FontStyles.Bold : FontStyles.Normal;
        StretchFull(nameTxt.rectTransform);

        // Lig adı (kupa aralığına göre — Clash Royale arena karşılığı)
        var leagueCol = ColFixed(row, "LeagueCol", 130);
        var leagueTxt = MakeTxt(leagueCol, "League",
            LeaderboardManager.GetLeagueName((int)entry.Score), 12,
            isOwn ? new Color(0.75f, 0.95f, 0.75f, 1f) : TextSec,
            new Vector2(0.5f, 0.5f), Vector2.zero);
        StretchFull(leagueTxt.rectTransform);

        var trophyCol = ColFixed(row, "TrophyCol", 110);
        var trophyTxt = MakeTxt(trophyCol, "Trophies", $"{(int)entry.Score}", 16,
            isOwn ? Color.white : GoldRank, new Vector2(0.5f, 0.5f), Vector2.zero);
        trophyTxt.fontStyle = FontStyles.Bold;
        trophyTxt.alignment = TextAlignmentOptions.Right;
        StretchFull(trophyTxt.rectTransform);
    }

    /// <summary>UGS anonim adlarındaki "#1234" ekini gizle; boş adlara yer tutucu ver.</summary>
    static string CleanName(string playerName)
    {
        if (string.IsNullOrEmpty(playerName)) return Loc.T("Player");
        int hash = playerName.IndexOf('#');
        return hash > 0 ? playerName.Substring(0, hash) : playerName;
    }

    void MakeEmptyRow(string msg)
    {
        var go = new GameObject("EmptyRow");
        go.transform.SetParent(_contentParent.transform, false);
        var emptyImg = go.AddComponent<Image>();
        emptyImg.color = RowBg;
        UiKit.Round(emptyImg, 2f);
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 60;

        var txt = MakeTxt(go, "Msg", msg, 15, TextSec,
            new Vector2(0.5f, 0.5f), Vector2.zero);
        StretchFull(txt.rectTransform);
    }

    // ════════════════════════════════════════════════════════════════════
    //  UI HELPERS (AchievementsPanelUI ile aynı kalıp)
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
        go.AddComponent<LayoutElement>().flexibleWidth = flex;
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
        txt.text         = content;
        txt.fontSize     = size;
        txt.color        = color;
        txt.alignment    = TextAlignmentOptions.Center;
        txt.overflowMode = TextOverflowModes.Ellipsis;
        var rt = txt.rectTransform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.sizeDelta        = sizeDelta;
        rt.anchoredPosition = Vector2.zero;
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
        UiKit.Round(img, 1.3f);
        UiKit.Shadow(go, 3f, 0.35f);
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.colors = UiKit.ButtonColors(normal);
        btn.onClick.AddListener(cb);
        UiKit.Press(go);
        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.sizeDelta        = size;
        rt.anchoredPosition = Vector2.zero;

        var lblGO = new GameObject("Lbl");
        lblGO.transform.SetParent(go.transform, false);
        var lbl = lblGO.AddComponent<TextMeshProUGUI>();
        lbl.text      = label;
        lbl.fontSize  = 13;
        lbl.color     = Color.white;
        lbl.alignment = TextAlignmentOptions.Center;
        var lrt = lbl.rectTransform;
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = lrt.offsetMax = Vector2.zero;
    }

    static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
