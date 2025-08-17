using UnityEngine;
using TMPro;

[RequireComponent(typeof(GravityBody))]
public class Shotgun : BaseProjectileAbility
{
    [Header("Onay & Cooldown")]
    public KeyCode activationKey = KeyCode.Alpha2;
    public float cooldownTime = 5f;
    private float cooldownTimer = 0f;
    private bool awaitingConfirmation = false;
    private bool fireAllowed = false;

    [Header("UI")]
    public TextMeshProUGUI shotgunCountText;

    private GravityBody gravityBody;
    private bool wasActive = false;
    private CharacterAbilities charAbilities;

    void Awake()
    {
        gravityBody = GetComponent<GravityBody>();

        charAbilities = GetComponent<CharacterAbilities>();
        if (charAbilities != null)
        {
            charAbilities.ShotgunAmmoChanged += UpdateAmmoUI;
            UpdateAmmoUI();
        }
    }

    void OnDestroy()
    {
        if (charAbilities != null)
            charAbilities.ShotgunAmmoChanged -= UpdateAmmoUI;
    }

    void Update()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;

        if (gravityBody.isActive && !wasActive)
        {
            wasActive = true;
            cooldownTimer = 0f;
            fireAllowed = false;
            awaitingConfirmation = false;
        }
        else if (!gravityBody.isActive)
        {
            wasActive = false;
            return;
        }

        if (cooldownTimer > 0f)
            return;

        if ((awaitingConfirmation || fireAllowed) && UIManager.Instance != null && UIManager.Instance.SelectedIndex != UISlotIndex)
        {
            CancelSelectionInternal();
        }

        if (Input.GetKeyDown(activationKey) && !awaitingConfirmation && !fireAllowed)
        {
            if (charAbilities != null && charAbilities.GetShotgunAmmo() == 0)
                return;

            awaitingConfirmation = true;
            fireAllowed = false;
            OnSelect();
        }

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

        // Ateşle
        if (Input.GetMouseButtonDown(0))
        {
            bool canFire = true;
            if (charAbilities != null)
                canFire = charAbilities.UseShotgun();

            if (canFire)
            {
                Debug.Log("🔫 SHOTGUN ATEŞLENDİ!");

                cooldownTimer = cooldownTime;
                UpdateAmmoUI();

                if (charAbilities != null)
                    charAbilities.HasUsedSkillThisTurn = true;
                UIManager.Instance.LockAllSkillsUI();
            }

            fireAllowed = false;
            OnCancelSelection();
        }
    }

    private void UpdateAmmoUI()
    {
        if (shotgunCountText != null && charAbilities != null)
            shotgunCountText.text = charAbilities.GetShotgunAmmo().ToString();
    }

    private void CancelSelectionInternal()
    {
        awaitingConfirmation = false;
        fireAllowed = false;
        OnCancelSelection();
    }
}
