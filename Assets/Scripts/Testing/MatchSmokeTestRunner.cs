using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using CosmicRumble.Localization;

/// <summary>
/// Canlı maç dumanı testi: menüden bot maçı başlatır, bir turu gerçekten oynar ve maç
/// HUD'ının tasarım kitinden gelen üç parçasını (tur bandı, "POWER % · °" nişan okuması,
/// duraklat butonu) çalışırken doğrular. Buton denetimi (<see cref="UiInteractionAuditor"/>)
/// yalnızca erişilebilirliğe bakar; bu koşu davranışa bakar.
///
/// Editör sürücüsü: Tools > UI > Run Match Smoke Test (Play Mode). Oyunun normal akışında
/// bu bileşen hiç oluşturulmaz.
/// </summary>
public class MatchSmokeTestRunner : MonoBehaviour
{
    public const string ReportFileName = "match-smoke-report.txt";
    public const string CountPrefix    = "FAILURES:";

    static readonly List<string> s_lines = new List<string>();
    static int  s_failures;
    /// <summary>Editor surucusu domain reload sirasinda ikinci bir surucu kurabiliyor;
    /// iki koroutine ayni statik rapora yazinca satirlar birbirine giriyor.</summary>
    static bool s_running;

    static void Pass(string what)           { s_lines.Add("  ok   " + what); }
    static void Fail(string what)           { s_lines.Add("  FAIL " + what); s_failures++; }
    static void Check(bool ok, string what) { if (ok) Pass(what); else Fail(what); }
    static void Step(string title)          { s_lines.Add(""); s_lines.Add("-- " + title); }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { s_running = false; }

    IEnumerator Start()
    {
        if (s_running) { Destroy(gameObject); yield break; }
        s_running = true;

        s_lines.Clear();
        s_failures = 0;

        yield return Run();

        var sb = new StringBuilder();
        sb.AppendLine("CosmicRumble - canli mac dumani testi");
        foreach (var l in s_lines) sb.AppendLine(l);
        sb.AppendLine();
        sb.AppendLine(CountPrefix + " " + s_failures);

        string report = sb.ToString();
        try { File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), ReportFileName), report); }
        catch (IOException e) { Debug.LogError("[match smoke] rapor yazilamadi: " + e.Message); }
        Debug.Log(report);

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    IEnumerator Run()
    {
        Step("Menu -> bot maci");
        yield return new WaitForSecondsRealtime(1.5f);
        yield return UiInteractionAuditor.PassLoginGate();

        if (LobbyPanelUI.Instance == null) { Fail("LobbyPanelUI kurulmadi - menuye girilemedi"); yield break; }
        LobbyPanelUI.Instance.Show();
        yield return new WaitForSecondsRealtime(0.5f);

        var plus = FindButton("btn_botPlus");
        if (plus == null) { Fail("btn_botPlus bulunamadi"); yield break; }
        plus.onClick.Invoke();                       // 1 bot
        yield return new WaitForSecondsRealtime(0.2f);
        Pass("bot sayisi 1'e cikarildi (btn_botPlus tiklandi)");

        var start = FindButton("btn_start");
        if (start == null) { Fail("btn_start bulunamadi"); yield break; }
        start.onClick.Invoke();
        Pass("btn_start tiklandi - mac sahnesi yukleniyor");

        // -- Mac kurulumu ---------------------------------------------------
        Step("Mac kurulumu");
        GravityBody shooter = null;
        float deadline = Time.realtimeSinceStartup + 30f;
        while (Time.realtimeSinceStartup < deadline)
        {
            var tm = TurnManager.Instance;
            if (tm != null && tm.CurrentCharacter != null) { shooter = tm.CurrentCharacter; break; }
            yield return new WaitForSecondsRealtime(0.25f);
        }
        if (shooter == null) { Fail("30 sn icinde TurnManager sirayi baslatmadi"); yield break; }
        Pass("mac basladi, siradaki karakter: " + shooter.gameObject.name);
        Check(TurnManager.Instance.characters != null && TurnManager.Instance.characters.Count >= 2,
              "sira listesinde en az 2 karakter var (oyuncu + bot): " +
              (TurnManager.Instance.characters == null ? 0 : TurnManager.Instance.characters.Count));

        if (MatchHudUI.Instance == null) { Fail("MatchHudUI sahneye kurulmadi"); yield break; }
        yield return new WaitForSecondsRealtime(1f);

        // -- 1. Tur bandi ---------------------------------------------------
        Step("HUD: tur bandi");
        var turnLabel = FindText("Label");
        var turnName  = FindText("Name");
        if (turnLabel == null || turnName == null) { Fail("TurnBanner metinleri bulunamadi"); yield break; }
        Check(!string.IsNullOrEmpty(turnLabel.text) && turnLabel.text != "WAITING",
              "tur etiketi doldu: \"" + turnLabel.text + "\"");
        Check(!string.IsNullOrEmpty(turnName.text),
              "tur adi doldu: \"" + turnName.text + "\"");
        // Cevrimdisi hot-seat'te bot slotlari da ayni klavyeyle oynanir ama band "YOUR TURN"
        // yerine "TURN" demeli - yoksa sira bottayken ekran "YOUR TURN / Bot_1" gosterir.
        var owner = TurnManager.Instance.CurrentCharacter;
        if (owner != null)
        {
            string expected = Loc.T(owner.isBot ? "TURN" : "YOUR TURN");
            Check(turnLabel.text == expected,
                  "band etiketi tur sahibiyle uyumlu (bot=" + owner.isBot + ", beklenen \"" +
                  expected + "\", gelen \"" + turnLabel.text + "\")");
        }
        int turnIndexBefore = TurnManager.Instance.CurrentTurnIndex;

        // -- 2. Duraklat butonu ---------------------------------------------
        Step("HUD: duraklat butonu (gercek tik)");
        var pause = FindButton("btn_pause");
        var menu  = FindFirstObjectByType<InGameMenu>(FindObjectsInactive.Include);
        if (pause == null)     Fail("btn_pause bulunamadi");
        else if (menu == null) Fail("InGameMenu sahnede yok");
        else
        {
            pause.onClick.Invoke();
            yield return new WaitForSecondsRealtime(0.6f);
            Check(menu.IsOpen, "duraklat tiklaninca oyun ici menu acildi");
            if (menu.IsOpen)
            {
                pause.onClick.Invoke();
                yield return new WaitForSecondsRealtime(0.6f);
                Check(!menu.IsOpen, "ikinci tikta menu kapandi");
            }
        }

        // -- 3. Karakter etiketleri (isim ustte, can bari altta) --------------
        // Artboard 16: isim ayri bir satir, can bari onun altinda. Ikisi de dunya uzayi
        // canvas'i; karakter yuzeye gore dondugu icin her ikisi de her karede dik
        // tutulmali, yoksa donen bar isim yazisinin uzerinden supuruyor.
        Step("Karakter etiketleri: isim / can bari");
        var nameCanvas = shooter.transform.Find("NameTagCanvas");
        var barCanvas  = shooter.transform.Find("HealthBarCanvas");
        if (nameCanvas == null || barCanvas == null)
        {
            Fail("NameTagCanvas veya HealthBarCanvas karakterde yok");
        }
        else
        {
            Check(Quaternion.Angle(nameCanvas.rotation, Quaternion.identity) < 0.5f,
                  "isim etiketi dik duruyor");
            Check(Quaternion.Angle(barCanvas.rotation, Quaternion.identity) < 0.5f,
                  "can bari dik duruyor");

            var nameLabel = nameCanvas.GetComponentInChildren<TextMeshPro>();
            if (nameLabel == null)
            {
                Fail("isim etiketinde TextMeshPro yok");
            }
            else
            {
                // Yazinin gercek cizim kutusu; punto kutudan tasarsa burada gorunur.
                float nameBottom = nameLabel.GetComponent<Renderer>().bounds.min.y;
                float barTop     = WorldTop(barCanvas as RectTransform);
                Check(nameBottom > barTop,
                      "isim yazisi can barinin ustunde kaliyor (isim alti " +
                      nameBottom.ToString("F2") + " > bar ustu " + barTop.ToString("F2") + ")");
            }
        }

        // -- 3. Nisan okumasi + gercek atis ---------------------------------
        Step("HUD: POWER % - derece okumasi ve atis");
        var abilities = shooter.GetComponent<CharacterAbilities>();
        if (abilities == null) { Fail("aktif karakterde CharacterAbilities yok"); yield break; }
        abilities.SelectSkill(0);                 // Pistol
        yield return null;
        abilities.ConfirmSkill(0);
        yield return null;
        Pass("Pistol secildi ve onaylandi");

        if (Mouse.current == null) { Fail("Mouse cihazi yok - pointer suruklemesi taklit edilemiyor"); yield break; }

        var cam = Camera.main;
        Vector3 origin = shooter.transform.position;
        // Slingshot: pull = dragStart - pointer, yani atis yonu suruklemenin tersi.
        // Gezegenden disari (karakterin "yukari"si) dogru guclu bir cekis.
        Vector2 downPos = cam.WorldToScreenPoint(origin + shooter.transform.up * 2.5f);
        Vector2 dragPos = cam.WorldToScreenPoint(origin + shooter.transform.up * 0.5f);

        yield return Pointer(downPos, true);                               // basis

        // Surukleme birakilana kadar surer: okumanin gorunmesi bir kare gecikebildigi icin
        // tek bir bakis yerine kisa bir pencere boyunca beklenir (yoksa test titrer).
        GameObject aimGO = null;
        float aimDeadline = Time.realtimeSinceStartup + 2f;
        while (Time.realtimeSinceStartup < aimDeadline)
        {
            yield return Pointer(dragPos, true);
            aimGO = FindGameObject("AimReadout");
            if (aimGO != null && aimGO.activeInHierarchy) break;
        }

        var aimText = FindText("Text");
        Check(aimGO != null && aimGO.activeInHierarchy,
              "nisan okumasi suruklerken gorunur oldu " + ActiveChain(aimGO));
        if (aimText != null)
            Check(Regex.IsMatch(aimText.text, @"\d+%.*-?\d+"),
                  "okuma bicimi dogru: \"" + aimText.text + "\"");
        else
            Fail("AimReadout metni bulunamadi");

        yield return Pointer(dragPos, false);      // birakis = ates

        // Mermi cok kisa yasayabilir (carpma/menzil) — tek bir gecikmeli bakis kacirir.
        bool sawProjectile = false;
        float watchUntil = Time.realtimeSinceStartup + 3f;
        while (Time.realtimeSinceStartup < watchUntil && !sawProjectile)
        {
            sawProjectile = FindFirstObjectByType<ProjectileBase>() != null
                         || TurnManager.Instance.ProjectileInFlight;
            yield return null;
        }
        Check(aimGO == null || !aimGO.activeInHierarchy, "ates sonrasi nisan okumasi gizlendi");
        Check(sawProjectile, "mermi sahneye cikti (yetenek kullanildi=" +
              abilities.HasUsedSkillThisTurn + ")");

        // -- 4. Tur devri ---------------------------------------------------
        Step("Tur devri");
        deadline = Time.realtimeSinceStartup + 40f;
        bool turned = false;
        while (Time.realtimeSinceStartup < deadline)
        {
            if (TurnManager.Instance.CurrentTurnIndex != turnIndexBefore) { turned = true; break; }
            yield return new WaitForSecondsRealtime(0.25f);
        }
        Check(turned, "atistan sonra sira bir sonraki karaktere gecti");
        if (turned)
        {
            yield return new WaitForSecondsRealtime(0.5f);
            Check(!string.IsNullOrEmpty(turnName.text),
                  "yeni tur icin band guncellendi: \"" + turnLabel.text + " / " + turnName.text + "\"");
        }
    }

    // -- Yardimcilar --------------------------------------------------------

    /// <summary>Tek karelik pointer durumu kuyruga atar ve islenmesi icin bir kare bekler.</summary>
    /// <summary>Dunya uzayi bir RectTransform'un en ust dunya Y'si.</summary>
    static float WorldTop(RectTransform rt)
    {
        if (rt == null) return float.NegativeInfinity;
        var corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        float top = corners[0].y;
        for (int i = 1; i < 4; i++) top = Mathf.Max(top, corners[i].y);
        return top;
    }

    static IEnumerator Pointer(Vector2 screenPos, bool pressed)
    {
        var state = new MouseState { position = screenPos };
        InputSystem.QueueStateEvent(Mouse.current, state.WithButton(MouseButton.Left, pressed));
        yield return null;
    }

    static Button FindButton(string name)
    {
        foreach (var b in FindObjectsByType<Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            if (b.gameObject.activeInHierarchy && b.name == name) return b;
        return null;
    }

    static TextMeshProUGUI FindText(string name)
    {
        var hud = MatchHudUI.Instance;
        if (hud == null) return null;
        foreach (var t in hud.GetComponentsInChildren<TextMeshProUGUI>(true))
            if (t.name == name) return t;
        return null;
    }

    /// <summary>Bir nesnenin ve atalarinin aktiflik zinciri - "gorunur degil" bulgularinda
    /// hangi halkanin kapali oldugunu gosterir.</summary>
    static string ActiveChain(GameObject go)
    {
        if (go == null) return "(nesne yok)";
        var sb = new StringBuilder("[");
        for (var t = go.transform; t != null; t = t.parent)
            sb.Append(t.name).Append(t.gameObject.activeSelf ? ":on " : ":OFF ");
        return sb.Append("]").ToString();
    }

    static GameObject FindGameObject(string name)
    {
        var hud = MatchHudUI.Instance;
        if (hud == null) return null;
        foreach (var t in hud.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t.gameObject;
        return null;
    }
}
