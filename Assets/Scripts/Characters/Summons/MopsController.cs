using System.Collections;
using UnityEngine;

namespace PennerKombat
{
    /// <summary>
    /// Paula, Le Bindes Mopsdame. Wird per "Mops-Kommando" beschworen,
    /// trottet zum Gegner und verursacht die psychologische Katastrophe:
    /// Debuff "Fassungslosigkeit", +Krit-Chance für Le Binde, und alle
    /// Arena-Objekte kippen um. Paula selbst ist unverwundbar.
    /// </summary>
    public class MopsController : MonoBehaviour
    {
        [Header("Paula")]
        public float moveSpeed = 2.5f;
        public float debuffDuration = 8f;
        public float inputDelayFrames = 4f;   // ≈ 4 * 16.7ms
        public float critBonusForOwner = 0.5f;

        private FighterController owner;
        private FighterController target;
        private bool triggered;

        public void Initialize(FighterController owner, FighterController target)
        {
            this.owner = owner;
            this.target = target;
            StartCoroutine(RunToTarget());
        }

        IEnumerator RunToTarget()
        {
            while (target != null && target.gameObject.activeSelf)
            {
                Vector3 dir = target.transform.position - transform.position;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.2f)
                {
                    dir.Normalize();
                    transform.position += dir * moveSpeed * Time.deltaTime;
                }
                else
                {
                    break;
                }
                yield return null;
            }
            TriggerDebuff();
        }

        void TriggerDebuff()
        {
            if (triggered) return;
            triggered = true;

            // Arena-Objekte kippen
            if (ArenaManager.Instance != null) ArenaManager.Instance.TiltAllProps();

            // Debuff auf Gegner anwenden
            if (target != null)
            {
                var debuff = target.gameObject.GetComponent<Fassungslosigkeit>();
                if (debuff == null) debuff = target.gameObject.AddComponent<Fassungslosigkeit>();
                debuff.Apply(inputDelayFrames, debuffDuration);
            }

            // Krit-Bonus für Le Binde
            if (owner is LeBinde leBinde)
                leBinde.ApplyMopsCritBonus(critBonusForOwner, debuffDuration);

            // Paula bleibt stehen und ist unverwundbar
            Destroy(gameObject, debuffDuration + 1f);
        }

        void OnTriggerEnter(Collider other)
        {
            // Paula interagiert nicht mit Hitboxen — sie ist unverwundbar.
        }
    }

    /// <summary>Debuff "Fassungslosigkeit": verzögert Eingaben des Gegners.</summary>
    public class Fassungslosigkeit : MonoBehaviour
    {
        private float remaining;
        private float inputDelay;

        public void Apply(float delaySeconds, float duration)
        {
            inputDelay = delaySeconds * 0.0167f;
            remaining = duration;
        }

        void Update()
        {
            if (remaining <= 0f) return;
            remaining -= Time.deltaTime;
            if (remaining <= 0f) { enabled = false; }
        }

        public float GetInputDelay() => remaining > 0f ? inputDelay : 0f;
    }
}
