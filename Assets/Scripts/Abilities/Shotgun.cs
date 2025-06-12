using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(GravityBody))]
public class Shotgun : MonoBehaviour
{
    [Header("Onay & Cooldown")]
    public KeyCode activationKey = KeyCode.Alpha2;
    public float cooldownTime = 5f;
    private float cooldownTimer = 0f;
    private bool awaitingConfirmation = false;
    private bool fireAllowed = false;

    [Header("UI Filter & Count")]
    public Image filterImage;
    public TextMeshProUGUI shotgunCountText;
    public Color selectionColor = new Color(1f, 1f, 0f, 0.5f);
    public Color confirmColor = new Color(0f, 1f, 0f, 0.5f);
    public Color emptyColor = new Color(1f, 0f, 0f, 0.5f);

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

        if (filterImage != null)
            filterImage.color = Color.clear;
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

        if (Input.GetKeyDown(activationKey) && !awaitingConfirmation && !fireAllowed)
        {
            UIManager.Instance.HighlightSkill(1); // Shotgun = index 1

            if (charAbilities != null && charAbilities.GetShotgunAmmo() == 0)
                return;

            awaitingConfirmation = true;
            if (filterImage != null)
                filterImage.color = selectionColor;
        }

        if (awaitingConfirmation)
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                UIManager.Instance.ConfirmSkill(1);

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

                if (charAbilities.GetShotgunAmmo() == 0 && filterImage != null)
                    filterImage.color = emptyColor;
            }

            fireAllowed = false;

            if (canFire && charAbilities.GetShotgunAmmo() > 0 && filterImage != null)
                filterImage.color = Color.clear;
        }
    }

    private void UpdateAmmoUI()
    {
        if (shotgunCountText != null && charAbilities != null)
            shotgunCountText.text = charAbilities.GetShotgunAmmo().ToString();
    }
    public void ResetCooldown()
    {
        cooldownTimer = 0f;
        fireAllowed = false;
        awaitingConfirmation = false;
    }

}
