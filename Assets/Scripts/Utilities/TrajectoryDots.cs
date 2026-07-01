using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Drag sırasında nokta tabanlı yörünge önizlemesi + merkezi ayar kaynağı.
/// - Havuzlanmış SpriteRenderer noktaları (reuse)
/// - İlk nokta küçük → son nokta büyük (endScale → startScale)
/// - Güce göre renk: yeşil → sarı → kırmızı (0..1 power)
/// - Ateş/iptalde Hide()
/// - GLOBAL AYARLAR: Diğer scriptler buradan çeksin:
///     TrajectoryDots.GlobalDotCount
///     TrajectoryDots.GlobalTimeStep
///     TrajectoryDots.GlobalStartScale
///     TrajectoryDots.GlobalEndScale
///
/// KULLANIM:
///   - (Önerilen) Silahlar değerleri buradan okusun; Setup çağırıyorsan
///     ignoreExternalSetup = true iken sadece firePoint güncellenir.
///   - Drag:    Show(initialVelocity, power01)
///   - Bırak:   Hide()
/// </summary>
[DisallowMultipleComponent]
public class TrajectoryDots : MonoBehaviour
{
    [Header("Pool / Visual")]
    [Min(2)] public int dotCount = 60;
    [Min(0.0001f)] public float timeStep = 0.02f;  // Unity fixedDeltaTime ile eşleşmeli
    [Min(0f)] public float startScale = 1f;
    [Min(0f)] public float endScale = 0.3f;
    public Transform firePoint;

    [Tooltip("Küçük beyaz daire sprite; atanmazsa Knob.psd fallback denenir.")]
    public Sprite dotSprite;
    public string sortingLayer = "Default";
    public int sortingOrder = 200;

    [Header("Gravity")]
    [Tooltip("Kaynağın gravityRadius’u dışındakileri yoksay.")]
    public bool respectGravityRadius = true;

    [Header("Authority / Global Settings")]
    [Tooltip("Bu bileşenin Inspector değerlerini global ayar kaynağı olarak ilan et.")]
    public bool useAsGlobalSettings = true;

    [Tooltip("TRUE ise dışarıdan Setup(count, step, fp) geldiğinde count/step YOK SAYILIR; sadece firePoint güncellenir.")]
    public bool ignoreExternalSetup = true;

    // --- Global erişim (diğer scriptler buradan okusun) ---
    private static TrajectoryDots _global;
    public static int GlobalDotCount => _global ? _global.dotCount : 60;
    public static float GlobalTimeStep => _global ? _global.timeStep : 0.05f;
    public static float GlobalStartScale => _global ? _global.startScale : 1f;
    public static float GlobalEndScale => _global ? _global.endScale : 0.3f;

    // --- Runtime ---
    private readonly List<Transform> _dots = new();
    private readonly List<SpriteRenderer> _srs = new();

    private void Awake()
    {
        // Global ayar kaynağı olarak kendini atar (ilk olan kazanır)
        if (useAsGlobalSettings && _global == null)
            _global = this;

        // Fallback sprite
        if (dotSprite == null)
        {
            // fallback: beyaz texture’dan sprite üret
            var tex = Texture2D.whiteTexture;
            dotSprite = Sprite.Create(tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f));
        }


        CreatePool();
        Hide();
    }

    /// <summary>
    /// Dışarıdan parametre verip havuzu tazelemek için. 
    /// ignoreExternalSetup = TRUE ise, count/step YOK SAYILIR; sadece firePoint set edilir.
    /// </summary>
    public void Setup(int count, float step, Transform fp)
    {
        firePoint = fp;

        if (!ignoreExternalSetup)
        {
            dotCount = Mathf.Max(2, count);
            timeStep = Mathf.Max(0.0001f, step);
            CreatePool();
        }

        Hide();
    }

    /// <summary> Havuzu yeniden kurar (sprite/sorting/dotCount değişmiş olabilir). </summary>
    private void CreatePool()
    {
        // Eski dotları temizle
        for (int i = 0; i < _dots.Count; i++)
            if (_dots[i] != null) Destroy(_dots[i].gameObject);
        _dots.Clear();
        _srs.Clear();

        if (dotSprite == null)
        {
            #if UNITY_EDITOR
            Debug.LogError("[TrajectoryDots] dotSprite is null. Assign a sprite in Inspector.");
            #endif
            return;
        }

        for (int i = 0; i < dotCount; i++)
        {
            var go = new GameObject($"TrajectoryDot_{i:00}");
            go.transform.SetParent(transform, false);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = dotSprite;
            sr.sortingLayerName = sortingLayer;
            sr.sortingOrder = sortingOrder;
            sr.color = Color.white; // renk Show() sırasında veriliyor

            go.SetActive(false);

            _dots.Add(go.transform);
            _srs.Add(sr);
        }
    }

    /// <summary> Drag sırasında çağır: fizik entegrasyonu ile noktaları hesaplayıp gösterir. </summary>
    /// <param name="initialVelocity">Silahın fırlatacağı ilk hız vektörü</param>
    /// <param name="power">0..1 normalize güç (renk için)</param>
    public void Show(Vector2 initialVelocity, float power)
    {
        if (firePoint == null || _dots.Count == 0 || _srs.Count == 0) return;

        Color col = EvaluateColor(power);
        Vector2 pos = firePoint.position;
        Vector2 vel = initialVelocity;

        for (int i = 0; i < _dots.Count; i++)
        {
            // Strateji üzerinden ivme hesapla — GravityManager.Instance.Strategy.CalculateAcceleration
            // Single/MultiPlanetGravity, GravitySource.OnTriggerStay2D ile aynı sabit kuvvet formülünü kullanır.
            Vector2 acc = (GravityManager.Instance != null && GravityManager.Instance.Strategy != null)
                ? GravityManager.Instance.Strategy.CalculateAcceleration(pos)
                : Vector2.zero;

            vel += acc * timeStep;
            pos += vel * timeStep;

            // Dot transform/görsel
            var t = _dots[i];
            t.position = new Vector3(pos.x, pos.y, 0f);

            float u = (_dots.Count <= 1) ? 0f : (float)i / (_dots.Count - 1); // 0..1
            float s = Mathf.Lerp(endScale, startScale, u);                    // küçük → büyük
            t.localScale = new Vector3(s, s, 1f);

            _srs[i].color = col;

            if (!t.gameObject.activeSelf) t.gameObject.SetActive(true);
        }
    }

    /// <summary> Tüm noktaları gizle (ateş/iptal). </summary>
    public void Hide()
    {
        for (int i = 0; i < _dots.Count; i++)
        {
            var t = _dots[i];
            if (t != null && t.gameObject.activeSelf)
                t.gameObject.SetActive(false);
        }
    }

    /// <summary> Güce göre renk: yeşil → sarı → kırmızı. </summary>
    private static Color EvaluateColor(float power)
    {
        power = Mathf.Clamp01(power);
        if (power <= 0.5f)
            return Color.Lerp(Color.green, Color.yellow, power / 0.5f);       // 0..0.5
        return Color.Lerp(Color.yellow, Color.red, (power - 0.5f) / 0.5f);    // 0.5..1
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        dotCount = Mathf.Max(2, dotCount);
        timeStep = Mathf.Max(0.0001f, timeStep);
        startScale = Mathf.Max(0f, startScale);
        endScale = Mathf.Max(0f, endScale);

        // Bu bileşen global ayar kaynağıysa, editörde ilk olana atansın
        if (useAsGlobalSettings && (_global == null || _global == this))
            _global = this;
    }
#endif
}
