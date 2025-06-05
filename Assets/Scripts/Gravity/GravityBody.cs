// Assets/Scripts/Gravity/GravityBody.cs
using UnityEngine;

/// <summary>
/// GravityBody:
/// - Yerçekimi alanındaki karakteri çeker ve yüzeye göre hizalar.
/// - Tangent doğrultusunda yürüme/sürtünme, ani durma (smoothing).
/// - Zıplama: normal veya süper (iki katı). Her tür zıplama sonrası 10 saniyelik genel cooldown.
/// - Süper zıplama için önce 5 tuşuna bas, ardından Enter ile onayla; onay sonrası bir sonraki W tuşu süper zıplama yapar.
/// - Havada ilave zıplama (double jump) yok.
/// - Turn-based sistem için isActive, OnTurnStart ve ZeroHorizontalVelocity.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class GravityBody : MonoBehaviour
{
    [HideInInspector] public bool isActive = false; // TurnManager’dan true/false ayarlanır

    private Rigidbody2D rb;
    private GravitySource currentSource;

    [Header("Yürüme Ayarları")]
    [Tooltip("Karakter yerdeyken max yürüme hızı")]
    public float maxWalkSpeed = 3f;
    [Tooltip("Karakter havadayken yürüme/hareket hızı (tangent yönü)")]
    public float maxAirSpeed = 3f;
    [Range(0f, 1f)]
    [Tooltip("0 = anında dur, 1 = hiçbir yavaşlama olmasın")]
    public float smoothing = 0f;

    [Header("Zıplama Ayarları")]
    [Tooltip("Normal zıplama kuvveti")]
    public float jumpForce = 5f;
    [Tooltip("Süper zıplama kuvveti = jumpForce * superMultiplier")]
    public float superMultiplier = 2f;
    [Tooltip("Zıplama sonrası cooldown süresi (saniye)")]
    public float jumpCooldown = 10f;

    private float cooldownTimer = 0f;
    private bool awaitingSuperConfirmation = false;
    private bool nextJumpIsSuper = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        isActive = false;
    }

    private void Update()
    {
        // Cooldown sayaç
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;

        if (!isActive)
            return;

        bool grounded = (currentSource != null);

        // Süper zıplama tuşlarına basınca onay süreci
        if (!nextJumpIsSuper && cooldownTimer <= 0f && grounded)
        {
            if (Input.GetKeyDown(KeyCode.Alpha5))
            {
                awaitingSuperConfirmation = true;
            }
            if (awaitingSuperConfirmation && Input.GetKeyDown(KeyCode.Return))
            {
                // Onay verilirse bir sonraki zıplama süper olacak
                nextJumpIsSuper = true;
                awaitingSuperConfirmation = false;
            }
        }

        // Zıplama tuşu (W veya Space) ve sadece grounded ve cooldown bitti ise
        if (grounded && cooldownTimer <= 0f && (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Space)))
        {
            if (nextJumpIsSuper)
            {
                // Süper zıplama: normal kuvvet * multiplier
                Vector2 outDir = (transform.position - currentSource.transform.position).normalized;
                rb.AddForce(outDir * jumpForce * superMultiplier, ForceMode2D.Impulse);

                nextJumpIsSuper = false;
            }
            else
            {
                // Normal zıplama
                Vector2 outDir = (transform.position - currentSource.transform.position).normalized;
                rb.AddForce(outDir * jumpForce, ForceMode2D.Impulse);
            }

            // Zıplama sonrası cooldown ve onay verilerini sıfırla
            cooldownTimer = jumpCooldown;
            awaitingSuperConfirmation = false;
            nextJumpIsSuper = false;
        }
    }

    private void FixedUpdate()
    {
        // --- Gezegen yerçekimi ve yüzeye hizalama ---
        if (currentSource != null)
        {
            Vector2 dir = (Vector2)(currentSource.transform.position - transform.position);
            float dist = dir.magnitude;
            if (dist <= currentSource.scaledRadius)
            {
                rb.AddForce(dir.normalized * currentSource.scaledGravityForce, ForceMode2D.Force);
            }
            transform.up = -dir.normalized;
        }

        if (!isActive)
            return;

        // --- Yürüme & Anında Durma (Tangent Hareketi) ---
        float h = -Input.GetAxisRaw("Horizontal");
        bool grounded = (currentSource != null);
        float speedLimit = grounded ? maxWalkSpeed : maxAirSpeed;

        Vector2 center = grounded
            ? (Vector2)(currentSource.transform.position - transform.position)
            : Vector2.up;
        Vector2 tangent = new Vector2(center.y, -center.x).normalized;

        Vector2 vel = rb.linearVelocity;
        float currTangentVel = Vector2.Dot(vel, tangent);
        float targetTangentVel = h * speedLimit;

        float newTangentVel = Mathf.Lerp(currTangentVel, targetTangentVel, 1f - smoothing);
        Vector2 normalComp = vel - tangent * currTangentVel;
        rb.linearVelocity = normalComp + tangent * newTangentVel;

        if (Mathf.Approximately(h, 0f))
        {
            rb.linearVelocity = normalComp;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent<GravitySource>(out var gs))
        {
            currentSource = gs;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.TryGetComponent<GravitySource>(out var gs) && gs == currentSource)
        {
            currentSource = null;
        }
    }

    /// <summary>
    /// Yatay (tangent) hız bileşenini sıfırlar. TurnManager tarafından sıra değiştiğinde çağrılır.
    /// </summary>
    public void ZeroHorizontalVelocity()
    {
        Vector2 vel = rb.linearVelocity;
        if (currentSource != null)
        {
            Vector2 center = (Vector2)(currentSource.transform.position - transform.position);
            Vector2 tangent = new Vector2(center.y, -center.x).normalized;
            float along = Vector2.Dot(vel, tangent);
            rb.linearVelocity = vel - tangent * along;
        }
        else
        {
            vel.x = 0f;
            rb.linearVelocity = vel;
        }
    }

    /// <summary>
    /// Yeni karakter aktif olduğunda TurnManager çağırır.
    /// Zıplama hakkı ve cooldown'u sıfırlar, yatay hızı temizler.
    /// </summary>
    public void OnTurnStart()
    {
        cooldownTimer = 0f;
        awaitingSuperConfirmation = false;
        nextJumpIsSuper = false;
        ZeroHorizontalVelocity();
    }

    private void OnGUI()
    {
        if (awaitingSuperConfirmation)
        {
            GUI.Label(new Rect(Screen.width / 2f - 150, Screen.height / 2f - 25, 300, 50),
                      "Super Jump için onaylıyor musunuz? [Enter]");
        }
    }
}
