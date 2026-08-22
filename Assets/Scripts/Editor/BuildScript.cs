using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PennerKombat.Editor
{
    /// <summary>
    /// CI-taugliche Build-Einstiegspunkte für native Apps.
    /// Aufruf z.B.:
    ///   Unity -quit -batchmode -nographics -projectPath . -buildTarget Android -executeMethod PennerKombat.Editor.BuildScript.BuildAndroid
    /// Siehe docs/BUILD_ANDROID.md und docs/NATIVE_PREREQ.md
    /// </summary>
    public static class BuildScript
    {
        const string ArenaScene = "Assets/Scenes/Arena.unity";
        const string PackageName = "com.dingelanggames.pennerkombat";

        [MenuItem("Tools/Penner Kombat/Build/Android APK (Debug)", priority = 50)]
        public static void BuildAndroidDebugMenu() => BuildAndroidInternal("Builds/PennerKombat-Debug.apk", development: true, aab: false);

        [MenuItem("Tools/Penner Kombat/Build/Android APK (Release)", priority = 51)]
        public static void BuildAndroidReleaseMenu() => BuildAndroidInternal("Builds/PennerKombat.apk", development: false, aab: false);

        [MenuItem("Tools/Penner Kombat/Build/Android AAB (Play Store)", priority = 52)]
        public static void BuildAndroidAABMenu() => BuildAndroidInternal("Builds/PennerKombat.aab", development: false, aab: true);

        [MenuItem("Tools/Penner Kombat/Build/WebGL", priority = 53)]
        public static void BuildWebGLMenu() => BuildWebGLInternal("Builds/WebGL");

        // CLI entry points (game-ci compatible)
        public static void BuildAndroid() => BuildAndroidInternal("Builds/PennerKombat.apk", development: false, aab: false);
        public static void BuildAndroidAAB() => BuildAndroidInternal("Builds/PennerKombat.aab", development: false, aab: true);
        public static void BuildWebGL() => BuildWebGLInternal("Builds/WebGL");

        static void BuildAndroidInternal(string outputPath, bool development, bool aab)
        {
            PkBuildPreparer.Prepare(createSceneIfMissing: true);
            PkProjectSetup.Run(silent: true);

            // Ensure scene list
            string scene = EnsureScene();
            if (scene == null)
            {
                Debug.LogError("[BuildScript] Keine Szene gefunden — Abbruch.");
                EditorApplication.Exit(1);
                return;
            }

            // Player settings (redundant to PkBuildPreparer, but safe for CI)
            PlayerSettings.companyName = "Dingelang Games";
            PlayerSettings.productName = "Penner Kombat";
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, PackageName);
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[]
            {
                UnityEngine.Rendering.GraphicsDeviceType.Vulkan,
                UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3
            });
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;

            // Managed stripping low — BroadcastMessage in Mell.Defi etc
            PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.Android, ManagedStrippingLevel.Low);

            // Keystore? If present via env, use it, otherwise debug
            // (game-ci handles signing via env vars)

            var options = development ? BuildOptions.Development : BuildOptions.None;
            if (aab) EditorUserBuildSettings.buildAppBundle = true;
            else EditorUserBuildSettings.buildAppBundle = false;

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            Debug.Log($"[BuildScript] Baue {(aab ? "AAB" : "APK")} -> {outputPath} (scene: {scene})");

            var report = BuildPipeline.BuildPlayer(
                new[] { scene },
                outputPath,
                BuildTarget.Android,
                options
            );

            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Debug.LogError($"[BuildScript] Build fehlgeschlagen: {report.summary.result}");
                EditorApplication.Exit(1);
            }
            else
            {
                Debug.Log($"[BuildScript] Build erfolgreich: {outputPath} ({report.summary.totalSize / 1024 / 1024} MB)");
            }
        }

        static void BuildWebGLInternal(string outputPath)
        {
            PkBuildPreparer.Prepare(createSceneIfMissing: true);
            PkProjectSetup.Run(silent: true);

            string scene = EnsureScene();
            if (scene == null)
            {
                Debug.LogError("[BuildScript] Keine Szene gefunden — Abbruch.");
                EditorApplication.Exit(1);
                return;
            }

            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.None;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.runInBackground = true;

            Directory.CreateDirectory(outputPath);

            Debug.Log($"[BuildScript] Baue WebGL -> {outputPath}");

            var report = BuildPipeline.BuildPlayer(
                new[] { scene },
                outputPath,
                BuildTarget.WebGL,
                BuildOptions.None
            );

            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Debug.LogError($"[BuildScript] WebGL Build fehlgeschlagen: {report.summary.result}");
                EditorApplication.Exit(1);
            }
        }

        static string EnsureScene()
        {
            if (File.Exists(ArenaScene)) return ArenaScene;
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length > 0) return scenes[0];
            var guids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets" });
            if (guids.Length > 0) return AssetDatabase.GUIDToAssetPath(guids[0]);
            // Create via wizard
            GameSetupWizard.EnsureFolders();
            GameSetupWizard.CreateArenaScene();
            return File.Exists(ArenaScene) ? ArenaScene : null;
        }
    }
}
