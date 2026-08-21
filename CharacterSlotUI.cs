using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PennerKombat
{
    /// <summary>
    /// Ein einzelner Charakter-Slot in der Auswahl. Zeigt Porträt, Name und
    /// Auswahl-Highlight; meldet Klicks über OnSelected zurück.
    /// </summary>
    public class CharacterSlotUI : MonoBehaviour
    {
        [Header("UI")]
        public Image portraitImage;
        public TextMeshProUGUI nameText;
        public Image highlightBorder;
        public GameObject lockIcon;

        public int Index { get; private set; }
        public event Action<int> OnSelected;

        private FighterConfig data;

        public void SetData(FighterConfig data, int index, bool isPlayer1)
        {
            this.data = data;
            Index = index;

            if (portraitImage != null && data.portrait != null) portraitImage.sprite = data.portrait;
            if (nameText != null) nameText.text = data.displayName;
            if (lockIcon != null) lockIcon.SetActive(data.isDLC);
            GetComponent<Image>().color = data.primaryColor;
        }

        public void SetSelected(bool selected)
        {
            if (highlightBorder != null) highlightBorder.gameObject.SetActive(selected);
        }

        public void OnPointerClick()
        {
            if (data != null && data.isDLC) return;
            OnSelected?.Invoke(Index);
        }
    }
}
