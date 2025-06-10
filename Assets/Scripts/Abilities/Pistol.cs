using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer), typeof(GravityBody))]
public class Pistol : MonoBehaviour
{
    [Header("Fire Settings")]
    [Tooltip("Namlu ucu (child Transform)")]
    public Transform firePoint;
    [Tooltip("Fırlatılacak mermi prefab’ı")]
    public GameObject projectilePrefab;
    [Tooltip("Maksimum geri çekme mesafesi (Unity birimi)")]
    public float maxDragDistance = 3f;
    [Tooltip("Geri çekme mesafesini hıza çeviren çarpan")]
    public float powerMultiplier = 5f;
    [Tooltip("Atıcıya çarpmama süresi (saniye)")]
    public float ignoreOwnerDuration = 1f;

    [Header("Trajectory Preview")]
    [Tooltip("Çizilecek nokta sayısı")]
    public int trajectoryPoints = 60;
    [Tooltip("Her nokta arasındaki zaman adımı (sn)")]
    public float timeStep = 0.05f;

    private LineRenderer lr;
    private GravityBody gravityBody;
    private GravitySource[] gravitySources;

    private bool isDragging = false;
    private Vector2 dragStart;

    void Awake()
    {
        // GravityBody üzerinden "sıra kimin" bilgisini alacağız
        gravityBody = GetComponent<GravityBody>();
        lr = GetComponent<LineRenderer>();
        gravitySources = FindObjectsOfType<GravitySource>();
    }

    void Start()
    {
        // Başlangıçta trajectory gizli olsun
        lr.enabled = false;
        lr.positionCount = 0;
    }

    void Update()
    {
        // Eğer bu karakterin sırası değilse hiçbir şey yapma
        if (!gravityBody.isActive)
        {
            // Drag iptal edilsin ki trajectory sahnede kalmasın
            if (isDragging) CancelDrag();
            return;
        }

        // Fare dünya koordinatı
        Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        // 1) Drag başlat
        if (Input.GetMouseButtonDown(0))
        {
            isDragging = true;
            dragStart = mouseWorld;
            lr.enabled = true;
            lr.positionCount = trajectoryPoints;
        }
        // 2) Drag sırasında trajectory çiz
        else if (isDragging && Input.GetMouseButton(0))
        {
            Vector2 pull = dragStart - mouseWorld;
            float clamped = Mathf.Min(pull.magnitude, maxDragDistance);
            Vector2 initialVelocity = pull.normalized * clamped * powerMultiplier;
            DrawTrajectory(initialVelocity);
        }
        // 3) Drag bırakıldığında ateşle ve temizle
        else if (isDragging && Input.GetMouseButtonUp(0))
        {
            Fire();
            CancelDrag();
        }
    }

    private void DrawTrajectory(Vector2 initialVelocity)
    {
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
                // GravitySource içindeki scaledGravityForce'u kullanıyoruz
                acc += dir.normalized * (src.scaledGravityForce / r2);
            }
            vel += acc * timeStep;
            pos += vel * timeStep;
            lr.SetPosition(i, pos);
        }
    }

    private void Fire()
    {
        // Drag sonunda hesaplanan initialVelocity
        Vector2 pull = dragStart - (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);
        float clamped = Mathf.Min(pull.magnitude, maxDragDistance);
        Vector2 initialVelocity = pull.normalized * clamped * powerMultiplier;

        // Mermiyi spawn et, Init ile owner-ignore ve hızı ata
        var bulletGO = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
        var proj = bulletGO.GetComponent<Projectile>();
        if (proj != null)
            proj.Init(initialVelocity, gameObject, ignoreOwnerDuration);
        else
        {
            // Eğer Init yoksa doğrudan force uygula
            var rb = bulletGO.GetComponent<Rigidbody2D>();
            if (rb != null)
                rb.AddForce(initialVelocity, ForceMode2D.Impulse);
        }
    }

    private void CancelDrag()
    {
        isDragging = false;
        lr.positionCount = 0;
        lr.enabled = false;
    }
}
