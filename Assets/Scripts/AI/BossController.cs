using UnityEngine;

namespace PennerKombat
{
    /// <summary>
    /// Boss-Gegner mit Phasen (docs/EXTRAS.md §10).
    /// Erweitert den normalen <see cref="AIController"/> um mehr HP, einen
    /// Phasenwechsel bei sinkender Gesundheit (schneller, aggressiver, härter)
    /// und eine Ansage beim Übergang.
    /// </summary>
    [RequireComponent(typeof(FighterController))]
    [RequireComponent(typeof(AIController))]
    public class BossController : MonoBehaviour
    {
        [Header("Boss")]
        public string bossName = "Der Baron";
        public float hpMultiplier = 4f;
        [Range(1, 3)] public int phases = 3;

        [Header("Phasenwechsel")]
        [Tooltip("Bei welchem HP-Anteil die nächste Phase startet.")]
        public float[] phaseThresholds = { 0.66f, 0.33f };
        public float healOnPhase = 0.08f;

        public int CurrentPhase { get; private set; } = 1;

        private FighterController fighter;
        private AIController ai;

        void Start()
        {
            fighter = GetComponent<FighterController>();
            ai = GetComponent<AIController>();

            // Bosse haben deutlich mehr Fleisch auf den Rippen
            fighter.maxHP *= hpMultiplier;
            fighter.currentHP = fighter.maxHP;

            ai.ApplyDifficulty(AIDifficulty.Hard);
            Announce($"{bossName.ToUpperInvariant()} — PHASE 1");
        }

        void Update()
        {
            if (fighter == null || CurrentPhase >= phases) return;

            float ratio = fighter.currentHP / Mathf.Max(1f, fighter.maxHP);
            int index = CurrentPhase - 1;
            if (index >= phaseThresholds.Length) return;

            if (ratio <= phaseThresholds[index])
                EnterPhase(CurrentPhase + 1);
        }

        void EnterPhase(int phase)
        {
            CurrentPhase = phase;

            // Härter, schneller, gemeiner
            ai.ApplyDifficulty(phase >= 3 ? AIDifficulty.Boss : AIDifficulty.VeryHard);
            fighter.moveSpeed *= 1.12f;
            fighter.HealSelf(fighter.maxHP * healOnPhase);

            // Inszenierung
            Announce($"PHASE {phase}");
            CameraShake.Shake(12f, 0.35f);
            CameraShake.SlowMotion(0.35f, 0.6f);
            ScreenEffects.FlashColor(PennerPalette.BloodRed, 0.55f, 0.4f);
            ComboExplosion3D.Ensure().Spawn(transform.position, 4f, PennerPalette.BloodRed, 0f, 9f, fighter);
            GetComponent<CharacterVisuals>()?.BuffFlash(PennerPalette.BloodRed, 2f);
            CrowdReactions.Instance?.React(CrowdMood.Entsetzt, 3f);
            MusicSync.Instance?.SetTempo(140f + phase * 12f);
        }

        void Announce(string text)
        {
            FloatingText.Show(transform.position + Vector3.up * 3f, text, PennerPalette.BloodRed, 2.2f);
        }
    }
}
