using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PennerKombat
{
    /// <summary>
    /// Charakter-Ressourcenanzeigen aus Spec §6: Mojo-Punkte (7 goldene Punkte),
    /// Mells Puls-Balken (rot, pulsierend) und Le Bindes Schmier-Schlüppa
    /// (gelb → braun). Baut sich als eigene Canvas-Ebene unter dem HUD auf,
    /// falls im UIManager keine Anzeigen zugewiesen sind.
    /// </summary>
    public class HudStatusBars : MonoBehaviour
    {
        public static HudStatusBars Instance { get; private set; }

        [Header("Layout")]
        public Vector2 p1Anchor = new Vector2(30f, -78f);
        public Vector2 p2Anchor = new Vector2(-30f, -78f);
        public Vector2 barSize = new Vector2(200f, 10f);

        private Canvas canvas;
        private readonly Dictionary<FighterController, Row> rows = new Dictionary<FighterController, Row>();

        private class Row
        {
            public RectTransform root;
            public Image fill;
            public TextMeshProUGUI caption;
            public Image[] dots;
        }

        public static HudStatusBars Ensure()
        {
            if (Instance == null)
            {
                var go = new GameObject("~HudStatusBars");
                Instance = go.AddComponent<HudStatusBars>();
            }
            return Instance;
        }

        void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }

            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 700;
            gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        }

        void Update()
        {
            var fighters = FindObjectsOfType<FighterController>();
            foreach (var f in fighters)
            {
                if (f == null || !f.gameObject.activeInHierarchy) continue;
                if (!rows.TryGetValue(f, out var row) || row.root == null)
                {
                    row = BuildRow(f);
                    rows[f] = row;
                }
                Refresh(f, row);
            }
        }

        Row BuildRow(FighterController f)
        {
            bool right = f.playerIndex == 1;

            var root = new GameObject($"Status_{f.displayName}", typeof(RectTransform));
            root.transform.SetParent(transform, false);
            var rt = (RectTransform)root.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(right ? 1f : 0f, 1f);
            rt.pivot = new Vector2(right ? 1f : 0f, 1f);
            rt.anchoredPosition = right ? p2Anchor : p1Anchor;
            rt.sizeDelta = barSize;

            // Hintergrund
            var bg = new GameObject("BG", typeof(RectTransform)).AddComponent<Image>();
            bg.transform.SetParent(root.transform, false);
            var bgRt = bg.rectTransform;
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;
            bg.color = PennerPalette.NightBlue.WithAlpha(0.75f);
            bg.raycastTarget = false;

            // Füllung
            var fill = new GameObject("Fill", typeof(RectTransform)).AddComponent<Image>();
            fill.transform.SetParent(root.transform, false);
            var fRt = fill.rectTransform;
            fRt.anchorMin = Vector2.zero; fRt.anchorMax = Vector2.one;
            fRt.offsetMin = Vector2.zero; fRt.offsetMax = Vector2.zero;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = right ? 1 : 0;
            fill.color = PennerPalette.ForCharacter(f.fighterId);
            fill.raycastTarget = false;

            // Beschriftung
            var cap = new GameObject("Caption", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            cap.transform.SetParent(root.transform, false);
            var cRt = cap.rectTransform;
            cRt.anchorMin = new Vector2(0f, 0f); cRt.anchorMax = new Vector2(1f, 0f);
            cRt.pivot = new Vector2(right ? 1f : 0f, 1f);
            cRt.anchoredPosition = new Vector2(0f, -2f);
            cRt.sizeDelta = new Vector2(barSize.x, 18f);
            cap.fontSize = 13f;
            cap.color = PennerPalette.Pure.WithAlpha(0.85f);
            cap.alignment = right ? TextAlignmentOptions.Right : TextAlignmentOptions.Left;
            cap.raycastTarget = false;

            // Mojo-Punkte (nur Bob)
            Image[] dots = null;
            if (f is MojoBob bob)
            {
                dots = new Image[bob.maxMojo];
                for (int i = 0; i < dots.Length; i++)
                {
                    var d = new GameObject($"Mojo{i}", typeof(RectTransform)).AddComponent<Image>();
                    d.transform.SetParent(root.transform, false);
                    var dRt = d.rectTransform;
                    dRt.anchorMin = dRt.anchorMax = new Vector2(right ? 1f : 0f, 0f);
                    dRt.pivot = new Vector2(right ? 1f : 0f, 1f);
                    dRt.sizeDelta = new Vector2(7f, 12f);
                    float x = (i * 11f + 2f) * (right ? -1f : 1f);
                    dRt.anchoredPosition = new Vector2(x, -20f);
                    d.color = PennerPalette.Gold.WithAlpha(0.25f);
                    d.raycastTarget = false;
                    dots[i] = d;
                }
            }

            return new Row { root = rt, fill = fill, caption = cap, dots = dots };
        }

        void Refresh(FighterController f, Row row)
        {
            switch (f)
            {
                case Mell mell:
                {
                    float p = Mathf.InverseLerp(60f, mell.maxPulse, mell.Pulse);
                    row.fill.fillAmount = p;
                    row.fill.color = Color.Lerp(PennerPalette.NeonBlue, PennerPalette.BloodRed, p);
                    // pulsiert im Herzschlag-Takt
                    float beat = 1f + Mathf.Sin(Time.time * Mathf.Lerp(3f, 12f, p)) * 0.06f * p;
                    row.root.localScale = new Vector3(1f, beat, 1f);
                    row.caption.text = $"PULS {Mathf.RoundToInt(mell.Pulse)} bpm";
                    break;
                }
                case MojoBob bob:
                {
                    row.fill.fillAmount = Mathf.Clamp01(bob.CritChance);
                    row.fill.color = PennerPalette.Gold;
                    row.caption.text = $"MOJO {bob.MojoPoints}/{bob.maxMojo} · KRIT {Mathf.RoundToInt(bob.CritChance * 100f)} %";
                    if (row.dots != null)
                        for (int i = 0; i < row.dots.Length; i++)
                            row.dots[i].color = i < bob.MojoPoints
                                ? PennerPalette.Gold
                                : PennerPalette.Gold.WithAlpha(0.22f);
                    break;
                }
                case LeBinde binde:
                {
                    float charge = binde.greaseChargesMax > 0
                        ? Mathf.Clamp01((float)binde.GreaseCharges / binde.greaseChargesMax)
                        : 0f;
                    row.fill.fillAmount = charge;
                    row.fill.color = Color.Lerp(PennerPalette.Gold, PennerPalette.Earth, 1f - charge);
                    row.caption.text = $"SCHMIER-SCHLÜPPA {binde.GreaseCharges}/{binde.greaseChargesMax}";
                    break;
                }
                default:
                {
                    float meter = f.fatalBlowMeter / GameConstants.FatalBlowMeterMax;
                    row.fill.fillAmount = meter;
                    row.fill.color = f.fatalBlowReady
                        ? PennerPalette.BloodRed
                        : PennerPalette.ForCharacter(f.fighterId);
                    row.caption.text = f.fatalBlowReady ? "FATAL BLOW BEREIT" : "FATAL BLOW";
                    break;
                }
            }
        }
    }
}
