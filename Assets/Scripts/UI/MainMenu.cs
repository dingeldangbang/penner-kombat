using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PennerKombat
{
    /// <summary>
    /// Hauptmenü: Story, Versus, Online, Optionen, Beenden.
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        [Header("Buttons")]
        public Button storyButton;
        public Button versusButton;
        public Button onlineButton;
        public Button optionsButton;
        public Button quitButton;

        void Start()
        {
            if (storyButton != null) storyButton.onClick.AddListener(() => SceneManager.LoadScene("StoryMode"));
            if (versusButton != null) versusButton.onClick.AddListener(() => SceneManager.LoadScene("Arena"));
            if (onlineButton != null) onlineButton.onClick.AddListener(() => SceneManager.LoadScene("Lobby"));
            if (optionsButton != null) optionsButton.onClick.AddListener(() => SceneManager.LoadScene("Options"));
            if (quitButton != null) quitButton.onClick.AddListener(Application.Quit);

            if (AudioManager.Instance != null) AudioManager.Instance.PlayMenuMusic();
        }
    }
}
