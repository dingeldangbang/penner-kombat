using System.Collections.Generic;
using UnityEngine;

namespace PennerKombat
{
    /// <summary>Sechs Schwierigkeitsstufen laut docs/CONTROLS.md §3.</summary>
    public enum AIDifficulty { VeryEasy, Easy, Medium, Hard, VeryHard, Boss }

    /// <summary>
    /// (3–5) Zustands-KI für Bot-Kämpfer mit sechs Schwierigkeitsstufen,
    /// Combo-Ketten und Med-Kapsel-Nutzung. Arbeitet ausschließlich über die
    /// öffentliche API des <see cref="FighterController"/>.
    /// </summary>
    public class AIController : MonoBehaviour
    {
        [Header("Schwierigkeit")]
        public AIDifficulty difficulty = AIDifficulty.Medium;
        [Tooltip("Werte beim Start aus der Stufe ableiten (sonst Inspector-Werte behalten).")]
        public bool applyDifficultyOnStart = true;

        [Header("Verhalten (wird aus der Stufe gesetzt)")]
        public float thinkIntervalMin = 0.3f;
        public float thinkIntervalMax = 0.6f;
        public float aggression = 0.6f;
        public float blockChance = 0.35f;
        public float comboChance = 0.3f;
        public float specialChance = 0.25f;
        public float damageMultiplier = 1f;
        public float wanderRadius = 15f;

        [Header("Med-Kapsel")]
        public float medHpThreshold = 0.3f;
        public float medChance = 0.4f;

        private FighterController fighter;
        private FighterController enemy;
        private MedSystem med;
        private float nextThinkTime;
        private Vector3 wanderTarget;
        private FighterMood mood;

        // Combo-Kette
        private List<string> combo = new List<string>();
        private int comboStep;
        private float comboNextStep;
        private bool inCombo;

        void Start()
        {
            fighter = GetComponent<FighterController>();
            med = GetComponent<MedSystem>();
            if (fighter != null) fighter.isAI = true;
            if (applyDifficultyOnStart) ApplyDifficulty(difficulty);
            FindEnemy();
            wanderTarget = GetRandomWanderPoint();
        }

        // ==================================================================
        //  Schwierigkeitsprofile (Spec-Tabelle §3.1)
        // ==================================================================
        public void ApplyDifficulty(AIDifficulty level)
        {
            difficulty = level;
            switch (level)
            {
                case AIDifficulty.VeryEasy:
                    thinkIntervalMin = 0.8f; thinkIntervalMax = 1.2f;
                    blockChance = 0.10f; comboChance = 0.00f; specialChance = 0.05f;
                    aggression = 0.30f; damageMultiplier = 0.6f; break;

                case AIDifficulty.Easy:
                    thinkIntervalMin = 0.5f; thinkIntervalMax = 0.9f;
                    blockChance = 0.20f; comboChance = 0.15f; specialChance = 0.12f;
                    aggression = 0.40f; damageMultiplier = 0.8f; break;

                case AIDifficulty.Medium:
                    thinkIntervalMin = 0.3f; thinkIntervalMax = 0.6f;
                    blockChance = 0.35f; comboChance = 0.30f; specialChance = 0.25f;
                    aggression = 0.60f; damageMultiplier = 1.0f; break;

                case AIDifficulty.Hard:
                    thinkIntervalMin = 0.15f; thinkIntervalMax = 0.35f;
                    blockChance = 0.50f; comboChance = 0.50f; specialChance = 0.35f;
                    aggression = 0.75f; damageMultiplier = 1.2f; break;

                case AIDifficulty.VeryHard:
                    thinkIntervalMin = 0.08f; thinkIntervalMax = 0.20f;
                    blockChance = 0.65f; comboChance = 0.70f; specialChance = 0.45f;
                    aggression = 0.90f; damageMultiplier = 1.4f; break;

                case AIDifficulty.Boss:
                    thinkIntervalMin = 0.05f; thinkIntervalMax = 0.15f;
                    blockChance = 0.80f; comboChance = 0.90f; specialChance = 0.55f;
                    aggression = 1.00f; damageMultiplier = 1.6f; break;
            }

            // Schadensmultiplikator direkt auf den Kämpfer anwenden
            if (fighter != null && !Mathf.Approximately(damageMultiplier, 1f))
            {
                fighter.lightDamage *= damageMultiplier;
                fighter.heavyDamage *= damageMultiplier;
                damageMultiplier = 1f;   // nur einmal anwenden
            }

            combo = BuildComboPattern();
        }

        /// <summary>Stufenlänge: 3 Schritte bei VeryEasy bis 13 beim Boss.</summary>
        List<string> BuildComboPattern()
        {
            int length = 3 + (int)difficulty * 2;
            var list = new List<string>(length);
            for (int i = 0; i < length; i++)
            {
                string[] pool;
                if (i % 5 == 0) pool = new[] { "Light", "Light", "Heavy", "Special2" };
                else if (i % 3 == 0) pool = new[] { "Heavy", "Special1", "Special2" };
                else if (i > 5 && i % 4 == 0) pool = new[] { "Jump", "Light", "Heavy" };
                else pool = new[] { "Light", "Light", "Light", "Heavy", "Special1" };
                list.Add(pool[Random.Range(0, pool.Length)]);
            }
            return list;
        }

        // ==================================================================
        //  Denken
        // ==================================================================
        void Update()
        {
            if (fighter == null || enemy == null) return;
            if (!fighter.gameObject.activeSelf || !enemy.gameObject.activeSelf) return;

            if (inCombo && Time.time >= comboNextStep) ContinueCombo();
            if (Time.time < nextThinkTime) return;

            nextThinkTime = Time.time + Random.Range(thinkIntervalMin, thinkIntervalMax);
            Think();
        }

        void FindEnemy()
        {
            var all = FindObjectsOfType<FighterController>();
            foreach (var f in all)
                if (f != fighter) enemy = f;
        }

        void Think()
        {
            TryUseMed();

            float dist = Vector3.Distance(transform.position, enemy.transform.position);
            bool enemyInAir = !enemy.isGrounded;

            if (inCombo) return;   // laufende Kette nicht unterbrechen

            if (dist < fighter.attackRange + 0.5f)
            {
                float r = Random.value;

                if (r < blockChance * 0.5f)
                {
                    fighter.isBlocking = true;
                    mood = FighterMood.Defensive;
                }
                else if (r < specialChance + blockChance * 0.5f && fighter.moves.Count > 0)
                {
                    fighter.isBlocking = false;
                    var move = fighter.moves[Random.Range(0, fighter.moves.Count)];
                    fighter.ExecuteMove(move);
                    mood = FighterMood.Aggressive;
                }
                else if (r < aggression)
                {
                    fighter.isBlocking = false;
                    fighter.StartAttack(Random.value < 0.3f);
                    mood = FighterMood.Aggressive;
                    if (Random.value < comboChance) StartCombo();
                }
                else
                {
                    fighter.isBlocking = false;
                    Vector3 dir = (transform.position - enemy.transform.position).normalized;
                    fighter.ApplyImpulse(dir * 3f);
                    mood = FighterMood.Retreating;
                }
            }
            else if (dist < 10f)
            {
                fighter.isBlocking = false;
                Vector3 dir = (enemy.transform.position - transform.position).normalized;
                fighter.MoveTowards(dir, 0.6f + aggression * 0.4f);
                if (enemyInAir && Random.value < 0.3f) fighter.Jump();
                if (dist > 5f && Random.value < specialChance * 0.6f && fighter.moves.Count > 0)
                    fighter.ExecuteMove(fighter.moves[Random.Range(0, fighter.moves.Count)]);
                mood = FighterMood.Aggressive;
            }
            else
            {
                fighter.isBlocking = false;
                if (Vector3.Distance(transform.position, wanderTarget) < 1f)
                    wanderTarget = GetRandomWanderPoint();
                Vector3 dir = (wanderTarget - transform.position).normalized;
                fighter.MoveTowards(dir, 0.5f);
                mood = FighterMood.Neutral;
            }

            if (mood != FighterMood.Defensive && fighter.isBlocking && Random.value < 0.4f)
                fighter.isBlocking = false;
        }

        // ==================================================================
        //  Combo-Ketten
        // ==================================================================
        void StartCombo()
        {
            if (combo == null || combo.Count == 0) return;
            inCombo = true;
            comboStep = 0;
            comboNextStep = Time.time + 0.25f;
        }

        void ContinueCombo()
        {
            if (comboStep >= combo.Count) { EndCombo(); return; }

            // Combo abbrechen, wenn der Gegner aus der Reichweite ist
            if (enemy == null || Vector3.Distance(transform.position, enemy.transform.position) > fighter.attackRange + 1.5f)
            {
                EndCombo();
                return;
            }

            switch (combo[comboStep])
            {
                case "Light":    fighter.StartAttack(false); break;
                case "Heavy":    fighter.StartAttack(true); break;
                case "Jump":     fighter.Jump(); break;
                case "Special1":
                case "Special2":
                    if (fighter.moves.Count > 0)
                        fighter.ExecuteMove(fighter.moves[Random.Range(0, fighter.moves.Count)]);
                    break;
            }

            comboStep++;
            comboNextStep = Time.time + Mathf.Lerp(0.45f, 0.18f, (int)difficulty / 5f);
        }

        void EndCombo()
        {
            inCombo = false;
            comboStep = 0;
            combo = BuildComboPattern();   // nächste Kette neu würfeln
        }

        // ==================================================================
        //  Med-Kapsel
        // ==================================================================
        void TryUseMed()
        {
            if (med == null) return;
            if (fighter.currentHP > fighter.maxHP * medHpThreshold) return;
            if (Random.value > medChance) return;
            // Nicht heilen, wenn der Gegner direkt danebensteht
            if (enemy != null && Vector3.Distance(transform.position, enemy.transform.position) < fighter.attackRange) return;
            med.Use();
        }

        Vector3 GetRandomWanderPoint()
        {
            Vector2 rand = Random.insideUnitCircle * wanderRadius;
            return new Vector3(rand.x, transform.position.y, rand.y);
        }

        // --- API für GameManager / Menü ---
        public void SetDifficulty(AIDifficulty level) => ApplyDifficulty(level);
        public AIDifficulty GetDifficulty() => difficulty;

        public static string DifficultyName(AIDifficulty d)
        {
            switch (d)
            {
                case AIDifficulty.VeryEasy: return "Sehr leicht";
                case AIDifficulty.Easy:     return "Leicht";
                case AIDifficulty.Medium:   return "Mittel";
                case AIDifficulty.Hard:     return "Schwer";
                case AIDifficulty.VeryHard: return "Sehr schwer";
                case AIDifficulty.Boss:     return "Boss";
                default:                    return d.ToString();
            }
        }
    }
}
