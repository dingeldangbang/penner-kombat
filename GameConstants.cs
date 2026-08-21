using UnityEngine;

namespace PennerKombat
{
    /// <summary>
    /// Zentrale Konstanten, Enums und statische Helfer für Penner Kombat.
    /// Alle Teilsysteme referenzieren diese Werte, damit Balance-Daten
    /// an einer Stelle gepflegt werden können.
    /// </summary>
    public static class GameConstants
    {
        // --- Runden / Match ---
        public const int DefaultBestOfRounds = 3;
        public const float DefaultRoundTime = 99f;
        public const float RoundEndDelay = 2.2f;
        public const float RoundStartDelay = 1.2f;

        // --- Basis-Kampfwerte ---
        public const float BlockDamageReduction = 0.22f;   // 78% Reduktion
        public const float BlockMoveScale = 0.35f;
        public const float ComboWindow = 2.0f;
        public const float DefaultKnockback = 8f;
        public const float DefaultKnockbackUp = 3f;
        public const float DefaultGravity = 26f;
        public const float DefaultJumpForce = 9.5f;

        // --- Fatal Blow (X-Ray) ---
        public const float FatalBlowMeterMax = 100f;
        public const float FatalBlowDamageMin = 28f;
        public const float FatalBlowDamageMax = 44f;
        public const float FatalBlowChargePerHit = 12f;
        public const float FatalBlowChargePerHitTaken = 8f;

        // --- Tags / Layer ---
        public const string TagGround = "Ground";
        public const string TagFighter = "Fighter";
        public const string TagInteractable = "Interactable";
        public const string TagProjectile = "Projectile";

        // --- Offizielle Charakter-IDs (für Datenbank + Audio-Mapping) ---
        public const string CharLeBinde = "le_binde";
        public const string CharMell = "mell";
        public const string CharMojoBob = "mojo_bob";
        public const string CharDieter = "dieter";
        public const string CharUschi = "uschi";
        public const string CharTetraPak = "tetrapak";
        public const string CharSigi = "sigi";
        public const string CharRolf = "rolf";
        public const string CharKalle = "kalle";

        public static readonly string[] AllCharacterIds =
        {
            CharLeBinde, CharMell, CharMojoBob, CharDieter, CharUschi,
            CharTetraPak, CharSigi, CharRolf, CharKalle
        };
    }

    /// <summary>Auswahl-Raritäten für Trophäen.</summary>
    public enum TrophyRarity { Bronze, Silver, Gold, Platinum }

    /// <summary>Grobzustand für die KI / Debug-UI.</summary>
    public enum FighterMood { Neutral, Aggressive, Defensive, Retreating }
}
