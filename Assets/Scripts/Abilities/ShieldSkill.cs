using UnityEngine;

public class ShieldSkill : MonoBehaviour
{
    public GravityBody gravityBody;
    public CharacterHealth characterHealth;
    public SpriteRenderer spriteRenderer;

    public float cooldownTime = 5f;
    private float cooldownTimer = 0f;
    private bool awaitingConfirmation = false;
    private bool shieldActiveVisual = false;

    private CharacterAbilities charAbilities;
    private const int slotIndex = 5;

    void Start()
    {
        if (gravityBody == null)
            gravityBody = GetComponent<GravityBody>();
        if (characterHealth == null)
            characterHealth = GetComponent<CharacterHealth>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        charAbilities = GetComponent<CharacterAbilities>();

        if (gravityBody == null) Debug.LogError("[ShieldSkill] GravityBody yok!");
        if (characterHealth == null) Debug.LogError("[ShieldSkill] CharacterHealth yok!");
        if (spriteRenderer == null) Debug.LogError("[ShieldSkill] SpriteRenderer yok!");
    }

    void Update()
    {
        if (gravityBody == null || !gravityBody.isActive)
            return;

        if (shieldActiveVisual && !characterHealth.isShielded)
        {
            spriteRenderer.color = Color.white;
            shieldActiveVisual = false;

            // UI rengi de sıfırla
            bool isEmpty = charAbilities != null && charAbilities.GetShieldsRemaining() == 0;
            UIManager.Instance.ClearSkillColor(slotIndex, isEmpty);
        }

        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
            return;
        }

        if (!awaitingConfirmation && Input.GetKeyDown(KeyCode.Alpha6))
        {
            awaitingConfirmation = true;
            UIManager.Instance.HighlightSkill(slotIndex); // Sarıya boya
        }

        if (awaitingConfirmation && Input.GetKeyDown(KeyCode.Return))
        {
            characterHealth.isShielded = true;
            spriteRenderer.color = new Color(0.5f, 0.5f, 1f);
            shieldActiveVisual = true;

            awaitingConfirmation = false;
            cooldownTimer = cooldownTime;

            // UI senkronizasyonu
            UIManager.Instance.ConfirmSkill(slotIndex);

            if (charAbilities != null)
                charAbilities.UseShield();
        }

        if (awaitingConfirmation && Input.GetKeyDown(KeyCode.Escape))
        {
            awaitingConfirmation = false;
            bool isEmpty = charAbilities != null && charAbilities.GetShieldsRemaining() == 0;
            UIManager.Instance.ClearSkillColor(slotIndex, isEmpty);
        }
    }

    private void OnGUI()
    {
        if (gravityBody != null && gravityBody.isActive && awaitingConfirmation)
        {
            GUI.Label(
                new Rect(Screen.width / 2f - 150, Screen.height / 2f - 25, 300, 50),
                "Shield için emin misin? [Enter]"
            );
        }
    }
    public void ResetCooldown()
    {
        cooldownTimer = 0f;
    }

}
