// Assets/Editor/MatchSmokeSelfTest.cs
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CosmicRumble.EditorTools
{
    /// <summary>
    /// Canli mac dumani testini play mode'da surer ve raporu dosyaya yazar.
    /// (Testin kendisi runtime tarafinda: <see cref="MatchSmokeTestRunner"/>.)
    ///
    /// Menuden: Tools > UI > Run Match Smoke Test (Play Mode)
    /// Headless:
    ///   Unity.exe -batchmode -projectPath &lt;proje&gt; \
    ///     -executeMethod CosmicRumble.EditorTools.MatchSmokeSelfTest.RunBatch
    /// Rapor: &lt;proje&gt;/match-smoke-report.txt. Hata varsa editor 1 ile cikar.
    /// </summary>
    public static class MatchSmokeSelfTest
    {
        const string k_StageKey = "cosmicrumble.matchsmoke.stage";
        const string k_BatchKey = "cosmicrumble.matchsmoke.batch";
        const string k_MenuScenePath = "Assets/Scenes/MenuScene.unity";

        [MenuItem("Tools/UI/Run Match Smoke Test (Play Mode)")]
        public static void RunFromMenu() => Start(false);

        public static void RunBatch() => Start(true);

        static void Start(bool batch)
        {
            SessionState.SetString(k_StageKey, "entering");
            SessionState.SetBool(k_BatchKey, batch);

            if (EditorApplication.isPlaying) { SpawnRunner(); return; }

            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != k_MenuScenePath)
                EditorSceneManager.OpenScene(k_MenuScenePath, OpenSceneMode.Single);
            EditorApplication.EnterPlaymode();
        }

        [InitializeOnLoadMethod]
        static void Hook()
        {
            if (SessionState.GetString(k_StageKey, "") == "") return;

            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;

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
            if (Object.FindFirstObjectByType<MatchSmokeTestRunner>() != null) return;
            var go = new GameObject("MatchSmokeTestRunner");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<MatchSmokeTestRunner>();
        }

        static void Finish()
        {
            SessionState.SetString(k_StageKey, "");

            string path   = Path.Combine(Directory.GetCurrentDirectory(), MatchSmokeTestRunner.ReportFileName);
            string report = File.Exists(path) ? File.ReadAllText(path) : "[match smoke] rapor dosyasi yazilmadi";
            int failures  = ParseFailureCount(report);

            if (failures == 0) Debug.Log(report);
            else               Debug.LogError(report);

            if (SessionState.GetBool(k_BatchKey, false))
                EditorApplication.Exit(failures == 0 ? 0 : 1);
        }

        /// <summary>Raporun son satirindaki "FAILURES: n" sayacini okur (-1 = rapor yok/bozuk).</summary>
        static int ParseFailureCount(string report)
        {
            int i = report.LastIndexOf(MatchSmokeTestRunner.CountPrefix, System.StringComparison.Ordinal);
            if (i < 0) return -1;
            string tail = report.Substring(i + MatchSmokeTestRunner.CountPrefix.Length).Trim();
            return int.TryParse(tail, out int n) ? n : -1;
        }
    }
}
