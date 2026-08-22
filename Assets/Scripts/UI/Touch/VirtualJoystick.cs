using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PennerKombat
{
    /// <summary>
    /// (1) Virtueller Joystick für die Bewegung — docs/TOUCH.md §2.1.
    /// Unterstützt festen, dynamischen (folgt dem Finger) und Snap-Modus,
    /// hat eine Totzone, Empfindlichkeit und meldet den normalisierten Vektor
    /// per Event sowie an <see cref="VirtualInput"/>.
    /// </summary>
    public class VirtualJoystick : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        [Header("Joystick")]
        public int playerIndex = 0;
        public float joystickRadius = 130f;
        [Range(0f, 0.5f)] public float deadZone = 0.18f;
        [Range(0.25f, 2f)] public float sensitivity = 1f;

        [Tooltip("Joystick springt beim Antippen an die Fingerposition.")]
        public bool snapToFinger = false;
        [Tooltip("Joystick bleibt an der zuletzt berührten Stelle stehen.")]
        public bool isDynamic = false;

        [Header("Visuals")]
        public Image background;
        public RectTransform handle;
        public Color normalColor = new Color(1f, 1f, 1f, 0.35f);
        public Color pressedColor = new Color(1f, 1f, 1f, 0.7f);

        public event Action<Vector2> OnMove;
        public event Action OnRelease;

        public Vector2 Value { get; private set; }
        public bool IsPressed { get; private set; }

        private RectTransform self;
        private Vector2 homePosition;
        private int pointerId = -1;

        void Awake()
        {
            self = (RectTransform)transform;
            homePosition = self.anchoredPosition;
            if (background == null) background = GetComponent<Image>();
            if (background != null) background.color = normalColor;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (IsPressed) return;
            if (AntiGhosting.Instance != null && !AntiGhosting.Instance.Claim(eventData.pointerId, "joystick"))
                return;

            IsPressed = true;
            pointerId = eventData.pointerId;
            if (background != null) background.color = pressedColor;

            if (snapToFinger || isDynamic)
            {
                var parent = self.parent as RectTransform;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        parent, eventData.position, eventData.pressEventCamera, out Vector2 local))
                    self.anchoredPosition = local;
            }

            Apply(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!IsPressed || eventData.pointerId != pointerId) return;
            Apply(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!IsPressed || eventData.pointerId != pointerId) return;

            IsPressed = false;
            AntiGhosting.Instance?.Release(pointerId);
            pointerId = -1;

            Value = Vector2.zero;
            if (handle != null) handle.anchoredPosition = Vector2.zero;
            if (background != null) background.color = normalColor;
            if (!isDynamic) self.anchoredPosition = homePosition;

            VirtualInput.SetAxis(playerIndex, Vector2.zero);
            OnMove?.Invoke(Vector2.zero);
            OnRelease?.Invoke();
        }

        void Apply(PointerEventData eventData)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    self, eventData.position, eventData.pressEventCamera, out Vector2 local))
                return;

            Vector2 clamped = Vector2.ClampMagnitude(local, joystickRadius);
            if (handle != null) handle.anchoredPosition = clamped;

            Vector2 axis = (clamped / joystickRadius) * sensitivity;
            axis = Vector2.ClampMagnitude(axis, 1f);
            if (axis.magnitude < deadZone) axis = Vector2.zero;

            Value = axis;
            VirtualInput.SetAxis(playerIndex, axis);
            OnMove?.Invoke(axis);
        }

        /// <summary>Größe/Deckkraft aus den Touch-Einstellungen übernehmen.</summary>
        public void ApplySettings(TouchSettings s)
        {
            if (s == null) return;
            transform.localScale = Vector3.one * s.joystickSize;
            joystickRadius = 130f * s.joystickSize;
            sensitivity = s.sensitivity;
            normalColor = new Color(1f, 1f, 1f, s.opacity * 0.6f);
            pressedColor = new Color(1f, 1f, 1f, Mathf.Min(1f, s.opacity * 1.3f));
            if (background != null && !IsPressed) background.color = normalColor;
        }

        void OnDisable()
        {
            IsPressed = false;
            VirtualInput.SetAxis(playerIndex, Vector2.zero);
        }
    }
}
