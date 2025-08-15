// Assets/Scripts/Character/PlayerController2D.cs
using UnityEngine;

/// <summary>
/// PlayerController2D:
/// - Yürüme animasyonu ve sprite yönünü yönetir.
/// - GravityBody içinde zaten hareket ve zıplama kodu olduğundan, burada yalnızca sprite flip işlemlerini yapıyoruz.
/// - Animator parametreleri yalnızca geçerli bir runtime controller olduğunda güncellenir.
/// </summary>
[RequireComponent(typeof(GravityBody))]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerController2D : MonoBehaviour
{
    private GravityBody gravityBody;
    private SpriteRenderer spriteRenderer;

    private Animator animator;

    [Header("Yürüme Ayarları")]
    [Tooltip("Animator içerisinde Speed parametresine gönderilecek değer.")]
    public string speedParam = "Speed";

    private void Awake()
    {
        gravityBody = GetComponent<GravityBody>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (gravityBody == null)
            Debug.LogError($"[PlayerController2D] {name} üzerinde GravityBody bulunamadı!");
        if (spriteRenderer == null)
            Debug.LogError($"[PlayerController2D] {name} üzerinde SpriteRenderer bulunamadı!");

        animator = GetComponent<Animator>();
        if (animator == null)
            Debug.LogError($"[PlayerController2D] {name} üzerinde Animator bulunamadı!");
    }

    private void Update()
    {
        // Eğer karakter şu an aktif değilse, animasyonları da güncelleme
        if (!gravityBody.isActive)
        {
            if (animator != null && animator.runtimeAnimatorController != null)
                animator.SetFloat(speedParam, 0f);
            return;
        }

        // Yürüme: GravityBody zaten yatay hızı ayarlıyor, burada yalnızca sprite flip yapıyoruz
        float horizInput = Input.GetAxisRaw("Horizontal"); // -1, 0 veya 1

        // Sprite yönünü güncelle
        if (horizInput < 0f)
            spriteRenderer.flipX = true;
        else if (horizInput > 0f)
            spriteRenderer.flipX = false;

        if (animator != null && animator.runtimeAnimatorController != null)
            animator.SetFloat(speedParam, Mathf.Abs(horizInput));
    }
}
