using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthBarUI : MonoBehaviour
{
    [Tooltip("Karakterin merkezinden dikey ofset (yerel birim)")]
    public float verticalOffset = 2.0f;

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
        rt.sizeDelta = new Vector2(1.5f, 0.4f);

        // Arka plan (siyah)
        var bgGO = new GameObject("Background");
        bgGO.transform.SetParent(canvasGO.transform, false);
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0, 0, 0, 0.6f);
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
        _fillImage.color = Color.green;
        var fillRt = _fillImage.rectTransform;
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = fillRt.offsetMax = Vector2.zero;

        // HP yazısı
        var textGO = new GameObject("HealthText");
        textGO.transform.SetParent(canvasGO.transform, false);
        _healthText = textGO.AddComponent<TextMeshPro>();
        _healthText.fontSize = 3f;
        _healthText.color = Color.white;
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
        _fillImage.color = Color.Lerp(Color.red, Color.green, Mathf.Clamp01(ratio * 2f));
    }

    void LateUpdate()
    {
    }
}