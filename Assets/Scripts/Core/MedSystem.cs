using UnityEngine;

namespace PennerKombat
{
    /// <summary>
    /// (5) Med-Kapsel — begrenzte Selbstheilung für Spieler und KI.
    /// Standard: 2 Ladungen à 25 HP, 1,2 s Anwendungszeit (in der man
    /// verwundbar ist) und 8 s Abklingzeit. Wird über die Taste `H`
    /// (P1) bzw. `Num Enter` (P2) oder von der KI ausgelöst.
    /// </summary>
    [RequireComponent(typeof(FighterController))]
    public class MedSystem : MonoBehaviour
    {
        [Header("Werte")]
        public int charges = 2;
        public int maxCharges = 2;
        public float healAmount = 25f;
        public float useDuration = 1.2f;
        public float cooldown = 8f;

        [Header("Feedback")]
        public AudioClip useSound;

        private FighterController fighter;
        private float cooldownTimer;
        private float useTimer;

        public bool IsUsing => useTimer > 0f;
        public bool HasCharge => charges > 0 && cooldownTimer <= 0f;

        void Awake() => fighter = GetComponent<FighterController>();

        void Update()
        {
            if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;

            if (useTimer > 0f)
            {
                useTimer -= Time.deltaTime;
                if (useTimer <= 0f) Complete();
                return;
            }

            // Spielereingabe
            if (!fighter.isAI && FighterInput.Instance != null &&
                FighterInput.Instance.GetMed(fighter.playerIndex))
                Use();
        }

        /// <summary>Med-Kapsel anwenden. Gibt false zurück, wenn nicht möglich.</summary>
        public bool Use()
        {
            if (!HasCharge || IsUsing) return false;
            if (fighter.currentHP >= fighter.maxHP) return false;

            charges--;
            useTimer = useDuration;
            cooldownTimer = cooldown;

            if (useSound != null) AudioSource.PlayClipAtPoint(useSound, transform.position);
            GetComponent<CharacterVisuals>()?.BuffFlash(PennerPalette.PoisonGrn, useDuration);
            FloatingText.Show(transform.position + Vector3.up * 2.4f, "MED", PennerPalette.PoisonGrn, 1f);
            return true;
        }

        void Complete()
        {
            if (fighter == null || !fighter.gameObject.activeInHierarchy) return;
            fighter.HealSelf(healAmount);
            VFXManager.Instance?.PlayPoison(transform.position + Vector3.up * 1.1f, 0.8f);
            FloatingText.Show(transform.position + Vector3.up * 2.4f,
                              $"+{Mathf.RoundToInt(healAmount)} HP", PennerPalette.PoisonGrn, 1.1f);
        }

        /// <summary>Rundenreset: Ladungen auffüllen.</summary>
        public void ResetForRound()
        {
            charges = maxCharges;
            cooldownTimer = 0f;
            useTimer = 0f;
        }
    }
}
