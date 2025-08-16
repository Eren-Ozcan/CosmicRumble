using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(GravityBody))]
public class RPG : BaseProjectileAbility
{
    protected override string WeaponKey => "RPG";
    [Header("Onay & Cooldown")]
    public KeyCode activationKey = KeyCode.Alpha3;
    public float cooldownTime = 7f;
    private float cooldownTimer = 0f;
    private bool awaitingConfirmation = false;
    private bool fireAllowed = false;

    [Header("UI Filter & Count")]
    public Image filterImage;
    public TextMeshProUGUI rpgCountText;
    public Color selectionColor = new Color(1f, 1f, 0f, 0.5f);
    public Color confirmColor = new Color(0f, 1f, 0f, 0.5f);
    public Color emptyColor = new Color(1f, 0f, 0f, 0.5f);

    [Header("Fire Settings")]
    public Transform firePoint;
    public GameObject projectilePrefab;
    public float maxDragDistance = 3f;
    public float powerMultiplier = 6f;
    public float ignoreOwnerDuration = 1.5f;

    private CharacterAbilities charAbilities;
    private bool isDragging;
    private Vector2 dragStart;
    private bool wasActive = false;

    protected override void Awake()
    {
        base.Awake();
        charAbilities = GetComponent<CharacterAbilities>();

        if (charAbilities != null)
        {
            charAbilities.RpgAmmoChanged += UpdateAmmoUI;
            UpdateAmmoUI();
        }

        if (filterImage != null)
            filterImage.color = Color.clear;
    }

    void OnDestroy()
    {
        if (charAbilities != null)
            charAbilities.RpgAmmoChanged -= UpdateAmmoUI;
    }

    void Update()
    {
        if (charAbilities.HasUsedSkillThisTurn)
            return; // Bu tur zaten skill kullanıldıysa çık

        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;

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

        if (Input.GetKeyDown(activationKey) && !awaitingConfirmation && !fireAllowed)
        {
            if (charAbilities != null && charAbilities.GetRpgAmmoRemaining() == 0)
                return;

            UIManager.Instance.HighlightSkill(2); // Slot index 2: RPG
            awaitingConfirmation = true;
            if (filterImage != null)
                filterImage.color = selectionColor;
        }

        if (awaitingConfirmation)
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                fireAllowed = true;
                awaitingConfirmation = false;
                UIManager.Instance.ConfirmSkill(2);
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
            ShowTrajectory(firePoint.position, initial, clamped / maxDragDistance);
        }
        else if (isDragging && Input.GetMouseButtonUp(0))
        {
            bool canFire = true;
            if (!CanFireProjectile())
                canFire = false;
            else if (charAbilities != null)
                canFire = charAbilities.UseRpg();

            if (canFire)
            {
                charAbilities.HasUsedSkillThisTurn = true;
                UIManager.Instance.LockAllSkillsUI();
                Fire();
                cooldownTimer = cooldownTime;
                UpdateAmmoUI();
                if (charAbilities.GetRpgAmmoRemaining() == 0 && filterImage != null)
                    filterImage.color = emptyColor;
            }

            CancelDrag();
            fireAllowed = false;

            if (canFire && charAbilities.GetRpgAmmoRemaining() > 0 && filterImage != null)
                filterImage.color = Color.clear;
        }
    }

    private void Fire()
    {
        Vector2 pull = dragStart - (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);
        float clamped = Mathf.Min(pull.magnitude, maxDragDistance);
        Vector2 initial = pull.normalized * clamped * powerMultiplier;

        var bulletGO = SpawnProjectile(projectilePrefab, firePoint.position, firePoint.rotation);
        if (bulletGO == null) return;
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
        HideTrajectory();
    }

    private void UpdateAmmoUI()
    {
        if (rpgCountText != null && charAbilities != null)
            rpgCountText.text = charAbilities.GetRpgAmmoRemaining().ToString();
    }
}
