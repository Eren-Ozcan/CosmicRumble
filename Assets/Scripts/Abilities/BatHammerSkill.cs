using UnityEngine;

[RequireComponent(typeof(GravityBody))]
public class BatHammerSkill : MonoBehaviour, IAbilitySelectable, ICooldownResettable
{
    [Header("Activation & Cooldown")]
    public KeyCode activationKey = KeyCode.Alpha8;
    public float cooldownTime = 5f;
    public float hitRadius = 1.5f;
    public float knockbackForce = 10f;

    private float cooldownTimer;
    private bool awaitingConfirmation;
    private bool isSelected;
    private CharacterAbilities charAbilities;
    private GravityBody gravityBody;

    public int SlotIndex => 7;

    void Start()
    {
        gravityBody = GetComponent<GravityBody>();
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

        if (!isSelected)
        {
            if (Input.GetKeyDown(activationKey) || Input.GetKeyDown(KeyCode.Keypad8))
                charAbilities?.SelectSkill(SlotIndex);
            return;
        }

        if (cooldownTimer > 0f)
            return;

        if (awaitingConfirmation)
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                PerformKnockback();
                UIManager.Instance?.ConfirmSkill(SlotIndex);
                charAbilities?.OnAbilityConsumed();
                cooldownTimer = cooldownTime;
                isSelected = false;
                awaitingConfirmation = false;
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                charAbilities?.DeselectAll();
            }
        }
    }

    private void PerformKnockback()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, hitRadius);
        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject) continue;
            if (hit.CompareTag("Player"))
            {
                Rigidbody2D rb = hit.attachedRigidbody;
                if (rb != null)
                {
                    Vector2 dir = (hit.transform.position - transform.position).normalized;
                    rb.AddForce(dir * knockbackForce, ForceMode2D.Impulse);
                }
            }
        }

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

    public void ResetCooldown()
    {
        cooldownTimer = 0f;
        awaitingConfirmation = false;
        isSelected = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, hitRadius);
    }
}
