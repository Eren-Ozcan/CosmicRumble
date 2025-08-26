using UnityEngine;

[RequireComponent(typeof(GravityBody))]
public class TeleportDevice : MonoBehaviour, IAbilitySelectable, ICooldownResettable
{
    [Header("Hotkey & Cooldown")]
    public KeyCode activationKey = KeyCode.Alpha7;
    public float cooldownTime = 6f;
    private float cooldownTimer;

    [Header("Fire Settings")]
    public Transform firePoint;
    public TeleportOrbProjectile projectilePrefab;
    public float maxDragDistance = 3f;
    public float powerMultiplier = 5f;
    public float ignoreOwnerDuration = 0.6f;

    [Header("Visuals (opsiyonel)")]
    public LineRenderer lr;

    // internals
    private GravityBody gravityBody;
    private CharacterAbilities charAbilities;

    private bool isSelected;
    private bool awaitingConfirmation;
    private bool fireAllowed;
    private bool isDragging;
    private Vector2 dragStartWorld;

    // Hotbar index (0-based). 7. slot için 6:
    public int SlotIndex => 6;

    void Awake()
    {
        gravityBody = GetComponent<GravityBody>();
        charAbilities = GetComponent<CharacterAbilities>();

        if (lr == null) lr = GetComponent<LineRenderer>();
        if (lr != null) { lr.enabled = false; lr.positionCount = 0; }
    }

    void Update()
    {
        if (gravityBody == null || !gravityBody.isActive)
            return;

        if (charAbilities != null && charAbilities.HasUsedSkillThisTurn)
            return;

        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;

        if (!isSelected)
        {
            if (Input.GetKeyDown(activationKey) || Input.GetKeyDown(KeyCode.Keypad7))
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

        if (!fireAllowed || cooldownTimer > 0f)
            return;

        // Drag start
        if (Input.GetMouseButtonDown(0))
        {
            isDragging = true;
            dragStartWorld = GetMouseWorld();
            EnableLine(true);
        }
        // Drag loop
        else if (isDragging && Input.GetMouseButton(0))
        {
            Vector2 current = GetMouseWorld();
            Vector2 delta = Vector2.ClampMagnitude(dragStartWorld - current, maxDragDistance);
            Vector2 end = (Vector2)firePoint.position + delta;
            if (lr != null)
            {
                lr.positionCount = 2;
                lr.SetPosition(0, firePoint.position);
                lr.SetPosition(1, end);
            }
        }
        // Drag bırakıldı
        else if (isDragging && Input.GetMouseButtonUp(0))
        {
            FireOrb();
            isDragging = false;
            fireAllowed = false;
            isSelected = false;
        }
    }

    private Vector2 GetMouseWorld()
    {
        var cam = Camera.main;
        if (!cam) return Vector2.zero;
        Vector3 w = cam.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, -cam.transform.position.z));
        return new Vector2(w.x, w.y);
    }

    private bool CanUseNow()
    {
        if (cooldownTimer > 0f) return false;
        if (charAbilities != null && charAbilities.HasUsedSkillThisTurn) return false;
        return true;
    }

    private void FireOrb()
    {
        if (!CanUseNow() || projectilePrefab == null || firePoint == null)
        {
            Cancel();
            return;
        }

        // Hız vektörü: geriye çekme kadar
        Vector2 current = GetMouseWorld();
        Vector2 pull = Vector2.ClampMagnitude(dragStartWorld - current, maxDragDistance);
        Vector2 velocity = pull * powerMultiplier;

        // Spawn
        TeleportOrbProjectile orb = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        orb.Init(owner: gameObject, initialVelocity: velocity, ignoreOwnerTime: ignoreOwnerDuration);

        // cooldown / turn state / UI
        cooldownTimer = cooldownTime;
        charAbilities?.OnAbilityConsumed();

        // temizle
        awaitingConfirmation = false;
        fireAllowed = false;
        EnableLine(false);
    }

    private void EnableLine(bool on)
    {
        if (lr != null)
        {
            lr.enabled = on;
            if (!on) lr.positionCount = 0;
        }
    }

    // IAbilitySelectable
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        awaitingConfirmation = selected;
        fireAllowed = false;
        isDragging = false;
        EnableLine(false);
    }

    public void Cancel()
    {
        awaitingConfirmation = false;
        fireAllowed = false;
        isDragging = false;
        EnableLine(false);
    }

    // ICooldownResettable
    public void ResetCooldown()
    {
        cooldownTimer = 0f;
        awaitingConfirmation = false;
        fireAllowed = false;
        isSelected = false;
        isDragging = false;
        EnableLine(false);
    }
}