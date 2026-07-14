using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CosmicRumble.Localization;

public class TurnTimerUI : MonoBehaviour
{
    public static TurnTimerUI Instance;

    [Header("Radial Bar")]
    public Image radialImage;

    [Header("Sayısal Metin (Opsiyonel)")]
    public TextMeshProUGUI timerText;

    [Header("Renkler")]
    public Color startColor = Color.green;
    public Color dangerColor = Color.red;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        BuildSkipButton();
    }

    // Dokunmatik/fare için tur pas butonu — Tab kısayolunun mobilde karşılığı yoktu ve online
    // client'ın turunu erken bitirecek HİÇBİR yolu yoktu (Tab yalnız server'da işleniyordu).
    // TurnManager.RequestEndTurn üç modda da (offline/host/client) doğru yoldan geçer; sırası
    // olmayanın isteğini server zaten reddeder, buton her zaman görünür kalabilir.
    private void BuildSkipButton()
    {
        if (radialImage == null) return;

        var go = new GameObject("SkipTurnButton");
        go.transform.SetParent(radialImage.transform, false);

        var img = go.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.55f);
        UiKit.Round(img);

        var rt = img.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -10f);
        rt.sizeDelta = new Vector2(120f, 38f);

        var btn = go.AddComponent<Button>();
        btn.onClick.AddListener(() =>
        {
            AudioManager.Instance?.PlayClick();
            TurnManager.Instance?.RequestEndTurn();
        });
        UiKit.Press(go);
        UiKit.Hover(go);

        var txtGO = new GameObject("Label");
        txtGO.transform.SetParent(go.transform, false);
        var txt = txtGO.AddComponent<TextMeshProUGUI>();
        txt.text = Loc.T("SKIP");
        txt.fontSize = 20f;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color = Color.white;
        var trt = txt.rectTransform;
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;
    }

    public void UpdateTimerDisplay(float timeLeft, float maxTime)
    {
        // Doluluk oranı
        float ratio = Mathf.Clamp01(timeLeft / maxTime);
        radialImage.fillAmount = ratio;

        // Renk geçişi
        radialImage.color = (timeLeft <= 5f) ? dangerColor : startColor;

        // Sayısal gösterim varsa
        if (timerText != null)
        {
            timerText.text = Mathf.CeilToInt(timeLeft).ToString();
            timerText.color = (timeLeft <= 5f) ? dangerColor : Color.white;
        }
    }
}
