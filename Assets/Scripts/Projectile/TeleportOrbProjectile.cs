using UnityEngine;

/// <summary>
/// El bombası gibi atılır. Gecikme bitince düştüğü noktaya SAHİBİ ışınlar.
/// Eğer bölgede geçerli bir yerçekimi kaynağı (GravitySource) YOKSA ışınlama İPTAL olur.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class TeleportOrbProjectile : MonoBehaviour
{
    [Header("Zamanlayıcı")]
    [Tooltip("Işınlama öncesi bekleme süresi")]
    public float delayBeforeTeleport = 2.5f;

    [Header("Görsel/Etki")]
    public GameObject spawnFxPrefab;
    public GameObject teleportFxPrefab;
    public float fxScale = 1f;

    [Header("Güvenli Yerleştirme")]
    [Tooltip("Karakter kapsül/daire yarıçapına yakın bir değer")]
    public float safeRadius = 0.35f;
    [Tooltip("Yüzeye doğru normal boyunca dışarı itme mesafesi")]
    public float surfaceSnap = 0.2f;
    [Tooltip("Duvar/zemin katmanı (teleport anında gömülmeyi engellemek için)")]
    public LayerMask groundMask = ~0; // isterseniz spesifik katman verin

    [Header("Sahibi Bir Süre Yoksay")]
    public float ignoreOwnerTime = 0.6f;

    [Header("Gravity Ayarları")]
    [Tooltip("Gravity yoksa ışınlamayı iptal et")]
    public bool cancelIfNoGravity = true;
    [Tooltip("GravitySource ararken bakılacak yarıçap (dünya boyutunuza göre)")]
    public float gravitySearchRadius = 100f;

    private Rigidbody2D rb;
    private Collider2D col;
    private GameObject owner;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        if (spawnFxPrefab) SpawnFx(spawnFxPrefab, transform.position);
    }

    /// <summary>
    /// El bombasında olduğu gibi çağırın.
    /// </summary>
    public void Init(Vector2 initialVelocity, GameObject ownerObj, float ignoreTime)
    {
        owner = ownerObj;
        rb.linearVelocity = initialVelocity;

        // Sahibi çarpmadan muaf tut
        foreach (var oc in owner.GetComponentsInChildren<Collider2D>())
            Physics2D.IgnoreCollision(col, oc, true);

        // Süre sonunda geri aç
        Invoke(nameof(ReenableOwnerCollision), Mathf.Max(ignoreTime, ignoreOwnerTime));

        // Teleport zamanlayıcı
        Invoke(nameof(TryTeleportOwner), delayBeforeTeleport);
    }

    private void TryTeleportOwner()
    {
        Vector2 orbPos = transform.position;

        // 1) Gravity var mı?
        GravitySource nearest;
        Vector2 gravityDir; // merkeze doğru (planet center - pos).normalized
        bool hasGravity = TryFindNearestGravity(orbPos, out nearest, out gravityDir);

        if (cancelIfNoGravity && !hasGravity)
        {
            Debug.Log("[TeleportOrb] Gravity bulunamadı. Işınlama İPTAL.");
            Cleanup();
            return;
        }

        // 2) Güvenli nokta bul: zemine gömülmemek için yüzey normali (-gravityDir) yönünde hafif ötele
        //    Gravity yoksa sadece overlap çözümü yap
        Vector2 outward = hasGravity ? (-gravityDir) : Vector2.up;
        Vector2 target = orbPos + outward * surfaceSnap;

        // Eğer hedefte çakışma varsa, küçük adımlarla dışarı it
        target = ResolveOverlap(target, outward);

        // 3) Sahibi ışınla
        if (owner != null && owner.TryGetComponent<Rigidbody2D>(out var ownerRb))
        {
            ownerRb.position = target;
            ownerRb.linearVelocity = Vector2.zero;

            // Karakteri yüzeye dik hizala (varsa)
            if (hasGravity)
            {
                // Transform.up'ı yüzey normaline (outward) eşitle
                owner.transform.up = outward;
            }
        }

        if (teleportFxPrefab) SpawnFx(teleportFxPrefab, target);
        Debug.Log("[TeleportOrb] Işınlama gerçekleşti.");
        Cleanup();
    }

    /// <summary>
    /// GravitySource bulur; en yakını döner ve gravity yönünü (merkeze doğru) verir.
    /// Projede GravitySource collider ile gezegen merkezinde duruyorsa basitçe merkez vektörünü kullanıyoruz.
    /// </summary>
    private bool TryFindNearestGravity(Vector2 pos, out GravitySource nearest, out Vector2 gravityDir)
    {
        nearest = null;
        gravityDir = Vector2.down;

        // Geniş bir dairede GravitySource ara
        var hits = Physics2D.OverlapCircleAll(pos, gravitySearchRadius);
        float best = float.PositiveInfinity;

        foreach (var h in hits)
        {
            if (h == null) continue;
            var gs = h.GetComponentInParent<GravitySource>();
            if (gs == null) continue;

            float d = Vector2.Distance(pos, (Vector2)gs.transform.position);
            if (d < best)
            {
                best = d;
                nearest = gs;
            }
        }

        if (nearest != null)
        {
            gravityDir = ((Vector2)nearest.transform.position - pos).normalized; // merkeze doğru
            return true;
        }
        return false;
    }

    /// <summary>
    /// Çakışma varsa outward yönünde küçük adımlarla güvenli bir nokta bulur.
    /// </summary>
    private Vector2 ResolveOverlap(Vector2 start, Vector2 outward)
    {
        const int maxIters = 10;
        Vector2 p = start;

        for (int i = 0; i < maxIters; i++)
        {
            var overlap = Physics2D.OverlapCircle(p, safeRadius, groundMask);
            if (overlap == null) break; // güvenli

            // azıcık daha dışarı
            p += outward * 0.1f;
        }

        // Ek güvenlik: hedef yolunda duvar varsa en yakın serbest noktaya yerleş
        var hit = Physics2D.CircleCast(start - outward * 0.01f, safeRadius, outward, 0.25f, groundMask);
        if (hit.collider != null)
        {
            p = hit.centroid - outward * (safeRadius + 0.02f);
        }
        return p;
    }

    private void ReenableOwnerCollision()
    {
        if (owner == null || col == null) return;
        foreach (var oc in owner.GetComponentsInChildren<Collider2D>())
            Physics2D.IgnoreCollision(col, oc, false);
    }

    private void SpawnFx(GameObject prefab, Vector2 pos)
    {
        var fx = Instantiate(prefab, pos, Quaternion.identity);
        fx.transform.localScale = Vector3.one * fxScale;
    }

    private void Cleanup()
    {
        Destroy(gameObject);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, safeRadius);
        Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, gravitySearchRadius * 0.1f); // referans
    }
#endif
}
