using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CosmicRumble.Economy;
using CosmicRumble.Localization;

/// <summary>
/// Profil avatarı seçici — Ayarlar > Hesap sekmesindeki "DEĞİŞTİR" butonundan açılır. Tüm
/// avatarlar baştan açık (kostümlerin aksine unlock/rarity yok), gerçek ikon sprite'ı yoksa
/// placeholderColor + baş harf rozeti gösterilir. UiKit/WardrobePanelUI ile aynı grid kalıbı.
/// </summary>
public class AvatarPickerUI : MonoBehaviour
{
    public static AvatarPickerUI Instance { get; private set; }

    static readonly Color CardBg     = new Color(0.07f,  0.07f,  0.16f,  0.97f);
    static readonly Color CellBg     = new Color(0.11f,  0.11f,  0.21f,  1f);
    static readonly Color SelectedGr = new Color(0.30f,  0.85f,  0.40f,  1f);
    static readonly Color TitleGold  = new Color(1f, 0.80f, 0.20f, 1f);
    static readonly Color StrokeCol  = new Color(1f, 1f, 1f, 0.09f);

    GameObject _panelRoot;
    GameObject _contentParent;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        BuildUI();
    }

    void OnEnable()
    {
        if (AvatarManager.Instance != null)
            AvatarManager.Instance.OnAvatarChanged += HandleAvatarChanged;
    }

    void OnDisable()
    {
        if (AvatarManager.Instance != null)
            AvatarManager.Instance.OnAvatarChanged -= HandleAvatarChanged;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void HandleAvatarChanged(AvatarDefinition def)
    {
        if (_panelRoot != null && _panelRoot.activeSelf) Populate();
    }

    public void Show()
    {
        _panelRoot.SetActive(true); // BrawlText/outline materyali için OnEnable önce gerekli
        Populate();
    }

    public void Hide() => _panelRoot.SetActive(false);

    void BuildUI()
    {
        var canvasGO = new GameObject("AvatarPickerCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 96; // LoginPanel kartının (95) üstünde açılabilsin diye biraz yüksek
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        _panelRoot = new GameObject("AvatarPickerPanel");
        _panelRoot.transform.SetParent(canvasGO.transform, false);
        var overlay = _panelRoot.AddComponent<Image>();
        overlay.color = new Color(0, 0, 0, 0.65f);
        StretchFull(overlay.rectTransform);

        var card = MakePanel(_panelRoot, "Card", CardBg, Vector2.zero,
            new Vector2(760, 560), new Vector2(0.5f, 0.5f));
        UiKit.Round(card.GetComponent<Image>());
        UiKit.Shadow(card, 8f, 0.55f);
        UiKit.Stroke(card, StrokeCol);
        UiKit.Pop(card);

        var title = MakeTxt(card, "Title", Loc.T("CHOOSE AVATAR"), 28, TitleGold,
            new Vector2(0.5f, 0.92f), new Vector2(600, 44));
        title.fontStyle = FontStyles.Bold;

        UiKit.CloseButton(card, Hide);

        BuildScrollView(card);

        _panelRoot.AddComponent<EscapeListener>().OnEscape = Hide;
        _panelRoot.SetActive(false);
    }

    void BuildScrollView(GameObject parent)
    {
        var scrollGO = new GameObject("ScrollView");
        scrollGO.transform.SetParent(parent.transform, false);
        var scrollRect = scrollGO.AddComponent<ScrollRect>();
        scrollRect.horizontal        = false;
        scrollRect.vertical          = true;
        scrollRect.scrollSensitivity = 30f;

        var scrollRt = scrollGO.GetComponent<RectTransform>();
        scrollRt.anchorMin = new Vector2(0.5f, 0.5f);
        scrollRt.anchorMax = new Vector2(0.5f, 0.5f);
        scrollRt.sizeDelta        = new Vector2(700, 440);
        scrollRt.anchoredPosition = new Vector2(0, -30);

        var vpGO  = new GameObject("Viewport");
        vpGO.transform.SetParent(scrollGO.transform, false);
        var vpImg = vpGO.AddComponent<Image>();
        vpImg.color = new Color(0, 0, 0, 0.01f);
        vpGO.AddComponent<Mask>().showMaskGraphic = false;
        var vpRt = vpImg.rectTransform;
        vpRt.anchorMin = Vector2.zero;
        vpRt.anchorMax = Vector2.one;
        vpRt.offsetMin = vpRt.offsetMax = Vector2.zero;
        scrollRect.viewport = vpRt;

        var contentGO = new GameObject("Content");
        contentGO.transform.SetParent(vpGO.transform, false);

        var grid = contentGO.AddComponent<GridLayoutGroup>();
        grid.cellSize        = new Vector2(150, 170);
        grid.spacing         = new Vector2(14, 14);
        grid.padding         = new RectOffset(4, 4, 4, 4);
        grid.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 4;
        grid.childAlignment  = TextAnchor.UpperCenter;

        var csf = contentGO.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var cRt = contentGO.GetComponent<RectTransform>();
        cRt.anchorMin = new Vector2(0, 1);
        cRt.anchorMax = new Vector2(1, 1);
        cRt.pivot     = new Vector2(0.5f, 1f);
        cRt.offsetMin = cRt.offsetMax = Vector2.zero;
        scrollRect.content = cRt;

        _contentParent = contentGO;
    }

    void Populate()
    {
        foreach (Transform child in _contentParent.transform)
            Destroy(child.gameObject);

        var mgr = AvatarManager.Instance;
        var db  = Resources.Load<AvatarDatabase>("Economy/AvatarDatabase");
        if (mgr == null || db == null || db.allAvatars.Count == 0) return;

        var selected = mgr.GetSelected();
        foreach (var def in db.allAvatars)
        {
            if (def == null) continue;
            BuildCell(def, selected != null && selected.avatarId == def.avatarId);
        }
    }

    void BuildCell(AvatarDefinition def, bool selected)
    {
        var cell = new GameObject($"Cell_{def.avatarId}");
        cell.transform.SetParent(_contentParent.transform, false);
        var bg = cell.AddComponent<Image>();
        bg.color = CellBg;
        UiKit.Round(bg, 1.6f);
        UiKit.Stroke(cell, selected ? SelectedGr : new Color(1f, 1f, 1f, 0.10f), 1.6f);

        var btn = cell.AddComponent<Button>();
        btn.targetGraphic = bg;
        btn.colors = UiKit.ButtonColors(CellBg);
        string id = def.avatarId;
        btn.onClick.AddListener(() =>
        {
            AudioManager.Instance?.PlayClick();
            AvatarManager.Instance?.Select(id); // OnAvatarChanged -> Populate
        });
        UiKit.Press(cell, 0.96f);

        var iconGO = new GameObject("Icon");
        iconGO.transform.SetParent(cell.transform, false);
        var iconImg = iconGO.AddComponent<Image>();
        iconImg.raycastTarget = false;
        if (def.icon != null)
        {
            iconImg.sprite = def.icon;
            iconImg.preserveAspect = true;
        }
        else
        {
            iconImg.sprite = UiKit.CircleSprite;
            iconImg.color  = def.placeholderColor;
        }
        var iconRt = iconImg.rectTransform;
        iconRt.anchorMin = iconRt.anchorMax = new Vector2(0.5f, 1f);
        iconRt.sizeDelta        = new Vector2(80, 80);
        iconRt.anchoredPosition = new Vector2(0, -52);

        if (def.icon == null)
        {
            string letter = string.IsNullOrEmpty(def.displayName) ? "?" : def.displayName.Substring(0, 1).ToUpperInvariant();
            var lbl = MakeTxt(iconGO, "Lbl", letter, 34, Color.white, new Vector2(0.5f, 0.5f), Vector2.zero);
            UiKit.BrawlText(lbl);
            lbl.raycastTarget = false;
            StretchFull(lbl.rectTransform);
        }

        var nameTxt = MakeTxt(cell, "Name", def.displayName, 14, Color.white,
            new Vector2(0.5f, 0f), new Vector2(140, 22));
        nameTxt.rectTransform.anchoredPosition = new Vector2(0, 34);
        nameTxt.fontStyle     = FontStyles.Bold;
        nameTxt.raycastTarget = false;

        if (selected)
        {
            var checkTxt = MakeTxt(cell, "Check", Loc.T("SELECTED"), 11, SelectedGr,
                new Vector2(0.5f, 0f), new Vector2(140, 16));
            checkTxt.rectTransform.anchoredPosition = new Vector2(0, 14);
            checkTxt.raycastTarget = false;
        }
    }

    static GameObject MakePanel(GameObject parent, string name, Color color,
        Vector2 anchoredPos, Vector2 size, Vector2 anchor)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        var rt  = img.rectTransform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.sizeDelta        = size;
        rt.anchoredPosition = anchoredPos;
        return go;
    }

    static TextMeshProUGUI MakeTxt(GameObject parent, string name, string content,
        int size, Color color, Vector2 anchor, Vector2 sizeDelta)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var txt = go.AddComponent<TextMeshProUGUI>();
        txt.text            = content;
        txt.fontSize        = size;
        txt.color           = color;
        txt.alignment       = TextAlignmentOptions.Center;
        txt.overflowMode    = TextOverflowModes.Ellipsis;
        var rt = txt.rectTransform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.sizeDelta        = sizeDelta;
        rt.anchoredPosition = Vector2.zero;
        return txt;
    }

    static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
