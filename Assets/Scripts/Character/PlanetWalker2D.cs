// Assets/Scripts/Character/PlanetWalker2D.cs
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlanetWalker2D : MonoBehaviour
{
    [Header("Planet")]
    [Tooltip("Gravity source that defines the planet center.")]
    public GravitySource gravitySource;
    [Tooltip("Layers considered as walkable planet surface.")]
    public LayerMask groundMask;
    [Tooltip("How far to keep the character above the surface.")]
    public float surfaceOffset = 0.02f;
    [Tooltip("Normals with a dot product below this are treated as underside.")]
    public float undersideThreshold = 0.05f;
    [Tooltip("Maximum slope angle allowed for walking.")]
    public float slopeLimitDegrees = 60f;

    [Header("Movement")]
    public float walkSpeed = 3f;
    public float jumpForce = 5f;
    public float maxAirSpeed = 3f;
    [Tooltip("Use MovePosition for soft snapping instead of directly setting position.")]
    public bool softSnap = true;

    [Header("Gravity")]
    [Tooltip("Acceleration magnitude applied toward the planet center when airborne.")]
    public float gravity = 9.81f;

    [Header("Edge Tolerance")]
    [Tooltip("Time allowed to remain grounded after losing surface contact.")]
    public float coyoteTime = 0.1f;

    private Rigidbody2D rb;
    private bool grounded;
    private float coyoteCounter;
    private Vector2 groundNormal = Vector2.up;
    private Vector2 lastValidNormal = Vector2.up;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.freezeRotation = false;

        // Pick the closest gravity source if none assigned
        if (!gravitySource && GravitySource.AllSources.Count > 0)
        {
            float best = float.MaxValue;
            foreach (var gs in GravitySource.AllSources)
            {
                float sqr = ((Vector2)gs.transform.position - rb.position).sqrMagnitude;
                if (sqr < best)
                {
                    best = sqr;
                    gravitySource = gs;
                }
            }
        }
    }

    private void Update()
    {
        if (grounded && Input.GetButtonDown("Jump"))
        {
            rb.linearVelocity += groundNormal * jumpForce;
            grounded = false;
            coyoteCounter = 0f;
        }
    }

    private void FixedUpdate()
    {
        if (!gravitySource)
            return;

        Vector2 center = gravitySource.transform.position;
        Vector2 centerToPlayer = rb.position - center;
        float distance = centerToPlayer.magnitude;
        if (distance <= 0f)
            return;
        Vector2 dirOut = centerToPlayer / distance;

        // Cast from planet center outward
        RaycastHit2D hit = Physics2D.Raycast(center, dirOut, distance + 0.5f, groundMask);
        if (!hit)
        {
            // Secondary raycast along the last surface normal to avoid edge gaps
            hit = Physics2D.Raycast(rb.position, -lastValidNormal, 0.5f, groundMask);
        }

        bool validGround = false;
        if (hit.collider != null)
        {
            float dot = Vector2.Dot(hit.normal, dirOut);
            float slope = Vector2.Angle(hit.normal, dirOut);
            bool underside = dot <= undersideThreshold;
            bool slopeTooSteep = slope > slopeLimitDegrees;

            if (!underside && !slopeTooSteep)
            {
                validGround = true;
                groundNormal = hit.normal;
                lastValidNormal = groundNormal;
            }
            else if (!underside && slopeTooSteep)
            {
                // Slide down steep slopes
                Vector2 gravityDir = (center - rb.position).normalized * gravity;
                Vector2 tangent = new Vector2(-hit.normal.y, hit.normal.x);
                float slideComp = Vector2.Dot(gravityDir, tangent);
                rb.AddForce(tangent * slideComp, ForceMode2D.Acceleration);
            }
        }

        if (validGround)
        {
            grounded = true;
            coyoteCounter = coyoteTime;

            Vector2 target = hit.point + hit.normal * surfaceOffset;
            if (softSnap)
                rb.MovePosition(target);
            else
                rb.position = target;

            Quaternion targetRot = Quaternion.LookRotation(Vector3.forward, groundNormal);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 10f * Time.fixedDeltaTime);

            Vector2 vel = rb.linearVelocity;
            vel -= groundNormal * Vector2.Dot(vel, groundNormal); // remove normal component

            Vector2 tangent = new Vector2(-groundNormal.y, groundNormal.x);
            float input = Input.GetAxisRaw("Horizontal");
            float tangentVel = Mathf.Clamp(Vector2.Dot(vel, tangent) + input * walkSpeed, -walkSpeed, walkSpeed);
            rb.linearVelocity = tangent * tangentVel;
        }
        else
        {
            if (coyoteCounter > 0f)
            {
                coyoteCounter -= Time.fixedDeltaTime;
                grounded = true;
                groundNormal = lastValidNormal;

                Quaternion targetRot = Quaternion.LookRotation(Vector3.forward, groundNormal);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 10f * Time.fixedDeltaTime);
            }
            else
            {
                grounded = false;
            }

            Vector2 gravityDir = (center - rb.position).normalized;
            rb.AddForce(gravityDir * gravity, ForceMode2D.Acceleration);

            float input = Input.GetAxisRaw("Horizontal");
            if (Mathf.Abs(input) > 0.01f)
            {
                Vector2 tangent = new Vector2(-gravityDir.y, gravityDir.x);
                float tangentVel = Mathf.Clamp(Vector2.Dot(rb.linearVelocity, tangent) + input * walkSpeed * 0.5f, -maxAirSpeed, maxAirSpeed);
                Vector2 normalVel = gravityDir * Vector2.Dot(rb.linearVelocity, gravityDir);
                rb.linearVelocity = normalVel + tangent * tangentVel;
            }
        }
    }
}

