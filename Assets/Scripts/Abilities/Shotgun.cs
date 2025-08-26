using UnityEngine;

[RequireComponent(typeof(GravityBody))]
public class Shotgun : MonoBehaviour, IAbilitySelectable, ICooldownResettable
{
    [Header("Onay & Cooldown")]
    public KeyCode activationKey = KeyCode.Alpha2;
    public float cooldownTime = 5f;
    private float cooldownTimer;
    private bool awaitingConfirmation;
    private bool fireAllowed;

    [Header("Fire Settings (Pistol ile benzer)")]
    public Transform firePoint;
    public GameObject pelletPrefab;
    public float maxDragDistance = 3f;
    public float powerMultiplier = 5f;
    public float ignoreOwnerDuration = 0.5f;

    [Header("Pellet & Spread")]
    public int pelletCount = 5;
    public float totalSpreadAngle = 12f;

    [Header("Kinetic Projectile Ayarları")]
    public float pelletMaxRange = 12f;
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

    [Header("Penetrasyon / Chunk")]
    public LayerMask planetChunkLayer;
    public LayerMask planetLayer;   // raycast için
    public bool useTrigger = true;
    public bool stopOnDamageable = true;

    [Header("Trajectory Override (Shotgun-only)")]
    [Tooltip("Sadece Shotgun için kısa preview kullan")]
    public bool overrideTrajectory = true;
    [Tooltip("Kısa ön izlemenin nokta sayısı (mesafeyi kısaltır)")]
    public int previewDotCount = 24;      // toplam süre ≈ dotCount * timeStep
    [Tooltip("Kısa ön izlemenin zaman adımı (mesafeyi kısaltır)")]
    public float previewTimeStep = 0.035f;  // 24 * 0.035 ≈ 0.84 sn

    private LineRenderer lr;
    [SerializeField] private TrajectoryDots trajectory; // child olarak atanacak
    private GravityBody gravityBody;
    private bool isDragging;
    private Vector2 dragStart;
    private bool wasActive;
    private CharacterAbilities charAbilities;
    private bool isSelected;

    public int SlotIndex => 1;

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
        if (lr != null) { lr.enabled = false; lr.positionCount = 0; }

        gravityBody = GetComponent<GravityBody>();
        charAbilities = GetComponent<CharacterAbilities>();

        // --- SADECE SHOTGUN'A ÖZEL KISA TRAJECTORY ---
        if (overrideTrajectory)
        {
            if (trajectory == null)
            {
                var go = new GameObject("ShotgunTrajectory");
                go.transform.SetParent(transform, false);
                trajectory = go.AddComponent<TrajectoryDots>();   // Awake() anında çalışır
            }

            // Global ayarları devre dışı bırak
            trajectory.useAsGlobalSettings = false;
            trajectory.ignoreExternalSetup = false;

            // SADECE mesafeyi kısalt (boyutlar globalle aynı kalacak)
            trajectory.dotCount = Mathf.Max(2, previewDotCount);
            trajectory.timeStep = Mathf.Max(0.0001f, previewTimeStep);

            // Boyutlar globalle aynı
            trajectory.startScale = TrajectoryDots.GlobalStartScale;
            trajectory.endScale = TrajectoryDots.GlobalEndScale;

            // Eğer sprite hâlâ yoksa geçici beyaz sprite ata (Knob.psd hatasını bastırmak için)
            if (trajectory.dotSprite == null)
            {
                var tex = Texture2D.whiteTexture;
                trajectory.dotSprite = Sprite.Create(
                    tex,
                    new Rect(0, 0, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f),
                    100f
                );
            }

            // Havuzu kur
            trajectory.Setup(trajectory.dotCount, trajectory.timeStep, firePoint);
        }
        else
        {
            // Global Trajectory kullan (diğer silahlarla aynı)
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
                trajectory.Setup(TrajectoryDots.GlobalDotCount, TrajectoryDots.GlobalTimeStep, firePoint);
                trajectory.startScale = TrajectoryDots.GlobalStartScale;
                trajectory.endScale = TrajectoryDots.GlobalEndScale;
            }
        }
    }

    void Start()
    {
        // Diğer bir TrajectoryDots varsa sprite'ı oradan kopyala ki tüm silahlarda görünüm aynı olsun
        if (overrideTrajectory && trajectory != null)
        {
#if UNITY_2022_2_OR_NEWER
            var all = FindObjectsByType<TrajectoryDots>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
            var all = FindObjectsOfType<TrajectoryDots>(true);
#endif
            foreach (var td in all)
            {
                if (!td || td == trajectory) continue;
                if (td.dotSprite != null)
                {
                    trajectory.dotSprite = td.dotSprite; // görünümü eşitle
                    break;
                }
            }

            // Boyutlar globalle aynı kalsın (güvence)
            trajectory.startScale = TrajectoryDots.GlobalStartScale;
            trajectory.endScale = TrajectoryDots.GlobalEndScale;

            // Yeniden kur (sprite/scale değiştiyse)
            trajectory.Setup(trajectory.dotCount, trajectory.timeStep, firePoint);
        }
    }

    public void SetSelected(bool selected) { isSelected = selected; awaitingConfirmation = selected; fireAllowed = false; if (!selected) CancelDrag(); }
    public void Cancel() { awaitingConfirmation = false; fireAllowed = false; CancelDrag(); }
    public void ResetCooldown() { cooldownTimer = 0f; awaitingConfirmation = false; fireAllowed = false; CancelDrag(); }

    void Update()
    {
        if (charAbilities != null && charAbilities.HasUsedSkillThisTurn) return;
        if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;

        if (gravityBody.isActive && !wasActive) { wasActive = true; Cancel(); }
        else if (!gravityBody.isActive) { wasActive = false; return; }

        if (cooldownTimer > 0f) { CancelDrag(); return; }

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

        if (Input.GetMouseButtonDown(0)) { isDragging = true; dragStart = mouseWorld; }
        else if (isDragging && Input.GetMouseButton(0))
        {
            Vector2 pull = dragStart - mouseWorld;
            float clamped = Mathf.Min(pull.magnitude, maxDragDistance);
            Vector2 initial = pull.normalized * clamped * powerMultiplier;
            trajectory?.Show(initial, clamped / maxDragDistance);
        }
        else if (isDragging && Input.GetMouseButtonUp(0))
        {
            bool canFire = charAbilities == null || charAbilities.UseShotgun();
            if (canFire)
            {
                FirePellets();
                cooldownTimer = cooldownTime;
                charAbilities?.OnAbilityConsumed();
            }
            CancelDrag(); fireAllowed = false; isSelected = false;
        }
    }

    private void FirePellets()
    {
        Vector2 pull = dragStart - (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);
        float clamped = Mathf.Min(pull.magnitude, maxDragDistance);
        Vector2 baseInitial = pull.normalized * clamped * powerMultiplier;

        float half = totalSpreadAngle * 0.5f;

        for (int i = 0; i < pelletCount; i++)
        {
            float t = (pelletCount == 1) ? 0f : (float)i / (pelletCount - 1);
            float ang = Mathf.Lerp(-half, +half, t);
            Vector2 initial = (Quaternion.AngleAxis(ang, Vector3.forward) * baseInitial);

            var go = Instantiate(pelletPrefab, firePoint.position, firePoint.rotation);

            // prefab'ta eski Projectile varsa kaldır, Kinetic garanti et
            var oldProj = go.GetComponent<Projectile>();
            if (oldProj) Destroy(oldProj);

            var kin = go.GetComponent<KineticProjectile>();
            if (!kin) kin = go.AddComponent<KineticProjectile>();

            // overrides
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

            kin.planetChunkLayer = planetChunkLayer;
            kin.planetLayer = planetLayer;
            kin.useTrigger = useTrigger;
            kin.stopOnDamageable = stopOnDamageable;

            kin.Init(initial, gameObject, ignoreOwnerDuration);
        }
    }

    private void CancelDrag()
    {
        isDragging = false;
        if (lr != null) { lr.enabled = false; lr.positionCount = 0; }
        trajectory?.Hide();
    }
}
