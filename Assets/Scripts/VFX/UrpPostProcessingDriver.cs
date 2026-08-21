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

        /// <summary>
        /// Combo-Stufe → komplette Profil-Zeile aus Spec §4.3
        /// (Bloom / Vignette / CA / Farbverschiebung / Grain).
        /// </summary>
        public void SetCombo(int combo)
        {
            float bl, vi, ca, gr, contrast, sat;
            if (combo >= 21)      { bl = 1.2f; vi = 0.80f; ca = 0.50f; gr = 0.10f; contrast = 55f; sat = 45f; }
            else if (combo >= 16) { bl = 1.0f; vi = 0.70f; ca = 0.40f; gr = 0.09f; contrast = 45f; sat = 35f; }
            else if (combo >= 11) { bl = 0.8f; vi = 0.60f; ca = 0.30f; gr = 0.08f; contrast = 35f; sat = 25f; }
            else if (combo >= 8)  { bl = 0.7f; vi = 0.50f; ca = 0.20f; gr = 0.07f; contrast = 25f; sat = 15f; }
            else if (combo >= 5)  { bl = 0.6f; vi = 0.40f; ca = 0.15f; gr = 0.06f; contrast = 20f; sat = 10f; }
            else                  { bl = 0.5f; vi = 0.30f; ca = 0.10f; gr = 0.05f; contrast = 15f; sat = 5f; }

            bloom?.intensity.Override(bl);
            vignette?.intensity.Override(vi);
            vignette?.color.Override(Color.Lerp(Color.black, PennerPalette.BloodRed, Mathf.Clamp01(combo / 21f) * 0.7f));
            chroma?.intensity.Override(ca);
            grain?.intensity.Override(gr);
            color?.contrast.Override(contrast);
            color?.saturation.Override(sat);
        }

        /// <summary>Benannte Zustände (Fatality, Dingeneldang, Blackout …) aus Spec §4.2.</summary>
        public void SetState(ScreenState state)
        {
            switch (state)
            {
                case ScreenState.Normal:
                    BuildProfile();
                    break;

                case ScreenState.Fatality:
                    bloom?.intensity.Override(1.2f);
                    bloom?.tint.Override(PennerPalette.BloodRed);
                    vignette?.intensity.Override(0.9f);
                    chroma?.intensity.Override(0.8f);
                    grain?.intensity.Override(0.15f);
                    color?.contrast.Override(30f);
                    color?.saturation.Override(-20f);
                    break;

                case ScreenState.Dingeneldang:
                    bloom?.intensity.Override(2.0f);
                    bloom?.tint.Override(PennerPalette.Gold);
                    vignette?.intensity.Override(0.4f);
                    vignette?.color.Override(PennerPalette.Gold);
                    chroma?.intensity.Override(0.5f);
                    grain?.intensity.Override(0.02f);
                    color?.contrast.Override(50f);
                    color?.saturation.Override(30f);
                    break;

                case ScreenState.MojoLockout:
                    bloom?.intensity.Override(0.25f);
                    color?.saturation.Override(-45f);
                    break;

                case ScreenState.Monochrome:
                    color?.saturation.Override(-100f);
                    vignette?.intensity.Override(0.85f);
                    break;

                case ScreenState.Fusel:
                    color?.colorFilter.Override(Color.Lerp(Color.white, PennerPalette.WarmOrange, 0.35f));
                    dof?.aperture.Override(1.2f);      // betrunkene Unschärfe
                    break;

                case ScreenState.Matrix:
                    color?.colorFilter.Override(Color.Lerp(Color.white, PennerPalette.Hex("39FF14"), 0.3f));
                    chroma?.intensity.Override(0.35f);
                    break;

                case ScreenState.RatSwarm:
                    color?.colorFilter.Override(Color.Lerp(Color.white, PennerPalette.Earth, 0.25f));
                    grain?.intensity.Override(0.12f);
                    break;
            }
        }

        /// <summary>Fokus auf einen bestimmten Kämpfer legen (Spec: DoF-Zeile).</summary>
        public void FocusOn(Transform target)
        {
            if (target == null || Camera.main == null) return;
            SetFocus(Vector3.Distance(Camera.main.transform.position, target.position));
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
