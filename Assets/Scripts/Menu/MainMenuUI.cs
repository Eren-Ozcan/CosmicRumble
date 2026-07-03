using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CosmicRumble.Economy;
using CosmicRumble.Economy.IAP;
using CosmicRumble.Achievements;
using CosmicRumble.Cloud;

/// <summary>
/// MenuScene'e boş bir GameObject ekle, bu scripti yapıştır — bitti.
/// Tüm UI runtime'da programatik oluşturulur.
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    // ── Palette ──────────────────────────────────────────────────────────────
    static readonly Color BgDeep       = new Color(0.04f, 0.03f, 0.10f, 1.00f);
    static readonly Color BgCard       = new Color(0.08f, 0.07f, 0.18f, 0.92f);
    static readonly Color BgCardDark   = new Color(0.05f, 0.05f, 0.13f, 0.95f);
    static readonly Color AccBlue      = new Color(0.22f, 0.45f, 0.95f, 1.00f);
    static readonly Color AccBlueHov   = new Color(0.32f, 0.58f, 1.00f, 1.00f);
    static readonly Color AccPurple    = new Color(0.48f, 0.20f, 0.85f, 1.00f);
    static readonly Color AccPurpleHov = new Color(0.60f, 0.32f, 1.00f, 1.00f);
    static readonly Color AccGold      = new Color(1.00f, 0.80f, 0.20f, 1.00f);
    static readonly Color AccGoldHov   = new Color(1.00f, 0.88f, 0.40f, 1.00f);
    static readonly Color AccGreen     = new Color(0.12f, 0.68f, 0.22f, 1.00f);
    static readonly Color AccGreenHov  = new Color(0.18f, 0.82f, 0.30f, 1.00f);
    static readonly Color AccRed       = new Color(0.60f, 0.12f, 0.12f, 1.00f);
    static readonly Color AccRedHov    = new Color(0.80f, 0.18f, 0.18f, 1.00f);
    static readonly Color AccPress     = new Color(0.10f, 0.18f, 0.45f, 1.00f);
    static readonly Color TextPrimary  = Color.white;
    static readonly Color TextDim      = new Color(0.65f, 0.70f, 0.82f, 1.00f);
    static readonly Color BarBg        = new Color(0.12f, 0.12f, 0.22f, 1.00f);
    static readonly Color Separator    = new Color(0.25f, 0.25f, 0.40f, 0.60f);

    const string VERSION = "v0.8.2";
    const int    BTN_W   = 290;
    const int    BTN_H   = 56;
    const int    BTN_GAP = 68;

    // ── Panel refs ────────────────────────────────────────────────────────────
    GameObject _mainPanel;
    GameObject _settingsPanel;

    GameObject _audioTab, _graphicsTab, _controlsTab, _accountTab;

    Slider _masterSlider, _musicSlider, _sfxSlider;
    Toggle _fsToggle;

    Toggle _vsyncToggle;
    CyclerControl _resolutionCycler, _qualityCycler;

    TextMeshProUGUI _btnMoveLeftLabel, _btnMoveRightLabel, _btnJumpLabel;
    string _awaitingRebindFor; // null | "MoveLeft" | "MoveRight" | "Jump"

    TextMeshProUGUI _accountStatusText, _accountActionLabel;
    Button _accountActionBtn;

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

    // ─────────────────────────────────────────────────────────────────────────

    void Awake()
    {
        StartCoroutine(BootstrapSequence());
    }

    // ════════════════════════════════════════════════════════════════════════
    //  SINGLETONS
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Sıra önemli: CloudSaveManager ve buluttan çekme işlemi, progress manager'ları
    /// (CurrencyManager vb.) oluşturulmadan ÖNCE tamamlanmalı — aksi halde o manager'ların
    /// kendi Awake/Load'u henüz senkronlanmamış (eski) yerel dosyayı okur.
    /// </summary>
    IEnumerator BootstrapSequence()
    {
        EnsureCoreSingletons();

        if (CloudSaveManager.Instance != null)
            yield return CloudSaveManager.Instance.InitializeAndPull();

        EnsureProgressSingletons();
        BuildUI();
        ShowPanel(_mainPanel);
    }

    void EnsureCoreSingletons()
    {
        if (GameConfig.Instance      == null) new GameObject("GameConfig").AddComponent<GameConfig>();
        if (SceneFader.Instance      == null) new GameObject("SceneFader").AddComponent<SceneFader>();
        if (AuthManager.Instance     == null) new GameObject("AuthManager").AddComponent<AuthManager>();
        if (AudioManager.Instance    == null) new GameObject("AudioManager").AddComponent<AudioManager>();
        if (CloudSaveManager.Instance == null) new GameObject("CloudSaveManager").AddComponent<CloudSaveManager>();
    }

    void EnsureProgressSingletons()
    {
        // Economy/achievement backend — CostumeManager kasıtlı olarak dahil edilmedi (bkz. TODO.md).
        if (CurrencyManager.Instance    == null) new GameObject("CurrencyManager").AddComponent<CurrencyManager>();
        if (PlayerLevelManager.Instance == null) new GameObject("PlayerLevelManager").AddComponent<PlayerLevelManager>();
        if (UnlockManager.Instance      == null) new GameObject("UnlockManager").AddComponent<UnlockManager>();
        if (QuestManager.Instance       == null) new GameObject("QuestManager").AddComponent<QuestManager>();
        if (ChestManager.Instance       == null) new GameObject("ChestManager").AddComponent<ChestManager>();
        if (LoginStreakManager.Instance == null) new GameObject("LoginStreakManager").AddComponent<LoginStreakManager>();
        if (AchievementManager.Instance == null)
        {
            new GameObject("AchievementManager").AddComponent<AchievementManager>();
            // Awake() alone loads the guest file by default -- after a Login-triggered scene
            // reload this recreates AchievementManager fresh, so it must be told the *current*
            // identity explicitly here, same as AuthManager.Login()/Register() already do on the
            // instance that's about to be destroyed.
            var auth = AuthManager.Instance;
            string username = (auth != null && auth.IsLoggedIn && !auth.IsGuest) ? auth.CurrentUsername : null;
            AchievementManager.Instance.LoadForUser(username);
        }
        if (AchievementTracker.Instance == null) new GameObject("AchievementTracker").AddComponent<AchievementTracker>();
        if (IAPManager.Instance          == null) new GameObject("IAPManager").AddComponent<IAPManager>();
    }

    // ════════════════════════════════════════════════════════════════════════
    //  BUILD UI
    // ════════════════════════════════════════════════════════════════════════

    void BuildUI()
    {
        // ── Canvas ───────────────────────────────────────────────────────────
        var canvasGO = new GameObject("MenuCanvas");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();
        EnsureEventSystem();

        // ── Background ───────────────────────────────────────────────────────
        MakeStretch(canvasGO, "Background", BgDeep);
        BuildStarfield(canvasGO, 90);
        BuildNebulaGlow(canvasGO);

        // ── Title ────────────────────────────────────────────────────────────
        BuildTitle(canvasGO);

        // ── Panels ───────────────────────────────────────────────────────────
        _mainPanel     = MakePanel(canvasGO, "MainPanel");
        _settingsPanel = MakePanel(canvasGO, "SettingsPanel");

        BuildMainPanel();
        BuildSettingsPanel();

        // ── Footer ───────────────────────────────────────────────────────────
        BuildFooter(canvasGO);
    }

    // ────────────────────────────────────────────────────────────────────────
    //  TITLE BLOCK
    // ────────────────────────────────────────────────────────────────────────

    void BuildTitle(GameObject parent)
    {
        // Glow backdrop behind title
        var glowGO  = new GameObject("TitleGlow");
        glowGO.transform.SetParent(parent.transform, false);
        var glowImg = glowGO.AddComponent<Image>();
        glowImg.color = new Color(0.18f, 0.25f, 0.60f, 0.18f);
        var glowRt  = glowImg.rectTransform;
        glowRt.anchorMin        = new Vector2(0.5f, 1f);
        glowRt.anchorMax        = new Vector2(0.5f, 1f);
        glowRt.pivot            = new Vector2(0.5f, 1f);
        glowRt.sizeDelta        = new Vector2(780, 145);
        glowRt.anchoredPosition = new Vector2(0, -22);

        // Main title
        var titleGO  = new GameObject("Title");
        titleGO.transform.SetParent(parent.transform, false);
        var title    = titleGO.AddComponent<TextMeshProUGUI>();
        title.text      = "COSMIC RUMBLE";
        title.fontSize  = 76;
        title.fontStyle = FontStyles.Bold;
        title.alignment = TextAlignmentOptions.Center;
        title.color     = AccGold;
        var titleRt = title.rectTransform;
        titleRt.anchorMin        = new Vector2(0.5f, 1f);
        titleRt.anchorMax        = new Vector2(0.5f, 1f);
        titleRt.pivot            = new Vector2(0.5f, 1f);
        titleRt.sizeDelta        = new Vector2(740, 95);
        titleRt.anchoredPosition = new Vector2(0, -30);

        // Separator line
        var lineGO  = new GameObject("TitleLine");
        lineGO.transform.SetParent(parent.transform, false);
        var lineImg = lineGO.AddComponent<Image>();
        lineImg.color = AccGold;
        var lineRt  = lineImg.rectTransform;
        lineRt.anchorMin        = new Vector2(0.5f, 1f);
        lineRt.anchorMax        = new Vector2(0.5f, 1f);
        lineRt.pivot            = new Vector2(0.5f, 1f);
        lineRt.sizeDelta        = new Vector2(480, 2);
        lineRt.anchoredPosition = new Vector2(0, -126);

        // Subtitle
        var subGO  = new GameObject("Subtitle");
        subGO.transform.SetParent(parent.transform, false);
        var sub    = subGO.AddComponent<TextMeshProUGUI>();
        sub.text      = "Turn-based planetary warfare";
        sub.fontSize  = 22;
        sub.fontStyle = FontStyles.Italic;
        sub.alignment = TextAlignmentOptions.Center;
        sub.color     = TextDim;
        var subRt = sub.rectTransform;
        subRt.anchorMin        = new Vector2(0.5f, 1f);
        subRt.anchorMax        = new Vector2(0.5f, 1f);
        subRt.pivot            = new Vector2(0.5f, 1f);
        subRt.sizeDelta        = new Vector2(600, 34);
        subRt.anchoredPosition = new Vector2(0, -136);
    }

    // ────────────────────────────────────────────────────────────────────────
    //  MAIN PANEL
    // ────────────────────────────────────────────────────────────────────────

    void BuildMainPanel()
    {
        // Card backdrop behind buttons
        var backdropGO  = new GameObject("ButtonCard");
        backdropGO.transform.SetParent(_mainPanel.transform, false);
        var backdropImg = backdropGO.AddComponent<Image>();
        backdropImg.color = BgCardDark;
        var backdropRt  = backdropImg.rectTransform;
        backdropRt.anchorMin        = new Vector2(0.5f, 0.5f);
        backdropRt.anchorMax        = new Vector2(0.5f, 0.5f);
        backdropRt.sizeDelta        = new Vector2(340, 476);
        backdropRt.anchoredPosition = new Vector2(0, -74);

        // Side accent bar
        var sideGO  = new GameObject("SideAccent");
        sideGO.transform.SetParent(backdropGO.transform, false);
        var sideImg = sideGO.AddComponent<Image>();
        sideImg.color = AccBlue;
        var sideRt  = sideImg.rectTransform;
        sideRt.anchorMin        = new Vector2(0f, 0f);
        sideRt.anchorMax        = new Vector2(0f, 1f);
        sideRt.pivot            = new Vector2(0f, 0.5f);
        sideRt.sizeDelta        = new Vector2(4, 0);
        sideRt.anchoredPosition = Vector2.zero;

        // Buttons — centered vertically in card
        float topY  = 110f;
        MakeBtn(_mainPanel, "btn_play",          "▶  PLAY",           topY - BTN_GAP * 0, AccBlue,   AccBlueHov,   () => { Click(); LobbyPanelUI.Instance?.Show(); });
        MakeBtn(_mainPanel, "btn_achievements",  "★  ACHIEVEMENTS",   topY - BTN_GAP * 1, AccPurple, AccPurpleHov, () => { Click(); AchievementsPanelUI.Instance?.Show(); });
        MakeBtn(_mainPanel, "btn_quests",        "◆  QUESTS",         topY - BTN_GAP * 2, AccGreen,  AccGreenHov,  () => { Click(); QuestsPanelUI.Instance?.Show(); });
        MakeBtn(_mainPanel, "btn_shop",          "$  SHOP",           topY - BTN_GAP * 3, AccGold,   AccGoldHov,   () => { Click(); ShopPanelUI.Instance?.Show(); });
        MakeBtn(_mainPanel, "btn_settings",      "⚙  SETTINGS",       topY - BTN_GAP * 4, AccBlue,   AccBlueHov,   () => { Click(); ShowPanel(_settingsPanel); });
        MakeBtn(_mainPanel, "btn_quit",          "✕  QUIT",           topY - BTN_GAP * 5, AccRed,    AccRedHov,    () => { Click(); OnQuit(); });
    }

    // ────────────────────────────────────────────────────────────────────────
    //  SETTINGS PANEL
    // ────────────────────────────────────────────────────────────────────────

    void BuildSettingsPanel()
    {
        // Card backdrop
        var backdropGO  = new GameObject("SettingsCard");
        backdropGO.transform.SetParent(_settingsPanel.transform, false);
        var backdropImg = backdropGO.AddComponent<Image>();
        backdropImg.color = BgCardDark;
        var backdropRt  = backdropImg.rectTransform;
        backdropRt.anchorMin        = new Vector2(0.5f, 0.5f);
        backdropRt.anchorMax        = new Vector2(0.5f, 0.5f);
        backdropRt.sizeDelta        = new Vector2(500, 520);
        backdropRt.anchoredPosition = new Vector2(0, -10);

        // Top accent
        var topGO  = new GameObject("TopAccent");
        topGO.transform.SetParent(backdropGO.transform, false);
        var topImg = topGO.AddComponent<Image>();
        topImg.color = AccBlue;
        var topRt  = topImg.rectTransform;
        topRt.anchorMin        = new Vector2(0f, 1f);
        topRt.anchorMax        = new Vector2(1f, 1f);
        topRt.pivot            = new Vector2(0.5f, 1f);
        topRt.sizeDelta        = new Vector2(0, 4);
        topRt.anchoredPosition = Vector2.zero;

        // Header
        MakeTxt(_settingsPanel, "hdr_settings", "⚙  SETTINGS", 26, FontStyles.Bold, AccGold,
            TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(420, 36), new Vector2(0, 220));

        MakeSeparator(_settingsPanel, new Vector2(0, 195));

        // ── Tab row ──────────────────────────────────────────────────────────
        MakeTabBtn("tab_audio",     "AUDIO",     -168, 170, () => ShowSettingsTab(_audioTab));
        MakeTabBtn("tab_graphics",  "GRAPHICS",   -56, 170, () => ShowSettingsTab(_graphicsTab));
        MakeTabBtn("tab_controls",  "CONTROLS",    56, 170, () => ShowSettingsTab(_controlsTab));
        MakeTabBtn("tab_account",   "ACCOUNT",    168, 170, () => ShowSettingsTab(_accountTab));

        MakeSeparator(_settingsPanel, new Vector2(0, 145));

        // ── Tab content containers ──────────────────────────────────────────
        _audioTab    = MakePanel(_settingsPanel, "AudioTab");
        _graphicsTab = MakePanel(_settingsPanel, "GraphicsTab");
        _controlsTab = MakePanel(_settingsPanel, "ControlsTab");
        _accountTab  = MakePanel(_settingsPanel, "AccountTab");

        BuildAudioTab(_audioTab);
        BuildGraphicsTab(_graphicsTab);
        BuildControlsTab(_controlsTab);
        BuildAccountTab(_accountTab);

        // Back button — always visible, outside the tabs
        MakeBtn(_settingsPanel, "btn_back", "← BACK", -230,
            new Color(0.18f, 0.28f, 0.42f), new Color(0.26f, 0.40f, 0.58f),
            () => { Click(); ShowPanel(_mainPanel); });

        ShowSettingsTab(_audioTab);
        LoadSettingsValues();
    }

    // ────────────────────────────────────────────────────────────────────────
    //  SETTINGS TABS
    // ────────────────────────────────────────────────────────────────────────

    void ShowSettingsTab(GameObject tab)
    {
        _audioTab.SetActive(tab == _audioTab);
        _graphicsTab.SetActive(tab == _graphicsTab);
        _controlsTab.SetActive(tab == _controlsTab);
        _accountTab.SetActive(tab == _accountTab);

        if (tab == _accountTab)  RefreshAccountTab();
        if (tab == _controlsTab) RefreshControlsLabels();
    }

    void BuildAudioTab(GameObject parent)
    {
        _masterSlider = MakeSliderRow(parent, "Master Volume", 100);
        _musicSlider  = MakeSliderRow(parent, "Music",          40);
        _sfxSlider    = MakeSliderRow(parent, "SFX",           -20);

        MakeSeparator(parent, new Vector2(0, -60));

        MakeTxt(parent, "lbl_fs", "Fullscreen", 18, FontStyles.Normal, TextPrimary,
            TextAlignmentOptions.Right, new Vector2(0.5f, 0.5f), new Vector2(200, 28), new Vector2(-70, -95));
        _fsToggle = MakeToggle(parent, -95);

        _masterSlider.onValueChanged.AddListener(v => { GameConfig.Instance.MasterVolume = v; AudioManager.Instance?.ApplyVolumes(); });
        _musicSlider.onValueChanged.AddListener(v  => { GameConfig.Instance.MusicVolume  = v; AudioManager.Instance?.ApplyVolumes(); });
        _sfxSlider.onValueChanged.AddListener(v    => { GameConfig.Instance.SfxVolume    = v; AudioManager.Instance?.ApplyVolumes(); });
        _fsToggle.onValueChanged.AddListener(v     => { GameConfig.Instance.Fullscreen = v; Screen.fullScreen = v; GameConfig.Instance.Save(); });
    }

    void BuildGraphicsTab(GameObject parent)
    {
        MakeTxt(parent, "lbl_res", "Resolution", 17, FontStyles.Normal, TextPrimary,
            TextAlignmentOptions.Left, new Vector2(0.5f, 0.5f), new Vector2(160, 26), new Vector2(-190, 100));

        var resolutions = Screen.resolutions;
        var resOptions  = new string[resolutions.Length];
        for (int i = 0; i < resolutions.Length; i++)
            resOptions[i] = $"{resolutions[i].width} x {resolutions[i].height}";
        if (resOptions.Length == 0) resOptions = new[] { $"{Screen.width} x {Screen.height}" };

        var cfg = GameConfig.Instance;
        int startRes = cfg != null
            ? Mathf.Clamp(cfg.ResolutionIndex, 0, resOptions.Length - 1)
            : Mathf.Max(0, resOptions.Length - 1);
        _resolutionCycler = MakeCycler(parent, "res", 100, resOptions, startRes,
            idx => { if (GameConfig.Instance != null) GameConfig.Instance.ResolutionIndex = idx; });

        MakeTxt(parent, "lbl_quality", "Quality", 17, FontStyles.Normal, TextPrimary,
            TextAlignmentOptions.Left, new Vector2(0.5f, 0.5f), new Vector2(160, 26), new Vector2(-190, 40));

        var qualityOptions = QualitySettings.names;
        int startQuality = cfg != null
            ? Mathf.Clamp(cfg.QualityLevel, 0, Mathf.Max(0, qualityOptions.Length - 1))
            : QualitySettings.GetQualityLevel();
        _qualityCycler = MakeCycler(parent, "quality", 40, qualityOptions, startQuality,
            idx => { if (GameConfig.Instance != null) GameConfig.Instance.QualityLevel = idx; });

        MakeTxt(parent, "lbl_vsync", "VSync", 18, FontStyles.Normal, TextPrimary,
            TextAlignmentOptions.Right, new Vector2(0.5f, 0.5f), new Vector2(200, 28), new Vector2(-70, -20));
        _vsyncToggle = MakeToggle(parent, -20);
        _vsyncToggle.isOn = cfg == null || cfg.VSync;
        _vsyncToggle.onValueChanged.AddListener(v => { if (GameConfig.Instance != null) GameConfig.Instance.VSync = v; });

        MakeBtn(parent, "btn_apply_graphics", "APPLY", -90,
            AccGreen, AccGreenHov, () =>
            {
                Click();
                GameConfig.Instance?.ApplyGraphics();
                GameConfig.Instance?.Save();
            });
    }

    void BuildControlsTab(GameObject parent)
    {
        MakeTxt(parent, "lbl_move_left", "Move Left", 17, FontStyles.Normal, TextPrimary,
            TextAlignmentOptions.Right, new Vector2(0.5f, 0.5f), new Vector2(180, 26), new Vector2(-115, 100));
        _btnMoveLeftLabel = MakeRebindBtn(parent, "btn_move_left", new Vector2(105, 100), () => BeginRebind("MoveLeft"));

        MakeTxt(parent, "lbl_move_right", "Move Right", 17, FontStyles.Normal, TextPrimary,
            TextAlignmentOptions.Right, new Vector2(0.5f, 0.5f), new Vector2(180, 26), new Vector2(-115, 30));
        _btnMoveRightLabel = MakeRebindBtn(parent, "btn_move_right", new Vector2(105, 30), () => BeginRebind("MoveRight"));

        MakeTxt(parent, "lbl_jump", "Jump", 17, FontStyles.Normal, TextPrimary,
            TextAlignmentOptions.Right, new Vector2(0.5f, 0.5f), new Vector2(180, 26), new Vector2(-115, -40));
        _btnJumpLabel = MakeRebindBtn(parent, "btn_jump", new Vector2(105, -40), () => BeginRebind("Jump"));

        MakeTxt(parent, "lbl_hint", "Click a button, then press the new key (Esc to cancel)", 13, FontStyles.Italic, TextDim,
            TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(400, 24), new Vector2(0, -110));

        RefreshControlsLabels();
    }

    void BuildAccountTab(GameObject parent)
    {
        _accountStatusText = MakeTxt(parent, "lbl_account_status", "—", 19, FontStyles.Bold, TextPrimary,
            TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(420, 60), new Vector2(0, 60));

        MakeAccountActionBtn(parent, -40);

        RefreshAccountTab();
    }

    // ────────────────────────────────────────────────────────────────────────
    //  CONTROLS TAB — rebinding
    // ────────────────────────────────────────────────────────────────────────

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

        if (_btnMoveLeftLabel)  _btnMoveLeftLabel.text  = _awaitingRebindFor == "MoveLeft"  ? "Press a key…" : left.ToString();
        if (_btnMoveRightLabel) _btnMoveRightLabel.text = _awaitingRebindFor == "MoveRight" ? "Press a key…" : right.ToString();
        if (_btnJumpLabel)      _btnJumpLabel.text       = _awaitingRebindFor == "Jump"      ? "Press a key…" : jump.ToString();
    }

    void Update()
    {
        if (_awaitingRebindFor == null) return;
        if (!TryGetAnyKeyDown(out var pressed)) return;

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

    // ────────────────────────────────────────────────────────────────────────
    //  ACCOUNT TAB
    // ────────────────────────────────────────────────────────────────────────

    void MakeAccountActionBtn(GameObject parent, float yOffset)
    {
        var go  = new GameObject("btn_account_action");
        go.transform.SetParent(parent.transform, false);
        var img = go.AddComponent<Image>();
        img.color = AccBlue;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(OnAccountActionClicked);

        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(BTN_W, BTN_H);
        rt.anchoredPosition = new Vector2(0, yOffset);

        var txtGO = new GameObject("Label");
        txtGO.transform.SetParent(go.transform, false);
        var txt = txtGO.AddComponent<TextMeshProUGUI>();
        txt.fontSize  = 20;
        txt.fontStyle = FontStyles.Bold;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color     = TextPrimary;
        var trt = txt.rectTransform;
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;

        _accountActionBtn   = btn;
        _accountActionLabel = txt;
    }

    void OnAccountActionClicked()
    {
        Click();
        if (AuthManager.Instance != null && AuthManager.Instance.IsLoggedIn)
        {
            AuthManager.Instance.Logout();
            MainMenuAuthButton.Instance?.RefreshButton();
            RefreshAccountTab();
        }
        else
        {
            ShowPanel(_mainPanel);
            LoginPanelUI.Instance?.Show();
        }
    }

    void RefreshAccountTab()
    {
        bool loggedIn = AuthManager.Instance != null && AuthManager.Instance.IsLoggedIn;
        bool isGuest  = AuthManager.Instance != null && AuthManager.Instance.IsGuest;

        if (_accountStatusText != null)
        {
            _accountStatusText.text = !loggedIn
                ? "Not logged in"
                : (isGuest ? "Playing as Guest" : $"Logged in as: {AuthManager.Instance.CurrentUsername}");
        }

        if (_accountActionLabel != null)
            _accountActionLabel.text = loggedIn ? "LOG OUT" : "LOG IN";

        if (_accountActionBtn != null)
        {
            var normal = loggedIn ? AccRed  : AccBlue;
            var hover  = loggedIn ? AccRedHov : AccBlueHov;
            _accountActionBtn.GetComponent<Image>().color = normal;
            _accountActionBtn.colors = new ColorBlock
            {
                normalColor      = normal,
                highlightedColor = hover,
                pressedColor     = AccPress,
                selectedColor    = hover,
                colorMultiplier  = 1f,
                fadeDuration     = 0.10f
            };
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    //  SMALL BUILDERS SHARED BY THE SETTINGS TABS
    // ────────────────────────────────────────────────────────────────────────

    void MakeTabBtn(string name, string label, float xOffset, float yOffset, UnityEngine.Events.UnityAction cb)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(_settingsPanel.transform, false);
        var img = go.AddComponent<Image>();
        img.color = BgCard;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.colors = new ColorBlock
        {
            normalColor      = BgCard,
            highlightedColor = AccBlueHov,
            pressedColor     = AccPress,
            selectedColor    = AccBlue,
            colorMultiplier  = 1f,
            fadeDuration     = 0.10f
        };
        btn.onClick.AddListener(() => { Click(); cb(); });

        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(104, 34);
        rt.anchoredPosition = new Vector2(xOffset, yOffset);

        var txtGO = new GameObject("Label");
        txtGO.transform.SetParent(go.transform, false);
        var txt = txtGO.AddComponent<TextMeshProUGUI>();
        txt.text      = label;
        txt.fontSize  = 13;
        txt.fontStyle = FontStyles.Bold;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color     = TextPrimary;
        var trt = txt.rectTransform;
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;
    }

    CyclerControl MakeCycler(GameObject parent, string name, float yOffset, string[] options, int startIndex, System.Action<int> onChanged)
    {
        var labelGO = new GameObject(name + "_Value");
        labelGO.transform.SetParent(parent.transform, false);
        var label = labelGO.AddComponent<TextMeshProUGUI>();
        label.fontSize  = 16;
        label.alignment = TextAlignmentOptions.Center;
        label.color     = TextPrimary;
        var lrt = label.rectTransform;
        lrt.anchorMin = lrt.anchorMax = new Vector2(0.5f, 0.5f);
        lrt.sizeDelta = new Vector2(190, 26);
        lrt.anchoredPosition = new Vector2(30, yOffset);

        var cycler = new CyclerControl { Options = options, Label = label, OnChanged = onChanged };

        MakeArrowBtn(parent, name + "_Prev", "◀", new Vector2(-95, yOffset), () => cycler.Step(-1));
        MakeArrowBtn(parent, name + "_Next", "▶", new Vector2(135, yOffset), () => cycler.Step(1));

        cycler.Set(startIndex);
        return cycler;
    }

    void MakeArrowBtn(GameObject parent, string name, string glyph, Vector2 pos, UnityEngine.Events.UnityAction cb)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.18f, 0.28f, 0.42f);
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(() => { Click(); cb(); });
        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(28, 26);
        rt.anchoredPosition = pos;

        var txtGO = new GameObject("Glyph");
        txtGO.transform.SetParent(go.transform, false);
        var txt = txtGO.AddComponent<TextMeshProUGUI>();
        txt.text      = glyph;
        txt.fontSize  = 15;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color     = TextPrimary;
        var trt = txt.rectTransform;
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;
    }

    TextMeshProUGUI MakeRebindBtn(GameObject parent, string name, Vector2 pos, UnityEngine.Events.UnityAction cb)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var img = go.AddComponent<Image>();
        img.color = BgCard;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.colors = new ColorBlock
        {
            normalColor      = BgCard,
            highlightedColor = AccBlueHov,
            pressedColor     = AccPress,
            selectedColor    = AccBlue,
            colorMultiplier  = 1f,
            fadeDuration     = 0.10f
        };
        btn.onClick.AddListener(() => { Click(); cb(); });
        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(140, 34);
        rt.anchoredPosition = pos;

        var txtGO = new GameObject("Label");
        txtGO.transform.SetParent(go.transform, false);
        var txt = txtGO.AddComponent<TextMeshProUGUI>();
        txt.fontSize  = 14;
        txt.fontStyle = FontStyles.Bold;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color     = TextPrimary;
        var trt = txt.rectTransform;
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;

        return txt;
    }

    // ────────────────────────────────────────────────────────────────────────
    //  STARFIELD + NEBULA
    // ────────────────────────────────────────────────────────────────────────

    void BuildStarfield(GameObject parent, int count)
    {
        var root = new GameObject("Starfield");
        root.transform.SetParent(parent.transform, false);
        root.transform.SetSiblingIndex(1);                   // just above background
        var rootRt = root.AddComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero; rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = rootRt.offsetMax = Vector2.zero;

        var rng = new System.Random(1337);
        for (int i = 0; i < count; i++)
        {
            float x     = (float)rng.NextDouble();
            float y     = (float)rng.NextDouble();
            float size  = (float)(rng.NextDouble() * 2.8f + 0.8f);
            float alpha = (float)(rng.NextDouble() * 0.55f + 0.25f);

            var starGO  = new GameObject($"Star_{i}");
            starGO.transform.SetParent(root.transform, false);
            var img     = starGO.AddComponent<Image>();
            img.color   = new Color(1f, 1f, 1f, alpha);
            var rt      = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(x, y);
            rt.sizeDelta        = new Vector2(size, size);
            rt.anchoredPosition = Vector2.zero;
        }
    }

    void BuildNebulaGlow(GameObject parent)
    {
        // Soft colored glow blobs for depth
        AddGlow(parent, new Color(0.15f, 0.10f, 0.40f, 0.12f), new Vector2(0.18f, 0.70f), new Vector2(520, 380));
        AddGlow(parent, new Color(0.05f, 0.20f, 0.45f, 0.10f), new Vector2(0.82f, 0.30f), new Vector2(440, 340));
        AddGlow(parent, new Color(0.30f, 0.10f, 0.20f, 0.08f), new Vector2(0.50f, 0.15f), new Vector2(600, 260));
    }

    void AddGlow(GameObject parent, Color color, Vector2 anchor, Vector2 size)
    {
        var go  = new GameObject("Glow");
        go.transform.SetParent(parent.transform, false);
        go.transform.SetSiblingIndex(2);
        var img = go.AddComponent<Image>();
        img.color = color;
        var rt  = img.rectTransform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.sizeDelta        = size;
        rt.anchoredPosition = Vector2.zero;
    }

    // ────────────────────────────────────────────────────────────────────────
    //  FOOTER
    // ────────────────────────────────────────────────────────────────────────

    void BuildFooter(GameObject parent)
    {
        // Version — bottom-left
        MakeTxt(parent, "Version", VERSION, 13, FontStyles.Normal, TextDim,
            TextAlignmentOptions.Left, new Vector2(0f, 0f), new Vector2(100, 22), new Vector2(14, 12));

        // Copyright — bottom-center
        MakeTxt(parent, "Copyright", "© 2025 CosmicRumble", 13, FontStyles.Normal, TextDim,
            TextAlignmentOptions.Center, new Vector2(0.5f, 0f), new Vector2(280, 22), new Vector2(0, 12));

        // Bottom separator
        var lineGO  = new GameObject("FooterLine");
        lineGO.transform.SetParent(parent.transform, false);
        var lineImg = lineGO.AddComponent<Image>();
        lineImg.color = Separator;
        var lineRt  = lineImg.rectTransform;
        lineRt.anchorMin        = new Vector2(0.05f, 0f);
        lineRt.anchorMax        = new Vector2(0.95f, 0f);
        lineRt.pivot            = new Vector2(0.5f, 0f);
        lineRt.sizeDelta        = new Vector2(0, 1);
        lineRt.anchoredPosition = new Vector2(0, 34);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  CALLBACKS
    // ════════════════════════════════════════════════════════════════════════

    void OnQuit()
    {
        GameConfig.Instance?.Save();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void ShowPanel(GameObject panel)
    {
        _mainPanel.SetActive(panel == _mainPanel);
        _settingsPanel.SetActive(panel == _settingsPanel);
    }

    void LoadSettingsValues()
    {
        var cfg = GameConfig.Instance;
        if (cfg == null) return;
        if (_masterSlider) _masterSlider.value = cfg.MasterVolume;
        if (_musicSlider)  _musicSlider.value  = cfg.MusicVolume;
        if (_sfxSlider)    _sfxSlider.value    = cfg.SfxVolume;
        if (_fsToggle)     _fsToggle.isOn      = cfg.Fullscreen;
    }

    static void Click() => AudioManager.Instance?.PlayClick();

    // ════════════════════════════════════════════════════════════════════════
    //  UI HELPERS
    // ════════════════════════════════════════════════════════════════════════

    void EnsureEventSystem()
    {
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() != null) return;
        var es = new GameObject("EventSystem");
        es.AddComponent<UnityEngine.EventSystems.EventSystem>();
        es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
    }

    Image MakeStretch(GameObject parent, string name, Color color)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        var rt  = img.rectTransform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return img;
    }

    GameObject MakePanel(GameObject parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return go;
    }

    // Primary button
    void MakeBtn(GameObject parent, string name, string label, float yOffset,
                 Color normal, Color hover,
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
            pressedColor     = AccPress,
            selectedColor    = hover,
            colorMultiplier  = 1f,
            fadeDuration     = 0.10f
        };
        btn.onClick.AddListener(callback);

        var rt = img.rectTransform;
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = new Vector2(BTN_W, BTN_H);
        rt.anchoredPosition = new Vector2(0, yOffset);

        // Left color accent strip
        var stripGO  = new GameObject("Strip");
        stripGO.transform.SetParent(go.transform, false);
        var stripImg = stripGO.AddComponent<Image>();
        stripImg.color = new Color(1f, 1f, 1f, 0.35f);
        var stripRt  = stripImg.rectTransform;
        stripRt.anchorMin        = new Vector2(0f, 0f);
        stripRt.anchorMax        = new Vector2(0f, 1f);
        stripRt.pivot            = new Vector2(0f, 0.5f);
        stripRt.sizeDelta        = new Vector2(4, 0);
        stripRt.anchoredPosition = Vector2.zero;

        var txtGO = new GameObject("Label");
        txtGO.transform.SetParent(go.transform, false);
        var txt = txtGO.AddComponent<TextMeshProUGUI>();
        txt.text      = label;
        txt.fontSize  = 21;
        txt.fontStyle = FontStyles.Bold;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color     = TextPrimary;
        var trt = txt.rectTransform;
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;
    }

    // Text helper — anchor + offset
    TextMeshProUGUI MakeTxt(GameObject parent, string name, string content,
                             int size, FontStyles style, Color color,
                             TextAlignmentOptions align,
                             Vector2 anchor, Vector2 sizeDelta, Vector2 pos)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var txt = go.AddComponent<TextMeshProUGUI>();
        txt.text      = content;
        txt.fontSize  = size;
        txt.fontStyle = style;
        txt.alignment = align;
        txt.color     = color;
        var rt = txt.rectTransform;
        rt.anchorMin        = rt.anchorMax = anchor;
        rt.sizeDelta        = sizeDelta;
        rt.anchoredPosition = pos;
        return txt;
    }

    void MakeSeparator(GameObject parent, Vector2 pos)
    {
        var go  = new GameObject("Sep");
        go.transform.SetParent(parent.transform, false);
        var img = go.AddComponent<Image>();
        img.color = Separator;
        var rt  = img.rectTransform;
        rt.anchorMin        = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = new Vector2(380, 1);
        rt.anchoredPosition = pos;
    }

    Slider MakeSliderRow(GameObject parent, string label, float yOffset)
    {
        MakeTxt(parent, "lbl_" + label, label, 17, FontStyles.Normal, TextPrimary,
            TextAlignmentOptions.Right, new Vector2(0.5f, 0.5f), new Vector2(160, 26), new Vector2(-105, yOffset));

        return MakeSlider(parent, "sl_" + label, 0f, 1f, 0.7f,
            new Vector2(0.5f, 0.5f), new Vector2(50, yOffset), 170);
    }

    Slider MakeSlider(GameObject parent, string name, float min, float max, float val,
                      Vector2 anchor, Vector2 pos, float width)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var sl = go.AddComponent<Slider>();
        sl.minValue = min; sl.maxValue = max; sl.value = val;

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin        = rt.anchorMax = anchor;
        rt.sizeDelta        = new Vector2(width, 16);
        rt.anchoredPosition = pos;

        // Bg
        var bgGO  = new GameObject("Background"); bgGO.transform.SetParent(go.transform, false);
        var bgImg = bgGO.AddComponent<Image>(); bgImg.color = BarBg;
        var bgRt  = bgImg.rectTransform;
        bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;

        // Fill
        var fillArea = new GameObject("Fill Area"); fillArea.transform.SetParent(go.transform, false);
        var faRt     = fillArea.AddComponent<RectTransform>();
        faRt.anchorMin = new Vector2(0, 0.25f); faRt.anchorMax = new Vector2(1, 0.75f);
        faRt.offsetMin = new Vector2(5, 0); faRt.offsetMax = new Vector2(-5, 0);
        var fillGO  = new GameObject("Fill"); fillGO.transform.SetParent(fillArea.transform, false);
        var fillImg = fillGO.AddComponent<Image>(); fillImg.color = AccBlue;
        var fillRt  = fillImg.rectTransform;
        fillRt.anchorMin = Vector2.zero; fillRt.anchorMax = new Vector2(1, 1);
        fillRt.offsetMin = fillRt.offsetMax = Vector2.zero;

        // Handle
        var handleArea = new GameObject("Handle Slide Area"); handleArea.transform.SetParent(go.transform, false);
        var haRt       = handleArea.AddComponent<RectTransform>();
        haRt.anchorMin = Vector2.zero; haRt.anchorMax = Vector2.one;
        haRt.offsetMin = new Vector2(8, 0); haRt.offsetMax = new Vector2(-8, 0);
        var handleGO  = new GameObject("Handle"); handleGO.transform.SetParent(handleArea.transform, false);
        var handleImg = handleGO.AddComponent<Image>(); handleImg.color = Color.white;
        var handleRt  = handleImg.rectTransform; handleRt.sizeDelta = new Vector2(16, 26);

        sl.fillRect      = fillRt;
        sl.handleRect    = handleRt;
        sl.targetGraphic = handleImg;
        return sl;
    }

    Toggle MakeToggle(GameObject parent, float yOffset)
    {
        var go     = new GameObject("Toggle_FS");
        go.transform.SetParent(parent.transform, false);
        var toggle = go.AddComponent<Toggle>();
        var rt     = go.GetComponent<RectTransform>();
        rt.anchorMin        = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = new Vector2(48, 26);
        rt.anchoredPosition = new Vector2(110, yOffset);

        var bgGO  = new GameObject("Bg"); bgGO.transform.SetParent(go.transform, false);
        var bgImg = bgGO.AddComponent<Image>(); bgImg.color = new Color(0.20f, 0.20f, 0.32f);
        var bgRt  = bgImg.rectTransform;
        bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;

        var checkGO  = new GameObject("Checkmark"); checkGO.transform.SetParent(bgGO.transform, false);
        var checkImg = checkGO.AddComponent<Image>(); checkImg.color = AccGreen;
        var checkRt  = checkImg.rectTransform;
        checkRt.anchorMin = new Vector2(0.06f, 0.06f);
        checkRt.anchorMax = new Vector2(0.94f, 0.94f);
        checkRt.offsetMin = checkRt.offsetMax = Vector2.zero;

        toggle.targetGraphic = bgImg;
        toggle.graphic       = checkImg;
        toggle.isOn          = Screen.fullScreen;
        return toggle;
    }
}
