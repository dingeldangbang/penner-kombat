using UnityEngine;
using UnityEngine.InputSystem;

namespace PennerKombat
{
    /// <summary>
    /// Erkennt Fatality-Eingaben (nur auf niedrigem Gegner-HP) und führt die
    /// Fatality des aktiven Charakters aus. Verdrahtet Trophäen-Trigger für
    /// die DLC-Trophäen (Le Binde 51, Mojo Bob 54, Mell 53/56).
    /// </summary>
    public class FatalitySystem : MonoBehaviour
    {
        [Header("Settings")]
        public float fatalityHpThreshold = 15f; // Gegner muss ≤ 15% HP haben
        public Key fatalityKey = Key.L;          // einfache Fatality-Eingabe (Demo)
        public bool enabled = true;

        private FighterController p1;
        private FighterController p2;

        void Start()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnFightersSpawned += OnFightersSpawned;
        }

        void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnFightersSpawned -= OnFightersSpawned;
        }

        void OnFightersSpawned(FighterController a, FighterController b)
        {
            p1 = a;
            p2 = b;
        }

        void Update()
        {
            if (!enabled || p1 == null || p2 == null) return;
            if (UnityEngine.InputSystem.Keyboard.current == null) return;
            if (!UnityEngine.InputSystem.Keyboard.current[fatalityKey].wasPressedThisFrame) return;

            // Spieler 1 versucht Fatality auf Spieler 2
            TryFatality(p1, p2);
        }

        void TryFatality(FighterController attacker, FighterController target)
        {
            if (target == null || !target.gameObject.activeSelf) return;
            if (target.currentHP > target.maxHP * (fatalityHpThreshold / 100f)) return;
            if (Vector3.Distance(attacker.transform.position, target.transform.position) > 3f) return;

            // Charakter-spezifische Fatality
            attacker.PerformFatality(target, "default");

            // Trophäen-Nebenwirkungen
            if (attacker is LeBinde) { /* 51 wird in LeBinde behandelt */ }
            if (attacker is MojoBob) { /* 54 wird in MojoBob behandelt */ }

            // Mell: Trophäe 53 "Auf der Kante" (Puls ≥ 210 ohne Blackout)
            if (attacker is Mell m && !m.IsBlackout && m.Pulse >= 210f && TrophyManager.Instance != null)
                TrophyManager.Instance.Unlock(TrophyManager.TROPHY_AUF_DER_KANTE);
        }

        /// <summary>Wird beim Online-Rematch aufgerufen, um Trophäe 56 auszulösen.</summary>
        public void NotifyOnlineRematch(FighterController mellUser)
        {
            if (mellUser is Mell m && m.lastFatalityDone && TrophyManager.Instance != null)
                TrophyManager.Instance.Unlock(TrophyManager.TROPHY_STABILE_SEITENLAGE);
        }
    }
}
