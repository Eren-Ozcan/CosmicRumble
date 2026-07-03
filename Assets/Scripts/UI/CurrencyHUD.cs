using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CosmicRumble.Economy;

/// <summary>
/// Always-visible XP/Gold/Gem readout during a match.
/// Subscribes to CurrencyManager.OnCurrencyChanged and updates live.
/// </summary>
public class CurrencyHUD : MonoBehaviour
{
    static readonly Color BgColor   = new Color(0.05f, 0.05f, 0.13f, 0.85f);
    static readonly Color XpColor   = new Color(0.30f, 0.65f, 1.00f, 1f);
    static readonly Color GoldColor = new Color(1.00f, 0.80f, 0.20f, 1f);
    static readonly Color GemColor  = new Color(0.60f, 0.85f, 1.00f, 1f);

    TextMeshProUGUI _xpText;
    TextMeshProUGUI _goldText;
    TextMeshProUGUI _gemText;

    void Awake()
    {
        BuildUI();
    }

    void Start()
    {
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnCurrencyChanged += HandleCurrencyChanged;
            RefreshAll();
        }
    }

    void OnDestroy()
    {
        if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.OnCurrencyChanged -= HandleCurrencyChanged;
    }

    void RefreshAll()
    {
        var cm = CurrencyManager.Instance;
        if (cm == null) return;
        _xpText.text   = $"XP {cm.Get(CurrencyType.XP)}";
        _goldText.text = $"Gold {cm.Get(CurrencyType.Gold)}";
        _gemText.text  = $"Gem {cm.Get(CurrencyType.Gem)}";
    }

    void HandleCurrencyChanged(CurrencyType type, long newBalance)
    {
        switch (type)
        {
            case CurrencyType.XP:   _xpText.text   = $"XP {newBalance}";   break;
            case CurrencyType.Gold: _goldText.text = $"Gold {newBalance}"; break;
            case CurrencyType.Gem:  _gemText.text  = $"Gem {newBalance}";  break;
        }
    }

    void BuildUI()
    {
        var canvasGO = new GameObject("CurrencyHUDCanvas");
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 300;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        // Match by height, not width: the game is landscape-only and phone width varies far more
        // than height across devices (16:9 to 21:9+) -- matching width would scale this top-left
        // badge up/down with device width instead of keeping it a consistent size relative to the
        // vertical space actually available, same reasoning as the main gameplay Canvas.
        scaler.matchWidthOrHeight  = 1f;

        var safeAreaGO = new GameObject("SafeAreaRoot", typeof(RectTransform));
        safeAreaGO.transform.SetParent(canvasGO.transform, false);
        var safeAreaRt = (RectTransform)safeAreaGO.transform;
        safeAreaRt.anchorMin = Vector2.zero;
        safeAreaRt.anchorMax = Vector2.one;
        safeAreaRt.offsetMin = safeAreaRt.offsetMax = Vector2.zero;
        safeAreaGO.AddComponent<SafeArea>();

        var bgGO = new GameObject("Background");
        bgGO.transform.SetParent(safeAreaGO.transform, false);
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.color = BgColor;
        bgImg.raycastTarget = false;
        var bgRt = bgImg.rectTransform;
        bgRt.anchorMin = new Vector2(0f, 1f);
        bgRt.anchorMax = new Vector2(0f, 1f);
        bgRt.pivot     = new Vector2(0f, 1f);
        bgRt.sizeDelta = new Vector2(260, 40);
        bgRt.anchoredPosition = new Vector2(14, -14);

        _xpText   = MakeEntry(bgGO.transform, 0,   XpColor);
        _goldText = MakeEntry(bgGO.transform, 1,   GoldColor);
        _gemText  = MakeEntry(bgGO.transform, 2,   GemColor);
    }

    TextMeshProUGUI MakeEntry(Transform parent, int slot, Color color)
    {
        var go = new GameObject("Entry_" + slot);
        go.transform.SetParent(parent, false);
        var txt = go.AddComponent<TextMeshProUGUI>();
        txt.fontSize  = 16;
        txt.fontStyle = FontStyles.Bold;
        txt.alignment = TextAlignmentOptions.Left;
        txt.color     = color;
        txt.raycastTarget = false;
        var rt = txt.rectTransform;
        rt.anchorMin = new Vector2(0f, 0.5f);
        rt.anchorMax = new Vector2(0f, 0.5f);
        rt.pivot     = new Vector2(0f, 0.5f);
        rt.sizeDelta = new Vector2(80, 30);
        rt.anchoredPosition = new Vector2(10 + slot * 84, 0);
        return txt;
    }
}
