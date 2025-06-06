// Assets/Scripts/Abilities/SuperJumpSkill.cs
using UnityEngine;

public class SuperJumpSkill : MonoBehaviour, IAbility
{
    public KeyCode ActivationKey { get; private set; }
    public bool IsSelected { get; set; }

    [Tooltip("Karakterin GravityBody bileşeni")]
    public GravityBody gravityBody;

    [Tooltip("Super jump onayı sonrası cooldown süresi (saniye)")]
    public float cooldownTime = 5f;
    private float cooldownTimer = 0f;
    private bool awaitingConfirmation = false;

    void Start()
    {
        // "0" tuşu super jump için atanır
        ActivationKey = KeyCode.Alpha5;
        IsSelected = false;
    }

    void Update()
    {
        // Cooldown çalışıyorsa azalt
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;

        // Eğer bu skill seçili değilse ya da cooldown devam ediyorsa hiçbir şey yapma
        if (!IsSelected || cooldownTimer > 0f)
            return;

        // "0" tuşuna basıldığında onay bekleme moduna gir
        if (Input.GetKeyDown(KeyCode.Alpha0) && !awaitingConfirmation)
        {
            awaitingConfirmation = true;
        }

        // Onay bekleme modundayken "Enter" tuşuna basılırsa süper zıplama aktif olsun
        if (awaitingConfirmation && Input.GetKeyDown(KeyCode.Return))
        {
            if (gravityBody != null)
                gravityBody.nextJumpIsSuper = true;

            awaitingConfirmation = false;
            cooldownTimer = cooldownTime;
            IsSelected = false;
        }
    }

    // IAbility arayüzündeki metot; burada doğrudan kullanılmıyor ama boş olarak implement edilmeli
    public void UseAbility() { }
}
