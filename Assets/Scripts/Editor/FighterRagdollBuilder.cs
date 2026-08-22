using UnityEditor;
using UnityEngine;

namespace PennerKombat.Editor
{
    /// <summary>
    /// Baut aus einem Humanoid-Rig automatisch ein Ragdoll: Rigidbodies,
    /// Collider und CharacterJoints an Hüfte, Wirbelsäule, Kopf, Armen und
    /// Beinen. <see cref="RagdollController"/> findet diese Knochen zur
    /// Laufzeit von selbst und schaltet sie beim K.o. scharf — bis dahin sind
    /// sie kinematisch und ihre Collider deaktiviert, stören also nichts.
    ///
    /// Ohne Rig passiert nichts; dann greift weiter die „Bruchbude" aus
    /// Primitives (docs/EXTRAS.md §3).
    ///
    /// Menü: Tools → Penner Kombat → GLB → Ragdoll für alle Kämpfer bauen
    /// </summary>
    public static class FighterRagdollBuilder
    {
        struct BoneDef
        {
            public HumanBodyBones bone;
            public HumanBodyBones parent;
            public float mass;
            public float radiusFactor;   // Anteil der Knochenlänge
            public BoneDef(HumanBodyBones b, HumanBodyBones p, float m, float r)
            { bone = b; parent = p; mass = m; radiusFactor = r; }
        }

        static readonly BoneDef[] Bones =
        {
            new BoneDef(HumanBodyBones.Hips,          HumanBodyBones.LastBone,      2.5f, 0.32f),
            new BoneDef(HumanBodyBones.Spine,         HumanBodyBones.Hips,          2.0f, 0.30f),
            new BoneDef(HumanBodyBones.Chest,         HumanBodyBones.Spine,         2.0f, 0.30f),
            new BoneDef(HumanBodyBones.Head,          HumanBodyBones.Chest,         1.2f, 0.45f),
            new BoneDef(HumanBodyBones.LeftUpperArm,  HumanBodyBones.Chest,         0.9f, 0.20f),
            new BoneDef(HumanBodyBones.LeftLowerArm,  HumanBodyBones.LeftUpperArm,  0.7f, 0.18f),
            new BoneDef(HumanBodyBones.RightUpperArm, HumanBodyBones.Chest,         0.9f, 0.20f),
            new BoneDef(HumanBodyBones.RightLowerArm, HumanBodyBones.RightUpperArm, 0.7f, 0.18f),
            new BoneDef(HumanBodyBones.LeftUpperLeg,  HumanBodyBones.Hips,          1.6f, 0.20f),
            new BoneDef(HumanBodyBones.LeftLowerLeg,  HumanBodyBones.LeftUpperLeg,  1.2f, 0.18f),
            new BoneDef(HumanBodyBones.RightUpperLeg, HumanBodyBones.Hips,          1.6f, 0.20f),
            new BoneDef(HumanBodyBones.RightLowerLeg, HumanBodyBones.RightUpperLeg, 1.2f, 0.18f),
        };

        [MenuItem("Tools/Penner Kombat/GLB/Ragdoll für alle Kämpfer bauen", priority = 27)]
        public static void BuildForAll()
        {
            var db = PkQuickStart.LoadOrCreateDatabase();
            int done = 0, skipped = 0;

            foreach (var cfg in db.fighters)
            {
                if (cfg == null || cfg.prefab == null) { skipped++; continue; }

                string path = AssetDatabase.GetAssetPath(cfg.prefab);
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(cfg.prefab);
                bool ok = Build(instance);
                if (ok)
                {
                    PrefabUtility.SaveAsPrefabAsset(instance, path);
                    done++;
                }
                else skipped++;
                Object.DestroyImmediate(instance);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[Penner Kombat] Ragdoll gebaut: {done} · übersprungen (kein Humanoid-Rig): {skipped}. "
                    + "Ohne Rig fällt der Kämpfer weiter in Primitiv-Trümmer auseinander.");
        }

        /// <summary>
        /// Baut das Ragdoll auf einer Instanz. Gibt false zurück, wenn kein
        /// Humanoid-Rig vorhanden ist oder bereits ein Ragdoll existiert.
        /// </summary>
        public static bool Build(GameObject root)
        {
            var animator = root.GetComponent<Animator>() ?? root.GetComponentInChildren<Animator>();
            if (animator == null || !animator.isHuman || animator.avatar == null) return false;

            var hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            if (hips == null) return false;

            // Schon gebaut? (Rigidbody an der Hüfte)
            if (hips.GetComponent<Rigidbody>() != null) return false;

            float scale = root.transform.lossyScale.y;
            if (scale <= 0.0001f) scale = 1f;

            foreach (var def in Bones)
            {
                var bone = animator.GetBoneTransform(def.bone);
                if (bone == null) continue;

                // Länge zum Kind schätzen (für Collider-Maße)
                float length = 0.25f;
                if (bone.childCount > 0)
                    length = Vector3.Distance(bone.position, bone.GetChild(0).position) / scale;
                length = Mathf.Clamp(length, 0.08f, 0.6f);

                var rb = bone.gameObject.AddComponent<Rigidbody>();
                rb.mass = def.mass;
                rb.isKinematic = true;                 // RagdollController schaltet scharf
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                rb.collisionDetectionMode = CollisionDetectionMode.Discrete;

                AddCollider(bone, def, length);

                if (def.parent == HumanBodyBones.LastBone) continue;
                var parentBone = animator.GetBoneTransform(def.parent);
                var parentBody = parentBone != null ? parentBone.GetComponent<Rigidbody>() : null;
                if (parentBody == null) continue;

                var joint = bone.gameObject.AddComponent<CharacterJoint>();
                joint.connectedBody = parentBody;
                joint.enablePreprocessing = false;
                joint.axis = Vector3.right;
                joint.swingAxis = Vector3.forward;
                joint.lowTwistLimit = new SoftJointLimit { limit = IsLeg(def.bone) ? -20f : -40f };
                joint.highTwistLimit = new SoftJointLimit { limit = IsLeg(def.bone) ? 20f : 40f };
                joint.swing1Limit = new SoftJointLimit { limit = def.bone == HumanBodyBones.Head ? 35f : 55f };
                joint.swing2Limit = new SoftJointLimit { limit = 20f };
            }

            return true;
        }

        static bool IsLeg(HumanBodyBones bone)
            => bone == HumanBodyBones.LeftUpperLeg || bone == HumanBodyBones.LeftLowerLeg
            || bone == HumanBodyBones.RightUpperLeg || bone == HumanBodyBones.RightLowerLeg;

        static void AddCollider(Transform bone, BoneDef def, float length)
        {
            if (def.bone == HumanBodyBones.Head)
            {
                var sphere = bone.gameObject.AddComponent<SphereCollider>();
                sphere.radius = length * def.radiusFactor * 2.2f;
                sphere.enabled = false;
                return;
            }

            if (def.bone == HumanBodyBones.Hips || def.bone == HumanBodyBones.Chest
                || def.bone == HumanBodyBones.Spine)
            {
                var box = bone.gameObject.AddComponent<BoxCollider>();
                box.size = new Vector3(length * 1.6f, length * 1.1f, length * 0.9f);
                box.enabled = false;
                return;
            }

            var capsule = bone.gameObject.AddComponent<CapsuleCollider>();
            capsule.height = length;
            capsule.radius = length * def.radiusFactor;
            capsule.direction = GuessDirection(bone);
            capsule.center = CenterAlong(capsule.direction, length * 0.5f);
            capsule.enabled = false;
        }

        /// <summary>Achse, entlang der der Knochen zu seinem Kind zeigt (0=X, 1=Y, 2=Z).</summary>
        static int GuessDirection(Transform bone)
        {
            if (bone.childCount == 0) return 1;
            Vector3 local = bone.InverseTransformPoint(bone.GetChild(0).position);
            Vector3 abs = new Vector3(Mathf.Abs(local.x), Mathf.Abs(local.y), Mathf.Abs(local.z));
            if (abs.x >= abs.y && abs.x >= abs.z) return 0;
            if (abs.y >= abs.z) return 1;
            return 2;
        }

        static Vector3 CenterAlong(int direction, float distance)
        {
            switch (direction)
            {
                case 0: return new Vector3(distance, 0f, 0f);
                case 2: return new Vector3(0f, 0f, distance);
                default: return new Vector3(0f, distance, 0f);
            }
        }
    }
}
