using UnityEngine;

[RequireComponent(typeof(GravityBody))]
public class ShieldSkill : AbilityBase
{
    [Header("Kalkan Bileşenleri")]
    public CharacterHealth characterHealth;
    public SpriteRenderer spriteRenderer;

    [Header("Onay & Cooldown")]
    public KeyCode activationKey = KeyCode.Alpha6;
    public float cooldownTime = 5f;

    public override int SlotIndex => 5;
    public override KeyCode ActivationKey => activationKey;
    public override float CooldownTime => cooldownTime;

    private bool shieldActiveVisual;

    protected override void Awake()
    {
        base.Awake();

        if (characterHealth == null)
            characterHealth = GetComponent<CharacterHealth>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public override void SetSelected(bool selected)
    {
        base.SetSelected(selected);
        if (selected)
            UIManager.Instance?.ShowConfirmPrompt("Confirm Shield? [Enter]");
        else
            UIManager.Instance?.HideConfirmPrompt();
    }

    public override void Cancel()
    {
        base.Cancel();
        UIManager.Instance?.HideConfirmPrompt();
    }

    protected override void Update()
    {
        // Kalkan görseli senkronizasyonu — her frame kontrol et
        if (shieldActiveVisual && characterHealth != null && !characterHealth.isShielded)
        {
            spriteRenderer.color = Color.white;
            shieldActiveVisual = false;
        }

        base.Update();
    }

    protected override void OnFireUpdate()
    {
        bool canUse = charAbilities == null || charAbilities.UseShield();
        if (canUse)
        {
            characterHealth.isShielded = true;
            spriteRenderer.color = new Color(0.5f, 0.5f, 1f);
            shieldActiveVisual = true;
            cooldownTimer = cooldownTime;
            charAbilities?.OnAbilityConsumed();
        }
        UIManager.Instance?.HideConfirmPrompt();
        isSelected = false;
        fireAllowed = false;
    }
}
