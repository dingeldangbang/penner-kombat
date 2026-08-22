using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PennerKombat
{
    /// <summary>
    /// (4) Gesten-Erkennung — docs/TOUCH.md §4.
    /// Erkennt Wischfolgen in 8 Richtungen auf dem Joystick und übersetzt sie
    /// in echte Richtungs-Sequenzen: die erkannte Numpad-Folge wird über
    /// <see cref="VirtualInput"/> Frame für Frame „nachgespielt", sodass der
    /// bestehende <see cref="CommandInput"/> sie ganz normal auswertet —
    /// keine Parallelwelt, kein doppeltes Moveset.
    /// </summary>
    public class TouchGestureDetector : MonoBehaviour
    {
        public static TouchGestureDetector Instance { get; private set; }

        [Header("Erkennung")]
        public int playerIndex = 0;
        [Tooltip("Ab welcher Auslenkung eine Richtung als gewischt gilt.")]
        public float directionThreshold = 0.55f;
        [Tooltip("Maximale Dauer einer Gestenfolge.")]
        public float gestureWindow = 0.6f;

        [Header("Wiedergabe")]
        [Tooltip("Frames, die jede Richtung der Sequenz gehalten wird.")]
        public int framesPerStep = 3;

        [Header("Feedback")]
        public bool showFeedback = true;

        /// <summary>Gesten-Kurzschrift → Numpad-Sequenz (siehe MoveCatalog).</summary>
        private static readonly Dictionary<string, int[]> patterns = new Dictionary<string, int[]>
        {
            { "↓↘→",    new[] { 2, 3, 6 } },     // Hadouken-Motion
            { "↓↙←",    new[] { 2, 1, 4 } },     // gespiegelt
            { "→↘↓↙←",  new[] { 6, 3, 2, 1, 4 } },
            { "←↙↓↘→",  new[] { 4, 1, 2, 3, 6 } },
            { "→→",     new[] { 6, 5, 6 } },     // Dash vorwärts
            { "←←",     new[] { 4, 5, 4 } },     // Dash rückwärts
            { "↓↓",     new[] { 2, 5, 2 } },
            { "↑↑",     new[] { 8, 5, 8 } }
        };

        private readonly List<int> swipeBuffer = new List<int>();
        private string shorthand = "";
        private float gestureStart = -1f;
        private bool replaying;

        public static TouchGestureDetector Ensure()
        {
            if (Instance == null)
            {
                var go = new GameObject("~TouchGestures");
                Instance = go.AddComponent<TouchGestureDetector>();
            }
            return Instance;
        }

        void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(this); return; }
        }

        void Update()
        {
            if (replaying) return;

            Vector2 axis = VirtualInput.GetAxis(playerIndex);

            if (axis.magnitude >= directionThreshold)
            {
                if (gestureStart < 0f) gestureStart = Time.time;

                int numpad = ToNumpad(axis);
                string symbol = ToSymbol(axis);
                if (swipeBuffer.Count == 0 || swipeBuffer[swipeBuffer.Count - 1] != numpad)
                {
                    swipeBuffer.Add(numpad);
                    shorthand += symbol;
                    if (swipeBuffer.Count > 6) { swipeBuffer.RemoveAt(0); shorthand = shorthand.Substring(1); }
                }
            }
            else if (gestureStart >= 0f)
            {
                Evaluate();
            }

            if (gestureStart >= 0f && Time.time - gestureStart > gestureWindow)
                Reset();
        }

        void Evaluate()
        {
            foreach (var kv in patterns)
            {
                if (!shorthand.EndsWith(kv.Key)) continue;
                if (showFeedback)
                    FloatingText.Show(FeedbackPosition(), kv.Key, PennerPalette.NeonBlue, 0.7f);
                StartCoroutine(Replay(kv.Value));
                Reset();
                return;
            }
            Reset();
        }

        /// <summary>Spielt die Numpad-Sequenz als echte Achseneingabe nach.</summary>
        IEnumerator Replay(int[] sequence)
        {
            replaying = true;
            foreach (int dir in sequence)
            {
                VirtualInput.SetAxis(playerIndex, FromNumpad(dir));
                for (int f = 0; f < framesPerStep; f++) yield return null;
            }
            VirtualInput.SetAxis(playerIndex, Vector2.zero);
            replaying = false;
        }

        void Reset()
        {
            swipeBuffer.Clear();
            shorthand = "";
            gestureStart = -1f;
        }

        Vector3 FeedbackPosition()
        {
            var gm = GameManager.Instance;
            var f = gm != null ? gm.Player1 : null;
            return f != null ? f.transform.position + Vector3.up * 2.6f : Vector3.up * 2f;
        }

        // --- Richtungs-Helfer (Numpad-Notation wie im MoveCatalog) ---
        public static int ToNumpad(Vector2 axis)
        {
            float a = Mathf.Atan2(axis.y, axis.x) * Mathf.Rad2Deg;
            if (a < 0f) a += 360f;
            if (a >= 337.5f || a < 22.5f) return 6;
            if (a < 67.5f) return 9;
            if (a < 112.5f) return 8;
            if (a < 157.5f) return 7;
            if (a < 202.5f) return 4;
            if (a < 247.5f) return 1;
            if (a < 292.5f) return 2;
            return 3;
        }

        public static Vector2 FromNumpad(int dir)
        {
            switch (dir)
            {
                case 1: return new Vector2(-0.7f, -0.7f);
                case 2: return new Vector2(0f, -1f);
                case 3: return new Vector2(0.7f, -0.7f);
                case 4: return new Vector2(-1f, 0f);
                case 6: return new Vector2(1f, 0f);
                case 7: return new Vector2(-0.7f, 0.7f);
                case 8: return new Vector2(0f, 1f);
                case 9: return new Vector2(0.7f, 0.7f);
                default: return Vector2.zero;
            }
        }

        static string ToSymbol(Vector2 axis)
        {
            switch (ToNumpad(axis))
            {
                case 1: return "↙";
                case 2: return "↓";
                case 3: return "↘";
                case 4: return "←";
                case 6: return "→";
                case 7: return "↖";
                case 8: return "↑";
                case 9: return "↗";
                default: return "";
            }
        }
    }
}
