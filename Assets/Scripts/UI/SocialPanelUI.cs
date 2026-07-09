using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Services.Friends.Models;
using CosmicRumble.Social;
using CosmicRumble.Localization;

/// <summary>
/// SOSYAL paneli (Brawl Stars'ın Sosyal ekranı karşılığı):
/// - Üst şerit: kendi arkadaş kodun ("Nova731#1234") + KOPYALA.
/// - Sekmeler: ARKADAŞLAR (ID ile ekle + arkadaş listesi, presence/kupa, OYUNA DAVET ET/ÇIKAR)
///   ve DAVETLER (gelen istekler, KABUL/REDDET; sekme başlığında rozet sayısı).
/// FriendsManager kullanılamıyorsa bilgi mesajı gösterir. Canvas order 40.
/// </summary>
public class SocialPanelUI : MonoBehaviour
{
    public static SocialPanelUI Instance { get; private set; }

    static readonly Color BgColor    = new Color(0.051f, 0.051f, 0.102f, 0.97f);
    static readonly Color PlateDark  = new Color(0.165f, 0.175f, 0.215f, 1f);
    static readonly Color RowBg      = new Color(0.09f,  0.09f,  0.18f,  1f);
    static readonly Color AccGold    = new Color(1.00f,  0.80f,  0.20f,  1f);
    static readonly Color AccGreen   = new Color(0.13f,  0.72f,  0.35f,  1f);
    static readonly Color AccRed     = new Color(0.72f,  0.18f,  0.18f,  1f);
    static readonly Color AccBlue    = new Color(0.29f,  0.62f,  1.00f,  1f);
    static readonly Color TabIdle    = new Color(0.13f,  0.13f,  0.24f,  1f);
    static readonly Color TextSec    = new Color(0.533f, 0.533f, 0.667f, 1f);
    static readonly Color OnlineDot  = new Color(0.25f,  0.85f,  0.35f,  1f);
    static readonly Color AwayDot    = new Color(0.95f,  0.75f,  0.20f,  1f);
    static readonly Color OfflineDot = new Color(0.45f,  0.45f,  0.55f,  1f);

    GameObject      _panelRoot;
    TextMeshProUGUI _ownCodeText;
    TextMeshProUGUI _copyToast;
    TextMeshProUGUI _addStatusText;
    TextMeshProUGUI _emptyText;
    TextMeshProUGUI _requestsBadge;
    TMP_InputField  _addInput;
    Image           _friendsTabImg, _requestsTabImg;
    GameObject      _addRow;
    RectTransform   _listContent;
    bool            _showingRequests;
    string          _pendingRemoveId; // inline "emin misin" onayı için

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        BuildUI();
        _panelRoot.SetActive(false);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        var fm = FriendsManager.Instance;
        if (fm != null)
        {
            fm.OnRelationshipsChanged -= RefreshList;
            fm.OnPresenceUpdated      -= RefreshList;
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  PUBLIC API
    // ════════════════════════════════════════════════════════════════════

    public void Show()
    {
        _panelRoot.SetActive(true);
        _showingRequests = false;
        _pendingRemoveId = null;
        _addStatusText.text = "";
        _addInput.text = "";
        _copyToast.text = "";

        var fm = FriendsManager.Instance;
        if (fm != null)
        {
            fm.OnRelationshipsChanged -= RefreshList;
            fm.OnRelationshipsChanged += RefreshList;
            fm.OnPresenceUpdated      -= RefreshList;
            fm.OnPresenceUpdated      += RefreshList;
            // Panel açıkken init tamamlanırsa liste event ile tazelenir
            _ = fm.EnsureInitializedAsync();
        }

        RefreshTabs();
        RefreshList();
        _ = RefreshOwnCodeAsync();
    }

    public void Hide()
    {
        var fm = FriendsManager.Instance;
        if (fm != null)
        {
            fm.OnRelationshipsChanged -= RefreshList;
            fm.OnPresenceUpdated      -= RefreshList;
        }
        _panelRoot.SetActive(false);
    }

    // ════════════════════════════════════════════════════════════════════
    //  CALLBACKS
    // ════════════════════════════════════════════════════════════════════

    async System.Threading.Tasks.Task RefreshOwnCodeAsync()
    {
        _ownCodeText.text = Loc.T("YOUR ID: ...");
        var fm = FriendsManager.Instance;
        string code = fm != null ? await fm.GetOwnFriendCodeAsync() : null;
        if (_ownCodeText != null)
            _ownCodeText.text = string.IsNullOrEmpty(code) ? Loc.T("YOUR ID: —") : string.Format(Loc.T("YOUR ID: {0}"), code);
        _cachedOwnCode = code;
    }

    string _cachedOwnCode;

    void OnCopyClicked()
    {
        if (string.IsNullOrEmpty(_cachedOwnCode)) return;
        GUIUtility.systemCopyBuffer = _cachedOwnCode;
        StartCoroutine(ShowToast(Loc.T("Copied!")));
    }

    IEnumerator ShowToast(string msg)
    {
        _copyToast.text = msg;
        yield return new WaitForSeconds(1.5f);
        if (_copyToast != null) _copyToast.text = "";
    }

    async void OnAddClicked()
    {
        var fm = FriendsManager.Instance;
        if (fm == null) return;
        string code = _addInput.text;
        _addStatusText.color = TextSec;
        _addStatusText.text  = Loc.T("Sending...");

        var (ok, error) = await fm.AddFriendByCodeAsync(code);
        if (ok)
        {
            _addStatusText.color = AccGreen;
            _addStatusText.text  = Loc.T("Request sent!");
            _addInput.text = "";
        }
        else
        {
            _addStatusText.color = new Color(1f, 0.35f, 0.35f);
            _addStatusText.text  = error;
        }
    }

    void OnTabClicked(bool requests)
    {
        _showingRequests = requests;
        _pendingRemoveId = null;
        RefreshTabs();
        RefreshList();
    }

    // ════════════════════════════════════════════════════════════════════
    //  LIST BUILD
    // ════════════════════════════════════════════════════════════════════

    void RefreshTabs()
    {
        if (_friendsTabImg  != null) _friendsTabImg.color  = _showingRequests ? TabIdle : AccBlue;
        if (_requestsTabImg != null) _requestsTabImg.color = _showingRequests ? AccBlue : TabIdle;
        if (_addRow != null) _addRow.SetActive(!_showingRequests);

        var fm = FriendsManager.Instance;
        int incoming = fm != null ? fm.IncomingRequests.Count : 0;
        if (_requestsBadge != null)
        {
            _requestsBadge.text = incoming > 0 ? incoming.ToString() : "";
            _requestsBadge.transform.parent.gameObject.SetActive(incoming > 0);
        }
    }

    void RefreshList()
    {
        if (_listContent == null || !_panelRoot.activeSelf) return;

        foreach (Transform child in _listContent)
            Destroy(child.gameObject);

        var fm = FriendsManager.Instance;
        if (fm == null || !fm.IsAvailable)
        {
            _emptyText.text = Loc.T("Friends system is currently unavailable.");
            _emptyText.gameObject.SetActive(true);
            RefreshTabs();
            return;
        }

        IReadOnlyList<Relationship> rows = _showingRequests ? fm.IncomingRequests : fm.Friends;
        _emptyText.gameObject.SetActive(rows.Count == 0);
        _emptyText.text = _showingRequests
            ? Loc.T("No pending requests.")
            : Loc.T("You don't have any friends yet — add one by ID!");

        foreach (var rel in rows)
            BuildRow(rel);

        RefreshTabs();
    }

    void BuildRow(Relationship rel)
    {
        string memberId = rel.Member?.Id;
        string name     = rel.Member?.Profile?.Name ?? "???";
        // "Nova731#1234" → listede sade "Nova731" göster
        int hashIdx = name.IndexOf('#');
        string shownName = hashIdx > 0 ? name.Substring(0, hashIdx) : name;

        var row = new GameObject("Row_" + memberId);
        row.transform.SetParent(_listContent, false);
        var rowImg = row.AddComponent<Image>();
        rowImg.color = RowBg;
        UiKit.Round(rowImg, 1.2f);
        var le = row.AddComponent<LayoutElement>();
        le.minHeight = le.preferredHeight = 78;

        // Presence bilgisi
        bool online = false, inMatch = false;
        string statusLine;
        if (_showingRequests)
        {
            statusLine = Loc.T("Wants to add you as a friend");
        }
        else
        {
            var (availability, activity) = ReadPresence(rel);
            online  = availability == Availability.Online;
            bool away = availability == Availability.Away || availability == Availability.Busy;
            inMatch = activity != null && activity.status == "in_match";

            string state = online ? (inMatch ? Loc.T("In Match") : Loc.T("Online"))
                         : away   ? Loc.T("Away")
                                  : Loc.T("Offline");
            statusLine = activity != null ? string.Format(Loc.T("{0}  •  {1} trophies"), state, activity.trophies) : state;

            // Durum noktası
            var dot = new GameObject("Dot");
            dot.transform.SetParent(row.transform, false);
            var dotImg = dot.AddComponent<Image>();
            dotImg.sprite = UiKit.CircleSprite;
            dotImg.color  = online ? OnlineDot : away ? AwayDot : OfflineDot;
            dotImg.raycastTarget = false;
            var dotRt = dotImg.rectTransform;
            dotRt.anchorMin = dotRt.anchorMax = new Vector2(0f, 0.5f);
            dotRt.sizeDelta = new Vector2(16, 16);
            dotRt.anchoredPosition = new Vector2(28, 0);
        }

        var nameTxt = MakeText(row, "Name", shownName, 20,
            new Vector2(0f, 0.5f), new Vector2(320, 28), Color.white);
        nameTxt.alignment = TextAlignmentOptions.Left;
        nameTxt.rectTransform.anchoredPosition = new Vector2(210, 14);
        UiKit.BrawlText(nameTxt);

        var subTxt = MakeText(row, "Sub", statusLine, 13,
            new Vector2(0f, 0.5f), new Vector2(360, 20), TextSec);
        subTxt.alignment = TextAlignmentOptions.Left;
        subTxt.rectTransform.anchoredPosition = new Vector2(230, -14);

        if (_showingRequests)
        {
            MakeRowButton(row, "btn_accept", Loc.T("ACCEPT"), AccGreen, new Vector2(-190, 0), 130,
                async () =>
                {
                    var (ok, err) = await FriendsManager.Instance.AcceptRequestAsync(memberId);
                    if (!ok) { _addStatusText.color = new Color(1f, 0.35f, 0.35f); _addStatusText.text = err; }
                });
            MakeRowButton(row, "btn_decline", Loc.T("DECLINE"), AccRed, new Vector2(-55, 0), 120,
                async () => await FriendsManager.Instance.DeclineRequestAsync(memberId));
        }
        else
        {
            bool canInvite = online && !inMatch;
            var inviteBtn = MakeRowButton(row, "btn_invite", Loc.T("INVITE TO GAME"),
                canInvite ? AccGreen : new Color(0.25f, 0.28f, 0.32f, 1f),
                new Vector2(-230, 0), 210,
                () =>
                {
                    Hide();
                    FriendLobbyPanelUI.Instance?.ShowAsHost(memberId, shownName);
                });
            inviteBtn.GetComponent<Button>().interactable = canInvite;

            bool pendingRemove = _pendingRemoveId == memberId;
            MakeRowButton(row, "btn_remove", pendingRemove ? Loc.T("ARE YOU SURE?") : Loc.T("REMOVE"),
                pendingRemove ? AccRed : new Color(0.35f, 0.20f, 0.20f, 1f),
                new Vector2(-60, 0), 130,
                async () =>
                {
                    if (_pendingRemoveId != memberId)
                    {
                        _pendingRemoveId = memberId; // ilk tık: onay iste
                        RefreshList();
                        return;
                    }
                    _pendingRemoveId = null;
                    await FriendsManager.Instance.RemoveFriendAsync(memberId);
                });
        }
    }

    static (Availability, PresenceActivity) ReadPresence(Relationship rel)
    {
        try
        {
            var presence = rel.Member?.Presence;
            if (presence == null) return (Availability.Offline, null);
            PresenceActivity activity = null;
            try { activity = presence.GetActivity<PresenceActivity>(); } catch { }
            return (presence.Availability, activity);
        }
        catch { return (Availability.Offline, null); }
    }

    // ════════════════════════════════════════════════════════════════════
    //  UI BUILD
    // ════════════════════════════════════════════════════════════════════

    void BuildUI()
    {
        var canvasGO = new GameObject("SocialCanvas");
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 40;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        _panelRoot = new GameObject("SocialRoot");
        _panelRoot.transform.SetParent(canvasGO.transform, false);
        var overlay = _panelRoot.AddComponent<Image>();
        overlay.color = BgColor;
        var overlayRt = overlay.rectTransform;
        overlayRt.anchorMin = Vector2.zero;
        overlayRt.anchorMax = Vector2.one;
        overlayRt.offsetMin = overlayRt.offsetMax = Vector2.zero;

        var title = MakeText(_panelRoot, "Title", Loc.T("SOCIAL"), 34,
            new Vector2(0.5f, 0.94f), new Vector2(400, 48), AccGold);
        UiKit.BrawlText(title);

        // ── Üst şerit: kendi ID + kopyala ────────────────────────────────
        var idPlate = new GameObject("IdPlate");
        idPlate.transform.SetParent(_panelRoot.transform, false);
        var idImg = idPlate.AddComponent<Image>();
        idImg.color = PlateDark;
        UiKit.Round(idImg, 1.2f);
        var idRt = idImg.rectTransform;
        idRt.anchorMin = idRt.anchorMax = new Vector2(0.5f, 0.845f);
        idRt.sizeDelta = new Vector2(700, 58);
        idRt.anchoredPosition = Vector2.zero;

        _ownCodeText = MakeText(idPlate, "OwnCode", Loc.T("YOUR ID: ..."), 19,
            new Vector2(0.5f, 0.5f), new Vector2(480, 30), Color.white);
        _ownCodeText.alignment = TextAlignmentOptions.Left;
        _ownCodeText.rectTransform.anchoredPosition = new Vector2(-70, 0);

        MakeRowButton(idPlate, "btn_copy", Loc.T("COPY"), AccBlue, new Vector2(-80, 0), 140, OnCopyClicked);

        _copyToast = MakeText(_panelRoot, "CopyToast", "", 15,
            new Vector2(0.5f, 0.795f), new Vector2(300, 24), AccGreen);

        // ── Sekmeler ─────────────────────────────────────────────────────
        _friendsTabImg  = MakeTabButton("tab_friends", Loc.T("FRIENDS"), new Vector2(0.36f, 0.745f),
            () => OnTabClicked(false));
        _requestsTabImg = MakeTabButton("tab_requests", Loc.T("REQUESTS"), new Vector2(0.64f, 0.745f),
            () => OnTabClicked(true));

        // Rozet (gelen istek sayısı)
        var badgeGO = new GameObject("Badge");
        badgeGO.transform.SetParent(_requestsTabImg.transform, false);
        var badgeImg = badgeGO.AddComponent<Image>();
        badgeImg.sprite = UiKit.CircleSprite;
        badgeImg.color  = AccRed;
        badgeImg.raycastTarget = false;
        var badgeRt = badgeImg.rectTransform;
        badgeRt.anchorMin = badgeRt.anchorMax = new Vector2(1f, 1f);
        badgeRt.sizeDelta = new Vector2(30, 30);
        badgeRt.anchoredPosition = new Vector2(-6, -2);
        _requestsBadge = MakeText(badgeGO, "n", "", 15,
            new Vector2(0.5f, 0.5f), new Vector2(30, 30), Color.white);
        badgeGO.SetActive(false);

        // ── ID ile ekle satırı (sadece ARKADAŞLAR sekmesinde) ────────────
        _addRow = new GameObject("AddRow");
        _addRow.transform.SetParent(_panelRoot.transform, false);
        var addRt = _addRow.AddComponent<RectTransform>();
        addRt.anchorMin = addRt.anchorMax = new Vector2(0.5f, 0.665f);
        addRt.sizeDelta = new Vector2(700, 54);
        addRt.anchoredPosition = Vector2.zero;

        _addInput = MakeInputField(_addRow, "addInput", Loc.T("Friend's ID (Name#1234)"),
            new Vector2(0.32f, 0.5f), new Vector2(420, 50));
        MakeRowButton(_addRow, "btn_add", Loc.T("ADD"), AccGreen, new Vector2(-40, 0), 150, OnAddClicked);

        _addStatusText = MakeText(_panelRoot, "AddStatus", "", 14,
            new Vector2(0.5f, 0.615f), new Vector2(700, 22), TextSec);

        // ── Liste (ScrollRect) ───────────────────────────────────────────
        var scrollGO = new GameObject("Scroll");
        scrollGO.transform.SetParent(_panelRoot.transform, false);
        var scrollImg = scrollGO.AddComponent<Image>();
        scrollImg.color = new Color(0f, 0f, 0f, 0.25f);
        UiKit.Round(scrollImg, 1.2f);
        var scrollRt = scrollImg.rectTransform;
        scrollRt.anchorMin = new Vector2(0.5f, 0.10f);
        scrollRt.anchorMax = new Vector2(0.5f, 0.585f);
        scrollRt.sizeDelta = new Vector2(860, 0);
        scrollRt.anchoredPosition = Vector2.zero;

        var scroll = scrollGO.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scrollGO.AddComponent<RectMask2D>();

        var contentGO = new GameObject("Content");
        contentGO.transform.SetParent(scrollGO.transform, false);
        _listContent = contentGO.AddComponent<RectTransform>();
        _listContent.anchorMin = new Vector2(0f, 1f);
        _listContent.anchorMax = new Vector2(1f, 1f);
        _listContent.pivot     = new Vector2(0.5f, 1f);
        _listContent.offsetMin = new Vector2(12, 0);
        _listContent.offsetMax = new Vector2(-12, 0);

        var layout = contentGO.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 10;
        layout.padding = new RectOffset(0, 0, 12, 12);
        layout.childForceExpandHeight = false;
        layout.childControlHeight     = true;
        contentGO.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.content = _listContent;

        _emptyText = MakeText(_panelRoot, "Empty", "", 18,
            new Vector2(0.5f, 0.35f), new Vector2(600, 60), TextSec);
        _emptyText.gameObject.SetActive(false);

        // ── GERİ ─────────────────────────────────────────────────────────
        var backBtn = MakeRowButton(_panelRoot, "btn_back", Loc.T("BACK"),
            new Color(0.30f, 0.30f, 0.45f, 1f), new Vector2(0, 0), 200, Hide);
        var backRt = backBtn.GetComponent<RectTransform>();
        backRt.anchorMin = backRt.anchorMax = new Vector2(0.5f, 0.05f);
        backRt.anchoredPosition = Vector2.zero;

        _panelRoot.AddComponent<EscapeListener>().OnEscape = Hide;
    }

    Image MakeTabButton(string name, string label, Vector2 anchor, UnityEngine.Events.UnityAction cb)
    {
        var go = new GameObject(name);
        go.transform.SetParent(_panelRoot.transform, false);
        var img = go.AddComponent<Image>();
        img.color = TabIdle;
        UiKit.Round(img, 1.2f);
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.transition = Selectable.Transition.None;
        btn.onClick.AddListener(cb);
        UiKit.Press(go, 0.96f);
        UiKit.Hover(go);
        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.sizeDelta = new Vector2(260, 52);
        rt.anchoredPosition = Vector2.zero;

        var txt = MakeText(go, "Lbl", label, 18, new Vector2(0.5f, 0.5f), new Vector2(240, 30), Color.white);
        UiKit.BrawlText(txt);
        return img;
    }

    static GameObject MakeRowButton(GameObject parent, string name, string label, Color color,
        Vector2 posFromRight, float width, UnityEngine.Events.UnityAction cb)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        UiKit.Round(img, 1.4f);
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.colors = UiKit.ButtonColors(color);
        btn.onClick.AddListener(cb);
        UiKit.Press(go, 0.95f);
        UiKit.Hover(go);
        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 0.5f);
        rt.pivot = new Vector2(1f, 0.5f);
        rt.sizeDelta = new Vector2(width, 46);
        rt.anchoredPosition = posFromRight;

        var txt = MakeText(go, "Lbl", label, 15, new Vector2(0.5f, 0.5f), new Vector2(width - 8, 26), Color.white);
        txt.fontStyle = FontStyles.Bold;
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
        txt.raycastTarget = false;
        var rt = txt.rectTransform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.sizeDelta = sizeDelta;
        rt.anchoredPosition = Vector2.zero;
        return txt;
    }

    static TMP_InputField MakeInputField(GameObject parent, string name, string placeholder,
        Vector2 anchor, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.12f, 0.12f, 0.22f, 1f);
        UiKit.Round(img, 1.3f);
        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.sizeDelta = size;
        rt.anchoredPosition = Vector2.zero;

        var textGO = new GameObject("Text");
        textGO.transform.SetParent(go.transform, false);
        var text = textGO.AddComponent<TextMeshProUGUI>();
        text.fontSize  = 18;
        text.color     = Color.white;
        text.alignment = TextAlignmentOptions.Left;
        text.margin    = new Vector4(12, 4, 12, 4);
        var trt = text.rectTransform;
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;

        var phGO = new GameObject("Placeholder");
        phGO.transform.SetParent(go.transform, false);
        var ph = phGO.AddComponent<TextMeshProUGUI>();
        ph.text      = placeholder;
        ph.fontSize  = 18;
        ph.color     = new Color(0.5f, 0.5f, 0.6f, 1f);
        ph.alignment = TextAlignmentOptions.Left;
        ph.margin    = new Vector4(12, 4, 12, 4);
        var prt = ph.rectTransform;
        prt.anchorMin = Vector2.zero; prt.anchorMax = Vector2.one;
        prt.offsetMin = prt.offsetMax = Vector2.zero;

        var input = go.AddComponent<TMP_InputField>();
        input.textViewport  = rt;
        input.textComponent = text;
        input.placeholder   = ph;
        return input;
    }
}
