// Assets/Scripts/Gravity/GravityBody.cs
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class GravityBody : MonoBehaviour
{
    [HideInInspector] public bool isActive = false;

    private Rigidbody2D rb;
    private GravitySource currentSource;

    [Header("Yürüme Ayarları")]
    public float maxWalkSpeed = 3f;
    public float maxAirSpeed = 3f;
    [Range(0f, 1f)]
    public float smoothing = 0f;

    [Header("Zıplama Ayarları")]
    public float jumpForce = 5f;
    public float superMultiplier = 2f;
    public float jumpCooldown = 10f;

    private float cooldownTimer = 0f;
    private bool awaitingSuperConfirmation = false;
    public bool nextJumpIsSuper = false;

    // Double jump için
    private int jumpCount = 0;         // 0 = yerde, 1 = bir kez zıpladı, 2 = double jump yapıldı
    private bool canDoubleJump = true; // Havada bir kez daha zıplama izni

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        isActive = false;
    }

    private void Update()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;

        if (!isActive)
            return;

        bool grounded = (currentSource != null);

        // Süper zıplama onayı (yalnızca yerde ve cooldown bittiğinde)
        if (!nextJumpIsSuper && cooldownTimer <= 0f && grounded)
        {
            if (Input.GetKeyDown(KeyCode.Alpha5))
                awaitingSuperConfirmation = true;

            if (awaitingSuperConfirmation && Input.GetKeyDown(KeyCode.Return))
            {
                nextJumpIsSuper = true;
                awaitingSuperConfirmation = false;
            }
        }

        // Zıplama tuşu (W veya Space) ve cooldown bittiğinde
        if (cooldownTimer <= 0f && (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Space)))
        {
            // 1) Eğer yerdeysek
            if (grounded)
            {
                PerformJump(nextJumpIsSuper);
                jumpCount = 1;
                canDoubleJump = true;
            }
            // 2) Havadaysa ve bir kez zıplamış + doubleJump izni varsa
            else if (!grounded && jumpCount == 1 && canDoubleJump)
            {
                PerformJump(nextJumpIsSuper);
                jumpCount = 2;
                canDoubleJump = false;
            }
            else
            {
                return;
            }

            cooldownTimer = jumpCooldown;
            awaitingSuperConfirmation = false;
            nextJumpIsSuper = false;
        }
    }

    private void FixedUpdate()
    {
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
            rb.linearVelocity = normalComp;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent<GravitySource>(out var gs))
        {
            currentSource = gs;
            jumpCount = 0;
            canDoubleJump = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.TryGetComponent<GravitySource>(out var gs) && gs == currentSource)
        {
            currentSource = null;
            // Havadayken jumpCount ve canDoubleJump durumu bozulmaz
        }
    }

    private void PerformJump(bool isSuper)
    {
        Vector2 outDir = (transform.position - currentSource.transform.position).normalized;
        if (isSuper)
            rb.AddForce(outDir * jumpForce * superMultiplier, ForceMode2D.Impulse);
        else
            rb.AddForce(outDir * jumpForce, ForceMode2D.Impulse);
    }

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

    public void OnTurnStart()
    {
        cooldownTimer = 0f;
        awaitingSuperConfirmation = false;
        nextJumpIsSuper = false;
        ZeroHorizontalVelocity();
        jumpCount = 0;
        canDoubleJump = true;

        // Turn başında karaktere ait shield’i kaldır
        var ch = GetComponent<CharacterHealth>();
        if (ch != null)
            ch.isShielded = false;
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
