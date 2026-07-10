using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Unity.Netcode;
using Unity.Services.Friends.Models;
using CosmicRumble.Networking;
using CosmicRumble.Social;
using CosmicRumble.Localization;
using CosmicRumble.Data;
using CosmicRumble.Utilities;

/// <summary>
/// Parti lobisi — 1v1'den 4v4/2v2v2v2'ye (8 oyuncu, projedeki en büyük mod) kadar tüm modların ortak
/// host/misafir akışı. Eski FriendLobbyPanelUI'nin (sabit 2 slot, tek arkadaş) yerini alır.
///
/// Host akışları:
///  - ShowAsHost(friendId, friendName): SocialPanelUI'daki tekil "OYUNA DAVET ET" — eski
///    FriendLobbyPanelUI.ShowAsHost ile birebir aynı davranış (Duel1v1, tek davet, anında kurulum).
///  - ShowModeSelect(): yeni "PARTİ" girişi (MainMenuUI çekmecesi) — mod seç → PARTİYİ KUR (özel
///    oturum) → roster ekranında istediğin kadar arkadaş davet et (aynı oturum koduna, sırayla) →
///    herkes katılınca BAŞLAT.
///
/// Misafir akışı: InvitePopupUI KATIL → ShowAsClient → host'un başlatması beklenir. Not: misafir
/// tarafında diğer katılımcıların İSİM bazlı tam roster senkronu YOK — yalnızca canlı "X/N
/// katılımcı" sayacı gösterilir (isim senkronu için server→client bir NetworkList/RPC gerekir,
/// bu geçişte kapsam dışı bırakıldı). Takım rengi önizlemesi katılım sırasına göre yapılır —
/// gerçek atama NetworkPlayerSpawner'da aynı sırayla teyit edilir.
///
/// Canvas order 46 (OnlineLobby 45'in üstünde) — FriendLobbyPanelUI ile aynı katman.
/// </summary>
public class PartyLobbyPanelUI : MonoBehaviour
{
    public static PartyLobbyPanelUI Instance { get; private set; }

    static readonly Color BgColor   = new Color(0.051f, 0.051f, 0.102f, 0.97f);
    static readonly Color CardBg    = new Color(0.09f,  0.09f,  0.18f,  1f);
    static readonly Color SlotBg    = new Color(0.09f,  0.09f,  0.18f,  1f);
    static readonly Color AccGold   = new Color(1.00f,  0.80f,  0.20f,  1f);
    static readonly Color AccGreen  = new Color(0.13f,  0.72f,  0.35f,  1f);
    static readonly Color AccBlue   = new Color(0.29f,  0.62f,  1.00f,  1f);
    static readonly Color TextSec   = new Color(0.533f, 0.533f, 0.667f, 1f);
    static readonly Color ModeIdle  = new Color(0.13f,  0.13f,  0.24f,  1f);
    static readonly Color OnlineDot = new Color(0.25f,  0.85f,  0.35f,  1f);

    GameObject _panelRoot;
    GameObject _modeSelectRoot, _rosterRoot, _inviteListRoot;

    // Mod seçimi
    readonly Dictionary<GameModeType, Image> _modeButtons = new Dictionary<GameModeType, Image>();
    GameObject      _ffaStepperRoot;
    TextMeshProUGUI _ffaCountText;
    TextMeshProUGUI _modeSummaryText;
    GameModeType    _pendingMode     = GameModeType.Duel1v1;
    int             _pendingFfaCount = GameModeCatalog.MinFfaPlayers;

    // Roster
    TextMeshProUGUI       _rosterStatusText;
    readonly List<GameObject>      _slotRoots  = new List<GameObject>();
    readonly List<TextMeshProUGUI> _slotTexts  = new List<TextMeshProUGUI>();
    readonly List<Image>           _slotAvatars = new List<Image>();
    GameObject _startBtn, _inviteBtn;
    RectTransform _inviteListContent;

    bool   _isHost;
    bool   _sessionActive;
    bool   _isTeamModeActive;
    int    _teamCountActive;
    int    _requiredPlayers;

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

    /// <summary>Parti henüz kurulmuşken (maç başlamadan) arka plana atılır/kapatılırsa oturumu
    /// temizler — bkz. eski FriendLobbyPanelUI'daki aynı desen.</summary>
    void OnApplicationPause(bool paused) { if (paused) CleanupOnBackground(); }
    void OnApplicationQuit() => CleanupOnBackground();

    void CleanupOnBackground()
    {
        if (!_sessionActive) return;
        _sessionActive = false;
        LobbyData.FriendOpponentId = null; // maç hiç başlamadı — KOZMIK_EKIP'e sızmasın
        UnsubscribeNetwork();
        _ = NetworkBootstrap.Instance?.LeaveSessionAsync();
    }

    // ════════════════════════════════════════════════════════════════════
    //  PUBLIC API
    // ════════════════════════════════════════════════════════════════════

    /// <summary>SOSYAL panelindeki tekil "OYUNA DAVET ET" — Duel1v1, tek arkadaş, anında kurulum
    /// (eski FriendLobbyPanelUI.ShowAsHost ile birebir aynı davranış).</summary>
    public async void ShowAsHost(string friendId, string friendName)
    {
        _isHost = true;
        _pendingMode = GameModeType.Duel1v1;
        LobbyData.SelectedMode = GameModeType.Duel1v1;
        _requiredPlayers = 2;

        _panelRoot.SetActive(true);
        _modeSelectRoot.SetActive(false);
        _rosterRoot.SetActive(true);
        _inviteListRoot.SetActive(false);
        BuildRosterSlots(2, isTeamMode: false, teamCount: 0);
        _inviteBtn.SetActive(false); // tekil davet akışında ek arkadaş daveti gösterilmez
        _rosterStatusText.text = Loc.T("Setting up session...");

        string code = await NetworkBootstrap.Instance.HostSessionAsync();
        if (string.IsNullOrEmpty(code))
        {
            _rosterStatusText.text = Loc.T("Couldn't set up the session, try again.");
            return;
        }
        _sessionActive = true;
        LobbyData.FriendOpponentId = friendId; // KOZMIK_EKIP — maç tamamlandığında TurnManager okur
        SetSlotFilled(0, PlayerIdentity.Get());

        var (sent, error) = await FriendsManager.Instance.SendMatchInviteAsync(friendId, code);
        _rosterStatusText.text = sent
            ? string.Format(Loc.T("Invite sent, waiting for {0}..."), friendName)
            : string.Format(Loc.T("Couldn't send invite: {0}"), error);
        if (!sent) return;

        SubscribeNetwork();
        _startBtn.SetActive(true);
        SetStartInteractable(false);
    }

    /// <summary>Yeni "PARTİ" girişi — host mod seçer, parti kurar, roster ekranından istediği
    /// kadar arkadaş davet eder.</summary>
    public void ShowModeSelect()
    {
        _isHost = true;
        _panelRoot.SetActive(true);
        _modeSelectRoot.SetActive(true);
        _rosterRoot.SetActive(false);
        SelectMode(GameModeType.Duel1v1);
    }

    /// <summary>Misafir: davet kabul edildikten sonra host'un başlatmasını bekler.</summary>
    public void ShowAsClient(string hostName, string hostId)
    {
        _isHost = false;
        _sessionActive = true;
        LobbyData.FriendOpponentId = hostId; // KOZMIK_EKIP (tek arkadaşlı davetlerde anlamlı)
        _panelRoot.SetActive(true);
        _modeSelectRoot.SetActive(false);
        _rosterRoot.SetActive(true);
        _inviteListRoot.SetActive(false);
        _inviteBtn.SetActive(false);
        _startBtn.SetActive(false);
        BuildRosterSlots(1, isTeamMode: false, teamCount: 0); // gerçek sayı bilinmiyor, sade sayaç gösterilecek
        SetSlotFilled(0, PlayerIdentity.Get());
        _rosterStatusText.text = string.Format(Loc.T("Waiting for {0} to start..."), hostName);
        SubscribeNetwork();
    }

    public void Hide() => _panelRoot.SetActive(false);

    // ════════════════════════════════════════════════════════════════════
    //  MOD SEÇİMİ
    // ════════════════════════════════════════════════════════════════════

    void SelectMode(GameModeType mode)
    {
        _pendingMode = mode;
        foreach (var kvp in _modeButtons)
            kvp.Value.color = kvp.Key == mode ? AccGold : ModeIdle;

        _ffaStepperRoot.SetActive(mode == GameModeType.Ffa);
        UpdateModeSummary();
    }

    void UpdateModeSummary()
    {
        GameModeCatalog.All.TryGetValue(_pendingMode, out var def);
        int total = GameModeCatalog.ResolveTotalPlayers(_pendingMode, _pendingFfaCount);
        _modeSummaryText.text = def.IsTeamMode
            ? string.Format(Loc.T("{0} teams x {1} — {2} players total"), def.TeamCount, def.TeamSize, total)
            : string.Format(Loc.T("{0} players — free for all"), total);
    }

    void OnFfaMinus()
    {
        _pendingFfaCount = Mathf.Max(GameModeCatalog.MinFfaPlayers, _pendingFfaCount - 1);
        _ffaCountText.text = _pendingFfaCount.ToString();
        UpdateModeSummary();
    }

    void OnFfaPlus()
    {
        _pendingFfaCount = Mathf.Min(GameModeCatalog.MaxFfaPlayers, _pendingFfaCount + 1);
        _ffaCountText.text = _pendingFfaCount.ToString();
        UpdateModeSummary();
    }

    async void OnCreatePartyClicked()
    {
        LobbyData.SelectedMode   = _pendingMode;
        LobbyData.FfaPlayerCount = _pendingFfaCount;
        GameModeCatalog.All.TryGetValue(_pendingMode, out var def);
        _requiredPlayers = GameModeCatalog.ResolveTotalPlayers(_pendingMode, _pendingFfaCount);

        _modeSelectRoot.SetActive(false);
        _rosterRoot.SetActive(true);
        _inviteListRoot.SetActive(false);
        BuildRosterSlots(_requiredPlayers, def.IsTeamMode, def.TeamCount);
        _inviteBtn.SetActive(true);
        _rosterStatusText.text = Loc.T("Setting up party...");

        string code = await NetworkBootstrap.Instance.HostSessionAsync();
        if (string.IsNullOrEmpty(code))
        {
            _rosterStatusText.text = Loc.T("Couldn't set up the session, try again.");
            return;
        }
        _sessionActive = true;
        SetSlotFilled(0, PlayerIdentity.Get());
        _rosterStatusText.text = Loc.T("Invite friends, then start once everyone's in.");

        SubscribeNetwork();
        _startBtn.SetActive(true);
        SetStartInteractable(false);
    }

    async void OnCancelClicked()
    {
        _rosterStatusText.text = Loc.T("Cancelling...");
        UnsubscribeNetwork();
        if (_sessionActive)
        {
            _sessionActive = false;
            LobbyData.FriendOpponentId = null; // maç hiç başlamadı — KOZMIK_EKIP'e sızmasın
            await NetworkBootstrap.Instance.LeaveSessionAsync();
        }
        Hide();
    }

    // ════════════════════════════════════════════════════════════════════
    //  ROSTER
    // ════════════════════════════════════════════════════════════════════

    void BuildRosterSlots(int visibleCount, bool isTeamMode, int teamCount)
    {
        _isTeamModeActive = isTeamMode;
        _teamCountActive  = teamCount;

        for (int i = 0; i < _slotRoots.Count; i++)
        {
            bool visible = i < visibleCount;
            _slotRoots[i].SetActive(visible);
            if (!visible) continue;

            int previewTeam = isTeamMode ? (i % Mathf.Max(1, teamCount)) : i;
            _slotAvatars[i].color = TeamColors.Get(previewTeam);
            _slotTexts[i].text    = Loc.T("Empty");
            _slotTexts[i].color   = TextSec;
        }
    }

    void SetSlotFilled(int index, string name)
    {
        if (index < 0 || index >= _slotTexts.Count) return;
        _slotTexts[index].text  = name;
        _slotTexts[index].color = Color.white;
    }

    void SubscribeNetwork()
    {
        if (NetworkManager.Singleton == null) return;
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    void UnsubscribeNetwork()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
    }

    void OnClientConnected(ulong clientId)
    {
        int connected = NetworkManager.Singleton.ConnectedClientsIds.Count;

        if (_isHost)
        {
            // Katılım sırasına göre doldur — kimin gerçekte hangi arkadaş olduğu bilinmiyor
            // (clientId↔PlayerId eşlemesi yok), yalnızca "N. kişi katıldı" gösterilir.
            for (int i = 1; i < _slotTexts.Count && i < connected; i++)
                if (_slotTexts[i].text == Loc.T("Empty"))
                    SetSlotFilled(i, Loc.T("Joined"));

            bool ready = connected >= _requiredPlayers;
            _rosterStatusText.text = ready
                ? Loc.T("Everyone's in — you can start!")
                : string.Format(Loc.T("{0}/{1} joined"), connected, _requiredPlayers);

            if (NetworkManager.Singleton.IsServer)
                SetStartInteractable(ready);
        }
        else
        {
            _rosterStatusText.text = string.Format(Loc.T("{0} players in the lobby..."), connected);
        }
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
        if (NetworkManager.Singleton.ConnectedClientsIds.Count < _requiredPlayers) return;

        UnsubscribeNetwork();
        NetworkManager.Singleton.SceneManager.LoadScene(SceneNames.Game, LoadSceneMode.Single);
    }

    // ════════════════════════════════════════════════════════════════════
    //  ARKADAŞ DAVET LİSTESİ (yalnızca host, PARTİ akışında)
    // ════════════════════════════════════════════════════════════════════

    void OnToggleInviteList()
    {
        bool showing = !_inviteListRoot.activeSelf;
        _inviteListRoot.SetActive(showing);
        if (showing) RebuildInviteList();
    }

    void RebuildInviteList()
    {
        for (int i = _inviteListContent.childCount - 1; i >= 0; i--)
            Destroy(_inviteListContent.GetChild(i).gameObject);

        var fm = FriendsManager.Instance;
        if (fm == null || !fm.IsAvailable)
        {
            MakeText(_inviteListContent.gameObject, "none", Loc.T("Friends system is currently unavailable."),
                15, new Vector2(0.5f, 0.5f), new Vector2(600, 40), TextSec);
            return;
        }

        bool any = false;
        foreach (var rel in fm.Friends)
        {
            bool online = false;
            try { online = rel.Member?.Presence != null && rel.Member.Presence.Availability == Availability.Online; }
            catch { }
            if (!online) continue;

            any = true;
            string memberId = rel.Member?.Id;
            string name     = rel.Member?.Profile?.Name ?? "???";
            int hashIdx     = name.IndexOf('#');
            string shownName = hashIdx > 0 ? name.Substring(0, hashIdx) : name;

            BuildInviteRow(memberId, shownName);
        }

        if (!any)
            MakeText(_inviteListContent.gameObject, "none", Loc.T("No online friends right now."),
                15, new Vector2(0.5f, 0.5f), new Vector2(600, 40), TextSec);
    }

    void BuildInviteRow(string memberId, string shownName)
    {
        var row = new GameObject("Row_" + memberId);
        row.transform.SetParent(_inviteListContent, false);
        var rowImg = row.AddComponent<Image>();
        rowImg.color = SlotBg;
        UiKit.Round(rowImg, 1.2f);
        var le = row.AddComponent<LayoutElement>();
        le.minHeight = le.preferredHeight = 64;

        var dot = new GameObject("Dot");
        dot.transform.SetParent(row.transform, false);
        var dotImg = dot.AddComponent<Image>();
        dotImg.sprite = UiKit.CircleSprite;
        dotImg.color  = OnlineDot;
        dotImg.raycastTarget = false;
        var dotRt = dotImg.rectTransform;
        dotRt.anchorMin = dotRt.anchorMax = new Vector2(0f, 0.5f);
        dotRt.sizeDelta = new Vector2(14, 14);
        dotRt.anchoredPosition = new Vector2(24, 0);

        var nameTxt = MakeText(row, "Name", shownName, 18,
            new Vector2(0f, 0.5f), new Vector2(280, 26), Color.white);
        nameTxt.alignment = TextAlignmentOptions.Left;
        nameTxt.rectTransform.anchoredPosition = new Vector2(200, 0);

        MakeRowButton(row, "btn_invite", Loc.T("INVITE"), AccGreen, new Vector2(-90, 0), 150, async () =>
        {
            var btnGo = row.transform.Find("btn_invite")?.gameObject;
            if (btnGo != null) btnGo.GetComponent<Button>().interactable = false;

            string code = NetworkBootstrap.Instance.LastJoinCode;
            var (sent, error) = await FriendsManager.Instance.SendMatchInviteAsync(memberId, code);
            var lbl = btnGo != null ? btnGo.transform.Find("Lbl")?.GetComponent<TextMeshProUGUI>() : null;
            if (lbl != null) lbl.text = sent ? Loc.T("SENT") : Loc.T("FAILED");

            // KOZMIK_EKIP: hâlâ tek arkadaş id'si takip ediyor (bkz. LobbyData.FriendOpponentId
            // yorumu) — parti modunda birden fazla arkadaş davet edilebildiği için "gerçek grup"
            // kavramı yok, en azından İLK davet edilen arkadaş için ilerleme sayılsın diye
            // henüz boşsa dolduruluyor. Tam grup takibi ayrı bir iş (bkz. TODO.md).
            if (sent && string.IsNullOrEmpty(LobbyData.FriendOpponentId))
                LobbyData.FriendOpponentId = memberId;
        });
    }

    // ════════════════════════════════════════════════════════════════════
    //  UI BUILD
    // ════════════════════════════════════════════════════════════════════

    void BuildUI()
    {
        var canvasGO = new GameObject("PartyLobbyCanvas");
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 46;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        _panelRoot = new GameObject("PartyLobbyRoot");
        _panelRoot.transform.SetParent(canvasGO.transform, false);
        var overlay = _panelRoot.AddComponent<Image>();
        overlay.color = BgColor;
        var overlayRt = overlay.rectTransform;
        overlayRt.anchorMin = Vector2.zero;
        overlayRt.anchorMax = Vector2.one;
        overlayRt.offsetMin = overlayRt.offsetMax = Vector2.zero;

        BuildModeSelectRoot();
        BuildRosterRoot();

        _panelRoot.AddComponent<EscapeListener>().OnEscape = OnCancelClicked;
    }

    void BuildModeSelectRoot()
    {
        _modeSelectRoot = new GameObject("ModeSelect");
        _modeSelectRoot.transform.SetParent(_panelRoot.transform, false);
        var rt = _modeSelectRoot.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        var title = MakeText(_modeSelectRoot, "Title", Loc.T("CHOOSE MODE"), 34,
            new Vector2(0.5f, 0.90f), new Vector2(500, 48), AccGold);
        UiKit.BrawlText(title);

        var modes = new (GameModeType type, string label)[]
        {
            (GameModeType.Duel1v1,     "1v1"),
            (GameModeType.Ffa,         "FFA"),
            (GameModeType.Team2v2,     "2v2"),
            (GameModeType.Team3v3,     "3v3"),
            (GameModeType.Team4v4,     "4v4"),
            (GameModeType.Team2v2v2v2, "2v2v2v2"),
        };

        int cols = 4;
        Vector2 cellSize = new Vector2(220, 64);
        float startX = 0.5f - (cols - 1) * 0.115f;
        for (int i = 0; i < modes.Length; i++)
        {
            int col = i % cols;
            int row = i / cols;
            var anchor = new Vector2(startX + col * 0.115f, 0.74f - row * 0.10f);
            var (type, label) = modes[i];
            var go = MakeButton(_modeSelectRoot, "mode_" + type, label, ModeIdle, anchor, cellSize,
                () => SelectMode(type));
            _modeButtons[type] = go.GetComponent<Image>();
        }

        // FFA oyuncu sayısı stepper'ı (yalnızca Ffa seçiliyken görünür)
        _ffaStepperRoot = new GameObject("FfaStepper");
        _ffaStepperRoot.transform.SetParent(_modeSelectRoot.transform, false);
        var stepperRt = _ffaStepperRoot.AddComponent<RectTransform>();
        stepperRt.anchorMin = stepperRt.anchorMax = new Vector2(0.5f, 0.46f);
        stepperRt.sizeDelta = new Vector2(360, 60);
        stepperRt.anchoredPosition = Vector2.zero;

        MakeButton(_ffaStepperRoot, "btn_ffa_minus", "-", ModeIdle, new Vector2(0.18f, 0.5f), new Vector2(60, 60), OnFfaMinus);
        _ffaCountText = MakeText(_ffaStepperRoot, "count", _pendingFfaCount.ToString(), 26,
            new Vector2(0.5f, 0.5f), new Vector2(100, 50), Color.white);
        MakeButton(_ffaStepperRoot, "btn_ffa_plus", "+", ModeIdle, new Vector2(0.82f, 0.5f), new Vector2(60, 60), OnFfaPlus);
        _ffaStepperRoot.SetActive(false);

        _modeSummaryText = MakeText(_modeSelectRoot, "Summary", "", 17,
            new Vector2(0.5f, 0.34f), new Vector2(700, 30), TextSec);

        MakeButton(_modeSelectRoot, "btn_create", Loc.T("CREATE PARTY"), AccGold,
            new Vector2(0.5f, 0.20f), new Vector2(340, 70), OnCreatePartyClicked);

        MakeButton(_modeSelectRoot, "btn_cancel_mode", Loc.T("CANCEL"), new Color(0.30f, 0.30f, 0.45f, 1f),
            new Vector2(0.5f, 0.10f), new Vector2(220, 54), OnCancelClicked);
    }

    void BuildRosterRoot()
    {
        _rosterRoot = new GameObject("Roster");
        _rosterRoot.transform.SetParent(_panelRoot.transform, false);
        var rt = _rosterRoot.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        var title = MakeText(_rosterRoot, "Title", Loc.T("PARTY LOBBY"), 34,
            new Vector2(0.5f, 0.90f), new Vector2(500, 48), AccGold);
        UiKit.BrawlText(title);

        // 3x3 slot ızgarası (8 dolu + 1 boş kalır) — projedeki en büyük mod 8 oyuncu.
        for (int i = 0; i < GameModeCatalog.MaxLobbySize; i++)
        {
            int col = i % 3;
            int row = i / 3;
            var anchor = new Vector2(0.32f + col * 0.18f, 0.72f - row * 0.16f);
            BuildSlot(anchor);
        }

        _rosterStatusText = MakeText(_rosterRoot, "Status", "", 17,
            new Vector2(0.5f, 0.20f), new Vector2(760, 30), AccGold);

        _inviteBtn = MakeButton(_rosterRoot, "btn_invite_friends", Loc.T("INVITE FRIENDS"), AccBlue,
            new Vector2(0.5f, 0.13f), new Vector2(320, 56), OnToggleInviteList);

        _startBtn = MakeButton(_rosterRoot, "btn_start", Loc.T("START"), AccGold,
            new Vector2(0.30f, 0.05f), new Vector2(240, 60), OnStartClicked);

        MakeButton(_rosterRoot, "btn_cancel_roster", Loc.T("LEAVE"), new Color(0.30f, 0.30f, 0.45f, 1f),
            new Vector2(0.70f, 0.05f), new Vector2(240, 60), OnCancelClicked);

        BuildInviteListRoot();

        // Tüm çocuklar (TMP dahil) hâlâ aktif hiyerarşideyken kurulduktan SONRA kapatılır —
        // UiKit.BrawlText()'in outlineWidth setter'ı TMP materyal instance'ı oluşturmak için
        // OnEnable()'ın çalışmış olmasını gerektiriyor; ters sıra (önce SetActive(false), sonra
        // BrawlText) inaktif hiyerarşide NullReferenceException fırlatır (bkz. WardrobePanelUI'da
        // aynı sınıftan bulunan/düzeltilen bug, TODO.md).
        _rosterRoot.SetActive(false);
    }

    void BuildSlot(Vector2 anchor)
    {
        var slot = new GameObject("Slot");
        slot.transform.SetParent(_rosterRoot.transform, false);
        var img = slot.AddComponent<Image>();
        img.color = SlotBg;
        UiKit.Round(img);
        UiKit.Stroke(slot, new Color(1f, 1f, 1f, 0.08f));
        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.sizeDelta = new Vector2(220, 130);
        rt.anchoredPosition = Vector2.zero;

        var avatar = new GameObject("Avatar");
        avatar.transform.SetParent(slot.transform, false);
        var avImg = avatar.AddComponent<Image>();
        avImg.sprite = UiKit.CircleSprite;
        avImg.color  = ModeIdle;
        avImg.raycastTarget = false;
        var avRt = avImg.rectTransform;
        avRt.anchorMin = avRt.anchorMax = new Vector2(0.5f, 0.66f);
        avRt.sizeDelta = new Vector2(50, 50);
        avRt.anchoredPosition = Vector2.zero;

        var nameTxt = MakeText(slot, "Name", Loc.T("Empty"), 15,
            new Vector2(0.5f, 0.24f), new Vector2(200, 26), TextSec);

        slot.SetActive(false);
        _slotRoots.Add(slot);
        _slotAvatars.Add(avImg);
        _slotTexts.Add(nameTxt);
    }

    void BuildInviteListRoot()
    {
        _inviteListRoot = new GameObject("InviteList");
        _inviteListRoot.transform.SetParent(_rosterRoot.transform, false);
        var rt = _inviteListRoot.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        var bg = _inviteListRoot.AddComponent<Image>();
        bg.color = BgColor;

        var title = MakeText(_inviteListRoot, "Title", Loc.T("INVITE FRIENDS"), 26,
            new Vector2(0.5f, 0.90f), new Vector2(500, 40), AccGold);
        UiKit.BrawlText(title);

        var scrollGO = new GameObject("Scroll");
        scrollGO.transform.SetParent(_inviteListRoot.transform, false);
        var scrollImg = scrollGO.AddComponent<Image>();
        scrollImg.color = new Color(0f, 0f, 0f, 0.25f);
        UiKit.Round(scrollImg, 1.2f);
        var scrollRt = scrollImg.rectTransform;
        scrollRt.anchorMin = new Vector2(0.5f, 0.20f);
        scrollRt.anchorMax = new Vector2(0.5f, 0.82f);
        scrollRt.sizeDelta = new Vector2(760, 0);
        scrollRt.anchoredPosition = Vector2.zero;

        var scroll = scrollGO.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scrollGO.AddComponent<RectMask2D>();

        var contentGO = new GameObject("Content");
        contentGO.transform.SetParent(scrollGO.transform, false);
        _inviteListContent = contentGO.AddComponent<RectTransform>();
        _inviteListContent.anchorMin = new Vector2(0f, 1f);
        _inviteListContent.anchorMax = new Vector2(1f, 1f);
        _inviteListContent.pivot     = new Vector2(0.5f, 1f);
        _inviteListContent.offsetMin = new Vector2(10, 0);
        _inviteListContent.offsetMax = new Vector2(-10, 0);

        var layout = contentGO.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 8;
        layout.padding = new RectOffset(0, 0, 10, 10);
        layout.childForceExpandHeight = false;
        layout.childControlHeight     = true;
        contentGO.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.content = _inviteListContent;

        MakeButton(_inviteListRoot, "btn_back", Loc.T("BACK"), new Color(0.30f, 0.30f, 0.45f, 1f),
            new Vector2(0.5f, 0.10f), new Vector2(220, 54), () => _inviteListRoot.SetActive(false));

        _inviteListRoot.SetActive(false);
    }

    // ── Ortak UI yardımcıları (diğer panellerdeki desenle aynı, dosya-içi kopya) ──

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

        var txt = MakeText(go, "Lbl", label, 16, new Vector2(0.5f, 0.5f), new Vector2(size.x - 10, 30), Color.white);
        txt.fontStyle = FontStyles.Bold;
        return go;
    }

    static GameObject MakeRowButton(GameObject parent, string name, string label, Color color,
        Vector2 anchoredPos, float width, UnityEngine.Events.UnityAction cb)
    {
        var go = MakeButton(parent, name, label, color, new Vector2(1f, 0.5f), new Vector2(width, 44), cb);
        go.GetComponent<RectTransform>().anchoredPosition = anchoredPos;
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
