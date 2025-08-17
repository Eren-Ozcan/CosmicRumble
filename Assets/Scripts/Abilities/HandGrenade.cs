using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(GravityBody))]
public class HandGrenade : MonoBehaviour
{
    [Header("Onay & Cooldown")]
    public KeyCode activationKey = KeyCode.Alpha4;
    public float cooldownTime = 6f;
    private float cooldownTimer = 0f;
    private bool awaitingConfirmation = false;
    private bool fireAllowed = false;

    [Header("UI Filter & Count")]
    public Image filterImage;                   // Inspector’da atayacağın FilterImage
    public TextMeshProUGUI grenadeCountText;    // Inspector’da atayacağın skill sayısı text’i
    public Color selectionColor = new Color(1f, 1f, 0f, 0.5f); // sarı yarı saydam
    public Color confirmColor = new Color(0f, 1f, 0f, 0.5f); // yeşil yarı saydam
    public Color emptyColor = new Color(1f, 0f, 0f, 0.5f); // kırmızı yarı saydam

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

        if (filterImage != null)
            filterImage.color = Color.clear;
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

        // Skill seçimi (sadece ammo > 0 ise)
        if (Input.GetKeyDown(activationKey) && !awaitingConfirmation && !fireAllowed)
        {
            UIManager.Instance.HighlightSkill(3);

            if (charAbilities != null && charAbilities.GetGrenadesRemaining() == 0)
                return;

            awaitingConfirmation = true;
            if (filterImage != null)
                filterImage.color = selectionColor;  // sarı filtre
        }

        // Onay / iptal
        if (awaitingConfirmation)
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                UIManager.Instance.ConfirmSkill(3);
                fireAllowed = true;
                awaitingConfirmation = false;
                if (filterImage != null)
                    filterImage.color = confirmColor;  // yeşil filtre
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                awaitingConfirmation = false;
                if (filterImage != null)
                    filterImage.color = Color.clear;    // temizle
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
                charAbilities.OnAbilityConsumed();          // ✅ turn hakkı kullanıldı ve UI kilitlendi

                Fire();
                cooldownTimer = cooldownTime;
                UpdateAmmoUI();
                if (charAbilities.GetGrenadesRemaining() == 0 && filterImage != null)
                    filterImage.color = emptyColor;
            }

            CancelDrag();
            fireAllowed = false;

            // clear any selection highlight if still ammo remains
            if (canFire && charAbilities.GetGrenadesRemaining() > 0 && filterImage != null)
                filterImage.color = Color.clear;
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

    private void UpdateAmmoUI()
    {
        if (grenadeCountText != null && charAbilities != null)
            grenadeCountText.text = charAbilities.GetGrenadesRemaining().ToString();
    }
}
