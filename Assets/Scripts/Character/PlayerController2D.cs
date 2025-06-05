// Assets/Scripts/Character/PlayerController2D.cs
using UnityEngine;

/// <summary>
/// PlayerController2D:
/// - Yürüme animasyonu ve sprite yönünü yönetir.
/// - GravityBody içinde zaten hareket ve zıplama kodu olduğundan, burada yalnızca animasyonları ve flip işlemlerini yapıyoruz.
/// - Eskiden kullanılan isOnPlanet ve CurrentSource referansları, GravityBody.currentSource ile değiştirildi.
/// </summary>
[RequireComponent(typeof(GravityBody))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerController2D : MonoBehaviour
{
    private GravityBody gravityBody;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    [Header("Yürüme Ayarları")]
    [Tooltip("Animator içerisinde Speed parametresine gönderilecek değer.")]
    public string speedParam = "Speed";

    private void Awake()
    {
        gravityBody = GetComponent<GravityBody>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (gravityBody == null)
            Debug.LogError($"[PlayerController2D] {name} üzerinde GravityBody bulunamadı!");
        if (animator == null)
            Debug.LogError($"[PlayerController2D] {name} üzerinde Animator bulunamadı!");
        if (spriteRenderer == null)
            Debug.LogError($"[PlayerController2D] {name} üzerinde SpriteRenderer bulunamadı!");
    }

    private void Update()
    {
        // Eğer karakter şu an aktif değilse, animasyonları da güncelleme
        if (!gravityBody.isActive)
        {
            animator.SetFloat(speedParam, 0f);
            return;
        }

        // Yürüme: GravityBody zaten yatay hızı ayarlıyor, burada animator ve flip yapıyoruz
        float horizInput = Input.GetAxisRaw("Horizontal"); // -1, 0 veya 1

        // Sprite yönünü güncelle
        if (horizInput < 0f)
            spriteRenderer.flipX = true;
        else if (horizInput > 0f)
            spriteRenderer.flipX = false;

        // Animator Speed parametresini güncelle (yürüme animasyonu için)
        animator.SetFloat(speedParam, Mathf.Abs(horizInput));

        // (Zıplama animasyonu gerekiyorsa, Animator içerisinde bir bool parametre tanımlayıp 
        //  GravityBody.currentSource != null kontrolüne göre set edebilirsiniz.
        //  Örneğin:
        //  bool grounded = (gravityBody.currentSource != null);
        //  animator.SetBool("IsGrounded", grounded);
        //)
    }
}
