using UnityEngine;

namespace PennerKombat
{
    /// <summary>Virtuelle Tasten der Touch-/On-Screen-Steuerung.</summary>
    public enum VButton { Light, Heavy, Jump, Block, Special1, Special2, FatalBlow }

    /// <summary>
    /// Eingabequelle für Touch- und On-Screen-Controls. <see cref="FighterInput"/>
    /// verodert diese Werte mit Tastatur und Gamepad, sodass Charaktere und KI
    /// nichts von der Eingabeart wissen müssen.
    ///
    /// Tastendrücke haben „wasPressedThisFrame"-Semantik: sie werden am Ende des
    /// Frames von <see cref="FighterInput"/> wieder gelöscht.
    /// </summary>
    public static class VirtualInput
    {
        public const int MaxPlayers = 2;
        private const int ButtonCount = 7;

        private static readonly Vector2[] axis = new Vector2[MaxPlayers];
        private static readonly bool[,] pressed = new bool[MaxPlayers, ButtonCount];
        private static readonly bool[,] held = new bool[MaxPlayers, ButtonCount];

        /// <summary>True, sobald irgendein Touch-Control aktiv ist (für HUD-Layout).</summary>
        public static bool Active { get; set; }

        public static void SetAxis(int player, Vector2 value)
        {
            if (!InRange(player)) return;
            axis[player] = value;
            Active = true;
        }

        public static Vector2 GetAxis(int player) => InRange(player) ? axis[player] : Vector2.zero;

        public static void Press(int player, VButton button)
        {
            if (!InRange(player)) return;
            pressed[player, (int)button] = true;
            held[player, (int)button] = true;
            Active = true;
        }

        public static void Release(int player, VButton button)
        {
            if (!InRange(player)) return;
            held[player, (int)button] = false;
        }

        public static bool WasPressed(int player, VButton button)
            => InRange(player) && pressed[player, (int)button];

        public static bool IsHeld(int player, VButton button)
            => InRange(player) && held[player, (int)button];

        /// <summary>Löscht die Einmal-Flags — wird von FighterInput in LateUpdate gerufen.</summary>
        public static void EndFrame()
        {
            for (int p = 0; p < MaxPlayers; p++)
                for (int b = 0; b < ButtonCount; b++)
                    pressed[p, b] = false;
        }

        /// <summary>Setzt alles zurück (Szenenwechsel, Pause).</summary>
        public static void Clear()
        {
            for (int p = 0; p < MaxPlayers; p++)
            {
                axis[p] = Vector2.zero;
                for (int b = 0; b < ButtonCount; b++)
                {
                    pressed[p, b] = false;
                    held[p, b] = false;
                }
            }
        }

        static bool InRange(int player) => player >= 0 && player < MaxPlayers;
    }
}
