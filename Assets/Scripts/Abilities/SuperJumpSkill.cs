using UnityEngine;

public class SuperJumpSkill : MonoBehaviour, IAbilitySelectable
{
    [Header("Selection & Cooldown")]
    public KeyCode activationKey = KeyCode.Alpha5;
    public float cooldownTime = 5f;

    private float cooldownTimer;
    private bool awaitingConfirmation;
    private bool isSelected;

    [Tooltip("Karakterin GravityBody bileşeni")]
    public GravityBody gravityBody;

    private CharacterAbilities charAbilities;

    public int SlotIndex => 4;

    void Start()
    {
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

    void Update()
    {
        if (gravityBody == null || !gravityBody.isActive)
            return;

        if (charAbilities != null && charAbilities.HasUsedSkillThisTurn)
            return;

        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;

        if (!isSelected)
        {
            if (Input.GetKeyDown(activationKey))
                charAbilities?.SelectSkill(SlotIndex);
            return;
        }

        if (cooldownTimer > 0f)
            return;

        if (charAbilities.GetSuperJumpsRemaining() <= 0)
        {
            charAbilities?.DeselectAll();
            return;
        }

        if (awaitingConfirmation)
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                bool canUse = charAbilities == null || charAbilities.UseSuperJump();
                if (canUse)
                {
                    gravityBody.nextJumpIsSuper = true;
                    UIManager.Instance.ConfirmSkill(SlotIndex);
                    charAbilities?.OnAbilityConsumed();
                    cooldownTimer = cooldownTime;
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

    public void ResetCooldown()
    {
        cooldownTimer = 0f;
    }
}

