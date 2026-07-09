using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CosmicRumble.Economy;
using CosmicRumble.Localization;

/// <summary>
/// Ana menüdeki GARDIROP butonu → sahip olunan kostümlerin paneli.
/// Yalnızca SAHİP OLUNAN kostümler listelenir — kilitli/satın alınmamış olanlar hiç görünmez
/// (mağaza/kilit açma ayrı iş; burası oyuncunun koleksiyonu). KARAKTER/SİLAH sekmeleri,
/// rarity çerçeveli grid kartları; karta dokunmak kostümü kuşandırır.
/// UiKit stiliyle (yuvarlatık kart + pill sekmeler + pop animasyonu + köşe X) programatik Canvas kurar.
/// </summary>
public class WardrobePanelUI : MonoBehaviour
{
    public static WardrobePanelUI Instance { get; private set; }

    // ── Renk paleti (UiKit mobil teması — QuestsPanelUI ile aynı) ─────────
    static readonly Color CardBg     = new Color(0.07f,  0.07f,  0.16f,  0.97f);
    static readonly Color PrimaryBtn = new Color(0.12f,  0.68f,  0.22f,  1f);
    static readonly Color TabOff     = new Color(0.15f,  0.15f,  0.27f,  1f);
    static readonly Color CellBg     = new Color(0.11f,  0.11f,  0.21f,  1f);
    static readonly Color EquippedGr = new Color(0.30f,  0.85f,  0.40f,  1f);
    static readonly Color TextSec    = new Color(0.533f, 0.533f, 0.667f, 1f);
    static readonly Color StrokeCol  = new Color(1f, 1f, 1f, 0.09f);
    static readonly Color TitleGold  = new Color(1f, 0.80f, 0.20f, 1f);

    CostumeType _currentTab = CostumeType.Character;

    // ── Referanslar ───────────────────────────────────────────────────────
    GameObject      _panelRoot;
    GameObject      _contentParent;
    TextMeshProUGUI _countText;
    TextMeshProUGUI _emptyText;
    Button          _tabCharBtn, _tabWeaponBtn;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        BuildUI();
    }

    void OnEnable()
    {
        if (CostumeManager.Instance != null)
        {
            CostumeManager.Instance.OnCostumePurchased += HandleCostumeChanged;
            CostumeManager.Instance.OnCostumeEquipped  += HandleCostumeChanged;
        }
    }

    void OnDisable()
    {
        if (CostumeManager.Instance != null)
        {
            CostumeManager.Instance.OnCostumePurchased -= HandleCostumeChanged;
            CostumeManager.Instance.OnCostumeEquipped  -= HandleCostumeChanged;
        }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void HandleCostumeChanged(CostumeDefinition def)
    {
        if (_panelRoot != null && _panelRoot.activeSelf) Populate();
    }

    // ════════════════════════════════════════════════════════════════════
    //  PUBLIC API
    // ════════════════════════════════════════════════════════════════════

    public void Show()
    {
        // Aktivasyon ÖNCE gelmeli: Populate() içinde UiKit.BrawlText çağrısı font materyali
        // instance'ı oluşturuyor, bu da TMP_Text'in OnEnable'ının çalışmış olmasını gerektirir —
        // inaktif hiyerarşide oluşturulan TMP objelerinde OnEnable ertelenir ve NullReferenceException'a yol açar.
        _panelRoot.SetActive(true);
        Populate();
    }

    public void Hide() => _panelRoot.SetActive(false);

    // ════════════════════════════════════════════════════════════════════
    //  UI BUILD
    // ════════════════════════════════════════════════════════════════════

    void BuildUI()
    {
        var canvasGO = new GameObject("WardrobeCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 46;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        _panelRoot = new GameObject("WardrobePanel");
        _panelRoot.transform.SetParent(canvasGO.transform, false);
        var overlay = _panelRoot.AddComponent<Image>();
        overlay.color = new Color(0, 0, 0, 0.65f);
        StretchFull(overlay.rectTransform);

        var card = MakePanel(_panelRoot, "Card", CardBg, Vector2.zero,
            new Vector2(860, 620), new Vector2(0.5f, 0.5f));
        UiKit.Round(card.GetComponent<Image>());
        UiKit.Shadow(card, 8f, 0.55f);
        UiKit.Stroke(card, StrokeCol);
        UiKit.Pop(card);

        var title = MakeTxt(card, "Title", Loc.T("WARDROBE"), 30, TitleGold,
            new Vector2(0.5f, 0.925f), new Vector2(680, 46));
        title.fontStyle = FontStyles.Bold;

        UiKit.CloseButton(card, Hide);

        BuildTabs(card);

        _countText = MakeTxt(card, "CountInfo", "", 13, TextSec,
            new Vector2(0.5f, 0.765f), new Vector2(680, 20));

        BuildScrollView(card);

        // Boş durum mesajı — grid'in DIŞINDA (GridLayoutGroup hücre boyutuna hapsetmesin)
        _emptyText = MakeTxt(card, "EmptyMsg", "", 16, TextSec,
            new Vector2(0.5f, 0.45f), new Vector2(640, 80));
        _emptyText.gameObject.SetActive(false);

        _panelRoot.AddComponent<EscapeListener>().OnEscape = Hide;
        _panelRoot.SetActive(false);
    }

    void BuildTabs(GameObject parent)
    {
        float y = 0.845f;
        _tabCharBtn   = MakeTabBtn(parent, "tab_character", Loc.T("CHARACTER"), new Vector2(0.38f, y), () => SwitchTab(CostumeType.Character));
        _tabWeaponBtn = MakeTabBtn(parent, "tab_weapon",    Loc.T("WEAPON"),    new Vector2(0.62f, y), () => SwitchTab(CostumeType.Weapon));
    }

    Button MakeTabBtn(GameObject parent, string name, string label, Vector2 anchor, UnityEngine.Events.UnityAction cb)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var img = go.AddComponent<Image>();
        UiKit.Round(img, 1.1f); // pill görünüm
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(cb);
        UiKit.Press(go);
        UiKit.Hover(go);
        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.sizeDelta        = new Vector2(164, 44);
        rt.anchoredPosition = Vector2.zero;

        var lbl = MakeTxt(go, "Lbl", label, 15, Color.white, new Vector2(0.5f, 0.5f), Vector2.zero);
        lbl.fontStyle = FontStyles.Bold;
        StretchFull(lbl.rectTransform);
        return btn;
    }

    void SwitchTab(CostumeType tab)
    {
        AudioManager.Instance?.PlayClick();
        _currentTab = tab;
        Populate();
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
        scrollRt.sizeDelta        = new Vector2(790, 420);
        scrollRt.anchoredPosition = new Vector2(0, -70);

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
        grid.cellSize        = new Vector2(180, 214);
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

    // ════════════════════════════════════════════════════════════════════
    //  POPULATE — yalnızca sahip olunan kostümler
    // ════════════════════════════════════════════════════════════════════

    void Populate()
    {
        foreach (Transform child in _contentParent.transform)
            Destroy(child.gameObject);

        RefreshTabHighlight();

        var mgr = CostumeManager.Instance;
        var db  = Resources.Load<CostumeDatabase>("Economy/CostumeDatabase");
        if (mgr == null || db == null)
        {
            ShowEmpty(Loc.T("Wardrobe is currently unavailable."));
            if (_countText) _countText.text = "";
            return;
        }

        var owned = new List<CostumeDefinition>();
        int totalInTab = 0;
        foreach (var c in db.allCostumes)
        {
            if (c == null || c.costumeType != _currentTab) continue;
            totalInTab++;
            if (mgr.IsOwned(c.costumeId)) owned.Add(c);
        }

        _countText.text = string.Format(Loc.T("Owned: {0} / {1}"), owned.Count, totalInTab);

        if (owned.Count == 0)
        {
            ShowEmpty(Loc.T("You don't have any costumes in this category yet.\nEarn costumes from chests, achievements, and leveling up."));
            return;
        }
        _emptyText.gameObject.SetActive(false);

        var equipped = mgr.GetEquipped(_currentTab);
        foreach (var def in owned)
            BuildCell(def, equipped != null && equipped.costumeId == def.costumeId);
    }

    void ShowEmpty(string msg)
    {
        _emptyText.text = msg;
        _emptyText.gameObject.SetActive(true);
    }

    void RefreshTabHighlight()
    {
        SetTabVisual(_tabCharBtn,   _currentTab == CostumeType.Character);
        SetTabVisual(_tabWeaponBtn, _currentTab == CostumeType.Weapon);
    }

    static void SetTabVisual(Button btn, bool active)
    {
        if (btn == null) return;
        var img = btn.GetComponent<Image>();
        Color c = active ? PrimaryBtn : TabOff;
        img.color  = c;
        btn.colors = UiKit.ButtonColors(c);
    }

    void BuildCell(CostumeDefinition def, bool equipped)
    {
        Color rarityCol = RarityColor(def.rarity);

        var cell = new GameObject($"Cell_{def.costumeId}");
        cell.transform.SetParent(_contentParent.transform, false);
        var bg = cell.AddComponent<Image>();
        bg.color = CellBg;
        UiKit.Round(bg, 1.6f);
        UiKit.Stroke(cell, equipped ? EquippedGr : new Color(rarityCol.r, rarityCol.g, rarityCol.b, 0.55f), 1.6f);

        var btn = cell.AddComponent<Button>();
        btn.targetGraphic = bg;
        btn.colors = UiKit.ButtonColors(CellBg);
        string id = def.costumeId;
        btn.onClick.AddListener(() =>
        {
            if (CostumeManager.Instance == null) return;
            var eq = CostumeManager.Instance.GetEquipped(_currentTab);
            if (eq != null && eq.costumeId == id) return; // zaten kuşanılı
            AudioManager.Instance?.PlayClick();
            CostumeManager.Instance.Equip(id); // OnCostumeEquipped → Populate
        });
        UiKit.Press(cell, 0.96f);
        UiKit.Hover(cell);

        // Önizleme: sprite varsa göster, yoksa rarity renkli daire + baş harf rozeti
        if (def.previewSprite != null)
        {
            var prevGO  = new GameObject("Preview");
            prevGO.transform.SetParent(cell.transform, false);
            var prevImg = prevGO.AddComponent<Image>();
            prevImg.sprite         = def.previewSprite;
            prevImg.preserveAspect = true;
            prevImg.raycastTarget  = false;
            PlacePreview(prevImg.rectTransform);
        }
        else
        {
            var prevGO  = new GameObject("Preview");
            prevGO.transform.SetParent(cell.transform, false);
            var prevImg = prevGO.AddComponent<Image>();
            prevImg.sprite        = UiKit.CircleSprite;
            prevImg.color         = rarityCol;
            prevImg.raycastTarget = false;
            PlacePreview(prevImg.rectTransform);

            string localizedName = Loc.T(def.displayName);
            string letter = string.IsNullOrEmpty(localizedName) ? "?" : localizedName.Substring(0, 1).ToUpperInvariant();
            var lbl = MakeTxt(prevGO, "Lbl", letter, 40, Color.white, new Vector2(0.5f, 0.5f), Vector2.zero);
            UiKit.BrawlText(lbl);
            lbl.raycastTarget = false;
            StretchFull(lbl.rectTransform);
        }

        var nameTxt = MakeTxt(cell, "Name", Loc.T(def.displayName), 14, Color.white,
            new Vector2(0.5f, 0f), new Vector2(168, 20));
        nameTxt.rectTransform.anchoredPosition = new Vector2(0, 74);
        nameTxt.fontStyle     = FontStyles.Bold;
        nameTxt.raycastTarget = false;

        var rarityTxt = MakeTxt(cell, "Rarity", RarityName(def.rarity), 11, rarityCol,
            new Vector2(0.5f, 0f), new Vector2(168, 16));
        rarityTxt.rectTransform.anchoredPosition = new Vector2(0, 54);
        rarityTxt.raycastTarget = false;

        // Alt pill: kuşanılı ise yeşil KUŞANILDI, değilse koyu KUŞAN (dokunma ipucu)
        var pillGO  = new GameObject("StatePill");
        pillGO.transform.SetParent(cell.transform, false);
        var pillImg = pillGO.AddComponent<Image>();
        pillImg.color         = equipped ? EquippedGr : TabOff;
        pillImg.raycastTarget = false;
        UiKit.Round(pillImg, 2.5f);
        var pillRt = pillImg.rectTransform;
        pillRt.anchorMin = pillRt.anchorMax = new Vector2(0.5f, 0f);
        pillRt.sizeDelta        = new Vector2(120, 28);
        pillRt.anchoredPosition = new Vector2(0, 20);

        var pillLbl = MakeTxt(pillGO, "Lbl", equipped ? Loc.T("EQUIPPED") : Loc.T("EQUIP"), 12,
            equipped ? new Color(0.03f, 0.15f, 0.05f, 1f) : Color.white,
            new Vector2(0.5f, 0.5f), Vector2.zero);
        pillLbl.fontStyle     = FontStyles.Bold;
        pillLbl.raycastTarget = false;
        StretchFull(pillLbl.rectTransform);
    }

    static void PlacePreview(RectTransform rt)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.sizeDelta        = new Vector2(92, 92);
        rt.anchoredPosition = new Vector2(0, -58);
    }

    // ════════════════════════════════════════════════════════════════════
    //  RARITY GÖRSELLERİ
    // ════════════════════════════════════════════════════════════════════

    static Color RarityColor(CostumeRarity r) => r switch
    {
        CostumeRarity.Uncommon  => new Color(0.30f, 0.85f, 0.40f, 1f),
        CostumeRarity.Rare      => new Color(0.25f, 0.55f, 1.00f, 1f),
        CostumeRarity.Epic      => new Color(0.65f, 0.35f, 1.00f, 1f),
        CostumeRarity.Legendary => new Color(1.00f, 0.80f, 0.20f, 1f),
        _                       => new Color(0.62f, 0.65f, 0.70f, 1f), // Common
    };

    static string RarityName(CostumeRarity r) => r switch
    {
        CostumeRarity.Uncommon  => Loc.T("UNCOMMON"),
        CostumeRarity.Rare      => Loc.T("RARE"),
        CostumeRarity.Epic      => Loc.T("EPIC"),
        CostumeRarity.Legendary => Loc.T("LEGENDARY"),
        _                       => Loc.T("COMMON"),
    };

    // ════════════════════════════════════════════════════════════════════
    //  LAYOUT HELPERS (QuestsPanelUI ile aynı kalıp)
    // ════════════════════════════════════════════════════════════════════

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
