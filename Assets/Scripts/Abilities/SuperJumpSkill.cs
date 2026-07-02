using UnityEngine;
using CosmicRumble.Achievements;

[RequireComponent(typeof(GravityBody))]
public class SuperJumpSkill : AbilityBase
{
    [Header("Onay & Cooldown")]
    public KeyCode activationKey = KeyCode.Alpha5;
    public float cooldownTime = 5f;

    public override int SlotIndex => 4;
    public override KeyCode ActivationKey => activationKey;
    public override float CooldownTime => cooldownTime;

    protected override void Awake()
    {
        base.Awake();
        gravityBody.onSuperJumpConsumed += ClearSuperJumpFilter;
    }

    private void OnDestroy()
    {
        if (gravityBody != null)
            gravityBody.onSuperJumpConsumed -= ClearSuperJumpFilter;
    }

    private void ClearSuperJumpFilter()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.filterImages[SlotIndex].color = Color.clear;
    }

    protected override void OnFireUpdate()
    {
        if (charAbilities != null && charAbilities.GetSuperJumpsRemaining() <= 0)
        {
            charAbilities.DeselectAll();
            isSelected = false;
            fireAllowed = false;
            return;
        }

        bool canUse = charAbilities == null || charAbilities.UseSuperJump();
        if (canUse)
        {
            gravityBody.nextJumpIsSuper = true;
            charAbilities?.OnAbilityConsumed();
            cooldownTimer = cooldownTime;
            AchievementEvents.FireAbilityUsed("skill_superjump");
            AudioManager.Instance?.PlaySfx("skill_superjump");
        }
        isSelected = false;
        fireAllowed = false;
    }
}
