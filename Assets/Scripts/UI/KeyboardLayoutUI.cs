using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

namespace PennerKombat
{
    /// <summary>
    /// (2) Tastatur-Layout-Anzeige und -Umbelegung — docs/CONTROLS.md.
    /// Zeigt beide Spielerbelegungen als Tabelle und erlaubt das Neubelegen
    /// per Klick („Drücke eine Taste …"). Baut die Liste selbst auf, wenn kein
    /// Container zugewiesen ist.
    /// </summary>
    public class KeyboardLayoutUI : MonoBehaviour
    {
        [Header("Container (leer = wird erzeugt)")]
        public RectTransform listP1;
        public RectTransform listP2;
        public TextMeshProUGUI hintText;
        public Button resetButton;

        [Header("Darstellung")]
        public bool allowRebinding = true;

        private static readonly (string action, string label)[] rows =
        {
            ("Up",        "Hoch"),
            ("Down",      "Runter"),
            ("Left",      "Links"),
            ("Right",     "Rechts"),
            ("Light",     "Leichter Angriff"),
            ("Heavy",     "Schwerer Angriff"),
            ("Block",     "Block"),
            ("Jump",      "Sprung"),
            ("Special1",  "Spezial 1"),
            ("Special2",  "Spezial 2"),
            ("Ex",        "EX-Move"),
            ("Interact",  "Interaktion"),
            ("FatalBlow", "X-Ray / Fatal Blow"),
            ("Med",       "Med-Kapsel")
        };

        private readonly Dictionary<(int, string), TextMeshProUGUI> labels =
            new Dictionary<(int, string), TextMeshProUGUI>();
        private bool waitingForKey;

        void Start()
        {
            if (listP1 == null || listP2 == null) BuildCanvas();
            BuildRows(0, listP1);
            BuildRows(1, listP2);
            Refresh();

            resetButton?.onClick.AddListener(() =>
            {
                KeyBindings.Reset();
                var input = FighterInput.Instance;
                if (input != null)
                {
                    // Standardwerte durch Neuinitialisierung der Komponente
                    var go = input.gameObject;
                    Destroy(input);
                    go.AddComponent<FighterInput>();
                }
                StartCoroutine(RefreshNextFrame());
            });
        }

        IEnumerator RefreshNextFrame()
        {
            yield return null;
            Refresh();
        }

        void BuildCanvas()
        {
            var canvas = gameObject.GetComponent<Canvas>() ?? gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            if (GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();

            listP1 = Column("Spieler 1  (WASD)", new Vector2(0.25f, 0.5f));
            listP2 = Column("Spieler 2  (Pfeile + Nummernblock)", new Vector2(0.75f, 0.5f));

            var hint = new GameObject("Hint", typeof(RectTransform));
            hint.transform.SetParent(transform, false);
            hintText = hint.AddComponent<TextMeshProUGUI>();
            hintText.alignment = TextAlignmentOptions.Center;
            hintText.fontSize = 24f;
            hintText.color = PennerPalette.Gold;
            var hrt = hintText.rectTransform;
            hrt.anchorMin = new Vector2(0.5f, 0f);
            hrt.anchorMax = new Vector2(0.5f, 0f);
            hrt.anchoredPosition = new Vector2(0f, 60f);
            hrt.sizeDelta = new Vector2(900f, 40f);
            hintText.text = allowRebinding ? "Klicke auf eine Taste, um sie neu zu belegen." : "";
        }

        RectTransform Column(string title, Vector2 anchor)
        {
            var go = new GameObject(title, typeof(RectTransform));
            go.transform.SetParent(transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(560f, 780f);

            var header = new GameObject("Header", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            header.transform.SetParent(rt, false);
            header.text = title;
            header.fontSize = 30f;
            header.color = PennerPalette.Pure;
            header.alignment = TextAlignmentOptions.Center;
            header.rectTransform.anchoredPosition = new Vector2(0f, 400f);
            header.rectTransform.sizeDelta = new Vector2(560f, 46f);
            return rt;
        }

        void BuildRows(int player, RectTransform parent)
        {
            if (parent == null) return;
            for (int i = 0; i < rows.Length; i++)
            {
                var (action, label) = rows[i];
                float y = 340f - i * 50f;

                var name = new GameObject($"{action}_Label", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
                name.transform.SetParent(parent, false);
                name.text = label;
                name.fontSize = 24f;
                name.color = PennerPalette.Pure.WithAlpha(0.85f);
                name.alignment = TextAlignmentOptions.Left;
                name.rectTransform.anchoredPosition = new Vector2(-130f, y);
                name.rectTransform.sizeDelta = new Vector2(300f, 40f);

                var keyGo = new GameObject($"{action}_Key", typeof(RectTransform));
                keyGo.transform.SetParent(parent, false);
                var keyRt = (RectTransform)keyGo.transform;
                keyRt.anchoredPosition = new Vector2(180f, y);
                keyRt.sizeDelta = new Vector2(190f, 40f);

                var bg = keyGo.AddComponent<Image>();
                bg.color = PennerPalette.NightBlue.WithAlpha(0.8f);

                var keyText = new GameObject("Text", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
                keyText.transform.SetParent(keyRt, false);
                keyText.alignment = TextAlignmentOptions.Center;
                keyText.fontSize = 24f;
                keyText.color = PennerPalette.Gold;
                keyText.rectTransform.anchorMin = Vector2.zero;
                keyText.rectTransform.anchorMax = Vector2.one;
                keyText.rectTransform.offsetMin = Vector2.zero;
                keyText.rectTransform.offsetMax = Vector2.zero;
                labels[(player, action)] = keyText;

                if (allowRebinding)
                {
                    var btn = keyGo.AddComponent<Button>();
                    int p = player;
                    string a = action;
                    btn.onClick.AddListener(() => StartCoroutine(Rebind(p, a)));
                }
            }
        }

        public void Refresh()
        {
            var input = FighterInput.Instance;
            if (input == null) return;
            foreach (var kv in labels)
                kv.Value.text = Pretty(input.GetBinding(kv.Key.Item1, kv.Key.Item2));
        }

        IEnumerator Rebind(int player, string action)
        {
            if (waitingForKey) yield break;
            waitingForKey = true;
            if (hintText != null)
                hintText.text = $"Neue Taste für {action} (Spieler {player + 1}) drücken … (Esc bricht ab)";

            var target = labels[(player, action)];
            string old = target.text;
            target.text = "…";

            while (waitingForKey)
            {
                var kb = Keyboard.current;
                if (kb == null) break;

                if (kb[Key.Escape].wasPressedThisFrame)
                {
                    target.text = old;
                    break;
                }

                foreach (var control in kb.allKeys)
                {
                    if (!control.wasPressedThisFrame) continue;
                    FighterInput.Instance.SetBinding(player, action, control.keyCode);
                    waitingForKey = false;
                    break;
                }
                yield return null;
            }

            waitingForKey = false;
            if (hintText != null) hintText.text = "Klicke auf eine Taste, um sie neu zu belegen.";
            Refresh();
        }

        static string Pretty(Key k)
        {
            switch (k)
            {
                case Key.None: return "—";
                case Key.LeftShift: return "Shift";
                case Key.Space: return "Leertaste";
                case Key.UpArrow: return "↑";
                case Key.DownArrow: return "↓";
                case Key.LeftArrow: return "←";
                case Key.RightArrow: return "→";
                case Key.NumpadEnter: return "Num Enter";
                case Key.NumpadPlus: return "Num +";
                case Key.NumpadPeriod: return "Num .";
                default:
                    string s = k.ToString();
                    return s.StartsWith("Numpad") ? "Num " + s.Substring(6) : s;
            }
        }
    }
}
