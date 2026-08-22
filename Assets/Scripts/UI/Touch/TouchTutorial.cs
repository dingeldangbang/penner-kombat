using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PennerKombat
{
    /// <summary>
    /// (8) Touch-Tutorial — docs/TOUCH.md §7.
    /// Sieben Schritte, die beim ersten Start eingeblendet werden. Der
    /// Fortschritt landet in <see cref="TouchSettings.tutorialSeen"/>.
    /// Baut sein Panel selbst, wenn keins zugewiesen ist.
    /// </summary>
    public class TouchTutorial : MonoBehaviour
    {
        public static TouchTutorial Instance { get; private set; }

        [Header("UI (leer = wird erzeugt)")]
        public GameObject panel;
        public TextMeshProUGUI label;
        public Button nextButton;
        public Button skipButton;

        [Header("Verhalten")]
        public bool onlyOnce = true;
        public float autoAdvance = 0f;      // > 0 = automatisch weiterblättern

        private static readonly string[] steps =
        {
            "Linker Daumen: Joystick bewegt deinen Penner.",
            "Rechter Daumen: □ leichter, △ schwerer Angriff.",
            "✕ springt, BLOCK halten verteidigt (78 % weniger Schaden).",
            "○ ist Spezial 1, S2 die zweite Spezialbewegung.",
            "Wische ↓ ↘ → und drücke einen Angriff für Spezialmoves.",
            "BLOCK halten + Spezial = EX-Version bzw. X-Ray.",
            "Steuerung anpassbar unter Optionen → Touch. Viel Glück!"
        };

        private int step;

        public static TouchTutorial Ensure()
        {
            if (Instance != null) return Instance;
            var go = new GameObject("~TouchTutorial");
            Instance = go.AddComponent<TouchTutorial>();
            return Instance;
        }

        void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
        }

        void Start()
        {
            if (onlyOnce && TouchSettings.Current.tutorialSeen) { Finish(); return; }
            if (panel == null) BuildUI();
            Show(0);
            if (autoAdvance > 0f) StartCoroutine(AutoAdvance());
        }

        void BuildUI()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 980;
            gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            gameObject.AddComponent<GraphicRaycaster>();

            panel = new GameObject("Panel", typeof(RectTransform));
            panel.transform.SetParent(transform, false);
            var prt = (RectTransform)panel.transform;
            prt.anchorMin = new Vector2(0.5f, 1f);
            prt.anchorMax = new Vector2(0.5f, 1f);
            prt.pivot = new Vector2(0.5f, 1f);
            prt.anchoredPosition = new Vector2(0f, -60f);
            prt.sizeDelta = new Vector2(1100f, 150f);

            var bg = panel.AddComponent<Image>();
            bg.color = PennerPalette.NightBlue.WithAlpha(0.85f);

            label = NewText(panel.transform, 34f, new Vector2(0f, 18f), new Vector2(1040f, 70f));
            label.color = PennerPalette.Pure;

            nextButton = NewButton("Weiter", new Vector2(320f, -48f), PennerPalette.Gold);
            skipButton = NewButton("Überspringen", new Vector2(-320f, -48f), PennerPalette.Earth);

            nextButton.onClick.AddListener(Next);
            skipButton.onClick.AddListener(Finish);
        }

        TextMeshProUGUI NewText(Transform parent, float size, Vector2 pos, Vector2 dims)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.fontSize = size;
            t.alignment = TextAlignmentOptions.Center;
            t.raycastTarget = false;
            t.rectTransform.anchoredPosition = pos;
            t.rectTransform.sizeDelta = dims;
            return t;
        }

        Button NewButton(string text, Vector2 pos, Color color)
        {
            var go = new GameObject(text, typeof(RectTransform));
            go.transform.SetParent(panel.transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(300f, 54f);

            var img = go.AddComponent<Image>();
            img.color = color.WithAlpha(0.85f);

            var btn = go.AddComponent<Button>();
            var lbl = NewText(go.transform, 26f, Vector2.zero, new Vector2(300f, 54f));
            lbl.text = text;
            lbl.color = PennerPalette.NightBlue;
            return btn;
        }

        void Show(int index)
        {
            step = index;
            if (step >= steps.Length) { Finish(); return; }
            if (label != null) label.text = $"({step + 1}/{steps.Length})  {steps[step]}";
        }

        public void Next() => Show(step + 1);

        IEnumerator AutoAdvance()
        {
            while (step < steps.Length)
            {
                yield return new WaitForSecondsRealtime(autoAdvance);
                Next();
            }
        }

        public void Finish()
        {
            TouchSettings.Current.tutorialSeen = true;
            TouchSettings.Current.Save();
            if (panel != null) panel.SetActive(false);
            Destroy(gameObject, 0.1f);
        }

        /// <summary>Tutorial erneut zeigen (Options-Menü).</summary>
        public static void Replay()
        {
            TouchSettings.Current.tutorialSeen = false;
            TouchSettings.Current.Save();
            Ensure();
        }
    }
}
