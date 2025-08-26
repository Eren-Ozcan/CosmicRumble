using UnityEngine;

[RequireComponent(typeof(GravityBody))]
public class Teleport : MonoBehaviour, IAbilitySelectable, ICooldownResettable
{
    [Header("Onay & Cooldown")]
    public KeyCode activationKey = KeyCode.Alpha7;
    public float cooldownTime = 6f;
    private float cooldownTimer;
    private bool awaitingConfirmation;
    private bool fireAllowed;

    [Header("Teleport Timing")]
    [Tooltip("Orb firlatildiktan sonra kac saniye bekleyip teleport edecek")]
    public float teleportDelay = 2.5f;   // <-- Inspector’dan a

    [Header("Fire Settings")]
    public Transform firePoint;
    [Tooltip("TeleportOrbProjectile içeren PREFAB (Project penceresinden sürükle)")]
    public GameObject projectilePrefab;   // <- Inspector'da atanmalı!
    public float maxDragDistance = 3f;
    public float powerMultiplier = 5f;
    public float ignoreOwnerDuration = 0.6f;

    private LineRenderer lr;
    [SerializeField] private TrajectoryDots trajectory;
    private GravityBody gravityBody;
    private CharacterAbilities charAbilities;
    private bool isDragging;
    private Vector2 dragStart;
    private bool wasActive;
    private bool isSelected;

    // UI slot indexini kendi düzenine göre ayarla
    public int SlotIndex => 6;

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

        // --- Teşhis logu ---
        Debug.Log($"[Teleport/Awake] Owner={name}, Prefab={(projectilePrefab ? projectilePrefab.name : "NULL")}, FirePoint={(firePoint ? firePoint.name : "NULL")}");
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        awaitingConfirmation = selected;
        fireAllowed = false;
        if (!selected) CancelDrag();
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

        // Aktif oyuncuya geçince state temizle
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
            bool canFire = true;
            if (charAbilities != null)
                canFire = charAbilities.UseGrenade(); // geçici ortak sayaç; istersen UseTeleport() ekle

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
        // --- Güvenlik kontrolleri ---
        if (projectilePrefab == null)
        {
            Debug.LogError($"[Teleport/Fire] {name}: projectilePrefab NULL! Player prefab'ında Teleport bileşenine 'TeleportOrbProjectile' prefab'ını ATA.");
            return;
        }
        if (firePoint == null)
        {
            Debug.LogError($"[Teleport/Fire] {name}: firePoint NULL! Teleport'un Fire Point alanını ata.");
            return;
        }

        Vector2 pull = dragStart - (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);
        float clamped = Mathf.Min(pull.magnitude, maxDragDistance);
        Vector2 initial = pull.normalized * clamped * powerMultiplier;

        var go = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);

        // Tercihen TeleportOrbProjectile kullan
        var orb = go.GetComponent<TeleportOrbProjectile>();
        if (orb != null)
        {
            // Inspector’dan ayarlayacağın teleportDelay değerini projectile’a aktar
            orb.delayBeforeTeleport = teleportDelay;
            orb.Init(initial, gameObject, ignoreOwnerDuration);
        }
        else
        {
            // Fallback
            var rb = go.GetComponent<Rigidbody2D>();
            if (rb != null) rb.AddForce(initial, ForceMode2D.Impulse);
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
