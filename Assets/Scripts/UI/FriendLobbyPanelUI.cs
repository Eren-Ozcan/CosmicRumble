using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Unity.Netcode;
using CosmicRumble.Networking;
using CosmicRumble.Social;
using CosmicRumble.Localization;

/// <summary>
/// ÖZEL MAÇ lobisi — arkadaş davetiyle kurulan 2 kişilik dostluk maçı (kupa değişmez).
/// Host akışı: NetworkBootstrap.HostSessionAsync (IsPrivate, unranked) → session kodu
/// FriendsManager.SendMatchInviteAsync ile arkadaşa gider → arkadaş bağlanınca BAŞLAT aktifleşir
/// → NGO SceneManager Game sahnesini yükler (client'a otomatik yayılır).
/// Client akışı: InvitePopupUI KATIL → JoinSessionAsync → burada host'un başlatması beklenir.
/// Kod hiçbir yerde gösterilmez — davet mekanizması tamamen arkadaş sistemi üzerinden.
/// Canvas order 46 (OnlineLobby 45'in üstünde).
/// </summary>
public class FriendLobbyPanelUI : MonoBehaviour
{
    public static FriendLobbyPanelUI Instance { get; private set; }

    static readonly Color BgColor   = new Color(0.051f, 0.051f, 0.102f, 0.97f);
    static readonly Color SlotBg    = new Color(0.09f,  0.09f,  0.18f,  1f);
    static readonly Color AccGold   = new Color(1.00f,  0.80f,  0.20f,  1f);
    static readonly Color AccGreen  = new Color(0.13f,  0.72f,  0.35f,  1f);
    static readonly Color TextSec   = new Color(0.533f, 0.533f, 0.667f, 1f);
    static readonly Color NameBlue  = new Color(0.45f,  0.80f,  1.00f,  1f);

    GameObject      _panelRoot;
    TextMeshProUGUI _hostSlotName, _guestSlotName, _statusText;
    GameObject      _startBtn;
    bool            _isHost;
    bool            _sessionActive;

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
        UnsubscribeNetwork();
    }

    // ════════════════════════════════════════════════════════════════════
    //  PUBLIC API
    // ════════════════════════════════════════════════════════════════════

    /// <summary>Host: özel oturum kurar, arkadaşa davet yollar, katılmasını bekler.</summary>
    public async void ShowAsHost(string friendId, string friendName)
    {
        _isHost = true;
        OpenCommon(hostName: PlayerIdentity.Get(), guestName: null);
        _statusText.text = Loc.T("Setting up session...");

        string code = await NetworkBootstrap.Instance.HostSessionAsync();
        if (string.IsNullOrEmpty(code))
        {
            _statusText.text = Loc.T("Couldn't set up the session, try again.");
            return;
        }
        _sessionActive = true;

        var (sent, error) = await FriendsManager.Instance.SendMatchInviteAsync(friendId, code);
        _statusText.text = sent
            ? string.Format(Loc.T("Invite sent, waiting for {0}..."), friendName)
            : string.Format(Loc.T("Couldn't send invite: {0}"), error);
        if (!sent) return;

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }
        _guestSlotNamePending = friendName;
    }

    /// <summary>Client: davete katıldıktan sonra host'un başlatmasını bekler.</summary>
    public void ShowAsClient(string hostName)
    {
        _isHost = false;
        _sessionActive = true;
        OpenCommon(hostName: hostName, guestName: PlayerIdentity.Get());
        _statusText.text = Loc.T("Waiting for host to start...");
        // Sahne yüklemesi NGO üzerinden otomatik gelir — burada ek iş yok.
    }

    public void Hide() => _panelRoot.SetActive(false);

    // ════════════════════════════════════════════════════════════════════
    //  INTERNALS
    // ════════════════════════════════════════════════════════════════════

    string _guestSlotNamePending;

    void OpenCommon(string hostName, string guestName)
    {
        _panelRoot.SetActive(true);
        _hostSlotName.text  = hostName ?? "?";
        _guestSlotName.text = string.IsNullOrEmpty(guestName) ? Loc.T("Waiting...") : guestName;
        _guestSlotName.color = string.IsNullOrEmpty(guestName) ? TextSec : NameBlue;
        _startBtn.SetActive(_isHost);
        SetStartInteractable(false);
    }

    void OnClientConnected(ulong clientId)
    {
        if (!_isHost || NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;
        if (NetworkManager.Singleton.ConnectedClientsIds.Count < 2) return;

        _guestSlotName.text  = _guestSlotNamePending ?? Loc.T("Opponent");
        _guestSlotName.color = NameBlue;
        _statusText.text = Loc.T("Ready! You can start the match.");
        SetStartInteractable(true);
    }

    void SetStartInteractable(bool on)
    {
        var btn = _startBtn.GetComponent<Button>();
        btn.interactable = on;
        _startBtn.GetComponent<Image>().color = on ? AccGold : new Color(0.35f, 0.32f, 0.18f, 1f);
    }

    void OnStartClicked()
    {
        if (!_isHost || NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;
        if (NetworkManager.Singleton.ConnectedClientsIds.Count < 2) return;

        UnsubscribeNetwork();
        // Dostluk maçı (HostSessionAsync IsRankedMatch=false bıraktı) — kupa değişmez.
        NetworkManager.Singleton.SceneManager.LoadScene(SceneNames.Game, LoadSceneMode.Single);
    }

    async void OnCancelClicked()
    {
        _statusText.text = Loc.T("Cancelling...");
        UnsubscribeNetwork();
        if (_sessionActive)
        {
            _sessionActive = false;
            await NetworkBootstrap.Instance.LeaveSessionAsync();
        }
        Hide();
    }

    void UnsubscribeNetwork()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
    }

    // ════════════════════════════════════════════════════════════════════
    //  UI BUILD
    // ════════════════════════════════════════════════════════════════════

    void BuildUI()
    {
        var canvasGO = new GameObject("FriendLobbyCanvas");
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 46;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        _panelRoot = new GameObject("FriendLobbyRoot");
        _panelRoot.transform.SetParent(canvasGO.transform, false);
        var overlay = _panelRoot.AddComponent<Image>();
        overlay.color = BgColor;
        var overlayRt = overlay.rectTransform;
        overlayRt.anchorMin = Vector2.zero;
        overlayRt.anchorMax = Vector2.one;
        overlayRt.offsetMin = overlayRt.offsetMax = Vector2.zero;

        var title = MakeText(_panelRoot, "Title", Loc.T("PRIVATE MATCH"), 34,
            new Vector2(0.5f, 0.88f), new Vector2(500, 48), AccGold);
        UiKit.BrawlText(title);

        MakeText(_panelRoot, "Sub", Loc.T("Friendly match — trophies unaffected"), 16,
            new Vector2(0.5f, 0.81f), new Vector2(500, 26), TextSec);

        _hostSlotName  = BuildSlot("HostSlot",  new Vector2(0.32f, 0.58f), "HOST");
        _guestSlotName = BuildSlot("GuestSlot", new Vector2(0.68f, 0.58f), Loc.T("OPPONENT"));

        _statusText = MakeText(_panelRoot, "Status", "", 17,
            new Vector2(0.5f, 0.38f), new Vector2(700, 30), AccGold);

        _startBtn = MakeButton(_panelRoot, "btn_start", Loc.T("START"), AccGold,
            new Vector2(0.5f, 0.25f), new Vector2(340, 70), OnStartClicked);
        var startLbl = _startBtn.transform.Find("Lbl").GetComponent<TextMeshProUGUI>();
        startLbl.fontSize = 24;
        UiKit.BrawlText(startLbl);

        MakeButton(_panelRoot, "btn_cancel", Loc.T("CANCEL"), new Color(0.30f, 0.30f, 0.45f, 1f),
            new Vector2(0.5f, 0.12f), new Vector2(220, 54), OnCancelClicked);

        _panelRoot.AddComponent<EscapeListener>().OnEscape = OnCancelClicked;
    }

    TextMeshProUGUI BuildSlot(string name, Vector2 anchor, string roleLabel)
    {
        var slot = new GameObject(name);
        slot.transform.SetParent(_panelRoot.transform, false);
        var img = slot.AddComponent<Image>();
        img.color = SlotBg;
        UiKit.Round(img);
        UiKit.Shadow(slot, 5f, 0.45f);
        UiKit.Stroke(slot, new Color(1f, 1f, 1f, 0.08f));
        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.sizeDelta = new Vector2(380, 260);
        rt.anchoredPosition = Vector2.zero;

        MakeText(slot, "Role", roleLabel, 14, new Vector2(0.5f, 0.85f), new Vector2(300, 22), TextSec);

        // Avatar dairesi (harf rozeti — art asset yok)
        var avatar = new GameObject("Avatar");
        avatar.transform.SetParent(slot.transform, false);
        var avImg = avatar.AddComponent<Image>();
        avImg.sprite = UiKit.CircleSprite;
        avImg.color  = new Color(0.22f, 0.45f, 0.95f, 1f);
        avImg.raycastTarget = false;
        var avRt = avImg.rectTransform;
        avRt.anchorMin = avRt.anchorMax = new Vector2(0.5f, 0.55f);
        avRt.sizeDelta = new Vector2(90, 90);
        avRt.anchoredPosition = Vector2.zero;

        var nameTxt = MakeText(slot, "Name", "", 20, new Vector2(0.5f, 0.22f), new Vector2(340, 30), NameBlue);
        UiKit.BrawlText(nameTxt);
        return nameTxt;
    }

    static GameObject MakeButton(GameObject parent, string name, string label, Color color,
        Vector2 anchor, Vector2 size, UnityEngine.Events.UnityAction cb)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        UiKit.Round(img);
        UiKit.Shadow(go, 4f, 0.40f);
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.colors = UiKit.ButtonColors(color);
        btn.onClick.AddListener(cb);
        UiKit.Press(go);
        UiKit.Hover(go);
        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.sizeDelta = size;
        rt.anchoredPosition = Vector2.zero;

        var txt = MakeText(go, "Lbl", label, 18, new Vector2(0.5f, 0.5f), new Vector2(size.x - 12, 32), Color.white);
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
}
