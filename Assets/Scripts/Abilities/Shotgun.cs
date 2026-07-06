using Unity.Netcode;
using UnityEngine;
using CosmicRumble.Achievements;

[RequireComponent(typeof(GravityBody))]
public class Shotgun : AbilityBase
{
    [Header("Onay & Cooldown")]
    public KeyCode activationKey = KeyCode.Alpha2;
    public float cooldownTime = 5f;

    public override int SlotIndex => 1;
    public override KeyCode ActivationKey => activationKey;
    public override float CooldownTime => cooldownTime;

    [Header("Fire Settings (Pistol ile benzer)")]
    public Transform firePoint;
    public GameObject pelletPrefab;
    public float maxDragDistance = 3f;
    public float powerMultiplier = 5f;
    public float ignoreOwnerDuration = 0.5f;

    [Header("Pellet & Spread")]
    public int pelletCount = 5;
    public float totalSpreadAngle = 24f;

    [Header("Kinetic Projectile Ayarları")]
    public float pelletMaxRange = 1.152f;
    [Range(0f, 1f)] public float fullDamagePortion = 0.30f;
    public float pelletMaxDamage = 8f;
    public float pelletMinDamage = 0f;

    public bool destroyOnPlanetHit = true;
    public bool applyPlanetDamage = true;
    public float planetDamageRadius = 0.6f;
    public float planetDamageForce = 2f;

    [Header("Oval Delik Ayarları (Capsule)")]
    public bool useElongatedPlanetDamage = true;
    public float ovalLength = 6f;
    public float ovalWidth = 3f;
    public int ovalStamps = 7;
    public bool alignToVelocity = true;
    public bool anchorAtSurface = true;

    private LineRenderer lr;
    private ShotgunConePreview conePreview;
    private bool isDragging;
    private Vector2 dragStart;

    protected override void Awake()
    {
        base.Awake();

        lr = GetComponent<LineRenderer>();
        if (lr != null) { lr.enabled = false; lr.positionCount = 0; }

        var go = new GameObject("ShotgunConePreview");
        go.transform.SetParent(transform, false);
        conePreview = go.AddComponent<ShotgunConePreview>();
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
            if (firePoint != null)
                conePreview?.Show(firePoint.position, pull.normalized, totalSpreadAngle * 0.5f, pelletMaxRange, clamped / maxDragDistance);
        }
        else if (isDragging && PointerUp)
        {
            bool canFire = charAbilities == null || charAbilities.UseShotgun();
            if (canFire)
            {
                FirePellets();
                cooldownTimer = cooldownTime;
                charAbilities?.OnAbilityConsumed();
            }
            CancelAim();
            fireAllowed = false;
            isSelected = false;
        }
    }

    private void FirePellets()
    {
        Vector2 pull = dragStart - PointerWorldPosition;
        float clamped = Mathf.Min(pull.magnitude, maxDragDistance);
        Vector2 baseInitial = pull.normalized * clamped * powerMultiplier;

        // Networked modda tüm volley tek bir RPC ile server'a taşınır (server pellet döngüsünü
        // kendisi çalıştırır) — offline hotseat'te eski doğrudan yerel yol aynen çalışır.
        if (IsSpawned) FirePelletsServerRpc(baseInitial);
        else SpawnAllPellets(baseInitial);
    }

    [ServerRpc]
    private void FirePelletsServerRpc(Vector2 baseInitial) => SpawnAllPellets(baseInitial);

    private void SpawnAllPellets(Vector2 baseInitial)
    {
        float half = totalSpreadAngle * 0.5f;

        TurnManager.Instance?.RegisterShot();
        AchievementEvents.FireWeaponUsed("weapon_shotgun");
        AudioManager.Instance?.PlaySfx("weapon_shotgun_fire");

        for (int i = 0; i < pelletCount; i++)
        {
            float t = (pelletCount == 1) ? 0f : (float)i / (pelletCount - 1);
            float ang = Mathf.Lerp(-half, +half, t);
            Vector2 initial = (Quaternion.AngleAxis(ang, Vector3.forward) * baseInitial);

            var go = Instantiate(pelletPrefab, firePoint.position, firePoint.rotation);
            var kin = go.GetComponent<KineticProjectile>();

            kin.maxRange = pelletMaxRange;
            kin.fullDamagePortion = fullDamagePortion;
            kin.maxDamage = pelletMaxDamage;
            kin.minDamage = pelletMinDamage;

            kin.destroyOnPlanetHit = destroyOnPlanetHit;
            kin.applyPlanetDamage = applyPlanetDamage;
            kin.planetDamageRadius = planetDamageRadius;
            kin.planetDamageForce = planetDamageForce;

            kin.useElongatedPlanetDamage = useElongatedPlanetDamage;
            kin.ovalLength = ovalLength;
            kin.ovalWidth = ovalWidth;
            kin.ovalStamps = ovalStamps;
            kin.alignToVelocity = alignToVelocity;
            kin.anchorAtSurface = anchorAtSurface;

            if (IsSpawned) go.GetComponent<NetworkObject>().Spawn();

            TurnManager.NotifyProjectileLaunched();
            kin.Init(initial, gameObject, ignoreOwnerDuration);
        }
    }

    protected override void CancelAim()
    {
        isDragging = false;
        if (lr != null) { lr.enabled = false; lr.positionCount = 0; }
        conePreview?.Hide();
        base.CancelAim(); // trajectory null olduğu için Hide() no-op
    }
}
