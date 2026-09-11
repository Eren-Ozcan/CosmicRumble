using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Bot karakterin turunu oynar: hedef seçer, gerekiyorsa yüzeyde ona doğru yürür, yerçekimi
/// altında bir atış çözümü arar ve ateş eder.
///
/// <para><b>Neden simülasyon:</b> mermiler çok gezegenli, değişken bir çekim alanında uçuyor —
/// kapalı formda bir balistik çözüm yok. Bot da insan oyuncunun gördüğü yörüngenin aynısını
/// (<see cref="GravityManager"/> stratejisi + aynı zaman adımı, bkz. TrajectoryDots) hesaplayıp
/// aday açı/güç çiftlerini puanlar. Önce kaba bir tarama, sonra en iyi adayın çevresinde ince
/// tarama yapılır; hepsini tek karede yapmak kare düşürdüğü için tarama karelere yayılır.</para>
///
/// <para><b>Yetki:</b> çevrimdışı maçta herkes tek makinede olduğu için doğrudan çalışır;
/// çevrimiçi maçta yalnızca sunucuda çalışır — botlar sunucunun sahip olduğu karakterlerdir,
/// bir client'ın onları sürmesi hem yetkisiz olurdu hem de her makinede farklı sonuç üretirdi.</para>
/// </summary>
[RequireComponent(typeof(GravityBody))]
public class BotBrain : MonoBehaviour
{
    // ── Ayarlar ───────────────────────────────────────────────────────────

    [Tooltip("0 = çaylak (geniş sapma), 1 = keskin nişancı.")]
    [Range(0f, 1f)] public float accuracy = 0.75f;

    [Tooltip("Ateşten önceki düşünme süresi — bot anında ateş ederse tur atlanmış gibi görünüyor.")]
    public float thinkDelay = 0.8f;

    [Tooltip("Bu mesafenin üstünde hedefe doğru yürümeyi dener.")]
    public float walkIfFartherThan = 6f;

    [Tooltip("Tek turda toplam en fazla bu kadar saniye yürür (yaklaşma + yeniden konumlanma).")]
    public float maxWalkSeconds = 3.5f;

    [Tooltip("Bu kadar birimden yakına düşen atış isabet sayılır; daha kötüyse bot yeniden konumlanıp tekrar arar.")]
    public float acceptableMiss = 2.5f;

    // ── Simülasyon sabitleri ──────────────────────────────────────────────

    const float SimStep      = 0.02f;   // TrajectoryDots.timeStep ile aynı (fixedDeltaTime)
    const int   SimMaxSteps  = 130;     // ~2.6 sn uçuş
    const int   CoarseAngles = 30;      // 12 derece adım
    const int   YieldEvery   = 24;      // bu kadar adaydan sonra bir kare bekle

    static readonly float[] CoarsePowers = { 0.35f, 0.55f, 0.75f, 0.95f };

    // Botun deneyeceği silahlar, tercih sırasıyla: önce hasarı yüksek olanlar.
    static readonly int[] WeaponSlots = { 2 /*Rpg*/, 3 /*HandGrenade*/, 0 /*Pistol*/ };

    // ── Durum ─────────────────────────────────────────────────────────────

    GravityBody        _body;
    CharacterAbilities _abilities;
    Coroutine          _turnRoutine;
    bool               _wasActive;

    void Awake()
    {
        _body      = GetComponent<GravityBody>();
        _abilities = GetComponent<CharacterAbilities>();
    }

    /// <summary>Bu makine botu sürmeye yetkili mi (çevrimdışı her zaman, çevrimiçi yalnız sunucu).</summary>
    static bool HasAuthority
    {
        get
        {
            var nm = NetworkManager.Singleton;
            return nm == null || !nm.IsListening || nm.IsServer;
        }
    }

    void Update()
    {
        if (_body == null || !_body.isBot) return;

        bool active = _body.isActive.Value && HasAuthority;

        if (active && !_wasActive && _turnRoutine == null)
        {
            _turnRoutine = StartCoroutine(PlayTurn());
        }
        else if (!active && _turnRoutine != null)
        {
            // Tur elden gitti (süre doldu, bot öldü, host göçü) — yarım kalan planı bırak.
            StopCoroutine(_turnRoutine);
            _turnRoutine       = null;
            _body.botMoveInput = 0f;
        }

        _wasActive = active;
    }

    // ══════════════════════════════════════════════════════════════════════
    //  TUR
    // ══════════════════════════════════════════════════════════════════════

    IEnumerator PlayTurn()
    {
        yield return new WaitForSeconds(thinkDelay);

        var target = PickTarget();
        if (target == null) { EndTurn(); yield break; }

        // 1) Çok uzaktaysa yüzey boyunca hedefe doğru biraz yaklaş.
        float walkBudget = maxWalkSeconds;
        if (Vector2.Distance(transform.position, target.position) > walkIfFartherThan)
            yield return WalkToward(target, walkBudget, stopWhenClose: true, b => walkBudget = b);

        // 2) Kullanılabilir ilk silahı seç.
        var weapon = PickWeapon();
        if (weapon == null) { EndTurn(); yield break; }

        // 3) Atış çözümü ara (karelere yayılır).
        Vector2 origin = FireOrigin(weapon);
        Shot best = default;
        yield return SearchShot(origin, target, weapon, r => best = r);

        // 3b) Durduğu yerden temiz bir çözüm yoksa (tipik olarak gezegen yolu kesiyor) yürüyüş
        //     bütçesinin kalanıyla bir kez daha konumlanıp yeniden ara. Tek bir 1.2 sn'lik yürüyüş
        //     bütçesi botu yarım gezegen öteye taşımaya yetmiyordu ve tur boşa gidiyordu.
        if ((!best.valid || best.score > acceptableMiss) && walkBudget > 0.1f)
        {
            yield return WalkToward(target, walkBudget, stopWhenClose: false, b => walkBudget = b);

            origin = FireOrigin(weapon);
            Shot again = default;
            yield return SearchShot(origin, target, weapon, r => again = r);
            if (again.valid && (!best.valid || again.score < best.score)) best = again;
        }

        if (!best.valid) { EndTurn(); yield break; }

        // 4) Zorluğa göre sapma: accuracy 1 iken sapma yok, 0 iken ±18 derece ve ±%25 güç.
        float miss  = 1f - Mathf.Clamp01(accuracy);
        float angle = best.angleDeg + Random.Range(-18f, 18f) * miss;
        float power = Mathf.Clamp01(best.power01 + Random.Range(-0.25f, 0.25f) * miss);

        // 5) Silahı normal akıştaki gibi seç + onayla (TurnManager'ın silah durumu bozulmasın),
        //    sonra pointer yerine programatik ateş.
        _abilities?.SelectSkill(weapon.SlotIndex);
        yield return null;
        _abilities?.ConfirmSkill(weapon.SlotIndex);
        yield return null;

        if (!weapon.BotFire(AngleToDir(angle), power))
        {
            EndTurn();   // cephane son anda tükendiyse turu kilitleme
            yield break;
        }

        _turnRoutine = null;
    }

    void EndTurn()
    {
        _body.botMoveInput = 0f;
        _turnRoutine       = null;
        TurnManager.Instance?.RequestEndTurn();
    }

    // ══════════════════════════════════════════════════════════════════════
    //  HEDEF / SİLAH / YÜRÜYÜŞ
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>Farklı takımdaki en yakın canlı karakter.</summary>
    Transform PickTarget()
    {
        var tm = TurnManager.Instance;
        if (tm == null || tm.characters == null) return null;

        Transform best    = null;
        float     bestSqr = float.MaxValue;

        foreach (var gb in tm.characters)
        {
            if (gb == null || gb == _body) continue;
            if (gb.teamId.Value == _body.teamId.Value) continue;

            float sqr = ((Vector2)gb.transform.position - (Vector2)transform.position).sqrMagnitude;
            if (sqr < bestSqr) { bestSqr = sqr; best = gb.transform; }
        }
        return best;
    }

    /// <summary>Tercih sırasındaki ilk cephanesi olan silah.</summary>
    AbilityBase PickWeapon()
    {
        var owned = GetComponents<AbilityBase>();
        foreach (int slot in WeaponSlots)
            foreach (var ab in owned)
                if (ab.SlotIndex == slot && ab.BotHasAmmo)
                    return ab;
        return null;
    }

    /// <summary>Yüzey boyunca hedefe doğru yürür; yön, GravityBody'nin kendi hareket ekseninden
    /// çıkarılır, böylece gezegenin hangi tarafında olduğu fark etmez.</summary>
    IEnumerator WalkToward(Transform target, float budgetSeconds, bool stopWhenClose,
                           System.Action<float> reportRemaining)
    {
        float started = Time.time;
        float until   = started + budgetSeconds;
        while (Time.time < until && _body.isActive.Value && !_body.movementLocked)
        {
            if (stopWhenClose &&
                Vector2.Distance(transform.position, target.position) <= walkIfFartherThan)
                break;

            Vector2 toTarget = (Vector2)target.position - (Vector2)transform.position;
            _body.botMoveInput = Mathf.Sign(Vector2.Dot(toTarget, _body.MoveAxis));
            yield return null;
        }
        _body.botMoveInput = 0f;
        reportRemaining(Mathf.Max(0f, until - Time.time));

        // Yürüyüş sonrası hız sıfırlansın, yoksa atış anında karakter hâlâ kayıyor olur.
        yield return new WaitForSeconds(0.25f);
    }

    // ══════════════════════════════════════════════════════════════════════
    //  ATIŞ ÇÖZÜMÜ
    // ══════════════════════════════════════════════════════════════════════

    struct Shot
    {
        public bool  valid;
        public float angleDeg;
        public float power01;
        public float score;   // küçük = iyi (hedefe en yakın yaklaşma mesafesi)
    }

    /// <summary>Kaba tarama (12 derece × 4 güç) + en iyinin çevresinde ince tarama.
    /// Sonuç <paramref name="report"/> ile döner; arama karelere yayıldığı için coroutine.</summary>
    IEnumerator SearchShot(Vector2 origin, Transform target, AbilityBase weapon,
                           System.Action<Shot> report)
    {
        var ignore = new HashSet<Collider2D>(GetComponentsInChildren<Collider2D>());
        var best   = new Shot { valid = false, score = float.MaxValue };
        int tried  = 0;

        for (int a = 0; a < CoarseAngles; a++)
        {
            float angle = a * (360f / CoarseAngles);
            foreach (float p in CoarsePowers)
            {
                Consider(origin, target, weapon, ignore, angle, p, ref best);
                if (++tried % YieldEvery == 0) yield return null;
            }
        }

        if (best.valid)
        {
            float baseAngle = best.angleDeg;
            float basePower = best.power01;
            for (int i = -4; i <= 4; i++)
            {
                float angle = baseAngle + i * 3f;
                for (int j = -2; j <= 2; j++)
                {
                    float p = Mathf.Clamp(basePower + j * 0.08f, 0.15f, 1f);
                    Consider(origin, target, weapon, ignore, angle, p, ref best);
                    if (++tried % YieldEvery == 0) yield return null;
                }
            }
        }

        report(best);
    }

    void Consider(Vector2 origin, Transform target, AbilityBase weapon,
                  HashSet<Collider2D> ignore, float angleDeg, float power01, ref Shot best)
    {
        float score = Simulate(origin, target, weapon, ignore, angleDeg, power01);
        if (score >= best.score) return;
        best = new Shot { valid = true, angleDeg = angleDeg, power01 = power01, score = score };
    }

    /// <summary>Mermiyi insan oyuncunun yörünge çizgisiyle aynı modelle uçurur ve hedefe en
    /// yakın yaklaşma mesafesini döner. Yol üstünde bir şeye çarparsa çarpma noktasının hedefe
    /// uzaklığı puandır — hedefin kendisine çarptıysa 0.</summary>
    float Simulate(Vector2 origin, Transform target, AbilityBase weapon,
                   HashSet<Collider2D> ignore, float angleDeg, float power01)
    {
        Vector2 vel      = AngleToDir(angleDeg) * (power01 * weapon.BotMuzzleSpeed);
        Vector2 pos      = origin;
        float   bestDist = float.MaxValue;

        for (int i = 0; i < SimMaxSteps; i++)
        {
            Vector2 acc = (GravityManager.Instance != null && GravityManager.Instance.Strategy != null)
                ? GravityManager.Instance.Strategy.CalculateAcceleration(pos)
                : Vector2.zero;

            vel += acc * SimStep;
            Vector2 next = pos + vel * SimStep;

            var hits = Physics2D.LinecastAll(pos, next);
            for (int h = 0; h < hits.Length; h++)
            {
                var col = hits[h].collider;
                if (col == null || ignore.Contains(col)) continue;
                // Trigger'lar yolu KESMEZ. Gezegenlerin cekim/ic alan trigger'lari tum oyun
                // alanini kapliyor ve bot her zaman onlarin ICINDE duruyor; queriesStartInColliders
                // acik oldugu icin ilk linecast adimi daima namlu ucunda bir "isabet" donduruyordu.
                // Sonuc: her aday ayni puani (namlu ucu - hedef mesafesi) aliyor, arama olu kaliyor.
                if (col.isTrigger) continue;
                bool isTarget = col.transform.root == target.root;
                return isTarget ? 0f : Vector2.Distance(hits[h].point, target.position);
            }

            pos      = next;
            bestDist = Mathf.Min(bestDist, Vector2.Distance(pos, target.position));
        }
        return bestDist;
    }

    /// <summary>Silahın namlu ucu — yoksa karakterin merkezi.</summary>
    static Vector2 FireOrigin(AbilityBase weapon) =>
        weapon.BotFirePoint != null ? (Vector2)weapon.BotFirePoint.position
                                    : (Vector2)weapon.transform.position;

    static Vector2 AngleToDir(float deg) =>
        new Vector2(Mathf.Cos(deg * Mathf.Deg2Rad), Mathf.Sin(deg * Mathf.Deg2Rad));
}
