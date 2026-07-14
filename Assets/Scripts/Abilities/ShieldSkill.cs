using Unity.Netcode;
using UnityEngine;
using CosmicRumble.Achievements;

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

    protected override void Awake()
    {
        base.Awake();

        if (characterHealth == null)
            characterHealth = GetComponent<CharacterHealth>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        if (characterHealth != null)
            characterHealth.OnShieldedChanged += OnShieldedChanged;
    }

    private void OnDisable()
    {
        if (characterHealth != null)
            characterHealth.OnShieldedChanged -= OnShieldedChanged;
    }

    // characterHealth.isShielded artık server-authoritative NetworkVariable ile taşınıyor —
    // bu event her peer'da (sahibi olsun olmasın) tetiklenir, o yüzden görsel artık
    // owner-only Update() polling yerine doğrudan bu event'e bağlı. Offline'da da SetShielded
    // event'i manuel tetiklediği için aynı kod yolu çalışır.
    private void OnShieldedChanged(bool shielded)
    {
        if (spriteRenderer != null)
            spriteRenderer.color = shielded ? new Color(0.5f, 0.5f, 1f) : Color.white;
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

    protected override void OnFireUpdate()
    {
        bool canUse = charAbilities == null || charAbilities.UseShield();
        if (canUse)
        {
            // Networked modda aktivasyon server'a taşınır (TakeDamage zaten sadece server'da
            // isShielded'ı okuyor) — offline hotseat'te eski doğrudan yol aynen çalışır.
            if (IsSpawned) ActivateShieldServerRpc();
            else characterHealth.SetShielded(true);

            cooldownTimer = cooldownTime;
            charAbilities?.OnAbilityConsumed();
            AchievementEvents.FireAbilityUsed("skill_shield");
            AudioManager.Instance?.PlaySfx("skill_shield_activate");
        }
        UIManager.Instance?.HideConfirmPrompt();
        isSelected = false;
        fireAllowed = false;
    }

    [ServerRpc]
    private void ActivateShieldServerRpc()
    {
        if (!ServerCanAct) return;
        characterHealth.SetShielded(true);
    }
}
