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
            float extra = 0f;
            if (combo >= 16) extra = 0.30f;
            else if (combo >= 11) extra = 0.20f;
            else if (combo >= 8) extra = 0.13f;
            else if (combo >= 5) extra = 0.07f;
            Instance.vignetteTarget = Instance.vignetteBase + extra;
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
            tint.color = Color.clear;
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
}
