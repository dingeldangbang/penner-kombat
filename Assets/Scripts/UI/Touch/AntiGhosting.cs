using System.Collections.Generic;
using UnityEngine;

namespace PennerKombat
{
    /// <summary>
    /// (6) Anti-Ghosting — docs/TOUCH.md §6.2.
    /// Jeder Finger (pointerId) darf nur genau ein Bedienelement steuern.
    /// Verhindert, dass ein über mehrere Buttons wischender Finger sie alle
    /// gleichzeitig auslöst.
    /// </summary>
    public class AntiGhosting : MonoBehaviour
    {
        public static AntiGhosting Instance { get; private set; }

        private readonly Dictionary<int, string> fingers = new Dictionary<int, string>();

        public static AntiGhosting Ensure()
        {
            if (Instance == null)
            {
                var go = new GameObject("~AntiGhosting");
                Instance = go.AddComponent<AntiGhosting>();
            }
            return Instance;
        }

        void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(this); return; }
        }

        /// <summary>Belegt einen Finger für ein Element. False, wenn er schon vergeben ist.</summary>
        public bool Claim(int pointerId, string elementId)
        {
            if (fingers.TryGetValue(pointerId, out string owner))
                return owner == elementId;
            fingers[pointerId] = elementId;
            return true;
        }

        public void Release(int pointerId)
        {
            if (pointerId >= -10) fingers.Remove(pointerId);
        }

        public bool IsElementActive(string elementId) => fingers.ContainsValue(elementId);
        public int ActiveFingers => fingers.Count;

        public void Clear() => fingers.Clear();
    }
}
