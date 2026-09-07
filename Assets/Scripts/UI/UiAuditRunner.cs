using System.Collections;
using System.IO;
using UnityEngine;

/// <summary>
/// <see cref="UiInteractionAuditor"/> denetimini play mode'da sürer, raporu proje kökündeki
/// ui-audit-report.txt dosyasına yazar ve play mode'dan çıkar. Editör tarafındaki sürücü
/// (Tools > UI > Run Button Audit) bu bileşeni kurar; oyunun normal akışında hiç oluşturulmaz.
///
/// Denetim birkaç telefon en-boy oranında tekrarlanır: 16:9'a sığan bir öğe 20:9 ekranın
/// dışına taşabilir ve bunu ancak o çözünürlükteki gerçek raycast yakalar.
/// </summary>
public class UiAuditRunner : MonoBehaviour
{
    public const string ReportFileName = "ui-audit-report.txt";
    public const string CountPrefix    = "FINDINGS:";

    /// <summary>Denetlenecek ekran boyutları — yaygın telefon oranları (yatay oyun).</summary>
    static readonly Vector2Int[] Resolutions =
    {
        new Vector2Int(1920, 1080), // 16:9
        new Vector2Int(2400, 1080), // 20:9 — uzun modern telefon
        new Vector2Int(1600,  720), // 20:9 düşük yoğunluk
        new Vector2Int(1440, 1080), // 4:3 — tablet
    };

    IEnumerator Start()
    {
        for (int i = 0; i < Resolutions.Length; i++)
        {
            var res = Resolutions[i];
            SetRenderingResolution(res.x, res.y);
            yield return new WaitForSecondsRealtime(1f);

            UiInteractionAuditor.ResolutionLabel = $"[{res.x}x{res.y}]";
            bool last = i == Resolutions.Length - 1;
            // Maç sahnesi menü sahnesinden çıkar, o yüzden yalnızca son turda denetlenir.
            yield return UiInteractionAuditor.AuditMenuScreens(reset: i == 0, includeGameScene: last);
        }

        UiInteractionAuditor.ResolutionLabel = "";

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

    /// <summary>
    /// Game view'ın render çözünürlüğünü değiştirir. Editör dışında (gerçek cihazda) bu
    /// denetim zaten çalıştırılmaz; o yüzden çağrı editöre özeldir ve sessizce atlanır.
    /// </summary>
    static void SetRenderingResolution(int width, int height)
    {
#if UNITY_EDITOR
        UnityEditor.PlayModeWindow.SetCustomRenderingResolution(
            (uint)width, (uint)height, $"{width}x{height}");
#endif
    }
}
