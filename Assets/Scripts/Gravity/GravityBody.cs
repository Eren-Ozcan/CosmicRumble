// Assets/Scripts/Gravity/GravityBody.cs
using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class GravityBody : NetworkBehaviour
{
    // Sadece server yazar (TurnManager) — offline hotseat'te (IsSpawned=false) network'ten
    // bağımsız normal bir bool gibi davranır, hiçbir NetworkManager/spawn gerektirmez.
    public NetworkVariable<bool> isActive =
        new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    /// <summary>true iken yürüme ve zıplama inputu engellenir; silah ateşleme etkilenmez.</summary>
    [HideInInspector] public bool movementLocked = false;

    /// <summary>
    /// Super jump gerçekleştiğinde tetiklenir. UI temizliği için SuperJumpSkill subscribe olur.
    /// </summary>
    public event Action onSuperJumpConsumed;

    private Rigidbody2D rb;

    // Tüm aktif çekim kaynakları — vektörel toplama için
    private readonly List<GravitySource> activeSources = new List<GravitySource>();

    private readonly Vector2[] _rayOrigins = new Vector2[3];

    // En güçlü kaynak — zıplama yönü ve hareket tanjantı için
    public GravitySource DominantSource { get; private set; }

    [Header("Yürüme Ayarları")]
    public float maxWalkSpeed = 3f;
    public float maxAirSpeed = 3f;
    [Range(0f, 1f)]
    public float smoothing = 0f;

    [Header("Zıplama Ayarları")]
    public float jumpForce = 5f;
    public float superMultiplier = 2f;
    public float jumpCooldown = 10f;

    private float cooldownTimer = 0f;
    public bool nextJumpIsSuper = false;

    private int jumpCount = 0;
    private bool canDoubleJump = true;

    // Input Update'te okunur, FixedUpdate'te uygulanır (Kural #7)
    private float cachedHorizontalInput = 0f;

    // Net yerçekimi yönü — iki gezegen arası tangent kararlılığı için cache'lenir
    private Vector2 _gravDir = Vector2.down;

    [Header("Surface Angles")]
    public float walkAngleLimit  = 60f;
    public float stableAngleLimit = 75f;
    public float slideForce      = 5f;
    public float rotationSmoothSpeed = 15f;
    public float edgeTolerance = 0.1f;

    // ── Grounded detection ─────────────────────────────────────────────────────
    // GetContacts  → hasContact   (fizik motorundan, uçurum kenarında anında false)
    // edgeTimer    → _isGrounded  (coyote window — uçurum kenarında kısa tolerans)
    // 3× Raycast   → surfaceNormal (sol/orta/sağ — küçük deliklerde titreme engeli)
    // ──────────────────────────────────────────────────────────────────────────
    private bool  _isGrounded = false;
    private float _edgeTimer  = 0f;
    private int   _planetMask;

    private Collider2D            _col;
    private readonly ContactPoint2D[] _contacts = new ContactPoint2D[8];
    private readonly RaycastHit2D[]   _rayHits  = new RaycastHit2D[4];

    // Yüzey normal raycasti için sabitler
    private const float SurfaceRayOriginOffset = 0.3f;  // merkezden ayağa doğru offset
    private const float SurfaceRayDistance     = 0.9f;

    private void Awake()
    {
        rb   = GetComponent<Rigidbody2D>();
        _col = GetComponent<Collider2D>();
        rb.freezeRotation = true;
        _planetMask = LayerMask.GetMask("Planet");
    }

    private void Start()
    {
        // NetworkRigidbody2D.Awake() (AutoUpdateKinematicState=true) her zaman Kinematic'e zorlar ve
        // bunu sadece OnNetworkSpawn()/UpdateOwnershipAuthority() düzeltir — offline hotseat'te
        // NetworkObject hiç spawn edilmediği için bu düzeltme hiç çalışmaz, karakter kalıcı olarak
        // Kinematic kalır ve rb.AddForce() (zıplama, patlama kuvveti, knockback — kod tabanındaki her
        // AddForce çağrısı) sessizce no-op'a döner. Start() tüm Awake()'lerden sonra çalıştığı için
        // (component sırasından bağımsız) burada güvenle düzeltilebilir; networked modda (IsSpawned
        // sonradan true olacak) hiçbir şey yapmadan çıkar, OnNetworkSpawn kendi mantığını uygular.
        if (!IsSpawned)
            rb.bodyType = RigidbodyType2D.Dynamic;
    }

    public override void OnNetworkSpawn()
    {
        isActive.OnValueChanged += (oldValue, newValue) =>
            Debug.Log($"[TURN] {name} isActive {oldValue}->{newValue} IsOwner={IsOwner} frame={Time.frameCount}");
    }

    private void Update()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;

        // IsSpawned=false → offline hotseat, herkes tek makineden kontrol edilir (eski davranış).
        // IsSpawned=true  → networked, sadece bu GravityBody'nin sahibi kendi input'unu uygulayabilir
        // (aksi halde her client'in klavyesi kimin sirasi olursa olsun o karakteri hareket ettirirdi).
        if (!isActive.Value || (IsSpawned && !IsOwner))
            return;

        if (movementLocked)
        {
            cachedHorizontalInput = 0f;
            return;
        }

        // Input is cached this frame in Update (keys are rebindable via GameConfig)
        var cfg = GameConfig.Instance;
        KeyCode leftKey  = cfg != null ? cfg.MoveLeftKey  : KeyCode.A;
        KeyCode rightKey = cfg != null ? cfg.MoveRightKey : KeyCode.D;
        KeyCode jumpKey  = cfg != null ? cfg.JumpKey      : KeyCode.Space;

        float h = 0f;
        if (Input.GetKey(leftKey))  h += 1f;
        if (Input.GetKey(rightKey)) h -= 1f;
        cachedHorizontalInput = h;

        bool grounded = _isGrounded;

        if (cooldownTimer <= 0f && Input.GetKeyDown(jumpKey))
        {
            if (grounded)
            {
                PerformJump(nextJumpIsSuper);
                jumpCount    = 1;
                canDoubleJump = true;
            }
            else if (jumpCount == 1 && canDoubleJump)
            {
                PerformJump(nextJumpIsSuper);
                jumpCount    = 2;
                canDoubleJump = false;
            }
            else
            {
                return;
            }

            cooldownTimer = jumpCooldown;
            nextJumpIsSuper = false;
        }
    }

    private void FixedUpdate()
    {
        // Networked modda non-owner peer'larda Rigidbody2D NetworkRigidbody2D tarafından kinematik
        // yapılır (dış kuvvetleri zaten yok sayar), ama doğrudan linearVelocity ataması hâlâ
        // etkili olurdu — bu satır olmadan aşağıdaki zone-tabanlı hız ayarları NetworkTransform'un
        // replike ettiği pozisyonla çakışırdı. Offline hotseat'te (IsSpawned=false) devre dışı.
        if (IsSpawned && !IsOwner) return;

        // ── 1. Yerçekimi yönü ve baskın kaynak ────────────────────────────────
        // Null kaynakları temizle
        for (int i = activeSources.Count - 1; i >= 0; i--)
            if (activeSources[i] == null) activeSources.RemoveAt(i);

        Vector2 netGravity = (GravityManager.Instance != null && GravityManager.Instance.Strategy != null)
            ? GravityManager.Instance.Strategy.CalculateAcceleration(transform.position)
            : Vector2.zero;

        var allSrc = GravitySource.AllSources;
        DominantSource = allSrc.Count > 0 ? allSrc[0] : null;

        if (netGravity.sqrMagnitude > 0.001f)
            _gravDir = netGravity.normalized;

        // ── 2. Grounded tespiti — GetContacts + edge tolerance (coyote time) ──
        // hasContact: fizik motorundan gerçek temas (uçurum kenarında anında false)
        // _edgeTimer: kenardan ayrılınca kısa tolerans penceresi
        int contactCount = _col.GetContacts(_contacts);
        bool hasContact = false;
        for (int i = 0; i < contactCount; i++)
        {
            if ((_planetMask & (1 << _contacts[i].collider.gameObject.layer)) != 0)
            {
                hasContact = true;
                break;
            }
        }
        if (hasContact)
        {
            _isGrounded = true;
            _edgeTimer  = edgeTolerance;
        }
        else
        {
            _edgeTimer -= Time.fixedDeltaTime;
            if (_edgeTimer <= 0f)
                _isGrounded = false;
        }

        // ── 3. Yüzey normali — 3× Raycast (sol/orta/sağ, titreme engeli) ────────
        // Planet_Interior'ın solid CircleCollider2D'si her zaman radyal normal döndürür.
        // Planet_External'ın trigger PolygonCollider2D'si gerçek yüzey normaline sahip.
        // Üç ray'ın ortalaması: küçük delikler veya köşe geçişlerinde rotasyon titremesini engeller.
        Vector2 moveDir = new Vector2(_gravDir.y, -_gravDir.x);

        var filter = new ContactFilter2D();
        filter.SetLayerMask(_planetMask);
        filter.useTriggers = true;

        _rayOrigins[0] = (Vector2)transform.position + moveDir * 0.3f  + _gravDir * SurfaceRayOriginOffset;
        _rayOrigins[1] = (Vector2)transform.position                   + _gravDir * SurfaceRayOriginOffset;
        _rayOrigins[2] = (Vector2)transform.position - moveDir * 0.3f  + _gravDir * SurfaceRayOriginOffset;

        Vector2 normalSum  = Vector2.zero;
        int     normalCount = 0;
        for (int r = 0; r < 3; r++)
        {
            int hitCount = Physics2D.Raycast(_rayOrigins[r], _gravDir, filter, _rayHits, SurfaceRayDistance);

            // PolygonCollider2D'yi önce ara; bulunamazsa solid CircleCollider2D'ye düş
            RaycastHit2D best = default;
            RaycastHit2D fallback = default;
            for (int i = 0; i < hitCount; i++)
            {
                if (_rayHits[i].collider is PolygonCollider2D)
                { best = _rayHits[i]; break; }
                else if (fallback.collider == null)
                    fallback = _rayHits[i];
            }
            if (best.collider == null) best = fallback;

            if (best.collider != null)
            {
                normalSum += best.normal;
                normalCount++;
            }
        }

        Vector2 surfaceNormal = normalCount > 0 ? (normalSum / normalCount).normalized : (Vector2)(-_gravDir);
        float   surfaceAngle  = normalCount > 0
            ? Vector2.Angle(surfaceNormal, -_gravDir)
            : 0f;

        // ── 4. Rotasyon ───────────────────────────────────────────────────────
        // Zone 1-2 (≤75°): yüzey normaline doğru smooth lerp → ayaklar yüzeye dik
        // Zone 3 (>75°) / havada: yerçekimi yönüne göre radyal
        // Force is applied by GravitySource.OnTriggerStay2D — only update orientation here
        Vector2 targetUp = (_isGrounded && surfaceAngle <= stableAngleLimit)
            ? surfaceNormal
            : (Vector2)(-_gravDir);
        transform.up = Vector2.Lerp((Vector2)transform.up, targetUp,
                                    rotationSmoothSpeed * Time.fixedDeltaTime);

        // ── 5. Hareket yönü ───────────────────────────────────────────────────
        // moveDir step 3'te tanımlandı — burada tekrar tanımlanmaz

        // ── 6. Sıra dışı: Zone 1-2'de tangential hızı sıfırla (kayma engeli) ──
        // Bu, tur senkronu (isActive) durumuna göre TÜM client'larda aynı şekilde uygulanır —
        // sahiplik kontrolü gerekmez, sadece "bu karakter aktif değil" fiziksel sabitlemesi.
        if (!isActive.Value)
        {
            if (_isGrounded && surfaceAngle <= stableAngleLimit)
            {
                Vector2 v = rb.linearVelocity;
                rb.linearVelocity = v - moveDir * Vector2.Dot(v, moveDir);
            }
            return;
        }

        // ── 7. Aktif hareket zone'ları ─────────────────────────────────────────
        Vector2 vel         = rb.linearVelocity;
        float   currMoveVel = Vector2.Dot(vel, moveDir);
        Vector2 normalComp  = vel - moveDir * currMoveVel;

        if (!_isGrounded)
        {
            // Havada: input varsa yönlendir, yoksa fizik tamamen serbest.
            // Tangential momentum SIFIRLANMAZ — uçurumdan doğal düşüş sağlar.
            if (!Mathf.Approximately(cachedHorizontalInput, 0f))
            {
                float newMoveVel = Mathf.Lerp(currMoveVel, cachedHorizontalInput * maxAirSpeed, 1f - smoothing);
                rb.linearVelocity = normalComp + moveDir * newMoveVel;
            }
        }
        else if (surfaceAngle < walkAngleLimit)
        {
            // Zone 1 — Yürünebilir: hareket yüzey tanjantı boyunca uygulanır.
            // moveDir (yerçekimine dik, yatay) değil surfaceTangent (yüzey normaline dik)
            // kullanılır — eğime karşı yürüyünce karakterin yüzeyden kalkması engellenir.
            Vector2 surfaceTangent = new Vector2(surfaceNormal.y, -surfaceNormal.x);
            if (Vector2.Dot(surfaceTangent, moveDir) < 0f)
                surfaceTangent = -surfaceTangent;

            float   tangVel       = Vector2.Dot(vel, surfaceTangent);
            Vector2 surfNormalComp = vel - surfaceTangent * tangVel;   // yüzeye dik bileşen korunur

            if (Mathf.Approximately(cachedHorizontalInput, 0f))
                rb.linearVelocity = surfNormalComp;
            else
                rb.linearVelocity = surfNormalComp + surfaceTangent * (cachedHorizontalInput * maxWalkSpeed);
        }
        else if (surfaceAngle <= stableAngleLimit)
        {
            // Zone 2 — Stabil: hareket yok, kayma yok
            rb.linearVelocity = normalComp;
        }
        else
        {
            // Zone 3 — Kaygan: yüzey boyunca aşağı kuvvet, input yok
            Vector2 downhill = _gravDir - Vector2.Dot(_gravDir, surfaceNormal) * surfaceNormal;
            if (downhill.sqrMagnitude > 0.001f)
                rb.AddForce(downhill.normalized * slideForce, ForceMode2D.Force);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var gs = other.GetComponentInParent<GravitySource>();
        if (gs != null)
        {
            bool wasAirborne = activeSources.Count == 0;
            if (!activeSources.Contains(gs))
                activeSources.Add(gs);

            // Zıplama sayacını yalnızca gerçekten iniş yapıldığında sıfırla
            // (önceden başka bir gravity zone'da grounded idiyse sıfırlama — exploit kapatılır)
            if (wasAirborne)
            {
                jumpCount    = 0;
                canDoubleJump = true;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        var gs = other.GetComponentInParent<GravitySource>();
        if (gs != null)
        {
            activeSources.Remove(gs);

            // Tüm gravity zone'lardan çıkıldıysa (havaya kalkış): yere basmadan çıkıldıysa
            // jumpCount=0 anlamına gelir — yerden zıplamak gibi sayılır ki
            // havada double-jump hakkı doğru şekilde kalsın.
            if (activeSources.Count == 0 && jumpCount == 0)
                jumpCount = 1;
        }
    }

    private void PerformJump(bool isSuper)
    {
        Vector2 outDir;
        if (DominantSource != null)
            outDir = (transform.position - DominantSource.transform.position).normalized;
        else
            outDir = transform.up; // Uzayda — mevcut "yukarı" yönünde zıpla

        float force = isSuper ? jumpForce * superMultiplier : jumpForce;
        rb.AddForce(outDir * force, ForceMode2D.Impulse);

        if (isSuper)
            onSuperJumpConsumed?.Invoke();
    }

    public void ZeroHorizontalVelocity()
    {
        Vector2 vel     = rb.linearVelocity;
        Vector2 moveDir = new Vector2(_gravDir.y, -_gravDir.x);
        float   along   = Vector2.Dot(vel, moveDir);
        rb.linearVelocity = vel - moveDir * along;
    }

    public void OnTurnStart()
    {
        cooldownTimer   = 0f;
        nextJumpIsSuper = false;
        ZeroHorizontalVelocity();
        jumpCount    = 0;
        canDoubleJump = true;

        var ch = GetComponent<CharacterHealth>();
        if (ch != null)
            ch.SetShielded(false);
    }

    // ── Cross-machine effect helpers ───────────────────────────────────────
    // BlackHoleZone'un çekimi, BatHammerSkill'in knockback'i ve TeleportOrbProjectile'ın
    // ışınlaması hep bu karakterin Rigidbody2D'sine doğrudan yazıyordu — offline'da (tek process)
    // sorunsuz çalışır, ama networked modda bu etkiler sadece server'da tetiklenir (bkz. ilgili
    // ServerRpc'ler) ve Player.prefab'ın NetworkTransform'u Owner Authoritative: server'ın (veya
    // herhangi bir non-owner peer'ın) bu karakterin Rigidbody2D'sine yaptığı doğrudan yazı ya hiç
    // etki etmez (AddForce, NetworkRigidbody2D'nin non-owner'da otomatik kinematic yaptığı body'de
    // no-op'tur) ya da gerçek sahibin bir sonraki authoritative güncellemesiyle ezilir (position).
    // Çözüm: sahibi DEĞİLSEK ama server isek, gerçek sahibin makinesine hedefli bir ClientRpc ile
    // "bunu sen uygula" de — offline'da veya zaten sahibiysek (örn. host kendi karakterini
    // etkiliyorsa) doğrudan uygulanır, ekstra round-trip yok.
    public void ApplyForce(Vector2 force, ForceMode2D mode)
    {
        if (!IsSpawned || IsOwner)
        {
            rb.AddForce(force, mode);
            return;
        }
        if (IsServer)
        {
            var rpcParams = new ClientRpcParams
            { Send = new ClientRpcSendParams { TargetClientIds = new[] { OwnerClientId } } };
            ApplyForceClientRpc(force, mode, rpcParams);
        }
    }

    [ClientRpc]
    private void ApplyForceClientRpc(Vector2 force, ForceMode2D mode, ClientRpcParams rpcParams = default)
    {
        if (!IsOwner) return; // güvenlik: hedefli RPC zaten sadece sahibine gider
        rb.AddForce(force, mode);
    }

    public void Teleport(Vector2 position, Vector2 up)
    {
        if (!IsSpawned || IsOwner)
        {
            ApplyTeleportLocal(position, up);
            return;
        }
        if (IsServer)
        {
            var rpcParams = new ClientRpcParams
            { Send = new ClientRpcSendParams { TargetClientIds = new[] { OwnerClientId } } };
            TeleportClientRpc(position, up, rpcParams);
        }
    }

    [ClientRpc]
    private void TeleportClientRpc(Vector2 position, Vector2 up, ClientRpcParams rpcParams = default)
    {
        if (!IsOwner) return;
        ApplyTeleportLocal(position, up);
    }

    private void ApplyTeleportLocal(Vector2 position, Vector2 up)
    {
        rb.position = position;
        rb.linearVelocity = Vector2.zero;
        transform.up = up;
    }
}
