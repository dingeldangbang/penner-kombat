#if PK_URP
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace PennerKombat
{
    /// <summary>
    /// Setzt das Post-Processing-Profil aus Spec Abschnitt 7 (Bloom, Tonemapping,
    /// Vignette, Chromatic Aberration, DoF, Film Grain) und moduliert es im Kampf:
    /// Vignette/CA steigen mit der Combo, CA springt bei Fatalities auf 0,5.
    ///
    /// Aktivierung: Scripting-Define <c>PK_URP</c> setzen
    /// (Project Settings → Player → Scripting Define Symbols), sobald das
    /// URP-Package im Projekt liegt. Ohne Define bleibt der Code inaktiv und
    /// <see cref="ScreenEffects"/> übernimmt die Overlay-Variante.
    /// </summary>
    [RequireComponent(typeof(Volume))]
    public class UrpPostProcessingDriver : MonoBehaviour
    {
        public static UrpPostProcessingDriver Instance { get; private set; }

        private Volume volume;
        private Bloom bloom;
        private Vignette vignette;
        private ChromaticAberration chroma;
        private FilmGrain grain;
        private ColorAdjustments color;
        private DepthOfField dof;

        public static UrpPostProcessingDriver Ensure()
        {
            if (Instance == null)
            {
                var go = new GameObject("~PostProcessing");
                var v = go.AddComponent<Volume>();
                v.isGlobal = true;
                v.profile = ScriptableObject.CreateInstance<VolumeProfile>();
                Instance = go.AddComponent<UrpPostProcessingDriver>();
            }
            return Instance;
        }

        void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(this); return; }

            volume = GetComponent<Volume>();
            if (volume.profile == null) volume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
            BuildProfile();
        }

        void BuildProfile()
        {
            bloom = Get<Bloom>();
            bloom.threshold.Override(0.8f);
            bloom.intensity.Override(0.5f);
            bloom.scatter.Override(0.7f);

            color = Get<ColorAdjustments>();
            color.contrast.Override(15f);
            color.saturation.Override(5f);

            vignette = Get<Vignette>();
            vignette.intensity.Override(0.3f);
            vignette.smoothness.Override(0.5f);
            vignette.color.Override(Color.black);

            chroma = Get<ChromaticAberration>();
            chroma.intensity.Override(0.1f);

            grain = Get<FilmGrain>();
            grain.intensity.Override(0.05f);

            dof = Get<DepthOfField>();
            dof.mode.Override(DepthOfFieldMode.Bokeh);
            dof.focusDistance.Override(12f);
            dof.aperture.Override(2f);

            var tonemap = Get<Tonemapping>();
            tonemap.mode.Override(TonemappingMode.ACES);

            var wb = Get<WhiteBalance>();
            wb.temperature.Override(5f);   // wärmer, Laternenlicht
        }

        T Get<T>() where T : VolumeComponent
        {
            if (!volume.profile.TryGet(out T comp))
                comp = volume.profile.Add<T>(true);
            return comp;
        }

        /// <summary>Combo-Stufe → Vignette 0,3…0,6 und leichte Rotverschiebung.</summary>
        public void SetCombo(int combo)
        {
            if (vignette == null) return;
            float v = combo >= 16 ? 0.60f : combo >= 11 ? 0.50f : combo >= 8 ? 0.42f : combo >= 5 ? 0.36f : 0.30f;
            vignette.intensity.Override(v);
            vignette.color.Override(Color.Lerp(Color.black, PennerPalette.BloodRed, Mathf.Clamp01(combo / 20f) * 0.6f));
            if (color != null) color.saturation.Override(5f + Mathf.Clamp01(combo / 20f) * 15f);
        }

        /// <summary>Fatality/X-Ray: Chromatic Aberration auf 0,5 hochziehen.</summary>
        public void SetChromatic(float intensity)
        {
            chroma?.intensity.Override(Mathf.Clamp01(intensity));
        }

        public void SetFocus(float distance)
        {
            dof?.focusDistance.Override(Mathf.Max(0.1f, distance));
        }
    }
}
#endif
