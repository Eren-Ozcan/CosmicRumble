using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using CosmicRumble.Achievements;

[RequireComponent(typeof(GravityBody))]
public class BatHammerSkill : AbilityBase
{
    [Header("Onay & Cooldown")]
    public KeyCode activationKey = KeyCode.Alpha8;
    public float cooldownTime = 5f;

    public override int SlotIndex => 7;
    public override KeyCode ActivationKey => activationKey;
    public override float CooldownTime => cooldownTime;

    [Header("Koni (Hasar & Ön İzleme)")]
    public float hitRadius = 1.5f;
    [Range(10f, 179f)] public float coneAngleDeg = 100f;
    [Range(12, 256)] public int coneSegments = 48;
    public float knockbackForce = 10f;
    public bool onlyAffectTaggedPlayers = true;

    [Header("Drag / Güç")]
    public float maxDragDistance = 3f;

    [Header("Preview")]
    [Range(0f, 1f)] public float previewBaseAlpha = 0.20f;

    [Header("Ön İzleme Materyal")]
    public Material previewMaterial;

    private bool isDragging;
    private Vector2 dragStart;

    // Ön izleme cone mesh objesi
    private GameObject coneGO;
    private MeshFilter coneMF;
    private MeshRenderer coneMR;
    private Mesh coneMesh;

    protected override void Awake()
    {
        base.Awake();
        EnsureConeObjects();
        HideCone();
    }

    // Keypad8 alternatif tuş — AbilityBase sadece ActivationKey'i dinler
    protected override void Update()
    {
        if (gravityBody != null && gravityBody.isActive.Value &&
            (!gravityBody.IsSpawned || gravityBody.IsOwner) &&
            !isSelected && Input.GetKeyDown(KeyCode.Keypad8))
            charAbilities?.SelectSkill(SlotIndex);
        base.Update();
    }

    protected override void OnFireUpdate()
    {
        Vector2 mouseWorld = PointerWorldPosition;

        if (PointerDown)
        {
            isDragging = true;
            dragStart = mouseWorld;
            ShowCone();
            UpdateConePreview((Vector2)transform.position + Vector2.right);
        }
        else if (isDragging && PointerHeld)
        {
            Vector2 pull = dragStart - mouseWorld;
            Vector2 aimDir = pull.sqrMagnitude > 0.0001f ? pull.normalized : Vector2.right;
            UpdateConePreview((Vector2)transform.position + aimDir);
        }
        else if (isDragging && PointerUp)
        {
            Vector2 pull = dragStart - mouseWorld;
            float clamped = Mathf.Min(pull.magnitude, maxDragDistance);
            Vector2 aimDir = pull.sqrMagnitude > 0.0001f ? pull.normalized : Vector2.right;
            float power01 = (maxDragDistance > 0f) ? (clamped / maxDragDistance) : 0f;

            // Koni içinde hedef var mı? — bu, cooldown/ses/başarım "sadece isabet varsa tüketilsin"
            // kuralı için hâlâ SAHİBİNİN makinesinde (yerel) karar veriliyor; sıra tabanlı oyunda
            // sadece aktif karakter hareket ettiğinden hedeflerin konumu bu turda sabit — yerel ve
            // server'ın az sonra kendi tespitiyle bulacağı sonuç pratikte hep aynı.
            var targets = DetectTargets(aimDir);

            if (targets.Count > 0)
            {
                // Networked modda gerçek kuvvet uygulaması server'a taşınır (server kendi tespitini
                // tazeden yapar ve her hedefin gerçek sahibine GravityBody.ApplyForce ile ulaşır) —
                // offline hotseat'te eski doğrudan yerel yol aynen çalışır.
                if (IsSpawned) SwingServerRpc(aimDir, power01);
                else ApplyKnockback(targets, power01);

                cooldownTimer = cooldownTime;
                charAbilities?.OnAbilityConsumed();
                AchievementEvents.FireAbilityUsed("skill_bathammer");
                AudioManager.Instance?.PlaySfx("skill_bathammer_swing");
            }
#if UNITY_EDITOR
            else
            {
                Debug.Log("[BatHammer] No target in cone");
            }
#endif

            CancelAim();
            fireAllowed = false;
            isSelected = false;
        }
    }

    [ServerRpc]
    private void SwingServerRpc(Vector2 aimDir, float power01)
    {
        if (!ServerCanAct) return;
        if (charAbilities != null && !charAbilities.ServerTryConsume(SlotIndex)) return;
        // aimDir client'tan geliyor — birim vektör olduğu garanti değil (ör. bozuk/kötü niyetli
        // bir client normalize etmeden gönderebilir), bu da DetectTargets'ın Dot-karşılaştırmalı
        // koni testini bozar. Server kendi hesaplamasında her zaman normalize edilmiş haliyle çalışır.
        if (aimDir.sqrMagnitude < 0.0001f) return;
        var targets = DetectTargets(aimDir.normalized);
        ApplyKnockback(targets, power01);
    }

    protected override void CancelAim()
    {
        EndDrag();
        base.CancelAim(); // trajectory?.Hide() — no-op for BatHammer
    }

    // ----------------- KNOCKBACK (koni alanı) -----------------
    // Tespit (fizik sorgusu, yan etkisiz) ve uygulama (gerçek kuvvet) ayrıldı — sunucu, client'ın
    // "isabet var mı" kararını tekrar hesaplayıp asıl kuvveti kendisi uygulayabilsin diye
    // (bkz. SwingServerRpc).
    private struct KnockbackTarget
    {
        public GravityBody gravityBody;
        public Rigidbody2D rb;
        public Vector2 toTarget;
    }

    private List<KnockbackTarget> DetectTargets(Vector2 aimDir)
    {
        var result = new List<KnockbackTarget>();
        float halfRad = 0.5f * coneAngleDeg * Mathf.Deg2Rad;
        float cosHalf = Mathf.Cos(halfRad);

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, hitRadius);

        foreach (var hit in hits)
        {
            if (!hit || hit.gameObject == gameObject) continue;
            if (onlyAffectTaggedPlayers && !hit.CompareTag("Player")) continue;

            var rb = hit.attachedRigidbody;
            if (!rb) continue;

            Vector2 toTarget = ((Vector2)hit.transform.position - (Vector2)transform.position).normalized;
            if (Vector2.Dot(aimDir, toTarget) < cosHalf) continue; // koni dışında

            result.Add(new KnockbackTarget
            {
                gravityBody = rb.GetComponent<GravityBody>(),
                rb = rb,
                toTarget = toTarget
            });
        }

        return result;
    }

    private int ApplyKnockback(List<KnockbackTarget> targets, float power01)
    {
        float finalForce = knockbackForce * Mathf.Clamp01(power01);
        int affected = 0;

        foreach (var t in targets)
        {
            Vector2 force = t.toTarget * finalForce;

            // GravityBody.ApplyForce, sahibi bu makine değilse (networked modda bu her zaman
            // server'dır) doğru sahibin makinesine yönlendirir — offline'da veya sahip bizsek
            // doğrudan uygulanır.
            if (t.gravityBody != null) t.gravityBody.ApplyForce(force, ForceMode2D.Impulse);
            else t.rb.AddForce(force, ForceMode2D.Impulse);

            affected++;
#if UNITY_EDITOR
            Debug.Log($"[BatHammer] Knockback ({finalForce:F2}) -> {t.rb.name}");
#endif
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

    private void UpdateConePreview(Vector2 aimPoint)
    {
        if (!coneMesh) return;

        Vector2 origin = transform.position;
        Vector2 aimDir = (aimPoint - origin).sqrMagnitude > 0.0001f
            ? (aimPoint - origin).normalized
            : Vector2.right;

        float halfRad = 0.5f * coneAngleDeg * Mathf.Deg2Rad;

        int vertsCount = 1 + (coneSegments + 1);
        Vector3[] verts = new Vector3[vertsCount];
        Color[] colors = new Color[vertsCount];
        int[] tris = new int[coneSegments * 3];

        Color matColor = Color.HSVToRGB(0f, 1f, 0.95f);
        matColor.a = Mathf.Clamp01(previewBaseAlpha);

        verts[0] = Vector3.zero;
        colors[0] = Color.white;

        Quaternion rot = Quaternion.FromToRotation(Vector3.right, (Vector3)aimDir);

        for (int i = 0; i <= coneSegments; i++)
        {
            float t = (float)i / coneSegments;
            float ang = -halfRad + t * (2f * halfRad);
            Vector3 local = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f) * hitRadius;
            verts[1 + i] = rot * local;
            colors[1 + i] = Color.white;

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
        coneMesh.colors = colors;
        coneMesh.triangles = tris;
        coneMesh.RecalculateBounds();
        coneMesh.RecalculateNormals();

        coneGO.transform.position = origin;
        coneGO.transform.rotation = Quaternion.identity;

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
