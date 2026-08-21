using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PennerKombat.Editor
{
    /// <summary>
    /// Ein-Klick-Start: richtet das Projekt ein, baut die Arena-Szene, erzeugt
    /// Platzhalter-Prefabs für alle 9 Kämpfer (Kapsel-Look), trägt sie in die
    /// FighterDatabase ein und startet den Play-Modus.
    ///
    /// Menü: Tools → Penner Kombat → ▶ Alles einrichten und spielen
    /// Siehe docs/SPIELEN.md
    /// </summary>
    public static class PkQuickStart
    {
        const string PrefabFolder = "Assets/Prefabs/Fighters";
        const string MaterialFolder = "Assets/Materials/Fighters";
        const string DatabasePath = "Assets/Resources/FighterDatabase.asset";

        [MenuItem("Tools/Penner Kombat/▶ Alles einrichten und spielen", priority = 0)]
        public static void SetupAndPlay()
        {
            SetupOnly();
            EditorApplication.isPlaying = true;
        }

        [MenuItem("Tools/Penner Kombat/Alles einrichten (ohne Play)", priority = 1)]
        public static void SetupOnly()
        {
            PkProjectSetup.Run(silent: false);
            GameSetupWizard.EnsureFolders();
            GameSetupWizard.CreateArenaScene();
            var db = LoadOrCreateDatabase();
            CreatePlaceholderPrefabs(db);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("✅ [Penner Kombat] Fertig eingerichtet. Play drücken — P1 = WASD/J/K/Shift, Gegner = KI.");
        }

        [MenuItem("Tools/Penner Kombat/Platzhalter-Kämpfer erzeugen", priority = 2)]
        public static void CreatePlaceholdersMenu()
        {
            var db = LoadOrCreateDatabase();
            CreatePlaceholderPrefabs(db);
            AssetDatabase.SaveAssets();
            Debug.Log($"✅ [Penner Kombat] {db.fighters.Count} Platzhalter-Prefabs erzeugt und zugewiesen.");
        }

        // ------------------------------------------------------------------

        public static FighterDatabase LoadOrCreateDatabase()
        {
            EnsureFolder("Assets/Resources");
            var db = AssetDatabase.LoadAssetAtPath<FighterDatabase>(DatabasePath);
            if (db == null)
            {
                db = ScriptableObject.CreateInstance<FighterDatabase>();
                AssetDatabase.CreateAsset(db, DatabasePath);
            }
            db.EnsureDefaultRoster();
            PersistConfigs(db);
            EditorUtility.SetDirty(db);
            return db;
        }

        /// <summary>
        /// Die Roster-Einträge werden per CreateInstance erzeugt und wären nach
        /// einem Editor-Neustart weg. Deshalb hängen wir sie als Unter-Assets
        /// an die Datenbank.
        /// </summary>
        static void PersistConfigs(FighterDatabase db)
        {
            string dbPath = AssetDatabase.GetAssetPath(db);
            if (string.IsNullOrEmpty(dbPath)) return;

            foreach (var cfg in db.fighters)
            {
                if (cfg == null) continue;
                if (!string.IsNullOrEmpty(AssetDatabase.GetAssetPath(cfg))) continue;
                cfg.name = cfg.id;
                AssetDatabase.AddObjectToAsset(cfg, db);
            }
        }

        static void CreatePlaceholderPrefabs(FighterDatabase db)
        {
            EnsureFolder("Assets/Prefabs");
            EnsureFolder(PrefabFolder);
            EnsureFolder("Assets/Materials");
            EnsureFolder(MaterialFolder);

            foreach (var cfg in db.fighters)
            {
                if (cfg == null) continue;
                if (cfg.prefab != null && !IsPlaceholder(cfg.prefab)) continue;   // echtes Modell nicht überschreiben

                var instance = FighterFactory.CreatePlaceholder(cfg, Vector3.zero, Quaternion.identity);
                if (instance == null) continue;
                instance.name = $"PK_{cfg.id}";

                BakeMaterials(instance, cfg.id);

                string path = $"{PrefabFolder}/PK_{cfg.id}.prefab";
                var prefab = PrefabUtility.SaveAsPrefabAsset(instance, path);
                Object.DestroyImmediate(instance);

                cfg.prefab = prefab;
                EditorUtility.SetDirty(cfg);
            }
            EditorUtility.SetDirty(db);
        }

        static bool IsPlaceholder(GameObject prefab)
            => prefab != null && prefab.name.StartsWith("PK_");

        /// <summary>
        /// Zur Laufzeit erzeugte Materialien überleben das Speichern eines
        /// Prefabs nicht — deshalb werden sie als Assets abgelegt.
        /// </summary>
        static void BakeMaterials(GameObject root, string id)
        {
            var cache = new Dictionary<Color, Material>();
            foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                var mat = renderer.sharedMaterial;
                if (mat == null) continue;
                Color color = mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor")
                            : mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;

                if (!cache.TryGetValue(color, out var asset))
                {
                    string safe = ColorUtility.ToHtmlStringRGB(color);
                    string path = $"{MaterialFolder}/PK_{id}_{safe}.mat";
                    asset = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (asset == null)
                    {
                        asset = new Material(mat);
                        AssetDatabase.CreateAsset(asset, path);
                    }
                    cache[color] = asset;
                }
                renderer.sharedMaterial = asset;
            }
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
