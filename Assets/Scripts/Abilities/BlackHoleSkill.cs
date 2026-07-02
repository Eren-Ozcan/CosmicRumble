using UnityEngine;
using CosmicRumble.Achievements;

[RequireComponent(typeof(GravityBody))]
public class BlackHoleSkill : AbilityBase
{
    [Header("Onay & Cooldown")]
    public KeyCode activationKey = KeyCode.Alpha8;
    public float cooldownTime = 8f;

    // Slot 8 corresponds to keyboard '9'
    public override int SlotIndex => 8;
    public override KeyCode ActivationKey => activationKey;
    public override float CooldownTime => cooldownTime;

    [Header("Fire Settings")]
    public Transform firePoint;
    public GameObject projectilePrefab;
    public float maxDragDistance = 3f;
    public float powerMultiplier = 8f;
    public float ignoreOwnerDuration = 0.5f;

    private LineRenderer lr;
    private bool isDragging;
    private Vector2 dragStart;

    protected override void Awake()
    {
        base.Awake();

        lr = GetComponent<LineRenderer>();
        if (lr != null)
        {
            lr.enabled = false;
            lr.positionCount = 0;
        }

        if (trajectory != null)
        {
            if (firePoint == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning("[BlackHoleSkill] firePoint is null, trajectory will not be set up.");
#endif
            }
            else
            {
                trajectory.Setup(
                    TrajectoryDots.GlobalDotCount,
                    TrajectoryDots.GlobalTimeStep,
                    firePoint
                );
                trajectory.startScale = TrajectoryDots.GlobalStartScale;
                trajectory.endScale = TrajectoryDots.GlobalEndScale;
            }
        }
    }

    protected override void OnFireUpdate()
    {
        Vector2 mouseWorld = Camera.main != null
            ? (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition)
            : Vector2.zero;

        if (Input.GetMouseButtonDown(0))
        {
            isDragging = true;
            dragStart = mouseWorld;
        }
        else if (isDragging && Input.GetMouseButton(0))
        {
            Vector2 pull = dragStart - mouseWorld;
            float clamped = Mathf.Min(pull.magnitude, maxDragDistance);
            Vector2 initial = (clamped > 0f) ? pull.normalized * clamped * powerMultiplier : Vector2.zero;
            float power01 = (maxDragDistance <= 0f) ? 0f : clamped / maxDragDistance;
            trajectory?.Show(initial, power01);
        }
        else if (isDragging && Input.GetMouseButtonUp(0))
        {
            bool canFire = charAbilities == null || charAbilities.UseBlackHole();
            if (canFire)
            {
                Fire();
                cooldownTimer = cooldownTime;
                charAbilities?.OnAbilityConsumed();
            }
            CancelAim();
            fireAllowed = false;
            isSelected = false;
        }
    }

    private void Fire()
    {
        if (projectilePrefab == null || firePoint == null)
        {
#if UNITY_EDITOR
            Debug.LogError("[BlackHoleSkill] projectilePrefab veya firePoint eksik!");
#endif
            return;
        }

        Vector2 mouseWorld = Camera.main != null
            ? (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition)
            : dragStart;

        Vector2 pull = dragStart - mouseWorld;
        float clamped = Mathf.Min(pull.magnitude, maxDragDistance);
        Vector2 initial = (clamped > 0f) ? pull.normalized * clamped * powerMultiplier : Vector2.zero;

        AchievementEvents.FireAbilityUsed("skill_blackhole");

        var go = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);

        var proj = go.GetComponent<BlackHoleProjectile>();
        if (proj != null)
        {
            proj.Init(initial, gameObject, ignoreOwnerDuration);
        }
        else
        {
            var rb = go.GetComponent<Rigidbody2D>();
            if (rb != null)
                rb.AddForce(initial, ForceMode2D.Impulse);
#if UNITY_EDITOR
            else
                Debug.LogWarning("[BlackHoleSkill] Projectile has no Rigidbody2D; applied no impulse.");
#endif
        }
    }

    protected override void CancelAim()
    {
        isDragging = false;
        if (lr != null)
        {
            lr.enabled = false;
            lr.positionCount = 0;
        }
        base.CancelAim(); // trajectory?.Hide()
    }
}
