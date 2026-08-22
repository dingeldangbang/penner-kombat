using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PennerKombat
{
    /// <summary>
    /// Modell-Menü im laufenden Spiel (Taste <b>F7</b>): ordnet jedem der neun
    /// Charaktere eine GLB-Datei zu, justiert Größe, Drehung und Höhe und
    /// übernimmt alles sofort in den laufenden Kampf.
    ///
    /// Baut sich komplett prozedural — kein Canvas-Prefab nötig.
    /// Modelle kommen aus den Ordnern in <see cref="GlbLibrary.SearchFolders"/>.
    /// Siehe docs/MODELLE.md
    /// </summary>
    public class ModelMenuUI : MonoBehaviour
    {
        public static ModelMenuUI Instance { get; private set; }

        public bool visible;

        GameObject panel;
        readonly List<Row> rows = new List<Row>();
        List<string> files = new List<string>();
        TextMeshProUGUI header;

        class Row
        {
            public string id;
            public TextMeshProUGUI label;
            public TextMeshProUGUI value;
        }

        public static ModelMenuUI Ensure()
        {
            if (Instance == null)
            {
                var go = new GameObject("~ModelMenu");
                Instance = go.AddComponent<ModelMenuUI>();
            }
            return Instance;
        }

        void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
            Build();
            SetVisible(false);
        }

        void Update()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.f7Key.wasPressedThisFrame) SetVisible(!visible);
            if (visible && kb != null && kb.escapeKey.wasPressedThisFrame) SetVisible(false);
#endif
        }

        public void SetVisible(bool value)
        {
            visible = value;
            if (panel != null) panel.SetActive(value);
            if (value) Rescan();
        }

        // ------------------------------------------------------------------

        void Build()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 900;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            gameObject.AddComponent<GraphicRaycaster>();

            panel = NewPanel(transform, new Vector2(1240f, 900f),
                             new Color(PennerPalette.NightBlue.r, PennerPalette.NightBlue.g,
                                       PennerPalette.NightBlue.b, 0.96f));

            header = Label(panel.transform, new Vector2(0.5f, 1f), new Vector2(0f, -24f),
                           new Vector2(1180f, 60f), 34, TextAlignmentOptions.Center);
            header.color = PennerPalette.Gold;
            header.text = "MODELLE (GLB)";

            var hint = Label(panel.transform, new Vector2(0.5f, 1f), new Vector2(0f, -84f),
                             new Vector2(1180f, 44f), 19, TextAlignmentOptions.Center);
            hint.color = new Color(1f, 1f, 1f, 0.7f);
            hint.text = "GLB-Dateien in den Modell-Ordner legen · F7 schließt · < > wechselt das Modell";

            float y = -140f;
            foreach (string id in GameConstants.AllCharacterIds)
            {
                rows.Add(BuildRow(panel.transform, id, y));
                y -= 66f;
            }

            // Fußleiste
            float footerY = y - 26f;
            Button(panel.transform, new Vector2(0.5f, 1f), new Vector2(-440f, footerY), new Vector2(280f, 60f),
                   "AUTO-ZUORDNEN", PennerPalette.BloodRed, () =>
                   {
                       int n = GlbLibrary.AutoAssignAll();
                       header.text = $"MODELLE (GLB) — {n} automatisch zugeordnet";
                       Rescan();
                   });
            Button(panel.transform, new Vector2(0.5f, 1f), new Vector2(-140f, footerY), new Vector2(280f, 60f),
                   "ÜBERNEHMEN", PennerPalette.WarmOrange, () =>
                   {
                       GlbModelLoader.RefreshAll();
                       header.text = "MODELLE (GLB) — übernommen";
                   });
            Button(panel.transform, new Vector2(0.5f, 1f), new Vector2(160f, footerY), new Vector2(280f, 60f),
                   "ALLE LEEREN", new Color(0.25f, 0.25f, 0.3f), () =>
                   {
                       foreach (string id in GameConstants.AllCharacterIds) GlbLibrary.Clear(id);
                       GlbModelLoader.RefreshAll();
                       Rescan();
                   });
            Button(panel.transform, new Vector2(0.5f, 1f), new Vector2(460f, footerY), new Vector2(280f, 60f),
                   "ORDNER ZEIGEN", PennerPalette.NeonBlue, () =>
                   {
                       string folder = GlbLibrary.UserFolder;
                       header.text = folder;
                       Debug.Log($"[Penner Kombat] Modell-Ordner: {folder}");
#if UNITY_EDITOR || UNITY_STANDALONE
                       Application.OpenURL("file://" + folder);
#endif
                   });
        }

        Row BuildRow(Transform parent, string id, float y)
        {
            var row = new Row { id = id };

            row.label = Label(parent, new Vector2(0f, 1f), new Vector2(40f, y),
                              new Vector2(240f, 46f), 24, TextAlignmentOptions.Left);
            row.label.text = id;

            Button(parent, new Vector2(0f, 1f), new Vector2(290f, y), new Vector2(56f, 46f),
                   "<", new Color(0.2f, 0.2f, 0.26f), () => Cycle(row, -1), pivotTopLeft: true);
            Button(parent, new Vector2(0f, 1f), new Vector2(354f, y), new Vector2(56f, 46f),
                   ">", new Color(0.2f, 0.2f, 0.26f), () => Cycle(row, +1), pivotTopLeft: true);

            row.value = Label(parent, new Vector2(0f, 1f), new Vector2(426f, y),
                              new Vector2(430f, 46f), 22, TextAlignmentOptions.Left);
            row.value.color = new Color(1f, 1f, 1f, 0.85f);

            // Feinjustage
            Button(parent, new Vector2(0f, 1f), new Vector2(870f, y), new Vector2(56f, 46f),
                   "−", new Color(0.2f, 0.2f, 0.26f), () => Nudge(row, -0.05f), pivotTopLeft: true);
            Button(parent, new Vector2(0f, 1f), new Vector2(934f, y), new Vector2(56f, 46f),
                   "+", new Color(0.2f, 0.2f, 0.26f), () => Nudge(row, +0.05f), pivotTopLeft: true);
            Button(parent, new Vector2(0f, 1f), new Vector2(998f, y), new Vector2(90f, 46f),
                   "↻ 90°", new Color(0.2f, 0.2f, 0.26f), () =>
                   {
                       GlbLibrary.SetYaw(row.id, Mathf.Repeat(GlbLibrary.GetYaw(row.id) + 90f, 360f));
                       Refresh(row);
                   }, pivotTopLeft: true);
            Button(parent, new Vector2(0f, 1f), new Vector2(1102f, y), new Vector2(90f, 46f),
                   "LEER", new Color(0.35f, 0.12f, 0.12f), () =>
                   {
                       GlbLibrary.Clear(row.id);
                       Refresh(row);
                   }, pivotTopLeft: true);

            return row;
        }

        void Cycle(Row row, int direction)
        {
            if (files.Count == 0) { Rescan(); if (files.Count == 0) return; }

            string current = GlbLibrary.GetAssigned(row.id);
            int index = current != null ? files.IndexOf(current) : -1;
            index += direction;

            if (index < -1) index = files.Count - 1;
            if (index >= files.Count) index = -1;      // -1 = Platzhalter (kein Modell)

            if (index < 0) GlbLibrary.Clear(row.id);
            else GlbLibrary.Assign(row.id, files[index]);
            Refresh(row);
        }

        void Nudge(Row row, float delta)
        {
            float scale = GlbLibrary.GetScale(row.id);
            if (scale <= 0.0001f) scale = 1f;          // 0 = automatisch → ab jetzt manuell
            GlbLibrary.SetScale(row.id, Mathf.Max(0.05f, scale + delta));
            Refresh(row);
        }

        public void Rescan()
        {
            files = GlbLibrary.FindAll();
            foreach (var row in rows) Refresh(row);
            if (files.Count == 0)
                header.text = GlbModelLoader.Available
                    ? "MODELLE (GLB) — keine Dateien gefunden (Ordner zeigen)"
                    : "MODELLE (GLB) — glTFast fehlt: Tools → Penner Kombat → glTFast installieren";
        }

        void Refresh(Row row)
        {
            string path = GlbLibrary.GetAssigned(row.id);
            float scale = GlbLibrary.GetScale(row.id);
            string scaleText = scale > 0.0001f ? $"  ×{scale:0.00}" : "  (auto)";
            float yaw = GlbLibrary.GetYaw(row.id);
            string yawText = Mathf.Abs(yaw) > 0.5f ? $"  {yaw:0}°" : "";

            row.value.text = path == null
                ? "— Platzhalter (Kapsel)"
                : Path.GetFileName(path) + scaleText + yawText;
            row.value.color = path == null ? new Color(1f, 1f, 1f, 0.45f) : PennerPalette.Gold;
        }

        // ---------- kleine UI-Helfer ----------

        static GameObject NewPanel(Transform parent, Vector2 size, Color color)
        {
            var go = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;
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
            rt.pivot = new Vector2(anchor.x, anchor.y);
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

        static void Button(Transform parent, Vector2 anchor, Vector2 offset, Vector2 size, string caption,
                           Color color, UnityEngine.Events.UnityAction onClick, bool pivotTopLeft = false)
        {
            var go = new GameObject("Btn_" + caption, typeof(RectTransform), typeof(Image), typeof(UnityEngine.UI.Button));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivotTopLeft ? new Vector2(0f, 1f) : new Vector2(0.5f, 1f);
            rt.sizeDelta = size;
            rt.anchoredPosition = offset;
            go.GetComponent<Image>().color = color;

            var label = Label(go.transform, new Vector2(0.5f, 0.5f), Vector2.zero, size, 22, TextAlignmentOptions.Center);
            label.text = caption;

            go.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(onClick);
        }
    }
}
