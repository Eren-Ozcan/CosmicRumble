using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Tam ekran yükleme perdesi. Açılışta (BootstrapSequence) sessiz giriş + bulut senkronu
/// sırasında gösterilir; menü kurulunca gizlenir. Sahne ömürlü (DontDestroyOnLoad değil) —
/// her menü dönüşünde MainMenuUI tarafından yeniden yaratılır.
/// Canvas sıralaması: Menu 10, Social 40, OnlineLobby 45, FriendLobby 46, InvitePopup 80,
/// LoginScreen 85, Loading 90, LoginPanel kartı 95, NetworkBanner 100.
/// </summary>
public class LoadingScreenUI : MonoBehaviour
{
    public static LoadingScreenUI Instance { get; private set; }

    static readonly Color GradTop    = new Color(0.17f, 0.10f, 0.40f, 1f);
    static readonly Color GradBottom = new Color(0.34f, 0.10f, 0.33f, 1f);
    static readonly Color AccGold    = new Color(1.00f, 0.80f, 0.20f, 1f);
    static readonly Color TextDim    = new Color(0.65f, 0.70f, 0.82f, 1f);

    GameObject      _root;
    TextMeshProUGUI _statusText;
    string          _baseStatus = "";
    Coroutine       _dotsRoutine;

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

    // ── Public API ───────────────────────────────────────────────────────

    public void Show(string status)
    {
        _root.SetActive(true);
        SetStatus(status);
        if (_dotsRoutine == null) _dotsRoutine = StartCoroutine(AnimateDots());
    }

    public void SetStatus(string status)
    {
        _baseStatus = status ?? "";
        if (_statusText != null) _statusText.text = _baseStatus;
    }

    public void Hide()
    {
        if (_dotsRoutine != null) { StopCoroutine(_dotsRoutine); _dotsRoutine = null; }
        _root.SetActive(false);
    }

    // ── UI Build ─────────────────────────────────────────────────────────

    void BuildUI()
    {
        var canvasGO = new GameObject("LoadingCanvas");
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        _root = new GameObject("LoadingRoot");
        _root.transform.SetParent(canvasGO.transform, false);
        var rootRt = _root.AddComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = rootRt.offsetMax = Vector2.zero;

        // Arka plan gradyanı (menüyle aynı mor sahne)
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
        title.fontSize  = 52;
        title.alignment = TextAlignmentOptions.Center;
        title.color     = AccGold;
        UiKit.BrawlText(title);
        var titleRt = title.rectTransform;
        titleRt.anchorMin = titleRt.anchorMax = new Vector2(0.5f, 0.58f);
        titleRt.sizeDelta = new Vector2(800, 70);
        titleRt.anchoredPosition = Vector2.zero;

        // Durum satırı (noktalar coroutine ile canlanır)
        var statusGO = new GameObject("Status");
        statusGO.transform.SetParent(_root.transform, false);
        _statusText = statusGO.AddComponent<TextMeshProUGUI>();
        _statusText.fontSize  = 20;
        _statusText.alignment = TextAlignmentOptions.Center;
        _statusText.color     = TextDim;
        var statusRt = _statusText.rectTransform;
        statusRt.anchorMin = statusRt.anchorMax = new Vector2(0.5f, 0.40f);
        statusRt.sizeDelta = new Vector2(700, 34);
        statusRt.anchoredPosition = Vector2.zero;
    }

    IEnumerator AnimateDots()
    {
        int dots = 0;
        var wait = new WaitForSeconds(0.4f);
        while (true)
        {
            dots = (dots + 1) % 4;
            // Durum metni "..." ile bitiyorsa taban kısmı sabit tutup nokta sayısını oynat
            string baseText = _baseStatus.TrimEnd('.');
            if (_statusText != null) _statusText.text = baseText + new string('.', dots);
            yield return wait;
        }
    }
}
