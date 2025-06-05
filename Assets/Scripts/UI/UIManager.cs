// Assets/Scripts/UI/UIManager.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro; // Eğer TextMeshPro kullanıyorsanız; yoksa Text yerine TextMeshProUGUI tipine göre düzenleyin.

public class UIManager : MonoBehaviour
{
    [Header("Oyuncu Yetkilendirmeleri (CharacterAbilities)")]
    public CharacterAbilities playerAbilities; // Inspector’dan Player GameObject’i sürüklenecek

    [Header("UI Text/Slider Referansları")]
    public TextMeshProUGUI superJumpText;
    public TextMeshProUGUI rpgAmmoText;
    public TextMeshProUGUI pistolAmmoText;
    public Slider healthBarSlider;

    private CharacterHealth playerHealth;

    private void Start()
    {
        // 1) playerAbilities null mı, kontrol et:
        if (playerAbilities == null)
        {
            Debug.LogWarning($"[{nameof(UIManager)}] 'playerAbilities' alanına bir CharacterAbilities referansı atanmamış!");
        }
        else
        {
            // Karakterin sağlık component’ini al
            playerHealth = playerAbilities.GetComponent<CharacterHealth>();
            if (playerHealth != null)
            {
                // Slider’ın maksimum değerini ayarla
                if (healthBarSlider != null)
                {
                    healthBarSlider.maxValue = playerHealth.maxHealth;
                    // Sağlık değiştiğinde Slider’ı güncelle
                    playerHealth.OnHealthChanged += OnHealthChanged;
                    // Başlangıçta güncelle
                    UpdateHealthUI();
                }
                else
                {
                    Debug.LogWarning($"[{nameof(UIManager)}] 'healthBarSlider' referansı atanmamış!");
                }
            }
            else
            {
                Debug.LogWarning($"[{nameof(UIManager)}] Player GameObject’inde CharacterHealth component’i bulunamadı!");
            }

            // Event’lere abone ol (CharacterAbilities içinde event’ler tanımlandıysa)
            playerAbilities.SuperJumpChanged += UpdateSuperJumpUI;
            playerAbilities.RpgAmmoChanged += UpdateRpgAmmoUI;
            playerAbilities.PistolAmmoChanged += UpdatePistolAmmoUI;
        }

        // Başlangıçta tüm UI’ı güncelle
        UpdateSuperJumpUI();
        UpdateRpgAmmoUI();
        UpdatePistolAmmoUI();
    }

    private void OnDestroy()
    {
        // Event aboneliklerinden çıkmak önemli
        if (playerAbilities != null)
        {
            playerAbilities.SuperJumpChanged -= UpdateSuperJumpUI;
            playerAbilities.RpgAmmoChanged -= UpdateRpgAmmoUI;
            playerAbilities.PistolAmmoChanged -= UpdatePistolAmmoUI;
        }

        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged -= OnHealthChanged;
        }
    }

    private void UpdateHealthUI()
    {
        if (playerHealth != null && healthBarSlider != null)
        {
            healthBarSlider.value = playerHealth.GetCurrentHealth();
        }
    }

    // CharacterHealth.OnHealthChanged event’i ile çağrılır
    private void OnHealthChanged(float newHealth)
    {
        if (healthBarSlider != null)
            healthBarSlider.value = newHealth;
    }

    private void UpdateSuperJumpUI()
    {
        if (playerAbilities != null && superJumpText != null)
        {
            superJumpText.text = "Super Jump: " + playerAbilities.GetSuperJumpsRemaining();
        }
    }

    private void UpdateRpgAmmoUI()
    {
        if (playerAbilities != null && rpgAmmoText != null)
        {
            rpgAmmoText.text = "RPG Ammo: " + playerAbilities.GetRpgAmmoRemaining();
        }
    }

    private void UpdatePistolAmmoUI()
    {
        if (playerAbilities != null && pistolAmmoText != null)
        {
            int p = playerAbilities.GetPistolAmmo();
            pistolAmmoText.text = p < 0 ? "Pistol Ammo: ∞" : "Pistol Ammo: " + p;
        }
    }
}
