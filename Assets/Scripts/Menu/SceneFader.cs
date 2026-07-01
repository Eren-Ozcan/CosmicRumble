using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Siyah fade overlay ile sahne geçişleri yapar.
/// DontDestroyOnLoad ile tüm sahnelerde aktiftir.
/// Kullanım: SceneFader.Instance.FadeToScene("SampleScene");
/// </summary>
public class SceneFader : MonoBehaviour
{
    public static SceneFader Instance { get; private set; }

    [Header("Fade Ayarları")]
    [SerializeField] float fadeDuration = 0.4f;

    // Runtime'da oluşturulan fullscreen siyah panel
    Canvas    _canvas;
    Image     _panel;
    CanvasGroup _cg;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildOverlay();
        _cg.alpha = 0f;
    }

    void BuildOverlay()
    {
        _canvas = gameObject.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 9999;

        var cr = gameObject.AddComponent<CanvasScaler>();
        cr.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

        var panel = new GameObject("FadePanel");
        panel.transform.SetParent(transform, false);

        _panel = panel.AddComponent<Image>();
        _panel.color = Color.black;
        _cg = panel.AddComponent<CanvasGroup>();
        _cg.blocksRaycasts = false;

        var rt = _panel.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin  = Vector2.zero;
        rt.offsetMax  = Vector2.zero;
    }

    // ── Public API ───────────────────────────────────────────────

    public void FadeToScene(string sceneName)       => StartCoroutine(DoFade(sceneName));
    public void FadeIn()                            => StartCoroutine(DoFadeAlpha(0f));
    public void FadeOut()                           => StartCoroutine(DoFadeAlpha(1f));

    // ── Coroutines ───────────────────────────────────────────────

    IEnumerator DoFade(string sceneName)
    {
        _cg.blocksRaycasts = true;
        yield return DoFadeAlpha(1f);
        SceneManager.LoadScene(sceneName);
        yield return null;          // bir frame bekle (sahne yüklensin)
        yield return DoFadeAlpha(0f);
        _cg.blocksRaycasts = false;
    }

    IEnumerator DoFadeAlpha(float target)
    {
        float start = _cg.alpha;
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            _cg.alpha = Mathf.Lerp(start, target, t / fadeDuration);
            yield return null;
        }
        _cg.alpha = target;
    }
}
