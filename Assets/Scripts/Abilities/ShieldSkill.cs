// Assets/Scripts/Abilities/ShieldSkill.cs
using UnityEngine;

public class ShieldSkill : MonoBehaviour
{
    [Tooltip("Karakterin GravityBody bileşeni (aktif karakter kontrolü için)")]
    public GravityBody gravityBody;

    [Tooltip("Karakterin Health bileşeni")]
    public CharacterHealth characterHealth;

    [Tooltip("Karakterin SpriteRenderer bileşeni; rengi değiştirmek için")]
    public SpriteRenderer spriteRenderer;

    [Tooltip("Shield uygulandıktan sonra yeniden aktif olana kadar beklenen cooldown süresi")]
    public float cooldownTime = 5f;
    private float cooldownTimer = 0f;

    private bool awaitingConfirmation = false;
    private bool shieldActiveVisual = false;

    void Start()
    {
        // Gerekli component’ler atanmadıysa otomatik al
        if (gravityBody == null)
            gravityBody = GetComponent<GravityBody>();
        if (characterHealth == null)
            characterHealth = GetComponent<CharacterHealth>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (gravityBody == null)
            Debug.LogError("[ShieldSkill] GravityBody bulunamadı!");
        if (characterHealth == null)
            Debug.LogError("[ShieldSkill] CharacterHealth bulunamadı!");
        if (spriteRenderer == null)
            Debug.LogError("[ShieldSkill] SpriteRenderer bulunamadı!");
    }

    void Update()
    {
        // 0) Sadece aktif karakterin ShieldSkill’i dinlesin
        if (gravityBody == null || !gravityBody.isActive)
            return;

        // 1) Önce: Eğer önceki turda mavi kalan karaktere dönüldüyse 
        //    isShielded false hâle gelmiştir; hemen rengi sıfırla.
        if (shieldActiveVisual && !characterHealth.isShielded)
        {
            spriteRenderer.color = Color.white;
            shieldActiveVisual = false;
        }

        // 2) Cooldown varsa azalt ve girişleri dinleme
        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
            return;
        }

        // 3) “6” tuşuna basıldıysa onay ekranını aç
        if (!awaitingConfirmation && Input.GetKeyDown(KeyCode.Alpha6))
        {
            awaitingConfirmation = true;
            Debug.Log("[ShieldSkill] '6' tuşuna basıldı, onay bekleniyor...");
        }

        // 4) Onay ekranı açıkken “Enter” tuşuna basıldıysa shield aktif et
        if (awaitingConfirmation && Input.GetKeyDown(KeyCode.Return))
        {
            characterHealth.isShielded = true;

            // Karakterin rengini mavi tonuna çevir
            spriteRenderer.color = new Color(0.5f, 0.5f, 1f);
            shieldActiveVisual = true;

            awaitingConfirmation = false;
            cooldownTimer = cooldownTime;
            Debug.Log("[ShieldSkill] Enter basıldı, shield aktif edildi; cooldown başladı.");
        }
    }

    private void OnGUI()
    {
        // 5) Sadece aktif karakter ve awaitingConfirmation ise GUI çizecek
        if (gravityBody != null && gravityBody.isActive && awaitingConfirmation)
        {
            GUI.Label(
                new Rect(Screen.width / 2f - 150, Screen.height / 2f - 25, 300, 50),
                "Shield için emin misin? [Enter]"
            );
        }
    }
}
