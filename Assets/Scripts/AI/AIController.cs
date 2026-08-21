using UnityEngine;

namespace PennerKombat
{
    /// <summary>
    /// Zustands-KI für Bot-Kämpfer. Denkt in Intervallen neu, nähert sich an,
    /// greift an, blockt oder zieht sich zurück. Nutzt die öffentliche
    /// Bewegungs-/Angriffs-API der FighterController (kein Zugriff auf private Felder).
    /// </summary>
    public class AIController : MonoBehaviour
    {
        [Header("AI Settings")]
        public float thinkIntervalMin = 0.35f;
        public float thinkIntervalMax = 0.85f;
        public float aggression = 0.6f;
        public float wanderRadius = 15f;
        public float specialChance = 0.25f;   // Chance, im Nahkampf eine Spezialbewegung zu nutzen

        private FighterController fighter;
        private FighterController enemy;
        private float nextThinkTime;
        private Vector3 wanderTarget;
        private FighterMood mood;

        void Start()
        {
            fighter = GetComponent<FighterController>();
            if (fighter != null) fighter.isAI = true;
            FindEnemy();
            wanderTarget = GetRandomWanderPoint();
        }

        void Update()
        {
            if (fighter == null || enemy == null || !fighter.gameObject.activeSelf || !enemy.gameObject.activeSelf) return;
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
            float dist = Vector3.Distance(transform.position, enemy.transform.position);
            bool enemyInAir = !enemy.isGrounded;

            if (dist < fighter.attackRange + 0.5f)
            {
                // Im Nahkampf: aggressiv angreifen, gelegentlich blocken oder weichen
                float r = Random.value;
                float a = Mathf.Clamp01(aggression);
                // Gewichtung: Spezial ~specialChance, Angriff ~a*0.5, Block ~0.2, Rückzug Rest
                if (r < specialChance && fighter.moves.Count > 0)
                {
                    var move = fighter.moves[Random.Range(0, fighter.moves.Count)];
                    fighter.ExecuteMove(move);
                    mood = FighterMood.Aggressive;
                }
                else if (r < specialChance + a * 0.5f)
                {
                    fighter.StartAttack(Random.value < 0.3f);
                    mood = FighterMood.Aggressive;
                }
                else if (r < specialChance + a * 0.5f + 0.2f)
                {
                    fighter.isBlocking = true;
                    mood = FighterMood.Defensive;
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
                // Annähern; springen, wenn der Gegner in der Luft ist
                fighter.isBlocking = false;
                Vector3 dir = (enemy.transform.position - transform.position).normalized;
                fighter.MoveTowards(dir, 0.8f);
                if (enemyInAir && Random.value < 0.3f)
                    fighter.Jump();
                mood = FighterMood.Aggressive;
            }
            else
            {
                // Wandern
                fighter.isBlocking = false;
                if (Vector3.Distance(transform.position, wanderTarget) < 1f)
                    wanderTarget = GetRandomWanderPoint();
                Vector3 dir = (wanderTarget - transform.position).normalized;
                fighter.MoveTowards(dir, 0.5f);
                mood = FighterMood.Neutral;
            }

            // Block-Reset falls nicht mehr im Blockzustand
            if (mood != FighterMood.Defensive && fighter.isBlocking && Random.value < 0.4f)
                fighter.isBlocking = false;
        }

        Vector3 GetRandomWanderPoint()
        {
            Vector2 rand = Random.insideUnitCircle * wanderRadius;
            return new Vector3(rand.x, transform.position.y, rand.y);
        }
    }
}
