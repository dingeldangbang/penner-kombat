using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace PennerKombat
{
    /// <summary>
    /// Vollbild-Effekte ohne Post-Processing-Package-Zwang: Vignette,
    /// Farbblitz, Blut-auf-der-Linse, Puls-Rand (Mell) und Grau-Lockout
    /// (Mojo Bob). Alle Texturen werden zur Laufzeit erzeugt.
    ///
    /// Ist URP-Post-Processing im Projekt aktiv, ergänzt
    /// <see cref="UrpPostProcessingDriver"/> Bloom/CA/Vignette on top
    /// (Scripting-Define PK_URP).
    ///
    /// Spezifikation: docs/VISUALS.md, Abschnitte 4.3 und 7.
    /// </summary>
    [DefaultExecutionOrder(400)]
    public class ScreenEffects : MonoBehaviour
    {
        public static ScreenEffects Instance { get; private set; }

        private Canvas canvas;
        private Image vignette;      // Randabdunklung (Combo, Puls)
        private Image flash;         // kurzer Farbblitz
        private Image splatter;      // Blut auf der Kamera (Fatality)
        private Image tint;          // Dauer-Tint (Lockout, Blackout)

        private float vignetteBase = 0.30f;   // Spec: Intensität 0,3
        private float vignetteTarget = 0.30f;
        private float pulseAmount;            // Mells Herzschlag-Rand
        private float pulsePhase;

        public static ScreenEffects Ensure()
        {
            if (Instance == null)
            {
                var go = new GameObject("~ScreenEffects");
                Instance = go.AddComponent<ScreenEffects>();
            }
            return Instance;
        }

        void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
            Build();
        }

        void Build()
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 900;      // über HUD, unter Pause-Menü
            gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

            vignette = CreateLayer("Vignette", BuildVignetteTexture());
            vignette.color = Color.black.WithAlpha(vignetteBase);

            tint     = CreateLayer("Tint", Texture2D.whiteTexture);
            tint.color = Color.clear;

            flash    = CreateLayer("Flash", Texture2D.whiteTexture);
            flash.color = Color.clear;

            splatter = CreateLayer("Splatter", BuildSplatterTexture());
            splatter.color = Color.clear;
        }

        Image CreateLayer(string name, Texture2D tex)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            img.raycastTarget = false;
            return img;
        }

        // ---------------- statische Kurz-API ----------------

        public static void FlashColor(Color color, float alpha, float duration)
        {
            Ensure();
            Instance.StartCoroutine(Instance.FlashRoutine(color, alpha, duration));
        }

        /// <summary>Blut auf der Kameralinse (Fatality).</summary>
        public static void Splatter()
        {
            Ensure();
            Instance.StartCoroutine(Instance.SplatterRoutine());
        }

        /// <summary>Vignette laut Combo-Stufe (10/20/30/50 % dunkler).</summary>
        public static void SetComboVignette(int combo)
        {
            Ensure();
            // Spec 4.3: Vignette 0,3 / 0,4 / 0,5 / 0,6 / 0,7 / 0,8
            float v = combo >= 21 ? 0.80f
                    : combo >= 16 ? 0.70f
                    : combo >= 11 ? 0.60f
                    : combo >= 8  ? 0.50f
                    : combo >= 5  ? 0.40f
                                  : 0.30f;
            Instance.vignetteTarget = v;
        }

        /// <summary>Mells Puls-Rand: 0 = aus, 1 = maximal (roter Herzschlag).</summary>
        public static void SetPulseRim(float amount01)
        {
            Ensure();
            Instance.pulseAmount = Mathf.Clamp01(amount01);
        }

        /// <summary>Dauerhafter Bildschirm-Tint (Blackout, Lockout, Giftnebel).</summary>
        public static void SetTint(Color color)
        {
            Ensure();
            Instance.tint.color = color;
        }

        public static void ClearTint()
        {
            if (Instance != null) Instance.tint.color = Color.clear;
        }

        /// <summary>Benannte Bildschirm-Zustände aus der Spec (§4.2/§4.4).</summary>
        public static void SetState(ScreenState state)
        {
            Ensure();
            Instance.ApplyState(state);
        }

        void ApplyState(ScreenState state)
        {
            switch (state)
            {
                case ScreenState.Normal:
                    tint.color = Color.clear;
                    vignetteBase = 0.30f;
                    break;

                case ScreenState.Dingeneldang:      // goldene Überbelichtung, goldener Rand
                    tint.color = PennerPalette.Gold.WithAlpha(0.16f);
                    vignetteBase = 0.40f;
                    FlashColor(PennerPalette.Gold, 0.7f, 0.3f);
                    break;

                case ScreenState.MojoLockout:       // grauer Filter, 15 s
                    tint.color = new Color(0.35f, 0.35f, 0.38f, 0.28f);
                    vignetteBase = 0.35f;
                    break;

                case ScreenState.Fatality:          // fast schwarze Vignette
                    tint.color = PennerPalette.BloodRed.WithAlpha(0.12f);
                    vignetteBase = 0.90f;
                    break;

                case ScreenState.Fusel:             // TetraPak: oranger Feuerfilter
                    tint.color = PennerPalette.WarmOrange.WithAlpha(0.18f);
                    break;

                case ScreenState.Matrix:            // Sigi: grüner Hacker-Glitch
                    tint.color = PennerPalette.Hex("39FF14").WithAlpha(0.12f);
                    break;

                case ScreenState.RatSwarm:          // Rolf: bräunlicher Schwarm-Schleier
                    tint.color = PennerPalette.Earth.WithAlpha(0.14f);
                    break;

                case ScreenState.Monochrome:        // Blackout / X-Ray: entsättigt (Näherung)
                    tint.color = new Color(0.5f, 0.5f, 0.52f, 0.55f);
                    vignetteBase = 0.75f;
                    break;
            }
            vignetteTarget = Mathf.Max(vignetteTarget, vignetteBase);
#if PK_URP
            UrpPostProcessingDriver.Instance?.SetState(state);
#endif
        }

        /// <summary>Regenbogen-Farbrotation für Mells „Sechzehn Stunden" (Spec §4.4).</summary>
        public static void RainbowSweep(float duration = 0.6f)
        {
            Ensure();
            Instance.StartCoroutine(Instance.RainbowRoutine(duration));
        }

        IEnumerator RainbowRoutine(float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                Color c = Color.HSVToRGB((t / duration) % 1f, 0.8f, 1f);
                flash.color = c.WithAlpha(0.18f);
                yield return null;
            }
            flash.color = Color.clear;
        }

        /// <summary>Blackout-Sequenz (Mell bei Puls 220): Schwarz + Elektro-Flackern.</summary>
        public static void Blackout(float seconds)
        {
            Ensure();
            Instance.StartCoroutine(Instance.BlackoutRoutine(seconds));
        }

        // ---------------- Routinen ----------------

        IEnumerator FlashRoutine(Color color, float alpha, float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                flash.color = color.WithAlpha(Mathf.Lerp(alpha, 0f, t / duration));
                yield return null;
            }
            flash.color = Color.clear;
        }

        IEnumerator SplatterRoutine()
        {
            splatter.color = PennerPalette.BloodRed.WithAlpha(0.85f);
            yield return new WaitForSecondsRealtime(1.2f);
            float t = 0f;
            while (t < 2.5f)
            {
                t += Time.unscaledDeltaTime;
                splatter.color = PennerPalette.BloodRed.WithAlpha(Mathf.Lerp(0.85f, 0f, t / 2.5f));
                yield return null;
            }
            splatter.color = Color.clear;
        }

        IEnumerator BlackoutRoutine(float seconds)
        {
            ApplyState(ScreenState.Monochrome);
            tint.color = Color.black;
            float t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                // elektrisches Flackern blau/weiß
                if (Random.value < 0.08f)
                    flash.color = (Random.value < 0.5f ? PennerPalette.NeonBlue : Color.white).WithAlpha(0.25f);
                else
                    flash.color = Color.clear;
                yield return null;
            }
            flash.color = Color.clear;
            ApplyState(ScreenState.Normal);
        }

        void Update()
        {
            // Vignette weich nachziehen
            float current = vignette.color.a;
            float target = vignetteTarget;

            // Mells Herzschlag moduliert die Vignette rot
            Color vColor = Color.black;
            if (pulseAmount > 0.01f)
            {
                pulsePhase += Time.deltaTime * Mathf.Lerp(1.5f, 4.5f, pulseAmount);
                float beat = Mathf.Pow(Mathf.Abs(Mathf.Sin(pulsePhase * Mathf.PI)), 6f);
                target += pulseAmount * (0.15f + 0.25f * beat);
                vColor = Color.Lerp(Color.black, PennerPalette.BloodRed, pulseAmount * (0.5f + 0.5f * beat));
            }

            vignette.color = Color.Lerp(vignette.color, vColor.WithAlpha(target), 6f * Time.unscaledDeltaTime);
        }

        // ---------------- prozedurale Texturen ----------------

        static Texture2D BuildVignetteTexture()
        {
            const int size = 256;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), c) / (size * 0.5f);
                    // Weichheit 0,5 laut Spec
                    float a = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((d - 0.45f) / 0.5f));
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            tex.Apply();
            return tex;
        }

        static Texture2D BuildSplatterTexture()
        {
            const int size = 256;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color[size * size];
            for (int i = 0; i < px.Length; i++) px[i] = Color.clear;

            var rng = new System.Random(1337);
            for (int blob = 0; blob < 26; blob++)
            {
                int cx = rng.Next(size), cy = rng.Next(size);
                int r = rng.Next(6, 34);
                for (int y = -r; y <= r; y++)
                    for (int x = -r; x <= r; x++)
                    {
                        int px_ = cx + x, py = cy + y;
                        if (px_ < 0 || py < 0 || px_ >= size || py >= size) continue;
                        float d = Mathf.Sqrt(x * x + y * y) / r;
                        if (d > 1f) continue;
                        float a = Mathf.Clamp01(1f - d * d);
                        int idx = py * size + px_;
                        px[idx] = new Color(1f, 1f, 1f, Mathf.Max(px[idx].a, a));
                    }
            }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }
    }

    /// <summary>Benannte Vollbild-Zustände (docs/VISUALS.md §4.2/§4.4).</summary>
    public enum ScreenState
    {
        Normal,
        Dingeneldang,
        MojoLockout,
        Fatality,
        Fusel,
        Matrix,
        RatSwarm,
        Monochrome
    }
}
