using UnityEngine;
using CosmicRumble.Achievements;

[RequireComponent(typeof(GravityBody))]
public class Bomb : AbilityBase
{
    [Header("Onay & Cooldown")]
    public KeyCode activationKey = KeyCode.Alpha0;
    public float cooldownTime = 8f;

    public override int SlotIndex => 9;
    public override KeyCode ActivationKey => activationKey;
    public override float CooldownTime => cooldownTime;

    [Header("Fire Settings")]
    public Transform firePoint;
    public GameObject projectilePrefab;
    public float maxDragDistance = 3f;
    public float powerMultiplier = 10f;

    [Header("Bomb Settings")]
    public float fuseTime = 2f;
    public GameObject explosionPrefab;

    private bool isDragging;
    private Vector2 dragStart;

    protected override void Awake()
    {
        base.Awake();
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
            Vector2 initial = pull.normalized * clamped * powerMultiplier;
            float power01 = (maxDragDistance <= 0f) ? 0f : clamped / maxDragDistance;
            trajectory?.Show(initial, power01);
        }
        else if (isDragging && PointerUp)
        {
            Fire();
            cooldownTimer = cooldownTime;
            charAbilities?.OnAbilityConsumed();
            CancelAim();
            fireAllowed = false;
            isSelected = false;
        }
    }

    private void Fire()
    {
        if (projectilePrefab == null || firePoint == null) return;

        Vector2 pull = dragStart - PointerWorldPosition;
        float clamped = Mathf.Min(pull.magnitude, maxDragDistance);
        Vector2 initial = pull.normalized * clamped * powerMultiplier;

        AchievementEvents.FireWeaponUsed("weapon_bomb");
        AudioManager.Instance?.PlaySfx("weapon_bomb_place");

        GameObject bombObj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        Rigidbody2D rb = bombObj.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.AddForce(initial, ForceMode2D.Impulse);

        BombBehaviour bb = bombObj.AddComponent<BombBehaviour>();
        bb.Init(fuseTime, explosionPrefab);
        // TurnManager bildirimleri BombBehaviour.Init() içinde yapılıyor
    }

    protected override void CancelAim()
    {
        isDragging = false;
        base.CancelAim(); // trajectory?.Hide()
    }
}
