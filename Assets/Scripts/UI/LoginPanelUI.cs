using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CosmicRumble.Localization;

/// <summary>
/// Cosmic ID form kartı (kullanıcı adı/şifre ile giriş/kayıt). Açılış kapısı rolü artık tam
/// ekran LoginScreenUI'da — bu kart iki yerden, hep kapatılabilir halde açılır:
/// 1) LoginScreenUI → "COSMIC ID İLE GİRİŞ" (kapatınca tam ekran kapı arkada durur).
/// 2) Ayarlar → Hesap → COSMIC ID satırındaki BAĞLA.
/// Kendi Canvas'ını programatik oluşturur (order 95); UiKit stilinde.
/// </summary>
public class LoginPanelUI : MonoBehaviour
{
    public static LoginPanelUI Instance { get; private set; }

#if UNITY_EDITOR
    // Editor-only test hesabı — UGS şifre kuralını karşılar (8-30 karakter,
    // en az 1 büyük harf, 1 küçük harf, 1 rakam, 1 sembol). Sadece "TEST" butonuyla doldurulur.
    const string TestUsername = "testuser1";
    const string TestPassword = "Test1234!";
#endif

    // ── Renk paleti (UiKit mobil teması) ──────────────────────────────────
    static readonly Color CardBg        = new Color(0.07f,  0.07f,  0.16f,  0.97f);
    static readonly Color PrimaryBtn    = new Color(0.29f,  0.62f,  1.00f,  1f);
    static readonly Color RegisterBtn   = new Color(0.16f,  0.72f,  0.26f,  1f);
    static readonly Color InputBg       = new Color(0.12f,  0.12f,  0.22f,  1f);
    static readonly Color TextSecondary = new Color(0.533f, 0.533f, 0.667f, 1f);
    static readonly Color StrokeCol     = new Color(1f, 1f, 1f, 0.09f);

    // ── Referanslar ───────────────────────────────────────────────────────
    GameObject        _root;
    RectTransform     _card;
    TMP_InputField    _userInput;
    TMP_InputField    _passInput;
    TextMeshProUGUI   _errorText;
    TextMeshProUGUI   _titleText;
    TextMeshProUGUI   _hintText;
    GameObject        _closeBtn;
    EscapeListener    _escListener;
    Button            _loginBtn;
    Button            _registerBtn;
    bool              _busy;

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

    /// <param name="dismissable">false = açılış kapısı (X/ESC yok, giriş zorunlu);
    /// true = Ayarlar'dan açılan hesap bağlama diyaloğu.</param>
    public void Show(bool dismissable = true)
    {
        _root.SetActive(true);
        _busy = false;
        SetButtonsInteractable(true);
        ClearError();

        _closeBtn.SetActive(dismissable);
        _escListener.enabled = dismissable;
        _titleText.text = dismissable ? Loc.T("LINK YOUR ACCOUNT") : Loc.T("SIGN IN");
        _hintText.text  = dismissable
            ? Loc.T("Linking an account keeps your progress saved and\nlets you continue from other devices.")
            : Loc.T("Sign in to your account to continue\nor create a new one.");
    }

    public void Hide() => _root.SetActive(false);

    // ════════════════════════════════════════════════════════════════════
    //  UI BUILD
    // ════════════════════════════════════════════════════════════════════

    void BuildUI()
    {
        // Canvas
        var canvasGO = new GameObject("LoginCanvas");
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        // Tam ekran LoginScreenUI'ın (85) ÜSTÜNDE açılan Cosmic ID form kartı.
        canvas.sortingOrder = 95;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // Yarı saydam arka plan kaplaması
        _root = new GameObject("LoginRoot");
        _root.transform.SetParent(canvasGO.transform, false);
        var overlay = _root.AddComponent<Image>();
        overlay.color = new Color(0, 0, 0, 0.65f);
        var overlayRt = overlay.rectTransform;
        overlayRt.anchorMin = Vector2.zero;
        overlayRt.anchorMax = Vector2.one;
        overlayRt.offsetMin = overlayRt.offsetMax = Vector2.zero;

        // Kart
        var cardGO = new GameObject("Card");
        cardGO.transform.SetParent(_root.transform, false);
        var cardImg = cardGO.AddComponent<Image>();
        cardImg.color = CardBg;
        UiKit.Round(cardImg);
        UiKit.Shadow(cardGO, 8f, 0.55f);
        UiKit.Stroke(cardGO, StrokeCol);
        UiKit.Pop(cardGO);
        _card = cardImg.rectTransform;
        _card.anchorMin = _card.anchorMax = new Vector2(0.5f, 0.5f);
        _card.sizeDelta = new Vector2(440, 540);
        _card.anchoredPosition = Vector2.zero;

        _titleText = MakeText(cardGO, "Title", Loc.T("SIGN IN"), 28,
            new Vector2(0.5f, 0.90f), new Vector2(380, 42), Color.white);
        _titleText.fontStyle = FontStyles.Bold;

        _hintText = MakeText(cardGO, "Hint", "",
            13, new Vector2(0.5f, 0.80f), new Vector2(400, 40), TextSecondary);

        UiKit.CloseButton(cardGO, Hide);
        _closeBtn = cardGO.transform.Find("btn_close").gameObject;

        // Username input
        MakeLabel(cardGO, "lbl_user", Loc.T("Username"), new Vector2(0.5f, 0.685f));
        _userInput = MakeInputField(cardGO, "inp_user", Loc.T("Username"),
            new Vector2(0.5f, 0.60f), new Vector2(340, 50), false);

        // Password input
        MakeLabel(cardGO, "lbl_pass", Loc.T("Password"), new Vector2(0.5f, 0.505f));
        _passInput = MakeInputField(cardGO, "inp_pass", Loc.T("Password"),
            new Vector2(0.5f, 0.42f), new Vector2(340, 50), true);

        // Error message
        _errorText = MakeText(cardGO, "err_text", "", 14,
            new Vector2(0.5f, 0.325f), new Vector2(380, 30), new Color(1f, 0.3f, 0.3f));
        _errorText.gameObject.SetActive(false);

        // Buttons
        _loginBtn = MakeButton(cardGO, "btn_login", Loc.T("SIGN IN"), new Vector2(0.5f, 0.23f),
            new Vector2(340, 54), PrimaryBtn, OnLoginClicked);
        _registerBtn = MakeButton(cardGO, "btn_register", Loc.T("CREATE NEW ACCOUNT"), new Vector2(0.5f, 0.115f),
            new Vector2(340, 54), RegisterBtn, OnRegisterClicked);

        MakeText(cardGO, "RegisterHint", Loc.T("A new account inherits your progress on this device as-is."), 12,
            new Vector2(0.5f, 0.045f), new Vector2(400, 22), TextSecondary);

#if UNITY_EDITOR
        MakeButton(cardGO, "btn_fill_test", Loc.T("FILL TEST CREDENTIALS"), new Vector2(0.5f, -0.03f),
            new Vector2(340, 32), TextSecondary, FillTestCredentials);
#endif

        // ESC dinleyici (yalnızca dismissable modda etkin — Show() yönetir)
        _escListener = _root.AddComponent<EscapeListener>();
        _escListener.OnEscape = Hide;

        // Tab → şifre alanına geç
        var tabNav = _root.AddComponent<TabNavigator>();
        tabNav.fields = new TMP_InputField[] { _userInput, _passInput };
    }

    // ════════════════════════════════════════════════════════════════════
    //  CALLBACKS
    // ════════════════════════════════════════════════════════════════════

    async void OnLoginClicked()
    {
        if (_busy) return;
        string user = _userInput.text.Trim();
        string pass = _passInput.text;

        if (AuthManager.Instance == null) { ShowError(Loc.T("AuthManager not found.")); return; }
        if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pass))
        {
            ShowError(Loc.T("Username and password required."));
            StartCoroutine(Shake());
            return;
        }

        await RunAuthAction(AuthManager.Instance.Login(user, pass), Loc.T("Signing in..."));
    }

    async void OnRegisterClicked()
    {
        if (_busy) return;
        string user = _userInput.text.Trim();
        string pass = _passInput.text;

        if (AuthManager.Instance == null) { ShowError(Loc.T("AuthManager not found.")); return; }
        if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pass))
        {
            ShowError(Loc.T("Username and password required."));
            StartCoroutine(Shake());
            return;
        }

        await RunAuthAction(AuthManager.Instance.Register(user, pass), Loc.T("Creating account..."));
    }

    /// <summary>Login()/Register() ortak akışı: yükleniyor durumunu göster, sonucu bekle, başarı/hata işle.</summary>
    async Task RunAuthAction(Task<(bool success, string error)> action, string statusMessage)
    {
        _busy = true;
        SetButtonsInteractable(false);
        ShowStatus(statusMessage);

        var (success, error) = await action;

        _busy = false;
        SetButtonsInteractable(true);

        if (success)
        {
            ClearError();
            Hide();
        }
        else
        {
            ShowError(error);
            StartCoroutine(Shake());
        }
    }

#if UNITY_EDITOR
    void FillTestCredentials()
    {
        _userInput.text = TestUsername;
        _passInput.text = TestPassword;
    }
#endif

    void SetButtonsInteractable(bool interactable)
    {
        if (_loginBtn    != null) _loginBtn.interactable    = interactable;
        if (_registerBtn != null) _registerBtn.interactable = interactable;
    }

    // ════════════════════════════════════════════════════════════════════
    //  HELPERS
    // ════════════════════════════════════════════════════════════════════

    void ShowError(string msg)
    {
        if (_errorText == null) return;
        _errorText.color = new Color(1f, 0.3f, 0.3f);
        _errorText.text  = msg;
        _errorText.gameObject.SetActive(true);
    }

    void ShowStatus(string msg)
    {
        if (_errorText == null) return;
        _errorText.color = TextSecondary;
        _errorText.text  = msg;
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
        var lbl = MakeText(parent, name, content, 13, anchor, new Vector2(340, 22), TextSecondary);
        lbl.alignment = TextAlignmentOptions.Left;
    }

    static TMP_InputField MakeInputField(GameObject parent, string name, string placeholder,
        Vector2 anchor, Vector2 size, bool isPassword)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var img = go.AddComponent<Image>();
        img.color = InputBg;
        UiKit.Round(img, 1.5f);
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
        areaRt.offsetMin = new Vector2(12, 2); areaRt.offsetMax = new Vector2(-12, -2);

        // Placeholder text
        var phGO  = new GameObject("Placeholder"); phGO.transform.SetParent(areaGO.transform, false);
        var phTxt = phGO.AddComponent<TextMeshProUGUI>();
        phTxt.text      = placeholder;
        phTxt.fontSize  = 16;
        phTxt.color     = new Color(0.5f, 0.5f, 0.6f, 1f);
        phTxt.alignment = TextAlignmentOptions.Left;
        var phRt = phTxt.rectTransform;
        phRt.anchorMin = Vector2.zero; phRt.anchorMax = Vector2.one;
        phRt.offsetMin = phRt.offsetMax = Vector2.zero;

        // Main text
        var txtGO  = new GameObject("Text"); txtGO.transform.SetParent(areaGO.transform, false);
        var mainTxt = txtGO.AddComponent<TextMeshProUGUI>();
        mainTxt.fontSize  = 16;
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

    static Button MakeButton(GameObject parent, string name, string label,
        Vector2 anchor, Vector2 size, Color normal,
        UnityEngine.Events.UnityAction callback)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var img = go.AddComponent<Image>();
        img.color = normal;
        UiKit.Round(img);
        UiKit.Shadow(go, 3f, 0.35f);
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.colors = UiKit.ButtonColors(normal);
        btn.onClick.AddListener(callback);
        UiKit.Press(go);
        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.sizeDelta = size;
        rt.anchoredPosition = Vector2.zero;

        var txtGO = new GameObject("Label"); txtGO.transform.SetParent(go.transform, false);
        var txt   = txtGO.AddComponent<TextMeshProUGUI>();
        txt.text      = label;
        txt.fontSize  = 17;
        txt.fontStyle = FontStyles.Bold;
        txt.color     = Color.white;
        txt.alignment = TextAlignmentOptions.Center;
        var trt = txt.rectTransform;
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;

        return btn;
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
