using System.Collections.Generic;
using UnityEngine;

namespace PennerKombat
{
    /// <summary>
    /// (7) Eingabepuffer — docs/TOUCH.md §6.1.
    /// Ein Tastendruck bleibt kurz „gültig", damit Eingaben knapp vor dem
    /// Ende einer Animation nicht verschluckt werden (Standard in Fighting
    /// Games). <see cref="FighterController"/> kann über
    /// <see cref="Consume"/> prüfen, ob eine gepufferte Eingabe anliegt.
    /// </summary>
    public class InputBuffer : MonoBehaviour
    {
        public static InputBuffer Instance { get; private set; }

        [Tooltip("Wie lange eine Eingabe gültig bleibt (Sekunden).")]
        public float bufferWindow = 0.15f;

        private struct Entry
        {
            public int player;
            public VButton button;
            public float time;
        }

        private readonly List<Entry> entries = new List<Entry>();

        public static InputBuffer Ensure()
        {
            if (Instance == null)
            {
                var go = new GameObject("~InputBuffer");
                Instance = go.AddComponent<InputBuffer>();
            }
            return Instance;
        }

        void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(this); return; }
        }

        void Update()
        {
            // abgelaufene Einträge verwerfen
            for (int i = entries.Count - 1; i >= 0; i--)
                if (Time.time - entries[i].time > bufferWindow)
                    entries.RemoveAt(i);
        }

        public void Buffer(int player, VButton button)
        {
            entries.Add(new Entry { player = player, button = button, time = Time.time });
        }

        /// <summary>Liegt eine gepufferte Eingabe an? (ohne sie zu verbrauchen)</summary>
        public bool Has(int player, VButton button)
        {
            foreach (var e in entries)
                if (e.player == player && e.button == button && Time.time - e.time <= bufferWindow)
                    return true;
            return false;
        }

        /// <summary>Wie <see cref="Has"/>, entfernt den Eintrag aber.</summary>
        public bool Consume(int player, VButton button)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].player != player || entries[i].button != button) continue;
                if (Time.time - entries[i].time > bufferWindow) continue;
                entries.RemoveAt(i);
                return true;
            }
            return false;
        }

        public void Clear() => entries.Clear();
    }
}
