using UnityEngine;
using UnityEngine.InputSystem;

namespace PennerKombat
{
    /// <summary>
    /// Abstraktionsschicht für Eingaben, getrennt pro Spieler-Index:
    /// Tastatur (P1: WASD+JKL, P2: Pfeiltasten+Nummernblock), Gamepad
    /// (jeweils Pad #1 / #2) und Touch (<see cref="VirtualInput"/>).
    /// Alle Quellen werden verodert — der Kampfcode kennt nur die Aktion.
    ///
    /// Belegung und Umbelegung: docs/CONTROLS.md
    /// </summary>
    public class FighterInput : MonoBehaviour
    {
        public static FighterInput Instance;

        [Header("Spieler 1 — Tastatur (WASD)")]
        public Key p1Up = Key.W, p1Down = Key.S, p1Left = Key.A, p1Right = Key.D;
        public Key p1Light = Key.J, p1Heavy = Key.K;
        public Key p1Block = Key.LeftShift, p1Jump = Key.Space;
        public Key p1Special1 = Key.U, p1Special2 = Key.I;
        public Key p1Ex = Key.O, p1Interact = Key.E;
        public Key p1FatalBlow = Key.Y, p1Med = Key.H;

        [Header("Spieler 2 — Tastatur (Pfeile + Nummernblock)")]
        public Key p2Up = Key.UpArrow, p2Down = Key.DownArrow, p2Left = Key.LeftArrow, p2Right = Key.RightArrow;
        public Key p2Light = Key.Numpad1, p2Heavy = Key.Numpad2;
        public Key p2Block = Key.Numpad3, p2Jump = Key.Numpad0;
        public Key p2Special1 = Key.Numpad4, p2Special2 = Key.Numpad5;
        public Key p2Ex = Key.Numpad6, p2Interact = Key.NumpadPeriod;
        public Key p2FatalBlow = Key.NumpadPlus, p2Med = Key.NumpadEnter;

        [Header("Gamepad")]
        public float deadZone = 0.22f;

        void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(this); return; }
            KeyBindings.LoadInto(this);
        }

        // ==================================================================
        //  Richtung
        // ==================================================================
        public Vector3 GetMoveDirection(int playerIndex)
        {
            // Tastatur
            if (Keyboard.current != null)
            {
                float h = 0f, v = 0f;
                if (playerIndex == 0)
                {
                    if (Keyboard.current[p1Right].isPressed) h = 1;
                    if (Keyboard.current[p1Left].isPressed) h = -1;
                    if (Keyboard.current[p1Up].isPressed) v = 1;
                    if (Keyboard.current[p1Down].isPressed) v = -1;
                }
                else if (playerIndex == 1)
                {
                    if (Keyboard.current[p2Right].isPressed) h = 1;
                    if (Keyboard.current[p2Left].isPressed) h = -1;
                    if (Keyboard.current[p2Up].isPressed) v = 1;
                    if (Keyboard.current[p2Down].isPressed) v = -1;
                }
                Vector3 kbd = new Vector3(h, 0, v).normalized;
                if (kbd != Vector3.zero) return kbd;
            }

            // Touch / On-Screen-Stick
            Vector2 touch = VirtualInput.GetAxis(playerIndex);
            if (touch.sqrMagnitude > 0.0001f)
                return new Vector3(touch.x, 0f, touch.y);

            // Gamepad: Spieler 1 = Pad #1, Spieler 2 = Pad #2
            var gamepads = Gamepad.all;
            if (gamepads != null && playerIndex < gamepads.Count)
            {
                Vector2 stick = gamepads[playerIndex].leftStick.ReadValue();
                if (stick.magnitude > deadZone)
                    return new Vector3(stick.x, 0, stick.y).normalized;
            }
            return Vector3.zero;
        }

        // ==================================================================
        //  Aktionen
        // ==================================================================
        public bool GetLightAttack(int p) =>
            KeyDown(p, p1Light, p2Light) || Pad(p, gp => gp.buttonSouth.wasPressedThisFrame)
            || VirtualInput.WasPressed(p, VButton.Light);

        public bool GetHeavyAttack(int p) =>
            KeyDown(p, p1Heavy, p2Heavy) || Pad(p, gp => gp.buttonEast.wasPressedThisFrame)
            || VirtualInput.WasPressed(p, VButton.Heavy);

        public bool GetJump(int p) =>
            KeyDown(p, p1Jump, p2Jump) || Pad(p, gp => gp.buttonNorth.wasPressedThisFrame)
            || VirtualInput.WasPressed(p, VButton.Jump);

        public bool GetBlock(int p) =>
            KeyHeld(p, p1Block, p2Block)
            || Pad(p, gp => gp.leftShoulder.isPressed || gp.rightShoulder.isPressed)
            || VirtualInput.IsHeld(p, VButton.Block);

        public bool GetSpecial1(int p) =>
            KeyDown(p, p1Special1, p2Special1) || Pad(p, gp => gp.buttonWest.wasPressedThisFrame)
            || VirtualInput.WasPressed(p, VButton.Special1);

        public bool GetSpecial2(int p) =>
            KeyDown(p, p1Special2, p2Special2) || Pad(p, gp => gp.rightShoulder.wasPressedThisFrame)
            || VirtualInput.WasPressed(p, VButton.Special2);

        /// <summary>EX-Version einer Spezialbewegung (verstärkt, kostet Leiste).</summary>
        public bool GetEx(int p) =>
            KeyDown(p, p1Ex, p2Ex) || Pad(p, gp => gp.rightTrigger.wasPressedThisFrame)
            || VirtualInput.WasPressed(p, VButton.Ex);

        /// <summary>Arena-Objekt aufheben/benutzen.</summary>
        public bool GetInteract(int p) =>
            KeyDown(p, p1Interact, p2Interact) || Pad(p, gp => gp.leftTrigger.wasPressedThisFrame)
            || VirtualInput.WasPressed(p, VButton.Interact);

        public bool GetFatalBlow(int p) =>
            KeyDown(p, p1FatalBlow, p2FatalBlow)
            || Pad(p, gp => gp.buttonWest.wasPressedThisFrame && gp.buttonEast.wasPressedThisFrame)
            || VirtualInput.WasPressed(p, VButton.FatalBlow);

        /// <summary>Med-Kapsel (siehe MedSystem).</summary>
        public bool GetMed(int p) =>
            KeyDown(p, p1Med, p2Med) || Pad(p, gp => gp.dpad.up.wasPressedThisFrame)
            || VirtualInput.WasPressed(p, VButton.Med);

        // ==================================================================
        //  Helfer
        // ==================================================================
        bool KeyDown(int p, Key k1, Key k2)
        {
            if (Keyboard.current == null) return false;
            Key k = p == 0 ? k1 : p == 1 ? k2 : Key.None;
            return k != Key.None && Keyboard.current[k].wasPressedThisFrame;
        }

        bool KeyHeld(int p, Key k1, Key k2)
        {
            if (Keyboard.current == null) return false;
            Key k = p == 0 ? k1 : p == 1 ? k2 : Key.None;
            return k != Key.None && Keyboard.current[k].isPressed;
        }

        bool Pad(int p, System.Func<Gamepad, bool> test)
        {
            var gps = Gamepad.all;
            if (gps != null && p >= 0 && p < gps.Count) return test(gps[p]);
            return false;
        }

        /// <summary>Löscht die Einmal-Flags der Touch-Eingabe am Frame-Ende.</summary>
        void LateUpdate() => VirtualInput.EndFrame();

        /// <summary>Aktuelle Belegung als Text (für die Layout-Anzeige).</summary>
        public Key GetBinding(int player, string action)
        {
            bool p1 = player == 0;
            switch (action)
            {
                case "Up":       return p1 ? p1Up : p2Up;
                case "Down":     return p1 ? p1Down : p2Down;
                case "Left":     return p1 ? p1Left : p2Left;
                case "Right":    return p1 ? p1Right : p2Right;
                case "Light":    return p1 ? p1Light : p2Light;
                case "Heavy":    return p1 ? p1Heavy : p2Heavy;
                case "Block":    return p1 ? p1Block : p2Block;
                case "Jump":     return p1 ? p1Jump : p2Jump;
                case "Special1": return p1 ? p1Special1 : p2Special1;
                case "Special2": return p1 ? p1Special2 : p2Special2;
                case "Ex":       return p1 ? p1Ex : p2Ex;
                case "Interact": return p1 ? p1Interact : p2Interact;
                case "FatalBlow":return p1 ? p1FatalBlow : p2FatalBlow;
                case "Med":      return p1 ? p1Med : p2Med;
                default:         return Key.None;
            }
        }

        public void SetBinding(int player, string action, Key key)
        {
            SetBindingSilent(player, action, key);
            KeyBindings.SaveFrom(this);
        }

        /// <summary>Belegung setzen, ohne sofort zu speichern (Ladevorgang).</summary>
        public void SetBindingSilent(int player, string action, Key key)
        {
            bool p1 = player == 0;
            switch (action)
            {
                case "Up":        if (p1) p1Up = key; else p2Up = key; break;
                case "Down":      if (p1) p1Down = key; else p2Down = key; break;
                case "Left":      if (p1) p1Left = key; else p2Left = key; break;
                case "Right":     if (p1) p1Right = key; else p2Right = key; break;
                case "Light":     if (p1) p1Light = key; else p2Light = key; break;
                case "Heavy":     if (p1) p1Heavy = key; else p2Heavy = key; break;
                case "Block":     if (p1) p1Block = key; else p2Block = key; break;
                case "Jump":      if (p1) p1Jump = key; else p2Jump = key; break;
                case "Special1":  if (p1) p1Special1 = key; else p2Special1 = key; break;
                case "Special2":  if (p1) p1Special2 = key; else p2Special2 = key; break;
                case "Ex":        if (p1) p1Ex = key; else p2Ex = key; break;
                case "Interact":  if (p1) p1Interact = key; else p2Interact = key; break;
                case "FatalBlow": if (p1) p1FatalBlow = key; else p2FatalBlow = key; break;
                case "Med":       if (p1) p1Med = key; else p2Med = key; break;
            }
        }

        public static readonly string[] Actions =
        {
            "Up", "Down", "Left", "Right", "Light", "Heavy", "Block", "Jump",
            "Special1", "Special2", "Ex", "Interact", "FatalBlow", "Med"
        };
    }

    /// <summary>Persistenz der Tastenbelegung (PlayerPrefs, pro Aktion ein Eintrag).</summary>
    public static class KeyBindings
    {
        const string Prefix = "pk_key_";

        public static void SaveFrom(FighterInput input)
        {
            for (int p = 0; p < 2; p++)
                foreach (var a in FighterInput.Actions)
                    PlayerPrefs.SetInt($"{Prefix}{p}_{a}", (int)input.GetBinding(p, a));
            PlayerPrefs.Save();
        }

        public static void LoadInto(FighterInput input)
        {
            for (int p = 0; p < 2; p++)
                foreach (var a in FighterInput.Actions)
                {
                    string key = $"{Prefix}{p}_{a}";
                    if (!PlayerPrefs.HasKey(key)) continue;
                    input.SetBindingSilent(p, a, (Key)PlayerPrefs.GetInt(key));
                }
        }

        public static void Reset()
        {
            for (int p = 0; p < 2; p++)
                foreach (var a in FighterInput.Actions)
                    PlayerPrefs.DeleteKey($"{Prefix}{p}_{a}");
            PlayerPrefs.Save();
        }
    }
}
