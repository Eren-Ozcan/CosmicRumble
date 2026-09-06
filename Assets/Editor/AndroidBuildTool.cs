// Assets/Editor/AndroidBuildTool.cs
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CosmicRumble.EditorTools
{
    /// <summary>
    /// Play Store'a çıkacak Android derlemesinin tek yeri: player ayarları, imzalama ve build.
    /// Elle Inspector'dan tıklanan ayarlar sürüm sürüm kayboluyor (ve bir imzasız/yanlış paket
    /// adıyla yüklenen AAB Console'da geri alınamıyor), o yüzden hepsi burada kod olarak duruyor.
    ///
    /// İmza parolası repoya GİRMEZ: önce CR_KEYSTORE_PASS ortam değişkenine, yoksa
    /// android-keystore/keystore.pass dosyasına bakılır (o klasör .gitignore'da; yedeği private
    /// Eren-Ozcan/pictures reposunda).
    /// </summary>
    public static class AndroidBuildTool
    {
        public const string PackageName  = "com.yilkgames.cosmicrumble";
        public const string CompanyName  = "Yilk Games";
        const string KeystorePath = "android-keystore/cosmicrumble-upload.jks";
        const string KeyAlias     = "cosmicrumble";
        const string PassFile     = "android-keystore/keystore.pass";

        [MenuItem("Tools/Android/Apply Release Player Settings")]
        public static void ApplyPlayerSettings()
        {
            PlayerSettings.companyName = CompanyName;
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, PackageName);

            // Play, yeni uygulamalarda 64-bit ve IL2CPP istiyor; ARM64-only paket hem şart hem
            // daha küçük (ARMv7 cihaz hedefimizde yok, minSdk 23 zaten 2015 sonrası).
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion    = AndroidSdkVersions.AndroidApiLevel23;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;

            ApplySigning();

            Debug.Log($"[Android] player settings applied: {PlayerSettings.companyName} / " +
                      $"{PlayerSettings.GetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android)} " +
                      $"version={PlayerSettings.bundleVersion} code={PlayerSettings.Android.bundleVersionCode} " +
                      $"arch={PlayerSettings.Android.targetArchitectures}");
        }

        static void ApplySigning()
        {
            string pass = Environment.GetEnvironmentVariable("CR_KEYSTORE_PASS");
            if (string.IsNullOrEmpty(pass) && File.Exists(PassFile))
                pass = File.ReadAllText(PassFile).Trim();

            if (string.IsNullOrEmpty(pass))
            {
                // İmzasız build de üretilebilir (yerel deneme), ama Console'a yüklenemez.
                Debug.LogWarning("[Android] no keystore password found (CR_KEYSTORE_PASS or " +
                                 PassFile + ") — the build will not be signed with the upload key.");
                PlayerSettings.Android.useCustomKeystore = false;
                return;
            }

            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = Path.GetFullPath(KeystorePath);
            PlayerSettings.Android.keystorePass = pass;
            PlayerSettings.Android.keyaliasName = KeyAlias;
            PlayerSettings.Android.keyaliasPass = pass;
        }

        /// <summary>Play Console'a yüklenecek imzalı AAB.</summary>
        [MenuItem("Tools/Android/Build Signed AAB")]
        public static void BuildAab() => BuildAndroid(aab: true, development: false);

        /// <summary>Cihaza adb ile kurulacak APK — mağaza paketiyle aynı ayarlar, tek fark biçim.</summary>
        [MenuItem("Tools/Android/Build APK (device test)")]
        public static void BuildApk() => BuildAndroid(aab: false, development: false);

        static void BuildAndroid(bool aab, bool development)
        {
            ApplyPlayerSettings();
            EditorUserBuildSettings.buildAppBundle = aab;

            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            string dir = "Builds/Android";
            Directory.CreateDirectory(dir);
            string path = $"{dir}/CosmicRumble-{PlayerSettings.bundleVersion}-{PlayerSettings.Android.bundleVersionCode}" +
                          (aab ? ".aab" : ".apk");

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = path,
                target = BuildTarget.Android,
                options = development ? BuildOptions.Development : BuildOptions.None,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            var s = report.summary;
            Debug.Log($"[Android] result={s.result} errors={s.totalErrors} warnings={s.totalWarnings} " +
                      $"output={s.outputPath} sizeBytes={s.totalSize}");
        }
    }
}
