using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CosmicRumble.Economy;
using CosmicRumble.Localization;

/// <summary>
/// Ana menüdeki GARDIROP butonu → kostüm paneli. 2026-07-16 yeniden tasarımı:
/// 5 karakter sütunu × 3 kademe (Standard/Advanced/Elite), TOPLAM 15 kostüm.
/// Eski "yalnızca sahip olunanlar" davranışının aksine kilitli kostümler de görünür:
/// Gold/Gem olanlar fiyat + satın alma (CostumeManager.TryPurchase, bakiye yetmiyorsa pasif),
/// Level/Sandık/Başarım olanlar koşul etiketiyle listelenir. Sahip olunana dokunmak kuşandırır.
/// UiKit stiliyle (yuvarlatık kart + pop animasyonu + köşe X) programatik Canvas kurar.
/// </summary>
public class WardrobePanelUI : MonoBehaviour
{
    public static WardrobePanelUI Instance { get; private set; }

    const int CharacterCount = 5;

    // ── Renk paleti (UiKit mobil teması — QuestsPanelUI ile aynı) ─────────
    static readonly Color CardBg     = new Color(0.07f,  0.07f,  0.16f,  0.97f);
    static readonly Color CellBg     = new Color(0.11f,  0.11f,  0.21f,  1f);
    static readonly Color CellBgLock = new Color(0.085f, 0.085f, 0.16f,  1f);
    static readonly Color EquippedGr = new Color(0.30f,  0.85f,  0.40f,  1f);
    static readonly Color PillOff    = new Color(0.15f,  0.15f,  0.27f,  1f);
    static readonly Color GoldCol    = new Color(1.00f,  0.72f,  0.00f,  1f);
    static readonly Color GemCol     = new Color(0.60f,  0.85f,  1.00f,  1f);
    static readonly Color TextSec    = new Color(0.533f, 0.533f, 0.667f, 1f);
    static readonly Color StrokeCol  = new Color(1f, 1f, 1f, 0.09f);
    static readonly Color TitleGold  = new Color(1f, 0.80f, 0.20f, 1f);

    // ── Referanslar ───────────────────────────────────────────────────────
    GameObject      _panelRoot;
    GameObject      _contentParent;
    TextMeshProUGUI _countText;
    TextMeshProUGUI _emptyText;

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

        _countText = MakeTxt(card, "CountInfo", "", 13, TextSec,
            new Vector2(0.5f, 0.855f), new Vector2(680, 20));

        BuildScrollView(card);

        // Boş durum mesajı — yalnızca veritabanı/manager yoksa (normalde 15 kostüm hep görünür)
        _emptyText = MakeTxt(card, "EmptyMsg", "", 16, TextSec,
            new Vector2(0.5f, 0.45f), new Vector2(640, 80));
        _emptyText.gameObject.SetActive(false);

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
        scrollRt.sizeDelta        = new Vector2(800, 470);
        scrollRt.anchoredPosition = new Vector2(0, -45);

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

        // İçerik: 5 karakter sütunu yan yana (her sütun kendi dikey grubu)
        var contentGO = new GameObject("Content");
        contentGO.transform.SetParent(vpGO.transform, false);

        var hlg = contentGO.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing                = 10f;
        hlg.padding                = new RectOffset(4, 4, 4, 4);
        hlg.childAlignment         = TextAnchor.UpperCenter;
        hlg.childControlWidth      = false;
        hlg.childControlHeight     = false;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = false;

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

        for (int ch = 1; ch <= CharacterCount; ch++)
        {
            var column = BuildColumn(ch);
            var costumes = db.allCostumes
                .Where(c => c != null && c.characterId == ch)
                .OrderBy(c => c.costumeId)
                .ToList();
            foreach (var def in costumes)
                BuildCell(column, def,
                    equipped != null && equipped.costumeId == def.costumeId,
                    mgr.IsOwned(def.costumeId));
        }
    }

    GameObject BuildColumn(int characterIndex)
    {
        var col = new GameObject($"Col_Char{characterIndex}");
        col.transform.SetParent(_contentParent.transform, false);

        var vlg = col.AddComponent<VerticalLayoutGroup>();
        vlg.spacing                = 10f;
        vlg.childAlignment         = TextAnchor.UpperCenter;
        // childControl=false: layout, çocukların KENDİ RectTransform boyutunu kullanır —
        // bu yüzden aşağıda her çocuğa sizeDelta açıkça veriliyor (LayoutElement etkisiz olurdu).
        vlg.childControlWidth      = false;
        vlg.childControlHeight     = false;
        vlg.childForceExpandWidth  = false;
        vlg.childForceExpandHeight = false;

        var colRt = col.GetComponent<RectTransform>();
        colRt.sizeDelta = new Vector2(150, 0); // genişlik HLG için; yükseklik CSF'den

        var csf = col.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var headGO = new GameObject("Header");
        headGO.transform.SetParent(col.transform, false);
        var head = headGO.AddComponent<TextMeshProUGUI>();
        head.text      = string.Format(Loc.T("CHARACTER {0}"), characterIndex);
        head.fontSize  = 13;
        head.fontStyle = FontStyles.Bold;
        head.color     = TitleGold;
        head.alignment = TextAlignmentOptions.Center;
        head.overflowMode = TextOverflowModes.Ellipsis;
        head.rectTransform.sizeDelta = new Vector2(150, 26);

        return col;
    }

    void ShowEmpty(string msg)
    {
        _emptyText.text = msg;
        _emptyText.gameObject.SetActive(true);
    }

    // ════════════════════════════════════════════════════════════════════
    //  HÜCRE
    // ════════════════════════════════════════════════════════════════════

    void BuildCell(GameObject column, CostumeDefinition def, bool equipped, bool owned)
    {
        Color rarityCol = RarityColor(def.rarity);

        var cell = new GameObject($"Cell_{def.costumeId}");
        cell.transform.SetParent(column.transform, false);

        var bg = cell.AddComponent<Image>();
        bg.rectTransform.sizeDelta = new Vector2(150, 196); // VLG childControl=false → rect boyutu geçerli
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
            var lbl = MakeTxt(prevGO, "Lbl", letter, 34, new Color(1f, 1f, 1f, owned ? 1f : 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero);
            UiKit.BrawlText(lbl);
            lbl.raycastTarget = false;
            StretchFull(lbl.rectTransform);
        }

        var nameTxt = MakeTxt(cell, "Name", Loc.T(def.displayName), 13,
            owned ? Color.white : TextSec, new Vector2(0.5f, 0f), new Vector2(140, 18));
        nameTxt.rectTransform.anchoredPosition = new Vector2(0, 70);
        nameTxt.fontStyle     = FontStyles.Bold;
        nameTxt.raycastTarget = false;

        var rarityTxt = MakeTxt(cell, "Rarity", RarityName(def.rarity), 10, rarityCol,
            new Vector2(0.5f, 0f), new Vector2(140, 14));
        rarityTxt.rectTransform.anchoredPosition = new Vector2(0, 52);
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
        pillRt.sizeDelta        = new Vector2(132, 26);
        pillRt.anchoredPosition = new Vector2(0, 18);

        var pillLbl = MakeTxt(pillGO, "Lbl", label, 11, lblCol, new Vector2(0.5f, 0.5f), Vector2.zero);
        pillLbl.fontStyle     = FontStyles.Bold;
        pillLbl.raycastTarget = false;
        StretchFull(pillLbl.rectTransform);
    }

    static void PlacePreview(RectTransform rt)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.sizeDelta        = new Vector2(80, 80);
        rt.anchoredPosition = new Vector2(0, -52);
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
