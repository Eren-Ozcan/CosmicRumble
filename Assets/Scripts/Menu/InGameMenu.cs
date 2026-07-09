using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CosmicRumble.Localization;

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

    GameObject _audioTab, _graphicsTab, _controlsTab;

    Slider  _masterSlider, _musicSlider, _sfxSlider;
    Toggle  _fsToggle;
    Toggle  _vsyncToggle;
    CyclerControl _resolutionCycler, _qualityCycler;

    TextMeshProUGUI _btnMoveLeftLabel, _btnMoveRightLabel, _btnJumpLabel;
    string _awaitingRebindFor; // null | "MoveLeft" | "MoveRight" | "Jump"

    bool _isOpen;

    /// <summary>Simple prev/next value cycler used for Resolution/Quality settings rows.</summary>
    class CyclerControl
    {
        public int Index;
        public string[] Options;
        public TextMeshProUGUI Label;
        public System.Action<int> OnChanged;

        public void Set(int idx)
        {
            if (Options.Length == 0) return;
            Index = Mathf.Clamp(idx, 0, Options.Length - 1);
            Label.text = Options[Index];
            OnChanged?.Invoke(Index);
        }

        public void Step(int dir)
        {
            if (Options.Length == 0) return;
            Set(((Index + dir) % Options.Length + Options.Length) % Options.Length);
        }
    }

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
        if (_awaitingRebindFor != null)
        {
            if (TryGetAnyKeyDown(out var pressed))
            {
                if (pressed != KeyCode.Escape)
                {
                    var cfg = GameConfig.Instance;
                    if (cfg != null)
                    {
                        switch (_awaitingRebindFor)
                        {
                            case "MoveLeft":  cfg.MoveLeftKey  = pressed; break;
                            case "MoveRight": cfg.MoveRightKey = pressed; break;
                            case "Jump":      cfg.JumpKey      = pressed; break;
                        }
                        cfg.Save();
                    }
                }
                _awaitingRebindFor = null;
                RefreshControlsLabels();
            }
            return;
        }

        // Don't intercept ESC while free camera is active
        if (FreeCameraController.IsActive) return;

        if (Input.GetKeyDown(toggleKey))
            SetOpen(!_isOpen);
    }

    static readonly KeyCode[] AllKeyCodes = (KeyCode[])System.Enum.GetValues(typeof(KeyCode));

    static bool TryGetAnyKeyDown(out KeyCode pressed)
    {
        foreach (var kc in AllKeyCodes)
        {
            if (kc == KeyCode.None) continue;
            if (Input.GetKeyDown(kc)) { pressed = kc; return true; }
        }
        pressed = KeyCode.None;
        return false;
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
        ShowSettingsTab(_audioTab);
    }

    void ShowSettingsTab(GameObject tab)
    {
        _audioTab.SetActive(tab == _audioTab);
        _graphicsTab.SetActive(tab == _graphicsTab);
        _controlsTab.SetActive(tab == _controlsTab);
        if (tab == _controlsTab) RefreshControlsLabels();
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

        MakeTitle(_mainPanel, Loc.T("GAME MENU"));
        MakeBtn(_mainPanel, Loc.T("Resume"),             0,    OnResume,     _btnResume);
        MakeBtn(_mainPanel, Loc.T("Settings"),         -70,  ShowSettings, _btnColor);
        MakeBtn(_mainPanel, Loc.T("Return to Main Menu"), -140, OnQuitToMenu, _btnDanger);

        // ── Settings Panel ───────────────────────────────────────
        _settingsPanel = MakeCenteredPanel("SettingsPanel", 420, 560);
        _settingsPanel.transform.SetParent(_root.transform, false);

        MakeTitle(_settingsPanel, Loc.T("SETTINGS"));

        MakeTabBtn(_settingsPanel, Loc.T("Audio"),     -110, 200, () => ShowSettingsTab(_audioTab));
        MakeTabBtn(_settingsPanel, Loc.T("Graphics"),     0, 200, () => ShowSettingsTab(_graphicsTab));
        MakeTabBtn(_settingsPanel, Loc.T("Controls"),   110, 200, () => ShowSettingsTab(_controlsTab));

        _audioTab    = MakeRect(_settingsPanel, "AudioTab");
        _graphicsTab = MakeRect(_settingsPanel, "GraphicsTab");
        _controlsTab = MakeRect(_settingsPanel, "ControlsTab");
        StretchFull(_audioTab.GetComponent<RectTransform>());
        StretchFull(_graphicsTab.GetComponent<RectTransform>());
        StretchFull(_controlsTab.GetComponent<RectTransform>());

        BuildAudioTab(_audioTab);
        BuildGraphicsTab(_graphicsTab);
        BuildControlsTab(_controlsTab);

        MakeBtn(_settingsPanel, Loc.T("← Back"), -240, () => {
            GameConfig.Instance?.Save();
            ShowMain();
        }, _btnColor);

        ShowSettingsTab(_audioTab);
    }

    void BuildAudioTab(GameObject parent)
    {
        _masterSlider = MakeLabeledSlider(parent, Loc.T("Master Volume"), 0f, 1f, 0.8f, 110);
        _musicSlider  = MakeLabeledSlider(parent, Loc.T("Music"),         0f, 1f, 0.7f,  40);
        _sfxSlider    = MakeLabeledSlider(parent, Loc.T("SFX"),           0f, 1f, 1f,   -30);

        _masterSlider.onValueChanged.AddListener(v => {
            if (GameConfig.Instance != null) GameConfig.Instance.MasterVolume = v; });
        _musicSlider.onValueChanged.AddListener(v => {
            if (GameConfig.Instance != null) GameConfig.Instance.MusicVolume  = v; });
        _sfxSlider.onValueChanged.AddListener(v => {
            if (GameConfig.Instance != null) GameConfig.Instance.SfxVolume    = v; });
    }

    void BuildGraphicsTab(GameObject parent)
    {
        MakeTxt(parent, Loc.T("Resolution"), 110, 15, TextAlignmentOptions.Center, Color.white);

        var resolutions = Screen.resolutions;
        var resOptions  = new string[resolutions.Length];
        for (int i = 0; i < resolutions.Length; i++)
            resOptions[i] = $"{resolutions[i].width} x {resolutions[i].height}";
        if (resOptions.Length == 0) resOptions = new[] { $"{Screen.width} x {Screen.height}" };

        var cfg = GameConfig.Instance;
        int startRes = cfg != null
            ? Mathf.Clamp(cfg.ResolutionIndex, 0, resOptions.Length - 1)
            : Mathf.Max(0, resOptions.Length - 1);
        _resolutionCycler = MakeCycler(parent, "res", 80, resOptions, startRes,
            idx => { if (GameConfig.Instance != null) GameConfig.Instance.ResolutionIndex = idx; });

        MakeTxt(parent, Loc.T("Quality"), 20, 15, TextAlignmentOptions.Center, Color.white);

        var qualityOptions = QualitySettings.names;
        int startQuality = cfg != null
            ? Mathf.Clamp(cfg.QualityLevel, 0, Mathf.Max(0, qualityOptions.Length - 1))
            : QualitySettings.GetQualityLevel();
        _qualityCycler = MakeCycler(parent, "quality", -10, qualityOptions, startQuality,
            idx => { if (GameConfig.Instance != null) GameConfig.Instance.QualityLevel = idx; });

        MakeTxt(parent, "VSync", -70, 15, TextAlignmentOptions.Center, Color.white);
        _vsyncToggle = MakeToggle(parent, -95);
        _vsyncToggle.isOn = cfg == null || cfg.VSync;
        _vsyncToggle.onValueChanged.AddListener(v => { if (GameConfig.Instance != null) GameConfig.Instance.VSync = v; });

        MakeBtn(parent, Loc.T("Apply"), -150, () => {
            GameConfig.Instance?.ApplyGraphics();
            GameConfig.Instance?.Save();
        }, _btnResume);
    }

    void BuildControlsTab(GameObject parent)
    {
        MakeTxt(parent, Loc.T("Move Left"), 110, 15, TextAlignmentOptions.Center, Color.white);
        _btnMoveLeftLabel = MakeRebindBtn(parent, 80, () => BeginRebind("MoveLeft"));

        MakeTxt(parent, Loc.T("Move Right"), 20, 15, TextAlignmentOptions.Center, Color.white);
        _btnMoveRightLabel = MakeRebindBtn(parent, -10, () => BeginRebind("MoveRight"));

        MakeTxt(parent, Loc.T("Jump"), -70, 15, TextAlignmentOptions.Center, Color.white);
        _btnJumpLabel = MakeRebindBtn(parent, -100, () => BeginRebind("Jump"));

        MakeTxt(parent, Loc.T("Click a button, then press the new key (Esc to cancel)"), -160, 12,
            TextAlignmentOptions.Center, new Color(0.75f, 0.78f, 0.88f));

        RefreshControlsLabels();
    }

    // ── Controls tab: rebinding ─────────────────────────────────────

    void BeginRebind(string action)
    {
        _awaitingRebindFor = action;
        RefreshControlsLabels();
    }

    void RefreshControlsLabels()
    {
        var cfg = GameConfig.Instance;
        KeyCode left  = cfg != null ? cfg.MoveLeftKey  : KeyCode.A;
        KeyCode right = cfg != null ? cfg.MoveRightKey : KeyCode.D;
        KeyCode jump  = cfg != null ? cfg.JumpKey      : KeyCode.Space;

        string waiting = Loc.T("Press a key…");
        if (_btnMoveLeftLabel)  _btnMoveLeftLabel.text  = _awaitingRebindFor == "MoveLeft"  ? waiting : left.ToString();
        if (_btnMoveRightLabel) _btnMoveRightLabel.text = _awaitingRebindFor == "MoveRight" ? waiting : right.ToString();
        if (_btnJumpLabel)      _btnJumpLabel.text       = _awaitingRebindFor == "Jump"      ? waiting : jump.ToString();
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
        UiKit.Hover(go);
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

    // ── Settings-tab helpers ─────────────────────────────────────

    void MakeTabBtn(GameObject parent, string label, float xOff, float yOff, UnityEngine.Events.UnityAction cb)
    {
        var go  = new GameObject("Tab_" + label);
        go.transform.SetParent(parent.transform, false);
        var img = go.AddComponent<Image>();
        img.color = _btnColor;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(cb);
        UiKit.Hover(go);
        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(120, 34);
        rt.anchoredPosition = new Vector2(xOff, yOff);

        var tGO = new GameObject("Label"); tGO.transform.SetParent(go.transform, false);
        var txt = tGO.AddComponent<TextMeshProUGUI>();
        txt.text = label; txt.fontSize = 14; txt.fontStyle = FontStyles.Bold;
        txt.alignment = TextAlignmentOptions.Center; txt.color = Color.white;
        var trt = txt.rectTransform;
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;
    }

    GameObject MakeTxt(GameObject parent, string text, float yOff, int fontSize,
                        TextAlignmentOptions align, Color color)
    {
        var go = new GameObject("Txt_" + text);
        go.transform.SetParent(parent.transform, false);
        var txt = go.AddComponent<TextMeshProUGUI>();
        txt.text = text; txt.fontSize = fontSize; txt.alignment = align; txt.color = color;
        var rt = txt.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(360, 24);
        rt.anchoredPosition = new Vector2(0, yOff);
        return go;
    }

    Toggle MakeToggle(GameObject parent, float yOff)
    {
        var go = new GameObject("Toggle");
        go.transform.SetParent(parent.transform, false);
        var toggle = go.AddComponent<Toggle>();
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(46, 24);
        rt.anchoredPosition = new Vector2(0, yOff);

        var bgGO = new GameObject("Bg"); bgGO.transform.SetParent(go.transform, false);
        var bgImg = bgGO.AddComponent<Image>(); bgImg.color = new Color(0.22f, 0.22f, 0.30f);
        var bgRt = bgImg.rectTransform;
        bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one; bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;

        var checkGO = new GameObject("Checkmark"); checkGO.transform.SetParent(bgGO.transform, false);
        var checkImg = checkGO.AddComponent<Image>(); checkImg.color = _btnResume;
        var checkRt = checkImg.rectTransform;
        checkRt.anchorMin = new Vector2(0.08f, 0.08f);
        checkRt.anchorMax = new Vector2(0.92f, 0.92f);
        checkRt.offsetMin = checkRt.offsetMax = Vector2.zero;

        toggle.targetGraphic = bgImg;
        toggle.graphic       = checkImg;
        return toggle;
    }

    CyclerControl MakeCycler(GameObject parent, string name, float yOff, string[] options,
                              int startIndex, System.Action<int> onChanged)
    {
        var labelGO = new GameObject(name + "_Value");
        labelGO.transform.SetParent(parent.transform, false);
        var label = labelGO.AddComponent<TextMeshProUGUI>();
        label.fontSize = 15; label.alignment = TextAlignmentOptions.Center; label.color = Color.white;
        var lrt = label.rectTransform;
        lrt.anchorMin = lrt.anchorMax = new Vector2(0.5f, 0.5f);
        lrt.sizeDelta = new Vector2(200, 24);
        lrt.anchoredPosition = new Vector2(0, yOff);

        var cycler = new CyclerControl { Options = options, Label = label, OnChanged = onChanged };

        MakeArrowBtn(parent, name + "_Prev", "◀", new Vector2(-120, yOff), () => cycler.Step(-1));
        MakeArrowBtn(parent, name + "_Next", "▶", new Vector2(120, yOff),  () => cycler.Step(1));

        cycler.Set(startIndex);
        return cycler;
    }

    void MakeArrowBtn(GameObject parent, string name, string glyph, Vector2 pos, UnityEngine.Events.UnityAction cb)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.18f, 0.28f, 0.42f);
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(cb);
        UiKit.Hover(go);
        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(26, 24);
        rt.anchoredPosition = pos;

        var tGO = new GameObject("Glyph"); tGO.transform.SetParent(go.transform, false);
        var txt = tGO.AddComponent<TextMeshProUGUI>();
        txt.text = glyph; txt.fontSize = 14; txt.alignment = TextAlignmentOptions.Center; txt.color = Color.white;
        var trt = txt.rectTransform;
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;
    }

    TextMeshProUGUI MakeRebindBtn(GameObject parent, float yOff, UnityEngine.Events.UnityAction cb)
    {
        var go = new GameObject("RebindBtn");
        go.transform.SetParent(parent.transform, false);
        var img = go.AddComponent<Image>();
        img.color = _btnColor;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(cb);
        UiKit.Hover(go);
        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(150, 30);
        rt.anchoredPosition = new Vector2(0, yOff);

        var tGO = new GameObject("Label"); tGO.transform.SetParent(go.transform, false);
        var txt = tGO.AddComponent<TextMeshProUGUI>();
        txt.fontSize = 14; txt.fontStyle = FontStyles.Bold;
        txt.alignment = TextAlignmentOptions.Center; txt.color = Color.white;
        var trt = txt.rectTransform;
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;

        return txt;
    }
}
