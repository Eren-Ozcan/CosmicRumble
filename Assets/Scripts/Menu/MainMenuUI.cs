using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using CosmicRumble.Economy;
using CosmicRumble.Economy.IAP;
using CosmicRumble.Achievements;
using CosmicRumble.Cloud;
using CosmicRumble.Localization;
using CosmicRumble.Legal;

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
    static readonly Color AccCyan      = new Color(0.15f, 0.70f, 0.75f, 1.00f);
    static readonly Color AccCyanHov   = new Color(0.25f, 0.85f, 0.90f, 1.00f);
    static readonly Color AccPress     = new Color(0.10f, 0.18f, 0.45f, 1.00f);
    static readonly Color TextPrimary  = Color.white;
    static readonly Color TextDim      = new Color(0.65f, 0.70f, 0.82f, 1.00f);
    static readonly Color BarBg        = new Color(0.12f, 0.12f, 0.22f, 1.00f);
    static readonly Color Separator    = new Color(0.25f, 0.25f, 0.40f, 0.60f);

    // ── Brawl Stars plaka paleti ────────────────────────────────────────────
    static readonly Color PlateDark    = new Color(0.165f, 0.175f, 0.215f, 1f); // koyu füme plaka
    static readonly Color PlateEdge    = new Color(0.085f, 0.09f,  0.115f, 1f); // plakanın alt kenarı
    static readonly Color BrawlYellow  = new Color(0.99f,  0.79f,  0.10f,  1f); // OYNA/DÜKKAN sarısı
    static readonly Color YellowEdge   = new Color(0.72f,  0.50f,  0.02f,  1f);
    static readonly Color NameBlue     = new Color(0.45f,  0.80f,  1.00f,  1f); // oyuncu adı mavisi

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

    CyclerControl _languageCycler;

    // Ayarlar paneli "OK" ile onaylanana kadar hiçbir gerçek ayar (GameConfig.Instance,
    // LocalizationManager, Screen) değişmez — her kontrol sadece bu pending alanları günceller.
    // Panel her açıldığında (OpenSettingsPanel) bu alanlar güncel cfg/dil değerinden yeniden
    // tohumlanır, böylece "BACK" ile bırakılan yarım değişiklikler bir sonraki açılışa taşınmaz.
    float    _pendingMaster, _pendingMusic, _pendingSfx;
    bool     _pendingFullscreen, _pendingVSync;
    int      _pendingResolutionIndex, _pendingQualityIndex;
    KeyCode  _pendingMoveLeftKey, _pendingMoveRightKey, _pendingJumpKey;
    Language _pendingLanguage;

    TextMeshProUGUI _accountStatusText;
    TextMeshProUGUI _googleStatusText, _cosmicStatusText;
    GameObject _googleLinkBtn, _cosmicLinkBtn, _logoutBtn;

    Image           _avatarImg;
    TextMeshProUGUI _avatarInitialTxt;

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

        // Açılış perdesi: sessiz giriş + bulut senkronu bitene kadar menü görünmez.
        LoadingScreenUI.Instance?.Show(Loc.T("Connecting..."));

        // 1) Sessiz oturum kurtarma: session token bağlı hesabı geri yükler; Android'de
        //    ayrıca sessiz GPGS bağlama denenir. IsGuest/CurrentUsername kimliklerden dolar
        //    (eskiden restore edilen bağlı hesap bile misafir sayılıyordu — kapı her açılışta
        //    çıkıyordu; artık çıkmaz).
        if (AuthManager.Instance != null)
        {
            var silentTask = AuthManager.Instance.TrySilentSignInAsync();
            while (!silentTask.IsCompleted) yield return null;
        }

        // 2) Giriş kapısı: adlı/platform hesabı yoksa TAM EKRAN giriş ekranı (popup değil).
        //    Cihazda atlanamaz; Editor'da "MİSAFİR OLARAK DEVAM (TEST)" butonu var.
        bool named = AuthManager.Instance != null && AuthManager.Instance.HasNamedAccount;
        if (!named && LoginScreenUI.Instance != null)
        {
            LoadingScreenUI.Instance?.Hide();
            var waitTask = LoginScreenUI.Instance.ShowAndWaitAsync();
            while (!waitTask.IsCompleted) yield return null;
            // Not: Cosmic ID Login() sahneyi yeniden yükler — o durumda bu coroutine ölür ve
            // yeni bootstrap sessiz restore ile devam eder; buraya hiç dönülmez.
            LoadingScreenUI.Instance?.Show(Loc.T("Fetching cloud save..."));
        }
        else
        {
            LoadingScreenUI.Instance?.SetStatus(Loc.T("Fetching cloud save..."));
        }

        // 3) Bulut senkronu — progress manager'lar oluşturulmadan ÖNCE bitmeli.
        if (CloudSaveManager.Instance != null)
        {
            yield return CloudSaveManager.Instance.InitializeAndPull();

            // Zaman aşımı/erişilemezlik durumunda kullanıcıya "bir şey mi bozuldu" hissi vermek
            // yerine kısa, sakin bir "çevrimdışı" bildirimi göster (önceden sessizce sonraki
            // adıma atlıyordu — ağ yoksa açılış donmuş gibi görünebiliyordu).
            if (CloudSaveManager.Instance.IsUnavailable)
            {
                LoadingScreenUI.Instance?.SetStatus(Loc.T("Playing offline"));
                yield return new WaitForSecondsRealtime(0.8f);
            }
        }

        // 4) Offline/başarısızlık emniyeti: hâlâ oturum yoksa misafir olarak devam (oyun offline
        //    da açılmalı) — "Misafir" UI'da görünmez, PlayerIdentity takma ad üretir.
        if (AuthManager.Instance != null && !AuthManager.Instance.IsLoggedIn)
        {
            var guestTask = AuthManager.Instance.LoginAsGuest();
            while (!guestTask.IsCompleted) yield return null;
        }

        LoadingScreenUI.Instance?.SetStatus(Loc.T("Loading profile..."));
        EnsureProgressSingletons();

        // UGS player name'i (Nova731#1234) erken eşitle — SOSYAL panelin arkadaş kodu ve
        // leaderboard görünen adı buna bağlı (eskiden sadece leaderboard açılınca yapılıyordu).
        if (CosmicRumble.Cloud.LeaderboardManager.Instance != null)
        {
            var nameTask = CosmicRumble.Cloud.LeaderboardManager.Instance.SyncPlayerNameAsync();
            while (!nameTask.IsCompleted) yield return null;
        }

        // Arkadaş sistemi — oturum kesinleştikten sonra (hesap değişiminde sahne reload'u yine
        // buradan geçer, yeni PlayerId ile yeniden init olur). Boot'u bloklamaz.
        if (CosmicRumble.Social.FriendsManager.Instance != null)
            _ = CosmicRumble.Social.FriendsManager.Instance.EnsureInitializedAsync();

        // Crash raporlama Player Settings üzerinden otomatik (Cloud Diagnostics paketi + native
        // CrashReportHandler) — kod gerekmiyor. Analitik veri toplama burada başlatılıyor.
        CosmicRumble.Analytics.AnalyticsManager.Instance?.EnsureStarted();

        BuildUI();
        ShowPanel(_mainPanel);
        LoadingScreenUI.Instance?.Hide();
    }

    void EnsureCoreSingletons()
    {
        if (LocalizationManager.Instance == null) new GameObject("LocalizationManager").AddComponent<LocalizationManager>();
        if (GameConfig.Instance      == null) new GameObject("GameConfig").AddComponent<GameConfig>();
        if (SceneFader.Instance      == null) new GameObject("SceneFader").AddComponent<SceneFader>();
        if (AuthManager.Instance     == null) new GameObject("AuthManager").AddComponent<AuthManager>();
        if (AudioManager.Instance    == null) new GameObject("AudioManager").AddComponent<AudioManager>();
        if (CloudSaveManager.Instance == null) new GameObject("CloudSaveManager").AddComponent<CloudSaveManager>();
        if (CosmicRumble.Analytics.AnalyticsManager.Instance == null)
            new GameObject("AnalyticsManager").AddComponent<CosmicRumble.Analytics.AnalyticsManager>();
        if (CosmicRumble.Notifications.LocalNotificationManager.Instance == null)
            new GameObject("LocalNotificationManager").AddComponent<CosmicRumble.Notifications.LocalNotificationManager>();
        if (CosmicRumble.Social.FriendsManager.Instance == null)
            new GameObject("FriendsManager").AddComponent<CosmicRumble.Social.FriendsManager>();

        // Açılış ekranları — sahne ömürlü (DontDestroyOnLoad değil), her menü dönüşünde yeniden kurulur.
        if (LoadingScreenUI.Instance == null) new GameObject("LoadingScreenUI").AddComponent<LoadingScreenUI>();
        if (LoginScreenUI.Instance   == null) new GameObject("LoginScreenUI").AddComponent<LoginScreenUI>();
    }

    void EnsureProgressSingletons()
    {
        // Economy/achievement backend
        if (CurrencyManager.Instance    == null) new GameObject("CurrencyManager").AddComponent<CurrencyManager>();
        if (CostumeManager.Instance     == null) new GameObject("CostumeManager").AddComponent<CostumeManager>();
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
            string userKey = (auth != null && auth.HasNamedAccount) ? auth.UserFileKey : null;
            AchievementManager.Instance.LoadForUser(userKey);
        }
        if (AchievementTracker.Instance == null) new GameObject("AchievementTracker").AddComponent<AchievementTracker>();
        if (IAPManager.Instance          == null) new GameObject("IAPManager").AddComponent<IAPManager>();
        if (LeaderboardManager.Instance  == null) new GameObject("LeaderboardManager").AddComponent<LeaderboardManager>();
        // Paneller MenuScene'e bağlı yaşar (DontDestroyOnLoad değil) — her menü dönüşünde yeniden kurulur.
        if (LeaderboardPanelUI.Instance  == null) new GameObject("LeaderboardPanelUI").AddComponent<LeaderboardPanelUI>();
        if (SocialPanelUI.Instance       == null) new GameObject("SocialPanelUI").AddComponent<SocialPanelUI>();
        if (PartyLobbyPanelUI.Instance   == null) new GameObject("PartyLobbyPanelUI").AddComponent<PartyLobbyPanelUI>();
        if (InvitePopupUI.Instance       == null) new GameObject("InvitePopupUI").AddComponent<InvitePopupUI>();
        if (WardrobePanelUI.Instance     == null) new GameObject("WardrobePanelUI").AddComponent<WardrobePanelUI>();
        if (AvatarManager.Instance       == null) new GameObject("AvatarManager").AddComponent<AvatarManager>();
        if (AvatarPickerUI.Instance      == null) new GameObject("AvatarPickerUI").AddComponent<AvatarPickerUI>();
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

        // ── Background — canlı mor sahne (Brawl Stars lobi zemini gibi) ─────
        var bg = MakeStretch(canvasGO, "Background", Color.white);
        UiKit.Gradient(bg, new Color(0.17f, 0.10f, 0.40f, 1f), new Color(0.34f, 0.10f, 0.33f, 1f));
        BuildStarfield(canvasGO, 90);
        BuildNebulaGlow(canvasGO);
        BuildPattern(canvasGO, 34);

        // ── Title ────────────────────────────────────────────────────────────
        BuildTitle(canvasGO);

        // ── Panels ───────────────────────────────────────────────────────────
        _mainPanel     = MakePanel(canvasGO, "MainPanel");
        _settingsPanel = MakePanel(canvasGO, "SettingsPanel");

        BuildMainPanel();
        BuildSettingsPanel();
        // Footer yok — BS lobisinde alt bilgi çubuğu bulunmaz (sürüm no Ayarlar'a taşındı).
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
        UiKit.Round(glowImg);
        var glowRt  = glowImg.rectTransform;
        glowRt.anchorMin        = new Vector2(0.5f, 1f);
        glowRt.anchorMax        = new Vector2(0.5f, 1f);
        glowRt.pivot            = new Vector2(0.5f, 1f);
        glowRt.sizeDelta        = new Vector2(470, 88);
        glowRt.anchoredPosition = new Vector2(0, -12);

        // Main title — kompakt banner (BS üst-orta gibi), beyaz konturlu altın yazı
        var titleGO  = new GameObject("Title");
        titleGO.transform.SetParent(parent.transform, false);
        var title    = titleGO.AddComponent<TextMeshProUGUI>();
        title.text      = "COSMIC RUMBLE";
        title.fontSize  = 40;
        title.alignment = TextAlignmentOptions.Center;
        title.color     = AccGold;
        UiKit.BrawlText(title);
        var titleRt = title.rectTransform;
        titleRt.anchorMin        = new Vector2(0.5f, 1f);
        titleRt.anchorMax        = new Vector2(0.5f, 1f);
        titleRt.pivot            = new Vector2(0.5f, 1f);
        titleRt.sizeDelta        = new Vector2(600, 52);
        titleRt.anchoredPosition = new Vector2(0, -20);

        // Separator line
        var lineGO  = new GameObject("TitleLine");
        lineGO.transform.SetParent(parent.transform, false);
        var lineImg = lineGO.AddComponent<Image>();
        lineImg.color = AccGold;
        var lineRt  = lineImg.rectTransform;
        lineRt.anchorMin        = new Vector2(0.5f, 1f);
        lineRt.anchorMax        = new Vector2(0.5f, 1f);
        lineRt.pivot            = new Vector2(0.5f, 1f);
        lineRt.sizeDelta        = new Vector2(340, 2);
        lineRt.anchoredPosition = new Vector2(0, -88);

        // Subtitle
        var subGO  = new GameObject("Subtitle");
        subGO.transform.SetParent(parent.transform, false);
        var sub    = subGO.AddComponent<TextMeshProUGUI>();
        sub.text      = Loc.T("Turn-based planetary warfare");
        sub.fontSize  = 16;
        sub.fontStyle = FontStyles.Italic;
        sub.alignment = TextAlignmentOptions.Center;
        sub.color     = TextDim;
        var subRt = sub.rectTransform;
        subRt.anchorMin        = new Vector2(0.5f, 1f);
        subRt.anchorMax        = new Vector2(0.5f, 1f);
        subRt.pivot            = new Vector2(0.5f, 1f);
        subRt.sizeDelta        = new Vector2(500, 26);
        subRt.anchoredPosition = new Vector2(0, -94);
    }

    // ────────────────────────────────────────────────────────────────────────
    //  MAIN PANEL
    // ────────────────────────────────────────────────────────────────────────

    // ── Brawl Stars tarzı lobi ana ekranı ────────────────────────────────────
    // Araştırma bulguları (Brawl Stars + modern mobil lobi kalıpları):
    //  • Ana ekran bir menü listesi değil, HUB'dır — merkez temiz kalır.
    //  • Tek birincil eylem: BÜYÜK SARI OYNA, sağ-alt (yatay tutuşta başparmak bölgesi).
    //  • Üst bar: sol = profil + kupa, sağ = para birimleri (dokunmatikten uzak bilgi alanı).
    //  • İkincil özellikler sol kenarda dikey buton yığını.
    void BuildMainPanel()
    {
        BuildLobbyDecor();
        BuildTopBar();
        BuildLeftRail();
        BuildPlayCluster();
        BuildDrawer(); // en son: her şeyin üstünde render edilsin
    }

    /// <summary>Dekor: alt ufukta büyük gezegen + küçük bir ay (derinlik hissi).</summary>
    void BuildLobbyDecor()
    {
        // MakeCircleDecor her çağrıda SetAsFirstSibling yapar → SON oluşturulan en arkada çizilir.
        // Gezegen önce, rim (hale) sonra oluşturulur ki rim gezegenin ARKASINA düşsün ve yalnızca
        // üst kenardan 12px'lik ince bir atmosfer parlaması olarak görünsün.
        MakeCircleDecor(_mainPanel, "PlanetHorizon", new Vector2(0.42f, 0f), new Vector2(-80, -486),
            1250, new Color(0.11f, 0.09f, 0.24f, 1f));
        MakeCircleDecor(_mainPanel, "PlanetRim",     new Vector2(0.42f, 0f), new Vector2(-80, -474),
            1250, new Color(0.25f, 0.32f, 0.70f, 0.35f));
        MakeCircleDecor(_mainPanel, "Moon",          new Vector2(0.74f, 0.72f), Vector2.zero,
            72, new Color(0.24f, 0.22f, 0.42f, 1f));
    }

    void MakeCircleDecor(GameObject parent, string name, Vector2 anchor, Vector2 pos, float dia, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.transform.SetAsFirstSibling(); // butonların arkasında kalsın
        var img = go.AddComponent<Image>();
        img.sprite = UiKit.CircleSprite;
        img.color  = color;
        img.raycastTarget = false;
        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.sizeDelta = new Vector2(dia, dia);
        rt.anchoredPosition = pos;
    }

    /// <summary>
    /// Brawl Stars üst barı: sol = [avatar+ad plakası][kupa kutusu] (iki ayrı koyu plaka),
    /// sağ = [gold][gem] para plakaları + [☰] menü (Ayarlar'ı açar).
    /// </summary>
    void BuildTopBar()
    {
        string playerName = PlayerIdentity.Get();

        // ── Profil plakası (sol-üst) → Sıralama açılır ───────────────────
        var profile = MakePlate(_mainPanel, "ProfilePlate", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(16, -14), new Vector2(252, 64),
            () => { Click(); LeaderboardPanelUI.Instance?.Show(); });

        var avatarGO = new GameObject("Avatar");
        avatarGO.transform.SetParent(profile.transform, false);
        _avatarImg = avatarGO.AddComponent<Image>();
        _avatarImg.raycastTarget = false;
        var avatarRt = _avatarImg.rectTransform;
        avatarRt.anchorMin = avatarRt.anchorMax = new Vector2(0f, 0.5f);
        avatarRt.sizeDelta = new Vector2(48, 48);
        avatarRt.anchoredPosition = new Vector2(32, 0);
        _avatarInitialTxt = MakeTxt(avatarGO, "Initial", "",
            24, FontStyles.Normal, Color.white,
            TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(48, 48), Vector2.zero);
        UiKit.BrawlText(_avatarInitialTxt);
        _avatarInitialTxt.raycastTarget = false;
        ApplyAvatarVisuals(AvatarManager.Instance?.GetSelected(), playerName);

        if (AvatarManager.Instance != null)
        {
            AvatarManager.Instance.OnAvatarChanged -= OnAvatarChangedForTopBar;
            AvatarManager.Instance.OnAvatarChanged += OnAvatarChangedForTopBar;
        }

        // Avatar köşesinde küçük "düzenle" rozeti — kendi Button/raycast hedefi, plakanın geri
        // kalanı (Sıralama açan tıklama) etkilenmez, yalnızca bu küçük alan avatar seçiciyi açar.
        var editBadgeGO = new GameObject("btn_edit_avatar");
        editBadgeGO.transform.SetParent(avatarGO.transform, false);
        var editBadgeImg = editBadgeGO.AddComponent<Image>();
        editBadgeImg.sprite = UiKit.CircleSprite;
        editBadgeImg.color  = new Color(0.12f, 0.12f, 0.18f, 1f);
        var editBadgeBtn = editBadgeGO.AddComponent<Button>();
        editBadgeBtn.targetGraphic = editBadgeImg;
        editBadgeBtn.onClick.AddListener(() => { Click(); AvatarPickerUI.Instance?.Show(); });
        UiKit.Hover(editBadgeGO);
        var editBadgeRt = editBadgeImg.rectTransform;
        editBadgeRt.anchorMin = editBadgeRt.anchorMax = new Vector2(1f, 0f);
        editBadgeRt.sizeDelta = new Vector2(20, 20);
        editBadgeRt.anchoredPosition = new Vector2(-2, 2);
        var editBadgeLbl = MakeTxt(editBadgeGO, "Lbl", "+", 13, FontStyles.Bold, Color.white,
            TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        editBadgeLbl.raycastTarget = false;
        editBadgeLbl.rectTransform.anchorMin = Vector2.zero;
        editBadgeLbl.rectTransform.anchorMax = Vector2.one;
        editBadgeLbl.rectTransform.offsetMin = editBadgeLbl.rectTransform.offsetMax = Vector2.zero;

        var nameTxt = MakeTxt(profile, "Name", playerName, 19, FontStyles.Normal, NameBlue,
            TextAlignmentOptions.Left, new Vector2(0.5f, 0.5f), new Vector2(180, 30), new Vector2(42, 0));
        UiKit.BrawlText(nameTxt);
        nameTxt.overflowMode = TextOverflowModes.Ellipsis;

        // ── Kupa kutusu (profilin sağında) → Sıralama açılır ─────────────
        var trophyPlate = MakePlate(_mainPanel, "TrophyPlate", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(280, -14), new Vector2(226, 64),
            () => { Click(); LeaderboardPanelUI.Instance?.Show(); });

        MakeIconCircle(trophyPlate, AccGold, Loc.T("T"), new Vector2(30, 8));

        int trophies = CosmicRumble.Cloud.LeaderboardManager.Instance != null
            ? CosmicRumble.Cloud.LeaderboardManager.Instance.Trophies : 0;
        var trophyNum = MakeTxt(trophyPlate, "Count", trophies.ToString(), 22, FontStyles.Normal, AccGold,
            TextAlignmentOptions.Left, new Vector2(0.5f, 0.5f), new Vector2(150, 30), new Vector2(42, 8));
        UiKit.BrawlText(trophyNum);
        _trophyText = MakeTxt(trophyPlate, "League",
            CosmicRumble.Cloud.LeaderboardManager.GetLeagueName(trophies), 11, FontStyles.Normal,
            TextDim, TextAlignmentOptions.Left,
            new Vector2(0.5f, 0.5f), new Vector2(190, 16), new Vector2(14, -20));

        // ── Sağ-üst: ☰ menü (Ayarlar) + para plakaları ───────────────────
        var menuBtn = MakePlate(_mainPanel, "MenuBtn", new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-16, -14), new Vector2(72, 64),
            () => { Click(); SetDrawer(true); });
        for (int i = 0; i < 3; i++)
        {
            var bar = new GameObject($"Bar{i}");
            bar.transform.SetParent(menuBtn.transform, false);
            var barImg = bar.AddComponent<Image>();
            barImg.sprite = UiKit.RoundedSprite;
            barImg.type = Image.Type.Sliced;
            barImg.pixelsPerUnitMultiplier = 4f;
            barImg.color = Color.white;
            barImg.raycastTarget = false;
            var barRt = barImg.rectTransform;
            barRt.anchorMin = barRt.anchorMax = new Vector2(0.5f, 0.5f);
            barRt.sizeDelta = new Vector2(34, 6);
            barRt.anchoredPosition = new Vector2(0, 11 - i * 11);
        }

        _gemText  = BuildCurrencyChip("GemChip",  new Vector2(-100, -14), GemChipColor);
        _goldText = BuildCurrencyChip("GoldChip", new Vector2(-262, -14), GoldChipColor);
        RefreshCurrencyChips();
        if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.OnCurrencyChanged += OnCurrencyChangedForTopBar;
    }

    /// <summary>Koyu, hafif eğik Brawl plakası — üst bar ve raylardaki temel yüzey.</summary>
    GameObject MakePlate(GameObject parent, string name, Vector2 anchor, Vector2 pivot,
                         Vector2 pos, Vector2 size, UnityEngine.Events.UnityAction callback)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var img = go.AddComponent<Image>();
        img.color = PlateDark;
        UiKit.Round(img, 1.2f);
        UiKit.Skew(img, 0.07f);
        UiKit.Shadow(go, 4f, 0.5f);

        if (callback != null)
        {
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.colors = UiKit.ButtonColors(PlateDark);
            btn.onClick.AddListener(callback);
            UiKit.Press(go, 0.96f);
            UiKit.Hover(go);
        }

        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        return go;
    }

    /// <summary>Plaka içinde küçük renkli ikon dairesi (art asset'i yok — harfli rozet).</summary>
    void MakeIconCircle(GameObject parent, Color color, string letter, Vector2 pos, float dia = 34f)
    {
        var go = new GameObject("Icon");
        go.transform.SetParent(parent.transform, false);
        var img = go.AddComponent<Image>();
        img.sprite = UiKit.CircleSprite;
        img.color = color;
        img.raycastTarget = false;
        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
        rt.sizeDelta = new Vector2(dia, dia);
        rt.anchoredPosition = pos;

        var lbl = MakeTxt(go, "Lbl", letter, (int)(dia * 0.5f), FontStyles.Normal, Color.white,
            TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(dia, dia), Vector2.zero);
        UiKit.BrawlText(lbl);
        lbl.raycastTarget = false;
    }

    static readonly Color GoldChipColor = new Color(1.00f, 0.80f, 0.20f, 1f);
    static readonly Color GemChipColor  = new Color(0.55f, 0.80f, 1.00f, 1f);
    TextMeshProUGUI _goldText, _gemText, _trophyText;

    TextMeshProUGUI BuildCurrencyChip(string name, Vector2 pos, Color accent)
    {
        var chip = MakePlate(_mainPanel, name, new Vector2(1f, 1f), new Vector2(1f, 1f),
            pos, new Vector2(154, 52),
            () => { Click(); ShopPanelUI.Instance?.Show(); });

        MakeIconCircle(chip, accent, "", new Vector2(24, 0), 28f);

        var txt = MakeTxt(chip, "Value", "0", 19, FontStyles.Normal, TextPrimary,
            TextAlignmentOptions.Left, new Vector2(0.5f, 0.5f), new Vector2(100, 30), new Vector2(24, 0));
        UiKit.BrawlText(txt);
        return txt;
    }

    void RefreshCurrencyChips()
    {
        var cm = CurrencyManager.Instance;
        if (cm == null) return;
        if (_goldText) _goldText.text = cm.Get(CurrencyType.Gold).ToString();
        if (_gemText)  _gemText.text  = cm.Get(CurrencyType.Gem).ToString();
    }

    void OnCurrencyChangedForTopBar(CurrencyType type, long newBalance)
    {
        if (type == CurrencyType.Gold && _goldText) _goldText.text = newBalance.ToString();
        if (type == CurrencyType.Gem  && _gemText)  _gemText.text  = newBalance.ToString();
    }

    void OnDestroy()
    {
        if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.OnCurrencyChanged -= OnCurrencyChangedForTopBar;
        if (AvatarManager.Instance != null)
            AvatarManager.Instance.OnAvatarChanged -= OnAvatarChangedForTopBar;
    }

    /// <summary>Avatar seçici içindeyken bile üst bardaki daireyi canlı günceller — panel yeniden
    /// kurulmadan (menü sahnesi reload olmadan) seçim anında yansır.</summary>
    void OnAvatarChangedForTopBar(AvatarDefinition def) => ApplyAvatarVisuals(def, PlayerIdentity.Get());

    void ApplyAvatarVisuals(AvatarDefinition def, string fallbackName)
    {
        if (_avatarImg == null || _avatarInitialTxt == null) return;

        bool hasIcon = def != null && def.icon != null;
        if (hasIcon)
        {
            _avatarImg.sprite = def.icon;
            _avatarImg.type   = Image.Type.Simple;
            _avatarImg.preserveAspect = true;
            _avatarImg.color  = Color.white;
        }
        else
        {
            _avatarImg.sprite = UiKit.RoundedSprite;
            _avatarImg.type   = Image.Type.Sliced;
            _avatarImg.pixelsPerUnitMultiplier = 1.6f;
            _avatarImg.color  = def != null ? def.placeholderColor : AccBlue;
        }

        string letter = def != null && !string.IsNullOrEmpty(def.displayName)
            ? def.displayName.Substring(0, 1).ToUpperInvariant()
            : (!string.IsNullOrEmpty(fallbackName) ? fallbackName.Substring(0, 1).ToUpperInvariant() : "?");
        _avatarInitialTxt.text = hasIcon ? "" : letter;
    }

    /// <summary>
    /// Brawl Stars ray düzeni (ekran görüntülerinden birebir):
    /// - Sol kolon (koleksiyon tarafı): MARKET (sarı, DÜKKAN karşılığı) + BAŞARIMLAR (koyu).
    /// - Alt-sol: GÖREVLER (BS'nin quest slotu).
    /// - Sağ kolon (sosyal taraf): SIRALAMA + YEREL MAÇ (koyu plakalar).
    /// Ayarlar üst-sağdaki ☰ menüde; ÇIKIŞ butonu yok.
    /// Tüm plakalar: koyu füme + solda renkli ikon rozeti + beyaz konturlu yazı.
    /// </summary>
    void BuildLeftRail()
    {
        var size = new Vector2(238, 70);

        // Sol kolon
        MakeBrawlBtn(_mainPanel, "btn_wardrobe", Loc.T("WARDROBE"), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(16, 204), size, 17, PlateDark, PlateEdge, AccPurple, "W",
            () => { Click(); WardrobePanelUI.Instance?.Show(); });
        MakeBrawlBtn(_mainPanel, "btn_shop", Loc.T("SHOP"), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(16, 118), size, 19, BrawlYellow, YellowEdge, AccGold, "$",
            () => { Click(); ShopPanelUI.Instance?.Show(); });
        MakeBrawlBtn(_mainPanel, "btn_social", Loc.T("SOCIAL"), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(16, 32), size, 17, PlateDark, PlateEdge, AccCyan, "S",
            () => { Click(); SocialPanelUI.Instance?.Show(); });

        // Alt-sol: GÖREVLER
        MakeBrawlBtn(_mainPanel, "btn_quests", Loc.T("QUESTS"), new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(16, 22), new Vector2(238, 74), 18, PlateDark, PlateEdge, AccGreen, "Q",
            () => { Click(); QuestsPanelUI.Instance?.Show(); });

        // Sağ tarafta kalıcı buton yok — ikincil her şey ☰ çekmecesinde (BS kalıbı).
    }

    // ── ☰ Çekmece (Brawl Stars'ın sağdan açılan menüsü) ──────────────────────
    GameObject    _drawerRoot;
    RectTransform _drawerCol;
    Coroutine     _drawerAnim;

    /// <summary>
    /// Sağdan kayarak açılan ikincil menü: AYARLAR / SIRALAMA / YEREL MAÇ / HESAP.
    /// ☰ butonu açar-kapar; dışına tıklamak kapatır (BS ekran görüntüsündeki çekmecenin karşılığı).
    /// </summary>
    void BuildDrawer()
    {
        _drawerRoot = new GameObject("Drawer");
        _drawerRoot.transform.SetParent(_mainPanel.transform, false);
        var rootRt = _drawerRoot.AddComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero; rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = rootRt.offsetMax = Vector2.zero;

        // Dışına tıklayınca kapat — hafif karartma
        var dimGO = new GameObject("Dim");
        dimGO.transform.SetParent(_drawerRoot.transform, false);
        var dimImg = dimGO.AddComponent<Image>();
        dimImg.color = new Color(0f, 0f, 0f, 0.35f);
        var dimRt = dimImg.rectTransform;
        dimRt.anchorMin = Vector2.zero; dimRt.anchorMax = Vector2.one;
        dimRt.offsetMin = dimRt.offsetMax = Vector2.zero;
        var dimBtn = dimGO.AddComponent<Button>();
        dimBtn.targetGraphic = dimImg;
        dimBtn.transition = Selectable.Transition.None;
        dimBtn.onClick.AddListener(() => SetDrawer(false));

        // Sağ kolon
        var colGO = new GameObject("Column");
        colGO.transform.SetParent(_drawerRoot.transform, false);
        _drawerCol = colGO.AddComponent<RectTransform>();
        _drawerCol.anchorMin = _drawerCol.anchorMax = new Vector2(1f, 0.5f);
        _drawerCol.pivot = new Vector2(1f, 0.5f);
        _drawerCol.sizeDelta = new Vector2(300, 4 * 82 + 16);
        _drawerCol.anchoredPosition = new Vector2(0, 40);

        var size = new Vector2(280, 70);
        int itemCount = 0;
        void Item(string nm, string label, Color icon, string letter, UnityEngine.Events.UnityAction act)
        {
            int i = itemCount++;
            MakeBrawlBtn(colGO, nm, label, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-10, -8 - i * 82), size, 17, PlateDark, PlateEdge, icon, letter,
                () => { Click(); SetDrawer(false); act(); });
        }
        Item("dw_settings",     Loc.T("SETTINGS"),     new Color(0.55f, 0.58f, 0.66f, 1f), "S",
            () => OpenSettingsPanel());
        Item("dw_leaderboard",  Loc.T("LEADERBOARD"),  AccCyan, "L",
            () => LeaderboardPanelUI.Instance?.Show());
        Item("dw_achievements", Loc.T("ACHIEVEMENTS"), AccPurple, "A",
            () => AchievementsPanelUI.Instance?.Show());
        Item("dw_account",      Loc.T("ACCOUNT"),      AccGreen, "@",
            () => OpenSettingsPanel(_accountTab));
        Item("dw_training",     Loc.T("TRAINING"),     AccGold, "T",
            StartTrainingMatch);
        Item("dw_party",        Loc.T("PARTY"),         new Color(0.95f, 0.45f, 0.65f, 1f), "P",
            () => PartyLobbyPanelUI.Instance?.ShowModeSelect());
        // Yerel/bot maçı menüden kaldırılmıştı (arkadaş davetli özel lobi yerini almıştı),
        // ardından test amaçlı geri eklendi — hot-seat kontrol edilebilir botlarla gerçek
        // build'lerde de test yapılabilsin diye Editor kısıtı kaldırıldı.
        Item("dw_botmatch",     Loc.T("BOT MATCH (DEV)"), AccBlue, "B",
            () => LobbyPanelUI.Instance?.Show());
        _drawerCol.sizeDelta = new Vector2(300, itemCount * 82 + 16);

        _drawerRoot.SetActive(false);
    }

    /// <summary>
    /// Antrenman modu: 2 pasif bot (asla hareket/ateş etmez — sadece nişan tahtası; bkz.
    /// LobbyData.IsTraining + GameInitializer + TurnManager.isTrainingMode) ile doğrudan
    /// Game sahnesini açar. Lobi ekranı yok — pratik amaçlı, tek tıkla başlar.
    /// </summary>
    void StartTrainingMatch()
    {
        LobbyData.IsTraining  = true;
        LobbyData.BotCount    = 2;
        LobbyData.MapName     = "CosmicArena";
        LobbyData.SelectedMode = CosmicRumble.Data.GameModeType.Duel1v1;

        GameConfig.Instance?.Save();

        if (SceneFader.Instance != null) SceneFader.Instance.FadeToScene(SceneNames.Game);
        else                             SceneManager.LoadScene(SceneNames.Game);
    }

    void SetDrawer(bool open)
    {
        if (_drawerRoot == null) return;
        if (open == _drawerRoot.activeSelf) { if (!open) return; }
        if (_drawerAnim != null) StopCoroutine(_drawerAnim);
        if (open)
        {
            _drawerRoot.SetActive(true);
            _drawerAnim = StartCoroutine(SlideDrawer(320f, 0f, deactivateAfter: false));
        }
        else if (_drawerRoot.activeSelf)
        {
            _drawerAnim = StartCoroutine(SlideDrawer(0f, 320f, deactivateAfter: true));
        }
    }

    System.Collections.IEnumerator SlideDrawer(float fromX, float toX, bool deactivateAfter)
    {
        const float dur = 0.14f;
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / dur);
            k = 1f - (1f - k) * (1f - k); // ease-out
            _drawerCol.anchoredPosition = new Vector2(Mathf.Lerp(fromX, toX, k), _drawerCol.anchoredPosition.y);
            yield return null;
        }
        _drawerCol.anchoredPosition = new Vector2(toX, _drawerCol.anchoredPosition.y);
        if (deactivateAfter) _drawerRoot.SetActive(false);
        _drawerAnim = null;
    }

    /// <summary>
    /// Alt bölge, Brawl Stars düzeni: alt-orta = mod/harita plakası (koyu, bilgi),
    /// alt-sağ = BÜYÜK SARI OYNA (beyaz konturlu yazı + içinde küçük alt satır).
    /// </summary>
    void BuildPlayCluster()
    {
        // ── Alt-orta: mod plakası (BS'nin "SAVAŞ AŞÇISI / Kuantum Mutfak" kutusu) ──
        var mode = MakePlate(_mainPanel, "ModePlate", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0, 22), new Vector2(430, 66),
            () => { Click(); OnlineLobbyPanelUI.Instance?.Show(); });
        var modeTitle = MakeTxt(mode, "Title", Loc.T("QUICK MATCH"), 19, FontStyles.Normal, Color.white,
            TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(400, 26), new Vector2(0, 11));
        UiKit.BrawlText(modeTitle);
        modeTitle.raycastTarget = false;
        var modeSub = MakeTxt(mode, "Sub", Loc.T("Ranked  •  Win +30 Trophies"), 12, FontStyles.Normal,
            AccGold, TextAlignmentOptions.Center,
            new Vector2(0.5f, 0.5f), new Vector2(400, 18), new Vector2(0, -16));
        modeSub.raycastTarget = false;

        // ── Alt-sağ: BÜYÜK SARI OYNA ─────────────────────────────────────
        var go = new GameObject("btn_play_big");
        go.transform.SetParent(_mainPanel.transform, false);
        var edgeImg = go.AddComponent<Image>();
        edgeImg.color = YellowEdge;
        UiKit.Round(edgeImg);
        UiKit.Skew(edgeImg, 0.07f);
        UiKit.Shadow(go, 6f, 0.50f);
        var rt = edgeImg.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0f);
        rt.sizeDelta = new Vector2(420, 128);
        rt.anchoredPosition = new Vector2(-24, 22);

        var faceGO = new GameObject("Face");
        faceGO.transform.SetParent(go.transform, false);
        var faceImg = faceGO.AddComponent<Image>();
        faceImg.color = Color.white;
        UiKit.Round(faceImg);
        UiKit.Skew(faceImg, 0.07f);
        UiKit.Gradient(faceImg, new Color(1.00f, 0.86f, 0.22f, 1f), new Color(0.96f, 0.70f, 0.05f, 1f));
        var frt = faceImg.rectTransform;
        frt.anchorMin = Vector2.zero;
        frt.anchorMax = Vector2.one;
        frt.offsetMin = new Vector2(0, 7);
        frt.offsetMax = Vector2.zero;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = faceImg;
        btn.colors = UiKit.ButtonColors(Color.white);
        // Mod plakasından farklı olarak burada paneli açmakla yetinmiyoruz — hızlı eşleşmeyi
        // direkt başlatıyoruz (oyuncu ortadaki plakadan modu inceleyebilir, ama OYNA'ya basınca
        // ekstra bir "OYNA" tıklaması daha istemiyoruz).
        btn.onClick.AddListener(() => { Click(); OnlineLobbyPanelUI.Instance?.ShowAndStartQuickMatch(); });
        UiKit.Pulse(go); // tek birincil eylem: sürekli çok hafif nefes (Press ile çakışmasın diye Press yok)
        UiKit.Hover(go);

        var mainLbl = MakeTxt(faceGO, "Label", Loc.T("PLAY"), 50, FontStyles.Normal, Color.white,
            TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(380, 60), new Vector2(0, 10));
        UiKit.BrawlText(mainLbl);
        mainLbl.raycastTarget = false;
        var subLbl = MakeTxt(faceGO, "Sub", Loc.T("Quick Match  •  Ranked"), 14, FontStyles.Normal,
            new Color(0.42f, 0.27f, 0.02f, 1f),
            TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(380, 20), new Vector2(0, -34));
        subLbl.raycastTarget = false;
    }

    /// <summary>
    /// Brawl Stars ray butonu: koyu (veya sarı) eğik plaka + koyu alt kenar + solda renkli
    /// ikon rozeti + beyaz konturlu yazı.
    /// </summary>
    void MakeBrawlBtn(GameObject parent, string name, string label,
                      Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, int fontSize,
                      Color plateColor, Color edgeColor, Color iconColor, string iconLetter,
                      UnityEngine.Events.UnityAction callback)
    {
        // Kök = alt kenar (alt 5px görünür)
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var edgeImg = go.AddComponent<Image>();
        edgeImg.color = edgeColor;
        UiKit.Round(edgeImg, 1.2f);
        UiKit.Skew(edgeImg, 0.07f);
        UiKit.Shadow(go, 4f, 0.5f);
        var rt = edgeImg.rectTransform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;

        // Yüz plakası
        var faceGO = new GameObject("Face");
        faceGO.transform.SetParent(go.transform, false);
        var faceImg = faceGO.AddComponent<Image>();
        faceImg.color = plateColor;
        UiKit.Round(faceImg, 1.2f);
        UiKit.Skew(faceImg, 0.07f);
        var frt = faceImg.rectTransform;
        frt.anchorMin = Vector2.zero;
        frt.anchorMax = Vector2.one;
        frt.offsetMin = new Vector2(0, 5);
        frt.offsetMax = Vector2.zero;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = faceImg;
        btn.colors = UiKit.ButtonColors(plateColor);
        btn.onClick.AddListener(callback);
        UiKit.Press(go);
        UiKit.Hover(go);

        // İkon rozeti
        if (!string.IsNullOrEmpty(iconLetter))
            MakeIconCircle(faceGO, iconColor, iconLetter, new Vector2(30, 0), 38f);

        var txtGO = new GameObject("Label");
        txtGO.transform.SetParent(faceGO.transform, false);
        var txt = txtGO.AddComponent<TextMeshProUGUI>();
        txt.text      = label;
        txt.fontSize  = fontSize;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color     = TextPrimary;
        UiKit.BrawlText(txt);
        txt.overflowMode = TextOverflowModes.Ellipsis;
        var trt = txt.rectTransform;
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(string.IsNullOrEmpty(iconLetter) ? 8 : 54, 0);
        trt.offsetMax = new Vector2(-8, 0);
    }

    // ────────────────────────────────────────────────────────────────────────
    //  SETTINGS PANEL
    // ────────────────────────────────────────────────────────────────────────

    // BS ayarlar ekranı paleti (2. referans görüntü): parlak mavi zemin + mavi plakalar
    static readonly Color SettingsBg   = new Color(0.13f, 0.42f, 0.90f, 1f);
    static readonly Color SettingsBtn  = new Color(0.16f, 0.32f, 0.72f, 1f);

    void BuildSettingsPanel()
    {
        // Tam ekran parlak mavi zemin (Brawl Stars ayarlar ekranı gibi) + soluk desen
        var backdropGO  = new GameObject("SettingsCard");
        backdropGO.transform.SetParent(_settingsPanel.transform, false);
        var backdropImg = backdropGO.AddComponent<Image>();
        backdropImg.color = Color.white;
        UiKit.Gradient(backdropImg, SettingsBg, new Color(0.10f, 0.32f, 0.74f, 1f));
        var backdropRt  = backdropImg.rectTransform;
        backdropRt.anchorMin = Vector2.zero;
        backdropRt.anchorMax = Vector2.one;
        backdropRt.offsetMin = backdropRt.offsetMax = Vector2.zero;
        BuildPattern(backdropGO, 26);

        // Header — beyaz konturlu büyük başlık, üst-orta
        var hdr = MakeTxt(_settingsPanel, "hdr_settings", Loc.T("SETTINGS"), 36, FontStyles.Normal, Color.white,
            TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(420, 48), new Vector2(0, -36));
        UiKit.BrawlText(hdr);

        // Sürüm — alt köşe (footer kaldırıldı, buraya taşındı)
        MakeTxt(_settingsPanel, "Version", $"{VERSION}  •  © 2025 CosmicRumble", 12, FontStyles.Normal,
            new Color(1f, 1f, 1f, 0.55f), TextAlignmentOptions.Center,
            new Vector2(0.5f, 0f), new Vector2(360, 20), new Vector2(0, 12));

        // Yasal linkler — tüm sekmelerde görünür (tab içeriği değil, panel geneli footer)
        MakeLinkText(_settingsPanel, "lbl_privacy", Loc.T("Privacy Policy"), new Vector2(-95, 32),
            () => Application.OpenURL(LegalLinks.PrivacyPolicyUrl));
        MakeLinkText(_settingsPanel, "lbl_terms", Loc.T("Terms of Service"), new Vector2(95, 32),
            () => Application.OpenURL(LegalLinks.TermsOfServiceUrl));

        // ── Tab row ──────────────────────────────────────────────────────────
        MakeTabBtn("tab_audio",     Loc.T("AUDIO"),    -168, 170, () => ShowSettingsTab(_audioTab));
        MakeTabBtn("tab_graphics",  Loc.T("GRAPHICS"),  -56, 170, () => ShowSettingsTab(_graphicsTab));
        MakeTabBtn("tab_controls",  Loc.T("CONTROLS"),   56, 170, () => ShowSettingsTab(_controlsTab));
        MakeTabBtn("tab_account",   Loc.T("ACCOUNT"),   168, 170, () => ShowSettingsTab(_accountTab));

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

        // Back/OK butonları — yan yana, sekmelerin dışında, her zaman görünür. BACK hiçbir
        // pending değişikliği uygulamadan kapatır (zaten hiçbir kontrol cfg/dile doğrudan
        // yazmıyor); OK == ApplyPendingSettings, tüm bekleyen değişiklikleri tek seferde uygular.
        MakeSettingsButton(_settingsPanel, "btn_back", Loc.T("BACK"),
            PlateDark, new Vector2(-155, -230), () => { Click(); ShowPanel(_mainPanel); },
            new Vector2(180, 52));
        MakeSettingsButton(_settingsPanel, "btn_ok", Loc.T("OK"),
            AccGreen, new Vector2(155, -230), ApplyPendingSettings,
            new Vector2(180, 52));

        ShowSettingsTab(_audioTab);
    }

    /// <summary>Ayarlar panelini açar ve tüm kontrolleri (slider/toggle/cycler/dil) güncel
    /// GameConfig/LocalizationManager değerinden yeniden tohumlar — önceki bir açılışta BACK
    /// ile bırakılmış yarım değişiklikler burada silinir.</summary>
    void OpenSettingsPanel(GameObject tab = null)
    {
        ShowPanel(_settingsPanel);
        LoadSettingsValues();
        ShowSettingsTab(tab != null ? tab : _audioTab);
    }

    /// <summary>Panel "OK" ile onaylandığında tüm bekleyen ayarları tek seferde uygular.
    /// Dil DEĞİŞİKLİĞİ en sona bırakılır çünkü LocalizationManager.SetLanguage sahneyi yeniden
    /// yüklüyor — ondan önceki adımların (Save/ApplyGraphics/ApplyVolumes) diske/motöre işlenmiş
    /// olması gerekiyor, yoksa reload'da kaybolurlar.</summary>
    void ApplyPendingSettings()
    {
        Click();

        var cfg = GameConfig.Instance;
        if (cfg != null)
        {
            cfg.MasterVolume    = _pendingMaster;
            cfg.MusicVolume     = _pendingMusic;
            cfg.SfxVolume       = _pendingSfx;
            cfg.Fullscreen      = _pendingFullscreen;
            cfg.ResolutionIndex = _pendingResolutionIndex;
            cfg.QualityLevel    = _pendingQualityIndex;
            cfg.VSync           = _pendingVSync;
            cfg.MoveLeftKey     = _pendingMoveLeftKey;
            cfg.MoveRightKey    = _pendingMoveRightKey;
            cfg.JumpKey         = _pendingJumpKey;
            cfg.Save();
            cfg.ApplyGraphics();
            Screen.fullScreen = cfg.Fullscreen;
        }
        AudioManager.Instance?.ApplyVolumes();
        RefreshControlsLabels();

        if (LocalizationManager.Instance != null && _pendingLanguage != LocalizationManager.Instance.CurrentLanguage)
        {
            LocalizationManager.Instance.SetLanguage(_pendingLanguage); // sahne reload — buradan sonrası çalışmaz
            return;
        }

        ShowPanel(_mainPanel);
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
        _masterSlider = MakeSliderRow(parent, Loc.T("Master Volume"), 100);
        _musicSlider  = MakeSliderRow(parent, Loc.T("Music"),    40);
        _sfxSlider    = MakeSliderRow(parent, Loc.T("Effects"), -20);

        MakeSeparator(parent, new Vector2(0, -60));

        MakeTxt(parent, "lbl_fs", Loc.T("Fullscreen"), 18, FontStyles.Normal, TextPrimary,
            TextAlignmentOptions.Right, new Vector2(0.5f, 0.5f), new Vector2(200, 28), new Vector2(-70, -95));
        _fsToggle = MakeToggle(parent, -95);

        // OK'a basılana kadar sadece pending alanları güncellenir — ne cfg ne Screen ne AudioManager
        // burada dokunulur (bkz. ApplyPendingSettings).
        _masterSlider.onValueChanged.AddListener(v => _pendingMaster = v);
        _musicSlider.onValueChanged.AddListener(v  => _pendingMusic  = v);
        _sfxSlider.onValueChanged.AddListener(v    => _pendingSfx    = v);
        _fsToggle.onValueChanged.AddListener(v     => _pendingFullscreen = v);
    }

    void BuildGraphicsTab(GameObject parent)
    {
        MakeTxt(parent, "lbl_res", Loc.T("Resolution"), 17, FontStyles.Normal, TextPrimary,
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
            idx => _pendingResolutionIndex = idx);

        MakeTxt(parent, "lbl_quality", Loc.T("Quality"), 17, FontStyles.Normal, TextPrimary,
            TextAlignmentOptions.Left, new Vector2(0.5f, 0.5f), new Vector2(160, 26), new Vector2(-190, 40));

        var qualityOptions = QualitySettings.names;
        int startQuality = cfg != null
            ? Mathf.Clamp(cfg.QualityLevel, 0, Mathf.Max(0, qualityOptions.Length - 1))
            : QualitySettings.GetQualityLevel();
        _qualityCycler = MakeCycler(parent, "quality", 40, qualityOptions, startQuality,
            idx => _pendingQualityIndex = idx);

        MakeTxt(parent, "lbl_vsync", "VSync", 18, FontStyles.Normal, TextPrimary,
            TextAlignmentOptions.Right, new Vector2(0.5f, 0.5f), new Vector2(200, 28), new Vector2(-70, -20));
        _vsyncToggle = MakeToggle(parent, -20);
        _vsyncToggle.isOn = cfg == null || cfg.VSync;
        _vsyncToggle.onValueChanged.AddListener(v => _pendingVSync = v);

        // Ayrı "APPLY" kaldırıldı — panel altındaki tek "OK" butonu (ApplyPendingSettings)
        // Resolution/Quality/VSync dahil tüm sekmelerin bekleyen değişikliklerini uygular.
    }

    void BuildControlsTab(GameObject parent)
    {
        MakeTxt(parent, "lbl_move_left", Loc.T("Move Left"), 17, FontStyles.Normal, TextPrimary,
            TextAlignmentOptions.Right, new Vector2(0.5f, 0.5f), new Vector2(180, 26), new Vector2(-115, 100));
        _btnMoveLeftLabel = MakeRebindBtn(parent, "btn_move_left", new Vector2(105, 100), () => BeginRebind("MoveLeft"));

        MakeTxt(parent, "lbl_move_right", Loc.T("Move Right"), 17, FontStyles.Normal, TextPrimary,
            TextAlignmentOptions.Right, new Vector2(0.5f, 0.5f), new Vector2(180, 26), new Vector2(-115, 30));
        _btnMoveRightLabel = MakeRebindBtn(parent, "btn_move_right", new Vector2(105, 30), () => BeginRebind("MoveRight"));

        MakeTxt(parent, "lbl_jump", Loc.T("Jump"), 17, FontStyles.Normal, TextPrimary,
            TextAlignmentOptions.Right, new Vector2(0.5f, 0.5f), new Vector2(180, 26), new Vector2(-115, -40));
        _btnJumpLabel = MakeRebindBtn(parent, "btn_jump", new Vector2(105, -40), () => BeginRebind("Jump"));

        MakeTxt(parent, "lbl_hint", Loc.T("Tap a button, then press a new key (Esc to cancel)"), 13, FontStyles.Italic, TextDim,
            TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(400, 24), new Vector2(0, -110));

        RefreshControlsLabels();
    }

    void BuildAccountTab(GameObject parent)
    {
        _accountStatusText = MakeTxt(parent, "lbl_account_status", "—", 18, FontStyles.Bold, TextPrimary,
            TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(520, 50), new Vector2(0, 130));

        // Bağlı sağlayıcılar listesi — Platform + Cosmic ID modeli
#if UNITY_ANDROID
        (_googleStatusText, _googleLinkBtn) = MakeProviderRow(parent, "row_google", "GOOGLE",
            new Vector2(0, 60), OnGoogleLinkClicked);
#endif
        (_cosmicStatusText, _cosmicLinkBtn) = MakeProviderRow(parent, "row_cosmic", "COSMIC ID",
            new Vector2(0, -14), OnCosmicLinkClicked);

        _logoutBtn = MakeSettingsButton(parent, "btn_logout", Loc.T("LOG OUT"), AccRed,
            new Vector2(0, -110), OnLogoutClicked);

        BuildLanguageRow(parent);

        RefreshAccountTab();
    }

    static readonly Language[] AllLanguages = (Language[])System.Enum.GetValues(typeof(Language));

    /// <summary>Dil seçici — Kaynak/Kalite cycler'ıyla aynı ok-ile-gez kalıbı. Sadece _pendingLanguage'ı
    /// günceller; gerçek LocalizationManager.SetLanguage (sahne reload'u) panel "OK" ile onaylanana
    /// kadar tetiklenmez (bkz. ApplyPendingSettings) — aksi halde tek tıkla anında sahneyi yeniden
    /// yükleyip ayarlar panelinden dışarı atardı.</summary>
    void BuildLanguageRow(GameObject parent)
    {
        MakeTxt(parent, "lbl_lang", Loc.T("Language"), 17, FontStyles.Normal, TextPrimary,
            TextAlignmentOptions.Left, new Vector2(0.5f, 0.5f), new Vector2(160, 26), new Vector2(-190, -180));

        var langOptions = new string[AllLanguages.Length];
        for (int i = 0; i < AllLanguages.Length; i++)
            langOptions[i] = LocalizationManager.DisplayName(AllLanguages[i]);

        int startIdx = 0;
        if (LocalizationManager.Instance != null)
            startIdx = System.Array.IndexOf(AllLanguages, LocalizationManager.Instance.CurrentLanguage);

        _languageCycler = MakeCycler(parent, "lang", -180, langOptions, Mathf.Max(0, startIdx),
            idx => _pendingLanguage = AllLanguages[idx]);
    }

    // ────────────────────────────────────────────────────────────────────────
    //  CONTROLS TAB — rebinding
    // ────────────────────────────────────────────────────────────────────────

    void BeginRebind(string action)
    {
        _awaitingRebindFor = action;
        RefreshControlsLabels();
    }

    /// <summary>Etiketler pending tuş alanlarını gösterir (cfg'yi değil) — OK'a basılana kadar
    /// gerçek GameConfig tuşları değişmez, ama kullanıcı seçtiği tuşu burada görebilir.</summary>
    void RefreshControlsLabels()
    {
        string waiting = Loc.T("Press a key…");
        if (_btnMoveLeftLabel)  _btnMoveLeftLabel.text  = _awaitingRebindFor == "MoveLeft"  ? waiting : _pendingMoveLeftKey.ToString();
        if (_btnMoveRightLabel) _btnMoveRightLabel.text = _awaitingRebindFor == "MoveRight" ? waiting : _pendingMoveRightKey.ToString();
        if (_btnJumpLabel)      _btnJumpLabel.text       = _awaitingRebindFor == "Jump"      ? waiting : _pendingJumpKey.ToString();
    }

    void Update()
    {
        if (_awaitingRebindFor == null) return;
        if (!TryGetAnyKeyDown(out var pressed)) return;

        if (pressed != KeyCode.Escape)
        {
            switch (_awaitingRebindFor)
            {
                case "MoveLeft":  _pendingMoveLeftKey  = pressed; break;
                case "MoveRight": _pendingMoveRightKey = pressed; break;
                case "Jump":      _pendingJumpKey       = pressed; break;
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

    /// <summary>Sağlayıcı satırı: solda ad, ortada bağlılık durumu, sağda BAĞLA butonu.</summary>
    (TextMeshProUGUI status, GameObject linkBtn) MakeProviderRow(GameObject parent, string name,
        string providerLabel, Vector2 pos, UnityEngine.Events.UnityAction onLink)
    {
        var row = new GameObject(name);
        row.transform.SetParent(parent.transform, false);
        var img = row.AddComponent<Image>();
        img.color = BgCardDark;
        UiKit.Round(img, 1.2f);
        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(560, 64);
        rt.anchoredPosition = pos;

        var nameTxt = MakeTxt(row, "Provider", providerLabel, 17, FontStyles.Bold, TextPrimary,
            TextAlignmentOptions.Left, new Vector2(0f, 0.5f), new Vector2(170, 28), new Vector2(110, 0));
        UiKit.BrawlText(nameTxt);

        var statusTxt = MakeTxt(row, "Status", "", 14, FontStyles.Normal, TextDim,
            TextAlignmentOptions.Left, new Vector2(0f, 0.5f), new Vector2(240, 40), new Vector2(320, 0));

        var btnGO = MakeSettingsButton(row, "btn_link", Loc.T("LINK"), AccBlue, Vector2.zero, onLink,
            new Vector2(130, 46));
        var btnRt = btnGO.GetComponent<RectTransform>();
        btnRt.anchorMin = btnRt.anchorMax = new Vector2(1f, 0.5f);
        btnRt.pivot = new Vector2(1f, 0.5f);
        btnRt.anchoredPosition = new Vector2(-12, 0);

        return (statusTxt, btnGO);
    }

    GameObject MakeSettingsButton(GameObject parent, string name, string label, Color color,
        Vector2 pos, UnityEngine.Events.UnityAction onClick, Vector2? size = null)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        UiKit.Round(img);
        UiKit.Shadow(go, 3f, 0.35f);

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.colors = UiKit.ButtonColors(color);
        btn.onClick.AddListener(onClick);
        UiKit.Press(go);
        UiKit.Hover(go);

        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size ?? new Vector2(BTN_W, BTN_H);
        rt.anchoredPosition = pos;

        var txtGO = new GameObject("Label");
        txtGO.transform.SetParent(go.transform, false);
        var txt = txtGO.AddComponent<TextMeshProUGUI>();
        txt.text      = label;
        txt.fontSize  = 17;
        txt.fontStyle = FontStyles.Bold;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color     = TextPrimary;
        var trt = txt.rectTransform;
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;

        return go;
    }

    async void OnGoogleLinkClicked()
    {
        Click();
        if (AuthManager.Instance == null) return;
        if (_googleStatusText != null) _googleStatusText.text = Loc.T("Connecting...");

        var (ok, error) = await AuthManager.Instance.SignInWithPlatformAsync(
            CosmicRumble.Auth.GooglePlayAuthProvider.Shared, silent: false);

        if (!ok && _googleStatusText != null)
            _googleStatusText.text = error ?? Loc.T("Connection failed.");
        RefreshAccountTab();
        // Not: hesap değişimi gerektiyse (AccountAlreadyLinked) sahne zaten yeniden yüklenir.
    }

    void OnCosmicLinkClicked()
    {
        Click();
        ShowPanel(_mainPanel);
        LoginPanelUI.Instance?.Show(dismissable: true);
    }

    void OnLogoutClicked()
    {
        Click();
        AuthManager.Instance?.Logout(); // sahneyi yeniden yükler → giriş ekranı yeniden görünür
    }

    void RefreshAccountTab()
    {
        var auth = AuthManager.Instance;
        bool named = auth != null && auth.HasNamedAccount;

        if (_accountStatusText != null)
        {
            _accountStatusText.text = named
                ? string.Format(Loc.T("Account: {0}"), auth.CurrentUsername)
                : string.Format(Loc.T("{0} — no account linked.\nLink an account to protect your progress."), PlayerIdentity.Get());
        }

        bool googleLinked = auth != null && auth.HasProvider(AuthManager.ProviderGooglePlayGames);
        bool cosmicLinked = auth != null && auth.HasProvider(AuthManager.ProviderUsernamePassword);

        if (_googleStatusText != null)
        {
            _googleStatusText.text = googleLinked ? string.Format(Loc.T("Linked ({0})"), auth.CurrentUsername) : Loc.T("Not linked");
            _googleLinkBtn?.SetActive(!googleLinked);
        }
        if (_cosmicStatusText != null)
        {
            _cosmicStatusText.text = cosmicLinked ? string.Format(Loc.T("Linked ({0})"), auth.CurrentUsername) : Loc.T("Not linked");
            _cosmicLinkBtn?.SetActive(!cosmicLinked);
        }

        _logoutBtn?.SetActive(named);
    }

    // ────────────────────────────────────────────────────────────────────────
    //  SMALL BUILDERS SHARED BY THE SETTINGS TABS
    // ────────────────────────────────────────────────────────────────────────

    void MakeTabBtn(string name, string label, float xOffset, float yOffset, UnityEngine.Events.UnityAction cb)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(_settingsPanel.transform, false);
        var img = go.AddComponent<Image>();
        img.color = SettingsBtn;
        UiKit.Round(img, 1.6f);
        UiKit.Skew(img, 0.06f);
        UiKit.Shadow(go, 3f, 0.35f);
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.colors = UiKit.ButtonColors(SettingsBtn);
        btn.onClick.AddListener(() => { Click(); cb(); });
        UiKit.Press(go);
        UiKit.Hover(go);

        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(110, 42);
        rt.anchoredPosition = new Vector2(xOffset, yOffset);

        var txtGO = new GameObject("Label");
        txtGO.transform.SetParent(go.transform, false);
        var txt = txtGO.AddComponent<TextMeshProUGUI>();
        txt.text      = label;
        txt.fontSize  = 13;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color     = TextPrimary;
        UiKit.BrawlText(txt);
        txt.overflowMode = TextOverflowModes.Ellipsis;
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

        // "<" ">" — ◀▶ glifleri ne Titan One'da ne LiberationSans'ta var (□ görünüyordu)
        MakeArrowBtn(parent, name + "_Prev", "<", new Vector2(-95, yOffset), () => cycler.Step(-1));
        MakeArrowBtn(parent, name + "_Next", ">", new Vector2(135, yOffset), () => cycler.Step(1));

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
        UiKit.Hover(go);
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
        UiKit.Hover(go);
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
        // Brawl Stars lobisi gibi: sol mavi, sağ pembe/kırmızı sahne ışığı
        AddGlow(parent, new Color(0.12f, 0.30f, 0.85f, 0.40f), new Vector2(0.12f, 0.55f), new Vector2(1500, 1300));
        AddGlow(parent, new Color(0.95f, 0.18f, 0.45f, 0.34f), new Vector2(0.90f, 0.50f), new Vector2(1400, 1250));
        AddGlow(parent, new Color(0.55f, 0.20f, 0.85f, 0.30f), new Vector2(0.50f, 0.10f), new Vector2(1200, 700));
    }

    /// <summary>Zeminde soluk desen dokusu (Brawl Stars'ın kurukafa deseni karşılığı) —
    /// düşük alfa, döndürülmüş yuvarlatık karolar.</summary>
    void BuildPattern(GameObject parent, int count)
    {
        var root = new GameObject("Pattern");
        root.transform.SetParent(parent.transform, false);
        root.transform.SetSiblingIndex(3); // glow'ların üstünde, UI'ın altında
        var rootRt = root.AddComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero; rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = rootRt.offsetMax = Vector2.zero;

        var rng = new System.Random(4242);
        for (int i = 0; i < count; i++)
        {
            var go = new GameObject($"P_{i}");
            go.transform.SetParent(root.transform, false);
            var img = go.AddComponent<Image>();
            img.sprite = UiKit.RoundedSprite;
            img.color = new Color(1f, 1f, 1f, 0.022f);
            img.raycastTarget = false;
            float size = 40f + (float)rng.NextDouble() * 50f;
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2((float)rng.NextDouble(), (float)rng.NextDouble());
            rt.sizeDelta = new Vector2(size, size);
            rt.localRotation = Quaternion.Euler(0, 0, 45f + (float)rng.NextDouble() * 20f - 10f);
        }
    }

    void AddGlow(GameObject parent, Color color, Vector2 anchor, Vector2 size)
    {
        var go  = new GameObject("Glow");
        go.transform.SetParent(parent.transform, false);
        go.transform.SetSiblingIndex(2);
        var img = go.AddComponent<Image>();
        img.sprite = UiKit.GlowSprite; // radyal solan leke — düz Image dikdörtgen gibi görünüyordu
        img.color = color;
        img.raycastTarget = false;
        var rt  = img.rectTransform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.sizeDelta        = size;
        rt.anchoredPosition = Vector2.zero;
    }

    // ────────────────────────────────────────────────────────────────────────
    //  FOOTER
    // ────────────────────────────────────────────────────────────────────────

    // ════════════════════════════════════════════════════════════════════════
    //  CALLBACKS
    // ════════════════════════════════════════════════════════════════════════

    void ShowPanel(GameObject panel)
    {
        _mainPanel.SetActive(panel == _mainPanel);
        _settingsPanel.SetActive(panel == _settingsPanel);
    }

    /// <summary>Ayarlar paneli her açıldığında (OpenSettingsPanel) çağrılır — tüm kontrolleri VE
    /// pending alanlarını güncel cfg/dil durumundan yeniden tohumlar, böylece önceki bir açılışta
    /// BACK ile terk edilmiş yarım değişiklikler bir sonraki açılışa asla taşınmaz.</summary>
    void LoadSettingsValues()
    {
        var cfg = GameConfig.Instance;
        if (cfg == null) return;

        if (_masterSlider) _masterSlider.value = cfg.MasterVolume;
        if (_musicSlider)  _musicSlider.value  = cfg.MusicVolume;
        if (_sfxSlider)    _sfxSlider.value    = cfg.SfxVolume;
        if (_fsToggle)     _fsToggle.isOn      = cfg.Fullscreen;
        if (_vsyncToggle)  _vsyncToggle.isOn   = cfg.VSync;

        // Slider/Toggle setter'ı yalnızca değer gerçekten farklıysa onValueChanged'i tetikler —
        // eşit kaldığı durumları da kapsamak için pending alanları burada ayrıca doğrudan eşitliyoruz.
        _pendingMaster       = cfg.MasterVolume;
        _pendingMusic        = cfg.MusicVolume;
        _pendingSfx          = cfg.SfxVolume;
        _pendingFullscreen   = cfg.Fullscreen;
        _pendingVSync        = cfg.VSync;
        _pendingMoveLeftKey  = cfg.MoveLeftKey;
        _pendingMoveRightKey = cfg.MoveRightKey;
        _pendingJumpKey      = cfg.JumpKey;

        if (_resolutionCycler != null && _resolutionCycler.Options.Length > 0)
            _resolutionCycler.Set(Mathf.Clamp(cfg.ResolutionIndex, 0, _resolutionCycler.Options.Length - 1));
        if (_qualityCycler != null && _qualityCycler.Options.Length > 0)
            _qualityCycler.Set(Mathf.Clamp(cfg.QualityLevel, 0, _qualityCycler.Options.Length - 1));
        if (_languageCycler != null && LocalizationManager.Instance != null)
            _languageCycler.Set(Mathf.Max(0, System.Array.IndexOf(AllLanguages, LocalizationManager.Instance.CurrentLanguage)));

        RefreshControlsLabels();
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
        UiKit.Round(img);
        UiKit.Shadow(go, 4f, 0.40f);

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.colors = UiKit.ButtonColors(normal);
        btn.onClick.AddListener(callback);
        UiKit.Hover(go);

        var rt = img.rectTransform;
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = new Vector2(BTN_W, BTN_H);
        rt.anchoredPosition = new Vector2(0, yOffset);

        var txtGO = new GameObject("Label");
        txtGO.transform.SetParent(go.transform, false);
        var txt = txtGO.AddComponent<TextMeshProUGUI>();
        txt.text      = label;
        txt.fontSize  = 21;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color     = TextPrimary;
        UiKit.BrawlText(txt);
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

    /// <summary>Altı çizili, tıklanabilir küçük metin — gizlilik politikası/kullanım koşulları gibi
    /// tam bir buton plakası hak etmeyen yasal linkler için.</summary>
    TextMeshProUGUI MakeLinkText(GameObject parent, string name, string content, Vector2 pos, UnityEngine.Events.UnityAction onClick)
    {
        var txt = MakeTxt(parent, name, content, 12, FontStyles.Underline,
            new Color(1f, 1f, 1f, 0.65f), TextAlignmentOptions.Center,
            new Vector2(0.5f, 0f), new Vector2(170, 20), pos);

        var btn = txt.gameObject.AddComponent<Button>();
        btn.targetGraphic = txt;
        btn.colors = UiKit.ButtonColors(new Color(1f, 1f, 1f, 0.65f));
        btn.onClick.AddListener(onClick);
        UiKit.Hover(txt.gameObject);
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
        // Etiket rect'i slider'ın sol kenarından (x=-35) önce bitmeli, yoksa son harf altında kalır
        MakeTxt(parent, "lbl_" + label, label, 17, FontStyles.Normal, TextPrimary,
            TextAlignmentOptions.Right, new Vector2(0.5f, 0.5f), new Vector2(160, 26), new Vector2(-130, yOffset));

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
