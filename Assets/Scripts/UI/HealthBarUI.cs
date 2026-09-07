using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthBarUI : MonoBehaviour
{
    // Artboard 16: isim etiketi ÜSTTE, can barı onun ALTINDA ayrı bir şerit.
    // CharacterNameTag.verticalOffset bu değerin üstünde kalmalı — ikisi birlikte
    // değiştirilir, yoksa etiketler tekrar üst üste biner.
    [Tooltip("Karakterin merkezinden dikey ofset (yerel birim)")]
    public float verticalOffset = 1.72f;

    // Bar gövdesinin dünya birimi ölçüsü ve içindeki HP yazısının punto karşılığı.
    const float BarWidth  = 1.6f;
    const float BarHeight = 0.34f;
    const float TextSize  = 0.26f;

    private CharacterHealth _characterHealth;
    private Image _fillImage;
    private TextMeshPro _healthText;
    private Transform _canvasTransform;

    void Awake()
    {
        _characterHealth = GetComponent<CharacterHealth>();
        if (_characterHealth == null) { enabled = false; return; }
        BuildHealthBar();
    }

    void Start()
    {
        _characterHealth.OnHealthChanged += UpdateHealthBar;
        UpdateHealthBar(_characterHealth.GetCurrentHealth());
    }

    void OnDestroy()
    {
        if (_characterHealth != null)
            _characterHealth.OnHealthChanged -= UpdateHealthBar;
    }

    void BuildHealthBar()
    {
        // Canvas
        var canvasGO = new GameObject("HealthBarCanvas");
        canvasGO.transform.SetParent(transform, false);
        canvasGO.transform.localPosition = new Vector3(0, verticalOffset, 0);

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        var rt = canvasGO.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(BarWidth, BarHeight);

        // Arka plan (siyah)
        var bgGO = new GameObject("Background");
        bgGO.transform.SetParent(canvasGO.transform, false);
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.color = UiTheme.CardDeep;
        var bgRt = bgImg.rectTransform;
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;

        // Dolgu (yeşil/kırmızı)
        var fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(canvasGO.transform, false);
        _fillImage = fillGO.AddComponent<Image>();
        _fillImage.type = Image.Type.Filled;
        _fillImage.fillMethod = Image.FillMethod.Horizontal;
        _fillImage.color = UiTheme.Green;
        var fillRt = _fillImage.rectTransform;
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = fillRt.offsetMax = Vector2.zero;

        // HP yazısı
        var textGO = new GameObject("HealthText");
        textGO.transform.SetParent(canvasGO.transform, false);
        _healthText = textGO.AddComponent<TextMeshPro>();
        _healthText.fontSize = TextSize;
        _healthText.color = UiTheme.TextPrimary;
        _healthText.alignment = TextAlignmentOptions.Center;
        _healthText.fontStyle = FontStyles.Bold;
        _healthText.outlineWidth = 0.2f;
        _healthText.outlineColor = new Color32(0, 0, 0, 200);
        _healthText.renderMode = TextRenderFlags.Render;
        _healthText.sortingOrder = 1;
        var trt = _healthText.rectTransform;
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;

        _canvasTransform = canvasGO.transform;
    }

    void UpdateHealthBar(float currentHealth)
    {
        float ratio = currentHealth / _characterHealth.maxHealth;
        _fillImage.fillAmount = ratio;
        _healthText.text = $"{(int)currentHealth}";
        _fillImage.color = Color.Lerp(UiTheme.Danger, UiTheme.Green, Mathf.Clamp01(ratio * 2f));
    }

    // Karakter yüzeye göre döndüğü için barın da her karede dik tutulması gerekir —
    // aksi halde dönen bar, üstündeki isim etiketinin üzerinden süpürüyordu.
    void LateUpdate()
    {
        if (_canvasTransform != null)
            _canvasTransform.rotation = Quaternion.identity;
    }
}