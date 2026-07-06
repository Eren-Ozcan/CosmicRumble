using Unity.Netcode;
using UnityEngine;
using CosmicRumble.Achievements;

[RequireComponent(typeof(GravityBody))]
public class RPG : AbilityBase
{
    [Header("Onay & Cooldown")]
    public KeyCode activationKey = KeyCode.Alpha3;
    public float cooldownTime = 7f;

    public override int SlotIndex => 2;
    public override KeyCode ActivationKey => activationKey;
    public override float CooldownTime => cooldownTime;

    [Header("Fire Settings")]
    public Transform firePoint;
    public GameObject projectilePrefab;
    public float maxDragDistance = 3f;
    public float powerMultiplier = 8f;
    public float ignoreOwnerDuration = 1.5f;

    [Header("Patlama Ayarları")]
    public float explosionRadius = 1f;
    public float explosionForce = 5f;
    public float maxDamage = 10f;
    public float projectileTTL = 15f;

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
            bool canFire = charAbilities == null || charAbilities.UseRpg();
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
        Vector2 pull = dragStart - PointerWorldPosition;
        float clamped = Mathf.Min(pull.magnitude, maxDragDistance);
        Vector2 initial = pull.normalized * clamped * powerMultiplier;

        // Networked modda ateşleme isteği server'a taşınır — offline hotseat'te eski doğrudan
        // yerel yol aynen çalışır.
        if (IsSpawned) FireServerRpc(initial);
        else SpawnAndInit(initial);
    }

    [ServerRpc]
    private void FireServerRpc(Vector2 initialVelocity) => SpawnAndInit(initialVelocity);

    private void SpawnAndInit(Vector2 initialVelocity)
    {
        AchievementEvents.FireWeaponUsed("weapon_rpg");
        AudioManager.Instance?.PlaySfx("weapon_rpg_fire");

        var bulletGO = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
        var proj = bulletGO.GetComponent<Projectile>();
        if (proj != null)
        {
            proj.explosionRadius = explosionRadius;
            proj.explosionForce = explosionForce;
            proj.maxDamage = maxDamage;
            proj.timeToLive = projectileTTL;

            if (IsSpawned) bulletGO.GetComponent<NetworkObject>().Spawn();

            proj.Init(initialVelocity, gameObject, ignoreOwnerDuration);
        }
        else
        {
            var rb = bulletGO.GetComponent<Rigidbody2D>();
            rb?.AddForce(initialVelocity, ForceMode2D.Impulse);
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
