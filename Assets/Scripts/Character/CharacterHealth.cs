// Assets/Scripts/Character/CharacterHealth.cs
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Karakterin canını yönetir, hasar alındığında günceller ve ölünce gerekli efektleri oynatır.
/// IDamageable arayüzünü uygulayarak dışarıdan hasar almayı sağlar.
/// Ayrıca, sağlık değiştiğinde OnHealthChanged event’ini tetikler.
///
/// Networked modda (IsSpawned=true) can sadece server tarafından değiştirilir — bir mermi her
/// client'ta ayrıca fizik simülasyonuyla var olduğu için (NGO transform/existence'ı replike eder,
/// collision event'lerini değil), TakeDamage her makinede bağımsız çağrılabilir; sadece server'ın
/// çağrısı gerçek mutasyona dönüşür, sonuç NetworkVariable ile tüm client'lara otomatik yayılır.
/// Offline hotseat'te (IsSpawned=false) eski davranış aynen korunur.
/// </summary>
public class CharacterHealth : NetworkBehaviour, IDamageable
{
    [Header("Can Ayarları")]
    [Tooltip("Karakterin maksimum can değeri")]
    public float maxHealth = 100f;
    private NetworkVariable<float> currentHealth =
        new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Header("Ölüm Efekti")]
    [Tooltip("Karakter öldüğünde instantiate edilecek efekt prefab'ı")]
    public GameObject deathEffectPrefab;
    [Tooltip("Ölüm efektinin oynatılmasından sonra GameObject'in yok edilme süresi")]
    public float destroyDelay = 0.5f;

    [Header("Zırh Ayarları")]
    [Range(0f, 1f), Tooltip("Shield aktifken hasarın ne kadarı engellenir (0 = engelsiz, 1 = tam koruma)")]
    public float shieldDamageReduction = 0.5f;

    // Server-yazımlı: ShieldSkill'in aktivasyonu sahibinin makinesinde tetiklenir (AbilityBase
    // owner-only gate), ama hasar indirimi TakeDamage içinde sadece server'da okunur — bu yüzden
    // plain bool yerine NetworkVariable olmalı, aksi halde uzak client'ın kalkanı server'a hiç
    // yansımaz ve hasar indirimi sessizce uygulanmaz. Offline hotseat'te (IsSpawned=false) normal
    // bir bool gibi davranır.
    private readonly NetworkVariable<bool> _isShielded =
        new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public bool isShielded => _isShielded.Value;

    /// <summary>Sadece server (veya offline'da herhangi bir çağıran) gerçekten değeri değiştirir.</summary>
    public void SetShielded(bool value)
    {
        if (!IsSpawned)
        {
            _isShielded.Value = value;
            OnShieldedChanged?.Invoke(value); // offline'da NetworkVariable.OnValueChanged tetiklenmez
            return;
        }
        if (IsServer) _isShielded.Value = value;
    }

    private bool isDead = false;

    // Sağlık değiştiğinde dışarıya bildirmek için event
    public event System.Action<float> OnHealthChanged;

    // Kalkan durumu değiştiğinde dışarıya bildirmek için event (görsel senkron — tüm peer'larda tetiklenir)
    public event System.Action<bool> OnShieldedChanged;

    private void Awake()
    {
        // Offline hotseat: IsSpawned henüz false, eski davranış aynen korunur.
        if (!IsSpawned)
        {
            currentHealth.Value = maxHealth;
            OnHealthChanged?.Invoke(currentHealth.Value);
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
            currentHealth.Value = maxHealth;

        currentHealth.OnValueChanged += (oldValue, newValue) => OnHealthChanged?.Invoke(newValue);
        OnHealthChanged?.Invoke(currentHealth.Value);

        _isShielded.OnValueChanged += (oldValue, newValue) => OnShieldedChanged?.Invoke(newValue);
        OnShieldedChanged?.Invoke(_isShielded.Value);
    }

    /// <summary>
    /// IDamageable arayüzü yöntemi: Karakter hasar aldığında çağrılır.
    /// </summary>
    /// <param name="amount">Alınan hasar miktarı</param>
    public void TakeDamage(float amount)
    {
        // Networked modda sadece server yetkilidir — diğer client'ların kendi lokal fizik
        // simülasyonundan gelen çağrıları no-op'tur (server'ın kendi çarpışma tespiti zaten
        // ayrıca çalışır ve gerçek mutasyonu oradan yapar).
        if (IsSpawned && !IsServer) return;
        if (isDead)
            return;

        // Eğer shield aktifse, hasarı %50 indir
        float finalDamage = isShielded ? amount * (1f - shieldDamageReduction) : amount;

        // Mevcut canı azalt, 0-maximum aralığında tut
        float newHealth = Mathf.Clamp(currentHealth.Value - finalDamage, 0f, maxHealth);
        currentHealth.Value = newHealth;

        // Offline hotseat'te NetworkVariable.OnValueChanged tetiklenmez (IsSpawned=false), event'i
        // burada da tetikle.
        if (!IsSpawned)
            OnHealthChanged?.Invoke(newHealth);

        // Eğer can 0 veya altına düştüyse ölme işlemini başlat
        if (newHealth <= 0f)
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
        return currentHealth.Value;
    }
}
