using System.Collections;
using UnityEngine;

namespace PennerKombat
{
    /// <summary>
    /// Konter/Parry (docs/EXTRAS.md §5). Wird im richtigen Moment geblockt —
    /// im Parry-Fenster nach Blockbeginn — bricht der Konter die Combo des
    /// Gegners, schleudert ihn weg und öffnet ein kurzes Zeitfenster für eine
    /// Gegen-Combo.
    /// </summary>
    [RequireComponent(typeof(FighterController))]
    public class ParrySystem : MonoBehaviour
    {
        [Header("Zeitfenster")]
        [Tooltip("So lange nach Blockbeginn zählt ein Treffer als Parry.")]
        public float parryWindow = 0.16f;
        public float cooldown = 1.2f;

        [Header("Wirkung")]
        public float counterDamage = 14f;
        public float knockback = 14f;
        public float enemyStun = 0.55f;
        [Tooltip("Fenster, in dem die eigene Gegen-Combo verstärkt ist.")]
        public float punishWindow = 1.2f;
        public float punishBonus = 1.35f;

        public bool PunishActive => punishTimer > 0f;

        private FighterController fighter;
        private bool wasBlocking;
        private float blockStartTime = -10f;
        private float cooldownTimer;
        private float punishTimer;

        void Awake() => fighter = GetComponent<FighterController>();

        void OnEnable()
        {
            if (fighter != null) fighter.OnDamaged += HandleIncoming;
        }

        void OnDisable()
        {
            if (fighter != null) fighter.OnDamaged -= HandleIncoming;
        }

        void Update()
        {
            if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;
            if (punishTimer > 0f) punishTimer -= Time.deltaTime;

            // Blockbeginn merken — daraus ergibt sich das Parry-Fenster
            if (fighter.isBlocking && !wasBlocking)
                blockStartTime = Time.time;
            wasBlocking = fighter.isBlocking;
        }

        /// <summary>
        /// Der FighterController meldet geblockte Treffer über OnDamaged nicht,
        /// deshalb prüft <see cref="TryParry"/> direkt beim Block (siehe
        /// FighterController.OnBlocked → ParrySystem.TryParry).
        /// </summary>
        public bool TryParry(FighterController attacker)
        {
            if (attacker == null || cooldownTimer > 0f) return false;
            if (Time.time - blockStartTime > parryWindow) return false;

            cooldownTimer = cooldown;
            punishTimer = punishWindow;

            // Angriff des Gegners bricht ab
            attacker.ApplyGrabStun(enemyStun);
            ComboSystem.Instance?.ResetAll();

            Vector3 dir = (attacker.transform.position - transform.position).normalized;
            attacker.ApplyImpulse(dir * knockback + Vector3.up * 3f);
            attacker.TakeDamage(counterDamage, dir, fighter);

            // Präsentation
            CameraShake.HitStop(0.09f);
            CameraShake.Shake(9f, 0.14f);
            ScreenEffects.FlashColor(PennerPalette.NeonBlue, 0.45f, 0.18f);
            VFXManager.Instance?.PlayBlock(transform.position + Vector3.up * 1.1f, dir);
            FloatingText.Show(transform.position + Vector3.up * 2.4f, "KONTER!", PennerPalette.NeonBlue, 1.1f);
            MusicSync.Instance?.Duck(0.4f);

            return true;
        }

        void HandleIncoming(FighterController target, FighterController attacker, float damage)
        {
            // Ungeblockter Treffer beendet das Bestrafungsfenster
            punishTimer = 0f;
        }

        /// <summary>Schadensbonus im Bestrafungsfenster (vom Kampfsystem abgefragt).</summary>
        public float DamageMultiplier => PunishActive ? punishBonus : 1f;
    }
}
