using UnityEngine;

[RequireComponent(typeof(GravityBody))]
public class BatHammerSkill : MonoBehaviour, IAbilitySelectable, ICooldownResettable
{
    [Header("Onay & Cooldown")]
    public KeyCode activationKey = KeyCode.Alpha8;
    public float cooldownTime = 5f;
    private float cooldownTimer;
    private bool awaitingConfirmation;
    private bool fireAllowed;

    [Header("Koni (Hasar & Ön İzleme)")]
    public float hitRadius = 1.5f;
    [Range(10f, 179f)] public float coneAngleDeg = 100f;
    [Range(12, 256)] public int coneSegments = 48;
    public float knockbackForce = 10f;
    public LayerMask targetLayers;
    public bool onlyAffectTaggedPlayers = true;

    [Header("Drag / Güç")]
    public float maxDragDistance = 3f; // drag mesafesi → güç (0..1)

    [Header("Preview")]
    [Range(0f, 1f)] public float previewBaseAlpha = 0.20f; // sabit opaklık

    [Header("Ön İzleme Materyal")]
    public Material previewMaterial; // boşsa Sprites/Default ile oluşturulur

    private GravityBody gravityBody;
    private CharacterAbilities charAbilities;

    private bool wasActive;
    private bool isSelected;
    private bool isDragging;
    private Vector2 dragStart;

    // Ön izleme cone mesh objesi
    private GameObject coneGO;
    private MeshFilter coneMF;
    private MeshRenderer coneMR;
    private Mesh coneMesh;

    public int SlotIndex => 7;

    void Awake()
    {
        gravityBody = GetComponent<GravityBody>();
        charAbilities = GetComponent<CharacterAbilities>();
        EnsureConeObjects();
        HideCone();
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        awaitingConfirmation = selected;
        fireAllowed = false;
        if (!selected) EndDrag();
    }

    public void Cancel()
    {
        awaitingConfirmation = false;
        fireAllowed = false;
        EndDrag();
    }

    public void ResetCooldown()
    {
        cooldownTimer = 0f;
        awaitingConfirmation = false;
        fireAllowed = false;
        isSelected = false;
        EndDrag();
    }

    void Update()
    {
        if (charAbilities != null && charAbilities.HasUsedSkillThisTurn) return;
        if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;

        // gravity aktif/pasif geçişleri
        if (gravityBody.isActive && !wasActive) { wasActive = true; Cancel(); }
        else if (!gravityBody.isActive) { wasActive = false; return; }

        if (cooldownTimer > 0f) { EndDrag(); return; }

        // Seçim
        if (!isSelected)
        {
            if (Input.GetKeyDown(activationKey) || Input.GetKeyDown(KeyCode.Keypad8))
                charAbilities?.SelectSkill(SlotIndex);
            return;
        }

        // Onay
        if (awaitingConfirmation)
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                fireAllowed = true;
                awaitingConfirmation = false;
                UIManager.Instance?.ConfirmSkill(SlotIndex);
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                charAbilities?.DeselectAll();
            }
            return;
        }

        if (!fireAllowed) return;

        // --- DRAG: koni ön izleme + güç ---
        Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        if (Input.GetMouseButtonDown(0))
        {
            isDragging = true;
            dragStart = mouseWorld;
            ShowCone();
            UpdateConePreview((Vector2)transform.position + Vector2.right); // ilk frame
        }
        else if (isDragging && Input.GetMouseButton(0))
        {
            Vector2 pull = dragStart - mouseWorld; // Pistol ile aynı mantık
            float clamped = Mathf.Min(pull.magnitude, maxDragDistance);
            Vector2 aimDir = pull.sqrMagnitude > 0.0001f ? pull.normalized : Vector2.right;

            UpdateConePreview((Vector2)transform.position + aimDir);
        }
        else if (isDragging && Input.GetMouseButtonUp(0))
        {
            Vector2 pull = dragStart - mouseWorld;
            float clamped = Mathf.Min(pull.magnitude, maxDragDistance);
            Vector2 aimDir = pull.sqrMagnitude > 0.0001f ? pull.normalized : Vector2.right;
            float power01 = (maxDragDistance > 0f) ? (clamped / maxDragDistance) : 0f;

            int affected = PerformKnockback(aimDir, power01);

            if (affected > 0)
            {
                cooldownTimer = cooldownTime;
                charAbilities?.OnAbilityConsumed();
            }
            else
            {
                Debug.Log("[BatHammer] No target in cone");
            }

            EndDrag();
            fireAllowed = false;
            isSelected = false;
        }
    }

    // ----------------- KNOCKBACK (koni alanı) -----------------
    private int PerformKnockback(Vector2 aimDir, float power01)
    {
        int affected = 0;
        float finalForce = knockbackForce * Mathf.Clamp01(power01);
        float halfRad = 0.5f * coneAngleDeg * Mathf.Deg2Rad;
        float cosHalf = Mathf.Cos(halfRad);

        Collider2D[] hits = (targetLayers.value != 0)
            ? Physics2D.OverlapCircleAll(transform.position, hitRadius, targetLayers)
            : Physics2D.OverlapCircleAll(transform.position, hitRadius);

        foreach (var hit in hits)
        {
            if (!hit || hit.gameObject == gameObject) continue;
            if (onlyAffectTaggedPlayers && !hit.CompareTag("Player")) continue;

            var rb = hit.attachedRigidbody;
            if (!rb) continue;

            Vector2 toTarget = ((Vector2)hit.transform.position - (Vector2)transform.position).normalized;
            if (Vector2.Dot(aimDir, toTarget) < cosHalf) continue; // koni dışında

            rb.AddForce(toTarget * finalForce, ForceMode2D.Impulse);
            affected++;
            Debug.Log($"[BatHammer] Knockback ({finalForce:F2}) -> {hit.name}");
        }

        return affected;
    }

    // ----------------- ÖN İZLEME (doldurulmuş koni mesh) -----------------
    private void EnsureConeObjects()
    {
        if (coneGO) return;

        coneGO = new GameObject("ConePreview");
        coneGO.transform.SetParent(transform, false);
        coneGO.transform.localPosition = Vector3.zero;

        coneMF = coneGO.AddComponent<MeshFilter>();
        coneMR = coneGO.AddComponent<MeshRenderer>();
        coneMesh = new Mesh { name = "ConeMesh" };
        coneMF.sharedMesh = coneMesh;

        if (!previewMaterial)
        {
            var shader = Shader.Find("Sprites/Default");
            previewMaterial = new Material(shader);
        }
        // Sabit: parlak kırmızı (H=0, S=1, V=0.95) ve sabit opaklık
        var c0 = Color.HSVToRGB(0f, 1f, 0.95f);
        c0.a = Mathf.Clamp01(previewBaseAlpha);
        previewMaterial.color = c0;

        coneMR.sharedMaterial = previewMaterial;
        coneMR.sortingOrder = 1000;
    }

    private void ShowCone()
    {
        if (coneGO) coneGO.SetActive(true);
    }

    private void HideCone()
    {
        if (coneGO) coneGO.SetActive(false);
    }

    /// <summary>
    /// aimPoint: world uzayında yön için referans nokta (origin → aimDir)
    /// </summary>
    private void UpdateConePreview(Vector2 aimPoint)
    {
        if (!coneMesh) return;

        // Yön ve dönüş
        Vector2 origin = transform.position;
        Vector2 aimDir = (aimPoint - origin).sqrMagnitude > 0.0001f
            ? (aimPoint - origin).normalized
            : Vector2.right;

        float halfRad = 0.5f * coneAngleDeg * Mathf.Deg2Rad;

        // Vertexler (fan): merkez + yay
        int vertsCount = 1 + (coneSegments + 1);
        Vector3[] verts = new Vector3[vertsCount];
        Color[] colors = new Color[vertsCount];
        int[] tris = new int[coneSegments * 3];

        // Sabit kırmızı/alpha sadece material'dan gelir; vertex renkleri BEYAZ.
        Color matColor = Color.HSVToRGB(0f, 1f, 0.95f);
        matColor.a = Mathf.Clamp01(previewBaseAlpha);

        verts[0] = Vector3.zero;      // merkez (local)
        colors[0] = Color.white;

        // aimDir’i +X eksenine hizala
        Quaternion rot = Quaternion.FromToRotation(Vector3.right, (Vector3)aimDir);

        for (int i = 0; i <= coneSegments; i++)
        {
            float t = (float)i / coneSegments;            // 0..1
            float ang = -halfRad + t * (2f * halfRad);    // -half .. +half
            Vector3 local = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f) * hitRadius;
            verts[1 + i] = rot * local;
            colors[1 + i] = Color.white;                  // tüm yay noktaları beyaz

            if (i < coneSegments)
            {
                int triIndex = i * 3;
                tris[triIndex + 0] = 0;
                tris[triIndex + 1] = 1 + i;
                tris[triIndex + 2] = 2 + i;
            }
        }

        coneMesh.Clear();
        coneMesh.vertices = verts;
        coneMesh.colors = colors;     // material rengi boyar
        coneMesh.triangles = tris;
        coneMesh.RecalculateBounds();
        coneMesh.RecalculateNormals();

        // world pozisyonu (origin)
        coneGO.transform.position = origin;
        coneGO.transform.rotation = Quaternion.identity;

        // Materyali sabit kırmızı/alpha yap (emniyet)
        if (previewMaterial != null)
            previewMaterial.color = matColor;
    }

    private void EndDrag()
    {
        isDragging = false;
        HideCone();
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // debug koni yay çizimi
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.4f);
        Vector3 o = transform.position;
        float half = coneAngleDeg * 0.5f * Mathf.Deg2Rad;
        Vector3 left = new Vector3(Mathf.Cos(-half), Mathf.Sin(-half), 0f) * hitRadius;
        Vector3 right = new Vector3(Mathf.Cos(half), Mathf.Sin(half), 0f) * hitRadius;
        Gizmos.DrawLine(o, o + left);
        Gizmos.DrawLine(o, o + right);
    }
#endif
}
