using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PennerKombat
{
    /// <summary>
    /// Verwaltet das HUD: Lebensbalken, Timer, Rundenanzeige, Combo-Counter,
    /// Fatal-Blow-Leisten und das Match-Ergebnis-Panel.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance;

        [Header("HUD")]
        public Image hpBar1;
        public Image hpBar2;
        public Image fatalBlow1;
        public Image fatalBlow2;
        public TextMeshProUGUI timerText;
        public TextMeshProUGUI roundText;
        public TextMeshProUGUI comboText1;
        public TextMeshProUGUI comboText2;
        public TextMeshProUGUI name1;
        public TextMeshProUGUI name2;

        [Header("Match Result")]
        public GameObject resultPanel;
        public TextMeshProUGUI resultText;
        public Button rematchButton;
        public Button menuButton;

        void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(this); return; }
            if (resultPanel != null) resultPanel.SetActive(false);
        }

        public static void NotifyDamage(FighterController attacker, FighterController target, float dmg)
        {
            // Kann für Flickeffekte / Trophäen / Netzwerk genutzt werden.
        }

        public void UpdateHealth(float p1, float p2)
        {
            if (hpBar1 != null) { hpBar1.fillAmount = p1; hpBar1.color = Color.Lerp(Color.red, Color.green, p1); }
            if (hpBar2 != null) { hpBar2.fillAmount = p2; hpBar2.color = Color.Lerp(Color.red, Color.green, p2); }
        }

        public void UpdateFatalBlow(float f1, float f2)
        {
            if (fatalBlow1 != null) fatalBlow1.fillAmount = f1;
            if (fatalBlow2 != null) fatalBlow2.fillAmount = f2;
        }

        public void UpdateTimer(float time)
        {
            if (timerText != null) timerText.text = Mathf.Ceil(time).ToString("0");
        }

        public void UpdateRoundDisplay(int round, int max)
        {
            if (roundText != null) roundText.text = $"Runde {round} von {max}";
        }

        public void UpdateCombo(int c1, int c2)
        {
            if (comboText1 != null) comboText1.text = c1 > 1 ? $"{c1} Treffer!" : "";
            if (comboText2 != null) comboText2.text = c2 > 1 ? $"{c2} Treffer!" : "";
        }

        public void ResetCombo()
        {
            if (comboText1 != null) comboText1.text = "";
            if (comboText2 != null) comboText2.text = "";
        }

        public void SetNames(string p1, string p2)
        {
            if (name1 != null) name1.text = p1;
            if (name2 != null) name2.text = p2;
        }

        public void ShowMatchResult(string winner)
        {
            if (resultPanel == null) return;
            resultPanel.SetActive(true);
            if (resultText != null) resultText.text = $"🏆 {winner} gewinnt!";
            if (rematchButton != null)
                rematchButton.onClick.RemoveAllListeners();
            if (rematchButton != null)
                rematchButton.onClick.AddListener(() => GameManager.Instance.RestartMatch());
            if (menuButton != null)
                menuButton.onClick.RemoveAllListeners();
            if (menuButton != null)
                menuButton.onClick.AddListener(() => UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu"));
        }
    }
}
