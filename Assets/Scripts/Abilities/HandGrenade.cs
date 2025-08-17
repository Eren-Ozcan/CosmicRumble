using UnityEngine;
using TMPro;

[RequireComponent(typeof(GravityBody))]
public class HandGrenade : BaseProjectileAbility
{
    [Header("Onay & Cooldown")]
    public KeyCode activationKey = KeyCode.Alpha4;
    public float cooldownTime = 6f;
    private float cooldownTimer = 0f;
    private bool awaitingConfirmation = false;
    private bool fireAllowed = false;

    [Header("UI")]
    public TextMeshProUGUI grenadeCountText;

    [Header("Fire Settings")]
    public Transform firePoint;
    public GameObject projectilePrefab;
    public float maxDragDistance = 3f;
    public float powerMultiplier = 5f;
    public float ignoreOwnerDuration = 0.5f;

    private LineRenderer lr;

    [SerializeField]                 // İstersen Inspector’dan bağlayabilirsin; boşsa otomatik bulunur
    private TrajectoryDots trajectory;

    private GravityBody gravityBody;
    private bool isDragging;
    private Vector2 dragStart;
    private bool wasActive = false;

    // --- Ammo / UI ---
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

        // TrajectoryDots referansı: Inspector > aynı obje/child > sahnede ilk bul
        trajectory = trajectory
                  ?? GetComponent<TrajectoryDots>()
                  ?? GetComponentInChildren<TrajectoryDots>(true)
#if UNITY_2022_2_OR_NEWER
                  ?? FindFirstObjectByType<TrajectoryDots>(FindObjectsInactive.Include);
#else
                  ?? FindObjectOfType<TrajectoryDots>();
#endif
        if (trajectory != null)
        {
            // SADECE ANA SINIFTAN ÇEK (silah içinden ayar yapılmasın)
            trajectory.Setup(
                TrajectoryDots.GlobalDotCount,
                TrajectoryDots.GlobalTimeStep,
                firePoint
            );
            trajectory.startScale = TrajectoryDots.GlobalStartScale;
            trajectory.endScale = TrajectoryDots.GlobalEndScale;
        }

        // UI ve ammo
        charAbilities = GetComponent<CharacterAbilities>();
        if (charAbilities != null)
        {
            charAbilities.SkillChanged += OnSkillChanged; // sadece slot 3 için UI tazeleyelim
            UpdateAmmoUI();
        }
    }

    void OnDestroy()
    {
        if (charAbilities != null)
            charAbilities.SkillChanged -= OnSkillChanged;
    }

    // Sadece grenade slot’u değişince UI güncelle
    private void OnSkillChanged(int slotIndex)
    {
        if (slotIndex == 3) // slotIndex 3 = grenade
            UpdateAmmoUI();
    }

    void Update()
    {
        if (charAbilities != null && charAbilities.HasUsedSkillThisTurn)
            return; // Bu tur zaten skill kullanıldıysa çık

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

        if ((awaitingConfirmation || fireAllowed) && UIManager.Instance != null && UIManager.Instance.SelectedIndex != UISlotIndex)
        {
            CancelSelectionInternal();
        }

        // Skill seçimi (sadece ammo > 0 ise)
        if (Input.GetKeyDown(activationKey) && !awaitingConfirmation && !fireAllowed)
        {
            if (charAbilities != null && charAbilities.GetGrenadesRemaining() == 0)
                return;

            awaitingConfirmation = true;
            fireAllowed = false;
            OnSelect();
        }

        // Onay / iptal
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

        if (!fireAllowed)
            return;

        // Drag & fire
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

            // SADECE ANA SINIFTAN GELEN AYARLARLA çiz
            trajectory?.Show(initial, power01);
        }
        else if (isDragging && Input.GetMouseButtonUp(0))
        {
            // attempt to use one grenade
            bool canFire = true;
            if (charAbilities != null)
                canFire = charAbilities.UseGrenade();

            if (canFire)
            {
                charAbilities.HasUsedSkillThisTurn = true; // ✅ turn hakkı kullanıldı
                UIManager.Instance.LockAllSkillsUI();       // ✅ UI’ı kilitle

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
        var grenade = bulletGO.GetComponent<HandGrenadeProjectile>();
        if (grenade != null)
            grenade.Init(initial, gameObject, ignoreOwnerDuration);
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
        if (grenadeCountText != null && charAbilities != null)
            grenadeCountText.text = charAbilities.GetGrenadesRemaining().ToString();
    }
}
