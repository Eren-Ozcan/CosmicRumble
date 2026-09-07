using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Menüdeki her butonun gerçekten TIKLANABİLİR olduğunu doğrulayan runtime denetçisi.
/// Her buton için dört şey kontrol edilir:
///   1. onClick'e en az bir dinleyici bağlı mı (ölü buton yok),
///   2. Selectable etkileşime açık ve raycast hedefi olan bir grafiği var mı,
///   3. Buton merkezine atılan bir EventSystem raycast'i gerçekten O butona ulaşıyor mu
///      (üstünü kaplayan görünmez bir panel/backdrop yoksa),
///   4. Dokunmatik için minimum hedef boyutunu (UiTheme.MinTouchSize) tutturuyor mu.
/// (3) hem fare hem parmak için AYNI yolu kullanır: EventSystem.RaycastAll — yani bu denetim
/// PC ve mobil için ortak geçerlidir. Ekran oranına bağlı sorunları yakalamak için denetim
/// farklı çözünürlüklerde (portre/yatay) ayrı ayrı çalıştırılır.
///
/// Butonlar GERÇEKTEN tıklanmaz: satın alma, oturum kapatma, eşleşme başlatma gibi geri
/// dönüşü olmayan akışları tetiklememek için yalnızca erişilebilirlik doğrulanır.
/// </summary>
public class UiInteractionAuditor : MonoBehaviour
{
    public class Finding
    {
        public string Screen;
        public string Path;
        public string Problem;
        public override string ToString() => $"{Screen} :: {Path} — {Problem}";
    }

    /// <summary>Denetim bittiğinde doldurulur (batchmode sürücüsü bunu okur).</summary>
    public static readonly List<Finding> Findings = new List<Finding>();

    // Aynı buton birden çok ekran turunda görünebilir; her sorun bir kez raporlanır.
    static readonly HashSet<string> s_seen = new HashSet<string>();

    static void Add(string screen, string path, string problem)
    {
        if (!s_seen.Add(path + "|" + problem)) return;
        Add(screen, path, problem);
    }
    public static int AuditedButtons;
    public static bool Finished;

    /// <summary>Menü ekranlarını sırayla açıp hepsini denetler.</summary>
    public static IEnumerator AuditMenuScreens()
    {
        Findings.Clear();
        s_seen.Clear();
        AuditedButtons = 0;
        Finished       = false;

        // Ana menü kendi kendini kurar; paneller MainMenuUI tarafından oluşturulur.
        yield return new WaitForSecondsRealtime(1.5f);

        // Editörde giriş kapısı açık geliyor: menü ve paneller ancak oturum açılınca kurulur.
        // Test amaçlı misafir butonu (yalnızca editör/dev build'de var) denetimin ön koşulu.
        yield return PassLoginGate();

        yield return AuditScreen("MainMenu", null, null);

        yield return AuditScreen("Shop",
            () => ShopPanelUI.Instance?.Show(),        () => ShopPanelUI.Instance?.Hide());
        yield return AuditScreen("Wardrobe",
            () => WardrobePanelUI.Instance?.Show(),    () => WardrobePanelUI.Instance?.Hide());
        yield return AuditScreen("Quests",
            () => QuestsPanelUI.Instance?.Show(),      () => QuestsPanelUI.Instance?.Hide());
        yield return AuditScreen("Achievements",
            () => AchievementsPanelUI.Instance?.Show(),() => AchievementsPanelUI.Instance?.Hide());
        yield return AuditScreen("Leaderboard",
            () => LeaderboardPanelUI.Instance?.Show(), () => LeaderboardPanelUI.Instance?.Hide());
        yield return AuditScreen("Social",
            () => SocialPanelUI.Instance?.Show(),      () => SocialPanelUI.Instance?.Hide());
        yield return AuditScreen("PartyLobby",
            () => PartyLobbyPanelUI.Instance?.ShowModeSelect(), () => PartyLobbyPanelUI.Instance?.Hide());
        yield return AuditScreen("OnlineLobby",
            () => OnlineLobbyPanelUI.Instance?.Show(), () => OnlineLobbyPanelUI.Instance?.Hide());
        yield return AuditScreen("BotLobby",
            () => LobbyPanelUI.Instance?.Show(),       () => LobbyPanelUI.Instance?.Hide());
        yield return AuditScreen("AvatarPicker",
            () => AvatarPickerUI.Instance?.Show(),     () => AvatarPickerUI.Instance?.Hide());

        yield return AuditGameScene();

        Finished = true;
    }

    /// <summary>
    /// Maç sahnesinin HUD'ını denetler: silah tepsisi, tur sayacı/SKIP, tepsi toggle'ı,
    /// duraklat butonu ve oyun içi menü. Sahne doğrudan yüklenir (maç kurulmaz) — HUD öğeleri
    /// sahnenin kendisinde olduğu için tıklanabilirlik böyle de doğrulanabilir.
    /// </summary>
    static IEnumerator AuditGameScene()
    {
        HideAllPanels();
        UnityEngine.SceneManagement.SceneManager.LoadScene(SceneNames.Game);
        yield return new WaitForSecondsRealtime(3f);

        AuditActiveButtons("GameHud");

        var menu = FindFirstObjectByType<InGameMenu>(FindObjectsInactive.Include);
        if (menu == null)
        {
            Add("PauseMenu", "(scene)", "InGameMenu sahnede yok — maç içinde menü açılamaz");
            yield break;
        }

        menu.ToggleMenu();
        yield return new WaitForSecondsRealtime(1f);
        AuditActiveButtons("PauseMenu");
        if (menu.IsOpen) menu.ToggleMenu();
    }

    /// <summary>Giriş ekranı açıksa test misafir butonuna basıp menünün kurulmasını bekler.</summary>
    static IEnumerator PassLoginGate()
    {
        var guest = FindActiveButton("btn_guest_test");
        if (guest == null) yield break;

        guest.onClick.Invoke();

        // Misafir girişi ağ üzerinden gider; menü panelinin kurulması birkaç saniye sürebilir.
        float deadline = Time.realtimeSinceStartup + 20f;
        while (Time.realtimeSinceStartup < deadline)
        {
            if (FindActiveButton("btn_guest_test") == null && ShopPanelUI.Instance != null)
                break;
            yield return new WaitForSecondsRealtime(0.5f);
        }
        yield return new WaitForSecondsRealtime(1f);
    }

    static Button FindActiveButton(string name)
    {
        foreach (var b in FindObjectsByType<Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            if (b.gameObject.activeInHierarchy && b.name == name) return b;
        return null;
    }

    /// <summary>Tek seferde tek panel açık kalsın: "engellenmiş buton" bulguları ancak
    /// böyle güvenilir olur (yoksa arkada unutulmuş bir panel yanlış alarm üretir).</summary>
    static void HideAllPanels()
    {
        ShopPanelUI.Instance?.Hide();
        WardrobePanelUI.Instance?.Hide();
        QuestsPanelUI.Instance?.Hide();
        AchievementsPanelUI.Instance?.Hide();
        LeaderboardPanelUI.Instance?.Hide();
        SocialPanelUI.Instance?.Hide();
        PartyLobbyPanelUI.Instance?.Hide();
        OnlineLobbyPanelUI.Instance?.Hide();
        LobbyPanelUI.Instance?.Hide();
        AvatarPickerUI.Instance?.Hide();
    }

    static IEnumerator AuditScreen(string screen, Action open, Action close)
    {
        HideAllPanels();
        yield return new WaitForSecondsRealtime(0.4f);

        if (open != null)
        {
            bool opened = true;
            try { open(); }
            catch (Exception e)
            {
                opened = false;
                Add(screen, "(open)", "Show() hata verdi: " + e.Message);
            }
            if (!opened) yield break;
            // Panel içerikleri bir kare sonra (ve bazıları async veriyle) dolur; ayrıca UiKit.Pop
            // açılış animasyonu 0.16 sn boyunca ölçeği 0.92'den 1'e taşır — animasyon bitmeden
            // ölçülen boyutlar yanlış "küçük hedef" bulgusu üretir.
            yield return new WaitForSecondsRealtime(1.2f);
        }

        AuditActiveButtons(screen);

        if (close != null)
        {
            try { close(); } catch { /* kapatma hatası denetimi bozmasın */ }
            yield return new WaitForSecondsRealtime(0.2f);
        }
    }

    /// <summary>O anda ekranda görünen bütün butonları denetler.</summary>
    public static void AuditActiveButtons(string screen)
    {
        var es = EventSystem.current;
        if (es == null)
        {
            Add(screen, "(scene)", "EventSystem yok — hiçbir buton tıklanamaz");
            return;
        }

        // Modal açıkken arkadaki menü butonlarının "engellenmiş" görünmesi normaldir; denetim
        // yalnızca EN ÖNDEKİ canvas katmanına bakar, arkadaki ekran kendi turunda denetlenir.
        int topOrder = TopCanvasOrder();

        foreach (var sel in FindObjectsByType<Selectable>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (sel == null || !sel.gameObject.activeInHierarchy) continue;
            if (sel is Scrollbar || sel is Slider) continue; // sürükleme kontrolleri ayrı denetlenir

            var rootCanvas = sel.GetComponentInParent<Canvas>();
            if (rootCanvas != null && rootCanvas.rootCanvas.sortingOrder < topOrder) continue;

            var rt = sel.transform as RectTransform;
            if (rt == null) continue;

            AuditedButtons++;
            string path = Path(sel.transform);

            if (!sel.IsInteractable())
            {
                // Devre dışı buton tasarımın parçası olabilir (ör. host olmayanda START) — bilgi amaçlı.
                continue;
            }

            var btn = sel as Button;
            if (btn != null && ListenerCount(btn.onClick) == 0)
                Add(screen, path, "onClick boş — buton hiçbir şey yapmıyor");

            if (sel.targetGraphic == null || !sel.targetGraphic.raycastTarget)
            {
                bool anyRaycastChild = false;
                foreach (var g in sel.GetComponentsInChildren<Graphic>(false))
                    if (g.raycastTarget) { anyRaycastChild = true; break; }
                if (!anyRaycastChild)
                    Add(screen, path, "raycastTarget yok — dokunuş/tıklama yakalanmıyor");
            }

            var canvas = sel.GetComponentInParent<Canvas>();
            if (canvas != null && canvas.rootCanvas.GetComponent<GraphicRaycaster>() == null)
                Add(screen, path, "Canvas'ta GraphicRaycaster yok");

            Vector2 screenPos = ScreenCenterOf(rt, canvas);
            // Kaydırma listesinde görünür alanın dışında kalan öğeler doğal olarak
            // tıklanamaz — kaydırınca erişilir, bu bir hata değil.
            if (!IsClippedByScrollView(rt) && !ReachableAt(es, sel.gameObject, screenPos, out string blocker))
                Add(screen, path, $"merkezine ({screenPos.x:0},{screenPos.y:0}) atılan raycast \"{blocker}\" öğesine çarpıyor — üstü kapalı ya da ekran dışı");

            Vector2 size = DesignSize(rt, canvas, out float refShortSide);
            // Eşik, canvas referansının KISA kenarına göre ölçeklenir (1080 = kit referansı) —
            // böylece denetim Game view çözünürlüğünden ve ekran oranından bağımsız aynı kalır.
            float minSide = UiTheme.MinTouchSize * (refShortSide / 1080f);
            if (size.x < minSide || size.y < minSide)
                Findings.Add(new Finding { Screen = screen, Path = path, Problem = $"dokunmatik hedef küçük ({size.x:0}x{size.y:0} tasarım birimi, en az {minSide:0} olmalı)" });
        }
    }

    // ── Yardımcılar ──────────────────────────────────────────────────────────

    /// <summary>
    /// UnityEvent'e AddListener ile eklenen (persistent olmayan) dinleyicileri sayar.
    /// Public API yalnızca persistent çağrıları sayabildiği için m_Calls listesine yansımayla
    /// bakılır — kod ile kurulan bu UI'da dinleyicilerin tamamı runtime call'dur.
    /// </summary>
    public static int ListenerCount(UnityEngine.Events.UnityEventBase evt)
    {
        int persistent = evt.GetPersistentEventCount();

        var callsField = typeof(UnityEngine.Events.UnityEventBase)
            .GetField("m_Calls", BindingFlags.Instance | BindingFlags.NonPublic);
        object calls = callsField?.GetValue(evt);
        var runtimeField = calls?.GetType().GetField("m_RuntimeCalls", BindingFlags.Instance | BindingFlags.NonPublic);
        var list = runtimeField?.GetValue(calls) as IList;

        return persistent + (list?.Count ?? 0);
    }

    /// <summary>Etkileşimli buton içeren aktif canvas'lar arasındaki en yüksek sortingOrder.</summary>
    static int TopCanvasOrder()
    {
        int top = int.MinValue;
        foreach (var sel in FindObjectsByType<Selectable>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (sel == null || !sel.gameObject.activeInHierarchy || !sel.IsInteractable()) continue;
            var c = sel.GetComponentInParent<Canvas>();
            if (c != null) top = Mathf.Max(top, c.rootCanvas.sortingOrder);
        }
        return top == int.MinValue ? 0 : top;
    }

    /// <summary>Öğe bir ScrollRect viewport'unun görünür alanı dışında mı (kaydırma gerekiyor)?</summary>
    static bool IsClippedByScrollView(RectTransform rt)
    {
        var scroll = rt.GetComponentInParent<ScrollRect>();
        if (scroll == null || scroll.viewport == null) return false;

        var corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        var view = new Vector3[4];
        scroll.viewport.GetWorldCorners(view);

        float minX = Mathf.Min(view[0].x, view[2].x), maxX = Mathf.Max(view[0].x, view[2].x);
        float minY = Mathf.Min(view[0].y, view[2].y), maxY = Mathf.Max(view[0].y, view[2].y);
        Vector3 c = (corners[0] + corners[2]) * 0.5f;
        return c.x < minX || c.x > maxX || c.y < minY || c.y > maxY;
    }

    static bool ReachableAt(EventSystem es, GameObject target, Vector2 screenPos, out string blocker)
    {
        blocker = "(hiçbir şey)";
        var data = new PointerEventData(es) { position = screenPos };
        var results = new List<RaycastResult>();
        es.RaycastAll(data, results);
        if (results.Count == 0) return false;
        blocker = Path(results[0].gameObject.transform);

        // İlk (en üstteki) sonuç butonun kendisi ya da bir alt öğesi olmalı.
        var hit = results[0].gameObject;
        for (var t = hit.transform; t != null; t = t.parent)
            if (t.gameObject == target) return true;
        return false;
    }

    static Vector2 ScreenCenterOf(RectTransform rt, Canvas canvas)
    {
        Vector3 world = rt.TransformPoint(rt.rect.center);
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay && canvas.worldCamera != null)
            return RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, world);
        return RectTransformUtility.WorldToScreenPoint(null, world);
    }

    /// <summary>Butonun canvas tasarım birimlerindeki boyutu (+ canvas'ın referans genişliği).</summary>
    static Vector2 DesignSize(RectTransform rt, Canvas canvas, out float refShortSide)
    {
        refShortSide = 1080f;
        var scaler = canvas != null ? canvas.rootCanvas.GetComponent<CanvasScaler>() : null;
        if (scaler != null && scaler.referenceResolution.x > 0f && scaler.referenceResolution.y > 0f)
            refShortSide = Mathf.Min(scaler.referenceResolution.x, scaler.referenceResolution.y);

        // rect.size doğrudan tasarım birimindedir. Transform ölçeği KASTEN dışarıda bırakılır:
        // UiKit.Pop/Press animasyonları ölçeği geçici olarak 0.92-0.96'ya çeker ve o an ölçüm
        // yapılırsa sahte "küçük hedef" bulgusu üretirdi.
        return rt.rect.size;
    }

    static string Path(Transform t)
    {
        var sb = new StringBuilder(t.name);
        for (var p = t.parent; p != null; p = p.parent)
            sb.Insert(0, p.name + "/");
        return sb.ToString();
    }

    /// <summary>Bulguları okunur bir rapora çevirir.</summary>
    public static string Report()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"[UI audit] {Screen.width}x{Screen.height} — {AuditedButtons} buton denetlendi, {Findings.Count} bulgu");
        foreach (var f in Findings) sb.AppendLine("  ! " + f);
        if (Findings.Count == 0) sb.AppendLine("  hepsi tıklanabilir");
        return sb.ToString();
    }
}
