using System.Collections.Generic;
using UnityEngine;

namespace PennerKombat
{
    /// <summary>
    /// (3) Zentrale Touch-Steuerung — docs/TOUCH.md §2.3.
    /// Bündelt Joystick, Buttons, Gesten, Anti-Ghosting und Eingabepuffer,
    /// erkennt kombinierte Eingaben (Block + Spezial = EX) und blendet die
    /// Steuerung bei Pause/Cinematics aus.
    ///
    /// Die eigentliche Weitergabe ans Spiel läuft über <see cref="VirtualInput"/>,
    /// das <see cref="FighterInput"/> mit Tastatur und Gamepad verodert.
    /// </summary>
    public class TouchInputManager : MonoBehaviour
    {
        public static TouchInputManager Instance { get; private set; }

        [Header("Bedienelemente")]
        public VirtualJoystick joystick;
        public List<TouchButton> buttons = new List<TouchButton>();

        [Header("Verhalten")]
        public int playerIndex = 0;
        [Tooltip("Block + Spezial löst die EX-Version aus.")]
        public bool enableExCombo = true;

        public static TouchInputManager Ensure()
        {
            if (Instance == null)
            {
                var go = new GameObject("~TouchInputManager");
                Instance = go.AddComponent<TouchInputManager>();
            }
            return Instance;
        }

        void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(this); return; }

            AntiGhosting.Ensure();
            InputBuffer.Ensure();
        }

        public void Register(VirtualJoystick stick, IEnumerable<TouchButton> btns)
        {
            joystick = stick;
            buttons.Clear();
            if (btns != null) buttons.AddRange(btns);
            ApplySettings(TouchSettings.Current);
        }

        void Update()
        {
            if (enableExCombo) DetectExCombo();
        }

        /// <summary>Block gehalten + Spezialtaste = EX-Version (Spec §2.3 „DetectCombos").</summary>
        void DetectExCombo()
        {
            bool blocking = VirtualInput.IsHeld(playerIndex, VButton.Block);
            if (!blocking) return;

            if (VirtualInput.WasPressed(playerIndex, VButton.Special1) ||
                VirtualInput.WasPressed(playerIndex, VButton.Special2))
            {
                var fighter = FindFighter();
                if (fighter == null) return;
                // X-Ray/EX über die vorhandene Fatal-Blow-Leiste auslösen
                VirtualInput.Press(playerIndex, VButton.FatalBlow);
                if (TouchSettings.Current.vibration) Haptics.Tap(0.08f);
            }
        }

        FighterController FindFighter()
        {
            var gm = GameManager.Instance;
            if (gm == null) return null;
            return playerIndex == 0 ? gm.Player1 : gm.Player2;
        }

        // --- Abfrage-API (z. B. für HUD oder Tutorial) ---
        public Vector2 MoveInput => VirtualInput.GetAxis(playerIndex);
        public bool GetButton(VButton b) => VirtualInput.IsHeld(playerIndex, b);
        public bool GetButtonDown(VButton b) => VirtualInput.WasPressed(playerIndex, b);

        // --- Sichtbarkeit ---
        public void SetVisible(bool visible)
        {
            if (TouchControls.Instance != null)
                TouchControls.Instance.SetVisible(visible);
            if (!visible) VirtualInput.Clear();
        }

        // --- Einstellungen anwenden ---
        public void ApplySettings(TouchSettings s)
        {
            if (s == null) return;
            joystick?.ApplySettings(s);
            foreach (var b in buttons) b?.ApplySettings(s);
            if (InputBuffer.Instance != null) InputBuffer.Instance.bufferWindow = s.bufferWindow;
            if (TouchGestureDetector.Instance != null)
                TouchGestureDetector.Instance.showFeedback = s.gestureFeedback;
        }
    }
}
