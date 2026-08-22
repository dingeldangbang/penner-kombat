using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PennerKombat
{
    /// <summary>
    /// Baut das komplette HUD zur Laufzeit aus Code: Lebensbalken, Fatal-Blow-
    /// Leisten, Timer, Rundenanzeige, Combo-Texte, Namen und das Ergebnis-Panel
    /// mit Revanche-Knopf. Damit braucht man kein Canvas-Prefab, um zu spielen.
    ///
    /// Aufruf: <c>UIManager ui = HudBuilder.Ensure();</c>
    /// Ein manuell gebautes HUD (UIManager bereits in der Szene) hat Vorrang.
    /// Siehe docs/SPIELEN.md
    /// </summary>
    public static class HudBuilder
    {
        public static UIManager Ensure()
        {
            if (UIManager.Instance != null) return UIManager.Instance;

            var existing = Object.FindObjectOfType<UIManager>();
            if (existing != null) return existing;

            var root = new GameObject("~AutoHUD");
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            root.AddComponent<GraphicRaycaster>();
            EnsureEventSystem();

            var ui = root.AddComponent<UIManager>();

            // --- Lebensbalken links / rechts ---
            ui.hpBar1 = Bar(root.transform, new Vector2(0f, 1f), new Vector2(40f, -40f),
                            new Vector2(760f, 46f), false, "HP1");
            ui.hpBar2 = Bar(root.transform, new Vector2(1f, 1f), new Vector2(-40f, -40f),
                            new Vector2(760f, 46f), true, "HP2");

            // --- Fatal-Blow-Leisten darunter ---
            ui.fatalBlow1 = Bar(root.transform, new Vector2(0f, 1f), new Vector2(40f, -94f),
                                new Vector2(420f, 14f), false, "FB1");
            ui.fatalBlow2 = Bar(root.transform, new Vector2(1f, 1f), new Vector2(-40f, -94f),
                                new Vector2(420f, 14f), true, "FB2");

            // --- Namen ---
            ui.name1 = Label(root.transform, new Vector2(0f, 1f), new Vector2(46f, -8f),
                             new Vector2(500f, 30f), 26, TextAlignmentOptions.Left, "Name1");
            ui.name2 = Label(root.transform, new Vector2(1f, 1f), new Vector2(-46f, -8f),
                             new Vector2(500f, 30f), 26, TextAlignmentOptions.Right, "Name2");

            // --- Timer + Runden ---
            ui.timerText = Label(root.transform, new Vector2(0.5f, 1f), new Vector2(0f, -20f),
                                 new Vector2(300f, 90f), 76, TextAlignmentOptions.Center, "Timer");
            ui.timerText.color = PennerPalette.Gold;
            ui.roundText = Label(root.transform, new Vector2(0.5f, 1f), new Vector2(0f, -108f),
                                 new Vector2(420f, 34f), 26, TextAlignmentOptions.Center, "Runde");

            // --- Combo-Texte ---
            ui.comboText1 = Label(root.transform, new Vector2(0f, 1f), new Vector2(46f, -130f),
                                  new Vector2(500f, 44f), 34, TextAlignmentOptions.Left, "Combo1");
            ui.comboText2 = Label(root.transform, new Vector2(1f, 1f), new Vector2(-46f, -130f),
                                  new Vector2(500f, 44f), 34, TextAlignmentOptions.Right, "Combo2");

            // --- Hinweiszeile (Steuerung) ---
            var hint = Label(root.transform, new Vector2(0.5f, 0f), new Vector2(0f, 24f),
                             new Vector2(1600f, 30f), 20, TextAlignmentOptions.Center, "Hinweis");
            hint.color = new Color(1f, 1f, 1f, 0.55f);
            hint.text = "P1: WASD · J leicht · K schwer · Shift Block · Space Sprung · U/I Spezial · H Med   |   "
                      + "P2: Pfeile · Num1/2 · Num3 Block · Num0 Sprung   |   F4 Frame-Daten · F7 Modelle";

            // --- Ergebnis-Panel ---
            var panel = new GameObject("ResultPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(root.transform, false);
            var panelRt = (RectTransform)panel.transform;
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(900f, 400f);
            panelRt.anchoredPosition = Vector2.zero;
            panel.GetComponent<Image>().color = new Color(PennerPalette.NightBlue.r, PennerPalette.NightBlue.g,
                                                          PennerPalette.NightBlue.b, 0.92f);
            ui.resultPanel = panel;

            ui.resultText = Label(panel.transform, new Vector2(0.5f, 0.72f), Vector2.zero,
                                  new Vector2(860f, 120f), 60, TextAlignmentOptions.Center, "Ergebnis");
            ui.resultText.color = PennerPalette.Gold;

            ui.rematchButton = Button(panel.transform, new Vector2(0.5f, 0.32f), new Vector2(-170f, 0f),
                                      "REVANCHE", PennerPalette.BloodRed);
            ui.menuButton = Button(panel.transform, new Vector2(0.5f, 0.32f), new Vector2(170f, 0f),
                                   "MENÜ", PennerPalette.NightBlue);
            panel.SetActive(false);

            if (TMP_Settings.instance == null || TMP_Settings.defaultFontAsset == null)
            {
                Debug.LogWarning("[Penner Kombat] TextMeshPro-Grundressourcen fehlen. "
                    + "Menü: Window → TextMeshPro → Import TMP Essential Resources. "
                    + "Sonst bleibt das HUD leer.");
            }

            return ui;
        }

        static void EnsureEventSystem()
        {
            if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() != null) return;
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
#if ENABLE_INPUT_SYSTEM
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
#endif
        }

        static Image Bar(Transform parent, Vector2 anchor, Vector2 offset, Vector2 size,
                         bool mirrored, string name)
        {
            // Rahmen
            var back = new GameObject(name + "_BG", typeof(RectTransform), typeof(Image));
            back.transform.SetParent(parent, false);
            var backRt = (RectTransform)back.transform;
            backRt.anchorMin = backRt.anchorMax = anchor;
            backRt.pivot = new Vector2(mirrored ? 1f : 0f, 1f);
            backRt.sizeDelta = size;
            backRt.anchoredPosition = offset;
            back.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

            var fillGo = new GameObject(name, typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(back.transform, false);
            var rt = (RectTransform)fillGo.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(3f, 3f);
            rt.offsetMax = new Vector2(-3f, -3f);

            var img = fillGo.GetComponent<Image>();
            img.color = PennerPalette.Gold;
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal;
            img.fillOrigin = mirrored ? (int)Image.OriginHorizontal.Right : (int)Image.OriginHorizontal.Left;
            img.fillAmount = 1f;
            // Ohne Sprite zeichnet Unity ein weißes Rechteck — genau das wollen wir.
            return img;
        }

        static TextMeshProUGUI Label(Transform parent, Vector2 anchor, Vector2 offset, Vector2 size,
                                     float fontSize, TextAlignmentOptions align, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(anchor.x, anchor.y);
            rt.sizeDelta = size;
            rt.anchoredPosition = offset;

            var text = go.AddComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.alignment = align;
            text.color = Color.white;
            text.text = "";
            text.enableWordWrapping = false;
            return text;
        }

        static Button Button(Transform parent, Vector2 anchor, Vector2 offset, string caption, Color color)
        {
            var go = new GameObject("Btn_" + caption, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(300f, 80f);
            rt.anchoredPosition = offset;
            go.GetComponent<Image>().color = color;

            var label = Label(go.transform, new Vector2(0.5f, 0.5f), Vector2.zero,
                              new Vector2(290f, 70f), 30, TextAlignmentOptions.Center, "Label");
            label.text = caption;
            return go.GetComponent<Button>();
        }
    }
}
