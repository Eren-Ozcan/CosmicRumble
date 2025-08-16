using UnityEngine;

/// <summary>
/// Allows a 2D character to walk on a spherical/planetary surface.
/// Gravity is assumed to pull toward the active <see cref="GravitySource"/>
/// and the character will stick to surfaces facing outward from the centre.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class PlanetWalker2D : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 3f;
    [Tooltip("Speed for aligning rotation to the surface normal.")]
    public float alignSpeed = 10f;
    [Tooltip("Offset from the surface when snapping.")]
    public float surfaceOffset = 0.05f;
    [Tooltip("Maximum slope angle that can be walked on.")]
    [Range(0f, 90f)]
    public float slopeLimitDegrees = 70f;
    [Tooltip("Time after leaving ground that the character is still considered grounded.")]
    public float coyoteTime = 0.1f;

    [Header("Ground Check")]
    [Tooltip("Distance used when casting toward the planet centre to find the surface.")]
    public float groundCheckDistance = 0.75f;
    public LayerMask groundMask;
    [Tooltip("Dot product threshold to reject undersides.")]
    public float undersideThreshold = 0.05f;

    private Rigidbody2D rb;
    private GravitySource currentSource;
    private bool grounded;
    private float coyoteTimer;

    /// <summary>True if currently grounded or within coyote time.</summary>
    public bool IsGrounded => grounded || coyoteTimer > 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    private void FixedUpdate()
    {
        if (currentSource == null)
            return;

        Vector2 centre = currentSource.transform.position;
        Vector2 centreToPlayer = (Vector2)transform.position - centre;
        Vector2 dirToCentre = -centreToPlayer.normalized;

        // Ground check cast toward the centre.
        RaycastHit2D hit = Physics2D.Raycast(rb.position, dirToCentre, groundCheckDistance, groundMask);
        bool validGround = false;
        if (hit.collider != null)
        {
            float dot = Vector2.Dot(hit.normal, centreToPlayer.normalized);
            float slopeLimitCos = Mathf.Cos(slopeLimitDegrees * Mathf.Deg2Rad);
            if (dot > undersideThreshold && dot >= slopeLimitCos)
            {
                validGround = true;
            }
        }

        if (validGround)
        {
            grounded = true;
            coyoteTimer = coyoteTime;

            // Snap to surface.
            Vector2 targetPos = hit.point + hit.normal * surfaceOffset;
            rb.position = targetPos;

            // Align rotation to surface.
            Quaternion targetRot = Quaternion.FromToRotation(transform.up, hit.normal) * transform.rotation;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, alignSpeed * Time.fixedDeltaTime);

            // Cancel normal velocity.
            Vector2 vel = rb.linearVelocity;
            vel -= hit.normal * Vector2.Dot(vel, hit.normal);

            // Tangential movement along the surface.
            float h = -Input.GetAxisRaw("Horizontal");
            Vector2 tangent = new Vector2(-centreToPlayer.y, centreToPlayer.x).normalized;
            vel = tangent * (h * walkSpeed);
            rb.linearVelocity = vel;
        }
        else
        {
            if (grounded)
            {
                grounded = false;
            }
            else
            {
                coyoteTimer -= Time.fixedDeltaTime;
            }

            // Align to centre direction while airborne.
            Vector2 upDir = centreToPlayer.normalized;
            Quaternion targetRot = Quaternion.FromToRotation(transform.up, upDir) * transform.rotation;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, alignSpeed * Time.fixedDeltaTime);
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
            grounded = false;
        }
    }
}

