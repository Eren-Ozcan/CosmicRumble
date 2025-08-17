using UnityEngine;
using TMPro;

[RequireComponent(typeof(GravityBody))]
public class RPG : BaseProjectileAbility
{
    [Header("Onay & Cooldown")]
    public KeyCode activationKey = KeyCode.Alpha3;
    public float cooldownTime = 7f;
    private float cooldownTimer = 0f;
    private bool awaitingConfirmation = false;
    private bool fireAllowed = false;

    [Header("UI")]
    public TextMeshProUGUI rpgCountText;

    [Header("Fire Settings")]
    public Transform firePoint;
    public GameObject projectilePrefab;
    public float maxDragDistance = 3f;
    public float powerMultiplier = 6f;
    public float ignoreOwnerDuration = 1.5f;

    private LineRenderer lr;

    [SerializeField]
    private TrajectoryDots trajectory;  // Inspector’dan atanabilir; yoksa otomatik bulunur

    private GravityBody gravityBody;
    private CharacterAbilities charAbilities;
    private bool isDragging;
    private Vector2 dragStart;
    private bool wasActive = false;

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

        // TrajectoryDots global ayarlarıyla kurulum (sadece firePoint’i günceller; count/step globalden gelir)
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
        if (charAbilities != null)
        {
            charAbilities.RpgAmmoChanged += UpdateAmmoUI;
            UpdateAmmoUI();
        }
    }

    void OnDestroy()
    {
        if (charAbilities != null)
            charAbilities.RpgAmmoChanged -= UpdateAmmoUI;
    }

    void Update()
    {
        // (Null güvenliği) Skill turn kilidi kontrolü
        if (charAbilities != null && charAbilities.HasUsedSkillThisTurn)
            return;

        // Cooldown
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;

        // Turn başlangıcı / bitişi
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

        if ((awaitingConfirmation || fireAllowed) && UIManager.Instance != null && UIManager.Instance.SelectedIndex != UISlotIndex)
        {
            CancelSelectionInternal();
        }

        // Skill seçimi (ammo > 0 ise)
        if (Input.GetKeyDown(activationKey) && !awaitingConfirmation && !fireAllowed)
        {
            if (charAbilities != null && charAbilities.GetRpgAmmoRemaining() == 0)
                return;

            awaitingConfirmation = true;
            fireAllowed = false;
            OnSelect();
        }

        if (awaitingConfirmation)
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                fireAllowed = true;
                awaitingConfirmation = false;
                OnConfirm();
            }
            else if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(activationKey))
            {
                CancelSelectionInternal();
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
            float power01 = (maxDragDistance <= 0f) ? 0f : clamped / maxDragDistance;

            // Trajectory dots (GLOBAL ayarlara göre)
            trajectory?.Show(initial, power01);
        }
        else if (isDragging && Input.GetMouseButtonUp(0))
        {
            bool canFire = true;
            if (charAbilities != null)
                canFire = charAbilities.UseRpg();

            if (canFire)
            {
                charAbilities.HasUsedSkillThisTurn = true; // turn skill hakkı kullanıldı
                UIManager.Instance.LockAllSkillsUI();      // tüm UI’ı gri yap

                Fire();
                cooldownTimer = cooldownTime;
                UpdateAmmoUI();
            }

            CancelDrag();
            fireAllowed = false;
            OnCancelSelection();
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

    private void CancelSelectionInternal()
    {
        awaitingConfirmation = false;
        fireAllowed = false;
        CancelDrag();
        OnCancelSelection();
    }

    private void UpdateAmmoUI()
    {
        if (rpgCountText != null && charAbilities != null)
            rpgCountText.text = charAbilities.GetRpgAmmoRemaining().ToString();
    }
}
