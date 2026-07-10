using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Lobi paneli — harita seçimi, bot sayısı (test amaçlı), oyun modu.
/// Kendi Canvas'ını programatik oluşturur.
/// MenuScene'e boş bir GameObject ekleyip scripti yapıştır.
/// </summary>
public class LobbyPanelUI : MonoBehaviour
{
    public static LobbyPanelUI Instance { get; private set; }

    // ── Renk paleti ───────────────────────────────────────────────────────
    static readonly Color BgColor       = new Color(0.051f, 0.051f, 0.102f, 0.96f);
    static readonly Color CardBg        = new Color(0.09f,  0.09f,  0.18f,  1f);
    static readonly Color PrimaryBtn    = new Color(0.29f,  0.62f,  1.00f,  1f);
    static readonly Color PrimaryHover  = new Color(0.42f,  0.71f,  1.00f,  1f);
    static readonly Color DangerBtn     = new Color(1.00f,  0.267f, 0.267f, 1f);
    static readonly Color DangerHover   = new Color(1.00f,  0.40f,  0.40f,  1f);
    static readonly Color SuccessColor  = new Color(0.267f, 1.00f,  0.533f, 1f);
    static readonly Color AccentColor   = new Color(1.00f,  0.722f, 0.00f,  1f);
    static readonly Color TextSecondary = new Color(0.533f, 0.533f, 0.667f, 1f);
    static readonly Color BorderColor   = new Color(0.165f, 0.165f, 0.29f,  1f);

    // ── Runtime referanslar ───────────────────────────────────────────────
    GameObject      _root;
    int             _botCount = 0;
    TextMeshProUGUI _botCountText;
    TextMeshProUGUI _botPreviewText;
    TextMeshProUGUI _startBtnLabel;
    Button          _startBtn;

    // ─────────────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        BuildUI();
        _root.SetActive(false);
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
        _root.SetActive(true);
        RefreshStartButton();
    }

    public void Hide() => _root.SetActive(false);

    // ════════════════════════════════════════════════════════════════════
    //  UI BUILD
    // ════════════════════════════════════════════════════════════════════

    void BuildUI()
    {
        // ── Canvas ───────────────────────────────────────────────────────
        var canvasGO = new GameObject("LobbyCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 40;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // ── Root (tam ekran dolgu) ────────────────────────────────────────
        _root = new GameObject("LobbyRoot");
        _root.transform.SetParent(canvasGO.transform, false);
        var bgImg = _root.AddComponent<Image>();
        bgImg.color = BgColor;
        var rootRt = bgImg.rectTransform;
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = rootRt.offsetMax = Vector2.zero;

        // ── Title ────────────────────────────────────────────────────────
        MakeText(_root, "Title", "LOBBY", 42,
            new Vector2(0.5f, 0.93f), new Vector2(400, 55), Color.white);

        // ── Two columns ─────────────────────────────────────────────────────
        BuildLeftColumn();
        BuildRightColumn();

        // ── Bottom buttons ─────────────────────────────────────────────────
        MakeButton(_root, "btn_back", "← BACK",
            new Vector2(0.20f, 0.06f), new Vector2(160, 46),
            new Color(0.2f, 0.2f, 0.35f), new Color(0.3f, 0.3f, 0.5f), OnBackClicked);

        _root.AddComponent<EscapeListener>().OnEscape = Hide;

        var startBtnGO = MakeButtonGO(_root, "btn_start",
            new Vector2(0.65f, 0.06f), new Vector2(260, 52),
            PrimaryBtn, PrimaryHover, OnStartClicked);
        _startBtn      = startBtnGO.GetComponent<Button>();
        _startBtnLabel = startBtnGO.GetComponentInChildren<TextMeshProUGUI>();
        RefreshStartButton();
    }

    void BuildLeftColumn()
    {
        // Kart
        var card = MakeCard(_root, "LeftCard", new Vector2(0.25f, 0.55f), new Vector2(320, 400));

        MakeText(card, "hdr", "CREATE LOBBY", 18,
            new Vector2(0.5f, 0.90f), new Vector2(280, 30), Color.white);

        // Player name — "Misafir/Guest" görünmez, PlayerIdentity tek kaynak
        string playerName = PlayerIdentity.Get();

        MakeText(card, "playerName", playerName, 16,
            new Vector2(0.5f, 0.78f), new Vector2(280, 26), SuccessColor);

        // Bot count selector — test amaçlı, kontrol edilebilir bot ekler
        MakeText(card, "bot_lbl", "Bot Count (Test)", 15,
            new Vector2(0.5f, 0.64f), new Vector2(280, 24), TextSecondary);

        // [-] [0] [+]
        MakeSmallButton(card, "btn_botMinus", "−",
            new Vector2(0.25f, 0.53f), new Vector2(44, 40), OnBotMinus);

        _botCountText = MakeText(card, "bot_val", _botCount.ToString(), 22,
            new Vector2(0.5f, 0.53f), new Vector2(60, 40), Color.white);

        MakeSmallButton(card, "btn_botPlus", "+",
            new Vector2(0.75f, 0.53f), new Vector2(44, 40), OnBotPlus);

        // Preview
        _botPreviewText = MakeText(card, "bot_preview", GetBotPreviewText(), 12,
            new Vector2(0.5f, 0.43f), new Vector2(280, 24), TextSecondary);
    }

    void BuildRightColumn()
    {
        var card = MakeCard(_root, "RightCard", new Vector2(0.72f, 0.55f), new Vector2(320, 400));

        // Map
        MakeText(card, "map_hdr", "SELECT MAP", 18,
            new Vector2(0.5f, 0.90f), new Vector2(280, 30), Color.white);

        var mapCard = MakeCard(card, "MapCard",
            new Vector2(0.5f, 0.70f), new Vector2(220, 130));
        mapCard.GetComponent<Image>().color = new Color(0.18f, 0.12f, 0.30f, 1f);
        // Seçili border
        AddBorder(mapCard, AccentColor);

        // Renk dolgu (sprite gelince değiştirilir)
        var mapVisual = new GameObject("MapVisual");
        mapVisual.transform.SetParent(mapCard.transform, false);
        var mv = mapVisual.AddComponent<Image>();
        mv.color = new Color(0.12f, 0.22f, 0.45f, 1f);
        var mvRt = mv.rectTransform;
        mvRt.anchorMin = new Vector2(0.05f, 0.30f);
        mvRt.anchorMax = new Vector2(0.95f, 0.95f);
        mvRt.offsetMin = mvRt.offsetMax = Vector2.zero;

        MakeText(mapCard, "map_name", "Cosmic Arena", 14,
            new Vector2(0.5f, 0.12f), new Vector2(200, 22), Color.white);

        // Game mode
        MakeText(card, "mode_hdr", "GAME MODE", 18,
            new Vector2(0.5f, 0.36f), new Vector2(280, 30), Color.white);

        var modeCard = MakeCard(card, "ModeCard",
            new Vector2(0.5f, 0.18f), new Vector2(220, 52));
        modeCard.GetComponent<Image>().color = new Color(0.12f, 0.22f, 0.18f, 1f);
        AddBorder(modeCard, AccentColor);
        MakeText(modeCard, "mode_name", "Deathmatch", 16,
            new Vector2(0.5f, 0.5f), new Vector2(200, 34), Color.white);
    }

    // ════════════════════════════════════════════════════════════════════
    //  CALLBACKS
    // ════════════════════════════════════════════════════════════════════

    void OnBotMinus()
    {
        _botCount = Mathf.Max(0, _botCount - 1);
        RefreshBotUI();
    }

    void OnBotPlus()
    {
        _botCount = Mathf.Min(3, _botCount + 1);
        RefreshBotUI();
    }

    void RefreshBotUI()
    {
        if (_botCountText)   _botCountText.text   = _botCount.ToString();
        if (_botPreviewText) _botPreviewText.text  = GetBotPreviewText();
    }

    string GetBotPreviewText() =>
        _botCount == 0
            ? "Total: 1 player (no bots)"
            : $"Total: {_botCount + 1} players (1 human + {_botCount} test bot, all controllable)";

    void RefreshStartButton()
    {
        // Mobil akış: oturum açılışta sessizce kurulur (misafir varsayılan), giriş kapısı yok.
        if (_startBtnLabel == null) return;
        _startBtnLabel.text = "START GAME";
    }

    void OnStartClicked()
    {
        // LobbyData doldur
        LobbyData.BotCount     = _botCount;
        LobbyData.MapName      = "CosmicArena";
        LobbyData.SelectedMode = CosmicRumble.Data.GameModeType.Duel1v1;

        if (GameConfig.Instance != null) GameConfig.Instance.Save();

        SceneManager.LoadScene(SceneNames.Game);
    }

    void OnBackClicked() => Hide();

    // ════════════════════════════════════════════════════════════════════
    //  UI HELPERS
    // ════════════════════════════════════════════════════════════════════

    static TextMeshProUGUI MakeText(GameObject parent, string name, string content,
        int size, Vector2 anchor, Vector2 sizeDelta, Color color)
    {
        var go  = new GameObject(name);
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

    static GameObject MakeCard(GameObject parent, string name, Vector2 anchor, Vector2 size)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var img = go.AddComponent<Image>();
        img.color = CardBg;
        var rt  = img.rectTransform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.sizeDelta = size;
        rt.anchoredPosition = Vector2.zero;
        return go;
    }

    static void AddBorder(GameObject card, Color color)
    {
        var borderGO = new GameObject("Border");
        borderGO.transform.SetParent(card.transform, false);
        var img = borderGO.AddComponent<Image>();
        img.color = color;
        var rt  = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(-2, -2);
        rt.offsetMax = new Vector2(2, 2);
        borderGO.transform.SetAsFirstSibling();
    }

    void MakeButton(GameObject parent, string name, string label,
        Vector2 anchor, Vector2 size, Color normal, Color hover,
        UnityEngine.Events.UnityAction callback)
    {
        MakeButtonGO(parent, name, anchor, size, normal, hover, callback)
            .GetComponentInChildren<TextMeshProUGUI>().text = label;
    }

    static GameObject MakeButtonGO(GameObject parent, string name,
        Vector2 anchor, Vector2 size, Color normal, Color hover,
        UnityEngine.Events.UnityAction callback)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var img = go.AddComponent<Image>();
        img.color = normal;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.colors = new ColorBlock
        {
            normalColor      = normal,
            highlightedColor = hover,
            pressedColor     = new Color(normal.r * 0.7f, normal.g * 0.7f, normal.b * 0.7f),
            selectedColor    = hover,
            colorMultiplier  = 1f,
            fadeDuration     = 0.1f
        };
        btn.onClick.AddListener(callback);
        UiKit.Hover(go);
        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.sizeDelta = size;
        rt.anchoredPosition = Vector2.zero;

        var txtGO = new GameObject("Label");
        txtGO.transform.SetParent(go.transform, false);
        var txt   = txtGO.AddComponent<TextMeshProUGUI>();
        txt.text      = name;
        txt.fontSize  = 17;
        txt.color     = Color.white;
        txt.alignment = TextAlignmentOptions.Center;
        var trt = txt.rectTransform;
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;

        return go;
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
        btn.onClick.AddListener(callback);
        UiKit.Hover(go);
        var rt  = img.rectTransform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.sizeDelta = size;
        rt.anchoredPosition = Vector2.zero;

        var txtGO = new GameObject("Lbl"); txtGO.transform.SetParent(go.transform, false);
        var txt   = txtGO.AddComponent<TextMeshProUGUI>();
        txt.text      = label;
        txt.fontSize  = 22;
        txt.color     = Color.white;
        txt.alignment = TextAlignmentOptions.Center;
        var trt = txt.rectTransform;
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;
    }
}
