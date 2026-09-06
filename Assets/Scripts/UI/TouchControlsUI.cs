using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Mobilde hareket ve zıplama için ekran üstü kontroller.
///
/// Nişan alma ve ateş etme zaten dokunmatikte çalışıyordu (AbilityBase, Pointer.current üzerinden
/// mouse ve parmağı tek yoldan okuyor), ama YÜRÜME ve ZIPLAMA yalnızca klavyeden okunuyordu
/// (GravityBody: A/D/Space) — yani gerçek bir telefonda karakter hiç hareket edemiyordu. Bu sınıf
/// o boşluğu kapatır: sol altta ◀ ▶, sağ altta ZIPLA. Basılı tutma desteklenir ve Unity UI her
/// parmağı ayrı bir pointer olarak işlediği için "sola yürürken zıpla" aynı anda çalışır.
///
/// UI, projenin geri kalanındaki desenle kod içinde kurulur (bkz. NetworkBootstrap.BuildStatusUI)
/// — sahneyi elle düzenlemeye gerek kalmaz. Yalnızca Game sahnesinde ve yalnızca dokunmatik
/// platformda oluşur; masaüstünde hiç yaratılmaz, dolayısıyla klavye akışı hiç değişmez.
/// </summary>
public class TouchControlsUI : MonoBehaviour
{
    /// <summary>GravityBody'nin okuduğu yatay girdi: sola basılıysa +1, sağa basılıysa -1
    /// (klavyedeki leftKey/rightKey ile aynı işaret düzeni).</summary>
    public static float Horizontal { get; private set; }

    static bool _jumpQueued;

    /// <summary>Bir kez zıplama isteği okur ve sıfırlar — GetKeyDown'ın dokunmatik karşılığı.</summary>
    public static bool ConsumeJump()
    {
        if (!_jumpQueued) return false;
        _jumpQueued = false;
        return true;
    }

    /// <summary>Editörde/masaüstünde denemek için: true ise dokunmatik olmayan platformda da kurulur.</summary>
    public static bool ForceEnabled;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += (scene, mode) =>
        {
            if (scene.name != SceneNames.Game) return;
            if (!ForceEnabled && !Application.isMobilePlatform) return;
            if (FindFirstObjectByType<TouchControlsUI>() != null) return;

            new GameObject("TouchControlsUI").AddComponent<TouchControlsUI>();
        };
    }

    void Awake()
    {
        // Sahne değişince (maç bitip menüye dönünce) kalıntı bir girdi kalmasın.
        Horizontal  = 0f;
        _jumpQueued = false;
        Build();
    }

    void OnDestroy()
    {
        Horizontal  = 0f;
        _jumpQueued = false;
    }

    void Build()
    {
        var canvasGO = new GameObject("TouchControlsCanvas");
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;          // HUD'un üstünde, NetworkBootstrap banner'ının (100) altında
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // Çentik/gesture bar payı: butonlar ekranın en kenarına yapışmasın.
        var safe = new GameObject("SafeArea", typeof(RectTransform));
        safe.transform.SetParent(canvasGO.transform, false);
        var safeRt = (RectTransform)safe.transform;
        safeRt.anchorMin = Vector2.zero;
        safeRt.anchorMax = Vector2.one;
        safeRt.offsetMin = safeRt.offsetMax = Vector2.zero;
        safe.AddComponent<SafeArea>();

        // Silah tepsisi ekranın altını kaplıyor; yürüme butonları onun ÜSTÜNDE durur.
        const float size = 150f;
        const float margin = 40f;
        const float bottom = 250f;

        MakeHoldButton(safeRt, "MoveLeft",  "◀", new Vector2(0f, 0f), new Vector2( margin + size * 0.5f,       bottom), size, +1f);
        MakeHoldButton(safeRt, "MoveRight", "▶", new Vector2(0f, 0f), new Vector2( margin + size * 1.6f,       bottom), size, -1f);
        MakeJumpButton(safeRt, new Vector2(1f, 0f), new Vector2(-(margin + size * 0.5f), bottom), size);
    }

    RectTransform MakeButtonBase(RectTransform parent, string name, string label, Vector2 anchor,
                                 Vector2 pos, float size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(size, size);
        rt.anchoredPosition = pos;

        var img = go.AddComponent<Image>();
        img.sprite = UiKit.CircleSprite;
        img.color  = color;
        img.raycastTarget = true;

        var textGO = new GameObject("Label", typeof(RectTransform));
        textGO.transform.SetParent(rt, false);
        var txt = textGO.AddComponent<TMPro.TextMeshProUGUI>();
        txt.text      = label;
        txt.fontSize  = size * 0.42f;
        txt.color     = Color.white;
        txt.alignment = TMPro.TextAlignmentOptions.Center;
        txt.raycastTarget = false;
        var trt = txt.rectTransform;
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;

        return rt;
    }

    void MakeHoldButton(RectTransform parent, string name, string label, Vector2 anchor,
                        Vector2 pos, float size, float direction)
    {
        var rt = MakeButtonBase(parent, name, label, anchor, pos, size, new Color(0f, 0f, 0f, 0.42f));

        // Button yerine EventTrigger: yürümek basılı TUTMA gerektiriyor, tek bir tıklama değil.
        var trigger = rt.gameObject.AddComponent<EventTrigger>();
        AddEntry(trigger, EventTriggerType.PointerDown, _ => Horizontal = direction);
        AddEntry(trigger, EventTriggerType.PointerUp,   _ => { if (Mathf.Approximately(Horizontal, direction)) Horizontal = 0f; });
        // Parmak butondan kayarsa da yürüme durmalı, aksi halde karakter sonsuza kadar yürür.
        AddEntry(trigger, EventTriggerType.PointerExit, _ => { if (Mathf.Approximately(Horizontal, direction)) Horizontal = 0f; });
    }

    void MakeJumpButton(RectTransform parent, Vector2 anchor, Vector2 pos, float size)
    {
        var rt = MakeButtonBase(parent, "Jump", "▲", anchor, pos, size * 1.15f, new Color(0.15f, 0.55f, 0.95f, 0.55f));
        var trigger = rt.gameObject.AddComponent<EventTrigger>();
        AddEntry(trigger, EventTriggerType.PointerDown, _ => _jumpQueued = true);
    }

    static void AddEntry(EventTrigger trigger, EventTriggerType type, UnityEngine.Events.UnityAction<BaseEventData> action)
    {
        var entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener(action);
        trigger.triggers.Add(entry);
    }
}
