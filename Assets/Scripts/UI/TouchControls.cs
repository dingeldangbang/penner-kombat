using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace PennerKombat
{
    /// <summary>
    /// Baut das komplette Touch-Layout zur Laufzeit auf: virtueller Joystick
    /// links, Aktionsbuttons rechts, dazu Gesten-Erkennung, Anti-Ghosting,
    /// Eingabepuffer, Tutorial und Debug-Overlay.
    ///
    /// Alle Grafiken sind prozedural — es wird kein Prefab und kein Sprite
    /// benötigt. Layout, Größen und Deckkraft kommen aus
    /// <see cref="TouchSettings"/>; der Layout-Editor kann Positionen
    /// überschreiben. Belegung und Aufbau: docs/TOUCH.md.
    /// </summary>
    public class TouchControls : MonoBehaviour
    {
        public static TouchControls Instance { get; private set; }

        [Header("Setup")]
        public int playerIndex = 0;
        public bool showTutorial = true;
        public bool createDebugOverlay = true;

        private Canvas canvas;
        private RectTransform root;
        private VirtualJoystick joystick;
        private readonly List<TouchButton> buttons = new List<TouchButton>();

        /// <summary>Beschreibung eines Buttons im Layout.</summary>
        private struct Slot
        {
            public string id;
            public VButton button;
            public Vector2 pos;      // Offset vom rechten unteren Rand
            public Color color;
            public string label;
            public bool hold;
        }

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
            AntiGhosting.Ensure();
            InputBuffer.Ensure();
            Build();

            var gestures = TouchGestureDetector.Ensure();
            gestures.playerIndex = playerIndex;

            TouchInputManager.Ensure().Register(joystick, buttons);
            TouchSettings.Current.Apply();

            if (showTutorial) TouchTutorial.Ensure();
            if (createDebugOverlay) TouchDebug.Ensure();
        }

        void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null) return;
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        // ------------------------------------------------------------------
        //  Aufbau
        // ------------------------------------------------------------------

        void Build()
        {
            canvas = gameObject.GetComponent<Canvas>() ?? gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 950;

            var scaler = gameObject.GetComponent<CanvasScaler>() ?? gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            if (gameObject.GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();

            var rootGo = new GameObject("Layout", typeof(RectTransform));
            rootGo.transform.SetParent(transform, false);
            root = (RectTransform)rootGo.transform;
            root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero; root.offsetMax = Vector2.zero;

            var s = TouchSettings.Current;
            bool mirror = s.leftHanded;

            BuildJoystick(mirror, s);
            foreach (var slot in LayoutFor(s.layout))
                BuildButton(slot, mirror, s);
        }

        void BuildJoystick(bool mirror, TouchSettings s)
        {
            var anchor = new Vector2(mirror ? 1f : 0f, 0f);
            var rt = NewRect("Joystick", anchor, new Vector2(260f, 260f),
                             new Vector2(mirror ? -220f : 220f, 200f));

            var bg = rt.gameObject.AddComponent<Image>();
            bg.sprite = CircleSprite();
            bg.color = PennerPalette.NightBlue.WithAlpha(s.opacity * 0.6f);

            var knobRect = NewRect("Knob", new Vector2(0.5f, 0.5f), new Vector2(110f, 110f), Vector2.zero, rt);
            var knob = knobRect.gameObject.AddComponent<Image>();
            knob.sprite = CircleSprite();
            knob.color = PennerPalette.WarmOrange.WithAlpha(s.opacity);
            knob.raycastTarget = false;

            joystick = rt.gameObject.AddComponent<VirtualJoystick>();
            joystick.playerIndex = playerIndex;
            joystick.background = bg;
            joystick.handle = knobRect;
            joystick.joystickRadius = 130f * s.joystickSize;
            joystick.sensitivity = s.sensitivity;

            var drag = rt.gameObject.AddComponent<DragElement>();
            drag.elementId = "Joystick";
            ApplySavedPosition(rt, "Joystick", s);
        }

        void BuildButton(Slot slot, bool mirror, TouchSettings s)
        {
            var anchor = new Vector2(mirror ? 0f : 1f, 0f);
            Vector2 pos = mirror ? new Vector2(-slot.pos.x, slot.pos.y) : slot.pos;

            var rt = NewRect(slot.id, anchor, new Vector2(140f, 140f), pos);

            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = CircleSprite();
            img.color = slot.color.WithAlpha(s.opacity * 0.8f);

            var label = new GameObject("Label", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            label.transform.SetParent(rt, false);
            var lrt = label.rectTransform;
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            label.text = slot.label;
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = slot.label.Length > 2 ? 26f : 50f;
            label.color = PennerPalette.NightBlue;
            label.raycastTarget = false;

            var tb = rt.gameObject.AddComponent<TouchButton>();
            tb.Init(playerIndex, slot.button, slot.hold, img, slot.color, s.opacity);
            tb.isHoldable = slot.hold;
            buttons.Add(tb);

            var drag = rt.gameObject.AddComponent<DragElement>();
            drag.elementId = slot.id;
            ApplySavedPosition(rt, slot.id, s);
        }

        /// <summary>Vier Layout-Vorlagen aus der Spec (§5).</summary>
        IEnumerable<Slot> LayoutFor(string layout)
        {
            var light   = new Slot { id = "Light",     button = VButton.Light,     label = "□",     color = PennerPalette.Pure };
            var heavy   = new Slot { id = "Heavy",     button = VButton.Heavy,     label = "△",     color = PennerPalette.Gold };
            var spec1   = new Slot { id = "Special1",  button = VButton.Special1,  label = "○",     color = PennerPalette.BloodRed };
            var jump    = new Slot { id = "Jump",      button = VButton.Jump,      label = "✕",     color = PennerPalette.NeonBlue };
            var block   = new Slot { id = "Block",     button = VButton.Block,     label = "BLOCK", color = PennerPalette.Earth,     hold = true };
            var spec2   = new Slot { id = "Special2",  button = VButton.Special2,  label = "S2",    color = PennerPalette.PoisonGrn };
            var xray    = new Slot { id = "FatalBlow", button = VButton.FatalBlow, label = "X-RAY", color = PennerPalette.BloodRed };

            switch (layout)
            {
                case "Simple":      // nur die vier Grundtasten, größer verteilt
                    light.pos = new Vector2(-300f, 170f);
                    heavy.pos = new Vector2(-160f, 300f);
                    jump.pos  = new Vector2(-160f, 60f);
                    block.pos = new Vector2(-440f, 90f);
                    return new[] { light, heavy, jump, block };

                case "Fighting":    // enger Diamant für Kombos
                    light.pos = new Vector2(-270f, 150f);
                    heavy.pos = new Vector2(-165f, 245f);
                    spec1.pos = new Vector2(-165f, 60f);
                    jump.pos  = new Vector2(-60f,  150f);
                    block.pos = new Vector2(-390f, 70f);
                    spec2.pos = new Vector2(-60f,  265f);
                    xray.pos  = new Vector2(-270f, 300f);
                    return new[] { light, heavy, spec1, jump, block, spec2, xray };

                default:            // Standard / LeftHanded (wird gespiegelt)
                    light.pos = new Vector2(-280f, 160f);
                    heavy.pos = new Vector2(-170f, 265f);
                    spec1.pos = new Vector2(-170f, 65f);
                    jump.pos  = new Vector2(-60f,  160f);
                    block.pos = new Vector2(-410f, 80f);
                    spec2.pos = new Vector2(-60f,  285f);
                    xray.pos  = new Vector2(-280f, 320f);
                    return new[] { light, heavy, spec1, jump, block, spec2, xray };
            }
        }

        void ApplySavedPosition(RectTransform rt, string id, TouchSettings s)
        {
            if (s.TryGetPosition(id, out Vector2 pos)) rt.anchoredPosition = pos;
        }

        RectTransform NewRect(string name, Vector2 anchor, Vector2 size, Vector2 offset, Transform parent = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent != null ? parent : root, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = offset;
            return rt;
        }

        // ------------------------------------------------------------------
        //  Steuerung von außen
        // ------------------------------------------------------------------

        /// <summary>Baut das Layout neu auf (nach Layout-Wechsel/Linkshänder).</summary>
        public void Rebuild()
        {
            buttons.Clear();
            joystick = null;
            if (root != null) Destroy(root.gameObject);
            Build();
            TouchInputManager.Instance?.Register(joystick, buttons);
        }

        public void ApplyLayout(TouchSettings s)
        {
            joystick?.ApplySettings(s);
            foreach (var b in buttons) b?.ApplySettings(s);
        }

        public void SetVisible(bool visible)
        {
            if (root != null) root.gameObject.SetActive(visible);
            if (!visible) VirtualInput.Clear();
        }

        void OnDisable() => VirtualInput.Clear();

        // ------------------------------------------------------------------
        //  Prozedurale Kreis-Grafik
        // ------------------------------------------------------------------

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
    }
}
