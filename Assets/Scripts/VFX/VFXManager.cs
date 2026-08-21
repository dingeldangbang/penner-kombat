using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PennerKombat
{
    /// <summary>
    /// Zentrale Treffer-VFX-Fabrik. Baut alle Partikelsysteme (HitSpark,
    /// BloodSplat, Shockwave, GoldKrit, Staub) zur LAUFZEIT — es werden also
    /// keine Prefabs oder Art-Assets benötigt. Sind im Projekt eigene Prefabs
    /// vorhanden, können sie über die Felder oben zugewiesen werden und haben
    /// Vorrang.
    ///
    /// Spezifikation: docs/VISUALS.md, Abschnitte 4.1 und 5.
    /// </summary>
    public class VFXManager : MonoBehaviour
    {
        public static VFXManager Instance { get; private set; }

        [Header("Optionale Art-Overrides (sonst prozedural)")]
        public GameObject hitSparkPrefab;
        public GameObject bloodPrefab;
        public GameObject shockwavePrefab;
        public GameObject goldKritPrefab;

        [Header("Global")]
        [Range(0f, 2f)] public float intensity = 1f;
        public bool gore = true;

        private readonly Dictionary<string, ParticleSystem> pools = new Dictionary<string, ParticleSystem>();
        private Material additive;
        private Material alphaBlend;

        public static VFXManager Ensure()
        {
            if (Instance == null)
            {
                var go = new GameObject("~VFX");
                Instance = go.AddComponent<VFXManager>();
            }
            return Instance;
        }

        void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(this); return; }
            BuildMaterials();
        }

        void BuildMaterials()
        {
            // URP-Partikel-Shader mit Fallback auf Built-in — kein Package-Zwang.
            Shader add = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                      ?? Shader.Find("Particles/Standard Unlit")
                      ?? Shader.Find("Sprites/Default");
            additive = new Material(add) { name = "PK_Additive" };
            additive.SetFloat("_Surface", 1f);          // Transparent
            additive.SetFloat("_Blend", 1f);            // Additive
            additive.renderQueue = 3000;

            alphaBlend = new Material(add) { name = "PK_AlphaBlend" };
            alphaBlend.SetFloat("_Surface", 1f);
            alphaBlend.renderQueue = 3000;
        }

        // ------------------------------------------------------------------
        //  Öffentliche API
        // ------------------------------------------------------------------

        /// <summary>
        /// Kompletter Treffer-Impact: Funken + Blut + optional Schockwelle,
        /// Screen-Shake, Hitstop und Kameraruck — passend zur Trefferklasse.
        /// </summary>
        public void PlayHit(Vector3 position, Vector3 direction, float damage,
                            HitTier tier, string fighterId = null)
        {
            Color signature = fighterId != null
                ? PennerPalette.ForCharacter(fighterId)
                : PennerPalette.WarmOrange;

            switch (tier)
            {
                case HitTier.Light:
                    Spark(position, direction, 5, 0.10f, PennerPalette.Pure, PennerPalette.WarmOrange);
                    Blood(position, direction, damage, 3);
                    break;

                case HitTier.Heavy:
                    Spark(position, direction, 12, 0.18f, PennerPalette.Gold, PennerPalette.WarmOrange);
                    Blood(position, direction, damage, 6);
                    CameraShake.Shake(2f, 0.05f);
                    break;

                case HitTier.Special:
                    Spark(position, direction, 18, 0.22f, signature, PennerPalette.WarmOrange);
                    Blood(position, direction, damage, 7);
                    CameraShake.Shake(5f, 0.10f);
                    break;

                case HitTier.Ex:
                    Spark(position, direction, 40, 0.28f, signature, PennerPalette.Pure);
                    Shockwave(position, signature);
                    Blood(position, direction, damage, 10);
                    CameraShake.Shake(8f, 0.12f);
                    CameraShake.HitStop(0.05f);
                    break;

                case HitTier.Critical:
                    GoldKrit(position);
                    Blood(position, direction, damage, 8);
                    CameraShake.Shake(6f, 0.10f);
                    CameraShake.FrameFreeze(2);
                    ScreenEffects.FlashColor(PennerPalette.Gold, 0.35f, 0.12f);
                    break;

                case HitTier.FatalBlow:
                    Spark(position, direction, 50, 0.35f, PennerPalette.BloodRed, PennerPalette.Pure);
                    Shockwave(position, PennerPalette.BloodRed);
                    Blood(position, direction, damage, 20);
                    CameraShake.Shake(15f, 0.30f);
                    CameraShake.HitStop(0.12f);
                    break;

                case HitTier.Fatality:
                    BloodFountain(position);
                    CameraShake.Shake(20f, 0.50f);
                    ScreenEffects.Splatter();
                    break;
            }
        }

        /// <summary>Geblockter Treffer: weißer Abprall-Blitz, kein Blut.</summary>
        public void PlayBlock(Vector3 position, Vector3 direction)
        {
            Spark(position, -direction, 8, 0.12f, PennerPalette.Pure, PennerPalette.NeonBlue);
            CameraShake.Shake(1f, 0.04f);
        }

        /// <summary>Staubwolke (Wallbounce, Landung, Mops-Kommando).</summary>
        public void PlayDust(Vector3 position, float scale = 1f)
        {
            var ps = GetPool("dust", () => BuildDust());
            Emit(ps, position, Quaternion.identity, Mathf.RoundToInt(12 * scale));
        }

        /// <summary>Elektro-Arcs (Mells Defi, Sigis Hack).</summary>
        public void PlayElectro(Vector3 position, int arcs = 5)
        {
            var ps = GetPool("electro", () => BuildSpark(PennerPalette.NeonBlue, PennerPalette.Pure, 0.16f));
            Emit(ps, position, Quaternion.identity, arcs * 4);
            ScreenEffects.FlashColor(PennerPalette.NeonBlue, 0.25f, 0.1f);
        }

        /// <summary>Glassplitter (Le Bindes Flaschenhals).</summary>
        public void PlayGlass(Vector3 position, Vector3 direction)
        {
            var ps = GetPool("glass", () => BuildSpark(PennerPalette.PoisonGrn, PennerPalette.Earth, 0.09f));
            Emit(ps, position, Quaternion.LookRotation(direction), 5);
        }

        // ------------------------------------------------------------------
        //  Einzeleffekte
        // ------------------------------------------------------------------

        void Spark(Vector3 pos, Vector3 dir, int count, float size, Color from, Color to)
        {
            if (hitSparkPrefab != null) { Instantiate(hitSparkPrefab, pos, Quaternion.LookRotation(SafeDir(dir))); return; }
            var ps = GetPool($"spark_{ColorUtility.ToHtmlStringRGB(from)}_{size:0.00}",
                             () => BuildSpark(from, to, size));
            Emit(ps, pos, Quaternion.LookRotation(SafeDir(dir)), Mathf.RoundToInt(count * intensity));
        }

        void Blood(Vector3 pos, Vector3 dir, float damage, int baseCount)
        {
            if (!gore) return;
            if (bloodPrefab != null) { Instantiate(bloodPrefab, pos, Quaternion.identity); return; }
            var ps = GetPool("blood", () => BuildBlood());
            // Größe skaliert mit Schaden (Spec 4.1: „je mehr Schaden, desto größer")
            var main = ps.main;
            main.startSize = new ParticleSystem.MinMaxCurve(
                Mathf.Lerp(0.10f, 0.50f, Mathf.Clamp01(damage / 40f)) * 0.6f,
                Mathf.Lerp(0.10f, 0.50f, Mathf.Clamp01(damage / 40f)));
            Emit(ps, pos, Quaternion.LookRotation(SafeDir(dir)),
                 Mathf.RoundToInt((baseCount + damage * 0.2f) * intensity));
        }

        void BloodFountain(Vector3 pos)
        {
            if (!gore) return;
            var ps = GetPool("blood", () => BuildBlood());
            StartCoroutine(FountainRoutine(ps, pos));
        }

        IEnumerator FountainRoutine(ParticleSystem ps, Vector3 pos)
        {
            for (int i = 0; i < 14; i++)
            {
                Emit(ps, pos + Vector3.up * 1.2f, Quaternion.LookRotation(Vector3.up), 25);
                yield return new WaitForSeconds(0.06f);
            }
        }

        void Shockwave(Vector3 pos, Color color)
        {
            if (shockwavePrefab != null) { Instantiate(shockwavePrefab, pos, Quaternion.identity); return; }
            StartCoroutine(ShockwaveRoutine(pos, color));
        }

        IEnumerator ShockwaveRoutine(Vector3 pos, Color color)
        {
            // 2D-Ring: skaliert 0,5 m → 3,0 m in 0,15 s (Spec 5.3)
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(go.GetComponent<Collider>());
            go.name = "PK_Shockwave";
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            var mr = go.GetComponent<MeshRenderer>();
            mr.material = new Material(additive);
            mr.material.color = color.WithAlpha(0.8f);
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            float t = 0f, dur = 0.15f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float k = t / dur;
                go.transform.localScale = Vector3.one * Mathf.Lerp(0.5f, 3.0f, k);
                mr.material.color = color.WithAlpha(Mathf.Lerp(0.8f, 0f, k));
                yield return null;
            }
            Destroy(go);
        }

        void GoldKrit(Vector3 pos)
        {
            if (goldKritPrefab != null) { Instantiate(goldKritPrefab, pos, Quaternion.identity); return; }
            var ps = GetPool("goldkrit", () => BuildGoldKrit());
            Emit(ps, pos, Quaternion.identity, 25);
        }

        // ------------------------------------------------------------------
        //  Prozedurale Partikelsysteme (Spec Abschnitt 5)
        // ------------------------------------------------------------------

        ParticleSystem BuildSpark(Color from, Color to, float size)
        {
            var ps = NewSystem("HitSpark", additive);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.1f, 0.4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 8f);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size);
            main.startColor = from;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);

            var col = ps.colorOverLifetime;
            col.enabled = true;
            col.color = Gradient2(from, to);

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 35f;
            shape.radius = 0.05f;
            return ps;
        }

        ParticleSystem BuildBlood()
        {
            var ps = NewSystem("BloodSplat", alphaBlend);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 2.0f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.5f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                PennerPalette.BloodRed, PennerPalette.Hex("B01010"));
            main.gravityModifier = 1.4f;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.12f;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            col.color = Gradient2(PennerPalette.BloodRed, PennerPalette.BloodRed.WithAlpha(0f));
            return ps;
        }

        ParticleSystem BuildGoldKrit()
        {
            var ps = NewSystem("GoldKrit", additive);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(5f, 15f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.8f);
            main.startColor = PennerPalette.Gold;
            main.gravityModifier = 0.4f;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.2f;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            col.color = Gradient2(PennerPalette.Gold, PennerPalette.Pure.WithAlpha(0f));
            return ps;
        }

        ParticleSystem BuildDust()
        {
            var ps = NewSystem("Dust", alphaBlend);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 2.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 1.2f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                PennerPalette.Earth.WithAlpha(0.5f), PennerPalette.NightBlue.WithAlpha(0.4f));
            main.gravityModifier = -0.05f;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.4f;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            col.color = Gradient2(PennerPalette.Earth.WithAlpha(0.5f), PennerPalette.Earth.WithAlpha(0f));
            return ps;
        }

        ParticleSystem NewSystem(string name, Material mat)
        {
            var go = new GameObject("PK_" + name);
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.loop = false;
            main.playOnAwake = false;
            main.maxParticles = 400;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.enabled = false;   // wir emittieren manuell per Emit()

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.material = mat;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return ps;
        }

        static Gradient Gradient2(Color a, Color b)
        {
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(a, 0f), new GradientColorKey(b, 1f) },
                new[] { new GradientAlphaKey(a.a, 0f), new GradientAlphaKey(0f, 1f) });
            return g;
        }

        ParticleSystem GetPool(string key, System.Func<ParticleSystem> factory)
        {
            if (!pools.TryGetValue(key, out var ps) || ps == null)
            {
                ps = factory();
                pools[key] = ps;
            }
            return ps;
        }

        void Emit(ParticleSystem ps, Vector3 pos, Quaternion rot, int count)
        {
            if (ps == null || count <= 0) return;
            ps.transform.SetPositionAndRotation(pos, rot);
            ps.Emit(count);
        }

        static Vector3 SafeDir(Vector3 d) => d.sqrMagnitude < 0.0001f ? Vector3.forward : d.normalized;
    }
}
