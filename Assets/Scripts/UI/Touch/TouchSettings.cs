using System;
using System.Collections.Generic;
using UnityEngine;

namespace PennerKombat
{
    /// <summary>
    /// (9) Touch-Einstellungen — docs/TOUCH.md §10.
    /// Größen, Deckkraft, Vibration, Linkshänder-Layout, Empfindlichkeit und
    /// Puffergröße. Wird als JSON in den PlayerPrefs gehalten, damit es auch
    /// ohne <see cref="SaveSystem"/> funktioniert, und beim Laden auf die
    /// aktive Steuerung angewendet.
    /// </summary>
    [Serializable]
    public class TouchSettings
    {
        public const string PrefsKey = "pk_touch_settings";

        [Range(0.6f, 1.8f)] public float joystickSize = 1f;
        [Range(0.6f, 1.8f)] public float buttonSize = 1f;
        [Range(0.15f, 1f)]  public float opacity = 0.55f;
        [Range(0.25f, 2f)]  public float sensitivity = 1f;
        [Range(0.05f, 0.3f)] public float bufferWindow = 0.15f;

        public bool vibration = true;
        public bool gestureFeedback = true;
        public bool leftHanded = false;
        public bool tutorialSeen = false;

        /// <summary>Layout-Vorlage: Standard, Fighting, Simple, LeftHanded.</summary>
        public string layout = "Standard";

        /// <summary>Vom Layout-Editor verschobene Positionen (Elementname → Offset).</summary>
        public List<string> customPositions = new List<string>();

        private static TouchSettings current;

        public static TouchSettings Current
        {
            get
            {
                if (current == null) current = Load();
                return current;
            }
        }

        public static TouchSettings Load()
        {
            string json = PlayerPrefs.GetString(PrefsKey, "");
            if (string.IsNullOrEmpty(json)) return new TouchSettings();
            try { return JsonUtility.FromJson<TouchSettings>(json) ?? new TouchSettings(); }
            catch { return new TouchSettings(); }
        }

        public void Save()
        {
            PlayerPrefs.SetString(PrefsKey, JsonUtility.ToJson(this));
            PlayerPrefs.Save();
            Apply();
        }

        /// <summary>Wendet die Werte auf die laufende Steuerung an.</summary>
        public void Apply()
        {
            current = this;
            TouchInputManager.Instance?.ApplySettings(this);
            TouchControls.Instance?.ApplyLayout(this);
        }

        public void ResetToDefaults()
        {
            joystickSize = 1f;
            buttonSize = 1f;
            opacity = 0.55f;
            sensitivity = 1f;
            bufferWindow = 0.15f;
            vibration = true;
            gestureFeedback = true;
            leftHanded = false;
            layout = "Standard";
            customPositions.Clear();
            Save();
        }

        // --- Positions-Overrides (Layout-Editor) ---
        public void SetPosition(string element, Vector2 pos)
        {
            string entry = $"{element}|{pos.x:0.##}|{pos.y:0.##}";
            for (int i = 0; i < customPositions.Count; i++)
            {
                if (!customPositions[i].StartsWith(element + "|")) continue;
                customPositions[i] = entry;
                return;
            }
            customPositions.Add(entry);
        }

        public bool TryGetPosition(string element, out Vector2 pos)
        {
            pos = Vector2.zero;
            foreach (var e in customPositions)
            {
                if (!e.StartsWith(element + "|")) continue;
                var parts = e.Split('|');
                if (parts.Length != 3) continue;
                if (float.TryParse(parts[1], out float x) && float.TryParse(parts[2], out float y))
                {
                    pos = new Vector2(x, y);
                    return true;
                }
            }
            return false;
        }
    }
}
