using UnityEngine;

public class ToggleSkillPanel : MonoBehaviour
{
    [Header("UI Panel objesini buraya surukle")]
    public GameObject skillPanel;

    void Start()
    {
        if (skillPanel != null)
            skillPanel.SetActive(false);  // Baslangicta panel kapali olsun
    }

    void Update()
    {
        // CTRL tusuna basili tutuldugunda paneli aktif et
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
        {
            if (skillPanel != null && !skillPanel.activeSelf)
                skillPanel.SetActive(true);
        }
        else
        {
            // CTRL birakildiginda paneli gizle
            if (skillPanel != null && skillPanel.activeSelf)
                skillPanel.SetActive(false);
        }
    }
}
