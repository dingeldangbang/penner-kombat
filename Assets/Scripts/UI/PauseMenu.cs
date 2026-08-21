using UnityEngine;
using UnityEngine.SceneManagement;

namespace PennerKombat
{
    /// <summary>
    /// Pausemenü während eines Kampfes (Esc/P öffnet, pausiert die Zeit).
    /// </summary>
    public class PauseMenu : MonoBehaviour
    {
        [Header("UI")]
        public GameObject pausePanel;

        void Update()
        {
            if (UnityEngine.InputSystem.Keyboard.current == null) return;
            var kbd = UnityEngine.InputSystem.Keyboard.current;
            if (kbd[UnityEngine.InputSystem.Key.Escape].wasPressedThisFrame
                || kbd[UnityEngine.InputSystem.Key.P].wasPressedThisFrame)
                TogglePause();
        }

        public void TogglePause()
        {
            if (pausePanel == null) return;
            bool paused = pausePanel.activeSelf;
            pausePanel.SetActive(!paused);
            Time.timeScale = paused ? 1f : 0f;
        }

        public void Resume()
        {
            if (pausePanel != null) pausePanel.SetActive(false);
            Time.timeScale = 1f;
        }

        public void QuitToMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("MainMenu");
        }

        void OnDestroy()
        {
            Time.timeScale = 1f;
        }
    }
}
