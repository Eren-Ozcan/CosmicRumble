using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthBarUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI healthText; // Bar üzerindeki yazı
    [SerializeField] private Image fillImage;             // Dolu kısmı temsil eden görsel
    [SerializeField] private CharacterHealth characterHealth; // Takip edilecek sağlık bileşeni

    private bool hasValidUI = true;

    /// <summary>
    /// Dışarıdan sağlık referansı atamak için kullanılır.
    /// </summary>
    public CharacterHealth CharacterHealth
    {
        get => characterHealth;
        set => SetCharacterHealth(value);
    }

    private void Awake()
    {
        // FIX: Validate serialized UI references once on load
        hasValidUI = healthText != null && fillImage != null;
        if (!hasValidUI)
        {
            Debug.LogError("[HealthBarUI] Missing UI references", this);
            enabled = false;
        }
    }

    private void OnEnable()
    {
        if (!hasValidUI)
            return;

        if (characterHealth == null)
        {
            Debug.LogWarning("[HealthBarUI] CharacterHealth not assigned", this);
            return;
        }

        characterHealth.OnHealthChanged += UpdateHealthBar; // FIX: subscribe when enabled
        UpdateHealthBar(characterHealth.GetCurrentHealth());
    }

    private void OnDisable()
    {
        if (characterHealth != null)
            characterHealth.OnHealthChanged -= UpdateHealthBar; // FIX: unsubscribe when disabled
    }

    public void SetCharacterHealth(CharacterHealth newHealth)
    {
        if (characterHealth != null)
            characterHealth.OnHealthChanged -= UpdateHealthBar;

        characterHealth = newHealth;

        if (enabled && characterHealth != null && hasValidUI)
        {
            characterHealth.OnHealthChanged += UpdateHealthBar;
            UpdateHealthBar(characterHealth.GetCurrentHealth());
        }
    }

    private void UpdateHealthBar(float currentHealth)
    {
        float healthRatio = (characterHealth != null && characterHealth.maxHealth > 0f)
            ? currentHealth / characterHealth.maxHealth
            : 0f;

        // Barın doluluğu
        fillImage.fillAmount = healthRatio;
        healthText.text = $"{(int)currentHealth} / {(int)(characterHealth != null ? characterHealth.maxHealth : 0)}";

        // Renk geçişi (Yeşil → Sarı → Kırmızı)
        if (healthRatio > 0.6f)
        {
            // Yeşilden sarıya
            float t = (healthRatio - 0.6f) / 0.4f; // 0.6–1.0 → 0–1
            fillImage.color = Color.Lerp(Color.yellow, Color.green, t);
        }
        else
        {
            // Sarıdan kırmızıya
            float t = (healthRatio <= 0.6f && healthRatio >= 0f) ? healthRatio / 0.6f : 0f; // 0–0.6 → 0–1
            fillImage.color = Color.Lerp(Color.red, Color.yellow, t);
        }
    }

    private void LateUpdate()
    {
        transform.rotation = Quaternion.identity;
    }

    private void OnDestroy()
    {
        OnDisable(); // FIX: ensure event cleanup
    }
}

