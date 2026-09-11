using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Unity.Netcode;
using CosmicRumble.Networking;
using CosmicRumble.Localization;

/// <summary>
/// Online eşleşme paneli — tek akış: HIZLI EŞLEŞME (dereceli). Eski "KOD OLUŞTUR"/"KODA KATIL"
/// kartları kaldırıldı; arkadaşla oynamak artık SOSYAL panelindeki davet sistemiyle yapılıyor
/// (PartyLobbyPanelUI). Host tarafı 2. oyuncu bağlanınca (server olarak) Game sahnesini
/// yükler — NGO bu yüklemeyi bağlı client'a otomatik yayar. Kimse gelmezse
/// <see cref="BotFallbackSeconds"/> sonunda maç bir bota karşı başlar (dereceli değil).
/// </summary>
public class OnlineLobbyPanelUI : MonoBehaviour
{
    public static OnlineLobbyPanelUI Instance { get; private set; }

    static readonly Color BgColor      = UiTheme.Card;
    static readonly Color CardBg       = UiTheme.Card;
    static readonly Color PrimaryBtn   = UiTheme.Blue;
    static readonly Color PrimaryHover = Color.Lerp(UiTheme.Blue, Color.white, 0.18f);
    static readonly Color TextSec      = UiTheme.TextMuted;
    static readonly Color CodeColor    = new Color(1.00f,  0.80f,  0.20f,  1f);

    GameObject      _panelRoot;
    GameObject      _card;
    GameObject      _quickMatchCancelBtn;
    TextMeshProUGUI _quickMatchStatusText;
    bool            _waitingForOpponent;
    bool            _connectionActive;   // QuickMatch tıklandıktan sonra, LeaveSessionAsync'e kadar true
    Coroutine       _botFallback;

    /// <summary>Rakip beklerken bu süre dolarsa maç bir bota karşı başlar. Havuzda kimse
    /// yokken oyuncuyu süresiz bekletmek, oyunun tek başına oynanamaz görünmesi demekti.</summary>
    const float BotFallbackSeconds = 30f;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        BuildUI();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
    }

    /// <summary>
    /// Eşleşme kurulmuş ama maç henüz başlamamışken (rakip bekleniyor / host'un sahne yüklemesi
    /// bekleniyor) uygulama arka plana atılır veya kapatılırsa oturumu temizler — bkz.
    /// PartyLobbyPanelUI'daki aynı deseni.
    /// </summary>
    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && _connectionActive) _ = CancelConnectionAsync();
    }

    void OnApplicationQuit()
    {
        if (_connectionActive) _ = CancelConnectionAsync();
    }

    // ════════════════════════════════════════════════════════════════════
    //  PUBLIC API
    // ════════════════════════════════════════════════════════════════════

    public void Show()
    {
        _panelRoot.SetActive(true);
        _quickMatchStatusText.text = "";
        _quickMatchCancelBtn.SetActive(false);
    }

    /// <summary>Ana menüdeki büyük sarı OYNA butonu için: paneli açıp hızlı eşleşmeyi
    /// hemen başlatır — oyuncu "Rakip aranıyor..." durumunu ve İPTAL butonunu direkt görür,
    /// tekrar OYNA'ya basmasına gerek kalmaz.</summary>
    public void ShowAndStartQuickMatch()
    {
        Show();
        OnQuickMatchClicked();
    }

    public void Hide() => _panelRoot.SetActive(false);

    // ════════════════════════════════════════════════════════════════════
    //  HIZLI EŞLEŞME (Quick Match — ana akış)
    // ════════════════════════════════════════════════════════════════════

    async void OnQuickMatchClicked()
    {
        if (_connectionActive) return; // double-tap guard — already matchmaking/connected

        _quickMatchStatusText.text = Loc.T("Searching for opponent...");
        _connectionActive = true;
        bool ok = await NetworkBootstrap.Instance.QuickMatchAsync();

        if (!ok)
        {
            _quickMatchStatusText.text = Loc.T("Matchmaking failed, try again.");
            _connectionActive = false;
            return;
        }

        if (NetworkBootstrap.Instance.IsHostAfterQuickMatch)
        {
            // Havuzda bekleyen kimse yoktu, kendi genel oturumumuzu kurduk — rakip bekliyoruz.
            // Katılım kodu bilerek GÖSTERİLMEZ: bu dereceli bir oturum, koda bir arkadaş katılırsa
            // taraflar maçın dereceli olup olmadığı konusunda uyuşmazdı.
            _quickMatchStatusText.text = Loc.T("Waiting for opponent...");
            _waitingForOpponent = true;
            _quickMatchCancelBtn.SetActive(true);
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            _botFallback = StartCoroutine(BotFallbackCountdown());
        }
        else
        {
            // Bekleyen bir oturuma katıldık — host'un sahneyi yüklemesini bekliyoruz.
            _quickMatchStatusText.text = Loc.T("Opponent found, starting...");
        }
    }

    async void OnQuickMatchCancelClicked()
    {
        _quickMatchStatusText.text = Loc.T("Cancelling...");
        await CancelConnectionAsync();
        _quickMatchStatusText.text = "";
        _quickMatchCancelBtn.SetActive(false);
    }

    void OnClientConnected(ulong clientId)
    {
        Debug.Log($"[NET] ClientConnected id={clientId} isHost={NetworkManager.Singleton.IsHost} totalClients={NetworkManager.Singleton.ConnectedClientsIds.Count}");

        if (!_waitingForOpponent) return;
        if (!NetworkManager.Singleton.IsServer) return;
        if (NetworkManager.Singleton.ConnectedClientsIds.Count < 2) return;

        _waitingForOpponent = false;
        StopBotFallback();
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        NetworkManager.Singleton.SceneManager.LoadScene(SceneNames.Game, LoadSceneMode.Single);
    }

    /// <summary>Rakip beklerken geri sayar; süre dolunca maçı bir bota karşı başlatır.
    /// Bu maç DERECELİ DEĞİLDİR — bota karşı kupa kazanmak sıralamayı anlamsız kılardı.</summary>
    System.Collections.IEnumerator BotFallbackCountdown()
    {
        for (float left = BotFallbackSeconds; left > 0f; left -= 1f)
        {
            _quickMatchStatusText.text =
                string.Format(Loc.T("Waiting for opponent... ({0})"), Mathf.CeilToInt(left));
            yield return new WaitForSeconds(1f);
        }

        if (!_waitingForOpponent || NetworkManager.Singleton == null ||
            !NetworkManager.Singleton.IsServer) yield break;

        _quickMatchStatusText.text = Loc.T("No opponent found - starting against a bot");
        _waitingForOpponent = false;
        _botFallback        = null;
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;

        LobbyData.OnlineBotFill = 1;
        NetworkBootstrap.Instance?.MarkUnranked();

        yield return new WaitForSeconds(1f);
        NetworkManager.Singleton.SceneManager.LoadScene(SceneNames.Game, LoadSceneMode.Single);
    }

    void StopBotFallback()
    {
        if (_botFallback == null) return;
        StopCoroutine(_botFallback);
        _botFallback = null;
    }

    // Maç başladıktan sonraki bağlantı kopmaları artık burada değil,
    // NetworkBootstrap'ın kalıcı (DontDestroyOnLoad) durum banner'ı + reconnect döngüsü
    // tarafından ele alınıyor (bkz. NetworkBootstrap.cs) — bu panel MenuScene'e özel olduğu
    // için Game sahnesine geçildikten sonra zaten var olamıyordu, gerçek bir mid-match
    // kopuşu hiçbir zaman burada yakalanamazdı.

    async System.Threading.Tasks.Task CancelConnectionAsync()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        StopBotFallback();
        LobbyData.OnlineBotFill = 0;
        _waitingForOpponent = false;
        _connectionActive = false;
        await NetworkBootstrap.Instance.LeaveSessionAsync();
    }

    async void OnBackClicked()
    {
        if (_connectionActive) await CancelConnectionAsync();
        Hide();
    }

    // ════════════════════════════════════════════════════════════════════
    //  UI BUILD
    // ════════════════════════════════════════════════════════════════════

    void BuildUI()
    {
        var canvasGO = new GameObject("OnlineLobbyCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 45;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        _panelRoot = new GameObject("OnlineLobbyRoot");
        _panelRoot.transform.SetParent(canvasGO.transform, false);
        var overlay = _panelRoot.AddComponent<Image>();
        overlay.color = new Color(0, 0, 0, 0.65f);
        StretchFull(overlay.rectTransform);

        // Panelin govdesi kart; onceden baslik ve iki kutu dogrudan perdenin uzerindeydi,
        // baslik ana menu basligiyla, alttaki GERI de hizli eslesme seridiyle cakisiyordu.
        _card = MakeCard(_panelRoot, "Card", new Vector2(0.5f, 0.5f), new Vector2(900, 700));
        UiKit.Stroke(_card, UiTheme.Stroke);
        UiKit.Pop(_card);

        MakeText(_card, "Title", Loc.T("ONLINE"), 30,
            new Vector2(0.5f, 0.925f), new Vector2(600, 46), UiTheme.Gold);

        UiKit.CloseButton(_card, () => OnBackClicked());

        BuildQuickMatchCard();
        BuildSocialHint();

        _panelRoot.AddComponent<EscapeListener>().OnEscape = OnBackClicked;
        _panelRoot.SetActive(false);
    }

    void BuildQuickMatchCard()
    {
        var card = MakeCard(_card, "QuickMatchCard", new Vector2(0.5f, 0.60f), new Vector2(760, 300));

        MakeText(card, "hdr", Loc.T("QUICK MATCH — RANKED"), 24, new Vector2(0.5f, 0.88f), new Vector2(500, 36), Color.white);
        MakeText(card, "hint", Loc.T("Win +30 trophies  •  Loss −20 trophies"), 14,
            new Vector2(0.5f, 0.76f), new Vector2(500, 24), TextSec);
        // Birincil eylem: büyük, yeşil OYNA (mobil ana akış)
        MakeSmallButton(card, "btn_quickmatch", Loc.T("PLAY"),
            new Vector2(0.5f, 0.54f), new Vector2(340, 72), OnQuickMatchClicked,
            new Color(0.13f, 0.72f, 0.35f, 1f));

        _quickMatchStatusText = MakeText(card, "status", "", 16,
            new Vector2(0.5f, 0.24f), new Vector2(500, 90), CodeColor);

        _quickMatchCancelBtn = MakeSmallButton(card, "btn_quickmatch_cancel", Loc.T("CANCEL"),
            new Vector2(0.5f, 0.11f), new Vector2(220, 72), OnQuickMatchCancelClicked,
            new Color(0.30f, 0.30f, 0.45f, 1f));
        _quickMatchCancelBtn.SetActive(false);
    }

    /// <summary>Eski kod kartlarının yerine: arkadaşla oynamanın artık davetle olduğunu anlatan
    /// ipucu plakası + SOSYAL kısayolu.</summary>
    void BuildSocialHint()
    {
        var card = MakeCard(_card, "SocialHint", new Vector2(0.5f, 0.22f), new Vector2(760, 190));

        MakeText(card, "hint", Loc.T("Send an invite from the SOCIAL panel to play\nwith a friend — friendly match, trophies unaffected."), 15,
            new Vector2(0.5f, 0.68f), new Vector2(520, 48), TextSec);

        MakeSmallButton(card, "btn_social", Loc.T("SOCIAL"),
            new Vector2(0.5f, 0.24f), new Vector2(220, 72),
            () => { Hide(); SocialPanelUI.Instance?.Show(); },
            new Color(0.15f, 0.70f, 0.75f, 1f));
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

    static GameObject MakeCard(GameObject parent, string name, Vector2 anchor, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var img = go.AddComponent<Image>();
        img.color = CardBg;
        UiKit.Round(img);
        UiKit.Stroke(go, UiTheme.Stroke);
        UiKit.Shadow(go, 6f, 0.50f);
        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.sizeDelta = size;
        rt.anchoredPosition = Vector2.zero;
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
        var rt = txt.rectTransform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.sizeDelta = sizeDelta;
        rt.anchoredPosition = Vector2.zero;
        return txt;
    }

    static GameObject MakeSmallButton(GameObject parent, string name, string label,
        Vector2 anchor, Vector2 size, UnityEngine.Events.UnityAction callback)
        => MakeSmallButton(parent, name, label, anchor, size, callback, PrimaryBtn);

    static GameObject MakeSmallButton(GameObject parent, string name, string label,
        Vector2 anchor, Vector2 size, UnityEngine.Events.UnityAction callback, Color color)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        UiKit.Round(img);
        UiKit.BottomEdge(go, UiKit.EdgeOf(color));
        UiKit.Shadow(go, 4f, 0.40f);
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.colors = UiKit.ButtonColors(color);
        btn.onClick.AddListener(callback);
        UiKit.Hover(go);
        var rt  = img.rectTransform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.sizeDelta = size;
        rt.anchoredPosition = Vector2.zero;

        var txtGO = new GameObject("Lbl");
        txtGO.transform.SetParent(go.transform, false);
        var txt   = txtGO.AddComponent<TextMeshProUGUI>();
        txt.text      = label;
        txt.fontSize  = 17;
        txt.color     = Color.white;
        txt.alignment = TextAlignmentOptions.Center;
        var trt = txt.rectTransform;
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;

        return go;
    }
}
