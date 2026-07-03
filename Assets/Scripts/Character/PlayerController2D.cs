// Assets/Scripts/Character/PlayerController2D.cs
using UnityEngine;

/// <summary>
/// PlayerController2D:
/// - Yürüme animasyonu ve sprite yönünü yönetir.
/// - GravityBody içinde zaten hareket ve zıplama kodu olduğundan, burada yalnızca sprite flip işlemlerini yapıyoruz.
/// - Animator kısmı şimdilik kapalı (yorum satırlarında bırakıldı).
/// </summary>
[RequireComponent(typeof(GravityBody))]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerController2D : MonoBehaviour
{
    private GravityBody gravityBody;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;

    private void Awake()
    {
        gravityBody = GetComponent<GravityBody>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();

#if UNITY_EDITOR
        if (gravityBody == null)
            Debug.LogError($"[PlayerController2D] {name} üzerinde GravityBody bulunamadı!");
        if (spriteRenderer == null)
            Debug.LogError($"[PlayerController2D] {name} üzerinde SpriteRenderer bulunamadı!");
#endif
    }

    private void Update()
    {
        if (!gravityBody.isActive.Value) return;

        // Yüz yönü: karakterin yerel transform.right ekseni (gezegen rotasyonuna göre döner)
        // üzerindeki hız bileşenine bakarak flip yapılır.
        // Input.GetAxisRaw("Horizontal") dünya ekseni tabanlıdır; karakterin gezegen
        // yüzeyine göre döndüğü durumda yanlış yönü verir. Hız vektörü her zaman doğru.
        float lateralVel = Vector2.Dot(rb.linearVelocity, transform.right);
        if (lateralVel < -0.1f)
            spriteRenderer.flipX = true;
        else if (lateralVel > 0.1f)
            spriteRenderer.flipX = false;
    }
}
