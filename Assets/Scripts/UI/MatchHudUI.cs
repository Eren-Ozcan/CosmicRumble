using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using CosmicRumble.Localization;

/// <summary>
/// Maç HUD'ının tasarım kitindeki (16 · In-match HUD) ek parçaları — sahneye elle bir şey
/// eklemeden, Game sahnesi yüklenince runtime'da kurulur:
///   • Üst ortada "YOUR TURN / {isim}" bandı (TurnManager'ın sırasını okur).
///   • Nişan alırken "POWER 68% · 42°" okuması (TrajectoryDots.AimChanged olayını dinler).
///   • Silah tepsisindeki 10 slotun köşesine tasarımdaki slot numarası etiketi.
/// Sahnedeki mevcut HUD (tepsi, tur sayacı, tepsi toggle butonu) olduğu gibi kalır; burada
/// yalnızca eksik olan tasarım öğeleri eklenir, hiçbir buton kopyalanmaz.
/// </summary>
public class MatchHudUI : MonoBehaviour
{
    public static MatchHudUI Instance { get; private set; }

    TextMeshProUGUI _turnLabel;
    TextMeshProUGUI _turnName;
    GameObject      _aimReadout;
    TextMeshProUGUI _aimText;
    RectTransform   _turnPlate;
    Image           _turnPlateImg;

    int   _lastTurnIndex = -99;
    float _nextBannerRefresh;
    bool _slotLabelsDone;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += (scene, mode) =>
        {
            if (scene.name != SceneNames.Game) return;
            if (FindFirstObjectByType<MatchHudUI>() != null) return;
            new GameObject("MatchHudUI").AddComponent<MatchHudUI>();
        };
    }

    void Awake()
    {
        Instance = this;
        Build();
    }

    void OnEnable()
    {
        TrajectoryDots.AimChanged += OnAimChanged;
        TrajectoryDots.AimEnded   += OnAimEnded;
    }

    void OnDisable()
    {
        TrajectoryDots.AimChanged -= OnAimChanged;
        TrajectoryDots.AimEnded   -= OnAimEnded;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ── Kurulum ──────────────────────────────────────────────────────────────
    void Build()
    {
        var canvasGO = new GameObject("MatchHudCanvas");
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 40;                 // tepsinin üstünde, dokunmatik kontrollerin (50) altında
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;
        // Bu katman tamamen bilgilendirici: hiçbir grafiği dokunuşu yakalamamalı, yoksa
        // altındaki silah tepsisi/karakter tıklamaları engellenir. GraphicRaycaster EKLENMEZ.

        var safe = new GameObject("SafeArea", typeof(RectTransform));
        safe.transform.SetParent(canvasGO.transform, false);
        var safeRt = (RectTransform)safe.transform;
        safeRt.anchorMin = Vector2.zero;
        safeRt.anchorMax = Vector2.one;
        safeRt.offsetMin = safeRt.offsetMax = Vector2.zero;
        safe.AddComponent<SafeArea>();

        BuildTurnBanner(safeRt);
        BuildAimReadout(safeRt);
        BuildPauseButton();
    }

    /// <summary>
    /// Tasarımdaki sağ üst duraklat butonu. Kendi canvas'ında durur çünkü bilgilendirme
    /// katmanı bilerek raycast almıyor. Mobilde Escape tuşu yok — bu buton olmadan oyuncunun
    /// maç içinde menüyü açmasının (ve menüye dönmesinin) hiçbir yolu yok.
    /// </summary>
    void BuildPauseButton()
    {
        var canvasGO = new GameObject("MatchHudButtonsCanvas");
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 45;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        var safe = new GameObject("SafeArea", typeof(RectTransform));
        safe.transform.SetParent(canvasGO.transform, false);
        var safeRt = (RectTransform)safe.transform;
        safeRt.anchorMin = Vector2.zero;
        safeRt.anchorMax = Vector2.one;
        safeRt.offsetMin = safeRt.offsetMax = Vector2.zero;
        safe.AddComponent<SafeArea>();

        var go = new GameObject("btn_pause");
        go.transform.SetParent(safe.transform, false);
        var img = go.AddComponent<Image>();
        img.color = UiTheme.Plate;
        UiKit.Round(img, UiTheme.CornerPlate);
        UiKit.Shadow(go, 4f, 0.45f);

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.colors = UiKit.ButtonColors(UiTheme.Plate);
        btn.onClick.AddListener(OnPauseClicked);
        UiKit.Press(go);
        UiKit.Hover(go);

        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot     = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-24f, -24f);
        rt.sizeDelta = new Vector2(88f, 88f);

        var lbl = MakeText(go, "Lbl", "II", 34f, UiTheme.TextPrimary,
                           TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f),
                           Vector2.zero, new Vector2(88f, 88f));
        UiKit.BrawlText(lbl);
    }

    void OnPauseClicked()
    {
        AudioManager.Instance?.PlayClick();
        var menu = FindFirstObjectByType<InGameMenu>(FindObjectsInactive.Include);
        if (menu != null) menu.ToggleMenu();
    }

    void BuildTurnBanner(RectTransform parent)
    {
        var go = new GameObject("TurnBanner", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot     = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -22f);
        rt.sizeDelta = new Vector2(420f, 64f);

        var edge = go.AddComponent<Image>();
        edge.color = UiTheme.PlateEdge;
        edge.raycastTarget = false;
        UiKit.Round(edge, UiTheme.CornerPlate);

        var faceGO = new GameObject("Face", typeof(RectTransform));
        faceGO.transform.SetParent(go.transform, false);
        var face = faceGO.AddComponent<Image>();
        face.color = UiTheme.Plate;
        face.raycastTarget = false;
        UiKit.Round(face, UiTheme.CornerPlate);
        var frt = face.rectTransform;
        frt.anchorMin = Vector2.zero;
        frt.anchorMax = Vector2.one;
        frt.offsetMin = new Vector2(0f, UiTheme.PlateEdgeHeight);
        frt.offsetMax = Vector2.zero;
        _turnPlateImg = face;
        _turnPlate    = rt;

        _turnLabel = MakeText(faceGO, "Label", Loc.T("YOUR TURN"), 20f, UiTheme.Gold,
                              TextAlignmentOptions.Left, new Vector2(0f, 0.5f),
                              new Vector2(22f, 0f), new Vector2(190f, 40f));
        UiKit.BrawlText(_turnLabel);

        _turnName = MakeText(faceGO, "Name", "", 20f, UiTheme.TextPrimary,
                             TextAlignmentOptions.Right, new Vector2(1f, 0.5f),
                             new Vector2(-22f, 0f), new Vector2(210f, 40f));
        UiKit.BrawlText(_turnName);
    }

    void BuildAimReadout(RectTransform parent)
    {
        var go = new GameObject("AimReadout", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot     = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 250f);   // tepsinin hemen üstü
        rt.sizeDelta = new Vector2(340f, 52f);

        var bg = go.AddComponent<Image>();
        bg.color = new Color(UiTheme.BgDeep.r, UiTheme.BgDeep.g, UiTheme.BgDeep.b, 0.82f);
        bg.raycastTarget = false;
        UiKit.Round(bg, UiTheme.CornerChip);

        _aimText = MakeText(go, "Text", "", 22f, UiTheme.TextPrimary,
                            TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f),
                            Vector2.zero, new Vector2(320f, 44f));
        UiKit.BrawlText(_aimText);

        _aimReadout = go;
        _aimReadout.SetActive(false);
    }

    static TextMeshProUGUI MakeText(GameObject parent, string name, string text, float size,
                                    Color color, TextAlignmentOptions align, Vector2 anchor,
                                    Vector2 pos, Vector2 sizeDelta)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent.transform, false);
        var txt = go.AddComponent<TextMeshProUGUI>();
        txt.text          = text;
        txt.fontSize      = size;
        txt.color         = color;
        txt.alignment     = align;
        txt.raycastTarget = false;
        var rt = txt.rectTransform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot     = new Vector2(anchor.x, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = sizeDelta;
        return txt;
    }

    // ── Nişan okuması ────────────────────────────────────────────────────────
    void OnAimChanged(float power01, float angleDeg)
    {
        if (_aimReadout == null) return;
        if (!_aimReadout.activeSelf) _aimReadout.SetActive(true);

        // Açı 0..360 yerine okunabilir -180..180 aralığında gösterilir.
        int deg = Mathf.RoundToInt(Mathf.DeltaAngle(0f, angleDeg));
        _aimText.text  = string.Format(Loc.T("POWER {0}%  ·  {1}°"),
                                       Mathf.RoundToInt(power01 * 100f), deg);
        _aimText.color = Color.Lerp(UiTheme.GreenBright, UiTheme.DangerLight, power01);
    }

    void OnAimEnded()
    {
        if (_aimReadout != null) _aimReadout.SetActive(false);
    }

    // ── Tur bandı ────────────────────────────────────────────────────────────
    void Update()
    {
        RefreshTurnBanner();
        if (!_slotLabelsDone) TryAddSlotIndexLabels();
    }

    void RefreshTurnBanner()
    {
        var tm = TurnManager.Instance;
        if (tm == null || _turnPlate == null) return;

        // Tur sahibi okunur, ateş eden değil: TurnManager.CurrentShooter yalnızca silah
        // onaylanmış/mermi havadayken dolu olduğu için band turun büyük bölümünde "WAITING"de
        // kalırdı. Ad ağ değişkeninden geç gelebildiği için sıra değişmese de yarım saniyede
        // bir tazelenir (her kare string ayırmadan).
        bool turnChanged = tm.CurrentTurnIndex != _lastTurnIndex;
        if (!turnChanged && Time.unscaledTime < _nextBannerRefresh) return;
        _lastTurnIndex     = tm.CurrentTurnIndex;
        _nextBannerRefresh = Time.unscaledTime + 0.5f;

        var shooter = tm.CurrentCharacter;
        if (shooter == null)
        {
            _turnName.text  = "";
            _turnLabel.text = Loc.T("WAITING");
            return;
        }

        bool mine = IsLocalPlayer(shooter);
        _turnLabel.text  = mine ? Loc.T("YOUR TURN") : Loc.T("TURN");
        _turnLabel.color = mine ? UiTheme.Gold : UiTheme.TextMuted;
        _turnName.text   = ShooterName(shooter);
        _turnPlateImg.color = mine ? UiTheme.Plate : UiTheme.Slot;
    }

    static bool IsLocalPlayer(GravityBody shooter)
    {
        var netObj = shooter.GetComponent<Unity.Netcode.NetworkObject>();
        // Offline hotseat'te ağ nesnesi spawn edilmez — sıra her zaman "senin".
        if (netObj == null || !netObj.IsSpawned) return true;
        return netObj.IsOwner;
    }

    static string ShooterName(GravityBody shooter)
    {
        string net = shooter.playerName.Value.ToString();
        if (!string.IsNullOrEmpty(net)) return net;
        return shooter.gameObject.name;
    }

    // ── Tepsi slot numaraları (tasarımdaki 0..9 köşe etiketleri) ────────────
    void TryAddSlotIndexLabels()
    {
        var ui = UIManager.Instance;
        if (ui == null || ui.filterImages == null || ui.filterImages.Length == 0) return;

        for (int i = 0; i < ui.filterImages.Length; i++)
        {
            var filter = ui.filterImages[i];
            if (filter == null) continue;
            var slot = filter.transform.parent != null ? filter.transform.parent : filter.transform;
            if (slot.Find("SlotIndex") != null) continue;

            var go = new GameObject("SlotIndex", typeof(RectTransform));
            go.transform.SetParent(slot, false);
            var txt = go.AddComponent<TextMeshProUGUI>();
            txt.text          = i.ToString();
            txt.fontSize      = 16f;
            txt.color         = UiTheme.TextFaint;
            txt.alignment     = TextAlignmentOptions.TopLeft;
            txt.raycastTarget = false;
            var rt = txt.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot     = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(6f, -4f);
            rt.sizeDelta = new Vector2(24f, 20f);
        }
        _slotLabelsDone = true;
    }
}
