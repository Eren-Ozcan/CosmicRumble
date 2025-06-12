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
    public bool nextJumpIsSuper = false;

    private int jumpCount = 0;
    private bool canDoubleJump = true;

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

        if (cooldownTimer <= 0f && (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Space)))
        {
            Debug.Log($"[GravityBody] Zıplama denemesi – grounded: {grounded}, jumpCount: {jumpCount}, canDoubleJump: {canDoubleJump}, super: {nextJumpIsSuper}");

            if (grounded)
            {
                PerformJump(nextJumpIsSuper);
                Debug.Log("[GravityBody] Yerden zıplama gerçekleşti.");

                jumpCount = 1;
                canDoubleJump = true;
            }
            else if (!grounded && jumpCount == 1 && canDoubleJump)
            {
                PerformJump(nextJumpIsSuper);
                Debug.Log("[GravityBody] Havadan double jump gerçekleşti.");

                jumpCount = 2;
                canDoubleJump = false;
            }
            else
            {
                Debug.LogWarning("[GravityBody] Zıplama reddedildi – koşullar uygun değil.");
                return;
            }

            cooldownTimer = jumpCooldown;
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
        }
    }

    private void PerformJump(bool isSuper)
    {
        Vector2 outDir = (transform.position - currentSource.transform.position).normalized;
        float force = isSuper ? jumpForce * superMultiplier : jumpForce;

        Debug.Log($"[GravityBody] PerformJump() çağrıldı — isSuper: {isSuper}, force: {force}, direction: {outDir}");
        rb.AddForce(outDir * force, ForceMode2D.Impulse);

        // ✅ SuperJump hakkı buradan düşürülür
        if (isSuper)
        {
            var abilities = GetComponent<CharacterAbilities>();
            if (abilities != null)
            {
                abilities.UseSuperJump(); // sayaç azalt
                UIManager.Instance.filterImages[4].color = Color.clear; // yeşil filtre temizle
                Debug.Log("[GravityBody] SuperJump kullanıldı – sayaç düşürüldü, filtre temizlendi");
            }
        }
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
        nextJumpIsSuper = false;
        ZeroHorizontalVelocity();
        jumpCount = 0;
        canDoubleJump = true;

        var ch = GetComponent<CharacterHealth>();
        if (ch != null)
            ch.isShielded = false;
    }
}
