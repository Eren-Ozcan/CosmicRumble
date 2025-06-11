using UnityEngine;

public class ToggleSkillPanel : MonoBehaviour
{
    [Header("UI Panel objesini buraya surukle")]
    public GameObject skillPanel;

    void Awake()
    {
        // Başlangıçta panel her zaman açık
        if (skillPanel != null)
            skillPanel.SetActive(true);
    }

    // Update metodu tamamen boş bırakıldı
    void Update() { }
}
