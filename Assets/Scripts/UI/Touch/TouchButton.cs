using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PennerKombat
{
    /// <summary>
    /// (2) On-Screen-Aktionsbutton — docs/TOUCH.md §2.2.
    /// Kann als Tap-, Halte- oder Toggle-Button arbeiten, gibt haptisches und
    /// visuelles Feedback, meldet sich bei <see cref="AntiGhosting"/> an und
    /// schreibt in <see cref="VirtualInput"/> sowie den <see cref="InputBuffer"/>.
    /// </summary>
    public class TouchButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [Header("Belegung")]
        public int playerIndex = 0;
        public VButton button = VButton.Light;
        public bool isToggle = false;
        [Tooltip("Gedrückt-Halten wird durchgereicht (z. B. Block).")]
        public bool isHoldable = false;

        [Header("Visuals")]
        public Image image;
        public Color baseColor = Color.white;
        [Range(0f, 1f)] public float opacity = 0.55f;

        [Header("Feedback")]
        public bool vibrateOnPress = true;
        public float vibrationDuration = 0.05f;

        public event Action<VButton> OnPressed;
        public event Action<VButton> OnReleased;

        public bool IsPressed { get; private set; }

        private int pointerId = -1;
        private bool interactable = true;

        void Awake()
        {
            if (image == null) image = GetComponent<Image>();
            Repaint();
        }

        public void Init(int playerIndex, VButton button, bool holdMode, Image image, Color color, float opacity)
        {
            this.playerIndex = playerIndex;
            this.button = button;
            this.isHoldable = holdMode;
            this.image = image;
            this.baseColor = color;
            this.opacity = opacity;
            Repaint();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!interactable) return;
            if (AntiGhosting.Instance != null && !AntiGhosting.Instance.Claim(eventData.pointerId, name))
                return;

            pointerId = eventData.pointerId;

            if (isToggle)
            {
                IsPressed = !IsPressed;
                if (IsPressed) Press(); else Release();
                return;
            }

            IsPressed = true;
            Press();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!interactable || eventData.pointerId != pointerId) return;
            AntiGhosting.Instance?.Release(pointerId);
            pointerId = -1;

            if (isToggle) return;      // Toggle löst nur bei Down um
            IsPressed = false;
            Release();
        }

        void Press()
        {
            VirtualInput.Press(playerIndex, button);
            InputBuffer.Instance?.Buffer(playerIndex, button);
            if (vibrateOnPress && TouchSettings.Current.vibration) Haptics.Tap(vibrationDuration);
            OnPressed?.Invoke(button);
            Repaint();
        }

        void Release()
        {
            VirtualInput.Release(playerIndex, button);
            OnReleased?.Invoke(button);
            Repaint();
        }

        public void SetInteractable(bool value)
        {
            interactable = value;
            if (!value && IsPressed) { IsPressed = false; Release(); }
            Repaint();
        }

        void Repaint()
        {
            if (image == null) return;
            if (!interactable) { image.color = new Color(0.5f, 0.5f, 0.5f, opacity * 0.4f); return; }
            image.color = IsPressed
                ? baseColor.WithAlpha(Mathf.Min(1f, opacity * 1.7f))
                : baseColor.WithAlpha(opacity * 0.8f);
        }

        public void ApplySettings(TouchSettings s)
        {
            if (s == null) return;
            transform.localScale = Vector3.one * s.buttonSize;
            opacity = s.opacity;
            vibrateOnPress = s.vibration;
            Repaint();
        }

        void OnDisable()
        {
            if (IsPressed) { IsPressed = false; VirtualInput.Release(playerIndex, button); }
        }
    }

    /// <summary>Plattformunabhängiges haptisches Feedback (No-Op am Desktop).</summary>
    public static class Haptics
    {
        public static void Tap(float duration = 0.05f)
        {
#if UNITY_ANDROID || UNITY_IOS
            if (!Application.isEditor) Handheld.Vibrate();
#endif
        }
    }
}
