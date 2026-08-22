using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace PennerKombat.Editor
{
    /// <summary>
    /// Erzeugt automatisch einen Animator-Controller für einen Kämpfer und
    /// hängt vorhandene Clips (aus dem Modell selbst oder aus
    /// <c>Assets/Animations</c>) an die richtigen Zustände. Die Namen der
    /// Parameter entsprechen exakt dem, was <see cref="FighterController"/> und
    /// <see cref="AnimationsController3D"/> ansteuern.
    ///
    /// Clips werden über Schlüsselwörter im Dateinamen erkannt — passend zu
    /// dem, was Mixamo & Co. ausspucken („Idle", „Walking", „Punching",
    /// „Standing Melee Attack 360 High Kick", „Falling Back Death" …).
    ///
    /// Siehe docs/MODELLE.md §4
    /// </summary>
    public static class FighterAnimatorBuilder
    {
        public const string ControllerFolder = "Assets/Animations/Controllers";
        public const string ClipFolder = "Assets/Animations";

        /// <summary>Zustand → Schlüsselwörter im Clipnamen (erste Treffer gewinnen).</summary>
        static readonly (string state, string[] keywords)[] Mapping =
        {
            ("Idle",        new[] { "idle", "stand", "breathing" }),
            ("Walk",        new[] { "walk", "run", "jog", "strafe" }),
            ("LightAttack", new[] { "punch", "jab", "light", "hook", "cross" }),
            ("HeavyAttack", new[] { "heavy", "kick", "smash", "slam", "strong" }),
            ("Block",       new[] { "block", "guard", "defend" }),
            ("Jump",        new[] { "jump", "hop", "leap" }),
            ("Roll",        new[] { "roll", "dodge", "evade", "dive" }),
            ("HitReact",    new[] { "hit", "impact", "react", "hurt", "stagger" }),
            ("Death",       new[] { "death", "dying", "die", "falling back" }),
            ("Special",     new[] { "special", "combo", "spell", "cast", "taunt" }),
        };

        static readonly string[] Triggers =
            { "LightAttack", "HeavyAttack", "Jump", "Roll", "HitReact", "Death", "Special" };

        [MenuItem("Tools/Penner Kombat/GLB/Animator-Controller für alle Kämpfer bauen", priority = 26)]
        public static void BuildForAllMenu()
        {
            var db = PkQuickStart.LoadOrCreateDatabase();
            int built = 0;
            foreach (var cfg in db.fighters)
            {
                if (cfg == null || cfg.prefab == null) continue;
                var animator = cfg.prefab.GetComponent<Animator>();
                if (animator == null) continue;

                var controller = Build(cfg.id, CollectClips(AssetDatabase.GetAssetPath(cfg.prefab)));
                if (controller == null) continue;

                var instance = (GameObject)PrefabUtility.InstantiatePrefab(cfg.prefab);
                instance.GetComponent<Animator>().runtimeAnimatorController = controller;
                PrefabUtility.SaveAsPrefabAsset(instance, AssetDatabase.GetAssetPath(cfg.prefab));
                Object.DestroyImmediate(instance);
                built++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"✅ [Penner Kombat] {built} Animator-Controller gebaut/aktualisiert.");
        }

        /// <summary>
        /// Baut (oder überschreibt) den Controller für einen Charakter.
        /// Auch ohne einen einzigen Clip sinnvoll: die Parameter existieren dann
        /// schon, und der Kampfcode läuft ohne Warnungsflut.
        /// </summary>
        public static AnimatorController Build(string fighterId, List<AnimationClip> clips)
        {
            EnsureFolder(ControllerFolder);
            string path = $"{ControllerFolder}/PK_{fighterId}.controller";

            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller != null) AssetDatabase.DeleteAsset(path);
            controller = AnimatorController.CreateAnimatorControllerAtPath(path);

            // --- Parameter (Namen sind im Kampfcode fest verdrahtet) ---
            controller.AddParameter("MoveSpeed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Direction", AnimatorControllerParameterType.Float);
            controller.AddParameter("ComboCount", AnimatorControllerParameterType.Int);
            controller.AddParameter("SpecialIndex", AnimatorControllerParameterType.Int);
            controller.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Walk", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Block", AnimatorControllerParameterType.Bool);
            foreach (string t in Triggers)
                controller.AddParameter(t, AnimatorControllerParameterType.Trigger);

            var sm = controller.layers[0].stateMachine;
            var byState = MatchClips(clips);

            // --- Grundzustände ---
            var idle = sm.AddState("Idle", new Vector3(300f, 0f, 0f));
            idle.motion = byState.TryGetValue("Idle", out var idleClip) ? idleClip : null;
            sm.defaultState = idle;

            var walk = sm.AddState("Walk", new Vector3(300f, 90f, 0f));
            walk.motion = byState.TryGetValue("Walk", out var walkClip) ? walkClip : null;

            var toWalk = idle.AddTransition(walk);
            toWalk.hasExitTime = false;
            toWalk.duration = 0.12f;
            toWalk.AddCondition(AnimatorConditionMode.If, 0f, "Walk");

            var toIdle = walk.AddTransition(idle);
            toIdle.hasExitTime = false;
            toIdle.duration = 0.12f;
            toIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "Walk");

            // --- Block als Haltezustand ---
            var block = sm.AddState("Block", new Vector3(300f, 180f, 0f));
            block.motion = byState.TryGetValue("Block", out var blockClip) ? blockClip : null;
            var blockIn = sm.AddAnyStateTransition(block);
            blockIn.hasExitTime = false;
            blockIn.duration = 0.08f;
            blockIn.canTransitionToSelf = false;
            blockIn.AddCondition(AnimatorConditionMode.If, 0f, "Block");
            var blockOut = block.AddTransition(idle);
            blockOut.hasExitTime = false;
            blockOut.duration = 0.1f;
            blockOut.AddCondition(AnimatorConditionMode.IfNot, 0f, "Block");

            // --- Aktionen über Trigger, danach zurück nach Idle ---
            float y = -90f;
            foreach (string trigger in Triggers)
            {
                var state = sm.AddState(trigger, new Vector3(620f, y, 0f));
                state.motion = byState.TryGetValue(trigger, out var clip) ? clip : null;
                y += 70f;

                var enter = sm.AddAnyStateTransition(state);
                enter.hasExitTime = false;
                enter.duration = trigger == "HitReact" ? 0.03f : 0.05f;
                enter.canTransitionToSelf = trigger == "LightAttack" || trigger == "HeavyAttack";
                enter.AddCondition(AnimatorConditionMode.If, 0f, trigger);

                if (trigger == "Death") continue;   // Tod bleibt stehen

                var exit = state.AddTransition(idle);
                exit.hasExitTime = true;
                exit.exitTime = 0.85f;
                exit.duration = 0.1f;
            }

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        /// <summary>Ordnet Clips über Schlüsselwörter den Zuständen zu.</summary>
        static Dictionary<string, AnimationClip> MatchClips(List<AnimationClip> clips)
        {
            var result = new Dictionary<string, AnimationClip>();
            if (clips == null) return result;

            foreach (var (state, keywords) in Mapping)
            {
                foreach (var clip in clips)
                {
                    if (clip == null) continue;
                    string name = clip.name.ToLowerInvariant();
                    if (name.Contains("t-pose") || name.Contains("tpose")) continue;
                    if (!keywords.Any(k => name.Contains(k))) continue;
                    if (result.ContainsValue(clip)) continue;   // jeden Clip nur einmal vergeben
                    result[state] = clip;
                    break;
                }
            }
            return result;
        }

        /// <summary>
        /// Sammelt Clips: erst die im Modell/Prefab selbst enthaltenen,
        /// dann alles unter <see cref="ClipFolder"/>.
        /// </summary>
        public static List<AnimationClip> CollectClips(string assetPath)
        {
            var clips = new List<AnimationClip>();

            if (!string.IsNullOrEmpty(assetPath))
            {
                foreach (var sub in AssetDatabase.LoadAllAssetRepresentationsAtPath(assetPath))
                    if (sub is AnimationClip clip && !clip.name.StartsWith("__preview"))
                        clips.Add(clip);
            }

            if (AssetDatabase.IsValidFolder(ClipFolder))
            {
                foreach (string guid in AssetDatabase.FindAssets("t:AnimationClip", new[] { ClipFolder }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    foreach (var sub in AssetDatabase.LoadAllAssetsAtPath(path))
                        if (sub is AnimationClip clip && !clip.name.StartsWith("__preview") && !clips.Contains(clip))
                            clips.Add(clip);
                }
            }
            return clips;
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
