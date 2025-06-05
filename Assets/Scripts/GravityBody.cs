// GravityBody.cs
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class GravityBody : MonoBehaviour
{
    [HideInInspector] public bool isActive = false;
    Rigidbody2D rb;
    GravitySource currentSource;

    [Header("Yürüme Ayarları")]
    public float maxWalkSpeed = 3f;
    public float maxAirSpeed = 3f;
    [Range(0f, 1f)] public float smoothing = 0f;

    [Header("Zıplama Ayarları")]
    public float jumpForce = 5f;
    public float jumpCooldown = 10f;

    [Header("Skill: Super Jump")]
    public KeyCode skillKey = KeyCode.Alpha5;
    public float skillMultiplier = 2f;

    bool skillRequested = false;
    bool superJumpActive = false;
    float jumpTimer = 0f;

    // Double jump için ek alan
    bool canDoubleJump = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        Debug.Log("[Awake] " + name);
    }

    void Update()
    {
        if (!isActive) return;

        // Skill tetikleme: 5 tuşuna basıldıysa onay mesajını göster
        if (Input.GetKeyDown(skillKey) && !skillRequested && !superJumpActive)
        {
            skillRequested = true;
        }

        // Onay için Enter tuşu kullanılıyor
        if (skillRequested && Input.GetKeyDown(KeyCode.Return))
        {
            superJumpActive = true;
            skillRequested = false;
            Debug.Log("[Skill] Super Jump ACTIVE");
        }

        // Jump cooldown
        jumpTimer -= Time.deltaTime;

        // Zıplama (W tuşu)
        if (Input.GetKeyDown(KeyCode.W) && jumpTimer <= 0f)
        {
            if (currentSource != null)
            {
                // İlk zıplama: gezegen üzerindeyse
                float appliedForce = jumpForce;
                if (superJumpActive)
                {
                    appliedForce = jumpForce * skillMultiplier;
                    superJumpActive = false;
                    Debug.Log("[Skill] Super Jump USED");
                }

                Vector2 outDir = (transform.position - currentSource.transform.position).normalized;
                rb.AddForce(outDir * appliedForce, ForceMode2D.Impulse);
                jumpTimer = jumpCooldown;
                canDoubleJump = true;  // Havaya çıktıktan sonra çift zıplama izni ver
                Debug.Log($"[Jump] impulse={appliedForce}");
            }
            else if (canDoubleJump)
            {
                // İkinci zıplama: havadaysa ve izin varsa
                float appliedForce = jumpForce;
                if (superJumpActive)
                {
                    appliedForce = jumpForce * skillMultiplier;
                    superJumpActive = false;
                    Debug.Log("[Skill] Super Jump USED");
                }

                rb.AddForce(Vector2.up * appliedForce, ForceMode2D.Impulse);
                jumpTimer = jumpCooldown;
                canDoubleJump = false;  // Çift zıplama kullanıldı
                Debug.Log($"[DoubleJump] impulse={appliedForce}");
            }
        }
    }

    void FixedUpdate()
    {
        // --- Gezegen yerçekimi ve yönelim ---
        if (currentSource != null)
        {
            Vector2 dir = (Vector2)(currentSource.transform.position - transform.position);
            float d = dir.magnitude;
            if (d < currentSource.scaledRadius)
                rb.AddForce(dir.normalized * currentSource.scaledGravityForce);

            transform.up = -dir.normalized;
        }
        else
        {
            // Gezegen yerçekim alanından çıktıysa yatay hareket kodunu atla
            return;
        }

        if (!isActive)
            return;

        // --- Yürüme & Anında Durma ---
        float h = -Input.GetAxisRaw("Horizontal");
        bool grounded = (currentSource != null);
        float limit = grounded ? maxWalkSpeed : maxAirSpeed;
        float target = h * limit;

        Vector2 center = grounded
            ? (Vector2)(currentSource.transform.position - transform.position)
            : Vector2.up;
        Vector2 tangent = new Vector2(center.y, -center.x).normalized;

        float curr = Vector2.Dot(rb.linearVelocity, tangent);
        float diff = target - curr;
        float ns = curr + diff * (1f - smoothing);

        Vector2 normalComp = rb.linearVelocity - tangent * curr;
        rb.linearVelocity = normalComp + tangent * ns;

        if (Mathf.Approximately(h, 0f))
        {
            rb.linearVelocity = normalComp;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent(out GravitySource gs))
        {
            currentSource = gs;
            canDoubleJump = false;  // Gezegen yüzeyine yeniden indiğinde çift zıplama iznini sıfırla
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.TryGetComponent(out GravitySource gs) && gs == currentSource)
        {
            currentSource = null;
        }
    }

    /// <summary>
    /// Sadece yatay (tangent) bileşeni sıfırlar; dikey (yerçekimi/zipla) kalır.
    /// Bu sayede Tab tuşuna basıldığında yürüme momentumu anında kesilir.
    /// </summary>
    public void ZeroHorizontalVelocity()
    {
        if (currentSource != null)
        {
            Vector2 center = (Vector2)(currentSource.transform.position - transform.position);
            Vector2 tangent = new Vector2(center.y, -center.x).normalized;
            Vector2 vel = rb.linearVelocity;
            float along = Vector2.Dot(vel, tangent);
            rb.linearVelocity = vel - tangent * along;
        }
        else
        {
            Vector2 v = rb.linearVelocity;
            v.x = 0f;
            rb.linearVelocity = v;
        }
    }

    /// <summary>
    /// Yeni karakter aktif olduğunda çağrılır; zıplama/süreç durumlarını, yatay hızı temizler.
    /// </summary>
    public void OnTurnStart()
    {
        jumpTimer = 0f;
        superJumpActive = false;
        skillRequested = false;
        canDoubleJump = false;

        ZeroHorizontalVelocity();
        Debug.Log("[TurnStart] state reset for " + name);
    }

    void OnGUI()
    {
        // Super Jump için onay mesajı
        if (skillRequested)
        {
            GUI.Box(new Rect(Screen.width / 2 - 100, Screen.height / 2 - 25, 200, 50),
                    "Super Jump aktif olsun mu? [Enter]");
        }
    }
}
