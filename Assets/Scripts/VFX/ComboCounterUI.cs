using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace PennerKombat
{
    /// <summary>
    /// Dynamischer Combo-Zähler über dem Kämpfer (Spec 4.2):
    /// 1–4 weiß · 5–9 gelb, 20 % größer, pulsierend · 10+ rot, 40 % größer,
    /// flackernd, mit Glow. Erzeugt seine Canvas/Labels selbst, wenn im HUD
    /// keine zugewiesen sind.
    /// </summary>
    public class ComboCounterUI : MonoBehaviour
    {
        public static ComboCounterUI Instance { get; private set; }

        [Header("Layout")]
        public float baseFontSize = 42f;
        public Vector3 worldOffset = new Vector3(0f, 2.6f, 0f);

        private Canvas canvas;
        private readonly Dictionary<FighterController, Entry> entries = new Dictionary<FighterController, Entry>();

        private class Entry
        {
            public TextMeshProUGUI label;
            public TextMeshProUGUI sub;
            public float shownAt;
            public int combo;
        }

        public static ComboCounterUI Ensure()
        {
            if (Instance == null)
            {
                var go = new GameObject("~ComboCounterUI");
                Instance = go.AddComponent<ComboCounterUI>();
            }
            return Instance;
        }

        void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }

            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 800;
            gameObject.AddComponent<UnityEngine.UI.CanvasScaler>().uiScaleMode =
                UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        }

        public void Show(FighterController fighter, int combo)
        {
            if (fighter == null || combo < 2) return;   // ab 2 Treffern sichtbar
            if (!entries.TryGetValue(fighter, out var e) || e.label == null)
            {
                e = CreateEntry(fighter);
                entries[fighter] = e;
            }

            e.combo = combo;
            e.shownAt = Time.time;
            e.label.gameObject.SetActive(true);
            e.sub.gameObject.SetActive(true);

            e.label.text = combo.ToString();
            e.sub.text = combo >= 10 ? "KOMBO!" : "TREFFER";

            Color c = PennerPalette.ForCombo(combo);
            e.label.color = c;
            e.sub.color = c.WithAlpha(0.85f);

            float scale = combo >= 10 ? 1.4f : combo >= 5 ? 1.2f : 1.0f;
            e.label.fontSize = baseFontSize * scale;
            e.sub.fontSize = baseFontSize * 0.35f * scale;

            // Glow/Outline: ab 5 orange, ab 10 rot
            e.label.outlineWidth = combo >= 5 ? 0.25f : 0.15f;
            e.label.outlineColor = combo >= 10 ? PennerPalette.BloodRed : PennerPalette.NightBlue;
        }

        public void Hide(FighterController fighter)
        {
            if (fighter != null && entries.TryGetValue(fighter, out var e) && e.label != null)
            {
                e.label.gameObject.SetActive(false);
                e.sub.gameObject.SetActive(false);
            }
        }

        public void Clear()
        {
            foreach (var kv in entries)
            {
                if (kv.Value.label != null) kv.Value.label.gameObject.SetActive(false);
                if (kv.Value.sub != null) kv.Value.sub.gameObject.SetActive(false);
            }
        }

        Entry CreateEntry(FighterController fighter)
        {
            var root = new GameObject($"Combo_{fighter.displayName}", typeof(RectTransform));
            root.transform.SetParent(transform, false);

            var label = NewLabel(root.transform, baseFontSize, new Vector2(0f, 0f));
            var sub = NewLabel(root.transform, baseFontSize * 0.35f, new Vector2(0f, -baseFontSize * 0.7f));

            return new Entry { label = label, sub = sub };
        }

        TextMeshProUGUI NewLabel(Transform parent, float size, Vector2 offset)
        {
            var go = new GameObject("Label", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.alignment = TextAlignmentOptions.Center;
            t.fontSize = size;
            t.enableWordWrapping = false;
            t.raycastTarget = false;
            var rt = t.rectTransform;
            rt.sizeDelta = new Vector2(400f, size * 1.6f);
            rt.anchoredPosition = offset;
            return t;
        }

        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null) return;

            foreach (var kv in entries)
            {
                var fighter = kv.Key;
                var e = kv.Value;
                if (fighter == null || e.label == null || !e.label.gameObject.activeSelf) continue;

                // Position über dem Kämpfer
                Vector3 screen = cam.WorldToScreenPoint(fighter.transform.position + worldOffset);
                if (screen.z < 0f) { e.label.gameObject.SetActive(false); e.sub.gameObject.SetActive(false); continue; }
                var parent = (RectTransform)e.label.transform.parent;
                parent.position = screen;

                float age = Time.time - e.shownAt;

                // Pop-Animation beim Treffer
                float pop = Mathf.Max(0f, 1f - age * 6f);
                float scale = 1f + pop * 0.35f;

                // Pulsieren ab 5, Flackern ab 10
                if (e.combo >= 10)
                {
                    scale += Mathf.Sin(Time.time * 28f) * 0.05f;
                    float flicker = 0.75f + 0.25f * Mathf.PerlinNoise(Time.time * 18f, 0f);
                    e.label.color = PennerPalette.ForCombo(e.combo).WithAlpha(flicker);
                }
                else if (e.combo >= 5)
                {
                    scale += Mathf.Sin(Time.time * 10f) * 0.03f;
                }

                parent.localScale = Vector3.one * scale;
            }
        }
    }
}
