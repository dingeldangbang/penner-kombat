using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PennerKombat
{
    /// <summary>
    /// Charakterauswahl-Bildschirm für Versus. Beide Spieler wählen je einen
    /// Charakter; sobald beide "BEREIT" sind, startet der Kampf.
    /// </summary>
    public class CharacterSelectUI : MonoBehaviour
    {
        [Header("UI")]
        public GameObject characterSlotPrefab;
        public Transform player1Container;
        public Transform player2Container;
        public Button readyButtonP1;
        public Button readyButtonP2;
        public TextMeshProUGUI name1;
        public TextMeshProUGUI name2;
        public Image portrait1;
        public Image portrait2;

        [Header("Data")]
        public FighterDatabase database;

        private int selectedP1;
        private int selectedP2;
        private bool readyP1;
        private bool readyP2;
        private readonly List<CharacterSlotUI> slotsP1 = new List<CharacterSlotUI>();
        private readonly List<CharacterSlotUI> slotsP2 = new List<CharacterSlotUI>();

        void Start()
        {
            if (database != null) database.EnsureDefaultRoster();
            BuildList();
            if (readyButtonP1 != null) readyButtonP1.onClick.AddListener(() => ToggleReady(1));
            if (readyButtonP2 != null) readyButtonP2.onClick.AddListener(() => ToggleReady(2));
            UpdateSelection();
        }

        void BuildList()
        {
            if (database == null || characterSlotPrefab == null) return;
            for (int i = 0; i < database.fighters.Count; i++)
            {
                int idx = i;
                if (player1Container != null)
                {
                    var s1 = Instantiate(characterSlotPrefab, player1Container).GetComponent<CharacterSlotUI>();
                    s1.SetData(database.fighters[i], idx, true);
                    s1.OnSelected += value => { selectedP1 = value; UpdateSelection(); };
                    slotsP1.Add(s1);
                }
                if (player2Container != null)
                {
                    var s2 = Instantiate(characterSlotPrefab, player2Container).GetComponent<CharacterSlotUI>();
                    s2.SetData(database.fighters[i], idx, false);
                    s2.OnSelected += value => { selectedP2 = value; UpdateSelection(); };
                    slotsP2.Add(s2);
                }
            }
        }

        void UpdateSelection()
        {
            var d1 = database.GetFighter(selectedP1);
            var d2 = database.GetFighter(selectedP2);
            if (d1 != null) { if (name1 != null) name1.text = d1.displayName; if (portrait1 != null) portrait1.sprite = d1.portraitSelect; }
            if (d2 != null) { if (name2 != null) name2.text = d2.displayName; if (portrait2 != null) portrait2.sprite = d2.portraitSelect; }

            foreach (var s in slotsP1) s.SetSelected(s.Index == selectedP1);
            foreach (var s in slotsP2) s.SetSelected(s.Index == selectedP2);
        }

        void ToggleReady(int player)
        {
            if (player == 1) { readyP1 = !readyP1; if (readyButtonP1 != null) readyButtonP1.GetComponentInChildren<TextMeshProUGUI>().text = readyP1 ? "BEREIT" : "BEREIT?"; }
            else { readyP2 = !readyP2; if (readyButtonP2 != null) readyButtonP2.GetComponentInChildren<TextMeshProUGUI>().text = readyP2 ? "BEREIT" : "BEREIT?"; }

            if (readyP1 && readyP2 && GameManager.Instance != null)
                GameManager.Instance.StartVersusFight(selectedP1, selectedP2);
        }
    }
}
