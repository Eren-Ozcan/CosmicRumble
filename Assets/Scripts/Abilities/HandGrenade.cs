using Unity.Netcode;
using UnityEngine;
using CosmicRumble.Achievements;

[RequireComponent(typeof(GravityBody))]
public class HandGrenade : AbilityBase
{
    [Header("Onay & Cooldown")]
    public KeyCode activationKey = KeyCode.Alpha4;
    public float cooldownTime = 6f;

    public override int SlotIndex => 3;
    public override KeyCode ActivationKey => activationKey;
    public override float CooldownTime => cooldownTime;

    [Header("Fire Settings")]
    public Transform firePoint;
    public GameObject projectilePrefab;
    public float maxDragDistance = 3f;
    public float powerMultiplier = 8f;
    public float ignoreOwnerDuration = 0.5f;

    [Header("Patlama Ayarları")]
    public float explosionRadius = 1f;
    public float explosionForce = 5f;
    public float maxDamage = 20f;
    public float delayBeforeExplosion = 6f;
    public float gravityForceMultiplier = 1f;

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
            bool canFire = charAbilities == null || charAbilities.UseGrenade();
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
    private void FireServerRpc(Vector2 initialVelocity)
    {
        if (!ServerCanAct) return;
        SpawnAndInit(initialVelocity);
    }

    private void SpawnAndInit(Vector2 initialVelocity)
    {
        AchievementEvents.FireWeaponUsed("weapon_grenade");
        AudioManager.Instance?.PlaySfx("weapon_grenade_throw");

        var bulletGO = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
        var grenade = bulletGO.GetComponent<HandGrenadeProjectile>();
        if (grenade != null)
        {
            grenade.explosionRadius = explosionRadius;
            grenade.explosionForce = explosionForce;
            grenade.maxDamage = maxDamage;
            grenade.delayBeforeExplosion = delayBeforeExplosion;
            grenade.gravityForceMultiplier = gravityForceMultiplier;

            if (IsSpawned) bulletGO.GetComponent<NetworkObject>().Spawn();

            grenade.Init(initialVelocity, gameObject, ignoreOwnerDuration);
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
