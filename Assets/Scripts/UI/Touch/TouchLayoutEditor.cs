using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace PennerKombat
{
    /// <summary>
    /// (5) Layout-Editor — docs/TOUCH.md §5.
    /// Schaltet in einen Bearbeitungsmodus, in dem sich Joystick und Buttons
    /// per Finger verschieben lassen; Größe und Deckkraft hängen an Reglern.
    /// Speichert die Positionen in <see cref="TouchSettings"/>.
    /// </summary>
    public class TouchLayoutEditor : MonoBehaviour
    {
        public static TouchLayoutEditor Instance { get; private set; }

        [Header("Regler (optional)")]
        public Slider joystickSizeSlider;
        public Slider buttonSizeSlider;
        public Slider opacitySlider;
        public Toggle vibrationToggle;
        public Toggle leftHandedToggle;
        public TMP_Dropdown layoutDropdown;

        [Header("Buttons (optional)")]
        public Button toggleEditButton;
        public Button saveButton;
        public Button resetButton;

        public bool IsEditing { get; private set; }

        void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(this); return; }
        }

        void Start()
        {
            var s = TouchSettings.Current;

            if (joystickSizeSlider != null)
            {
                joystickSizeSlider.minValue = 0.6f; joystickSizeSlider.maxValue = 1.8f;
                joystickSizeSlider.value = s.joystickSize;
                joystickSizeSlider.onValueChanged.AddListener(v => { s.joystickSize = v; s.Apply(); });
            }
            if (buttonSizeSlider != null)
            {
                buttonSizeSlider.minValue = 0.6f; buttonSizeSlider.maxValue = 1.8f;
                buttonSizeSlider.value = s.buttonSize;
                buttonSizeSlider.onValueChanged.AddListener(v => { s.buttonSize = v; s.Apply(); });
            }
            if (opacitySlider != null)
            {
                opacitySlider.minValue = 0.15f; opacitySlider.maxValue = 1f;
                opacitySlider.value = s.opacity;
                opacitySlider.onValueChanged.AddListener(v => { s.opacity = v; s.Apply(); });
            }
            if (vibrationToggle != null)
            {
                vibrationToggle.isOn = s.vibration;
                vibrationToggle.onValueChanged.AddListener(v => { s.vibration = v; s.Apply(); });
            }
            if (leftHandedToggle != null)
            {
                leftHandedToggle.isOn = s.leftHanded;
                leftHandedToggle.onValueChanged.AddListener(v =>
                {
                    s.leftHanded = v;
                    s.Save();
                    TouchControls.Instance?.Rebuild();
                });
            }
            if (layoutDropdown != null)
            {
                layoutDropdown.ClearOptions();
                layoutDropdown.AddOptions(new System.Collections.Generic.List<string>
                    { "Standard", "Fighting", "Simple", "LeftHanded" });
                layoutDropdown.onValueChanged.AddListener(i =>
                {
                    s.layout = layoutDropdown.options[i].text;
                    s.leftHanded = s.layout == "LeftHanded";
                    s.Save();
                    TouchControls.Instance?.Rebuild();
                });
            }

            toggleEditButton?.onClick.AddListener(ToggleEditMode);
            saveButton?.onClick.AddListener(SaveLayout);
            resetButton?.onClick.AddListener(ResetLayout);
        }

        public void ToggleEditMode()
        {
            IsEditing = !IsEditing;
            foreach (var drag in FindObjectsOfType<DragElement>(true))
                drag.enabled = IsEditing;

            if (TouchControls.Instance != null)
                FloatingText.Show(Vector3.up * 2f,
                    IsEditing ? "LAYOUT VERSCHIEBEN" : "LAYOUT GESPEICHERT",
                    PennerPalette.Gold, 1.2f);

            if (!IsEditing) SaveLayout();
        }

        public void SaveLayout()
        {
            var s = TouchSettings.Current;
            foreach (var drag in FindObjectsOfType<DragElement>(true))
                s.SetPosition(drag.elementId, ((RectTransform)drag.transform).anchoredPosition);
            s.Save();
        }

        public void ResetLayout()
        {
            TouchSettings.Current.ResetToDefaults();
            TouchControls.Instance?.Rebuild();
        }
    }

    /// <summary>Verschiebbares Bedienelement im Layout-Editor.</summary>
    public class DragElement : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public string elementId = "element";

        private RectTransform rect;
        private Vector2 startPos;

        void Awake()
        {
            rect = (RectTransform)transform;
            enabled = false;    // nur im Editiermodus aktiv
        }

        public void OnBeginDrag(PointerEventData eventData) => startPos = rect.anchoredPosition;

        public void OnDrag(PointerEventData eventData)
        {
            var canvas = GetComponentInParent<Canvas>();
            float scale = canvas != null ? canvas.scaleFactor : 1f;
            rect.anchoredPosition += eventData.delta / Mathf.Max(0.01f, scale);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            TouchSettings.Current.SetPosition(elementId, rect.anchoredPosition);
        }
    }
}
