using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CosmicRumble.Economy;
using CosmicRumble.Localization;

/// <summary>
/// Ana menüdeki GARDIROP butonu → kostüm paneli. Artboard 09'a göre panel TEK karakter
/// gösterir ve karakterler arasında "◀ CHARACTER 1 ▶" karuseliyle geçilir; daha önce
/// 5 karakter sütunu yan yana sıkıştırılıyordu. Toplam 5 karakter × 3 kademe = 15 kostüm.
/// Eski "yalnızca sahip olunanlar" davranışının aksine kilitli kostümler de görünür:
/// Gold/Gem olanlar fiyat + satın alma (CostumeManager.TryPurchase, bakiye yetmiyorsa pasif),
/// Level/Sandık/Başarım olanlar koşul etiketiyle listelenir. Sahip olunana dokunmak kuşandırır.
/// UiKit stiliyle (yuvarlatık kart + pop animasyonu + köşe X) programatik Canvas kurar.
/// </summary>
public class WardrobePanelUI : MonoBehaviour
{
    public static WardrobePanelUI Instance { get; private set; }

    const int CharacterCount = 5;

    // Karusel tek karakter gosterdigi icin kartlar sutun duzenindekinden buyuk;
    // hucre icindeki ofsetler bu iki olcuye gore ayarlandi.
    const float CellW = 230f;
    const float CellH = 290f;

    // ── Renk paleti (UiKit mobil teması — QuestsPanelUI ile aynı) ─────────
    static readonly Color CardBg     = UiTheme.Card;
    static readonly Color CellBg     = UiTheme.Slot;
    static readonly Color CellBgLock = UiTheme.Locked;
    static readonly Color EquippedGr = UiTheme.Green;
    static readonly Color PillOff    = UiTheme.Slot;
    static readonly Color GoldCol    = UiTheme.GoldChip;
    static readonly Color GemCol     = UiTheme.GemChip;
    static readonly Color TextSec    = UiTheme.TextMuted;
    static readonly Color StrokeCol  = UiTheme.Stroke;
    static readonly Color TitleGold  = UiTheme.Gold;

    // ── Referanslar ───────────────────────────────────────────────────────
    GameObject      _panelRoot;
    GameObject      _contentParent;
    TextMeshProUGUI _countText;
    TextMeshProUGUI _equippedText;
    TextMeshProUGUI _emptyText;
    TextMeshProUGUI _carouselLabel;

    /// <summary>Karuselde gosterilen karakter (1..CharacterCount).</summary>
    int _currentCharacter = 1;

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
        if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.OnCurrencyChanged += HandleCurrencyChanged;
    }

    void OnDisable()
    {
        if (CostumeManager.Instance != null)
        {
            CostumeManager.Instance.OnCostumePurchased -= HandleCostumeChanged;
            CostumeManager.Instance.OnCostumeEquipped  -= HandleCostumeChanged;
        }
        if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.OnCurrencyChanged -= HandleCurrencyChanged;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void HandleCostumeChanged(CostumeDefinition def)
    {
        if (_panelRoot != null && _panelRoot.activeSelf) Populate();
    }

    void HandleCurrencyChanged(CurrencyType type, long newBalance)
    {
        // Satın alınabilir kartların pasif/aktif durumu bakiyeye bağlı
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
        JumpToEquippedCharacter();
        Populate();
    }

    /// <summary>Panel acilinca karusel, uzerinde kusanili kostum olan karakterde baslasin —
    /// aksi halde oyuncu her acilista 1'den kendi karakterine kadar ilerlemek zorunda kalir.</summary>
    void JumpToEquippedCharacter()
    {
        var equipped = CostumeManager.Instance?.GetEquipped(CostumeType.Character);
        if (equipped != null && equipped.characterId >= 1 && equipped.characterId <= CharacterCount)
            _currentCharacter = equipped.characterId;
    }

    /// <summary>Karuseli kaydirir; iki ucta basa/sona sarar.</summary>
    void StepCharacter(int delta)
    {
        _currentCharacter = ((_currentCharacter - 1 + delta + CharacterCount) % CharacterCount) + 1;
        AudioManager.Instance?.PlayClick();
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
        UiKit.ScrollFade(card, CardBg);

        _countText = MakeTxt(card, "CountInfo", "", 13, TextSec,
            new Vector2(0.5f, 0.865f), new Vector2(680, 20));

        // Tasarimda sayacin altinda kusanili kostumun adi yaziyor; panelde hangi kostumun
        // uzerinde oldugu yalnizca kucuk bir cerceveden anlasiliyordu.
        _equippedText = MakeTxt(card, "EquippedInfo", "", 14, EquippedGr,
            new Vector2(0.5f, 0.815f), new Vector2(680, 22));

        BuildCarousel(card);
        BuildScrollView(card);

        // Boş durum mesajı — yalnızca veritabanı/manager yoksa (normalde 15 kostüm hep görünür)
        _emptyText = MakeTxt(card, "EmptyMsg", "", 16, TextSec,
            new Vector2(0.5f, 0.45f), new Vector2(640, 80));
        _emptyText.gameObject.SetActive(false);

        _panelRoot.AddComponent<EscapeListener>().OnEscape = Hide;
        _panelRoot.SetActive(false);
    }

    /// <summary>"◀ CHARACTER n ▶" satiri: iki yon plakasi ve aradaki karakter etiketi.</summary>
    void BuildCarousel(GameObject parent)
    {
        _carouselLabel = MakeTxt(parent, "CarouselLabel", "", 20, TitleGold,
            new Vector2(0.5f, 0.755f), new Vector2(300, 34));
        _carouselLabel.fontStyle = FontStyles.Bold;

        MakeArrow(parent, "PrevChar", "◀", -190f, -1);
        MakeArrow(parent, "NextChar", "▶",  190f, +1);
    }

    void MakeArrow(GameObject parent, string name, string glyph, float x, int delta)
    {
        // 76x76: buton denetiminin en kucuk dokunma hedefi 72 tasarim birimi.
        var go = MakePanel(parent, name, UiTheme.Plate, new Vector2(x, 0f),
            new Vector2(76, 76), new Vector2(0.5f, 0.755f));
        var img = go.GetComponent<Image>();
        UiKit.Round(img, 1.4f);
        UiKit.BottomEdge(go, UiKit.EdgeOf(UiTheme.Plate), 5f, 1.4f);

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.colors = UiKit.ButtonColors(UiTheme.Plate);
        btn.onClick.AddListener(() => StepCharacter(delta));
        UiKit.Press(go);
        UiKit.Hover(go);

        var lbl = MakeTxt(go, "Lbl", glyph, 22, UiTheme.TextPrimary, new Vector2(0.5f, 0.5f), Vector2.zero);
        lbl.fontStyle     = FontStyles.Bold;
        lbl.raycastTarget = false;
        StretchFull(lbl.rectTransform);
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
        // Karusel satiri 0.755 ankorunda duruyor; liste onun altindan basliyor.
        scrollRt.sizeDelta        = new Vector2(800, 400);
        scrollRt.anchoredPosition = new Vector2(0, -80);

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

        // Icerik: TEK karakterin kostumleri, 3'lu izgara (kademe basina bir kart).
        var contentGO = new GameObject("Content");
        contentGO.transform.SetParent(vpGO.transform, false);

        var grid = contentGO.AddComponent<GridLayoutGroup>();
        grid.cellSize        = new Vector2(CellW, CellH);
        grid.spacing         = new Vector2(14f, 14f);
        grid.padding         = new RectOffset(4, 4, 4, 4);
        grid.childAlignment  = TextAnchor.UpperCenter;
        grid.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 3;

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
    //  POPULATE — 5 karakter × 3 kademe, kilitliler dahil
    // ════════════════════════════════════════════════════════════════════

    void Populate()
    {
        foreach (Transform child in _contentParent.transform)
            Destroy(child.gameObject);

        var mgr = CostumeManager.Instance;
        var db  = Resources.Load<CostumeDatabase>("Economy/CostumeDatabase");
        if (mgr == null || db == null)
        {
            ShowEmpty(Loc.T("Wardrobe is currently unavailable."));
            if (_countText) _countText.text = "";
            return;
        }
        _emptyText.gameObject.SetActive(false);

        int total = 0, ownedCount = 0;
        foreach (var c in db.allCostumes)
        {
            if (c == null) continue;
            total++;
            if (mgr.IsOwned(c.costumeId)) ownedCount++;
        }
        _countText.text = string.Format(Loc.T("Owned: {0} / {1}"), ownedCount, total);

        var equipped = mgr.GetEquipped(CostumeType.Character);
        if (_equippedText != null)
            _equippedText.text = equipped != null
                ? string.Format(Loc.T("Equipped: {0}"), Loc.T(equipped.displayName))
                : "";

        if (_carouselLabel != null)
            _carouselLabel.text = string.Format(Loc.T("CHARACTER {0}"), _currentCharacter);

        var costumes = db.allCostumes
            .Where(c => c != null && c.characterId == _currentCharacter)
            .OrderBy(c => c.costumeId)
            .ToList();

        if (costumes.Count == 0)
        {
            ShowEmpty(Loc.T("Wardrobe is currently unavailable."));
            return;
        }

        foreach (var def in costumes)
            BuildCell(_contentParent, def,
                equipped != null && equipped.costumeId == def.costumeId,
                mgr.IsOwned(def.costumeId));
    }

    void ShowEmpty(string msg)
    {
        _emptyText.text = msg;
        _emptyText.gameObject.SetActive(true);
    }

    // ════════════════════════════════════════════════════════════════════
    //  HÜCRE
    // ════════════════════════════════════════════════════════════════════

    void BuildCell(GameObject parent, CostumeDefinition def, bool equipped, bool owned)
    {
        Color rarityCol = RarityColor(def.rarity);

        var cell = new GameObject($"Cell_{def.costumeId}");
        cell.transform.SetParent(parent.transform, false);

        var bg = cell.AddComponent<Image>();
        bg.rectTransform.sizeDelta = new Vector2(CellW, CellH); // GridLayoutGroup zaten ezer, tek basina test icin
        bg.color = owned ? CellBg : CellBgLock;
        UiKit.Round(bg, 1.6f);
        UiKit.Stroke(cell,
            equipped ? EquippedGr
                     : new Color(rarityCol.r, rarityCol.g, rarityCol.b, owned ? 0.55f : 0.22f), 1.6f);

        bool purchasable = !owned &&
            (def.unlockMethod == CostumeUnlock.ByGold || def.unlockMethod == CostumeUnlock.ByGem);

        var btn = cell.AddComponent<Button>();
        btn.targetGraphic = bg;
        btn.colors = UiKit.ButtonColors(owned ? CellBg : CellBgLock);
        string id = def.costumeId;

        if (owned)
        {
            btn.onClick.AddListener(() =>
            {
                if (CostumeManager.Instance == null) return;
                var eq = CostumeManager.Instance.GetEquipped(CostumeType.Character);
                if (eq != null && eq.costumeId == id) return; // zaten kuşanılı
                AudioManager.Instance?.PlayClick();
                CostumeManager.Instance.Equip(id); // OnCostumeEquipped → Populate
            });
        }
        else if (purchasable)
        {
            btn.onClick.AddListener(() =>
            {
                if (CostumeManager.Instance == null) return;
                if (CostumeManager.Instance.TryPurchase(id)) // OnCostumePurchased → Populate
                    AudioManager.Instance?.PlayClick();
            });
            btn.interactable = CostumeManager.Instance != null
                && CostumeManager.Instance.CanPurchase(id).canUnlock;
        }
        else
        {
            btn.interactable = false; // Level/Sandık/Başarım: oynayarak kazanılır, tıklanmaz
        }
        UiKit.Press(cell, 0.96f);
        UiKit.Hover(cell);

        // Önizleme: sprite varsa göster, yoksa rarity renkli daire + baş harf rozeti
        // (kilitlilerde soluk — "henüz senin değil" hissi)
        float previewAlpha = owned ? 1f : 0.35f;
        if (def.previewSprite != null)
        {
            var prevGO  = new GameObject("Preview");
            prevGO.transform.SetParent(cell.transform, false);
            var prevImg = prevGO.AddComponent<Image>();
            prevImg.sprite         = def.previewSprite;
            prevImg.preserveAspect = true;
            prevImg.raycastTarget  = false;
            prevImg.color          = new Color(1f, 1f, 1f, previewAlpha);
            PlacePreview(prevImg.rectTransform);
        }
        else
        {
            var prevGO  = new GameObject("Preview");
            prevGO.transform.SetParent(cell.transform, false);
            var prevImg = prevGO.AddComponent<Image>();
            prevImg.sprite        = UiKit.CircleSprite;
            prevImg.color         = new Color(rarityCol.r, rarityCol.g, rarityCol.b, previewAlpha);
            prevImg.raycastTarget = false;
            PlacePreview(prevImg.rectTransform);

            string localizedName = Loc.T(def.displayName);
            string letter = string.IsNullOrEmpty(localizedName) ? "?" : localizedName.Substring(0, 1).ToUpperInvariant();
            var lbl = MakeTxt(prevGO, "Lbl", letter, 44, new Color(1f, 1f, 1f, owned ? 1f : 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero);
            UiKit.BrawlText(lbl);
            lbl.raycastTarget = false;
            StretchFull(lbl.rectTransform);
        }

        // Kilit rozeti - tasarimda her kilitli kart bir asma kilit tasiyor; burada yalnizca
        // sart yazisi vardi ve kart "kilitli" gibi okunmuyordu.
        if (!owned)
        {
            var lockGO = new GameObject("Lock");
            lockGO.transform.SetParent(cell.transform, false);
            var lockRt = lockGO.AddComponent<RectTransform>();
            lockRt.anchorMin = lockRt.anchorMax = new Vector2(1f, 1f);
            lockRt.sizeDelta = new Vector2(28, 28);
            lockRt.anchoredPosition = new Vector2(-18, -18);

            var body = new GameObject("Body");
            body.transform.SetParent(lockGO.transform, false);
            var bodyImg = body.AddComponent<Image>();
            bodyImg.sprite = UiKit.RoundedSprite;
            bodyImg.type   = Image.Type.Sliced;
            bodyImg.pixelsPerUnitMultiplier = 3f;
            bodyImg.color  = TextSec;
            bodyImg.raycastTarget = false;
            var bodyRt = bodyImg.rectTransform;
            bodyRt.anchorMin = bodyRt.anchorMax = new Vector2(0.5f, 0.5f);
            bodyRt.sizeDelta = new Vector2(20, 14);
            bodyRt.anchoredPosition = new Vector2(0, -5);

            var shackle = new GameObject("Shackle");
            shackle.transform.SetParent(lockGO.transform, false);
            var shImg = shackle.AddComponent<Image>();
            shImg.sprite = UiKit.RoundedSprite;
            shImg.type   = Image.Type.Sliced;
            shImg.pixelsPerUnitMultiplier = 6f;
            shImg.color  = TextSec;
            shImg.raycastTarget = false;
            var shRt = shImg.rectTransform;
            shRt.anchorMin = shRt.anchorMax = new Vector2(0.5f, 0.5f);
            shRt.sizeDelta = new Vector2(12, 12);
            shRt.anchoredPosition = new Vector2(0, 6);
        }

        var nameTxt = MakeTxt(cell, "Name", Loc.T(def.displayName), 15,
            owned ? Color.white : TextSec, new Vector2(0.5f, 0f), new Vector2(200, 22));
        nameTxt.rectTransform.anchoredPosition = new Vector2(0, 104);
        nameTxt.fontStyle     = FontStyles.Bold;
        nameTxt.raycastTarget = false;

        var rarityTxt = MakeTxt(cell, "Rarity", RarityName(def.rarity), 12, rarityCol,
            new Vector2(0.5f, 0f), new Vector2(200, 16));
        rarityTxt.rectTransform.anchoredPosition = new Vector2(0, 84);
        rarityTxt.raycastTarget = false;

        BuildStatePill(cell, def, equipped, owned, purchasable);
    }

    /// <summary>Alt pill: KUŞANILDI/KUŞAN (sahipli), fiyat (satın alınabilir) ya da kazanma koşulu.</summary>
    void BuildStatePill(GameObject cell, CostumeDefinition def, bool equipped, bool owned, bool purchasable)
    {
        string label;
        Color pillCol, lblCol = Color.white;

        if (owned)
        {
            label   = equipped ? Loc.T("EQUIPPED") : Loc.T("EQUIP");
            pillCol = equipped ? EquippedGr : PillOff;
            if (equipped) lblCol = new Color(0.03f, 0.15f, 0.05f, 1f);
        }
        else if (purchasable && def.unlockMethod == CostumeUnlock.ByGold)
        {
            label   = string.Format(Loc.T("{0} Gold"), def.goldCost);
            pillCol = new Color(GoldCol.r * 0.45f, GoldCol.g * 0.45f, GoldCol.b * 0.45f, 1f);
            lblCol  = GoldCol;
        }
        else if (purchasable) // ByGem
        {
            label   = string.Format(Loc.T("{0} Gem"), def.gemCost);
            pillCol = new Color(GemCol.r * 0.30f, GemCol.g * 0.30f, GemCol.b * 0.30f, 1f);
            lblCol  = GemCol;
        }
        else
        {
            label = def.unlockMethod switch
            {
                CostumeUnlock.ByLevel       => string.Format(Loc.T("Lv {0}"), def.requiredLevel),
                CostumeUnlock.ByChest       => Loc.T("Chest drop"),
                CostumeUnlock.ByAchievement => Loc.T("Achievement reward"),
                _                           => Loc.T("Locked"),
            };
            pillCol = PillOff;
            lblCol  = TextSec;
        }

        var pillGO  = new GameObject("StatePill");
        pillGO.transform.SetParent(cell.transform, false);
        var pillImg = pillGO.AddComponent<Image>();
        pillImg.color         = pillCol;
        pillImg.raycastTarget = false;
        UiKit.Round(pillImg, 2.5f);
        var pillRt = pillImg.rectTransform;
        pillRt.anchorMin = pillRt.anchorMax = new Vector2(0.5f, 0f);
        pillRt.sizeDelta        = new Vector2(190, 32);
        pillRt.anchoredPosition = new Vector2(0, 26);

        var pillLbl = MakeTxt(pillGO, "Lbl", label, 13, lblCol, new Vector2(0.5f, 0.5f), Vector2.zero);
        pillLbl.fontStyle     = FontStyles.Bold;
        pillLbl.raycastTarget = false;
        StretchFull(pillLbl.rectTransform);
    }

    static void PlacePreview(RectTransform rt)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.sizeDelta        = new Vector2(128, 128);
        rt.anchoredPosition = new Vector2(0, -70);
    }

    // ════════════════════════════════════════════════════════════════════
    //  RARITY GÖRSELLERİ
    // ════════════════════════════════════════════════════════════════════

    static Color RarityColor(CostumeRarity r) => r switch
    {
        CostumeRarity.Uncommon  => UiTheme.RarityUncommon,
        CostumeRarity.Rare      => UiTheme.RarityRare,
        CostumeRarity.Epic      => UiTheme.RarityEpic,
        CostumeRarity.Legendary => UiTheme.RarityLegendary,
        _                       => UiTheme.RarityCommon,
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
