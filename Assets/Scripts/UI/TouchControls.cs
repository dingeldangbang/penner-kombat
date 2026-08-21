using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace PennerKombat
{
    /// <summary>
    /// On-Screen-Steuerung für Android/Touch: virtueller Stick links,
    /// Buttons rechts (□ Leicht, △ Schwer, ○ Spezial 1, ✕ Sprung, Block, S2, X-Ray).
    /// Baut sich komplett zur Laufzeit auf — keine Prefabs nötig — und schreibt
    /// in <see cref="VirtualInput"/>, das <see cref="FighterInput"/> ausliest.
    ///
    /// Wird auf Touch-Plattformen automatisch vom <see cref="Bootstrapper"/>
    /// erzeugt; am Desktop nur, wenn <c>forceOnDesktop</c> gesetzt ist.
    /// </summary>
    public class TouchControls : MonoBehaviour
    {
        public static TouchControls Instance { get; private set; }

        [Header("Setup")]
        public int playerIndex = 0;
        public bool forceOnDesktop = false;
        [Range(0.3f, 1f)] public float opacity = 0.55f;
        public float buttonSize = 120f;
        public float stickRadius = 130f;

        private Canvas canvas;

        public static TouchControls Ensure(bool force = false)
        {
            if (Instance != null) return Instance;

            bool touchDevice = Application.isMobilePlatform || SystemInfo.deviceType == DeviceType.Handheld;
            if (!touchDevice && !force) return null;

            var go = new GameObject("~TouchControls");
            Instance = go.AddComponent<TouchControls>();
            return Instance;
        }

        void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
        }

        void Start()
        {
            EnsureEventSystem();
            Build();
        }

        void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null) return;
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        void Build()
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 950;      // über HUD, unter Pause
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();

            // --- Stick links unten ---
            var stickRoot = NewRect("Stick", new Vector2(0f, 0f), new Vector2(220f, 220f),
                                    new Vector2(stickRadius + 60f, stickRadius + 40f));
            var bg = stickRoot.gameObject.AddComponent<Image>();
            bg.color = PennerPalette.NightBlue.WithAlpha(opacity * 0.6f);
            bg.sprite = CircleSprite();
            var stick = stickRoot.gameObject.AddComponent<TouchStick>();

            var knobRect = NewRect("Knob", new Vector2(0.5f, 0.5f), new Vector2(90f, 90f), Vector2.zero, stickRoot);
            var knob = knobRect.gameObject.AddComponent<Image>();
            knob.color = PennerPalette.WarmOrange.WithAlpha(opacity);
            knob.sprite = CircleSprite();
            knob.raycastTarget = false;

            stick.Init(playerIndex, knobRect, stickRadius);

            // --- Buttons rechts unten (Diamant-Layout wie am Pad) ---
            AddButton("Leicht",  VButton.Light,     new Vector2(-260f, 150f), PennerPalette.Pure,      "□");
            AddButton("Schwer",  VButton.Heavy,     new Vector2(-150f, 250f), PennerPalette.Gold,      "△");
            AddButton("Spezial", VButton.Special1,  new Vector2(-150f,  60f), PennerPalette.BloodRed,  "○");
            AddButton("Sprung",  VButton.Jump,      new Vector2(-40f,  150f), PennerPalette.NeonBlue,  "✕");

            // --- Sekundärleiste ---
            AddButton("Block",    VButton.Block,     new Vector2(-380f,  70f), PennerPalette.Earth,     "BLOCK", hold: true);
            AddButton("Spezial2", VButton.Special2,  new Vector2(-40f,  270f), PennerPalette.PoisonGrn, "S2");
            AddButton("X-Ray",    VButton.FatalBlow, new Vector2(-260f, 300f), PennerPalette.BloodRed,  "X-RAY");
        }

        void AddButton(string name, VButton button, Vector2 offsetFromBottomRight,
                       Color color, string label, bool hold = false)
        {
            var rect = NewRect(name, new Vector2(1f, 0f), new Vector2(buttonSize, buttonSize), offsetFromBottomRight);
            var img = rect.gameObject.AddComponent<Image>();
            img.color = color.WithAlpha(opacity * 0.8f);
            img.sprite = CircleSprite();

            var txt = new GameObject("Label", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            txt.transform.SetParent(rect, false);
            var tr = txt.rectTransform;
            tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
            tr.offsetMin = Vector2.zero; tr.offsetMax = Vector2.zero;
            txt.text = label;
            txt.alignment = TextAlignmentOptions.Center;
            txt.fontSize = label.Length > 2 ? 26f : 46f;
            txt.color = PennerPalette.NightBlue;
            txt.raycastTarget = false;

            var tb = rect.gameObject.AddComponent<TouchButton>();
            tb.Init(playerIndex, button, hold, img, color, opacity);
        }

        RectTransform NewRect(string name, Vector2 anchor, Vector2 size, Vector2 offset, Transform parent = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent != null ? parent : transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            // Offset relativ zum Anker: bei rechtem Anker negative x-Werte
            rt.anchoredPosition = offset;
            return rt;
        }

        // --- prozedurale Kreis-Sprite (kein Art-Asset nötig) ---
        private static Sprite circleSprite;

        static Sprite CircleSprite()
        {
            if (circleSprite != null) return circleSprite;

            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var c = new Vector2(size * 0.5f, size * 0.5f);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), c) / (size * 0.5f);
                    float a = d > 1f ? 0f : Mathf.SmoothStep(1f, 0f, Mathf.InverseLerp(0.86f, 1f, d));
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            tex.Apply();
            circleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            return circleSprite;
        }

        void OnDisable() => VirtualInput.Clear();
    }

    /// <summary>Einzelner On-Screen-Button (Tap oder Halten).</summary>
    public class TouchButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        private int player;
        private VButton button;
        private bool holdMode;
        private Image image;
        private Color baseColor;
        private float opacity;

        public void Init(int player, VButton button, bool holdMode, Image image, Color color, float opacity)
        {
            this.player = player;
            this.button = button;
            this.holdMode = holdMode;
            this.image = image;
            this.baseColor = color;
            this.opacity = opacity;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            VirtualInput.Press(player, button);
            if (image != null) image.color = baseColor.WithAlpha(Mathf.Min(1f, opacity * 1.6f));
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            VirtualInput.Release(player, button);
            if (image != null) image.color = baseColor.WithAlpha(opacity * 0.8f);
        }

        void OnDisable()
        {
            VirtualInput.Release(player, button);
        }
    }

    /// <summary>Virtueller Analogstick: schreibt eine normalisierte Achse in VirtualInput.</summary>
    public class TouchStick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        private int player;
        private RectTransform knob;
        private RectTransform self;
        private float radius;

        public void Init(int player, RectTransform knob, float radius)
        {
            this.player = player;
            this.knob = knob;
            this.radius = radius;
            self = (RectTransform)transform;
        }

        public void OnPointerDown(PointerEventData eventData) => OnDrag(eventData);

        public void OnDrag(PointerEventData eventData)
        {
            if (self == null) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                self, eventData.position, eventData.pressEventCamera, out Vector2 local);

            Vector2 clamped = Vector2.ClampMagnitude(local, radius);
            if (knob != null) knob.anchoredPosition = clamped;

            Vector2 axis = clamped / radius;
            // kleine Totzone gegen Zittern
            if (axis.magnitude < 0.18f) axis = Vector2.zero;
            VirtualInput.SetAxis(player, axis);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (knob != null) knob.anchoredPosition = Vector2.zero;
            VirtualInput.SetAxis(player, Vector2.zero);
        }

        void OnDisable() => VirtualInput.SetAxis(player, Vector2.zero);
    }
}
