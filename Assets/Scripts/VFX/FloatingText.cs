using System.Collections;
using UnityEngine;
using TMPro;

namespace PennerKombat
{
    /// <summary>
    /// Aufsteigender Weltraum-Text für Treffer-Callouts („DINGENELDANG!",
    /// „REIF!", „FASSUNGSLOSIGKEIT", Schadenszahlen). Im Cover-Look:
    /// fette Schrift, dunkle Outline, leichtes Wackeln.
    /// </summary>
    public class FloatingText : MonoBehaviour
    {
        public static void Show(Vector3 worldPosition, string text, Color color, float lifetime = 1.1f)
        {
            var go = new GameObject("PK_FloatingText");
            var ft = go.AddComponent<FloatingText>();
            ft.Init(worldPosition, text, color, lifetime);
        }

        /// <summary>Schadenszahl in Trefferfarbe (weiß → gelb → rot je nach Schaden).</summary>
        public static void ShowDamage(Vector3 worldPosition, float damage)
        {
            Color c = damage >= 25f ? PennerPalette.BloodRed
                    : damage >= 12f ? PennerPalette.Gold
                                    : PennerPalette.Pure;
            Show(worldPosition, Mathf.RoundToInt(damage).ToString(), c, 0.8f);
        }

        private TextMeshPro label;
        private float life;
        private float age;
        private Vector3 drift;

        void Init(Vector3 pos, string text, Color color, float lifetime)
        {
            transform.position = pos;
            life = lifetime;
            drift = new Vector3(Random.Range(-0.4f, 0.4f), Random.Range(1.4f, 2.0f), 0f);

            label = gameObject.AddComponent<TextMeshPro>();
            label.text = text;
            label.color = color;
            label.fontSize = 6f;
            label.alignment = TextAlignmentOptions.Center;
            label.enableWordWrapping = false;
            label.outlineWidth = 0.25f;
            label.outlineColor = PennerPalette.NightBlue;
            label.sortingOrder = 100;
        }

        void LateUpdate()
        {
            age += Time.unscaledDeltaTime;
            float k = age / life;
            if (k >= 1f) { Destroy(gameObject); return; }

            transform.position += drift * Time.unscaledDeltaTime * (1f - k);
            if (Camera.main != null)
                transform.rotation = Camera.main.transform.rotation;

            float pop = Mathf.Max(0f, 1f - age * 7f);
            transform.localScale = Vector3.one * (1f + pop * 0.5f);

            if (label != null)
                label.color = label.color.WithAlpha(Mathf.Clamp01(1f - Mathf.Pow(k, 3f)));
        }
    }
}
