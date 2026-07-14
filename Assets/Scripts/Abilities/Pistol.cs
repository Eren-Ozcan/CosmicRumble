using Unity.Netcode;
using UnityEngine;
using CosmicRumble.Achievements;

[RequireComponent(typeof(GravityBody))]
public class Pistol : AbilityBase
{
    [Header("Onay & Cooldown")]
    public KeyCode activationKey = KeyCode.Alpha1;
    public float cooldownTime = 5f;

    public override int SlotIndex => 0;
    public override KeyCode ActivationKey => activationKey;
    public override float CooldownTime => cooldownTime;

    [Header("Fire Settings")]
    public Transform firePoint;
    public GameObject projectilePrefab;
    public float maxDragDistance = 3f;
    public float powerMultiplier = 8f;
    public float ignoreOwnerDuration = 1f;

    // ---- KineticProjectile ayarları (TEK DELİK için) ----
    [Header("Kinetic (Patlamasız / Tek Delik)")]
    public float kMaxRange = 12f;
    [Range(0f, 1f)] public float kFullDamagePortion = 0.30f;
    public float kMaxDamage = 8f;
    public float kMinDamage = 0f;

    [Header("Gezegen Etkileşimi (Tek Delik)")]
    public bool kDestroyOnPlanetHit = true;
    public bool kApplyPlanetDamage = true;
    public float kPlanetDamageRadius = 0.5f;
    public float kPlanetDamageForce = 2f;

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
            float power01 = clamped / maxDragDistance;
            trajectory?.Show(initial, power01);
        }
        else if (isDragging && PointerUp)
        {
            bool canFire = charAbilities == null || charAbilities.UsePistol();
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
        SpawnAndInit(ClampFireVelocity(initialVelocity, maxDragDistance, powerMultiplier));
    }

    private void SpawnAndInit(Vector2 initialVelocity)
    {
        var bulletGO = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
        var kin = bulletGO.GetComponent<KineticProjectile>();

        kin.maxRange = kMaxRange;
        kin.fullDamagePortion = kFullDamagePortion;
        kin.maxDamage = kMaxDamage;
        kin.minDamage = kMinDamage;

        kin.destroyOnPlanetHit = kDestroyOnPlanetHit;
        kin.applyPlanetDamage = kApplyPlanetDamage;
        kin.planetDamageRadius = kPlanetDamageRadius;
        kin.planetDamageForce = kPlanetDamageForce;

        kin.alignToVelocity = true;
        kin.anchorAtSurface = true;

        if (IsSpawned) bulletGO.GetComponent<NetworkObject>().Spawn();

        TurnManager.Instance?.RegisterShot();
        TurnManager.NotifyProjectileLaunched();
        AnnounceFire("weapon_pistol_fire", "weapon_pistol", isWeapon: true);
        kin.Init(initialVelocity, gameObject, ignoreOwnerDuration);
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
