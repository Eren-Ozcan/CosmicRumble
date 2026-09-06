// Assets/Editor/BuildDevClientTool.cs
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CosmicRumble.EditorTools
{
    public static class BuildDevClientTool
    {
        [MenuItem("Tools/Host Migration/Build DevClient Standalone")]
        public static void Build()
        {
            var scenes = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();

            // Kaydedilmemiş (untitled) sahneler build'i "save mi?" diyaloğuyla durdurur —
            // headless/otomasyon akışında kimse tıklayamaz. Kaydetmeden kapatıp geç. Untitled
            // tek açık sahmeyse Unity son sahneyi kapatmaya izin vermez, o yüzden önce gerçek
            // bir sahneyi ek olarak açıp öyle kapatıyoruz.
            bool onlyUntitledOpen = Enumerable.Range(0, SceneManager.sceneCount)
                .Select(SceneManager.GetSceneAt)
                .All(s => string.IsNullOrEmpty(s.path));
            if (onlyUntitledOpen && scenes.Length > 0)
                EditorSceneManager.OpenScene(scenes[0], OpenSceneMode.Additive);

            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (string.IsNullOrEmpty(scene.path) && SceneManager.sceneCount > 1)
                    EditorSceneManager.CloseScene(scene, true);
            }

            // Açık sahnelerden herhangi biri kirliyse (editörde değişiklik yapılmış ama
            // kaydedilmemiş) BuildPipeline.BuildPlayer build'den ÖNCE "kaydetmek ister misin?"
            // diyaloğunu açar — headless/otomasyon akışında kimse tıklayamaz, bridge de o sırada
            // tamamen kilitlenir (modal pencere ana thread'i bloklar). Diyalog hiç çıkmasın diye
            // hepsini sessizce diske kaydediyoruz; içerik zaten değişmiş, kaydetmemek anlamlı
            // değil.
            EditorSceneManager.SaveOpenScenes();

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = "Builds/DevClient/CosmicRumble.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
                // Test-only: standalone'da da MİSAFİR OLARAK DEVAM butonunu açar (bkz. LoginScreenUI).
                // CR_AUTOTEST: AutoTestBot'u derlemeye katar (bkz. Networking/AutoTestBot.cs) —
                // sadece -autohost/-autojoin komut satırı argümanı verilince aktifleşir, aksi halde
                // no-op. extraScriptingDefines build'e özel — ProjectSettings'e kalıcı yazılmaz.
                extraScriptingDefines = new[] { "CR_DEV_CLIENT", "CR_AUTOTEST" },
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            Debug.Log($"[BuildDevClient] result={summary.result} totalErrors={summary.totalErrors} totalWarnings={summary.totalWarnings} outputPath={summary.outputPath} sizeBytes={summary.totalSize}");
        }
    }
}
