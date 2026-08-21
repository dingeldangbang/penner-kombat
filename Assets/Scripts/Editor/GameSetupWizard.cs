using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace PennerKombat.Editor
{
    /// <summary>
    /// Editor-Assistent: erstellt automatisch eine minimale Arena-Szene
    /// (Boden, Spawn-Punkte, Bootstrapper) und die FighterDatabase mit allen
    /// 9 Charakteren. Menü: Tools → Penner Kombat → Setup-Szene erzeugen.
    ///
    /// Hinweis: Charakter-Prefabs müssen anschließend gemäß docs/SETUP.md
    /// angelegt und in der Datenbank zugewiesen werden (der Wizard kann keine
    /// animierten Charakter-Prefabs generieren).
    /// </summary>
    public static class GameSetupWizard
    {
        [MenuItem("Tools/Penner Kombat/Setup-Szene erzeugen")]
        public static void CreateArenaScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Boden
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            if (System.Array.IndexOf(UnityEditorInternal.InternalEditorUtility.tags, GameConstants.TagGround) >= 0)
                ground.tag = GameConstants.TagGround;

            // Spawn-Punkte
            var spawn1 = new GameObject("SpawnPoint1");
            spawn1.transform.position = new Vector3(-4f, 0f, 0f);
            var spawn2 = new GameObject("SpawnPoint2");
            spawn2.transform.position = new Vector3(4f, 0f, 0f);

            // Bootstrapper (erzeugt Manager + VFX-Schicht zur Laufzeit)
            var boot = new GameObject("Boot");
            boot.AddComponent<Bootstrapper>();

            // Arena-Look: Laternen, Neon, Nacht-Ambient (docs/VISUALS.md §3)
            var visuals = new GameObject("ArenaVisuals");
            visuals.AddComponent<ArenaVisuals>();

            // 3D-Kampfraum: Wände als Begrenzung (docs/3D.md §4)
            var walls = new GameObject("Walls");
            float half = 12f;
            (Vector3 pos, Vector3 scale)[] wallDefs =
            {
                (new Vector3(0f, 2f,  half), new Vector3(half * 2f, 4f, 0.5f)),
                (new Vector3(0f, 2f, -half), new Vector3(half * 2f, 4f, 0.5f)),
                (new Vector3( half, 2f, 0f), new Vector3(0.5f, 4f, half * 2f)),
                (new Vector3(-half, 2f, 0f), new Vector3(0.5f, 4f, half * 2f))
            };
            foreach (var (wPos, wScale) in wallDefs)
            {
                var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.name = "Wall";
                wall.transform.SetParent(walls.transform, false);
                wall.transform.position = wPos;
                wall.transform.localScale = wScale;
            }

            // Ein werfbares Objekt und ein Trampolin als Beispiel
            var crate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crate.name = "Bierkasten (werfbar)";
            crate.transform.position = new Vector3(-4f, 0.5f, 4f);
            crate.AddComponent<Rigidbody>();
            crate.AddComponent<ArenaObject3D>().kind = ArenaObject3D.ObjectKind.Throwable;

            // Boden im Cover-Look einfärben (nasses Pflaster, Nachtblau)
            var groundRenderer = ground.GetComponent<MeshRenderer>();
            if (groundRenderer != null && groundRenderer.sharedMaterial != null)
            {
                var mat = new Material(groundRenderer.sharedMaterial);
                mat.color = Color.Lerp(PennerPalette.NightBlue, Color.black, 0.25f);
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.65f);
                if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.65f);
                groundRenderer.sharedMaterial = mat;
            }

            // FighterDatabase anlegen und befüllen
            string path = "Assets/Resources/FighterDatabase.asset";
            var db = AssetDatabase.LoadAssetAtPath<FighterDatabase>(path);
            if (db == null)
            {
                db = ScriptableObject.CreateInstance<FighterDatabase>();
                AssetDatabase.CreateAsset(db, path);
            }
            db.EnsureDefaultRoster();
            EditorUtility.SetDirty(db);

            // GameManager direkt anlegen und verdrahten
            // (Bootstrapper dupliziert ihn zur Laufzeit NICHT, da Instance schon existiert)
            var cam = Camera.main;
            if (cam != null)
            {
                var camCtrl = cam.GetComponent<CameraController>() ?? cam.gameObject.AddComponent<CameraController>();
                camCtrl.mode = CameraController.CameraMode.Dynamic;
            }

            var gm = boot.AddComponent<GameManager>();
            gm.database = db;
            gm.spawnPoint1 = spawn1.transform;
            gm.spawnPoint2 = spawn2.transform;

            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/Arena.unity");
            Debug.Log("✅ Setup-Szene erzeugt: Assets/Scenes/Arena.unity. "
                    + "Vergiss nicht, Charakter-Prefabs in der FighterDatabase zuzuweisen (docs/SETUP.md).");
        }

        [MenuItem("Tools/Penner Kombat/Asset-Ordner anlegen")]
        public static void EnsureFolders()
        {
            string[] folders =
            {
                "Assets/Scenes", "Assets/Resources/Audio/Music", "Assets/Resources/Audio/SFX",
                "Assets/Prefabs/Fighters", "Assets/Prefabs/Arena/Props", "Assets/Animations"
            };
            foreach (var f in folders)
                if (!AssetDatabase.IsValidFolder(f))
                    CreateFolder(f);
            AssetDatabase.Refresh();
            Debug.Log("✅ Asset-Ordner bereit.");
        }

        static void CreateFolder(string path)
        {
            string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            string leaf = System.IO.Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
