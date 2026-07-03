using UnityEngine;
using CosmicRumble.Achievements;

[RequireComponent(typeof(GravityBody))]
public class Teleport : AbilityBase
{
    [Header("Onay & Cooldown")]
    public KeyCode activationKey = KeyCode.Alpha7;
    public float cooldownTime = 6f;

    public override int SlotIndex => 6;
    public override KeyCode ActivationKey => activationKey;
    public override float CooldownTime => cooldownTime;

    [Header("Teleport Timing")]
    [Tooltip("Orb firlatildiktan sonra kac saniye bekleyip teleport edecek")]
    public float teleportDelay = 2.5f;

    [Header("Fire Settings")]
    public Transform firePoint;
    [Tooltip("TeleportOrbProjectile içeren PREFAB (Project penceresinden sürükle)")]
    public GameObject projectilePrefab;
    public float maxDragDistance = 3f;
    public float powerMultiplier = 8f;
    public float ignoreOwnerDuration = 0.6f;

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
            trajectory.Setup(
                TrajectoryDots.GlobalDotCount,
                TrajectoryDots.GlobalTimeStep,
                firePoint
            );
            trajectory.startScale = TrajectoryDots.GlobalStartScale;
            trajectory.endScale = TrajectoryDots.GlobalEndScale;
        }

#if UNITY_EDITOR
        Debug.Log($"[Teleport/Awake] Owner={name}, Prefab={(projectilePrefab ? projectilePrefab.name : "NULL")}, FirePoint={(firePoint ? firePoint.name : "NULL")}");
#endif
    }

    protected override void OnFireUpdate()
    {
        Vector2 mouseWorld = PointerWorldPosition;

        if (PointerDown)
        {
            isDragging = true;
            dragStart = mouseWorld;
        }
        else if (isDragging && PointerHeld)
        {
            Vector2 pull = dragStart - mouseWorld;
            float clamped = Mathf.Min(pull.magnitude, maxDragDistance);
            Vector2 initial = pull.normalized * clamped * powerMultiplier;
            float power01 = (maxDragDistance <= 0f) ? 0f : clamped / maxDragDistance;
            trajectory?.Show(initial, power01);
        }
        else if (isDragging && PointerUp)
        {
            bool canFire = charAbilities == null || charAbilities.UseTeleport();
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
        if (projectilePrefab == null)
        {
#if UNITY_EDITOR
            Debug.LogError($"[Teleport/Fire] {name}: projectilePrefab NULL! Player prefab'ında Teleport bileşenine 'TeleportOrbProjectile' prefab'ını ATA.");
#endif
            return;
        }
        if (firePoint == null)
        {
#if UNITY_EDITOR
            Debug.LogError($"[Teleport/Fire] {name}: firePoint NULL! Teleport'un Fire Point alanını ata.");
#endif
            return;
        }

        Vector2 pull = dragStart - PointerWorldPosition;
        float clamped = Mathf.Min(pull.magnitude, maxDragDistance);
        Vector2 initial = pull.normalized * clamped * powerMultiplier;

        AchievementEvents.FireAbilityUsed("skill_teleport");
        AudioManager.Instance?.PlaySfx("skill_teleport");

        var go = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);

        var orb = go.GetComponent<TeleportOrbProjectile>();
        if (orb != null)
        {
            orb.delayBeforeTeleport = teleportDelay;
            orb.Init(initial, gameObject, ignoreOwnerDuration);
        }
        else
        {
            // Fallback
            var rb = go.GetComponent<Rigidbody2D>();
            if (rb != null) rb.AddForce(initial, ForceMode2D.Impulse);
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
