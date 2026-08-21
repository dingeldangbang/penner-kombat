using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace PennerKombat
{
    /// <summary>
    /// Tutorial mit 10 Lektionen. Jede Lektion erklärt eine Mechanik und gibt
    /// dem Spieler ein Ziel; über einen Fortschritts-Check kann der Spieler
    /// zur nächsten Lektion springen. Speichert den Fortschritt in PlayerPrefs.
    /// </summary>
    public class TutorialManager : MonoBehaviour
    {
        public static TutorialManager Instance;

        [Header("UI")]
        public GameObject tutorialPanel;
        public TextMeshProUGUI lessonTitle;
        public TextMeshProUGUI lessonDescription;
        public TextMeshProUGUI progressText;
        public GameObject nextButton;
        public GameObject prevButton;

        private int currentLesson;
        private readonly List<LessonData> lessons = new List<LessonData>();

        void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(this); return; }
            BuildLessons();
            currentLesson = PlayerPrefs.GetInt("tutorial_progress", 0);
        }

        void Start()
        {
            if (currentLesson >= lessons.Count) currentLesson = 0;
            ShowLesson(currentLesson);
        }

        void BuildLessons()
        {
            lessons.Add(new LessonData("1 · Bewegung", "Laufe mit WASD. Springe mit Leertaste. Ducken wird in einer späteren Version hinzugefügt."));
            lessons.Add(new LessonData("2 · Angriff", "Leichter Angriff = J · Schwerer Angriff = K."));
            lessons.Add(new LessonData("3 · Blocken", "Halte Shift zum Blocken. Kontere mit einem schnellen Angriff direkt nach dem Block."));
            lessons.Add(new LessonData("4 · Spezialbewegungen", "Führe Spezials mit Richtungs-Sequenzen + Button aus, z.B. ↓↘→ + J (Flaschenhals bei Le Binde)."));
            lessons.Add(new LessonData("5 · Kombos", "Kette leichte und schwere Angriffe, solange das Combo-Fenster aktiv ist."));
            lessons.Add(new LessonData("6 · EX-Moves", "Verstärkte Spezials: z.B. EX-Flaschenhals (↓↘→ + J bei voller Metergröße)."));
            lessons.Add(new LessonData("7 · X-Ray (Fatal Blow)", "Fülle die Fatal-Blow-Leiste und drücke Y (West+East am Gamepad) für den X-Ray."));
            lessons.Add(new LessonData("8 · Fatalities", "Übe Fatality-Eingaben (z.B. ←→←→△ bei Le Binde) am betäubten Gegner."));
            lessons.Add(new LessonData("9 · Brutalities", "Erfülle Brutality-Bedingungen, z.B. Runde mit Mops-Kommando gewinnen + letzter Treffer mit 'Der große Schwung'."));
            lessons.Add(new LessonData("10 · Charakter-Spezifisch", "Vertiefe einen Charakter: Lies den Strategieguide (docs/STRATEGY.md)."));
        }

        void ShowLesson(int index)
        {
            currentLesson = Mathf.Clamp(index, 0, lessons.Count - 1);
            var lesson = lessons[currentLesson];
            if (lessonTitle != null) lessonTitle.text = lesson.title;
            if (lessonDescription != null) lessonDescription.text = lesson.description;
            if (progressText != null) progressText.text = $"Lektion {currentLesson + 1} / {lessons.Count}";
            if (prevButton != null) prevButton.SetActive(currentLesson > 0);
            if (nextButton != null) nextButton.SetActive(currentLesson < lessons.Count - 1);
            PlayerPrefs.SetInt("tutorial_progress", currentLesson);
        }

        public void Next() { if (currentLesson < lessons.Count - 1) ShowLesson(currentLesson + 1); }
        public void Prev() { if (currentLesson > 0) ShowLesson(currentLesson - 1); }
        public void Close() { if (tutorialPanel != null) tutorialPanel.SetActive(false); }

        [System.Serializable]
        public class LessonData
        {
            public string title;
            public string description;
            public LessonData(string t, string d) { title = t; description = d; }
        }
    }
}
