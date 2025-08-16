// Assets/Scripts/Gravity/GravityBody.cs
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Handles planet centred gravity and Worms-style walking.
/// Attach to any character that should walk on spherical worlds.
/// Other movement scripts should be disabled to avoid conflicts.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class GravityBody : MonoBehaviour
{
    [HideInInspector] public bool isActive = false;   // Controlled by TurnManager

    private Rigidbody2D rb;
    private GravitySource currentSource;              // Active planet/attractor

    // --------------------------------------------------------------
    // Movement settings
    // --------------------------------------------------------------
    [Header("Movement")]
    [FormerlySerializedAs("maxWalkSpeed")] public float walkSpeed = 3f;
    public float maxAirSpeed = 3f;
    [FormerlySerializedAs("smoothing")] public float moveAccel = 15f;  // Ground acceleration
    public float airAccel = 10f;                                       // Air acceleration
    public float surfaceFriction = 5f;                                 // How fast we stop when no input

    // --------------------------------------------------------------
    // Ground detection settings
    // --------------------------------------------------------------
    [Header("Ground")]
    public LayerMask groundMask;
    public float groundCheckDistance = 0.3f;       // Distance for ray cast
    public float surfaceOffset = 0.02f;            // Offset from surface when snapped
    public float outwardDotThreshold = 0.05f;      // Reject if surface normal faces planet
    public float slopeLimitDegrees = 75f;          // Maximum walkable slope

    // --------------------------------------------------------------
    // Coyote time
    // --------------------------------------------------------------
    [Header("Coyote")]
    public float coyoteTime = 0.1f;

    // --------------------------------------------------------------
    // Jump settings (kept from previous version)
    // --------------------------------------------------------------
    [Header("Jumping")]
    public float jumpForce = 5f;
    public float superMultiplier = 2f;
    public float jumpCooldown = 10f;

    private float cooldownTimer = 0f;
    public bool nextJumpIsSuper = false;

    private int jumpCount = 0;          // 0: none, 1: jumped once
    private bool canDoubleJump = true;

    // --------------------------------------------------------------
    // Internal state
    // --------------------------------------------------------------
    private float input;                // Cached horizontal input
    private bool grounded;              // Result of last ground check
    private Vector2 lastGroundNormal = Vector2.up;   // For coyote time
    private float lastGroundedTime;     // When we were last on the ground
    public bool applyGravity = true;                 // If true we apply gravity forces ourselves

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        // Required Rigidbody2D settings
        rb.gravityScale = 0f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.freezeRotation = true;
    }

    private void Update()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;

        // Cache input for physics update
        input = -Input.GetAxisRaw("Horizontal");

        if (!isActive)
            return;

        bool jumpPressed = Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Space);

        if (jumpPressed && cooldownTimer <= 0f)
        {
            bool canGroundJump = grounded || (Time.time - lastGroundedTime <= coyoteTime);

            if (canGroundJump && jumpCount == 0)
            {
                PerformJump(nextJumpIsSuper, grounded ? lastGroundNormal : GetOutward());
                jumpCount = 1;
                canDoubleJump = true;
            }
            else if (!grounded && jumpCount == 1 && canDoubleJump)
            {
                PerformJump(nextJumpIsSuper, GetOutward());
                jumpCount = 2;
                canDoubleJump = false;
            }
            else
            {
                return; // Jump not allowed
            }

            cooldownTimer = jumpCooldown;
            nextJumpIsSuper = false;
        }
    }

    private void FixedUpdate()
    {
        if (!isActive || currentSource == null)
            return;

        // Direction from planet centre to body
        Vector2 toCenter = (Vector2)currentSource.transform.position - rb.position;
        Vector2 outward = -toCenter.normalized;

        // ----------------------------------------------------------
        // Ground detection
        // ----------------------------------------------------------
        RaycastHit2D hit = Physics2D.Raycast(rb.position, toCenter.normalized, groundCheckDistance, groundMask);
        if (!hit)
        {
            hit = Physics2D.CircleCast(rb.position, 0.1f, toCenter.normalized, groundCheckDistance, groundMask);
        }
        if (!hit)
        {
            Collider2D col = Physics2D.OverlapCircle(rb.position, 0.1f, groundMask);
            if (col)
            {
                Vector2 closest = col.ClosestPoint(rb.position);
                hit = new RaycastHit2D
                {
                    collider = col,
                    point = closest,
                    normal = (rb.position - closest).normalized,
                    distance = Vector2.Distance(rb.position, closest)
                };
            }
        }

        bool validGround = false;
        if (hit.collider != null)
        {
            float dot = Vector2.Dot(hit.normal, outward);
            if (dot > outwardDotThreshold && Vector2.Angle(hit.normal, outward) <= slopeLimitDegrees)
            {
                validGround = true;
                lastGroundNormal = hit.normal;
                lastGroundedTime = Time.time;
            }
        }

        grounded = validGround || (Time.time - lastGroundedTime <= coyoteTime);

        Vector2 groundNormal = grounded ? lastGroundNormal : outward;

        // ----------------------------------------------------------
        // Orientation towards surface/outward
        // ----------------------------------------------------------
        Quaternion targetRot = Quaternion.LookRotation(Vector3.forward, groundNormal);
        Quaternion newRot = Quaternion.Slerp(transform.rotation, targetRot, 10f * Time.fixedDeltaTime);
        rb.MoveRotation(newRot.eulerAngles.z);

        // ----------------------------------------------------------
        // Apply gravity if requested (do not use together with GravitySource force)
        // ----------------------------------------------------------
        if (applyGravity)
            rb.AddForce(toCenter.normalized * currentSource.gravityForce, ForceMode2D.Force);

        // ----------------------------------------------------------
        // Movement
        // ----------------------------------------------------------
        Vector2 vel = rb.linearVelocity;

        if (grounded && validGround)
        {
            // Snap to surface and zero normal velocity
            Vector2 targetPos = hit.point + hit.normal * surfaceOffset;
            rb.MovePosition(targetPos);
            vel -= hit.normal * Vector2.Dot(vel, hit.normal);

            // Tangent direction for movement
            Vector2 tangent = Vector3.Cross(Vector3.forward, hit.normal).normalized;
            float tangentVel = Vector2.Dot(vel, tangent);
            float targetTangent = input * walkSpeed;
            tangentVel = Mathf.MoveTowards(tangentVel, targetTangent, moveAccel * Time.fixedDeltaTime);
            if (Mathf.Approximately(input, 0f))
                tangentVel = Mathf.MoveTowards(tangentVel, 0f, surfaceFriction * Time.fixedDeltaTime);
            tangentVel = Mathf.Clamp(tangentVel, -walkSpeed, walkSpeed);
            vel = tangent * tangentVel;
        }
        else
        {
            // Air control
            Vector2 tangent = new Vector2(-outward.y, outward.x);
            float tangentVel = Vector2.Dot(vel, tangent);
            float targetTangent = input * maxAirSpeed;
            tangentVel = Mathf.MoveTowards(tangentVel, targetTangent, airAccel * Time.fixedDeltaTime);
            tangentVel = Mathf.Clamp(tangentVel, -maxAirSpeed, maxAirSpeed);
            Vector2 normalVel = vel - tangent * tangentVel;
            vel = normalVel + tangent * tangentVel;
        }

        rb.linearVelocity = vel;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.TryGetComponent<GravitySource>(out var gs))
            currentSource = gs;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.TryGetComponent<GravitySource>(out var gs) && gs == currentSource)
            currentSource = null;
    }

    /// <summary>
    /// Perform a jump in the supplied direction.
    /// </summary>
    private void PerformJump(bool isSuper, Vector2 normal)
    {
        if (currentSource == null) return;

        Vector2 outDir = normal; // Jump along surface normal
        Vector2 vel = rb.linearVelocity;
        vel -= outDir * Vector2.Dot(vel, outDir); // Remove normal component
        rb.linearVelocity = vel;

        float force = isSuper ? jumpForce * superMultiplier : jumpForce;
        rb.AddForce(outDir * force, ForceMode2D.Impulse);

        if (isSuper)
        {
            var abilities = GetComponent<CharacterAbilities>();
            if (abilities != null)
            {
                abilities.UseSuperJump();
                UIManager.Instance.filterImages[4].color = Color.clear;
            }
        }
    }

    /// <summary>Utility used by TurnManager when a turn starts.</summary>
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

    /// <summary>Called externally at the beginning of a turn.</summary>
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

    // Helper to get outward normal when no ground is hit
    private Vector2 GetOutward()
    {
        if (currentSource == null) return Vector2.up;
        return (rb.position - (Vector2)currentSource.transform.position).normalized;
    }
}

// Recommended Settings:
// Rigidbody2D -> gravityScale = 0, Interpolate, Continuous, freezeRotation = true
// groundCheckDistance ≈ 0.3, surfaceOffset ≈ 0.02, outwardDotThreshold ≈ 0.05
// slopeLimitDegrees 70-80, coyoteTime 0.1
