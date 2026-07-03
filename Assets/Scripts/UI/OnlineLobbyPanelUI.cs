using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Unity.Netcode;
using CosmicRumble.Networking;

/// <summary>
/// Online 2 oyunculu eşleşme paneli (Host/Join) — mevcut yerel hotseat "PLAY" akışından
/// (LobbyPanelUI, dokunulmadı) tamamen ayrı, ana menüde ayrı bir "ONLINE" butonuyla açılır.
/// Host: bir Relay oturumu oluşturur, katılım kodunu gösterir, 2. oyuncu bağlanınca (server
/// olarak) Game sahnesini yükler — NGO bu yüklemeyi bağlı client'a otomatik yayar.
/// Join: girilen kodla mevcut oturuma bağlanır, sahne yüklemesini host'tan bekler.
/// MenuScene'e boş bir GameObject ekleyip scripti yapıştır.
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
    TextMeshProUGUI _hostStatusText;
    TextMeshProUGUI _joinStatusText;
    TMP_InputField  _codeInput;
    bool            _waitingForOpponent;

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
        _hostStatusText.text = "";
        _joinStatusText.text = "";
        _codeInput.text      = "";
    }

    public void Hide() => _panelRoot.SetActive(false);

    // ════════════════════════════════════════════════════════════════════
    //  HOST / JOIN
    // ════════════════════════════════════════════════════════════════════

    async void OnHostClicked()
    {
        _hostStatusText.text = "Oturum oluşturuluyor...";
        string code = await NetworkBootstrap.Instance.HostSessionAsync();

        if (string.IsNullOrEmpty(code))
        {
            _hostStatusText.text = "Oluşturulamadı, tekrar dene.";
            return;
        }

        _hostStatusText.text = $"Kod: {code}\nRakip bekleniyor...";
        _waitingForOpponent  = true;
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    async void OnJoinClicked()
    {
        string code = _codeInput.text.Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(code))
        {
            _joinStatusText.text = "Bir kod gir.";
            return;
        }

        _joinStatusText.text = "Bağlanılıyor...";
        bool ok = await NetworkBootstrap.Instance.JoinSessionAsync(code);
        _joinStatusText.text = ok ? "Bağlandı, host başlatmasını bekleniyor..." : "Bağlanamadı, kodu kontrol et.";
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

    void OnBackClicked() => Hide();

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
            new Vector2(0.5f, 0.85f), new Vector2(600, 50), Color.white);

        BuildHostCard();
        BuildJoinCard();

        MakeSmallButton(_panelRoot, "btn_back", "← BACK",
            new Vector2(0.5f, 0.10f), new Vector2(160, 46), OnBackClicked);

        _panelRoot.AddComponent<EscapeListener>().OnEscape = Hide;
        _panelRoot.SetActive(false);
    }

    void BuildHostCard()
    {
        var card = MakeCard(_panelRoot, "HostCard", new Vector2(0.28f, 0.5f), new Vector2(420, 420));

        MakeText(card, "hdr", "HOST", 22, new Vector2(0.5f, 0.90f), new Vector2(360, 34), Color.white);
        MakeSmallButton(card, "btn_host", "HOST OLUŞTUR",
            new Vector2(0.5f, 0.72f), new Vector2(260, 54), OnHostClicked);

        _hostStatusText = MakeText(card, "status", "", 16,
            new Vector2(0.5f, 0.45f), new Vector2(380, 160), CodeColor);
    }

    void BuildJoinCard()
    {
        var card = MakeCard(_panelRoot, "JoinCard", new Vector2(0.72f, 0.5f), new Vector2(420, 420));

        MakeText(card, "hdr", "JOIN", 22, new Vector2(0.5f, 0.90f), new Vector2(360, 34), Color.white);

        _codeInput = MakeInputField(card, "codeInput",
            new Vector2(0.5f, 0.72f), new Vector2(260, 50));

        MakeSmallButton(card, "btn_join", "KATIL",
            new Vector2(0.5f, 0.58f), new Vector2(260, 54), OnJoinClicked);

        _joinStatusText = MakeText(card, "status", "", 16,
            new Vector2(0.5f, 0.40f), new Vector2(380, 120), TextSec);
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

    static TMP_InputField MakeInputField(GameObject parent, string name, Vector2 anchor, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.9f);
        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.sizeDelta = size;
        rt.anchoredPosition = Vector2.zero;

        var textGO = new GameObject("Text");
        textGO.transform.SetParent(go.transform, false);
        var text = textGO.AddComponent<TextMeshProUGUI>();
        text.fontSize  = 22;
        text.color     = Color.black;
        text.alignment = TextAlignmentOptions.Left;
        text.margin    = new Vector4(12, 4, 12, 4);
        var trt = text.rectTransform;
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;

        var placeholderGO = new GameObject("Placeholder");
        placeholderGO.transform.SetParent(go.transform, false);
        var placeholder = placeholderGO.AddComponent<TextMeshProUGUI>();
        placeholder.text      = "KOD";
        placeholder.fontSize  = 22;
        placeholder.color     = new Color(0, 0, 0, 0.4f);
        placeholder.alignment = TextAlignmentOptions.Left;
        placeholder.margin    = new Vector4(12, 4, 12, 4);
        var prt = placeholder.rectTransform;
        prt.anchorMin = Vector2.zero; prt.anchorMax = Vector2.one;
        prt.offsetMin = prt.offsetMax = Vector2.zero;

        var input = go.AddComponent<TMP_InputField>();
        input.textViewport      = rt;
        input.textComponent     = text;
        input.placeholder       = placeholder;
        input.characterLimit    = 6;
        input.characterValidation = TMP_InputField.CharacterValidation.Alphanumeric;

        return input;
    }

    static void MakeSmallButton(GameObject parent, string name, string label,
        Vector2 anchor, Vector2 size, UnityEngine.Events.UnityAction callback)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var img = go.AddComponent<Image>();
        img.color = PrimaryBtn;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.colors = new ColorBlock
        {
            normalColor      = PrimaryBtn,
            highlightedColor = PrimaryHover,
            pressedColor     = new Color(PrimaryBtn.r * 0.7f, PrimaryBtn.g * 0.7f, PrimaryBtn.b * 0.7f),
            selectedColor    = PrimaryHover,
            colorMultiplier  = 1f,
            fadeDuration     = 0.1f
        };
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
    }
}
