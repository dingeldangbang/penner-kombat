using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PennerKombat.Editor
{
    /// <summary>
    /// Bereitet einen Build vor — besonders wichtig in der CI, wo niemand
    /// klicken kann:
    ///
    ///  1. Projekt einrichten (Tags, Layer, Defines)
    ///  2. Arena-Szene erzeugen, falls keine existiert
    ///  3. Szene in die Build Settings eintragen (sonst baut Unity ein leeres Spiel)
    ///  4. Android-Player-Einstellungen setzen (Paketname, SDK, ARM64, Querformat)
    ///
    /// Läuft in der CI automatisch (Batchmode) und ist im Editor über
    /// <c>Tools → Penner Kombat → Build vorbereiten</c> erreichbar.
    /// </summary>
    [InitializeOnLoad]
    public static class PkBuildPreparer
    {
        const string ScenePath = "Assets/Scenes/Arena.unity";
        const string PackageName = "com.dingelanggames.pennerkombat";

        static PkBuildPreparer()
        {
            // Im Batchmode (CI) sofort vorbereiten — im Editor nur auf Zuruf,
            // damit niemandem ungefragt die Szene umgebaut wird.
            if (Application.isBatchMode)
                EditorApplication.delayCall += () => Prepare(createSceneIfMissing: true);
        }

        [MenuItem("Tools/Penner Kombat/Build vorbereiten", priority = 4)]
        public static void PrepareFromMenu() => Prepare(createSceneIfMissing: true);

        public static void Prepare(bool createSceneIfMissing)
        {
            PkProjectSetup.Run(silent: true);
            EnsureDatabase();
            EnsureScene(createSceneIfMissing);
            ConfigurePlayerSettings();
            ConfigureQuality();
            AssetDatabase.SaveAssets();
            Debug.Log("[Penner Kombat] Build vorbereitet.");
        }

        // ---------- Szene ----------

        static void EnsureScene(bool createIfMissing)
        {
            string scene = FindExistingScene();

            if (scene == null && createIfMissing)
            {
                GameSetupWizard.EnsureFolders();
                GameSetupWizard.CreateArenaScene();
                scene = File.Exists(ScenePath) ? ScenePath : FindExistingScene();
            }

            if (scene == null)
            {
                Debug.LogError("[Penner Kombat] Keine Szene vorhanden — der Build wäre leer.");
                return;
            }

            var current = EditorBuildSettings.scenes.ToList();
            if (current.Any(s => s.path == scene && s.enabled))
            {
                Debug.Log($"[Penner Kombat] Build-Szene: {scene}");
                return;
            }

            current.RemoveAll(s => s.path == scene);
            current.Insert(0, new EditorBuildSettingsScene(scene, true));
            EditorBuildSettings.scenes = current.ToArray();
            Debug.Log($"[Penner Kombat] Szene in die Build Settings eingetragen: {scene}");
        }

        static string FindExistingScene()
        {
            if (File.Exists(ScenePath)) return ScenePath;
            var guids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets" });
            return guids.Length > 0 ? AssetDatabase.GUIDToAssetPath(guids[0]) : null;
        }

        static void EnsureDatabase()
        {
            var db = PkQuickStart.LoadOrCreateDatabase();
            EditorUtility.SetDirty(db);
        }

        // ---------- Player Settings ----------

        static void ConfigurePlayerSettings()
        {
            PlayerSettings.companyName = "Dingelang Games";
            PlayerSettings.productName = "Penner Kombat";

            // Handheld: Querformat, kein Autorotieren ins Hochformat
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;

#if UNITY_ANDROID || UNITY_EDITOR
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, PackageName);
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;   // Android 7
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.forceSDCardPermission = false;
            PlayerSettings.Android.startInFullscreen = true;

            // ARM64 ist Pflicht für den Play Store und Standard auf allen aktuellen Geräten
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;

            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[]
            {
                UnityEngine.Rendering.GraphicsDeviceType.Vulkan,
                UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3
            });
#endif
            // WebGL: kleine Downloadgröße, keine Exceptions-Bremse
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.None;
            PlayerSettings.WebGL.dataCaching = true;
        }

        static void ConfigureQuality()
        {
            // Auf Handhelds zählt jedes Bild — Schatten und MSAA runter.
            var names = QualitySettings.names;
            for (int i = 0; i < names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.shadowDistance = Mathf.Min(QualitySettings.shadowDistance, 35f);
                QualitySettings.antiAliasing = 0;
                QualitySettings.vSyncCount = 0;
            }
            if (names.Length > 0) QualitySettings.SetQualityLevel(Mathf.Min(1, names.Length - 1), true);
        }
    }
}
