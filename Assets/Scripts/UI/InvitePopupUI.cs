using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using CosmicRumble.Networking;
using CosmicRumble.Social;
using CosmicRumble.Localization;

/// <summary>
/// Gelen maç daveti popup'ı — ekranın üst-ortasında küçük kart: "{isim} seni maça davet etti!"
/// KATIL → JoinSessionAsync(kod) → PartyLobbyPanelUI.ShowAsClient. Bayat kod (host iptal etti)
/// → "Davet artık geçerli değil." REDDET → kapat.
/// Sadece menü bağlamında çalışır: aktif bir NGO oturumu varken (maçta/lobide) gelen davetler
/// sessizce düşürülür — davet eden zaten presence'ta "Maçta" görür, butonu kilitlenir.
/// Sahne ömürlü, canvas order 80.
/// </summary>
public class InvitePopupUI : MonoBehaviour
{
    public static InvitePopupUI Instance { get; private set; }

    static readonly Color CardBg   = new Color(0.09f,  0.09f,  0.18f,  0.98f);
    static readonly Color AccGreen = new Color(0.13f,  0.72f,  0.35f,  1f);
    static readonly Color AccRed   = new Color(0.72f,  0.18f,  0.18f,  1f);
    static readonly Color TextSec  = new Color(0.533f, 0.533f, 0.667f, 1f);

    GameObject      _root;
    TextMeshProUGUI _messageText;
    Button          _joinBtn, _declineBtn;
    string          _pendingCode;
    string          _pendingFromName;
    string          _pendingFromId;
    bool            _busy;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        BuildUI();
        _root.SetActive(false);
    }

    void OnEnable()
    {
        if (FriendsManager.Instance != null)
        {
            FriendsManager.Instance.OnMatchInvite -= HandleInvite;
            FriendsManager.Instance.OnMatchInvite += HandleInvite;
        }
    }

    void OnDisable()
    {
        if (FriendsManager.Instance != null)
            FriendsManager.Instance.OnMatchInvite -= HandleInvite;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ════════════════════════════════════════════════════════════════════

    void HandleInvite(MatchInviteMessage invite, string senderId)
    {
        // Zaten bir oturumdayken (maç/lobi) gelen davetleri düşür
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening) return;

        _pendingCode     = invite.code;
        _pendingFromName = string.IsNullOrEmpty(invite.fromName) ? Loc.T("A friend") : invite.fromName;
        _pendingFromId   = senderId;
        _messageText.text = string.Format(Loc.T("{0} invited you to a match!"), _pendingFromName);
        _busy = false;
        SetButtonsInteractable(true);
        _root.SetActive(true);
    }

    async void OnJoinClicked()
    {
        if (_busy || string.IsNullOrEmpty(_pendingCode)) return;
        _busy = true;
        SetButtonsInteractable(false);
        _messageText.text = Loc.T("Connecting...");

        bool ok = await NetworkBootstrap.Instance.JoinSessionAsync(_pendingCode);
        if (ok)
        {
            _root.SetActive(false);
            PartyLobbyPanelUI.Instance?.ShowAsClient(_pendingFromName, _pendingFromId);
        }
        else
        {
            _busy = false;
            SetButtonsInteractable(true);
            _messageText.text = Loc.T("This invite is no longer valid.");
            _pendingCode = null;
        }
    }

    void OnDeclineClicked()
    {
        _pendingCode = null;
        _root.SetActive(false);
    }

    void SetButtonsInteractable(bool on)
    {
        _joinBtn.interactable    = on;
        _declineBtn.interactable = on;
    }

    // ════════════════════════════════════════════════════════════════════
    //  UI BUILD
    // ════════════════════════════════════════════════════════════════════

    void BuildUI()
    {
        var canvasGO = new GameObject("InvitePopupCanvas");
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 80;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        _root = new GameObject("InviteCard");
        _root.transform.SetParent(canvasGO.transform, false);
        var img = _root.AddComponent<Image>();
        img.color = CardBg;
        UiKit.Round(img);
        UiKit.Shadow(_root, 6f, 0.55f);
        UiKit.Stroke(_root, new Color(1f, 1f, 1f, 0.10f));
        UiKit.Pop(_root);
        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(560, 150);
        rt.anchoredPosition = new Vector2(0, -110);

        _messageText = MakeText(_root, "Msg", "", 19,
            new Vector2(0.5f, 0.72f), new Vector2(520, 30), Color.white);

        _joinBtn    = MakeButton(_root, "btn_join",    Loc.T("JOIN"),    AccGreen, new Vector2(0.30f, 0.28f), OnJoinClicked);
        _declineBtn = MakeButton(_root, "btn_decline", Loc.T("DECLINE"), AccRed,   new Vector2(0.70f, 0.28f), OnDeclineClicked);
    }

    static Button MakeButton(GameObject parent, string name, string label, Color color,
        Vector2 anchor, UnityEngine.Events.UnityAction cb)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        UiKit.Round(img, 1.3f);
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.colors = UiKit.ButtonColors(color);
        btn.onClick.AddListener(cb);
        UiKit.Press(go, 0.95f);
        UiKit.Hover(go);
        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.sizeDelta = new Vector2(200, 52);
        rt.anchoredPosition = Vector2.zero;

        var txt = MakeText(go, "Lbl", label, 17, new Vector2(0.5f, 0.5f), new Vector2(190, 28), Color.white);
        txt.fontStyle = FontStyles.Bold;
        return btn;
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
