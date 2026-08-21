using UnityEngine;
using TMPro;

namespace PennerKombat
{
    /// <summary>
    /// Ermittelt am Story-Ende welches der 4 Enden (A-D) erreicht wurde.
    /// Enden werden über Story-Flags aus Entscheidungen bestimmt.
    /// </summary>
    public class EndingSystem : MonoBehaviour
    {
        public static EndingSystem Instance;

        [Header("UI")]
        public GameObject endingPanel;
        public TextMeshProUGUI endingTitle;
        public TextMeshProUGUI endingText;

        void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(this); return; }
            if (endingPanel != null) endingPanel.SetActive(false);
        }

        public void ResolveAndShow()
        {
            var sm = StoryManager.Instance;
            if (sm == null) return;

            // Ende D "LEERGUT LIBRE" (Le Binde verteilt Suppe an alle)
            if (sm.HasFlag("choice_leergut_libre") || sm.HasFlag("leergut_libre"))
            {
                ShowEnding("D — LEERGUT LIBRE",
                    "Le Binde stellt vor 48 Kameras einen Topf Suppe in die Arena. Alle essen. Auch Pfandmaster Max nimmt einen Teller. Der Tag endet nicht mit Blut, sondern mit Resten.");
                return;
            }

            // Ende C — Pfand-Sieg
            if (sm.HasFlag("choice_pfand"))
            {
                ShowEnding("C — PFANDTIGER",
                    "Die Arena wird zum Großkampftag des Pfandsystems. Alle neun verbünden sich gegen den Automaten.");
                return;
            }

            // Ende B — Mell rettet
            if (sm.HasFlag("choice_retten"))
            {
                ShowEnding("B — SCHICHTENDE",
                    "Mell versorgt die Wunden aller, wie immer. Ihre Hände zittern erst, als niemand mehr hinsieht.");
                return;
            }

            // Ende A — Standard
            ShowEnding("A — LEERES GLÜCK",
                "Der letzte Kampf ist entschieden. Der Hof liegt still. Morgen geht es weiter — anders.");
        }

        void ShowEnding(string title, string text)
        {
            if (endingPanel == null) return;
            endingPanel.SetActive(true);
            if (endingTitle != null) endingTitle.text = title;
            if (endingText != null) endingText.text = text;
        }

        public void Hide() { if (endingPanel != null) endingPanel.SetActive(false); }
    }
}
