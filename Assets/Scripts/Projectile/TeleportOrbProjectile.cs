using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class TeleportOrbProjectile : MonoBehaviour
{
    [Header("Fuse & Impact")]
    public bool teleportOnImpact = true;   // true: ilk çarpmada ışınla
    public float fuseTime = 3f;            // false ise süre sonunda ışınla
    public LayerMask surfaceMask;          // gezegen/engel katmanları
    public float safeRadius = 0.35f;       // owner kapsül yarıçapına göre

    [Header("Placement")]
    public float surfacePlaceOffset = 0.35f; // normale doğru dışarı ofset
    public int outwardSteps = 6;             // tıkama çözümü için atılacak adım

    [Header("FX (opsiyonel)")]
    public GameObject spawnFx;
    public GameObject impactFx;
    public GameObject teleportFx;

    // state
    private Rigidbody2D rb;
    private Collider2D col;
    private GameObject owner;
    private float spawnTime;
    private bool armed;      // owner ignore süresi bitince true
    private bool done;       // tek seferlik trigger

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
    }

    public void Init(GameObject owner, Vector2 initialVelocity, float ignoreOwnerTime = 0.6f)
    {
        this.owner = owner;
        spawnTime = Time.time;
        rb.linearVelocity = initialVelocity;

        if (spawnFx) Instantiate(spawnFx, transform.position, Quaternion.identity);

        // Owner ile çarpışmayı yok say
        if (owner != null)
        {
            var ownerCol = owner.GetComponent<Collider2D>();
            if (ownerCol != null) Physics2D.IgnoreCollision(col, ownerCol, true);
            // Silme koruması
            Invoke(nameof(ArmAndReenableOwnerCollision), Mathf.Max(0.01f, ignoreOwnerTime));
        }

        // Fuse modunda, çarpmayı beklemeden süre dolunca da ışınla
        if (!teleportOnImpact && fuseTime > 0f)
        {
            Invoke(nameof(TeleportOwnerAtCurrent), fuseTime);
        }
    }

    private void ArmAndReenableOwnerCollision()
    {
        if (owner == null) { armed = true; return; }
        var ownerCol = owner.GetComponent<Collider2D>();
        if (ownerCol != null) Physics2D.IgnoreCollision(col, ownerCol, false);
        armed = true;
    }

    void OnCollisionEnter2D(Collision2D other)
    {
        if (done) return;
        if (!teleportOnImpact) return;       // fuse modunda, çarpışma bekleme

        if ((surfaceMask.value & (1 << other.collider.gameObject.layer)) == 0)
            return; // Yalnızca belirtilen yüzeyler

        Vector2 hitPoint = other.GetContact(0).point;
        Vector2 normal = other.GetContact(0).normal;

        TeleportOwnerTo(hitPoint + normal * surfacePlaceOffset, normal);
    }

    private void TeleportOwnerAtCurrent()
    {
        if (done) return;
        // Bulunduğun noktaya yakın güvenli yer
        Vector2 pos = rb.position;

        // Normali raycast ile tahmin et: yakın yüzeye doğru
        RaycastHit2D hit = Physics2D.Raycast(pos + Vector2.up * 0.01f, Vector2.down, 1.5f, surfaceMask);
        Vector2? n = hit.collider ? (Vector2?)hit.normal : null;

        TeleportOwnerTo(pos, n);
    }

    private void TeleportOwnerTo(Vector2 target, Vector2? surfaceNormal)
    {
        if (done) return;
        done = true;

        if (impactFx) Instantiate(impactFx, transform.position, Quaternion.identity);

        if (owner != null)
        {
            var ownerRb = owner.GetComponent<Rigidbody2D>();
            if (ownerRb != null)
            {
                // Güvenli yer bul
                Vector2 safe;
                if (!FindSafePlacement(target, surfaceNormal, out safe))
                {
                    // Çok sıkışıksa, en azından yüzey normal yönüne biraz daha iter
                    if (surfaceNormal.HasValue)
                        safe = target + surfaceNormal.Value.normalized * (surfacePlaceOffset + 0.2f);
                    else
                        safe = target;
                }

                ownerRb.position = safe;
                ownerRb.linearVelocity = Vector2.zero;

                if (teleportFx) Instantiate(teleportFx, safe, Quaternion.identity);
            }
        }

        Destroy(gameObject);
    }

    private bool FindSafePlacement(Vector2 target, Vector2? surfaceNormal, out Vector2 safePoint)
    {
        safePoint = target;

        if (!Physics2D.OverlapCircle(target, safeRadius, surfaceMask))
            return true;

        if (surfaceNormal.HasValue)
        {
            Vector2 n = surfaceNormal.Value.normalized;
            for (int i = 0; i < outwardSteps; i++)
            {
                Vector2 test = target + n * (surfacePlaceOffset + (i + 1) * 0.1f);
                if (!Physics2D.OverlapCircle(test, safeRadius, surfaceMask))
                {
                    safePoint = test;
                    return true;
                }
            }
        }

        // Küçük çember taraması
        const int samples = 16;
        for (int i = 0; i < samples; i++)
        {
            float ang = (Mathf.PI * 2f) * (i / (float)samples);
            Vector2 dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
            Vector2 test = target + dir * 0.5f;
            if (!Physics2D.OverlapCircle(test, safeRadius, surfaceMask))
            {
                safePoint = test;
                return true;
            }
        }
        return false;
    }
}
