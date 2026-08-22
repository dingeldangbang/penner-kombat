using UnityEngine;

namespace PennerKombat
{
    /// <summary>
    /// Verbindet den Charakter-Zustand mit <c>Assets/Shaders/PennerCharacter.shader</c>
    /// (Spec §4.1): setzt Effekt-Modus, Farbe, Intensität und den Treffer-Flash
    /// auf allen Renderern des Kämpfers.
    ///
    /// Ist der Shader nicht im Projekt (oder kein URP), passiert nichts —
    /// die Auren/Trails aus <see cref="CharacterVisuals"/> tragen den Look dann allein.
    /// </summary>
    [RequireComponent(typeof(FighterController))]
    public class CharacterShaderBinder : MonoBehaviour
    {
        public enum EffectMode { None = 0, Grease = 1, Pulse = 2, Gold = 3, Fire = 4, Matrix = 5, Rat = 6 }

        static readonly int IdMode      = Shader.PropertyToID("_EffectMode");
        static readonly int IdColor     = Shader.PropertyToID("_EffectColor");
        static readonly int IdIntensity = Shader.PropertyToID("_EffectIntensity");
        static readonly int IdSpeed     = Shader.PropertyToID("_EffectSpeed");
        static readonly int IdRim       = Shader.PropertyToID("_RimColor");
        static readonly int IdFlash     = Shader.PropertyToID("_Flash");
        static readonly int IdFlashCol  = Shader.PropertyToID("_FlashColor");

        [Header("Setup")]
        public bool applyShaderToRenderers = true;

        private FighterController fighter;
        private Renderer[] renderers;
        private MaterialPropertyBlock block;
        private float flash;

        void Awake()
        {
            fighter = GetComponent<FighterController>();
            renderers = GetComponentsInChildren<Renderer>();
            block = new MaterialPropertyBlock();

            if (applyShaderToRenderers)
            {
                var shader = Shader.Find("PennerKombat/Character");
                if (shader != null)
                    foreach (var r in renderers)
                    {
                        if (r is ParticleSystemRenderer || r is TrailRenderer) continue;
                        var mats = r.materials;
                        for (int i = 0; i < mats.Length; i++)
                            if (mats[i] != null && mats[i].shader != shader)
                                mats[i].shader = shader;
                        r.materials = mats;
                    }
            }
        }

        void LateUpdate()
        {
            if (fighter == null || renderers == null) return;

            EffectMode mode = ModeFor(fighter.fighterId);
            Color c = PennerPalette.ForCharacter(fighter.fighterId);
            float intensity = IntensityFor(fighter, mode);
            float speed = SpeedFor(fighter, mode);

            flash = Mathf.Max(0f, flash - Time.deltaTime * 6f);

            foreach (var r in renderers)
            {
                if (r == null || r is ParticleSystemRenderer || r is TrailRenderer) continue;
                r.GetPropertyBlock(block);
                block.SetFloat(IdMode, (float)mode);
                block.SetColor(IdColor, c);
                block.SetFloat(IdIntensity, intensity);
                block.SetFloat(IdSpeed, speed);
                block.SetColor(IdRim, Color.Lerp(PennerPalette.WarmOrange, c, 0.5f));
                block.SetFloat(IdFlash, flash);
                block.SetColor(IdFlashCol, Color.white);
                r.SetPropertyBlock(block);
            }
        }

        /// <summary>Kurze Überbelichtung beim Treffer (vom Kampfsystem gerufen).</summary>
        public void HitFlash(float amount = 0.85f) => flash = Mathf.Clamp01(amount);

        static EffectMode ModeFor(string id)
        {
            switch (id)
            {
                case GameConstants.CharLeBinde:  return EffectMode.Grease;
                case GameConstants.CharMell:     return EffectMode.Pulse;
                case GameConstants.CharMojoBob:  return EffectMode.Gold;
                case GameConstants.CharTetraPak: return EffectMode.Fire;
                case GameConstants.CharSigi:     return EffectMode.Matrix;
                case GameConstants.CharRolf:     return EffectMode.Rat;
                default:                         return EffectMode.None;
            }
        }

        static float IntensityFor(FighterController f, EffectMode mode)
        {
            switch (mode)
            {
                case EffectMode.Pulse:
                    return f is Mell mell ? Mathf.Lerp(0.2f, 2.2f, Mathf.InverseLerp(60f, 220f, mell.Pulse)) : 1f;
                case EffectMode.Gold:
                    return f is MojoBob bob ? Mathf.Lerp(0.3f, 2.5f, bob.MojoPoints / 7f) : 1f;
                case EffectMode.Grease:
                    return f is LeBinde binde && binde.greaseChargesMax > 0
                        ? Mathf.Lerp(0.15f, 1.4f, (float)binde.GreaseCharges / binde.greaseChargesMax)
                        : 0.6f;
                default:
                    return 0.8f;
            }
        }

        static float SpeedFor(FighterController f, EffectMode mode)
        {
            if (mode == EffectMode.Pulse && f is Mell mell)
                return Mathf.Lerp(1.5f, 9f, Mathf.InverseLerp(60f, 220f, mell.Pulse));
            return 2f;
        }
    }
}
