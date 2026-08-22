using UnityEngine;
using TMPro;

namespace PennerKombat
{
    /// <summary>
    /// (10) Debug-Overlay — docs/TOUCH.md §11.2.
    /// Zeigt Fingerzahl, Joystick-Vektor, Numpad-Richtung, Buttonzustände und
    /// die Puffergröße. Umschaltbar per <c>F3</c> oder Dreifach-Tipp mit drei
    /// Fingern.
    /// </summary>
    public class TouchDebug : MonoBehaviour
    {
        public static TouchDebug Instance { get; private set; }

        public bool visible = false;
        public int playerIndex = 0;

        private TextMeshProUGUI text;

        public static TouchDebug Ensure()
        {
            if (Instance != null) return Instance;
            var go = new GameObject("~TouchDebug");
            Instance = go.AddComponent<TouchDebug>();
            return Instance;
        }

        void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }

            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;
            gameObject.AddComponent<UnityEngine.UI.CanvasScaler>().uiScaleMode =
                UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;

            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            text = go.AddComponent<TextMeshProUGUI>();
            text.fontSize = 22f;
            text.color = PennerPalette.PoisonGrn;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.raycastTarget = false;
            var rt = text.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(24f, -24f);
            rt.sizeDelta = new Vector2(700f, 320f);
        }

        void Update()
        {
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null && keyboard[UnityEngine.InputSystem.Key.F3].wasPressedThisFrame)
                visible = !visible;

            if (text != null) text.enabled = visible;
            if (!visible || text == null) return;

            Vector2 axis = VirtualInput.GetAxis(playerIndex);
            int numpad = axis.sqrMagnitude > 0.01f ? TouchGestureDetector.ToNumpad(axis) : 5;

            text.text =
                $"<b>TOUCH DEBUG</b>  (F3 blendet aus)\n" +
                $"Finger aktiv : {(AntiGhosting.Instance != null ? AntiGhosting.Instance.ActiveFingers : 0)}\n" +
                $"Achse        : {axis.x:0.00} / {axis.y:0.00}   → Numpad {numpad}\n" +
                $"Leicht {Flag(VButton.Light)}  Schwer {Flag(VButton.Heavy)}  Sprung {Flag(VButton.Jump)}\n" +
                $"Block  {Flag(VButton.Block)}  Spez1  {Flag(VButton.Special1)}  Spez2  {Flag(VButton.Special2)}\n" +
                $"X-Ray  {Flag(VButton.FatalBlow)}\n" +
                $"Puffer : {(InputBuffer.Instance != null ? InputBuffer.Instance.bufferWindow : 0f):0.00} s\n" +
                $"Layout : {TouchSettings.Current.layout}  ·  Größe {TouchSettings.Current.buttonSize:0.0}×";
        }

        string Flag(VButton b)
            => VirtualInput.IsHeld(playerIndex, b) ? "<color=#FFD700>■</color>" : "□";
    }
}
