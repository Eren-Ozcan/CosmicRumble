using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Unity.Netcode;
using CosmicRumble.Networking;

/// <summary>
/// Online eşleşme paneli — tek akış: HIZLI EŞLEŞME (dereceli). Eski "KOD OLUŞTUR"/"KODA KATIL"
/// kartları kaldırıldı; arkadaşla oynamak artık SOSYAL panelindeki davet sistemiyle yapılıyor
/// (FriendLobbyPanelUI). Host tarafı 2. oyuncu bağlanınca (server olarak) Game sahnesini
/// yükler — NGO bu yüklemeyi bağlı client'a otomatik yayar.
/// </summary>
public class OnlineLobbyPanelUI : MonoBehaviour
{
    public static OnlineLobbyPanelUI Instance { get; private set; }

    static readonly Color BgColor      = new Color(0.051f, 0.051f, 0.102f, 0.97f);
    static readonly Color CardBg       = new Color(0.09f,  0.09f,  0.18f,  1f);
    static readonly Color PrimaryBtn   = new Color(0.29f,  0.62f,  1.00f,  1f);
    static readonly Color PrimaryHover = new Color(0.42f,  0.71f,  1.00f,  1f);
    static readonly Color TextSec      = new Color(0.533f, 0.533f, 0.667f, 1f);
    static readonly Color CodeColor    = new Color(1.00f,  0.80f,  0.20f,  1f);

    GameObject      _panelRoot;
    GameObject      _quickMatchCancelBtn;
    TextMeshProUGUI _quickMatchStatusText;
    bool            _waitingForOpponent;
    bool            _connectionActive;   // QuickMatch tıklandıktan sonra, LeaveSessionAsync'e kadar true

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

    // ════════════════════════════════════════════════════════════════════
    //  PUBLIC API
    // ════════════════════════════════════════════════════════════════════

    public void Show()
    {
        _panelRoot.SetActive(true);
        _quickMatchStatusText.text = "";
        _quickMatchCancelBtn.SetActive(false);
    }

    public void Hide() => _panelRoot.SetActive(false);

    // ════════════════════════════════════════════════════════════════════
    //  HIZLI EŞLEŞME (Quick Match — ana akış)
    // ════════════════════════════════════════════════════════════════════

    async void OnQuickMatchClicked()
    {
        _quickMatchStatusText.text = "Rakip aranıyor...";
        _connectionActive = true;
        bool ok = await NetworkBootstrap.Instance.QuickMatchAsync();

        if (!ok)
        {
            _quickMatchStatusText.text = "Eşleşme başarısız, tekrar dene.";
            _connectionActive = false;
            return;
        }

        if (NetworkBootstrap.Instance.IsHostAfterQuickMatch)
        {
            // Havuzda bekleyen kimse yoktu, kendi genel oturumumuzu kurduk — rakip bekliyoruz.
            // Katılım kodu bilerek GÖSTERİLMEZ: bu dereceli bir oturum, koda bir arkadaş katılırsa
            // taraflar maçın dereceli olup olmadığı konusunda uyuşmazdı.
            _quickMatchStatusText.text = "Rakip bekleniyor...";
            _waitingForOpponent = true;
            _quickMatchCancelBtn.SetActive(true);
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }
        else
        {
            // Bekleyen bir oturuma katıldık — host'un sahneyi yüklemesini bekliyoruz.
            _quickMatchStatusText.text = "Rakip bulundu, başlatılıyor...";
        }
    }

    async void OnQuickMatchCancelClicked()
    {
        _quickMatchStatusText.text = "İptal ediliyor...";
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
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        NetworkManager.Singleton.SceneManager.LoadScene(SceneNames.Game, LoadSceneMode.Single);
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

        MakeText(_panelRoot, "Title", "ONLINE MULTIPLAYER", 32,
            new Vector2(0.5f, 0.90f), new Vector2(600, 50), Color.white);

        BuildQuickMatchCard();
        BuildSocialHint();

        MakeSmallButton(_panelRoot, "btn_back", "GERİ",
            new Vector2(0.5f, 0.06f), new Vector2(200, 56), OnBackClicked,
            new Color(0.30f, 0.30f, 0.45f, 1f));

        _panelRoot.AddComponent<EscapeListener>().OnEscape = OnBackClicked;
        _panelRoot.SetActive(false);
    }

    void BuildQuickMatchCard()
    {
        var card = MakeCard(_panelRoot, "QuickMatchCard", new Vector2(0.5f, 0.55f), new Vector2(560, 260));

        MakeText(card, "hdr", "HIZLI EŞLEŞME — DERECELİ", 24, new Vector2(0.5f, 0.88f), new Vector2(500, 36), Color.white);
        MakeText(card, "hint", "Galibiyet +30 kupa  •  Mağlubiyet −20 kupa", 14,
            new Vector2(0.5f, 0.76f), new Vector2(500, 24), TextSec);
        // Birincil eylem: büyük, yeşil OYNA (mobil ana akış)
        MakeSmallButton(card, "btn_quickmatch", "OYNA",
            new Vector2(0.5f, 0.54f), new Vector2(340, 72), OnQuickMatchClicked,
            new Color(0.13f, 0.72f, 0.35f, 1f));

        _quickMatchStatusText = MakeText(card, "status", "", 16,
            new Vector2(0.5f, 0.24f), new Vector2(500, 90), CodeColor);

        _quickMatchCancelBtn = MakeSmallButton(card, "btn_quickmatch_cancel", "İPTAL ET",
            new Vector2(0.5f, 0.11f), new Vector2(220, 48), OnQuickMatchCancelClicked,
            new Color(0.30f, 0.30f, 0.45f, 1f));
        _quickMatchCancelBtn.SetActive(false);
    }

    /// <summary>Eski kod kartlarının yerine: arkadaşla oynamanın artık davetle olduğunu anlatan
    /// ipucu plakası + SOSYAL kısayolu.</summary>
    void BuildSocialHint()
    {
        var card = MakeCard(_panelRoot, "SocialHint", new Vector2(0.5f, 0.26f), new Vector2(560, 120));

        MakeText(card, "hint", "Arkadaşınla oynamak için SOSYAL panelinden\ndavet gönder — dostluk maçı, kupa değişmez.", 15,
            new Vector2(0.5f, 0.68f), new Vector2(520, 48), TextSec);

        MakeSmallButton(card, "btn_social", "SOSYAL",
            new Vector2(0.5f, 0.24f), new Vector2(220, 48),
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
        UiKit.Shadow(go, 4f, 0.40f);
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.colors = UiKit.ButtonColors(color);
        btn.onClick.AddListener(callback);
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
