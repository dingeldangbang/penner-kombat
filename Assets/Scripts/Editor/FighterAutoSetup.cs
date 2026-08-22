using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PennerKombat.Editor
{
    /// <summary>
    /// Macht aus einem Modell (GLB/glTF/FBX) automatisch einen spielfertigen
    /// Kämpfer: Charakterskript, Physik, Collider nach Modellmaßen, AttackPoint
    /// an der Schlaghand, Animator, Tag/Layer — gespeichert als Prefab und in
    /// der FighterDatabase eingetragen.
    ///
    /// Zwei Betriebsarten:
    ///  • **Automatik** (Standard): Ein Modell in <c>Assets/Models/Fighters</c>
    ///    ablegen — der Import-Wächter unten baut den Kämpfer sofort.
    ///  • **Manuell**: Menü <c>Tools → Penner Kombat → GLB → …</c>
    ///
    /// Siehe docs/MODELLE.md
    /// </summary>
    public static class FighterAutoSetup
    {
        public const string ModelFolder = "Assets/Models/Fighters";
        public const string PrefabFolder = "Assets/Prefabs/Fighters";
        public const string AutoPrefKey = "pk_model_autosetup";
        const string AutoMenuPath = "Tools/Penner Kombat/GLB/Automatik: neue Modelle sofort einrichten";

        public static bool AutoEnabled
        {
            get => EditorPrefs.GetBool(AutoPrefKey, true);
            set => EditorPrefs.SetBool(AutoPrefKey, value);
        }

        [MenuItem(AutoMenuPath, priority = 24)]
        static void ToggleAuto()
        {
            AutoEnabled = !AutoEnabled;
            Menu.SetChecked(AutoMenuPath, AutoEnabled);
            Debug.Log($"[Penner Kombat] Modell-Automatik ist {(AutoEnabled ? "AN" : "AUS")}.");
        }

        [MenuItem(AutoMenuPath, true)]
        static bool ToggleAutoValidate()
        {
            Menu.SetChecked(AutoMenuPath, AutoEnabled);
            return true;
        }

        [MenuItem("Tools/Penner Kombat/GLB/Ausgewähltes Modell zu Kämpfer machen", priority = 25)]
        static void BuildFromSelection()
        {
            var selection = Selection.GetFiltered<GameObject>(SelectionMode.Assets);
            if (selection.Length == 0)
            {
                Debug.LogWarning("[Penner Kombat] Kein Modell im Project-Fenster ausgewählt.");
                return;
            }
            foreach (var model in selection)
                BuildFor(AssetDatabase.GetAssetPath(model), verbose: true);
            AssetDatabase.SaveAssets();
        }

        // ------------------------------------------------------------------
        // Hauptarbeit
        // ------------------------------------------------------------------

        /// <summary>
        /// Baut aus dem Modell an <paramref name="assetPath"/> einen Kämpfer.
        /// Der Charakter wird über den Dateinamen erkannt (Charakter-ID).
        /// </summary>
        public static GameObject BuildFor(string assetPath, bool verbose)
        {
            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (modelAsset == null) return null;

            string id = GuessCharacterId(assetPath);
            if (id == null)
            {
                if (verbose)
                    Debug.LogWarning($"[Penner Kombat] '{Path.GetFileName(assetPath)}' keinem Charakter zuzuordnen. "
                        + "Dateiname muss eine ID enthalten: "
                        + string.Join(", ", GameConstants.AllCharacterIds));
                return null;
            }

            var db = PkQuickStart.LoadOrCreateDatabase();
            var cfg = db.GetFighter(id);
            if (cfg == null)
            {
                Debug.LogWarning($"[Penner Kombat] Charakter '{id}' fehlt in der FighterDatabase.");
                return null;
            }

            var prefab = BuildFighterPrefab(modelAsset, cfg);
            if (prefab == null) return null;

            cfg.prefab = prefab;
            EditorUtility.SetDirty(cfg);
            EditorUtility.SetDirty(db);

            if (verbose)
                Debug.Log($"✅ [Penner Kombat] {cfg.displayName} ist spielfertig: "
                        + $"{Path.GetFileName(assetPath)} → {AssetDatabase.GetAssetPath(prefab)}");
            return prefab;
        }

        /// <summary>Setzt einen kompletten Kämpfer aus Modell + Charakterdaten zusammen.</summary>
        public static GameObject BuildFighterPrefab(GameObject modelAsset, FighterConfig cfg)
        {
            EnsureFolder(PrefabFolder);

            var root = new GameObject($"PK_{cfg.id}");
            try
            {
                // 1. Modell einhängen
                var model = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset, root.transform);
                if (model == null) model = Object.Instantiate(modelAsset, root.transform);
                // Entpacken, damit wir Komponenten frei umbauen dürfen
                // (Meshes und Materialien zeigen weiterhin auf das Modell-Asset).
                if (PrefabUtility.IsPartOfPrefabInstance(model))
                    PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                model.name = "Model_GLB";
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;

                // 2. Auf Kämpfergröße normieren (1,80 m, Füße auf y = 0, mittig)
                Bounds bounds = NormalizeToFighterSize(model.transform, root.transform, cfg.id);

                // 3. Physik nach den echten Maßen
                var rb = root.AddComponent<Rigidbody>();
                rb.mass = 1f;
                rb.constraints = RigidbodyConstraints.FreezeRotation;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

                var capsule = root.AddComponent<CapsuleCollider>();
                float height = Mathf.Max(0.5f, bounds.size.y);
                float radius = Mathf.Clamp(Mathf.Max(bounds.size.x, bounds.size.z) * 0.5f, 0.2f, height * 0.35f);
                capsule.height = height;
                capsule.radius = radius;
                capsule.center = new Vector3(0f, height * 0.5f, 0f);

                // 4. AttackPoint — bevorzugt an der Schlaghand, sonst vor dem Körper
                var hand = FindHandBone(model.transform);
                var attackPoint = new GameObject("AttackPoint");
                if (hand != null)
                {
                    attackPoint.transform.SetParent(hand, false);
                    attackPoint.transform.localPosition = Vector3.zero;
                }
                else
                {
                    attackPoint.transform.SetParent(root.transform, false);
                    attackPoint.transform.localPosition = new Vector3(0f, height * 0.6f, radius + 0.5f);
                }

                // 5. Charakterskript mit Balance-Werten
                var fighter = (FighterController)root.AddComponent(FighterFactory.TypeFor(cfg.id));
                fighter.fighterId = cfg.id;
                fighter.displayName = cfg.displayName;
                fighter.maxHP = cfg.maxHP;
                fighter.moveSpeed = cfg.moveSpeed;
                fighter.lightDamage = cfg.lightDamage;
                fighter.heavyDamage = cfg.heavyDamage;
                fighter.attackRange = cfg.attackRange;
                fighter.attackPoint = attackPoint.transform;
                fighter.attackBoxSize = new Vector3(radius * 2.2f, height * 0.5f, cfg.attackRange);
                fighter.enemyLayer = FighterFactory.DefaultEnemyMask();

                // 6. Animator von der Modellwurzel hochziehen
                var modelAnimator = model.GetComponentInChildren<Animator>();
                if (modelAnimator != null)
                {
                    var animator = root.AddComponent<Animator>();
                    animator.runtimeAnimatorController = modelAnimator.runtimeAnimatorController;
                    animator.avatar = modelAnimator.avatar;
                    animator.applyRootMotion = false;
                    Object.DestroyImmediate(modelAnimator);
                }

                // 7. Tag und Layer
                TrySetTag(root, GameConstants.TagFighter);
                int layer = LayerMask.NameToLayer("Fighter");
                if (layer >= 0) root.layer = layer;

                // 8. Speichern
                // Immer derselbe Pfad: ein erneuter Import aktualisiert den Kämpfer,
                // statt ein zweites Prefab danebenzulegen.
                string path = $"{PrefabFolder}/PK_{cfg.id}.prefab";
                return PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        /// <summary>
        /// Skaliert das Modell auf <see cref="GlbLibrary.TargetHeight"/>, stellt es
        /// auf den Boden und zentriert es. Liefert die Maße nach der Skalierung.
        /// </summary>
        static Bounds NormalizeToFighterSize(Transform model, Transform root, string id)
        {
            var renderers = model.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return new Bounds(Vector3.zero, new Vector3(0.8f, 1.8f, 0.8f));

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);

            float manual = GlbLibrary.GetScale(id);
            float scale = manual > 0.0001f
                ? manual
                : (bounds.size.y > 0.0001f ? GlbLibrary.TargetHeight / bounds.size.y : 1f);
            model.localScale = Vector3.one * scale;

            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);

            model.position += root.position - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            model.localRotation = Quaternion.Euler(0f, GlbLibrary.GetYaw(id), 0f);

            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        /// <summary>
        /// Sucht die rechte Hand im Rig (Humanoid-Avatar bevorzugt, sonst über
        /// gängige Knochennamen aus Mixamo/Blender/Character Creator).
        /// </summary>
        public static Transform FindHandBone(Transform model)
        {
            var animator = model.GetComponentInChildren<Animator>();
            if (animator != null && animator.isHuman)
            {
                var bone = animator.GetBoneTransform(HumanBodyBones.RightHand);
                if (bone != null) return bone;
            }

            string[] patterns = { "righthand", "hand_r", "r_hand", "handright", "mixamorig:righthand", "hand.r" };
            foreach (var t in model.GetComponentsInChildren<Transform>(true))
            {
                string n = t.name.ToLowerInvariant().Replace(" ", "");
                if (patterns.Any(p => n.Contains(p.Replace(":", "")) || n.Contains(p))) return t;
            }
            return null;
        }

        public static string GuessCharacterId(string assetPath)
        {
            string name = Path.GetFileNameWithoutExtension(assetPath)
                              .Replace("_", "").Replace("-", "").Replace(" ", "").ToLowerInvariant();
            foreach (string id in GameConstants.AllCharacterIds)
                if (name.Contains(id.Replace("_", ""))) return id;
            return null;
        }

        static void TrySetTag(GameObject go, string tag)
        {
            try { go.tag = tag; } catch (UnityException) { }
        }

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string leaf = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }

    /// <summary>
    /// Import-Wächter: sobald ein Modell in <see cref="FighterAutoSetup.ModelFolder"/>
    /// landet oder neu importiert wird, entsteht daraus automatisch ein
    /// spielfertiges Kämpfer-Prefab. Abschaltbar über das Menü.
    /// </summary>
    public class FighterModelPostprocessor : AssetPostprocessor
    {
        static readonly string[] ModelExtensions = { ".glb", ".gltf", ".fbx" };

        /// <summary>FBX im Kämpfer-Ordner gleich als Humanoid importieren.</summary>
        void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(FighterAutoSetup.ModelFolder)) return;
            var importer = assetImporter as ModelImporter;
            if (importer == null) return;
            if (importer.importSettingsMissing)   // nur beim allerersten Import
            {
                importer.animationType = ModelImporterAnimationType.Human;
                importer.importCameras = false;
                importer.importLights = false;
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            }
        }

        static void OnPostprocessAllAssets(string[] imported, string[] deleted,
                                           string[] moved, string[] movedFrom)
        {
            if (!FighterAutoSetup.AutoEnabled) return;

            var candidates = imported.Concat(moved)
                .Where(p => p.StartsWith(FighterAutoSetup.ModelFolder))
                .Where(p => ModelExtensions.Contains(Path.GetExtension(p).ToLowerInvariant()))
                .Distinct()
                .ToArray();
            if (candidates.Length == 0) return;

            // Nach dem Import-Durchlauf ausführen, sonst hängt die AssetDatabase.
            EditorApplication.delayCall += () =>
            {
                foreach (string path in candidates)
                    FighterAutoSetup.BuildFor(path, verbose: true);
                AssetDatabase.SaveAssets();
            };
        }
    }
}
