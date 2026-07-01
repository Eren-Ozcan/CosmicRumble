using System.Collections;
using UnityEngine;

/// <summary>
/// Planet-oriented camera with projectile follow.
/// Attach to Main Camera.
///
/// Phase 1 — FollowCharacter: tracks the active character, rotates so the
///   nearest planet center always points "down" on screen.
/// Phase 2 — FollowProjectile: tracks the midpoint between shooter and
///   projectile, zooms out to keep both in frame, continues planet rotation
///   so the camera orbits the planet as the projectile arcs around it.
/// Phase 3 — Returning: waits projectileDelay seconds, then smoothly
///   returns to the active character and restores normal zoom.
/// </summary>
public class CameraController : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────
    public static CameraController Instance { get; private set; }

    // ── Inspector ─────────────────────────────────────────────────
    [Header("Follow / Rotation")]
    [SerializeField] float followSpeed   = 5f;
    [SerializeField] float rotationSpeed = 2f;

    [Header("Zoom")]
    [SerializeField] float zoomSpeed     = 3f;
    [SerializeField] float minZoom       = 5f;
    [SerializeField] float maxZoom       = 15f;
    [SerializeField] float zoomPadding   = 2f;   // extra world units of margin when fitting shot

    [Header("Projectile Return")]
    [SerializeField] float projectileDelay  = 1f;   // seconds to wait before returning
    [SerializeField] float projBiasStart    = 10f;  // distance at which focus starts biasing toward projectile

    // ── State ──────────────────────────────────────────────────────
    enum Phase { FollowCharacter, FollowProjectile, Returning }
    Phase _phase = Phase.FollowCharacter;

    Transform     _activeCharacter;
    Transform     _projectile;
    GravitySource _shooterPlanet;
    Camera        _cam;
    float         _normalZoom;   // zoom to restore after projectile phase
    bool          _firstActivation = true;

    // ── Lifecycle ──────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
        _cam = GetComponent<Camera>();
        if (_cam == null) _cam = Camera.main;
        _normalZoom = _cam.orthographicSize;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ── Public API ─────────────────────────────────────────────────

    /// <summary>
    /// Called by TurnManager each time the active character changes.
    /// </summary>
    public void SetActiveCharacter(Transform character)
    {
        _activeCharacter = character;

        // On first activation, snap so the camera doesn't fly in from the scene origin.
        if (_firstActivation && character != null)
        {
            Vector3 p = character.position;
            p.z = transform.position.z;
            transform.position = p;
            ApplyPlanetRotation(FindNearestSource(character.position), character.position, snap: true);
            _firstActivation = false;
        }

        // Don't interrupt an active projectile track
        if (_phase != Phase.FollowProjectile)
            _phase = Phase.FollowCharacter;
    }

    /// <summary>
    /// Call when a new tracked projectile is spawned.
    /// </summary>
    public static void OnProjectileSpawned(Transform projectile)
    {
        if (Instance == null) return;
        Instance.StartProjectileTracking(projectile);
    }

    /// <summary>
    /// Call when the tracked projectile is destroyed / arrives.
    /// </summary>
    public static void OnProjectileDestroyed()
    {
        if (Instance == null) return;
        Instance.BeginReturn();
    }

    // ── Internal ───────────────────────────────────────────────────

    void StartProjectileTracking(Transform projectile)
    {
        _projectile = projectile;
        _normalZoom = _cam.orthographicSize;   // save zoom before shot so we can restore it

        // Capture the shooter's dominant planet so Phase 2 stays oriented to it.
        var gb = _activeCharacter != null ? _activeCharacter.GetComponent<GravityBody>() : null;
        _shooterPlanet = gb != null ? gb.DominantSource : null;

        _phase = Phase.FollowProjectile;
        StopAllCoroutines();
    }

    void BeginReturn()
    {
        _projectile    = null;
        _shooterPlanet = null;
        StopAllCoroutines();
        StartCoroutine(ReturnAfterDelay());
    }

    IEnumerator ReturnAfterDelay()
    {
        _phase = Phase.Returning;
        yield return new WaitForSeconds(projectileDelay);
        _phase = Phase.FollowCharacter;
    }

    // ── Per-frame (LateUpdate so characters have moved first) ──────

    void LateUpdate()
    {
        if (_cam == null || _activeCharacter == null) return;

        switch (_phase)
        {
            case Phase.FollowCharacter:
            case Phase.Returning:
                UpdateFollowCharacter();
                break;

            case Phase.FollowProjectile:
                UpdateFollowProjectile();
                break;
        }
    }

    // Phase 1 / 3 ──────────────────────────────────────────────────

    void UpdateFollowCharacter()
    {
        // Position: follow active character
        Vector3 target = _activeCharacter.position;
        target.z = transform.position.z;
        transform.position = Vector3.Lerp(transform.position, target,
                                          Time.deltaTime * followSpeed);

        // Rotation: use DominantSource from the character's GravityBody;
        // if null (character is airborne between planets), hold last rotation.
        var gb = _activeCharacter.GetComponent<GravityBody>();
        ApplyPlanetRotation(gb != null ? gb.DominantSource : null, _activeCharacter.position);

        // Zoom: restore to pre-shot value
        _cam.orthographicSize = Mathf.Lerp(_cam.orthographicSize, _normalZoom,
                                            Time.deltaTime * zoomSpeed);
    }

    // Phase 2 ──────────────────────────────────────────────────────

    void UpdateFollowProjectile()
    {
        // Projectile may have been destroyed without firing the event
        if (_projectile == null) { BeginReturn(); return; }

        Vector2 charPos = _activeCharacter.position;
        Vector2 projPos = _projectile.position;
        float   dist    = Vector2.Distance(charPos, projPos);

        // Bias the focus point toward the projectile when it gets very far
        float   bias    = Mathf.Clamp01((dist - projBiasStart) / projBiasStart);
        Vector2 mid     = Vector2.Lerp((charPos + projPos) * 0.5f, projPos, bias);

        Vector3 targetPos = new Vector3(mid.x, mid.y, transform.position.z);
        transform.position = Vector3.Lerp(transform.position, targetPos,
                                           Time.deltaTime * followSpeed);

        // Zoom: fit both shooter and projectile using per-axis calculation
        Vector2 delta      = projPos - charPos;
        float   reqHeight  = Mathf.Abs(delta.y) * 0.5f + zoomPadding;
        float   reqWidth   = Mathf.Abs(delta.x) * 0.5f / _cam.aspect + zoomPadding;
        float   targetZoom = Mathf.Clamp(Mathf.Max(reqHeight, reqWidth), minZoom, maxZoom);
        _cam.orthographicSize = Mathf.Lerp(_cam.orthographicSize, targetZoom,
                                            Time.deltaTime * zoomSpeed);

        // Rotation: stay oriented to the shooter's planet throughout the flight.
        // If no source captured (deep space shot), hold last rotation.
        ApplyPlanetRotation(_shooterPlanet, _projectile.position);
    }

    // ── Helpers ────────────────────────────────────────────────────

    // source == null → keep last rotation (no snapping, no drift)
    void ApplyPlanetRotation(GravitySource source, Vector3 referenceWorldPos, bool snap = false)
    {
        if (source == null) return;

        // Direction from planet center to reference point = "up" relative to planet surface
        Vector2 toRef      = ((Vector2)referenceWorldPos - (Vector2)source.transform.position).normalized;
        float targetAngle  = Vector2.SignedAngle(Vector2.up, toRef);
        float currentAngle = transform.eulerAngles.z;

        float newAngle = snap
            ? targetAngle
            : Mathf.LerpAngle(currentAngle, targetAngle, Time.deltaTime * rotationSpeed);

        transform.rotation = Quaternion.Euler(0f, 0f, newAngle);
    }

    // Used for first-activation snap — reads from the static AllSources list.
    static GravitySource FindNearestSource(Vector3 pos)
    {
        GravitySource nearest  = null;
        float         bestDist = float.MaxValue;

        foreach (var gs in GravitySource.AllSources)
        {
            if (gs == null) continue;
            float d = Vector2.Distance(pos, gs.transform.position);
            if (d < bestDist) { bestDist = d; nearest = gs; }
        }
        return nearest;
    }

    // Used during projectile phase — searches active scene objects each frame.
    GravitySource FindNearestSourceTo(Vector3 pos)
    {
        GravitySource nearest  = null;
        float         bestDist = float.MaxValue;

        foreach (var gs in FindObjectsByType<GravitySource>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (gs == null) continue;
            float d = Vector2.Distance(pos, gs.transform.position);
            if (d < bestDist) { bestDist = d; nearest = gs; }
        }
        return nearest;
    }
}
