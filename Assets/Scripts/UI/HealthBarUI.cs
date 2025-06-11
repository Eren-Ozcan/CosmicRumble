using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class HealthBarUI : MonoBehaviour
{
    public TextMeshProUGUI healthText;
    public Image fillImage;
    public CharacterHealth characterHealth;

    private void Start()
    {
        if (fillImage == null || characterHealth == null)
        {
            Debug.LogError("[HealthBarUI] Eksik referans!");
            enabled = false;
            return;
        }

        characterHealth.OnHealthChanged += UpdateHealthBar;
        UpdateHealthBar(characterHealth.GetCurrentHealth());
    }

    private void UpdateHealthBar(float currentHealth)
    {
        float healthRatio = currentHealth / characterHealth.maxHealth;

        // Barın doluluğu
        fillImage.fillAmount = healthRatio;
        healthText.text = $"{(int)currentHealth} / {(int)characterHealth.maxHealth}";

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
            float t = healthRatio / 0.6f; // 0–0.6 → 0–1
            fillImage.color = Color.Lerp(Color.red, Color.yellow, t);
        }
    }

    private void LateUpdate()
    {
        transform.rotation = Quaternion.identity;
    }
}
