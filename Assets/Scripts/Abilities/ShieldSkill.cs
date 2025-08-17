using UnityEngine;

public class ShieldSkill : BaseProjectileAbility
{
    public GravityBody gravityBody;
    public CharacterHealth characterHealth;
    public SpriteRenderer spriteRenderer;

    public KeyCode activationKey = KeyCode.Alpha6;
    public float cooldownTime = 5f;
    private float cooldownTimer = 0f;
    private bool awaitingConfirmation = false;
    private bool shieldActiveVisual = false;

    private CharacterAbilities charAbilities;

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

            UIManager.Instance.SetConfirmed(UISlotIndex, false);
        }

        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
            return;
        }

        if (!awaitingConfirmation && Input.GetKeyDown(activationKey))
        {
            awaitingConfirmation = true;
            OnSelect();
        }

        if (awaitingConfirmation && UIManager.Instance != null && UIManager.Instance.SelectedIndex != UISlotIndex)
        {
            CancelSelectionInternal();
        }

        if (awaitingConfirmation && Input.GetKeyDown(KeyCode.Return))
        {
            characterHealth.isShielded = true;
            spriteRenderer.color = new Color(0.5f, 0.5f, 1f);
            shieldActiveVisual = true;

            awaitingConfirmation = false;
            cooldownTimer = cooldownTime;

            OnConfirm();

            if (charAbilities != null)
            {
                charAbilities.UseShield();
                charAbilities.HasUsedSkillThisTurn = true;
            }
            UIManager.Instance.LockAllSkillsUI();
        }

        if (awaitingConfirmation && (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(activationKey)))
        {
            CancelSelectionInternal();
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

    private void CancelSelectionInternal()
    {
        awaitingConfirmation = false;
        OnCancelSelection();
    }
}
