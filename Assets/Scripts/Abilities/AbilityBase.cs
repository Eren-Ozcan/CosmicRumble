using UnityEngine;

/// <summary>
/// Tüm silah/ability'ler için ortak boilerplate.
/// Cooldown, onay akışı, wasActive takibi ve TrajectoryDots yönetimini sağlar.
/// Alt sınıflar: SlotIndex, ActivationKey, CooldownTime ve OnFireUpdate() tanımlamalı.
/// </summary>
public abstract class AbilityBase : MonoBehaviour, IAbilitySelectable, ICooldownResettable
{
    // ── Ortak field'lar ──────────────────────────────────────────
    protected float cooldownTimer;
    protected bool awaitingConfirmation;
    protected bool fireAllowed;
    protected bool isSelected;
    protected bool wasActive;
    protected GravityBody gravityBody;
    protected CharacterAbilities charAbilities;
    protected TrajectoryDots trajectory;

    // ── Alt sınıfın tanımlaması gerekenler ──────────────────────
    public abstract int SlotIndex { get; }
    public abstract KeyCode ActivationKey { get; }
    public abstract float CooldownTime { get; }

    // ── IAbilitySelectable ───────────────────────────────────────
    public virtual void SetSelected(bool selected)
    {
        isSelected = selected;
        awaitingConfirmation = selected;
        fireAllowed = false;
        if (!selected) CancelAim();
    }

    public virtual void Cancel()
    {
        awaitingConfirmation = false;
        fireAllowed = false;
        CancelAim();
    }

    // ── ICooldownResettable ──────────────────────────────────────
    public virtual void ResetCooldown()
    {
        cooldownTimer = 0f;
        awaitingConfirmation = false;
        fireAllowed = false;
        CancelAim();
    }

    // ── Unity lifecycle ──────────────────────────────────────────
    protected virtual void Awake()
    {
        gravityBody = GetComponent<GravityBody>();
        charAbilities = GetComponent<CharacterAbilities>();
        trajectory = GetComponent<TrajectoryDots>()
                  ?? GetComponentInChildren<TrajectoryDots>(true)
#if UNITY_2022_2_OR_NEWER
                  ?? FindFirstObjectByType<TrajectoryDots>(FindObjectsInactive.Include);
#else
                  ?? FindObjectOfType<TrajectoryDots>(true);
#endif
    }

    protected virtual void Update()
    {
        // 1. Tur kullanım kontrolü
        if (charAbilities != null && charAbilities.HasUsedSkillThisTurn) return;

        // 2. Cooldown azalt
        if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;

        // 3. isActive değişim tespiti
        if (gravityBody != null && gravityBody.isActive && !wasActive)
        {
            wasActive = true;
            Cancel();
        }
        else if (gravityBody != null && !gravityBody.isActive)
        {
            wasActive = false;
            return;
        }

        // 4. Cooldown dolmadıysa aim iptal
        if (cooldownTimer > 0f) { CancelAim(); return; }

        // 5. Seçili değilse aktivasyon tuşunu dinle
        if (!isSelected)
        {
            if (Input.GetKeyDown(ActivationKey))
                charAbilities?.SelectSkill(SlotIndex);
            return;
        }

        // 6. Onay bekleniyor
        if (awaitingConfirmation)
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                fireAllowed = true;
                awaitingConfirmation = false;
                UIManager.Instance?.ConfirmSkill(SlotIndex);
                TurnManager.NotifyWeaponConfirmed();
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                charAbilities?.DeselectAll();
            }
            return;
        }

        // 7. Ateş hazır değilse dur
        if (!fireAllowed) return;

        // 8. Alt sınıfın nişan/ateş mantığı
        OnFireUpdate();
    }

    // ── Alt sınıfın implement etmesi gerekenler ─────────────────
    /// <summary>fireAllowed=true olduğunda her frame çağrılır.</summary>
    protected abstract void OnFireUpdate();

    /// <summary>Nişan/drag görselini iptal eder. Override ile özelleştir.</summary>
    protected virtual void CancelAim()
    {
        trajectory?.Hide();
    }
}
