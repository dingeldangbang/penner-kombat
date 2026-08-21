using UnityEngine;
using System;
using PennerKombat;

namespace PennerKombat
{
    /// <summary>
    /// Einfacher, wiederverwendbarer Countdown-/Cooldown-Helfer.
    /// Läuft unabhängig von einer Monobehaviour-Update-Schleife und kann
    /// deshalb für beliebige Effekte/Cooldowns genutzt werden.
    /// </summary>
    [Serializable]
    public class GameTimer
    {
        [SerializeField] private float duration;
        [SerializeField] private float remaining;

        public float Duration => duration;
        public float Remaining => remaining;
        public float Normalized => duration <= 0f ? 0f : Mathf.Clamp01(remaining / duration);
        public bool IsRunning => remaining > 0f;
        public bool IsDone => remaining <= 0f;

        public GameTimer(float duration = 0f)
        {
            Set(duration);
        }

        /// <summary>Setzt die Dauer und startet sofort.</summary>
        public void Set(float newDuration)
        {
            duration = Mathf.Max(0f, newDuration);
            remaining = duration;
        }

        /// <summary>Stoppt und setzt zurück.</summary>
        public void Stop() => remaining = 0f;

        /// <summary>Ein Frame-Server-Tick. Rückgabe: true, wenn gerade abgelaufen.</summary>
        public bool Tick(float delta)
        {
            if (remaining <= 0f) return false;
            remaining -= delta;
            if (remaining < 0f) remaining = 0f;
            return remaining == 0f;
        }
    }

    /// <summary>Stark vereinfachte, obsolete Unity-Timer-Klasse (für Rückwärtskompatibilität).</summary>
    [Obsolete("Nutze GameTimer statt dieser Klasse.")]
    public class Timer : MonoBehaviour
    {
        public float timeRemaining;
        public bool IsFinished => timeRemaining <= 0f;
        public void Update() { if (timeRemaining > 0f) timeRemaining -= Time.deltaTime; }
    }
}
