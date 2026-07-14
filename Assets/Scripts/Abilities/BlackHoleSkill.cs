using Unity.Netcode;
using UnityEngine;
using CosmicRumble.Achievements;

[RequireComponent(typeof(GravityBody))]
public class BlackHoleSkill : AbilityBase
{
    [Header("Onay & Cooldown")]
    public KeyCode activationKey = KeyCode.Alpha9;
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
        Vector2 mouseWorld = Camera.main != null ? PointerWorldPosition : Vector2.zero;

        if (PointerDown)
        {
            isDragging = true;
            dragStart = mouseWorld;
        }
        else if (isDragging && PointerHeld)
        {
            Vector2 pull = dragStart - mouseWorld;
            float clamped = Mathf.Min(pull.magnitude, maxDragDistance);
            Vector2 initial = (clamped > 0f) ? pull.normalized * clamped * powerMultiplier : Vector2.zero;
            float power01 = (maxDragDistance <= 0f) ? 0f : clamped / maxDragDistance;
            trajectory?.Show(initial, power01);
        }
        else if (isDragging && PointerUp)
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

        Vector2 mouseWorld = Camera.main != null ? PointerWorldPosition : dragStart;

        Vector2 pull = dragStart - mouseWorld;
        float clamped = Mathf.Min(pull.magnitude, maxDragDistance);
        Vector2 initial = (clamped > 0f) ? pull.normalized * clamped * powerMultiplier : Vector2.zero;

        // Networked modda ateşleme isteği server'a taşınır (server gerçek Instantiate'ı yapıp
        // NetworkObject.Spawn ile tüm client'lara yayar) — offline hotseat'te (IsSpawned=false)
        // eski doğrudan yerel yol aynen çalışır.
        if (IsSpawned) FireServerRpc(initial);
        else SpawnAndInit(initial);
    }

    [ServerRpc]
    private void FireServerRpc(Vector2 initialVelocity)
    {
        if (!ServerCanAct) return;
        if (charAbilities != null && !charAbilities.ServerTryConsume(SlotIndex)) return;
        SpawnAndInit(initialVelocity);
    }

    private void SpawnAndInit(Vector2 initialVelocity)
    {
        AchievementEvents.FireAbilityUsed("skill_blackhole");
        AudioManager.Instance?.PlaySfx("skill_blackhole_activate");

        var go = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);

        var proj = go.GetComponent<BlackHoleProjectile>();
        if (proj != null)
        {
            if (IsSpawned) go.GetComponent<NetworkObject>().Spawn();
            proj.Init(initialVelocity, gameObject, ignoreOwnerDuration);
        }
        else
        {
            var rb = go.GetComponent<Rigidbody2D>();
            if (rb != null)
                rb.AddForce(initialVelocity, ForceMode2D.Impulse);
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
