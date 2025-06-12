// Assets/Scripts/Abilities/SuperJumpSkill.cs
using UnityEngine;

public class SuperJumpSkill : MonoBehaviour, IAbility
{
    [Header("Selection & Cooldown")]
    public KeyCode activationKey = KeyCode.Alpha5;
    public float cooldownTime = 5f;

    private float cooldownTimer = 0f;
    private bool awaitingConfirmation = false;

    public bool IsSelected { get; set; }
    public KeyCode ActivationKey => activationKey;

    [Tooltip("Karakterin GravityBody bileşeni")]
    public GravityBody gravityBody;

    private CharacterAbilities charAbilities;

    void Start()
    {
        charAbilities = GetComponent<CharacterAbilities>();
        IsSelected = false;
    }

    void Update()
    {
        if (gravityBody == null || !gravityBody.isActive)
            return;

        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;

        if (!IsSelected || cooldownTimer > 0f)
            return;

        if (charAbilities.GetSuperJumpsRemaining() <= 0)
        {
            Debug.LogWarning("[SuperJumpSkill] Hakkın kalmadı – seçim yapılmadı");
            return;
        }

        if (Input.GetKeyDown(activationKey) && !awaitingConfirmation)
        {
            Debug.Log("[SuperJumpSkill] 5 tuşuna basıldı – seçim yapılıyor");
            UIManager.Instance.HighlightSkill(4); // sarı filtre
            awaitingConfirmation = true;
        }

        if (awaitingConfirmation)
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                Debug.Log("[SuperJumpSkill] Enter tuşu algılandı – onaylama denenecek");

                gravityBody.nextJumpIsSuper = true;
                Debug.Log("[SuperJumpSkill] SuperJump hazırlandı – nextJumpIsSuper = true");

                UIManager.Instance.ConfirmSkill(4); // yeşil filtre

                cooldownTimer = cooldownTime;
                IsSelected = false;
                awaitingConfirmation = false;
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                awaitingConfirmation = false;
                UIManager.Instance.filterImages[4].color = Color.clear;
            }
        }
    }
    public void ResetCooldown()
    {
        cooldownTimer = 0f;
        Debug.Log("[SuperJumpSkill] Cooldown sıfırlandı (karakter değişimi)");
    }

    public void UseAbility() { }
}
