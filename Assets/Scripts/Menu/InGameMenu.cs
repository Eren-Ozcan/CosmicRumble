using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// SampleScene'e boş bir GameObject ekle, bu scripti yapıştır — bitti.
/// ESC → menü açılır/kapanır. Oyun DURMUYOR (Time.timeScale değişmez).
///
/// Menüde: Devam Et | Ayarlar | Ana Menüye Dön
/// </summary>
public class InGameMenu : MonoBehaviour
{
    [Header("Tuş")]
    [SerializeField] KeyCode toggleKey = KeyCode.Escape;

    // ── Runtime referanslar ───────────────────────────────────────
    GameObject _root;
    GameObject _mainPanel;
    GameObject _settingsPanel;

    Slider  _masterSlider, _musicSlider, _sfxSlider;
    Toggle  _fsToggle;

    bool _isOpen;

    // ── Renkler ───────────────────────────────────────────────────
    readonly Color _overlayColor  = new Color(0f, 0f, 0f, 0.65f);
    readonly Color _panelColor    = new Color(0.06f, 0.06f, 0.15f, 0.97f);
    readonly Color _btnColor      = new Color(0.15f, 0.35f, 0.7f, 1f);
    readonly Color _btnDanger     = new Color(0.6f, 0.1f, 0.1f, 1f);
    readonly Color _btnResume     = new Color(0.1f, 0.55f, 0.15f, 1f);

    // ═════════════════════════════════════════════════════════════
    //  AWAKE — UI inşa et, kapalı başla
    // ═════════════════════════════════════════════════════════════

    void Awake()
    {
        BuildUI();
        SetOpen(false);
    }

    // ═════════════════════════════════════════════════════════════
    //  UPDATE — ESC dinle
    // ═════════════════════════════════════════════════════════════

    void Update()
    {
        // Don't intercept ESC while free camera is active
        if (FreeCameraController.IsActive) return;

        if (Input.GetKeyDown(toggleKey))
            SetOpen(!_isOpen);
    }

    // ═════════════════════════════════════════════════════════════
    //  PANEL AÇMA / KAPAMA
    // ═════════════════════════════════════════════════════════════

    void SetOpen(bool open)
    {
        _isOpen = open;
        _root.SetActive(open);

        if (open)
        {
            LoadSettingsValues();
            ShowMain();
        }
    }

    void ShowMain()
    {
        _mainPanel.SetActive(true);
        _settingsPanel.SetActive(false);
    }

    void ShowSettings()
    {
        _mainPanel.SetActive(false);
        _settingsPanel.SetActive(true);
    }

    // ═════════════════════════════════════════════════════════════
    //  CALLBACKS
    // ═════════════════════════════════════════════════════════════

    void OnResume()       => SetOpen(false);

    void OnQuitToMenu()
    {
        GameConfig.Instance?.Save();
        if (SceneFader.Instance != null)
            SceneFader.Instance.FadeToScene(SceneNames.Menu);
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene(SceneNames.Menu);
    }

    // ═════════════════════════════════════════════════════════════
    //  UI BUILD
    // ═════════════════════════════════════════════════════════════

    void BuildUI()
    {
        EnsureSingletons();

        // Root canvas
        var canvasGO = new GameObject("InGameMenuCanvas");
        canvasGO.transform.SetParent(transform, false);
        var canvas  = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        var scaler  = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var esGO = new GameObject("EventSystem");
            esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        _root = canvasGO;

        // Yarı saydam overlay (tıklamayı bloklasın ama görüntü geçsin)
        var overlay = MakeRect(_root, "Overlay");
        var overlayImg = overlay.AddComponent<Image>();
        overlayImg.color = _overlayColor;
        StretchFull(overlayImg.rectTransform);

        // ── Ana Menü Paneli ──────────────────────────────────────
        _mainPanel = MakeCenteredPanel("MainPanel", 320, 340);
        _mainPanel.transform.SetParent(_root.transform, false);

        MakeTitle(_mainPanel, "GAME MENU");
        MakeBtn(_mainPanel, "Resume",             0,    OnResume,     _btnResume);
        MakeBtn(_mainPanel, "Settings",         -70,  ShowSettings, _btnColor);
        MakeBtn(_mainPanel, "Return to Main Menu", -140, OnQuitToMenu, _btnDanger);

        // ── Settings Panel ───────────────────────────────────────
        _settingsPanel = MakeCenteredPanel("SettingsPanel", 360, 420);
        _settingsPanel.transform.SetParent(_root.transform, false);

        MakeTitle(_settingsPanel, "SETTINGS");

        _masterSlider = MakeLabeledSlider(_settingsPanel, "Master Volume", 0f, 1f, 0.8f,  80);
        _musicSlider  = MakeLabeledSlider(_settingsPanel, "Music",         0f, 1f, 0.7f,   0);
        _sfxSlider    = MakeLabeledSlider(_settingsPanel, "SFX",           0f, 1f, 1f,    -80);

        _masterSlider.onValueChanged.AddListener(v => {
            if (GameConfig.Instance != null) GameConfig.Instance.MasterVolume = v; });
        _musicSlider.onValueChanged.AddListener(v => {
            if (GameConfig.Instance != null) GameConfig.Instance.MusicVolume  = v; });
        _sfxSlider.onValueChanged.AddListener(v => {
            if (GameConfig.Instance != null) GameConfig.Instance.SfxVolume    = v; });

        MakeBtn(_settingsPanel, "← Back", -195, () => {
            GameConfig.Instance?.Save();
            ShowMain();
        }, _btnColor);
    }

    void LoadSettingsValues()
    {
        var cfg = GameConfig.Instance;
        if (cfg == null) return;
        if (_masterSlider) _masterSlider.value = cfg.MasterVolume;
        if (_musicSlider)  _musicSlider.value  = cfg.MusicVolume;
        if (_sfxSlider)    _sfxSlider.value    = cfg.SfxVolume;
    }

    // ═════════════════════════════════════════════════════════════
    //  UI HELPERS
    // ═════════════════════════════════════════════════════════════

    void EnsureSingletons()
    {
        if (GameConfig.Instance == null)
            new GameObject("GameConfig").AddComponent<GameConfig>();
        if (SceneFader.Instance == null)
            new GameObject("SceneFader").AddComponent<SceneFader>();
    }

    GameObject MakeCenteredPanel(string name, float w, float h)
    {
        var go  = new GameObject(name);
        var img = go.AddComponent<Image>();
        img.color = _panelColor;
        var rt  = img.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = Vector2.zero;
        return go;
    }

    void MakeTitle(GameObject parent, string text)
    {
        var go  = new GameObject("Title");
        go.transform.SetParent(parent.transform, false);
        var txt = go.AddComponent<TextMeshProUGUI>();
        txt.text      = text;
        txt.fontSize  = 26;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color     = Color.white;
        var rt = txt.rectTransform;
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
        rt.pivot     = new Vector2(0.5f, 1f);
        rt.offsetMin = new Vector2(0, 0); rt.offsetMax = new Vector2(0, 0);
        rt.anchoredPosition = new Vector2(0, -20);
        rt.sizeDelta = new Vector2(0, 40);
    }

    void MakeBtn(GameObject parent, string label, float yOff,
                 UnityEngine.Events.UnityAction cb, Color color)
    {
        var go  = new GameObject("Btn_" + label);
        go.transform.SetParent(parent.transform, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(cb);
        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(260, 48);
        rt.anchoredPosition = new Vector2(0, yOff);

        var tGO = new GameObject("Label"); tGO.transform.SetParent(go.transform, false);
        var txt = tGO.AddComponent<TextMeshProUGUI>();
        txt.text = label; txt.fontSize = 19;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color = Color.white;
        var trt = txt.rectTransform;
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;
    }

    Slider MakeLabeledSlider(GameObject parent, string label, float min, float max, float val, float yOff)
    {
        // Etiket
        var lGO = new GameObject("Lbl_" + label); lGO.transform.SetParent(parent.transform, false);
        var ltxt = lGO.AddComponent<TextMeshProUGUI>();
        ltxt.text = label; ltxt.fontSize = 16; ltxt.color = Color.white;
        ltxt.alignment = TextAlignmentOptions.Left;
        var lrt = ltxt.rectTransform;
        lrt.anchorMin = lrt.anchorMax = new Vector2(0.5f, 0.5f);
        lrt.sizeDelta = new Vector2(260, 24);
        lrt.anchoredPosition = new Vector2(0, yOff + 18);

        // Slider
        return MakeSlider(parent, "Sl_" + label, min, max, val, yOff);
    }

    Slider MakeSlider(GameObject parent, string name, float min, float max, float val, float yOff)
    {
        var go = new GameObject(name); go.transform.SetParent(parent.transform, false);
        var sl = go.AddComponent<Slider>();
        sl.minValue = min; sl.maxValue = max; sl.value = val;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(260, 18);
        rt.anchoredPosition = new Vector2(0, yOff);

        var bgGO = new GameObject("Bg"); bgGO.transform.SetParent(go.transform, false);
        var bgImg = bgGO.AddComponent<Image>(); bgImg.color = new Color(0.25f, 0.25f, 0.25f);
        var bgRt = bgImg.rectTransform;
        bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one; bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;

        var fa = new GameObject("FillArea"); fa.transform.SetParent(go.transform, false);
        var faRt = fa.AddComponent<RectTransform>();
        faRt.anchorMin = new Vector2(0, 0.25f); faRt.anchorMax = new Vector2(1, 0.75f);
        faRt.offsetMin = new Vector2(5, 0); faRt.offsetMax = new Vector2(-5, 0);

        var fill = new GameObject("Fill"); fill.transform.SetParent(fa.transform, false);
        var fillImg = fill.AddComponent<Image>(); fillImg.color = _btnColor;
        var fillRt = fillImg.rectTransform;
        fillRt.anchorMin = Vector2.zero; fillRt.anchorMax = new Vector2(1, 1); fillRt.offsetMin = fillRt.offsetMax = Vector2.zero;

        var ha = new GameObject("HandleArea"); ha.transform.SetParent(go.transform, false);
        var haRt = ha.AddComponent<RectTransform>();
        haRt.anchorMin = Vector2.zero; haRt.anchorMax = Vector2.one;
        haRt.offsetMin = new Vector2(10, 0); haRt.offsetMax = new Vector2(-10, 0);

        var handle = new GameObject("Handle"); handle.transform.SetParent(ha.transform, false);
        var handleImg = handle.AddComponent<Image>(); handleImg.color = Color.white;
        var handleRt = handleImg.rectTransform; handleRt.sizeDelta = new Vector2(18, 18);

        sl.fillRect = fillRt; sl.handleRect = handleRt; sl.targetGraphic = handleImg;
        return sl;
    }

    GameObject MakeRect(GameObject parent, string name)
    {
        var go = new GameObject(name); go.transform.SetParent(parent.transform, false);
        go.AddComponent<RectTransform>(); return go;
    }

    void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
