using UnityEngine;

public class ShieldSkill : MonoBehaviour, IAbilitySelectable, ICooldownResettable // ✨ DEĞİŞİKLİK
{
    public GravityBody gravityBody;
    public CharacterHealth characterHealth;
    public SpriteRenderer spriteRenderer;

    public KeyCode activationKey = KeyCode.Alpha6;
    public float cooldownTime = 5f;
    private float cooldownTimer;
    private bool awaitingConfirmation;
    private bool shieldActiveVisual;

    private CharacterAbilities charAbilities;
    private bool isSelected;

    public int SlotIndex => 5;

    void Start()
    {
        if (gravityBody == null)
            gravityBody = GetComponent<GravityBody>();
        if (characterHealth == null)
            characterHealth = GetComponent<CharacterHealth>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        charAbilities = GetComponent<CharacterAbilities>();
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        awaitingConfirmation = selected;
    }

    public void Cancel()
    {
        awaitingConfirmation = false;
    }

    public void ResetCooldown() // ✨ DEĞİŞİKLİK: Turn başında çağrılacak
    {
        cooldownTimer = 0f;
        awaitingConfirmation = false;
        isSelected = false;
    }

    void Update()
    {
        if (gravityBody == null || !gravityBody.isActive)
            return;

        if (charAbilities != null && charAbilities.HasUsedSkillThisTurn)
            return;

        // Eğer kalkan görselde aktif ama karakterHealth tarafında kapalıysa eski haline dön
        if (shieldActiveVisual && !characterHealth.isShielded)
        {
            spriteRenderer.color = Color.white;
            shieldActiveVisual = false;
        }

        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
            return;
        }

        if (!isSelected)
        {
            if (Input.GetKeyDown(activationKey))
                charAbilities?.SelectSkill(SlotIndex);
            return;
        }

        if (awaitingConfirmation)
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                bool canUse = charAbilities == null || charAbilities.UseShield();
                if (canUse)
                {
                    characterHealth.isShielded = true;
                    spriteRenderer.color = new Color(0.5f, 0.5f, 1f);
                    shieldActiveVisual = true;
                    UIManager.Instance.ConfirmSkill(SlotIndex);
                    cooldownTimer = cooldownTime;
                    charAbilities?.OnAbilityConsumed();
                }
                isSelected = false;
                awaitingConfirmation = false;
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                charAbilities?.DeselectAll();
            }
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
}
