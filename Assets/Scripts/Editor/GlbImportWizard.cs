using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace PennerKombat.Editor
{
    /// <summary>
    /// GLB-Import über das Menü: Paket installieren, Modelle einsammeln,
    /// Kämpfer-Prefabs daraus bauen und in die FighterDatabase eintragen.
    ///
    /// Menü: Tools → Penner Kombat → GLB …
    /// Siehe docs/MODELLE.md
    /// </summary>
    public static class GlbImportWizard
    {
        const string ModelFolder = FighterAutoSetup.ModelFolder;
        const string PackageId = "com.unity.cloud.gltfast";

        static AddRequest addRequest;

        // ------------------------------------------------------------------
        // 1. Paket
        // ------------------------------------------------------------------

        [MenuItem("Tools/Penner Kombat/GLB/glTFast (GLB-Import) installieren", priority = 20)]
        public static void InstallGltfast()
        {
            if (IsGltfastInstalled())
            {
                Debug.Log("[Penner Kombat] glTFast ist bereits installiert.");
                return;
            }
            Debug.Log("[Penner Kombat] Installiere glTFast … (Package Manager arbeitet im Hintergrund)");
            addRequest = Client.Add(PackageId);
            EditorApplication.update += TrackInstall;
        }

        static void TrackInstall()
        {
            if (addRequest == null || !addRequest.IsCompleted) return;
            EditorApplication.update -= TrackInstall;

            if (addRequest.Status == StatusCode.Success)
                Debug.Log($"✅ [Penner Kombat] {addRequest.Result.packageId} installiert. "
                        + "Define PK_GLTFAST wird beim nächsten Kompilieren gesetzt.");
            else
                Debug.LogError($"[Penner Kombat] Installation fehlgeschlagen: {addRequest.Error?.message}. "
                             + "Alternative: Package Manager → + → Add package by name → " + PackageId);
            addRequest = null;
        }

        public static bool IsGltfastInstalled()
            => System.AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == "glTFast");

        // ------------------------------------------------------------------
        // 2. Ordner
        // ------------------------------------------------------------------

        [MenuItem("Tools/Penner Kombat/GLB/Modell-Ordner anlegen und öffnen", priority = 21)]
        public static void OpenModelFolder()
        {
            EnsureFolder(ModelFolder);
            AssetDatabase.Refresh();
            EditorUtility.RevealInFinder(ModelFolder);
            Debug.Log($"[Penner Kombat] Lege deine .glb-Dateien hier ab: {ModelFolder}\n"
                    + "Benennung nach Charakter-ID, z.B. le_binde.glb, mell.glb, mojo_bob.glb.");
        }

        // ------------------------------------------------------------------
        // 3. Import → Prefabs
        // ------------------------------------------------------------------

        [MenuItem("Tools/Penner Kombat/GLB/Modelle zu Kämpfer-Prefabs machen", priority = 22)]
        public static void BuildPrefabsFromModels()
        {
            if (!IsGltfastInstalled())
            {
                if (EditorUtility.DisplayDialog("glTFast fehlt",
                        "Zum Lesen von .glb-Dateien braucht Unity das Paket glTFast. Jetzt installieren?",
                        "Installieren", "Abbrechen"))
                    InstallGltfast();
                return;
            }

            EnsureFolder(ModelFolder);
            var db = PkQuickStart.LoadOrCreateDatabase();
            var models = CollectModels();
            if (models.Count == 0)
            {
                Debug.LogWarning($"[Penner Kombat] Keine Modelle in {ModelFolder} gefunden. "
                               + "Menü: Tools → Penner Kombat → GLB → Modell-Ordner anlegen und öffnen.");
                return;
            }

            int built = 0;
            foreach (var pair in models)
            {
                string assetPath = AssetDatabase.GetAssetPath(pair.Value);
                if (FighterAutoSetup.BuildFor(assetPath, verbose: true) != null) built++;
            }

            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (built == 0)
                Debug.LogWarning("[Penner Kombat] Kein Modell konnte einem Charakter zugeordnet werden. "
                    + "Dateinamen müssen die Charakter-ID enthalten (le_binde, mell, mojo_bob, dieter, "
                    + "uschi, tetrapak, sigi, rolf, kalle).");
            else
                Debug.Log($"✅ [Penner Kombat] {built} Kämpfer-Prefabs aus GLB gebaut und zugewiesen. Play drücken.");
        }

        /// <summary>Sucht importierte Modelle und ordnet sie über den Dateinamen zu.</summary>
        static Dictionary<string, GameObject> CollectModels()
        {
            var result = new Dictionary<string, GameObject>();
            if (!AssetDatabase.IsValidFolder(ModelFolder)) return result;

            foreach (string guid in AssetDatabase.FindAssets("t:GameObject", new[] { ModelFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string ext = Path.GetExtension(path).ToLowerInvariant();
                if (ext != ".glb" && ext != ".gltf" && ext != ".fbx") continue;

                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (asset == null) continue;

                string name = Path.GetFileNameWithoutExtension(path)
                                  .Replace("_", "").Replace("-", "").ToLowerInvariant();
                foreach (string id in GameConstants.AllCharacterIds)
                {
                    if (!name.Contains(id.Replace("_", ""))) continue;
                    if (!result.ContainsKey(id)) result[id] = asset;
                    break;
                }
            }
            return result;
        }

        // ------------------------------------------------------------------
        // 4. Zurück zu Platzhaltern
        // ------------------------------------------------------------------

        [MenuItem("Tools/Penner Kombat/GLB/Zurück zu Platzhalter-Kapseln", priority = 23)]
        public static void ResetToPlaceholders()
        {
            var db = PkQuickStart.LoadOrCreateDatabase();
            foreach (var cfg in db.fighters)
            {
                if (cfg == null) continue;
                cfg.prefab = null;
                EditorUtility.SetDirty(cfg);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Penner Kombat] Alle Charaktere nutzen wieder die prozeduralen Kapseln.");
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string leaf = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
