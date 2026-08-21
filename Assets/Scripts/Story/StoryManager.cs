using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PennerKombat
{
    /// <summary>
    /// Steuert den Story-Modus: Kapitel, Dialoge, Kämpfe, Enden.
    /// Wird über den Endings (A-D) verknüpft; das Ergebnis eines Kampfes
    /// kommt aus dem GameManager (OnMatchEnded).
    /// </summary>
    public class StoryManager : MonoBehaviour
    {
        public static StoryManager Instance;

        [Header("Story Data")]
        public List<StoryChapter> chapters = new List<StoryChapter>();
        public DialogueSystem dialogueSystem;
        public AudioClip chapterStartSound;

        private int currentChapterIndex;
        private int currentFightIndex;
        private readonly Dictionary<string, string> storyState = new Dictionary<string, string>();
        private bool isActive;

        void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(this); return; }
            DontDestroyOnLoad(gameObject);
        }

        public void StartStory()
        {
            if (chapters.Count == 0) { Debug.LogWarning("StoryManager: keine Kapitel definiert."); return; }
            StartChapter(0);
        }

        public void StartChapter(int index)
        {
            if (index < 0 || index >= chapters.Count) return;
            currentChapterIndex = index;
            currentFightIndex = 0;
            isActive = true;

            var chapter = chapters[index];
            if (chapter.music != null) AudioManager.Instance?.PlayMusic(chapter.music);
            else AudioManager.Instance?.PlayStoryMusic();

            if (chapterStartSound != null) AudioManager.Instance?.PlaySFX(chapterStartSound);
            StartDialogueFlow();
        }

        void StartDialogueFlow()
        {
            var chapter = chapters[currentChapterIndex];
            if (chapter.dialogues.Count == 0)
            {
                StartFight();
                return;
            }

            if (dialogueSystem != null)
                dialogueSystem.PlaySequence(chapter.dialogues, StartFight);
            else
                StartFight();
        }

        void StartFight()
        {
            var chapter = chapters[currentChapterIndex];
            if (currentFightIndex >= chapter.fights.Count)
            {
                OnChapterComplete();
                return;
            }
            var fight = chapter.fights[currentFightIndex];
            if (GameManager.Instance != null)
                GameManager.Instance.StartStoryFight(fight.opponentId, fight.arenaId, fight.difficulty, fight.isBoss);
        }

        /// <summary>Wird vom GameManager nach einem Story-Kampf aufgerufen.</summary>
        public void OnFightResult(bool won)
        {
            var chapter = chapters[currentChapterIndex];
            if (currentFightIndex >= chapter.fights.Count) { OnChapterComplete(); return; }
            var fight = chapter.fights[currentFightIndex];

            if (won)
            {
                currentFightIndex++;
                StartFight();
            }
            else
            {
                // Niederlage: Kapitel-Kampf wiederholen (einfaches Retry)
                StartFight();
            }
        }

        void OnChapterComplete()
        {
            var chapter = chapters[currentChapterIndex];
            if (chapter.isFinalChapter)
            {
                OnGameComplete();
                return;
            }
            if (!string.IsNullOrEmpty(chapter.nextChapterId))
            {
                int next = chapters.FindIndex(c => c.id == chapter.nextChapterId);
                if (next >= 0) { StartChapter(next); return; }
            }
            if (currentChapterIndex + 1 < chapters.Count)
                StartChapter(currentChapterIndex + 1);
            else
                OnGameComplete();
        }

        void OnGameComplete()
        {
            isActive = false;
            StartCoroutine(ReturnToMenu(5f));
        }

        IEnumerator ReturnToMenu(float delay)
        {
            yield return new WaitForSeconds(delay);
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }

        // ===== Story-Zustand (für Endings A-D) =====
        public void SetStoryState(string key, string value) => storyState[key] = value;
        public string GetStoryState(string key) => storyState.TryGetValue(key, out var v) ? v : "";

        public bool HasFlag(string key) => GetStoryState(key) == "true";
    }
}
