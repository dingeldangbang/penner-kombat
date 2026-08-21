using UnityEngine;
using UnityEngine.InputSystem;
using PennerKombat;

namespace PennerKombat
{
    /// <summary>
    /// Abstraktionsschicht für Eingaben (Tastatur + Gamepad), getrennt pro
    /// Spieler-Index. Basierend auf dem Unity Input System; legacy-Input wird
    /// nur als Fallback genutzt (entspricht "Both"-Modus).
    /// </summary>
    public class FighterInput : MonoBehaviour
    {
        public static FighterInput Instance;

        [Header("Player 1 (Tastatur)")]
        public Key moveUp = Key.W, moveDown = Key.S, moveLeft = Key.A, moveRight = Key.D;
        public Key lightAttack = Key.J, heavyAttack = Key.K;
        public Key block = Key.LeftShift, jump = Key.Space;
        public Key special1 = Key.U, special2 = Key.H, fatalBlow = Key.Y;

        [Header("Gamepad")]
        public float deadZone = 0.22f;

        void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(this); return; }
        }

        // ---- Richtungs-Eingabe für Spieler-Index ----
        public Vector3 GetMoveDirection(int playerIndex)
        {
            // Tastatur = Player 1
            if (playerIndex == 0)
            {
                float h = 0, v = 0;
                if (Keyboard.current != null)
                {
                    if (Keyboard.current[moveRight].isPressed) h = 1;
                    if (Keyboard.current[moveLeft].isPressed) h = -1;
                    if (Keyboard.current[moveUp].isPressed) v = 1;
                    if (Keyboard.current[moveDown].isPressed) v = -1;
                }
                Vector3 kbd = new Vector3(h, 0, v).normalized;
                if (kbd != Vector3.zero) return kbd;
            }

            // Gamepad: Spieler 1 = Gamepad #1, Spieler 2 = Gamepad #2
            int pad = playerIndex;
            var gamepads = Gamepad.all;
            if (gamepads != null && pad < gamepads.Count)
            {
                var gp = gamepads[pad];
                Vector2 stick = gp.leftStick.ReadValue();
                if (stick.magnitude > deadZone)
                    return new Vector3(stick.x, 0, stick.y).normalized;
            }
            return Vector3.zero;
        }

        public bool GetLightAttack(int p) =>
            (p == 0 && Keyboard.current != null && Keyboard.current[lightAttack].wasPressedThisFrame)
            || GamepadButton(p, gp => gp.buttonSouth.wasPressedThisFrame);

        public bool GetHeavyAttack(int p) =>
            (p == 0 && Keyboard.current != null && Keyboard.current[heavyAttack].wasPressedThisFrame)
            || GamepadButton(p, gp => gp.buttonEast.wasPressedThisFrame);

        public bool GetJump(int p) =>
            (p == 0 && Keyboard.current != null && Keyboard.current[jump].wasPressedThisFrame)
            || GamepadButton(p, gp => gp.buttonNorth.wasPressedThisFrame);

        public bool GetBlock(int p) =>
            (p == 0 && Keyboard.current != null && (Keyboard.current[block].isPressed))
            || GamepadHeld(p, gp => gp.leftShoulder.isPressed || gp.rightShoulder.isPressed);

        public bool GetSpecial1(int p) =>
            (p == 0 && Keyboard.current != null && Keyboard.current[special1].wasPressedThisFrame)
            || GamepadButton(p, gp => gp.buttonWest.wasPressedThisFrame);

        public bool GetSpecial2(int p) =>
            (p == 0 && Keyboard.current != null && Keyboard.current[special2].wasPressedThisFrame)
            || GamepadButton(p, gp => gp.rightShoulder.wasPressedThisFrame);

        public bool GetFatalBlow(int p) =>
            (p == 0 && Keyboard.current != null && Keyboard.current[fatalBlow].wasPressedThisFrame)
            || GamepadButton(p, gp => gp.buttonWest.wasPressedThisFrame && gp.buttonEast.wasPressedThisFrame);

        // ---- Helfer ----
        bool GamepadButton(int p, System.Func<Gamepad, bool> test)
        {
            var gps = Gamepad.all;
            if (gps != null && p < gps.Count) return test(gps[p]);
            return false;
        }

        bool GamepadHeld(int p, System.Func<Gamepad, bool> test)
        {
            var gps = Gamepad.all;
            if (gps != null && p < gps.Count) return test(gps[p]);
            return false;
        }
    }
}
