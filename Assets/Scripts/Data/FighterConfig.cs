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
}
