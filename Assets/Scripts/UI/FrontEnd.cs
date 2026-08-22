using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PennerKombat
{
    /// <summary>
    /// Prozedurales Frontend: Hauptmenü und Charakterauswahl, ohne ein einziges
    /// Prefab. Ersetzt für den Start das Direkt-in-die-Arena-Springen.
    ///
    ///  • Hauptmenü: VERSUS · TRAINING · MODELLE (F7) · BEENDEN
    ///  • Auswahl: alle 9 Charaktere für P1 und P2, Gegner Mensch oder KI,
    ///    KI-Stufe von Sehr leicht bis Boss, Rundenzahl 1/3/5
    ///  • Taste <b>F1</b> oder Escape holt das Menü jederzeit zurück
    ///
    /// Siehe docs/SPIELEN.md
    /// </summary>
    public class FrontEnd : MonoBehaviour
    {
        public static FrontEnd Instance { get; private set; }

        /// <summary>Solange true, startet der GameManager kein Auto-Duell.</summary>
        public static bool SuppressAutoStart { get; private set; }

        enum Page { Main, Select }

        GameObject mainPanel, selectPanel;
        TextMeshProUGUI p1Label, p2Label, aiLabel, roundsLabel, modeLabel;
        readonly List<Button> p1Buttons = new List<Button>();
        readonly List<Button> p2Buttons = new List<Button>();

        int p1Index, p2Index = 2;
        bool p2IsHuman;
        int aiLevel = 2;                       // Medium
        int roundsIndex = 1;                   // 1 = Best of 3
        static readonly int[] RoundOptions = { 1, 3, 5 };

        static readonly string[] AiNames =
            { "Sehr leicht", "Leicht", "Mittel", "Schwer", "Sehr schwer", "BOSS" };

        public static FrontEnd Ensure()
        {
            if (Instance == null)
            {
                var go = new GameObject("~FrontEnd");
                Instance = go.AddComponent<FrontEnd>();
            }
            return Instance;
        }

        void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }

            SuppressAutoStart = true;
            Build();
            Show(Page.Main);
        }

        void Update()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb == null) return;
            if (kb.f1Key.wasPressedThisFrame) Show(Page.Main);
            else if (kb.escapeKey.wasPressedThisFrame && selectPanel.activeSelf) Show(Page.Main);
#endif
        }

        // ------------------------------------------------------------------

        void Show(Page page)
        {
            bool anyVisible = page == Page.Main || page == Page.Select;
            mainPanel.SetActive(page == Page.Main);
            selectPanel.SetActive(page == Page.Select);
            Time.timeScale = anyVisible ? 0f : 1f;
            if (page == Page.Select) RefreshSelection();
        }

        void Hide()
        {
            mainPanel.SetActive(false);
            selectPanel.SetActive(false);
            Time.timeScale = 1f;
        }

        void StartMatch()
        {
            SuppressAutoStart = false;
            Hide();

            var gm = GameManager.Instance;
            if (gm == null) return;
            gm.bestOfRounds = RoundOptions[roundsIndex];
            gm.aiDifficulty = (AIDifficulty)aiLevel;
            gm.StartVersusFight(p1Index, p2Index, !p2IsHuman);
        }

        void StartTraining()
        {
            SuppressAutoStart = false;
            Hide();

            var gm = GameManager.Instance;
            if (gm == null) return;
            gm.bestOfRounds = 1;
            gm.roundTime = 999f;
            gm.aiDifficulty = AIDifficulty.VeryEasy;
            gm.StartVersusFight(p1Index, p2Index, true);

            if (TrainingMode.Instance == null)
            {
                var training = new GameObject("~TrainingMode").AddComponent<TrainingMode>();
                training.infiniteHealth = true;
                training.showFrameData = true;
            }
            FrameDataOverlay.Ensure().visible = true;
        }

        // ------------------------------------------------------------------
        // Aufbau
        // ------------------------------------------------------------------

        void Build()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 950;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            gameObject.AddComponent<GraphicRaycaster>();
            HudBuilder.Ensure();   // sorgt nebenbei für ein EventSystem

            BuildMain();
            BuildSelect();
        }

        void BuildMain()
        {
            mainPanel = Panel("MainPanel", new Vector2(1920f, 1080f), Backdrop(0.97f));

            var title = Label(mainPanel.transform, new Vector2(0.5f, 0.82f), Vector2.zero,
                              new Vector2(1600f, 130f), 92, TextAlignmentOptions.Center);
            title.text = "PENNER KOMBAT";
            title.color = PennerPalette.BloodRed;

            var sub = Label(mainPanel.transform, new Vector2(0.5f, 0.74f), Vector2.zero,
                            new Vector2(1600f, 50f), 30, TextAlignmentOptions.Center);
            sub.text = "Kämpfe wie ein Penner — gewinne wie ein König";
            sub.color = PennerPalette.Gold;

            float y = 0.56f;
            Button(mainPanel.transform, new Vector2(0.5f, y), new Vector2(460f, 84f),
                   "VERSUS", PennerPalette.BloodRed, () => Show(Page.Select));
            Button(mainPanel.transform, new Vector2(0.5f, y -= 0.11f), new Vector2(460f, 84f),
                   "TRAINING", PennerPalette.WarmOrange, StartTraining);
            Button(mainPanel.transform, new Vector2(0.5f, y -= 0.11f), new Vector2(460f, 84f),
                   "MODELLE (GLB)", PennerPalette.NeonBlue, () =>
                   {
                       Hide();
                       ModelMenuUI.Ensure().SetVisible(true);
                   });
            Button(mainPanel.transform, new Vector2(0.5f, y -= 0.11f), new Vector2(460f, 84f),
                   "BEENDEN", new Color(0.22f, 0.22f, 0.26f), Quit);

            var hint = Label(mainPanel.transform, new Vector2(0.5f, 0.06f), Vector2.zero,
                             new Vector2(1600f, 40f), 20, TextAlignmentOptions.Center);
            hint.color = new Color(1f, 1f, 1f, 0.55f);
            hint.text = "F1 Menü · F4 Frame-Daten · F7 Modelle · P1: WASD J K Shift Space · P2: Pfeile Num1 Num2 Num3 Num0";
        }

        void BuildSelect()
        {
            selectPanel = Panel("SelectPanel", new Vector2(1920f, 1080f), Backdrop(0.96f));

            var title = Label(selectPanel.transform, new Vector2(0.5f, 0.93f), Vector2.zero,
                              new Vector2(1600f, 80f), 54, TextAlignmentOptions.Center);
            title.text = "WER PRÜGELT?";
            title.color = PennerPalette.Gold;

            p1Label = Label(selectPanel.transform, new Vector2(0.22f, 0.84f), Vector2.zero,
                            new Vector2(600f, 50f), 32, TextAlignmentOptions.Center);
            p2Label = Label(selectPanel.transform, new Vector2(0.78f, 0.84f), Vector2.zero,
                            new Vector2(600f, 50f), 32, TextAlignmentOptions.Center);

            var ids = GameConstants.AllCharacterIds;
            for (int i = 0; i < ids.Length; i++)
            {
                int index = i;
                float x = 0.10f + (i % 3) * 0.11f;
                float yy = 0.70f - (i / 3) * 0.12f;
                p1Buttons.Add(Button(selectPanel.transform, new Vector2(x, yy), new Vector2(190f, 74f),
                                     ShortName(ids[i]), SlotColor(ids[i]), () => { p1Index = index; RefreshSelection(); }));

                float x2 = 0.69f + (i % 3) * 0.11f;
                p2Buttons.Add(Button(selectPanel.transform, new Vector2(x2, yy), new Vector2(190f, 74f),
                                     ShortName(ids[i]), SlotColor(ids[i]), () => { p2Index = index; RefreshSelection(); }));
            }

            // Einstellungen in der Mitte
            modeLabel = Label(selectPanel.transform, new Vector2(0.5f, 0.70f), Vector2.zero,
                              new Vector2(420f, 44f), 26, TextAlignmentOptions.Center);
            Button(selectPanel.transform, new Vector2(0.5f, 0.645f), new Vector2(380f, 60f),
                   "GEGNER WECHSELN", new Color(0.2f, 0.2f, 0.26f), () =>
                   { p2IsHuman = !p2IsHuman; RefreshSelection(); });

            aiLabel = Label(selectPanel.transform, new Vector2(0.5f, 0.56f), Vector2.zero,
                            new Vector2(420f, 44f), 26, TextAlignmentOptions.Center);
            Button(selectPanel.transform, new Vector2(0.5f, 0.505f), new Vector2(380f, 60f),
                   "KI-STUFE +", new Color(0.2f, 0.2f, 0.26f), () =>
                   { aiLevel = (aiLevel + 1) % AiNames.Length; RefreshSelection(); });

            roundsLabel = Label(selectPanel.transform, new Vector2(0.5f, 0.42f), Vector2.zero,
                                new Vector2(420f, 44f), 26, TextAlignmentOptions.Center);
            Button(selectPanel.transform, new Vector2(0.5f, 0.365f), new Vector2(380f, 60f),
                   "RUNDEN +", new Color(0.2f, 0.2f, 0.26f), () =>
                   { roundsIndex = (roundsIndex + 1) % RoundOptions.Length; RefreshSelection(); });

            Button(selectPanel.transform, new Vector2(0.5f, 0.20f), new Vector2(520f, 96f),
                   "LOSPRÜGELN", PennerPalette.BloodRed, StartMatch);
            Button(selectPanel.transform, new Vector2(0.5f, 0.09f), new Vector2(300f, 60f),
                   "ZURÜCK", new Color(0.22f, 0.22f, 0.26f), () => Show(Page.Main));
        }

        void RefreshSelection()
        {
            var db = GameManager.Instance != null ? GameManager.Instance.database : null;
            string n1 = db != null && db.GetFighter(p1Index) != null ? db.GetFighter(p1Index).displayName
                                                                    : GameConstants.AllCharacterIds[p1Index];
            string n2 = db != null && db.GetFighter(p2Index) != null ? db.GetFighter(p2Index).displayName
                                                                    : GameConstants.AllCharacterIds[p2Index];
            p1Label.text = $"SPIELER 1 — {n1}";
            p1Label.color = PennerPalette.ForCharacter(GameConstants.AllCharacterIds[p1Index]);
            p2Label.text = (p2IsHuman ? "SPIELER 2 — " : "KI — ") + n2;
            p2Label.color = PennerPalette.ForCharacter(GameConstants.AllCharacterIds[p2Index]);

            modeLabel.text = p2IsHuman ? "Gegner: Spieler 2 (Tastatur)" : "Gegner: Computer";
            aiLabel.text = p2IsHuman ? "KI-Stufe: —" : $"KI-Stufe: {AiNames[aiLevel]}";
            roundsLabel.text = RoundOptions[roundsIndex] == 1
                ? "Runden: eine Runde"
                : $"Runden: Best of {RoundOptions[roundsIndex]}";

            for (int i = 0; i < p1Buttons.Count; i++)
            {
                Mark(p1Buttons[i], i == p1Index);
                Mark(p2Buttons[i], i == p2Index);
            }
        }

        static void Mark(Button button, bool selected)
        {
            var outline = button.GetComponent<Outline>();
            if (outline == null) outline = button.gameObject.AddComponent<Outline>();
            outline.effectColor = PennerPalette.Gold;
            outline.effectDistance = new Vector2(3f, 3f);
            outline.enabled = selected;
        }

        static string ShortName(string id)
        {
            switch (id)
            {
                case GameConstants.CharLeBinde:  return "LE BINDE";
                case GameConstants.CharMojoBob:  return "MOJO BOB";
                case GameConstants.CharTetraPak: return "TETRAPAK";
                default: return id.ToUpperInvariant();
            }
        }

        static Color SlotColor(string id)
            => Color.Lerp(PennerPalette.ForCharacter(id), Color.black, 0.35f);

        static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ---------- UI-Helfer ----------

        static Color Backdrop(float alpha)
            => new Color(PennerPalette.NightBlue.r, PennerPalette.NightBlue.g, PennerPalette.NightBlue.b, alpha);

        GameObject Panel(string name, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = color;
            return go;
        }

        static TextMeshProUGUI Label(Transform parent, Vector2 anchor, Vector2 offset, Vector2 size,
                                     float fontSize, TextAlignmentOptions align)
        {
            var go = new GameObject("Label", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = offset;

            var text = go.AddComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.alignment = align;
            text.color = Color.white;
            text.enableWordWrapping = false;
            text.text = "";
            return text;
        }

        static Button Button(Transform parent, Vector2 anchor, Vector2 size, string caption,
                             Color color, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject("Btn_" + caption, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;
            go.GetComponent<Image>().color = color;

            var label = Label(go.transform, new Vector2(0.5f, 0.5f), Vector2.zero, size,
                              Mathf.Clamp(size.y * 0.38f, 18f, 34f), TextAlignmentOptions.Center);
            label.text = caption;

            var button = go.GetComponent<Button>();
            button.onClick.AddListener(onClick);
            return button;
        }
    }
}
