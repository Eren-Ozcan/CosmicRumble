using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CosmicRumble.Economy;

/// <summary>
/// Sağ üst köşe profil kartı.
/// Giriş yapılmamış → "GİRİŞ YAP" butonu.
/// Giriş yapılmış   → kullanıcı adı, level, XP bar, gold + "ÇIKIŞ" butonu.
/// </summary>
public class MainMenuAuthButton : MonoBehaviour
{
    public static MainMenuAuthButton Instance { get; private set; }

    // ── Palette ──────────────────────────────────────────────────────────────
    static readonly Color CardBg        = new Color(0.07f, 0.07f, 0.16f, 0.96f);
    static readonly Color AccentBlue    = new Color(0.22f, 0.45f, 0.95f, 1.00f);
    static readonly Color AccentBlueHov = new Color(0.32f, 0.58f, 1.00f, 1.00f);
    static readonly Color AccentGold    = new Color(1.00f, 0.80f, 0.20f, 1.00f);
    static readonly Color AccentGreen   = new Color(0.14f, 0.70f, 0.25f, 1.00f);
    static readonly Color AccentRed     = new Color(0.70f, 0.14f, 0.14f, 1.00f);
    static readonly Color AccentRedHov  = new Color(0.85f, 0.20f, 0.20f, 1.00f);
    static readonly Color TextPrimary   = Color.white;
    static readonly Color TextDim       = new Color(0.60f, 0.65f, 0.78f, 1.00f);
    static readonly Color BarBg         = new Color(0.12f, 0.12f, 0.24f, 1.00f);
    static readonly Color Separator     = new Color(0.25f, 0.25f, 0.40f, 0.55f);

    // ── Referanslar ───────────────────────────────────────────────────────────
    GameObject      _cardRoot;
    GameObject      _loggedOutPanel;
    GameObject      _loggedInPanel;

    // Logged-in elemanlar
    TextMeshProUGUI _usernameTxt;
    TextMeshProUGUI _levelTxt;
    TextMeshProUGUI _goldTxt;
    TextMeshProUGUI _xpLabel;
    Image           _xpFill;

    // ─────────────────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        BuildUI();
        Refresh();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ════════════════════════════════════════════════════════════════════════
    //  PUBLIC API
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>Oturum değişince dışarıdan çağrılır (LoginPanelUI vb.).</summary>
    public void RefreshButton() => Refresh();

    // ════════════════════════════════════════════════════════════════════════
    //  BUILD
    // ════════════════════════════════════════════════════════════════════════

    void BuildUI()
    {
        // Canvas
        var canvasGO = new GameObject("AuthButtonCanvas");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // ── Kart kök ────────────────────────────────────────────────────────
        _cardRoot = new GameObject("ProfileCard");
        _cardRoot.transform.SetParent(canvasGO.transform, false);
        var cardImg = _cardRoot.AddComponent<Image>();
        cardImg.color = CardBg;
        var cardRt  = cardImg.rectTransform;
        cardRt.anchorMin        = new Vector2(1f, 1f);
        cardRt.anchorMax        = new Vector2(1f, 1f);
        cardRt.pivot            = new Vector2(1f, 1f);
        cardRt.sizeDelta        = new Vector2(240, 44);   // genişler logged-in'de
        cardRt.anchoredPosition = new Vector2(-16, -16);

        // Sol kenar aksanı
        var sideGO  = new GameObject("SideAccent");
        sideGO.transform.SetParent(_cardRoot.transform, false);
        var sideImg = sideGO.AddComponent<Image>();
        sideImg.color = AccentBlue;
        var sideRt  = sideImg.rectTransform;
        sideRt.anchorMin        = new Vector2(0f, 0f);
        sideRt.anchorMax        = new Vector2(0f, 1f);
        sideRt.pivot            = new Vector2(0f, 0.5f);
        sideRt.sizeDelta        = new Vector2(3, 0);
        sideRt.anchoredPosition = Vector2.zero;

        // ── Giriş yapılmamış panel ───────────────────────────────────────────
        _loggedOutPanel = new GameObject("LoggedOutPanel");
        _loggedOutPanel.transform.SetParent(_cardRoot.transform, false);
        var loRt = _loggedOutPanel.AddComponent<RectTransform>();
        loRt.anchorMin = Vector2.zero; loRt.anchorMax = Vector2.one;
        loRt.offsetMin = loRt.offsetMax = Vector2.zero;

        var loginBtn = MakeButton(_loggedOutPanel, "LoginBtn", "LOG IN",
            AccentBlue, AccentBlueHov, () => LoginPanelUI.Instance?.Show());
        StretchFull(loginBtn.GetComponent<RectTransform>());

        // ── Giriş yapılmış panel ─────────────────────────────────────────────
        _loggedInPanel = new GameObject("LoggedInPanel");
        _loggedInPanel.transform.SetParent(_cardRoot.transform, false);
        var liRt = _loggedInPanel.AddComponent<RectTransform>();
        liRt.anchorMin = Vector2.zero; liRt.anchorMax = Vector2.one;
        liRt.offsetMin = liRt.offsetMax = Vector2.zero;

        BuildLoggedInContent(_loggedInPanel);
    }

    void BuildLoggedInContent(GameObject parent)
    {
        // ── Satır 1: Kullanıcı adı + ÇIKIŞ butonu ───────────────────────────
        _usernameTxt = MakeTxt(parent, "Username", "—", 14, FontStyles.Bold, TextPrimary,
            TextAlignmentOptions.Left,
            new Vector2(0f, 1f), new Vector2(0.68f, 1f), new Vector2(10, -8), new Vector2(-4, -30));

        var logoutBtn = MakeButton(parent, "LogoutBtn", "LOG OUT",
            AccentRed, AccentRedHov, OnLogout);
        var lbRt = logoutBtn.GetComponent<RectTransform>();
        lbRt.anchorMin        = new Vector2(0.72f, 1f);
        lbRt.anchorMax        = new Vector2(1f, 1f);
        lbRt.pivot            = new Vector2(1f, 1f);
        lbRt.offsetMin        = new Vector2(0, -32);
        lbRt.offsetMax        = new Vector2(-6, -6);

        // ── Divider ──────────────────────────────────────────────────────────
        var divGO  = new GameObject("Div");
        divGO.transform.SetParent(parent.transform, false);
        var divImg = divGO.AddComponent<Image>();
        divImg.color = Separator;
        var divRt  = divImg.rectTransform;
        divRt.anchorMin = new Vector2(0.02f, 1f);
        divRt.anchorMax = new Vector2(0.98f, 1f);
        divRt.pivot     = new Vector2(0.5f, 1f);
        divRt.sizeDelta        = new Vector2(0, 1);
        divRt.anchoredPosition = new Vector2(0, -36);

        // ── Satır 2: Level | Gold ─────────────────────────────────────────────
        _levelTxt = MakeTxt(parent, "Level", "Lv.1", 13, FontStyles.Bold, AccentGold,
            TextAlignmentOptions.Left,
            new Vector2(0f, 1f), new Vector2(0.45f, 1f), new Vector2(10, -42), new Vector2(0, -58));

        _goldTxt = MakeTxt(parent, "Gold", "⬡ 0", 13, FontStyles.Normal, AccentGold,
            TextAlignmentOptions.Right,
            new Vector2(0.5f, 1f), new Vector2(1f, 1f), new Vector2(0, -42), new Vector2(-8, -58));

        // ── XP label ─────────────────────────────────────────────────────────
        _xpLabel = MakeTxt(parent, "XPLabel", "XP 0 / 100", 11, FontStyles.Normal, TextDim,
            TextAlignmentOptions.Left,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(10, -62), new Vector2(-8, -74));

        // ── XP bar ───────────────────────────────────────────────────────────
        var xpBgGO  = new GameObject("XPBarBg");
        xpBgGO.transform.SetParent(parent.transform, false);
        var xpBgImg = xpBgGO.AddComponent<Image>();
        xpBgImg.color = BarBg;
        var xpBgRt  = xpBgGO.GetComponent<RectTransform>();
        xpBgRt.anchorMin = new Vector2(0.02f, 1f);
        xpBgRt.anchorMax = new Vector2(0.98f, 1f);
        xpBgRt.pivot     = new Vector2(0.5f, 1f);
        xpBgRt.sizeDelta        = new Vector2(0, 7);
        xpBgRt.anchoredPosition = new Vector2(0, -80);

        var xpFillGO  = new GameObject("XPFill");
        xpFillGO.transform.SetParent(xpBgGO.transform, false);
        _xpFill = xpFillGO.AddComponent<Image>();
        _xpFill.color = AccentBlue;
        var xpFillRt = _xpFill.rectTransform;
        xpFillRt.anchorMin = Vector2.zero;
        xpFillRt.anchorMax = new Vector2(0f, 1f);   // fill güncellenir
        xpFillRt.offsetMin = xpFillRt.offsetMax = Vector2.zero;
    }

    // ════════════════════════════════════════════════════════════════════════
    //  REFRESH
    // ════════════════════════════════════════════════════════════════════════

    void Refresh()
    {
        bool loggedIn = AuthManager.Instance != null && AuthManager.Instance.IsLoggedIn;

        _loggedOutPanel.SetActive(!loggedIn);
        _loggedInPanel.SetActive(loggedIn);

        // Kart yüksekliğini ayarla
        var cardRt = _cardRoot.GetComponent<RectTransform>();
        cardRt.sizeDelta = new Vector2(240, loggedIn ? 96 : 44);

        if (!loggedIn) return;

        // Username
        _usernameTxt.text = $"● {AuthManager.Instance.CurrentUsername}";

        // Level
        int   level      = 1;
        long  xpIn       = 0;
        long  xpNeeded   = 100;
        float xpProgress = 0f;

        if (PlayerLevelManager.Instance != null)
        {
            var prog  = PlayerLevelManager.Instance.GetProgress();
            level      = prog.currentLevel;
            xpIn       = prog.xpInCurrentLevel;
            xpNeeded   = prog.xpNeededForNextLevel > 0 ? prog.xpNeededForNextLevel : 100;
            xpProgress = prog.levelProgress;
        }
        _levelTxt.text = $"Lv.{level}";

        // Gold
        long gold = CurrencyManager.Instance?.Get(CurrencyType.Gold) ?? 0L;
        _goldTxt.text = $"⬡ {gold}";

        // XP
        _xpLabel.text = $"XP  {xpIn} / {xpNeeded}";

        // XP bar fill
        float fill = Mathf.Clamp01(xpProgress);
        _xpFill.rectTransform.anchorMax = new Vector2(fill, 1f);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  CALLBACKS
    // ════════════════════════════════════════════════════════════════════════

    void OnLogout()
    {
        AuthManager.Instance?.Logout();
        Refresh();
    }

    // ════════════════════════════════════════════════════════════════════════
    //  UI HELPERS
    // ════════════════════════════════════════════════════════════════════════

    static Button MakeButton(GameObject parent, string name, string label,
                              Color normal, Color hover,
                              UnityEngine.Events.UnityAction cb)
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
            pressedColor     = new Color(normal.r * 0.65f, normal.g * 0.65f, normal.b * 0.65f),
            selectedColor    = hover,
            colorMultiplier  = 1f,
            fadeDuration     = 0.10f
        };
        btn.onClick.AddListener(cb);

        var txtGO = new GameObject("Lbl");
        txtGO.transform.SetParent(go.transform, false);
        var txt = txtGO.AddComponent<TextMeshProUGUI>();
        txt.text      = label;
        txt.fontSize  = 13;
        txt.fontStyle = FontStyles.Bold;
        txt.color     = Color.white;
        txt.alignment = TextAlignmentOptions.Center;
        var trt = txt.rectTransform;
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;

        return btn;
    }

    /// <summary>Stretch içinde değil, anchor bazlı metin (offsetMin/Max ile sınır).</summary>
    static TextMeshProUGUI MakeTxt(GameObject parent, string name, string content,
                                    int size, FontStyles style, Color color,
                                    TextAlignmentOptions align,
                                    Vector2 anchorMin, Vector2 anchorMax,
                                    Vector2 offsetMin, Vector2 offsetMax)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var txt = go.AddComponent<TextMeshProUGUI>();
        txt.text      = content;
        txt.fontSize  = size;
        txt.fontStyle = style;
        txt.color     = color;
        txt.alignment = align;
        txt.overflowMode = TextOverflowModes.Ellipsis;
        var rt = txt.rectTransform;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
        return txt;
    }

    static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
