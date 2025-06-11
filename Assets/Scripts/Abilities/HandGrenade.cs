using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(LineRenderer), typeof(GravityBody))]
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

    [Header("Trajectory Preview")]
    public int trajectoryPoints = 60;
    public float timeStep = 0.05f;

    private LineRenderer lr;
    private GravityBody gravityBody;
    private GravitySource[] gravitySources;
    private bool isDragging;
    private Vector2 dragStart;
    private bool wasActive = false;

    // --- NEW for ammo management ---
    private CharacterAbilities charAbilities;

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
        gravityBody = GetComponent<GravityBody>();
        gravitySources = FindObjectsOfType<GravitySource>();

        lr.enabled = false;
        lr.positionCount = 0;

        // UI ve ammo
        charAbilities = GetComponent<CharacterAbilities>();
        if (charAbilities != null)
        {
            // Generic SkillChanged event’ine abone ol
            charAbilities.SkillChanged += OnSkillChanged;
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
            // UI highlight
            UIManager.Instance.HighlightSkill(3);

            // check ammo
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
                // UI confirm
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
            lr.enabled = true;
            lr.positionCount = trajectoryPoints;
        }
        else if (isDragging && Input.GetMouseButton(0))
        {
            Vector2 pull = dragStart - mouseWorld;
            float clamped = Mathf.Min(pull.magnitude, maxDragDistance);
            Vector2 initial = pull.normalized * clamped * powerMultiplier;
            DrawTrajectory(initial);
        }
        else if (isDragging && Input.GetMouseButtonUp(0))
        {
            // attempt to use one grenade
            bool canFire = true;
            if (charAbilities != null)
                canFire = charAbilities.UseGrenade();

            if (canFire)
            {
                Fire();
                cooldownTimer = cooldownTime;
                UpdateAmmoUI();
                // if now empty, show red filter
                if (charAbilities.GetGrenadesRemaining() == 0 && filterImage != null)
                    filterImage.color = emptyColor;
            }

            lr.positionCount = 0;
            CancelDrag();
            fireAllowed = false;

            // clear any selection highlight if still ammo remains
            if (canFire && charAbilities.GetGrenadesRemaining() > 0 && filterImage != null)
                filterImage.color = Color.clear;
        }
    }

    private void DrawTrajectory(Vector2 initialVelocity)
    {
        if (!lr.enabled || lr.positionCount != trajectoryPoints)
        {
            lr.enabled = true;
            lr.positionCount = trajectoryPoints;
        }

        Vector2 pos = firePoint.position;
        Vector2 vel = initialVelocity;

        for (int i = 0; i < trajectoryPoints; i++)
        {
            Vector2 acc = Vector2.zero;
            foreach (var src in gravitySources)
            {
                Vector2 dir = (Vector2)src.transform.position - pos;
                float r2 = dir.sqrMagnitude;
                if (r2 < 0.001f) continue;
                acc += dir.normalized * (src.scaledGravityForce / r2);
            }

            vel += acc * timeStep;
            pos += vel * timeStep;

            if (i < lr.positionCount)
                lr.SetPosition(i, pos);
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
        lr.enabled = false;
        lr.positionCount = 0;
    }

    private void UpdateAmmoUI()
    {
        if (grenadeCountText != null && charAbilities != null)
            grenadeCountText.text = charAbilities.GetGrenadesRemaining().ToString();
    }
}
