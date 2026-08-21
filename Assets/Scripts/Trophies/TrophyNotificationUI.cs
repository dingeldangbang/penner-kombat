using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PennerKombat
{
    /// <summary>
    /// Kurz-Einblendung, wenn eine Trophäe freigeschaltet wird.
    /// Zeigt Name und Rarität-Farbe.
    /// </summary>
    public class TrophyNotificationUI : MonoBehaviour
    {
        [Header("UI")]
        public Image iconImage;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI rarityText;
        public Image rarityBackground;

        [Header("Farben")]
        public Color bronze = new Color(0.8f, 0.5f, 0.2f);
        public Color silver = new Color(0.75f, 0.75f, 0.75f);
        public Color gold = new Color(1f, 0.84f, 0f);
        public Color platinum = new Color(0.8f, 0.6f, 1f);

        public void Show(string displayName, TrophyRarity rarity)
        {
            if (nameText != null) nameText.text = displayName;
            string rarityStr;
            Color color;
            switch (rarity)
            {
                case TrophyRarity.Bronze: rarityStr = "BRONZE"; color = bronze; break;
                case TrophyRarity.Silver: rarityStr = "SILBER"; color = silver; break;
                case TrophyRarity.Gold: rarityStr = "GOLD"; color = gold; break;
                default: rarityStr = "PLATIN"; color = platinum; break;
            }
            if (rarityText != null) { rarityText.text = rarityStr; rarityText.color = color; }
            if (rarityBackground != null) rarityBackground.color = color;
            // UI-Animator könnte hier einen "Show"-Trigger bekommen
        }
    }
}
