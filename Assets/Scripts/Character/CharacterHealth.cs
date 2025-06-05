// Assets/Scripts/Character/CharacterHealth.cs
using UnityEngine;

/// <summary>
/// Karakterin canını yönetir, hasar alındığında günceller ve ölünce gerekli efektleri oynatır.
/// IDamageable arayüzünü uygulayarak dışarıdan hasar almayı sağlar.
/// Ayrıca, sağlık değiştiğinde OnHealthChanged event’ini tetikler.
/// </summary>
public class CharacterHealth : MonoBehaviour, IDamageable
{
    [Header("Can Ayarları")]
    [Tooltip("Karakterin maksimum can değeri")]
    public float maxHealth = 100f;
    private float currentHealth;

    [Header("Ölüm Efekti")]
    [Tooltip("Karakter öldüğünde instantiate edilecek efekt prefab'ı")]
    public GameObject deathEffectPrefab;
    [Tooltip("Ölüm efektinin oynatılmasından sonra GameObject'in yok edilme süresi")]
    public float destroyDelay = 0.5f;

    private bool isDead = false;

    // Sağlık değiştiğinde dışarıya bildirmek için event
    public event System.Action<float> OnHealthChanged;

    private void Awake()
    {
        // Oyuna başlarken canı maksimuma ayarla
        currentHealth = maxHealth;
        // Başlangıçta UI'ın doğru gösterebilmesi için event tetikle
        OnHealthChanged?.Invoke(currentHealth);
    }

    /// <summary>
    /// IDamageable arayüzü yöntemi: Karakter hasar aldığında çağrılır.
    /// </summary>
    /// <param name="amount">Alınan hasar miktarı</param>
    public void TakeDamage(float amount)
    {
        if (isDead)
            return;

        // Mevcut canı azalt, 0-maximum aralığında tut
        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        // Sağlık değiştiğinde event'i tetikle
        OnHealthChanged?.Invoke(currentHealth);

        // Eğer can 0 veya altına düştüyse ölme işlemini başlat
        if (currentHealth <= 0f)
            Die();
    }

    /// <summary>
    /// Karakter ölünce çağrılır: efekt oynat, collider ve hareketi kes, belirli süre sonra objeyi yok et.
    /// </summary>
    protected virtual void Die()
    {
        isDead = true;

        // Ölüm efekti varsa instantiate et
        if (deathEffectPrefab != null)
            Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);

        // Tüm diğer MonoBehaviour script'lerini devre dışı bırak
        MonoBehaviour[] scripts = GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour script in scripts)
        {
            if (script != this)
                script.enabled = false;
        }

        // Collider'ı kapat
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = false;

        // Rigidbody'yi kinematik moda geçir ve hızı sıfırla
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        // Belirtilen süre sonra GameObject'i yok et
        Destroy(gameObject, destroyDelay);
    }

    /// <summary>
    /// Mevcut can değerini döner.
    /// UIManager gibi diğer bileşenler bu metodu kullanarak can çubuğunu günceller.
    /// </summary>
    /// <returns>Şu anki can değeri</returns>
    public float GetCurrentHealth()
    {
        return currentHealth;
    }
}
