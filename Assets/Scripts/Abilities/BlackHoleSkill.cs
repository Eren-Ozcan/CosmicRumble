using UnityEngine;

[RequireComponent(typeof(GravityBody))]
public class BlackHoleSkill : MonoBehaviour, IAbilitySelectable, ICooldownResettable
{
    [Header("Onay & Cooldown")]
    public KeyCode activationKey = KeyCode.Alpha9;
    public float cooldownTime = 8f;
    private float cooldownTimer;
    private bool awaitingConfirmation;
    private bool fireAllowed;

    [Header("Fire Settings")]
    public Transform firePoint;
    public GameObject projectilePrefab;
    public float maxDragDistance = 3f;
    public float powerMultiplier = 5f;
    public float ignoreOwnerDuration = 0.5f;

    private LineRenderer lr;
    [SerializeField] private TrajectoryDots trajectory;
    private GravityBody gravityBody;
    private CharacterAbilities charAbilities;
    private bool isDragging;
    private Vector2 dragStart;
    private bool wasActive;
    private bool isSelected;

    // Slot 8 corresponds to keyboard '9'
    public int SlotIndex => 8;

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
                  ?? FindObjectOfType<TrajectoryDots>();
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
        isSelected = false;
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
            if (Input.GetKeyDown(activationKey) || Input.GetKeyDown(KeyCode.Keypad9))
                charAbilities?.SelectSkill(SlotIndex);
            return;
        }

        if (awaitingConfirmation)
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                fireAllowed = true;
                awaitingConfirmation = false;
                UIManager.Instance?.ConfirmSkill(SlotIndex);
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                charAbilities?.DeselectAll();
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
            float power01 = (maxDragDistance <= 0f) ? 0f : clamped / maxDragDistance;
            trajectory?.Show(initial, power01);
        }
        else if (isDragging && Input.GetMouseButtonUp(0))
        {
            bool canFire = true; // sınırsız kullanım
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
        if (projectilePrefab == null || firePoint == null)
        {
            Debug.LogError("BlackHoleSkill: prefab veya firePoint eksik!");
            return;
        }

        Vector2 pull = dragStart - (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);
        float clamped = Mathf.Min(pull.magnitude, maxDragDistance);
        Vector2 initial = pull.normalized * clamped * powerMultiplier;

        var go = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
        var proj = go.GetComponent<BlackHoleProjectile>();
        if (proj != null)
            proj.Init(initial, gameObject, ignoreOwnerDuration);
        else
        {
            var rb = go.GetComponent<Rigidbody2D>();
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
}

