using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer), typeof(GravityBody))]
public class RPG : MonoBehaviour
{
    [Header("Onay & Cooldown")]
    public KeyCode activationKey = KeyCode.Alpha3;
    public float cooldownTime = 7f;
    private float cooldownTimer = 0f;
    private bool awaitingConfirmation = false;
    private bool fireAllowed = false;

    [Header("Fire Settings")]
    public Transform firePoint;
    public GameObject projectilePrefab;
    public float maxDragDistance = 3f;
    public float powerMultiplier = 6f;
    public float ignoreOwnerDuration = 1.5f;

    [Header("Trajectory Preview")]
    public int trajectoryPoints = 60;
    public float timeStep = 0.05f;

    private LineRenderer lr;
    private GravityBody gravityBody;
    private GravitySource[] gravitySources;
    private bool isDragging;
    private Vector2 dragStart;
    private bool wasActive = false;

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
        gravityBody = GetComponent<GravityBody>();
        gravitySources = FindObjectsOfType<GravitySource>();

        lr.enabled = false;
        lr.positionCount = 0;
    }

    void Update()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;

        if (gravityBody.isActive && !wasActive)
        {
            wasActive = true;
            cooldownTimer = 0f;
            fireAllowed = false;
            awaitingConfirmation = false;
            CancelDrag();
        }
        else if (!gravityBody.isActive)
        {
            wasActive = false;
            return;
        }

        if (cooldownTimer > 0f)
        {
            CancelDrag();
            return;
        }

        if (Input.GetKeyDown(activationKey) && !awaitingConfirmation && !fireAllowed)
        {
            awaitingConfirmation = true;
        }

        if (awaitingConfirmation)
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                fireAllowed = true;
                awaitingConfirmation = false;
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                awaitingConfirmation = false;
            }
            return;
        }

        if (!fireAllowed)
            return;

        Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        if (Input.GetMouseButtonDown(0))
        {
            isDragging = true;
            dragStart = mouseWorld;
            lr.enabled = true;
            lr.positionCount = trajectoryPoints;
        }
        else if (isDragging && Input.GetMouseButton(0))
        {
            Vector2 pull = dragStart - mouseWorld;
            float clamped = Mathf.Min(pull.magnitude, maxDragDistance);
            Vector2 initial = pull.normalized * clamped * powerMultiplier;
            DrawTrajectory(initial);
        }
        else if (isDragging && Input.GetMouseButtonUp(0))
        {
            Fire();
            lr.positionCount = 0;
            CancelDrag();
            fireAllowed = false;
        }
    }

    void OnGUI()
    {
        if (awaitingConfirmation)
        {
            var style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 20
            };
            GUI.Label(
                new Rect(Screen.width / 2f - 250, Screen.height / 2f - 15, 500, 30),
                "RPG skillini kullanmak ister misiniz?  [Enter]=Evet  [Esc]=Hayır", style
            );
        }
    }

    private void DrawTrajectory(Vector2 initialVelocity)
    {
        // 🛡️ Güvenlik kontrolü
        if (!lr.enabled || lr.positionCount != trajectoryPoints)
        {
            lr.enabled = true;
            lr.positionCount = trajectoryPoints;
        }

        Vector2 pos = firePoint.position;
        Vector2 vel = initialVelocity;

        for (int i = 0; i < trajectoryPoints; i++)
        {
            Vector2 acc = Vector2.zero;
            foreach (var src in gravitySources)
            {
                Vector2 dir = (Vector2)src.transform.position - pos;
                float r2 = dir.sqrMagnitude;
                if (r2 < 0.001f) continue;
                acc += dir.normalized * (src.scaledGravityForce / r2);
            }

            vel += acc * timeStep;
            pos += vel * timeStep;
            if (i < lr.positionCount) lr.SetPosition(i, pos); // Güvenli set
        }
    }

    private void Fire()
    {
        Vector2 pull = dragStart - (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);
        float clamped = Mathf.Min(pull.magnitude, maxDragDistance);
        Vector2 initial = pull.normalized * clamped * powerMultiplier;

        cooldownTimer = cooldownTime;
        var bulletGO = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
        var proj = bulletGO.GetComponent<Projectile>();
        if (proj != null)
            proj.Init(initial, gameObject, ignoreOwnerDuration);
        else
        {
            var rb = bulletGO.GetComponent<Rigidbody2D>();
            rb?.AddForce(initial, ForceMode2D.Impulse);
        }
    }

    private void CancelDrag()
    {
        isDragging = false;
        lr.enabled = false;
        lr.positionCount = 0;
    }
}
