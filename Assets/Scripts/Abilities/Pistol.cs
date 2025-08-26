using UnityEngine;

[RequireComponent(typeof(GravityBody))]
public class Pistol : MonoBehaviour, IAbilitySelectable, ICooldownResettable
{
    [Header("Onay & Cooldown")]
    public KeyCode activationKey = KeyCode.Alpha1;
    public float cooldownTime = 5f;
    private float cooldownTimer;
    private bool awaitingConfirmation;
    private bool fireAllowed;

    [Header("Fire Settings")]
    public Transform firePoint;
    public GameObject projectilePrefab;
    public float maxDragDistance = 3f;
    public float powerMultiplier = 5f;
    public float ignoreOwnerDuration = 1f;

    // ---- KineticProjectile ayarları (TEK DELİK için) ----
    [Header("Kinetic (Patlamasız / Tek Delik)")]
    public float kMaxRange = 12f;
    [Range(0f, 1f)] public float kFullDamagePortion = 0.30f; // menzilin ilk %30'unda sabit hasar
    public float kMaxDamage = 8f;
    public float kMinDamage = 0f;

    [Header("Gezegen Etkileşimi (Tek Delik)")]
    public bool kDestroyOnPlanetHit = true;     // gezegene değince mermi yok olsun
    public bool kApplyPlanetDamage = true;     // delik aç
    public float kPlanetDamageRadius = 0.5f;    // TEK DAİRE delik yarıçapı
    public float kPlanetDamageForce = 2f;      // delik kuvveti

    [Header("Capsule/Oval Delik (KAPALI tut)")]
    public bool kUseElongatedPlanetDamage = false; // ✅ tek delik için kapalı
    public float kOvalLength = 6f;                 // kullanılmaz
    public float kOvalWidth = 3f;                 // kullanılmaz
    public int kOvalStamps = 1;                  // güvence: 1 damga

    [Header("Chunk / Katmanlar")]
    public LayerMask kPlanetChunkLayer; // gezegen parçaları
    public LayerMask kPlanetLayer;      // gezegen collider’ı (raycast için)
    public bool kUseTrigger = true;     // chunk içinden geçiş için önerilir
    public bool kStopOnDamageable = true; // karaktere çarpınca dur

    private LineRenderer lr;
    [SerializeField] private TrajectoryDots trajectory;
    private GravityBody gravityBody;
    private bool isDragging;
    private Vector2 dragStart;
    private bool wasActive;
    private CharacterAbilities charAbilities;
    private bool isSelected;

    public int SlotIndex => 0;

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
        if (lr != null)
        {
            lr.enabled = false;
            lr.positionCount = 0;
        }

        gravityBody = GetComponent<GravityBody>();

        trajectory = trajectory
                  ?? GetComponent<TrajectoryDots>()
                  ?? GetComponentInChildren<TrajectoryDots>(true)
#if UNITY_2022_2_OR_NEWER
                  ?? FindFirstObjectByType<TrajectoryDots>(FindObjectsInactive.Include);
#else
                  ?? FindObjectOfType<TrajectoryDots>(true);
#endif

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

        charAbilities = GetComponent<CharacterAbilities>();
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        awaitingConfirmation = selected;
        fireAllowed = false;
        if (!selected)
            CancelDrag();
    }

    public void Cancel()
    {
        awaitingConfirmation = false;
        fireAllowed = false;
        CancelDrag();
    }

    public void ResetCooldown()
    {
        cooldownTimer = 0f;
        awaitingConfirmation = false;
        fireAllowed = false;
        CancelDrag();
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
            Cancel();
        }
        else if (!gravityBody.isActive)
        {
            wasActive = false;
            return;
        }

        if (cooldownTimer > 0f)
        {
            CancelDrag();
            return;
        }

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

        if (!fireAllowed) return;

        Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        if (Input.GetMouseButtonDown(0))
        {
            isDragging = true;
            dragStart = mouseWorld;
        }
        else if (isDragging && Input.GetMouseButton(0))
        {
            Vector2 pull = dragStart - mouseWorld;
            float clamped = Mathf.Min(pull.magnitude, maxDragDistance);
            Vector2 initial = pull.normalized * clamped * powerMultiplier;
            float power01 = clamped / maxDragDistance;
            trajectory?.Show(initial, power01);
        }
        else if (isDragging && Input.GetMouseButtonUp(0))
        {
            bool canFire = charAbilities == null || charAbilities.UsePistol();
            if (canFire)
            {
                Fire();
                cooldownTimer = cooldownTime;
                charAbilities?.OnAbilityConsumed();
            }
            CancelDrag();
            fireAllowed = false;
            isSelected = false;
        }
    }

    private void Fire()
    {
        Vector2 pull = dragStart - (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);
        float clamped = Mathf.Min(pull.magnitude, maxDragDistance);
        Vector2 initial = pull.normalized * clamped * powerMultiplier;

        var bulletGO = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);

        // Eski patlamalı Projectile varsa sök, KineticProjectile kullan
        var oldProj = bulletGO.GetComponent<Projectile>();
        if (oldProj) Destroy(oldProj);

        var kin = bulletGO.GetComponent<KineticProjectile>();
        if (!kin) kin = bulletGO.AddComponent<KineticProjectile>();

        // Kinetic ayarları (tek delik)
        kin.maxRange = kMaxRange;
        kin.fullDamagePortion = kFullDamagePortion;
        kin.maxDamage = kMaxDamage;
        kin.minDamage = kMinDamage;

        kin.destroyOnPlanetHit = kDestroyOnPlanetHit;
        kin.applyPlanetDamage = kApplyPlanetDamage;
        kin.planetDamageRadius = kPlanetDamageRadius;
        kin.planetDamageForce = kPlanetDamageForce;

        // Tek delik için capsule kapalı
        kin.useElongatedPlanetDamage = kUseElongatedPlanetDamage; // false
        kin.ovalLength = kOvalLength;
        kin.ovalWidth = kOvalWidth;
        kin.ovalStamps = Mathf.Max(1, kOvalStamps); // güvence: 1

        kin.alignToVelocity = true;           // önemli değil (capsule kapalıyken)
        kin.anchorAtSurface = true;

        // Chunk & katmanlar
        kin.planetChunkLayer = kPlanetChunkLayer;
        kin.planetLayer = kPlanetLayer;
        kin.useTrigger = kUseTrigger;
        kin.stopOnDamageable = kStopOnDamageable;

        kin.Init(initial, gameObject, ignoreOwnerDuration);
    }

    private void CancelDrag()
    {
        isDragging = false;
        if (lr != null)
        {
            lr.enabled = false;
            lr.positionCount = 0;
        }
        trajectory?.Hide();
    }
}
