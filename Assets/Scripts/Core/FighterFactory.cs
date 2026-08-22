using System;
using System.Collections.Generic;
using UnityEngine;

namespace PennerKombat
{
    /// <summary>
    /// Baut spielbare Kämpfer komplett aus Code — ohne Prefab, ohne Modell,
    /// ohne Animationsclip. Damit ist das Spiel sofort startbar: Kapsel-Körper,
    /// Rigidbody, Collider, AttackPoint, Charakterskript und Farben aus der
    /// Cover-Palette. Sobald echte Modelle da sind, trägt man sie einfach als
    /// <see cref="FighterConfig.prefab"/> ein — dieser Fallback greift dann nicht mehr.
    ///
    /// Siehe docs/SPIELEN.md
    /// </summary>
    public static class FighterFactory
    {
        /// <summary>Charakter-ID → Klassentyp (alle erben von FighterController).</summary>
        static readonly Dictionary<string, Type> Types = new Dictionary<string, Type>
        {
            { GameConstants.CharLeBinde, typeof(LeBinde) },
            { GameConstants.CharMell,    typeof(Mell) },
            { GameConstants.CharMojoBob, typeof(MojoBob) },
            { GameConstants.CharDieter,  typeof(Dieter) },
            { GameConstants.CharUschi,   typeof(Uschi) },
            { GameConstants.CharTetraPak,typeof(TetraPak) },
            { GameConstants.CharSigi,    typeof(Sigi) },
            { GameConstants.CharRolf,    typeof(Rolf) },
            { GameConstants.CharKalle,   typeof(Kalle) },
        };

        public static Type TypeFor(string id)
            => id != null && Types.TryGetValue(id, out var t) ? t : typeof(LeBinde);

        public static Color ColorFor(string id) => PennerPalette.ForCharacter(id);

        /// <summary>
        /// Erzeugt einen kompletten, spielbaren Platzhalter-Kämpfer.
        /// </summary>
        public static GameObject CreatePlaceholder(FighterConfig cfg, Vector3 position, Quaternion rotation)
        {
            if (cfg == null) return null;

            var root = new GameObject($"Fighter_{cfg.id}");
            root.transform.SetPositionAndRotation(position, rotation);

            // --- Physik (Maße nach Statur, docs/DESIGN.md) ---
            float height = StatureTable.Height(cfg.stature);
            float radius = StatureTable.Radius(cfg.stature);

            var body = root.AddComponent<CapsuleCollider>();
            body.height = height;
            body.radius = radius;
            body.center = new Vector3(0f, height * 0.5f, 0f);

            var rb = root.AddComponent<Rigidbody>();
            rb.mass = StatureTable.Mass(cfg.stature);
            rb.useGravity = true;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            SetTag(root, GameConstants.TagFighter);
            SetLayer(root, "Fighter");

            // --- Sichtbarer Körper (reine Geometrie, kein Asset nötig) ---
            Color main = cfg.primaryColor == Color.white ? ColorFor(cfg.id) : cfg.primaryColor;
            Color accent = cfg.secondaryColor == Color.gray
                ? Color.Lerp(main, PennerPalette.Gold, 0.5f)
                : cfg.secondaryColor;

            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);

            float torsoWidth = radius * 2f;
            AddPart(visual.transform, PrimitiveType.Capsule, new Vector3(0f, height * 0.5f, 0f),
                    Vector3.zero, new Vector3(torsoWidth, height * 0.33f, torsoWidth), main, "Torso");
            AddPart(visual.transform, PrimitiveType.Sphere, new Vector3(0f, height * 0.97f, 0f),
                    Vector3.zero, Vector3.one * 0.45f, accent, "Kopf");
            // Nase = Blickrichtung, damit man sofort sieht, wohin die Kapsel schaut
            AddPart(visual.transform, PrimitiveType.Cube, new Vector3(0f, height * 0.955f, 0.26f),
                    Vector3.zero, new Vector3(0.12f, 0.12f, 0.18f), PennerPalette.Gold, "Nase");
            // Schultern
            AddPart(visual.transform, PrimitiveType.Cube, new Vector3(-(radius + 0.02f), height * 0.75f, 0f),
                    Vector3.zero, new Vector3(0.2f, 0.5f, 0.2f), accent, "ArmL");
            AddPart(visual.transform, PrimitiveType.Cube, new Vector3(radius + 0.02f, height * 0.75f, 0f),
                    Vector3.zero, new Vector3(0.2f, 0.5f, 0.2f), accent, "ArmR");

            // --- Angriffs-Punkt (Hitbox-Ursprung) ---
            var attackPoint = new GameObject("AttackPoint");
            attackPoint.transform.SetParent(root.transform, false);
            attackPoint.transform.localPosition = new Vector3(0f, height * 0.61f, radius + 0.5f);

            // --- Charakter-Skript ---
            var fighter = (FighterController)root.AddComponent(TypeFor(cfg.id));
            fighter.fighterId = cfg.id;
            fighter.displayName = cfg.displayName;
            fighter.maxHP = cfg.maxHP;
            fighter.moveSpeed = cfg.moveSpeed;
            fighter.lightDamage = cfg.lightDamage;
            fighter.heavyDamage = cfg.heavyDamage;
            fighter.attackRange = cfg.attackRange;
            fighter.attackPoint = attackPoint.transform;
            fighter.attackBoxSize = new Vector3(1.5f, 1.5f, 2f) * StatureTable.HitboxScale(cfg.stature);
            fighter.enemyLayer = DefaultEnemyMask();

            return root;
        }

        /// <summary>
        /// Trefferschicht: bevorzugt den Layer „Fighter", sonst alles.
        /// (Ein leerer LayerMask würde bedeuten: kein Angriff trifft jemals.)
        /// </summary>
        public static LayerMask DefaultEnemyMask()
        {
            int fighterLayer = LayerMask.NameToLayer("Fighter");
            return fighterLayer >= 0 ? (LayerMask)(1 << fighterLayer) : (LayerMask)~0;
        }

        static void AddPart(Transform parent, PrimitiveType type, Vector3 pos, Vector3 euler,
                            Vector3 scale, Color color, string name)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            var col = go.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(col);
                else UnityEngine.Object.DestroyImmediate(col);
            }
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localEulerAngles = euler;
            go.transform.localScale = scale;

            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                var mat = new Material(DefaultShader());
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.25f);
                if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.25f);
                mr.sharedMaterial = mat;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }
        }

        /// <summary>URP-Lit falls vorhanden, sonst Built-in Standard.</summary>
        public static Shader DefaultShader()
        {
            var s = Shader.Find("Universal Render Pipeline/Lit");
            if (s == null) s = Shader.Find("Standard");
            if (s == null) s = Shader.Find("Sprites/Default");
            return s;
        }

        static void SetTag(GameObject go, string tag)
        {
            try { go.tag = tag; }
            catch (UnityException) { /* Tag nicht angelegt — unkritisch */ }
        }

        static void SetLayer(GameObject go, string layerName)
        {
            int layer = LayerMask.NameToLayer(layerName);
            if (layer >= 0) go.layer = layer;
        }
    }
}
