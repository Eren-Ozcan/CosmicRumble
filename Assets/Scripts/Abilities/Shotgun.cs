using UnityEngine;

[RequireComponent(typeof(GravityBody))]
public class Shotgun : MonoBehaviour, IAbilitySelectable
{
    [Header("Onay & Cooldown")]
    public KeyCode activationKey = KeyCode.Alpha2;
    public float cooldownTime = 5f;
    private float cooldownTimer;
    private bool awaitingConfirmation;
    private bool fireAllowed;

    [Header("Fire Settings")]
    public Transform firePoint;
    public GameObject pelletPrefab;
    public float pelletSpeed = 25f;
    public float pelletRange = 15f;
    public float pelletDamage = 20f;
    [Range(0f, 1f)] public float damageFalloffStart = 0.3f;
    public int pelletCount = 5;
    public float spreadAngle = 15f;

    private GravityBody gravityBody;
    private bool wasActive;
    private CharacterAbilities charAbilities;
    private bool isSelected;

    public int SlotIndex => 1;

    void Awake()
    {
        gravityBody = GetComponent<GravityBody>();
        charAbilities = GetComponent<CharacterAbilities>();
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        awaitingConfirmation = selected;
        fireAllowed = false;
    }

    public void Cancel()
    {
        awaitingConfirmation = false;
        fireAllowed = false;
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
            cooldownTimer = 0f;
            Cancel();
        }
        else if (!gravityBody.isActive)
        {
            wasActive = false;
            return;
        }

        if (cooldownTimer > 0f)
            return;

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

        if (Input.GetMouseButtonDown(0))
        {
            bool canFire = charAbilities == null || charAbilities.UseShotgun();
            if (canFire)
            {
                Fire();
                cooldownTimer = cooldownTime;
                charAbilities?.OnAbilityConsumed();
            }
            fireAllowed = false;
            isSelected = false;
        }
    }

    private void Fire()
    {
        if (pelletPrefab == null || firePoint == null) return;

        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 baseDir = ((Vector2)mousePos - (Vector2)firePoint.position).normalized;

        for (int i = 0; i < pelletCount; i++)
        {
            float angle = spreadAngle * (i - (pelletCount - 1) / 2f);
            Vector2 dir = Quaternion.Euler(0, 0, angle) * baseDir;
            var pelletObj = Instantiate(pelletPrefab, firePoint.position, Quaternion.identity);
            var pellet = pelletObj.GetComponent<ShotgunPellet>();
            if (pellet != null)
            {
                pellet.Init(
                    dir * pelletSpeed,
                    pelletRange,
                    pelletDamage,
                    damageFalloffStart,
                    gameObject
                );
            }
            else
            {
                var rb = pelletObj.GetComponent<Rigidbody2D>();
                rb?.AddForce(dir * pelletSpeed, ForceMode2D.Impulse);
            }
        }
    }
}

