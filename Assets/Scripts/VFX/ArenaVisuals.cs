using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PennerKombat
{
    /// <summary>
    /// Arena-Beleuchtung und Atmosphäre für den Hinterhof „Zum Blauen Eimer"
    /// (Spec Abschnitt 3): warmes Laternen-Spotlight über der Kampfzone,
    /// bläuliches Mondlicht als Ambient, flackerndes Neonschild (1–2 Hz),
    /// Mücken/Staub-Partikel und Gasflaschen-Rauch.
    ///
    /// Erzeugt fehlende Lichter selbst — funktioniert also auch in der
    /// Wizard-Szene ohne gebautes Set.
    /// </summary>
    public class ArenaVisuals : MonoBehaviour
    {
        public static ArenaVisuals Instance { get; private set; }

        [Header("Lichter (leer = wird erzeugt)")]
        public Light keyLantern;
        public Light fillLantern;
        public Light moonAmbient;

        [Header("Neonschild")]
        public Light neonSign;
        public Renderer neonRenderer;
        public float flickerHzMin = 1f;
        public float flickerHzMax = 2f;

        [Header("Atmosphäre")]
        public bool spawnAmbientParticles = true;
        public bool wetGroundReflections = true;

        private float flickerPhase;
        private float nextGlitch;

        public static ArenaVisuals Ensure()
        {
            if (Instance == null)
            {
                var go = new GameObject("~ArenaVisuals");
                Instance = go.AddComponent<ArenaVisuals>();
            }
            return Instance;
        }

        void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(this); return; }
        }

        void Start()
        {
            SetupEnvironment();
            SetupLights();
            if (spawnAmbientParticles) SetupParticles();
        }

        void SetupEnvironment()
        {
            // Nachtblauer Hintergrund + kühles Ambient laut Palette
            var cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = PennerPalette.NightBlue;
            }
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = PennerPalette.NightBlue * 1.4f;
            RenderSettings.ambientEquatorColor = Color.Lerp(PennerPalette.NightBlue, PennerPalette.WarmOrange, 0.18f);
            RenderSettings.ambientGroundColor = PennerPalette.NightBlue * 0.5f;

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = PennerPalette.NightBlue;
            RenderSettings.fogDensity = 0.018f;
        }

        void SetupLights()
        {
            if (keyLantern == null)
                keyLantern = MakeLight("Laterne_Key", new Vector3(-3.5f, 6.5f, -1f), LightType.Spot,
                                       PennerPalette.WarmOrange, 4.2f, 22f, 70f);
            if (fillLantern == null)
                fillLantern = MakeLight("Laterne_Fill", new Vector3(4.5f, 5.5f, -2f), LightType.Spot,
                                        Color.Lerp(PennerPalette.WarmOrange, PennerPalette.Gold, 0.4f), 2.4f, 18f, 60f);
            if (moonAmbient == null)
            {
                moonAmbient = MakeLight("Mondlicht", new Vector3(0f, 12f, 8f), LightType.Directional,
                                        Color.Lerp(PennerPalette.NeonBlue, Color.white, 0.35f), 0.35f, 0f, 0f);
                moonAmbient.transform.rotation = Quaternion.Euler(58f, 200f, 0f);
                moonAmbient.shadows = LightShadows.Soft;
            }
            if (neonSign == null)
                neonSign = MakeLight("Neon_ZumBlauenEimer", new Vector3(0f, 4.2f, 5.5f), LightType.Point,
                                     PennerPalette.NeonBlue, 2.2f, 12f, 0f);

            keyLantern.shadows = LightShadows.Soft;
        }

        Light MakeLight(string name, Vector3 pos, LightType type, Color color,
                        float intensity, float range, float angle)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.position = pos;
            if (type == LightType.Spot) go.transform.rotation = Quaternion.Euler(65f, 0f, 0f);
            var l = go.AddComponent<Light>();
            l.type = type;
            l.color = color;
            l.intensity = intensity;
            if (range > 0f) l.range = range;
            if (angle > 0f) l.spotAngle = angle;
            l.shadows = LightShadows.None;
            return l;
        }

        void SetupParticles()
        {
            // Mücken um die Laterne
            MakeAmbientPS("Mücken", keyLantern.transform.position + Vector3.down * 1.2f,
                          PennerPalette.Gold.WithAlpha(0.5f), 18f, 0.03f, 1.2f, 2.5f);
            // Staub in der Kampfzone
            MakeAmbientPS("Staub", new Vector3(0f, 1.5f, 0f),
                          PennerPalette.Earth.WithAlpha(0.25f), 12f, 0.06f, 6f, 5f);
            // Rauch von der Gasflasche
            MakeAmbientPS("Gasflaschen-Rauch", new Vector3(5.5f, 0.6f, 2f),
                          Color.gray.WithAlpha(0.18f), 6f, 0.5f, 0.8f, 4f);
        }

        ParticleSystem MakeAmbientPS(string name, Vector3 pos, Color color,
                                     float rate, float size, float radius, float lifetime)
        {
            var go = new GameObject("PK_" + name);
            go.transform.SetParent(transform, false);
            go.transform.position = pos;
            var ps = go.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.startColor = color;
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size);
            main.startLifetime = lifetime;
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.35f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 200;

            var emission = ps.emission;
            emission.rateOverTime = rate;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = radius;

            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.35f;
            noise.frequency = 0.4f;

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(Shader.Find("Sprites/Default"));
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return ps;
        }

        void Update()
        {
            FlickerNeon();
        }

        void FlickerNeon()
        {
            if (neonSign == null) return;

            flickerPhase += Time.deltaTime * Mathf.Lerp(flickerHzMin, flickerHzMax, 0.5f) * Mathf.PI * 2f;
            float baseGlow = 1.9f + Mathf.Sin(flickerPhase) * 0.25f;

            // gelegentlicher Aussetzer (defektes Neon)
            if (Time.time > nextGlitch)
            {
                nextGlitch = Time.time + Random.Range(2.5f, 7f);
                StartCoroutine(NeonGlitch());
            }

            neonSign.intensity = Mathf.Lerp(neonSign.intensity, baseGlow, 8f * Time.deltaTime);
            if (neonRenderer != null && neonRenderer.material.HasProperty("_EmissionColor"))
                neonRenderer.material.SetColor("_EmissionColor", PennerPalette.NeonBlue * neonSign.intensity);
        }

        IEnumerator NeonGlitch()
        {
            for (int i = 0; i < Random.Range(2, 5); i++)
            {
                neonSign.intensity = 0.1f;
                yield return new WaitForSeconds(Random.Range(0.03f, 0.09f));
                neonSign.intensity = 2.4f;
                yield return new WaitForSeconds(Random.Range(0.04f, 0.12f));
            }
        }

        /// <summary>Wallbounce-Staub an der Arena-Wand (vom Kampfsystem gerufen).</summary>
        public void WallImpact(Vector3 position)
        {
            VFXManager.Instance?.PlayDust(position, 1.6f);
            CameraShake.Shake(6f, 0.1f);
        }

        /// <summary>Kippt alle Props und wirbelt dabei Staub auf (Le Bindes Mops-Kommando).</summary>
        public void TrashTheYard()
        {
            ArenaManager.Instance?.TiltAllProps();
            for (int i = 0; i < 6; i++)
            {
                Vector3 p = Extensions.RandomHorizontalPoint(Vector3.zero, 6f);
                VFXManager.Instance?.PlayDust(p, 1.2f);
            }
            CameraShake.SlowMotion(0.1f, 0.3f);
        }
    }
}
