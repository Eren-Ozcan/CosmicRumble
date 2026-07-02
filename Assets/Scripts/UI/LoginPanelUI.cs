using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Giriş / Kayıt paneli.
/// Kendi Canvas'ını programatik olarak oluşturur.
/// MenuScene'e boş bir GameObject ekleyip bu scripti yapıştır.
/// </summary>
public class LoginPanelUI : MonoBehaviour
{
    public static LoginPanelUI Instance { get; private set; }

    // ── Renk paleti ───────────────────────────────────────────────────────
    static readonly Color BgPanelColor  = new Color(0.051f, 0.051f, 0.102f, 0.96f);
    static readonly Color PrimaryBtn    = new Color(0.29f,  0.62f,  1.00f,  1f);
    static readonly Color PrimaryHover  = new Color(0.42f,  0.71f,  1.00f,  1f);
    static readonly Color DangerBtn     = new Color(1.00f,  0.267f, 0.267f, 1f);
    static readonly Color DangerHover   = new Color(1.00f,  0.40f,  0.40f,  1f);
    static readonly Color BorderColor   = new Color(0.165f, 0.165f, 0.29f,  1f);
    static readonly Color TextSecondary = new Color(0.533f, 0.533f, 0.667f, 1f);

    // ── Referanslar ───────────────────────────────────────────────────────
    GameObject        _root;
    RectTransform     _card;
    TMP_InputField    _userInput;
    TMP_InputField    _passInput;
    TextMeshProUGUI   _errorText;

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
        ClearError();
    }

    public void Hide() => _root.SetActive(false);

    // ════════════════════════════════════════════════════════════════════
    //  UI BUILD
    // ════════════════════════════════════════════════════════════════════

    void BuildUI()
    {
        // Canvas
        var canvasGO = new GameObject("LoginCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // Yarı saydam arka plan kaplaması
        _root = new GameObject("LoginRoot");
        _root.transform.SetParent(canvasGO.transform, false);
        var overlay = _root.AddComponent<Image>();
        overlay.color = new Color(0, 0, 0, 0.6f);
        var overlayRt = overlay.rectTransform;
        overlayRt.anchorMin = Vector2.zero;
        overlayRt.anchorMax = Vector2.one;
        overlayRt.offsetMin = overlayRt.offsetMax = Vector2.zero;

        // Kart (400×500)
        var cardGO = new GameObject("Card");
        cardGO.transform.SetParent(_root.transform, false);
        var cardImg = cardGO.AddComponent<Image>();
        cardImg.color = BgPanelColor;
        _card = cardImg.rectTransform;
        _card.anchorMin = _card.anchorMax = new Vector2(0.5f, 0.5f);
        _card.sizeDelta = new Vector2(400, 560);
        _card.anchoredPosition = Vector2.zero;

        // Başlık
        MakeText(cardGO, "Title", "CosmicRumble", 36,
            new Vector2(0.5f, 0.87f), new Vector2(360, 50), Color.white);

        // Username input
        MakeLabel(cardGO, "lbl_user", "Username", new Vector2(0.5f, 0.72f));
        _userInput = MakeInputField(cardGO, "inp_user", "Username",
            new Vector2(0.5f, 0.62f), new Vector2(320, 44), false);

        // Password input
        MakeLabel(cardGO, "lbl_pass", "Password", new Vector2(0.5f, 0.50f));
        _passInput = MakeInputField(cardGO, "inp_pass", "Password",
            new Vector2(0.5f, 0.40f), new Vector2(320, 44), true);

        // Error message
        _errorText = MakeText(cardGO, "err_text", "", 14,
            new Vector2(0.5f, 0.30f), new Vector2(340, 30), new Color(1f, 0.3f, 0.3f));
        _errorText.gameObject.SetActive(false);

        // Buttons
        MakeButton(cardGO, "btn_login",    "LOG IN",      new Vector2(0.5f, 0.23f),
            new Vector2(320, 44), PrimaryBtn, PrimaryHover, OnLoginClicked);
        MakeButton(cardGO, "btn_register", "REGISTER",        new Vector2(0.5f, 0.14f),
            new Vector2(320, 44), DangerBtn,  DangerHover,  OnRegisterClicked);
        MakeButton(cardGO, "btn_guest",    "PLAY AS GUEST", new Vector2(0.5f, 0.05f),
            new Vector2(320, 40),
            new Color(0.25f, 0.25f, 0.30f), new Color(0.35f, 0.35f, 0.42f),
            OnGuestClicked);

        // ESC dinleyici
        _root.AddComponent<EscapeListener>().OnEscape = Hide;

        // Tab → şifre alanına geç
        var tabNav = _root.AddComponent<TabNavigator>();
        tabNav.fields = new TMP_InputField[] { _userInput, _passInput };
    }

    // ════════════════════════════════════════════════════════════════════
    //  CALLBACKS
    // ════════════════════════════════════════════════════════════════════

    void OnLoginClicked()
    {
        string user = _userInput.text.Trim();
        string pass = _passInput.text;

        if (AuthManager.Instance == null) { ShowError("AuthManager not found."); return; }
        if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pass))
        {
            ShowError("Username and password are required.");
            StartCoroutine(Shake());
            return;
        }

        if (AuthManager.Instance.Login(user, pass))
        {
            ClearError();
            Hide();
            MainMenuAuthButton.Instance?.RefreshButton();
        }
        else
        {
            ShowError("Incorrect username or password.");
            StartCoroutine(Shake());
        }
    }

    void OnRegisterClicked()
    {
        string user = _userInput.text.Trim();
        string pass = _passInput.text;

        if (AuthManager.Instance == null) { ShowError("AuthManager not found."); return; }
        if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pass))
        {
            ShowError("Username and password are required.");
            StartCoroutine(Shake());
            return;
        }

        if (AuthManager.Instance.Register(user, pass))
        {
            // Auto-login after registering
            AuthManager.Instance.Login(user, pass);
            ClearError();
            Hide();
            MainMenuAuthButton.Instance?.RefreshButton();
        }
        else
        {
            ShowError("This username is already taken.");
            StartCoroutine(Shake());
        }
    }

    void OnGuestClicked()
    {
        if (AuthManager.Instance == null) return;
        AuthManager.Instance.LoginAsGuest();
        Hide();
        MainMenuAuthButton.Instance?.RefreshButton();
        LobbyPanelUI.Instance?.Show();
    }

    // ════════════════════════════════════════════════════════════════════
    //  HELPERS
    // ════════════════════════════════════════════════════════════════════

    void ShowError(string msg)
    {
        if (_errorText == null) return;
        _errorText.text = msg;
        _errorText.gameObject.SetActive(true);
    }

    void ClearError()
    {
        if (_errorText == null) return;
        _errorText.text = "";
        _errorText.gameObject.SetActive(false);
    }

    IEnumerator Shake()
    {
        if (_card == null) yield break;
        Vector2 origin = _card.anchoredPosition;
        float duration = 0.35f, elapsed = 0f;
        while (elapsed < duration)
        {
            float x = origin.x + Mathf.Sin(elapsed * 60f) * 12f * (1f - elapsed / duration);
            _card.anchoredPosition = new Vector2(x, origin.y);
            elapsed += Time.deltaTime;
            yield return null;
        }
        _card.anchoredPosition = origin;
    }

    // ── UI builder helpers ────────────────────────────────────────────────

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

    static void MakeLabel(GameObject parent, string name, string content, Vector2 anchor)
    {
        MakeText(parent, name, content, 14, anchor, new Vector2(320, 24), TextSecondary);
    }

    static TMP_InputField MakeInputField(GameObject parent, string name, string placeholder,
        Vector2 anchor, Vector2 size, bool isPassword)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.12f, 0.12f, 0.22f, 1f);
        var rt  = img.rectTransform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.sizeDelta = size;
        rt.anchoredPosition = Vector2.zero;

        var field = go.AddComponent<TMP_InputField>();
        if (isPassword) field.contentType = TMP_InputField.ContentType.Password;

        // Text area
        var areaGO = new GameObject("TextArea"); areaGO.transform.SetParent(go.transform, false);
        var areaRt = areaGO.AddComponent<RectTransform>();
        areaRt.anchorMin = Vector2.zero; areaRt.anchorMax = Vector2.one;
        areaRt.offsetMin = new Vector2(8, 2); areaRt.offsetMax = new Vector2(-8, -2);

        // Placeholder text
        var phGO  = new GameObject("Placeholder"); phGO.transform.SetParent(areaGO.transform, false);
        var phTxt = phGO.AddComponent<TextMeshProUGUI>();
        phTxt.text      = placeholder;
        phTxt.fontSize  = 15;
        phTxt.color     = new Color(0.5f, 0.5f, 0.6f, 1f);
        phTxt.alignment = TextAlignmentOptions.Left;
        var phRt = phTxt.rectTransform;
        phRt.anchorMin = Vector2.zero; phRt.anchorMax = Vector2.one;
        phRt.offsetMin = phRt.offsetMax = Vector2.zero;

        // Main text
        var txtGO  = new GameObject("Text"); txtGO.transform.SetParent(areaGO.transform, false);
        var mainTxt = txtGO.AddComponent<TextMeshProUGUI>();
        mainTxt.fontSize  = 15;
        mainTxt.color     = Color.white;
        mainTxt.alignment = TextAlignmentOptions.Left;
        var txtRt = mainTxt.rectTransform;
        txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = txtRt.offsetMax = Vector2.zero;

        field.textViewport   = areaRt;
        field.textComponent  = mainTxt;
        field.placeholder    = phTxt;
        field.targetGraphic  = img;

        return field;
    }

    static void MakeButton(GameObject parent, string name, string label,
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
        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.sizeDelta = size;
        rt.anchoredPosition = Vector2.zero;

        var txtGO = new GameObject("Label"); txtGO.transform.SetParent(go.transform, false);
        var txt   = txtGO.AddComponent<TextMeshProUGUI>();
        txt.text      = label;
        txt.fontSize  = 16;
        txt.color     = Color.white;
        txt.alignment = TextAlignmentOptions.Center;
        var trt = txt.rectTransform;
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;
    }
}

/// <summary>ESC tuşunu dinleyen yardımcı bileşen.</summary>
public class EscapeListener : MonoBehaviour
{
    public System.Action OnEscape;
    void Update() { if (Input.GetKeyDown(KeyCode.Escape)) OnEscape?.Invoke(); }
}

/// <summary>Tab tuşuyla input alanları arasında gezinme.</summary>
public class TabNavigator : MonoBehaviour
{
    public TMP_InputField[] fields;
    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Tab) || fields == null || fields.Length == 0) return;
        for (int i = 0; i < fields.Length; i++)
        {
            if (fields[i] != null && fields[i].isFocused)
            {
                int next = (i + 1) % fields.Length;
                fields[next]?.Select();
                fields[next]?.ActivateInputField();
                return;
            }
        }
    }
}
