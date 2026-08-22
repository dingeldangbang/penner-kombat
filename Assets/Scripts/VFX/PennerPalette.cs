using UnityEngine;

namespace PennerKombat
{
    /// <summary>
    /// Zentrale Farb- und Look-Definition — abgeleitet vom Cover-Artwork
    /// („Zum Blauen Eimer", Hinterhof bei Nacht, warmes Laternenlicht).
    /// Alle VFX-, HUD- und Arena-Systeme ziehen ihre Farben ausschließlich hier.
    /// </summary>
    public static class PennerPalette
    {
        // --- Grundpalette (Hex laut Visual-Spec, docs/VISUALS.md) ---
        public static readonly Color BloodRed   = Hex("8B0000");   // Titel, HP, Blut
        public static readonly Color NightBlue  = Hex("1A1C2A");   // Hinterhof-Nacht
        public static readonly Color WarmOrange = Hex("FF6B00");   // Laterne, Treffer
        public static readonly Color Gold       = Hex("FFD700");   // Mojo Bob, Krit, Sieg
        public static readonly Color NeonBlue   = Hex("00BFFF");   // Mell, Sigi, Elektro
        public static readonly Color Earth      = Hex("8B4513");   // Holz, Bierkästen
        public static readonly Color PoisonGrn  = Hex("228B22");   // TetraPak, Rattengift
        public static readonly Color Pure       = Color.white;

        // --- Charakter-Signaturfarben (Aura, Trails, Treffer-Tint) ---
        public static Color ForCharacter(string fighterId)
        {
            switch (fighterId)
            {
                case GameConstants.CharLeBinde:  return BloodRed;
                case GameConstants.CharMell:     return NeonBlue;
                case GameConstants.CharMojoBob:  return Gold;
                case GameConstants.CharDieter:   return WarmOrange;
                case GameConstants.CharUschi:    return Hex("C86432");
                case GameConstants.CharTetraPak: return PoisonGrn;
                case GameConstants.CharSigi:     return Hex("39FF14");
                case GameConstants.CharRolf:     return Hex("6B8E23");
                case GameConstants.CharKalle:    return Earth;
                default:                         return WarmOrange;
            }
        }

        /// <summary>Combo-Farbe laut Spec: 1–4 weiß, 5–9 gelb, 10+ rot.</summary>
        public static Color ForCombo(int combo)
        {
            if (combo >= 10) return Hex("FF2A2A");
            if (combo >= 5)  return Gold;
            return Pure;
        }

        public static Color Hex(string rgb)
        {
            return new Color(
                int.Parse(rgb.Substring(0, 2), System.Globalization.NumberStyles.HexNumber) / 255f,
                int.Parse(rgb.Substring(2, 2), System.Globalization.NumberStyles.HexNumber) / 255f,
                int.Parse(rgb.Substring(4, 2), System.Globalization.NumberStyles.HexNumber) / 255f,
                1f);
        }

        public static Color WithAlpha(this Color c, float a) => new Color(c.r, c.g, c.b, a);
    }

    /// <summary>Trefferklassen — steuern Funkenmenge, Shake, Sound und Hitstop.</summary>
    public enum HitTier
    {
        Light,      // □  kleiner Funke, kein Shake
        Heavy,      // △  gelber Funke + Blut, 2 px Shake
        Special,    // ○  Charakterfarbe, 5 px Shake
        Ex,         // R1+○ Schockwellenring, 8 px Shake
        Critical,   // ⭐ Gold + Frame-Freeze
        FatalBlow,  // X-Ray
        Fatality    // Blutfontäne
    }
}
