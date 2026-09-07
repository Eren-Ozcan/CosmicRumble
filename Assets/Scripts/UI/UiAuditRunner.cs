using System.Collections;
using System.IO;
using UnityEngine;

/// <summary>
/// <see cref="UiInteractionAuditor"/> denetimini play mode'da sürüp raporu proje kökündeki
/// ui-audit-report.txt dosyasına yazar, sonra play mode'dan çıkar. Editör tarafındaki sürücü
/// (Tools > UI > Run Button Audit) bu bileşeni kurar; oyunun normal akışında hiç oluşturulmaz.
/// </summary>
public class UiAuditRunner : MonoBehaviour
{
    public const string ReportFileName = "ui-audit-report.txt";
    public const string CountPrefix    = "FINDINGS:";

    IEnumerator Start()
    {
        yield return UiInteractionAuditor.AuditMenuScreens();

        string report = UiInteractionAuditor.Report()
                      + CountPrefix + " " + UiInteractionAuditor.Findings.Count + "\n";
        try
        {
            File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), ReportFileName), report);
        }
        catch (IOException e)
        {
            Debug.LogError("[UI audit] rapor yazılamadı: " + e.Message);
        }

        Debug.Log(report);

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
