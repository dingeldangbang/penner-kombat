using UnityEngine;

namespace PennerKombat
{
    /// <summary>
    /// Charakter-Look zur Laufzeit (Spec Abschnitt 2): Signatur-Aura,
    /// Bewegungs-Trail und zustandsabhängige Effekte —
    /// Le Binde: öliger Schmier-Schlüppa-Glanz ·
    /// Mell: pulsabhängiges Flackern + Speedlines ·
    /// Mojo Bob: goldener Glückspartikel-Staub je Mojo-Punkt.
    ///
    /// Wird automatisch an jeden Kämpfer gehängt (FighterController.Awake)
    /// und braucht keinerlei Art-Assets.
    /// </summary>
    [RequireComponent(typeof(FighterController))]
    public class CharacterVisuals : MonoBehaviour
    {
        [Header("Aura")]
        public bool enableAura = true;
        public float auraIntensity = 1f;

        private FighterController fighter;
        private Light aura;
        private TrailRenderer trail;
        private ParticleSystem ambient;
        private Color signature;

        void Awake()
        {
            fighter = GetComponent<FighterController>();
            signature = PennerPalette.ForCharacter(fighter.fighterId);
        }

        void Start()
        {
            if (enableAura) BuildAura();
            BuildTrail();
            BuildAmbient();
        }

        void BuildAura()
        {
            var go = new GameObject("PK_Aura");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.up * 1.1f;
            aura = go.AddComponent<Light>();
            aura.type = LightType.Point;
            aura.color = signature;
            aura.range = 3.2f;
            aura.intensity = 0.35f * auraIntensity;
            aura.shadows = LightShadows.None;
        }

        void BuildTrail()
        {
            trail = gameObject.AddComponent<TrailRenderer>();
            trail.time = 0.18f;
            trail.startWidth = 0.35f;
            trail.endWidth = 0f;
            trail.minVertexDistance = 0.12f;
            trail.emitting = false;
            trail.material = new Material(Shader.Find("Sprites/Default"));
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(signature, 0f), new GradientColorKey(signature, 1f) },
                new[] { new GradientAlphaKey(0.45f, 0f), new GradientAlphaKey(0f, 1f) });
            trail.colorGradient = grad;
        }

        void BuildAmbient()
        {
            var go = new GameObject("PK_Ambient");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.up * 1.0f;
            ambient = go.AddComponent<ParticleSystem>();

            var main = ambient.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 1.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.18f);
            main.startColor = signature;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;

            var emission = ambient.emission;
            emission.rateOverTime = 0f;

            var shape = ambient.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.5f;

            var renderer = ambient.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(Shader.Find("Sprites/Default"));
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ambient.Play();
        }

        void Update()
        {
            if (fighter == null) return;

            if (trail != null) trail.emitting = fighter.comboCount > 0 || IsFast();

            UpdateCharacterState();
        }

        bool IsFast()
        {
            var rb = GetComponent<Rigidbody>();
            return rb != null && rb.velocity.sqrMagnitude > 16f;
        }

        void UpdateCharacterState()
        {
            switch (fighter.fighterId)
            {
                case GameConstants.CharMell:  UpdateMell();    break;
                case GameConstants.CharMojoBob: UpdateMojo();  break;
                case GameConstants.CharLeBinde: UpdateBinde(); break;
                default: UpdateGeneric();                      break;
            }
        }

        // --- Mell: Puls 60–220 steuert Farbe, Trail und Bildschirmrand ---
        void UpdateMell()
        {
            var mell = fighter as Mell;
            if (mell == null) { UpdateGeneric(); return; }

            float p = Mathf.InverseLerp(60f, 220f, mell.Pulse);
            Color c = p < 0.25f ? PennerPalette.NeonBlue
                    : p < 0.6f  ? PennerPalette.WarmOrange
                                : PennerPalette.BloodRed;

            if (aura != null)
            {
                aura.color = c;
                // Herzschlag-Flackern, Frequenz steigt mit dem Puls
                float beat = Mathf.Pow(Mathf.Abs(Mathf.Sin(Time.time * Mathf.Lerp(2f, 9f, p) * Mathf.PI)), 6f);
                aura.intensity = (0.25f + p * 1.4f + beat * 0.6f) * auraIntensity;
            }
            if (trail != null)
            {
                trail.time = Mathf.Lerp(0.12f, 0.4f, p);
                SetTrailColor(c);
                if (p > 0.6f) trail.emitting = true;   // Speedlines ab 160 bpm
            }
            // Roter Bildschirmrand nur für den lokalen Spieler
            if (!fighter.isAI) ScreenEffects.SetPulseRim(p > 0.25f ? p : 0f);
        }

        // --- Mojo Bob: Mojo-Punkte = goldener Glückspartikel-Staub ---
        void UpdateMojo()
        {
            var bob = fighter as MojoBob;
            if (bob == null) { UpdateGeneric(); return; }

            float mojo = Mathf.Clamp01(bob.MojoPoints / 7f);
            if (aura != null)
            {
                aura.color = PennerPalette.Gold;
                aura.intensity = (0.2f + mojo * 1.6f) * auraIntensity;
            }
            if (ambient != null)
            {
                var em = ambient.emission;
                em.rateOverTime = mojo * 28f;
                var main = ambient.main;
                main.startColor = PennerPalette.Gold;
            }
            SetTrailColor(PennerPalette.Gold);
        }

        // --- Le Binde: Fettfilm-Glanz, wird mit Schlüppa-Ladungen stumpfer ---
        void UpdateBinde()
        {
            if (aura != null)
            {
                aura.color = Color.Lerp(PennerPalette.Earth, PennerPalette.BloodRed, 0.4f);
                aura.intensity = (0.25f + Mathf.Sin(Time.time * 1.4f) * 0.05f) * auraIntensity;
            }
            if (ambient != null)
            {
                var em = ambient.emission;
                em.rateOverTime = 4f;   // öliger Schimmer
            }
        }

        void UpdateGeneric()
        {
            if (aura != null)
            {
                aura.color = signature;
                float charge = fighter.fatalBlowMeter / GameConstants.FatalBlowMeterMax;
                aura.intensity = (0.25f + charge * 0.9f) * auraIntensity;
            }
            if (ambient != null)
            {
                var em = ambient.emission;
                em.rateOverTime = fighter.fatalBlowReady ? 14f : 0f;
            }
        }

        void SetTrailColor(Color c)
        {
            if (trail == null) return;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(c, 0f), new GradientColorKey(c, 1f) },
                new[] { new GradientAlphaKey(0.5f, 0f), new GradientAlphaKey(0f, 1f) });
            trail.colorGradient = grad;
        }

        /// <summary>Kurzer Aura-Burst (Buff aktiviert, z. B. Le Bindes „Reif!").</summary>
        public void BuffFlash(Color color, float duration = 0.6f)
        {
            StopAllCoroutines();
            StartCoroutine(BuffFlashRoutine(color, duration));
        }

        System.Collections.IEnumerator BuffFlashRoutine(Color color, float duration)
        {
            if (aura == null) yield break;
            Color old = aura.color;
            float oldI = aura.intensity;
            aura.color = color;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                aura.intensity = Mathf.Lerp(3f, oldI, t / duration);
                yield return null;
            }
            aura.color = old;
            aura.intensity = oldI;
        }
    }
}
