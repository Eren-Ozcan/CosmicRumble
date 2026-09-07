// Assets/Editor/UiInteractionSelfTest.cs
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CosmicRumble.EditorTools
{
    /// <summary>
    /// Menü UI'ının tıklanabilirlik denetimini play mode'da çalıştırır ve raporu dosyaya yazar.
    /// (Denetimin kendisi runtime tarafında: <see cref="UiInteractionAuditor"/>.)
    ///
    /// Menüden: Tools > UI > Run Button Audit (Play Mode)
    /// Headless (portre ve yatay ayrı ayrı çalıştırılmalı — bazı hatalar yalnız bir oranda çıkar):
    ///   Unity.exe -batchmode -projectPath &lt;proje&gt; -screen-width 1080 -screen-height 1920 \
    ///     -executeMethod CosmicRumble.EditorTools.UiInteractionSelfTest.RunBatch
    /// Rapor: &lt;proje&gt;/ui-audit-report.txt. Bulgu varsa editör 1 ile çıkar.
    ///
    /// -quit VERİLMEZ: script play mode'a girip çıkmayı beklemek zorunda, çıkışı kendi yapar.
    /// </summary>
    public static class UiInteractionSelfTest
    {
        const string k_StageKey  = "cosmicrumble.uiaudit.stage";
        const string k_BatchKey  = "cosmicrumble.uiaudit.batch";
        const string k_MenuScenePath = "Assets/Scenes/MenuScene.unity";
        static string k_ReportRel => UiAuditRunner.ReportFileName;

        [MenuItem("Tools/UI/Run Button Audit (Play Mode)")]
        public static void RunFromMenu() => Start(false);

        public static void RunBatch() => Start(true);

        static void Start(bool batch)
        {
            SessionState.SetString(k_StageKey, "entering");
            SessionState.SetBool(k_BatchKey, batch);

            // Zaten oynuyorsak (ör. editör aracı kodu play mode'da çalıştırıyorsa) sahne
            // açılamaz — doğrudan sürücüyü kur.
            if (EditorApplication.isPlaying)
            {
                SpawnRunner();
                return;
            }

            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != k_MenuScenePath)
                EditorSceneManager.OpenScene(k_MenuScenePath, OpenSceneMode.Single);
            EditorApplication.EnterPlaymode();
        }

        [InitializeOnLoadMethod]
        static void Hook()
        {
            if (SessionState.GetString(k_StageKey, "") == "")
                return;

            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;

            // Domain reload play mode'a girerken de olur: zaten oynuyorsak sürücüyü hemen kur.
            if (EditorApplication.isPlaying && SessionState.GetString(k_StageKey, "") == "entering")
                SpawnRunner();
        }

        static void OnPlayModeChanged(PlayModeStateChange change)
        {
            string stage = SessionState.GetString(k_StageKey, "");
            if (stage == "") return;

            if (change == PlayModeStateChange.EnteredPlayMode && stage == "entering")
                SpawnRunner();

            if (change == PlayModeStateChange.EnteredEditMode && stage == "running")
                Finish();
        }

        static void SpawnRunner()
        {
            SessionState.SetString(k_StageKey, "running");
            var go = new GameObject("UiAuditRunner");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<UiAuditRunner>();
        }

        static void Finish()
        {
            SessionState.SetString(k_StageKey, "");

            string path   = Path.Combine(Directory.GetCurrentDirectory(), k_ReportRel);
            string report = File.Exists(path) ? File.ReadAllText(path) : "[UI audit] rapor dosyası yazılmadı";
            int findings  = ParseFindingCount(report);

            if (findings == 0) Debug.Log(report);
            else               Debug.LogError(report);

            if (SessionState.GetBool(k_BatchKey, false))
                EditorApplication.Exit(findings == 0 ? 0 : 1);
        }

        /// <summary>Raporun son satırındaki "FINDINGS: n" sayacını okur (-1 = rapor yok/bozuk).</summary>
        static int ParseFindingCount(string report)
        {
            int i = report.LastIndexOf(UiAuditRunner.CountPrefix, System.StringComparison.Ordinal);
            if (i < 0) return -1;
            string tail = report.Substring(i + UiAuditRunner.CountPrefix.Length).Trim();
            return int.TryParse(tail, out int n) ? n : -1;
        }
    }
}
