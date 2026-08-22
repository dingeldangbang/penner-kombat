using UnityEngine;

namespace PennerKombat
{
    /// <summary>
    /// Einzelner Charakter-Datensatz als ScriptableObject. Wird vom
    /// FighterDatabase (Liste) referenziert und liefert alle
    /// Balance-/Anzeige-Werte eines Kämpfers.
    /// </summary>
    [CreateAssetMenu(fileName = "FighterConfig", menuName = "PennerKombat/FighterConfig")]
    public class FighterConfig : ScriptableObject
    {
        [Header("Identität")]
        public string id = GameConstants.CharLeBinde;
        public string displayName = "Le Binde";
        public GameObject prefab;
        public Sprite portrait;
        public Sprite portraitSelect;

        [Header("Statur")]
        [Tooltip("Körperbau — steuert Kapselmaße, Masse und Hitbox-Größe beim Auto-Setup.")]
        public Stature stature = Stature.Normal;

        [Header("Balance")]
        public float maxHP = 100f;
        public float moveSpeed = 5f;
        public float lightDamage = 8f;
        public float heavyDamage = 15f;
        public float attackRange = 2.5f;

        [Header("Optik")]
        public Color primaryColor = Color.white;
        public Color secondaryColor = Color.gray;

        [Header("Audio")]
        public AudioClip introVoice;
        public AudioClip victoryVoice;
        public AudioClip defeatVoice;

        [Header("Freischaltung")]
        public int unlockLevel;
        public bool isDLC;
        public string dlcId = "";

        [Header("Spezialbewegungen")]
        public string[] specialNames;
    }

    /// <summary>Körperbau eines Kämpfers (docs/DESIGN.md).</summary>
    public enum Stature { Hager, Normal, Breit, Adipoes }

    /// <summary>Maße und Gewicht je Statur — eine Quelle für alle Systeme.</summary>
    public static class StatureTable
    {
        /// <summary>Höhe in Metern.</summary>
        public static float Height(Stature s)
        {
            switch (s)
            {
                case Stature.Hager:   return 1.90f;
                case Stature.Breit:   return 1.82f;
                case Stature.Adipoes: return 1.74f;
                default:              return 1.80f;
            }
        }

        /// <summary>Kapselradius in Metern.</summary>
        public static float Radius(Stature s)
        {
            switch (s)
            {
                case Stature.Hager:   return 0.33f;
                case Stature.Breit:   return 0.52f;
                case Stature.Adipoes: return 0.62f;
                default:              return 0.40f;
            }
        }

        /// <summary>Rigidbody-Masse.</summary>
        public static float Mass(Stature s)
        {
            switch (s)
            {
                case Stature.Hager:   return 0.85f;
                case Stature.Breit:   return 1.25f;
                case Stature.Adipoes: return 1.45f;
                default:              return 1f;
            }
        }

        /// <summary>Faktor für die Angriffs-Hitbox (breitere Leute treffen breiter).</summary>
        public static float HitboxScale(Stature s)
        {
            switch (s)
            {
                case Stature.Hager:   return 0.85f;
                case Stature.Breit:   return 1.2f;
                case Stature.Adipoes: return 1.35f;
                default:              return 1f;
            }
        }
    }
}
