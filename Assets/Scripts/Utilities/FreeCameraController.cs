using UnityEngine;

/// <summary>
/// Attach to any GameObject in SampleScene.
/// When enabled (by UIManager), the player can freely pan and zoom the camera.
/// Press Escape to exit free camera and reopen the End-Game Menu.
///
/// Starts DISABLED — UIManager.OnEnterFreeCamera() activates it.
/// </summary>
public class FreeCameraController : MonoBehaviour
{
    /// <summary>True while free camera is active. Checked by InGameMenu to suppress ESC.</summary>
    public static bool IsActive { get; private set; }

    [Header("Target Camera (leave blank → Camera.main)")]
    [SerializeField] Camera cam;

    [Header("Pan")]
    [Tooltip("1 = 1:1 pixel-to-world panning, higher values feel faster")]
    [SerializeField] float panSpeed = 1f;

    [Header("Zoom (Orthographic)")]
    [SerializeField] float zoomSpeed = 3f;
    [SerializeField] float minZoom   = 2f;
    [SerializeField] float maxZoom   = 30f;

    Vector3 _dragOrigin;
    bool    _dragging;

    // ── Lifecycle ────────────────────────────────────────────────

    void Awake()
    {
        if (cam == null) cam = Camera.main;
        enabled = false;   // must start disabled; UIManager enables it
    }

    void OnEnable()
    {
        IsActive = true;
        if (cam == null) cam = Camera.main;
        _dragging = false;
    }

    void OnDisable()
    {
        IsActive = false;
        _dragging = false;
    }

    // ── Per-frame ────────────────────────────────────────────────

    void Update()
    {
        HandlePan();
        HandleZoom();
        HandleExit();
    }

    // ── Pan ──────────────────────────────────────────────────────

    void HandlePan()
    {
        if (Input.GetMouseButtonDown(0))
        {
            _dragging   = true;
            _dragOrigin = Input.mousePosition;
        }

        if (Input.GetMouseButtonUp(0))
            _dragging = false;

        if (!_dragging || !Input.GetMouseButton(0)) return;

        Vector3 delta = Input.mousePosition - _dragOrigin;

        // Convert screen-pixel delta → world units using current zoom level
        float worldPerPixel = cam.orthographicSize * 2f / Screen.height;
        cam.transform.position -= new Vector3(delta.x, delta.y, 0f) * (worldPerPixel * panSpeed);

        _dragOrigin = Input.mousePosition;
    }

    // ── Zoom ─────────────────────────────────────────────────────

    void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) < 0.001f) return;

        if (cam.orthographic)
        {
            cam.orthographicSize = Mathf.Clamp(
                cam.orthographicSize - scroll * zoomSpeed,
                minZoom, maxZoom);
        }
        else
        {
            // Perspective fallback: dolly forward/back
            cam.transform.position += cam.transform.forward * (scroll * zoomSpeed * 10f);
        }
    }

    // ── Exit ─────────────────────────────────────────────────────

    void HandleExit()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            UIManager.Instance?.ExitFreeCamera();
    }
}
