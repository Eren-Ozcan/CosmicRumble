using UnityEngine;

/// <summary>
/// Silah/skill tray'ini açar-kapar. Masaüstünde ekstra bir şey değiştirmez (tray varsayılan
/// açık kalabilir); mobilde ekran alanı kazanmak için tray'i kapatıp tek bir toggle butonuyla
/// tekrar açmayı sağlar. Tray kapanırken elde tutulan silah/skill seçimi de iptal edilir
/// (TurnManager onayı olmadan aksiyon alınamaz kuralına uygun: kapatmak = silahı elden bırakmak).
/// </summary>
public class ToggleSkillPanel : MonoBehaviour
{
    [Header("Tray (SkillPanel) objesini buraya sürükle")]
    public GameObject skillPanel;

    [Header("Başlangıç durumu")]
    public bool startOpen = true;

    public bool IsOpen { get; private set; }

    void Awake()
    {
        IsOpen = startOpen;
        ApplyState();
    }

    /// <summary>Toggle butonunun onClick'ine bağlanır.</summary>
    public void Toggle()
    {
        IsOpen = !IsOpen;
        if (!IsOpen)
            UIManager.Instance?.CancelSelection();
        ApplyState();
    }

    private void ApplyState()
    {
        if (skillPanel != null)
            skillPanel.SetActive(IsOpen);
    }
}
