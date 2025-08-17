using UnityEngine;

[RequireComponent(typeof(GravityBody))]
public class Shotgun : MonoBehaviour, IAbilitySelectable
{
    [Header("Onay & Cooldown")]
    public KeyCode activationKey = KeyCode.Alpha2;
    public float cooldownTime = 5f;
    private float cooldownTimer;
    private bool awaitingConfirmation;
    private bool fireAllowed;

    private GravityBody gravityBody;
    private bool wasActive;
    private CharacterAbilities charAbilities;
    private bool isSelected;

    public int SlotIndex => 1;

    void Awake()
    {
        gravityBody = GetComponent<GravityBody>();
        charAbilities = GetComponent<CharacterAbilities>();
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        awaitingConfirmation = selected;
        fireAllowed = false;
    }

    public void Cancel()
    {
        awaitingConfirmation = false;
        fireAllowed = false;
    }

    void Update()
    {
        if (charAbilities != null && charAbilities.HasUsedSkillThisTurn)
            return;

        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;

        if (gravityBody.isActive && !wasActive)
        {
            wasActive = true;
            cooldownTimer = 0f;
            Cancel();
        }
        else if (!gravityBody.isActive)
        {
            wasActive = false;
            return;
        }

        if (cooldownTimer > 0f)
            return;

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
                fireAllowed = true;
                awaitingConfirmation = false;
                UIManager.Instance.ConfirmSkill(SlotIndex);
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                charAbilities?.DeselectAll();
            }
            return;
        }

        if (!fireAllowed)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            bool canFire = charAbilities == null || charAbilities.UseShotgun();
            if (canFire)
            {
                Debug.Log("🔫 SHOTGUN ATEŞLENDİ!");
                cooldownTimer = cooldownTime;
                charAbilities?.OnAbilityConsumed();
            }
            fireAllowed = false;
            isSelected = false;
        }
    }
}

