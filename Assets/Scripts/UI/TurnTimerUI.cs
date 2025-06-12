using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
