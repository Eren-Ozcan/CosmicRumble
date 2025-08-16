using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(GravityBody))]
public class Pistol : BaseProjectileAbility
{
    [Header("Onay & Cooldown")]
    public KeyCode activationKey = KeyCode.Alpha1;
    public float cooldownTime = 5f;
    private float cooldownTimer = 0f;
    private bool awaitingConfirmation = false;
    private bool fireAllowed = false;

    [Header("UI Filter & Count")]
    public Image filterImage;
    public TextMeshProUGUI pistolCountText;
    public Color selectionColor = new Color(1f, 1f, 0f, 0.5f);
    public Color confirmColor = new Color(0f, 1f, 0f, 0.5f);
    public Color emptyColor = new Color(1f, 0f, 0f, 0.5f);

    protected override string WeaponKey => "Pistol";

    protected override void Awake()
    {
        base.Awake();
        UpdateAmmoUI();
        if (filterImage != null)
            filterImage.color = Color.clear;
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

        if (!CanSpawn())
        {
            if (filterImage != null)
                filterImage.color = emptyColor;
            return;
        }

        if (Input.GetKeyDown(activationKey) && !awaitingConfirmation && !fireAllowed)
        {
            UIManager.Instance.HighlightSkill(0);
            awaitingConfirmation = true;
            if (filterImage != null)
                filterImage.color = selectionColor;
        }

        if (awaitingConfirmation)
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                UIManager.Instance.ConfirmSkill(0);
                fireAllowed = true;
                awaitingConfirmation = false;
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
            float power01 = clamped / maxDragDistance;
            DrawTrajectory(initial, power01);
        }
        else if (isDragging && Input.GetMouseButtonUp(0))
        {
            if (CanSpawn())
            {
                Vector2 pull = dragStart - mouseWorld;
                float clamped = Mathf.Min(pull.magnitude, maxDragDistance);
                Vector2 initial = pull.normalized * clamped * powerMultiplier;

                Fire(initial);
                cooldownTimer = cooldownTime;

                if (charAbilities != null)
                    charAbilities.HasUsedSkillThisTurn = true;
                UIManager.Instance.LockAllSkillsUI();

                if (!CanSpawn() && filterImage != null)
                    filterImage.color = emptyColor;
            }

            CancelDrag();
            fireAllowed = false;

            if (CanSpawn() && filterImage != null)
                filterImage.color = Color.clear;
        }
    }

    protected override void UpdateAmmoUI()
    {
        if (pistolCountText != null)
            pistolCountText.text = (maxProjectiles - activeProjectiles).ToString();
    }
}
