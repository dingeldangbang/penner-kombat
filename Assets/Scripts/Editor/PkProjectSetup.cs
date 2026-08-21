using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace PennerKombat.Editor
{
    /// <summary>
    /// Richtet das Unity-Projekt beim ersten Öffnen selbst ein:
    /// Tags (Ground, Fighter, Interactable, Projectile), Layer (Fighter,
    /// Interactable, Projectile), Scripting-Define PK_URP wenn URP installiert
    /// ist, und ein Hinweis, falls die TextMeshPro-Grundressourcen fehlen.
    ///
    /// Menü: Tools → Penner Kombat → Projekt einrichten
    /// </summary>
    [InitializeOnLoad]
    public static class PkProjectSetup
    {
        const string DonePrefKey = "pk_project_setup_done";

        static readonly string[] RequiredTags = { "Ground", "Fighter", "Interactable", "Projectile" };
        static readonly string[] RequiredLayers = { "Fighter", "Interactable", "Projectile" };

        static PkProjectSetup()
        {
            EditorApplication.delayCall += () =>
            {
                if (SessionState.GetBool(DonePrefKey, false)) return;
                SessionState.SetBool(DonePrefKey, true);
                Run(silent: true);
            };
        }

        [MenuItem("Tools/Penner Kombat/Projekt einrichten")]
        public static void RunFromMenu() => Run(silent: false);

        public static void Run(bool silent)
        {
            foreach (var tag in RequiredTags) EnsureTag(tag);
            foreach (var layer in RequiredLayers) EnsureLayer(layer);
            EnsureUrpDefine();
            CheckTmp(silent);
            CheckInputHandler();

            if (!silent)
                Debug.Log("✅ [Penner Kombat] Projekt eingerichtet: Tags, Layer, Defines geprüft.");
        }

        // ---------- Tags & Layer ----------

        static SerializedObject TagManager()
        {
            var asset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset").FirstOrDefault();
            return asset != null ? new SerializedObject(asset) : null;
        }

        static void EnsureTag(string tag)
        {
            var so = TagManager();
            if (so == null) return;
            var tags = so.FindProperty("tags");
            for (int i = 0; i < tags.arraySize; i++)
                if (tags.GetArrayElementAtIndex(i).stringValue == tag) return;

            tags.InsertArrayElementAtIndex(tags.arraySize);
            tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
            so.ApplyModifiedProperties();
        }

        static void EnsureLayer(string layer)
        {
            if (LayerMask.NameToLayer(layer) >= 0) return;
            var so = TagManager();
            if (so == null) return;
            var layers = so.FindProperty("layers");
            for (int i = 8; i < layers.arraySize; i++)   // 0–7 sind reserviert
            {
                var element = layers.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(element.stringValue))
                {
                    element.stringValue = layer;
                    so.ApplyModifiedProperties();
                    return;
                }
            }
            Debug.LogWarning($"[Penner Kombat] Kein freier Layer-Slot für '{layer}'.");
        }

        // ---------- Scripting Defines ----------

        static void EnsureUrpDefine()
        {
            bool urpInstalled = System.AppDomain.CurrentDomain.GetAssemblies()
                .Any(a => a.GetName().Name.Contains("Unity.RenderPipelines.Universal"));
            if (!urpInstalled) return;

            foreach (var group in new[] { NamedBuildTarget.Standalone, NamedBuildTarget.Android })
            {
                string defines = PlayerSettings.GetScriptingDefineSymbols(group);
                if (defines.Split(';').Contains("PK_URP")) continue;
                defines = string.IsNullOrEmpty(defines) ? "PK_URP" : defines + ";PK_URP";
                PlayerSettings.SetScriptingDefineSymbols(group, defines);
                Debug.Log($"[Penner Kombat] Define PK_URP gesetzt für {group.TargetName}.");
            }
        }

        // ---------- TextMeshPro ----------

        static void CheckTmp(bool silent)
        {
            var settingsType = System.AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => { try { return a.GetTypes(); } catch { return new System.Type[0]; } })
                .FirstOrDefault(t => t.FullName == "TMPro.TMP_Settings");
            if (settingsType == null) return;

            var instanceProp = settingsType.GetProperty("instance",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic
                | System.Reflection.BindingFlags.Static);
            object instance = instanceProp?.GetValue(null);
            if (instance != null) return;

            // Versuch, die Grundressourcen automatisch zu importieren.
            var importer = System.AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => { try { return a.GetTypes(); } catch { return new System.Type[0]; } })
                .FirstOrDefault(t => t.Name == "TMP_PackageResourceImporter");
            var method = importer?.GetMethod("ImportResources",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic
                | System.Reflection.BindingFlags.Static);
            if (method != null && method.GetParameters().Length == 3)
            {
                try
                {
                    method.Invoke(null, new object[] { true, false, false });
                    Debug.Log("[Penner Kombat] TextMeshPro-Grundressourcen importiert.");
                    return;
                }
                catch { /* Fallback: Hinweis */ }
            }

            if (!silent)
                Debug.LogWarning("[Penner Kombat] Bitte einmalig ausführen: "
                    + "Window → TextMeshPro → Import TMP Essential Resources (sonst bleibt das HUD leer).");
        }

        // ---------- Input System ----------

        static void CheckInputHandler()
        {
            var settings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset").FirstOrDefault();
            if (settings == null) return;
            var so = new SerializedObject(settings);
            var prop = so.FindProperty("activeInputHandler");
            if (prop == null) return;
            // 0 = altes Input Manager, 1 = neues Input System, 2 = beide
            if (prop.intValue == 0)
            {
                prop.intValue = 2;
                so.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();
                Debug.LogWarning("[Penner Kombat] 'Active Input Handling' auf 'Both' gestellt. "
                    + "Unity muss dafür einmal neu gestartet werden — danach reagiert die Tastatur.");
            }
        }
    }
}
