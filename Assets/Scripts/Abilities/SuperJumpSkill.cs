// Assets/Scripts/Abilities/SuperJumpSkill.cs
using UnityEngine;

public class SuperJumpSkill : BaseProjectileAbility
{
    [Header("Selection & Cooldown")]
    public KeyCode activationKey = KeyCode.Alpha5;
    public float cooldownTime = 5f;

    private float cooldownTimer = 0f;
    private bool awaitingConfirmation = false;

    [Tooltip("Karakterin GravityBody bileşeni")]
    public GravityBody gravityBody;

    private CharacterAbilities charAbilities;

    void Start()
    {
        charAbilities = GetComponent<CharacterAbilities>();
    }

    void Update()
    {
        if (gravityBody == null || !gravityBody.isActive)
            return;

        if (charAbilities != null && charAbilities.HasUsedSkillThisTurn)
            return;

        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;

        if (cooldownTimer > 0f)
            return;

        if (awaitingConfirmation && UIManager.Instance != null && UIManager.Instance.SelectedIndex != UISlotIndex)
        {
            CancelSelectionInternal();
        }

        if (Input.GetKeyDown(activationKey) && !awaitingConfirmation)
        {
            if (charAbilities != null && charAbilities.GetSuperJumpsRemaining() <= 0)
            {
                Debug.LogWarning("[SuperJumpSkill] Hakkın kalmadı – seçim yapılmadı");
                return;
            }

            awaitingConfirmation = true;
            OnSelect();
        }

        if (!awaitingConfirmation)
            return;

        if (Input.GetKeyDown(KeyCode.Return))
        {
            gravityBody.nextJumpIsSuper = true;
            Debug.Log("[SuperJumpSkill] SuperJump hazırlandı");

            if (charAbilities != null)
                charAbilities.HasUsedSkillThisTurn = true;

            cooldownTimer = cooldownTime;

            UIManager.Instance.LockAllSkillsUI();
            OnConfirm();
            OnCancelSelection();
            awaitingConfirmation = false;
        }
        else if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(activationKey))
        {
            CancelSelectionInternal();
        }
    }

    public void ResetCooldown()
    {
        cooldownTimer = 0f;
        Debug.Log("[SuperJumpSkill] Cooldown sıfırlandı (karakter değişimi)");
    }

    private void CancelSelectionInternal()
    {
        awaitingConfirmation = false;
        OnCancelSelection();
    }
}
