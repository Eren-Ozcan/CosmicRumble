using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CosmicRumble.Localization;

/// <summary>
/// Tam ekran giriş kapısı (popup DEĞİL). BootstrapSequence, adlı/platform hesabı olmayan
/// oyuncuya açılışta gösterir; bir giriş tamamlanana kadar bekler (ShowAndWaitAsync).
/// Kapatma yolu yok — kapı rolü tamamen burada, LoginPanelUI artık sadece Cosmic ID form kartı.
/// - GOOGLE İLE DEVAM ET: Android + GPGS (sessiz deneme başarısızsa buradan etkileşimli).
/// - COSMIC ID İLE GİRİŞ: mevcut LoginPanelUI kartını üstte açar (kullanıcı adı/şifre).
/// - Editor: MİSAFİR OLARAK DEVAM (TEST) — hotseat testleri girişe takılmasın.
/// Canvas sıralaması 85 (Loading 90'ın altında, LoginPanel kartı 95'in altında).
/// </summary>
public class LoginScreenUI : MonoBehaviour
{
    public static LoginScreenUI Instance { get; private set; }

    static readonly Color GradTop      = new Color(0.17f, 0.10f, 0.40f, 1f);
    static readonly Color GradBottom   = new Color(0.34f, 0.10f, 0.33f, 1f);
    static readonly Color AccGold      = new Color(1.00f, 0.80f, 0.20f, 1f);
    static readonly Color TextDim      = new Color(0.65f, 0.70f, 0.82f, 1f);
    static readonly Color GoogleWhite  = new Color(0.97f, 0.97f, 0.97f, 1f);
    static readonly Color PlateDark    = new Color(0.165f, 0.175f, 0.215f, 1f);
    static readonly Color ErrorRed     = new Color(1f, 0.35f, 0.35f, 1f);

    GameObject      _root;
    TextMeshProUGUI _statusText;
    Button          _googleBtn;
    Button          _cosmicBtn;
    bool            _busy;
    TaskCompletionSource<bool> _waitTcs;

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
        if (AuthManager.Instance != null)
            AuthManager.Instance.OnSignedIn -= HandleSignedIn;
    }

    // ── Public API ───────────────────────────────────────────────────────

    /// <summary>Ekranı gösterir ve bir giriş tamamlanana kadar bekleyen Task döner.
    /// Cosmic ID Login() sahneyi yeniden yüklerse Task hiç tamamlanmaz — sorun değil,
    /// yeni BootstrapSequence sessiz restore ile devam eder.</summary>
    public Task<bool> ShowAndWaitAsync()
    {
        _root.SetActive(true);
        _busy = false;
        SetButtonsInteractable(true);
        _statusText.text = "";
        _waitTcs = new TaskCompletionSource<bool>();

        if (AuthManager.Instance != null)
        {
            AuthManager.Instance.OnSignedIn -= HandleSignedIn;
            AuthManager.Instance.OnSignedIn += HandleSignedIn;
        }
        return _waitTcs.Task;
    }

    public void Hide() => _root.SetActive(false);

    // ── Callbacks ────────────────────────────────────────────────────────

    void HandleSignedIn()
    {
        // Register (aynı oturuma kimlik ekleme) buraya düşer; Login sahneyi zaten yeniler.
        Hide();
        _waitTcs?.TrySetResult(true);
    }

#if UNITY_ANDROID && GPGS_INSTALLED
    async void OnGoogleClicked()
    {
        if (_busy || AuthManager.Instance == null) return;
        _busy = true;
        SetButtonsInteractable(false);
        ShowStatus(Loc.T("Connecting with Google..."));

        var (success, error) = await AuthManager.Instance.SignInWithPlatformAsync(
            CosmicRumble.Auth.GooglePlayAuthProvider.Shared, silent: false);

        _busy = false;
        SetButtonsInteractable(true);
        if (!success) ShowError(error ?? Loc.T("Google sign-in failed."));
        // Başarı → OnSignedIn event'i HandleSignedIn'i tetikler (veya sahne yenilenir).
    }
#endif

    void OnCosmicIdClicked()
    {
        if (_busy) return;
        // Kart kapatılabilir — kapatınca bu tam ekran kapı hâlâ arkada, kapı bozulmaz.
        LoginPanelUI.Instance?.Show(dismissable: true);
    }

#if UNITY_EDITOR
    async void OnGuestClicked()
    {
        if (_busy || AuthManager.Instance == null) return;
        _busy = true;
        SetButtonsInteractable(false);
        await AuthManager.Instance.LoginAsGuest();
        Hide();
        _waitTcs?.TrySetResult(true);
    }
#endif

    // ── Helpers ──────────────────────────────────────────────────────────

    void SetButtonsInteractable(bool on)
    {
        if (_googleBtn != null) _googleBtn.interactable = on;
        if (_cosmicBtn != null) _cosmicBtn.interactable = on;
    }

    void ShowStatus(string msg) { _statusText.color = TextDim;   _statusText.text = msg; }
    void ShowError(string msg)  { _statusText.color = ErrorRed;  _statusText.text = msg; }

    // ── UI Build ─────────────────────────────────────────────────────────

    void BuildUI()
    {
        var canvasGO = new GameObject("LoginScreenCanvas");
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 85;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();
        EnsureEventSystem();

        _root = new GameObject("LoginScreenRoot");
        _root.transform.SetParent(canvasGO.transform, false);
        var rootRt = _root.AddComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = rootRt.offsetMax = Vector2.zero;

        // Arka plan
        var bgGO  = new GameObject("Bg");
        bgGO.transform.SetParent(_root.transform, false);
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.color = Color.white;
        UiKit.Gradient(bgImg, GradTop, GradBottom);
        var bgRt = bgImg.rectTransform;
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;

        // Başlık
        var titleGO = new GameObject("Title");
        titleGO.transform.SetParent(_root.transform, false);
        var title = titleGO.AddComponent<TextMeshProUGUI>();
        title.text      = "COSMIC RUMBLE";
        title.fontSize  = 56;
        title.alignment = TextAlignmentOptions.Center;
        title.color     = AccGold;
        UiKit.BrawlText(title);
        var titleRt = title.rectTransform;
        titleRt.anchorMin = titleRt.anchorMax = new Vector2(0.5f, 0.74f);
        titleRt.sizeDelta = new Vector2(900, 76);
        titleRt.anchoredPosition = Vector2.zero;

        // Alt başlık
        var subGO = new GameObject("Sub");
        subGO.transform.SetParent(_root.transform, false);
        var sub = subGO.AddComponent<TextMeshProUGUI>();
        sub.text      = Loc.T("Sign in with your account to continue");
        sub.fontSize  = 20;
        sub.alignment = TextAlignmentOptions.Center;
        sub.color     = TextDim;
        var subRt = sub.rectTransform;
        subRt.anchorMin = subRt.anchorMax = new Vector2(0.5f, 0.655f);
        subRt.sizeDelta = new Vector2(700, 30);
        subRt.anchoredPosition = Vector2.zero;

        // ── Buton kolonu ────────────────────────────────────────────────
        float y = 0.52f;

#if UNITY_ANDROID && GPGS_INSTALLED
        _googleBtn = MakeBigButton(_root, "btn_google", Loc.T("CONTINUE WITH GOOGLE"),
            new Vector2(0.5f, y), GoogleWhite, new Color(0.15f, 0.15f, 0.18f, 1f), OnGoogleClicked);
        y -= 0.10f;
#endif

        _cosmicBtn = MakeBigButton(_root, "btn_cosmic", Loc.T("SIGN IN WITH COSMIC ID"),
            new Vector2(0.5f, y), PlateDark, Color.white, OnCosmicIdClicked);
        y -= 0.10f;

#if UNITY_EDITOR
        MakeBigButton(_root, "btn_guest_test", Loc.T("CONTINUE AS GUEST (TEST)"),
            new Vector2(0.5f, y), new Color(0.10f, 0.10f, 0.16f, 1f), TextDim, OnGuestClicked, 22);
        y -= 0.10f;
#endif

        // Durum/hata satırı
        var statusGO = new GameObject("Status");
        statusGO.transform.SetParent(_root.transform, false);
        _statusText = statusGO.AddComponent<TextMeshProUGUI>();
        _statusText.fontSize  = 17;
        _statusText.alignment = TextAlignmentOptions.Center;
        _statusText.color     = TextDim;
        var statusRt = _statusText.rectTransform;
        statusRt.anchorMin = statusRt.anchorMax = new Vector2(0.5f, y);
        statusRt.sizeDelta = new Vector2(700, 30);
        statusRt.anchoredPosition = Vector2.zero;
    }

    static Button MakeBigButton(GameObject parent, string name, string label,
        Vector2 anchor, Color bg, Color textColor,
        UnityEngine.Events.UnityAction callback, int fontSize = 24)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var img = go.AddComponent<Image>();
        img.color = bg;
        UiKit.Round(img);
        UiKit.Shadow(go, 4f, 0.4f);
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.colors = UiKit.ButtonColors(bg);
        btn.onClick.AddListener(callback);
        UiKit.Press(go);
        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.sizeDelta = new Vector2(460, 72);
        rt.anchoredPosition = Vector2.zero;

        var txtGO = new GameObject("Label");
        txtGO.transform.SetParent(go.transform, false);
        var txt = txtGO.AddComponent<TextMeshProUGUI>();
        txt.text      = label;
        txt.fontSize  = fontSize;
        txt.fontStyle = FontStyles.Bold;
        txt.color     = textColor;
        txt.alignment = TextAlignmentOptions.Center;
        var trt = txt.rectTransform;
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;

        return btn;
    }

    static void EnsureEventSystem()
    {
        if (UnityEngine.Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() != null) return;
        var es = new GameObject("EventSystem");
        es.AddComponent<UnityEngine.EventSystems.EventSystem>();
        es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
    }
}
