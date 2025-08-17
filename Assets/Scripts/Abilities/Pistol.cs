using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(GravityBody))]
public class Pistol : MonoBehaviour
{
    [Header("Onay & Cooldown")]
    public KeyCode activationKey = KeyCode.Alpha1;
    public float cooldownTime = 5f;
    private float cooldownTimer = 0f;
    private bool awaitingConfirmation = false;
    private bool fireAllowed = false;

    [Header("UI Filter & Count")]
    public Image filterImage;                   // Inspector’da atayacağın FilterImage
    public TextMeshProUGUI pistolCountText;     // Inspector’da atayacağın skill sayısı text’i
    public Color selectionColor = new Color(1f, 1f, 0f, 0.5f); // sarı yarı saydam
    public Color confirmColor = new Color(0f, 1f, 0f, 0.5f);   // yeşil yarı saydam
    public Color emptyColor = new Color(1f, 0f, 0f, 0.5f);     // kırmızı yarı saydam

    [Header("Fire Settings")]
    public Transform firePoint;
    public GameObject projectilePrefab;
    public float maxDragDistance = 3f;
    public float powerMultiplier = 5f;
    public float ignoreOwnerDuration = 1f;


    private LineRenderer lr;

    [SerializeField]
    private TrajectoryDots trajectory;         // Inspector’dan bağlanabilir; yoksa otomatik bulunur

    private GravityBody gravityBody;
    private bool isDragging;
    private Vector2 dragStart;
    private bool wasActive = false;

    // --- NEW for ammo management ---
    private CharacterAbilities charAbilities;

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
        if (lr != null)
        {
            lr.enabled = false;
            lr.positionCount = 0;
        }

        gravityBody = GetComponent<GravityBody>();

        // TrajectoryDots referansı: önce Inspector, sonra aynı obje/child, sonra sahnede ara
        trajectory = trajectory
                  ?? GetComponent<TrajectoryDots>()
                  ?? GetComponentInChildren<TrajectoryDots>(true)
#if UNITY_2022_2_OR_NEWER
                  ?? FindFirstObjectByType<TrajectoryDots>(FindObjectsInactive.Include);
#else
                  ?? FindObjectOfType<TrajectoryDots>(true);
#endif

        // TrajectoryDots global ayarlarıyla kurulum: sadece firePoint’i set ediyor (ignoreExternalSetup true ise)
        if (trajectory != null)
        {
            trajectory.Setup(
                TrajectoryDots.GlobalDotCount,
                TrajectoryDots.GlobalTimeStep,
                firePoint
            );

            // Görsel ölçekleri de globalden çek (TrajectoryDots içindeki değerlerle eşleşsin)
            trajectory.startScale = TrajectoryDots.GlobalStartScale;
            trajectory.endScale = TrajectoryDots.GlobalEndScale;
        }

        // get CharacterAbilities and subscribe to its ammo-change event
        charAbilities = GetComponent<CharacterAbilities>();
        if (charAbilities != null)
        {
            charAbilities.PistolAmmoChanged += UpdateAmmoUI;
            UpdateAmmoUI();
        }

        if (filterImage != null)
            filterImage.color = Color.clear;
    }

    void OnDestroy()
    {
        // clean up subscription
        if (charAbilities != null)
            charAbilities.PistolAmmoChanged -= UpdateAmmoUI;
    }

    void Update()
    {
        if (charAbilities != null && charAbilities.HasUsedSkillThisTurn)
            return;

        // Cooldown
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;

        // Turn start / end reset
        if (gravityBody.isActive && !wasActive)
        {
            wasActive = true;
            cooldownTimer = 0f;
            fireAllowed = false;
            awaitingConfirmation = false;
            CancelDrag();
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

        // Skill seçimi (sadece ammo > 0 ise)
        if (Input.GetKeyDown(activationKey) && !awaitingConfirmation && !fireAllowed)
        {
            UIManager.Instance.HighlightSkill(0);

            if (charAbilities != null && charAbilities.GetPistolAmmo() == 0)
                return;

            awaitingConfirmation = true;
            if (filterImage != null)
                filterImage.color = selectionColor;
        }

        if (awaitingConfirmation)
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                UIManager.Instance.ConfirmSkill(0);
                fireAllowed = true;
                awaitingConfirmation = false;
                if (filterImage != null)
                    filterImage.color = confirmColor;
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                awaitingConfirmation = false;
                if (filterImage != null)
                    filterImage.color = Color.clear;
            }
            return;
        }

        if (!fireAllowed)
            return;

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

            // Trajectory dots (GLOBAL ayarlara göre)
            trajectory?.Show(initial, power01);
        }
        else if (isDragging && Input.GetMouseButtonUp(0))
        {
            bool canFire = true;
            if (charAbilities != null)
                canFire = charAbilities.UsePistol();

            if (canFire)
            {
                Fire(); // mermi fırlat
                cooldownTimer = cooldownTime;
                UpdateAmmoUI();

                // Skill kullanıldığı için bu turn başka skill kullanımı engellenir
                charAbilities.OnAbilityConsumed();

                // Eğer mermi bittiyse kırmızı filtre göster
                if (charAbilities.GetPistolAmmo() == 0 && filterImage != null)
                    filterImage.color = emptyColor;
            }

            CancelDrag();
            fireAllowed = false;

            if (canFire && charAbilities.GetPistolAmmo() > 0 && filterImage != null)
                filterImage.color = Color.clear;
        }
    }

    private void Fire()
    {
        Vector2 pull = dragStart - (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);
        float clamped = Mathf.Min(pull.magnitude, maxDragDistance);
        Vector2 initial = pull.normalized * clamped * powerMultiplier;

        var bulletGO = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
        var proj = bulletGO.GetComponent<Projectile>();
        if (proj != null)
            proj.Init(initial, gameObject, ignoreOwnerDuration);
        else
        {
            var rb = bulletGO.GetComponent<Rigidbody2D>();
            rb?.AddForce(initial, ForceMode2D.Impulse);
        }
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

    private void UpdateAmmoUI()
    {
        if (pistolCountText != null && charAbilities != null)
            pistolCountText.text = charAbilities.GetPistolAmmo().ToString();
    }
}
