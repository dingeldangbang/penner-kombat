using System.Collections;
using UnityEngine;

namespace PennerKombat
{
    /// <summary>
    /// Eine einzelne Ratte aus Rolfs "Ratten"-Beschwörung. Läuft auf den
    /// Gegner zu und beißt bei Kontakt wiederholt (geringer Schaden).
    /// </summary>
    public class RatController : MonoBehaviour
    {
        [Header("Rat")]
        public float moveSpeed = 4f;
        public float biteDamage = 2f;
        public float biteInterval = 1f;
        public float lifetime = 12f;
        public AudioClip biteSound;

        private FighterController target;
        private bool attached;

        public void Initialize(FighterController target)
        {
            this.target = target;
            Destroy(gameObject, lifetime);
            StartCoroutine(Chase());
        }

        IEnumerator Chase()
        {
            while (target != null && target.gameObject.activeSelf)
            {
                Vector3 dir = target.transform.position - transform.position;
                dir.y = 0f;
                float dist = dir.magnitude;
                if (dist < 1.2f)
                {
                    Bite();
                }
                else
                {
                    transform.position += dir.normalized * moveSpeed * Time.deltaTime;
                    transform.rotation = Quaternion.LookRotation(dir);
                }
                yield return null;
            }
            Destroy(gameObject);
        }

        void Bite()
        {
            if (attached || target == null) return;
            attached = true;
            if (biteSound != null) AudioSource.PlayClipAtPoint(biteSound, transform.position);
            target.TakeDamage(biteDamage, Vector3.zero, null);
            Invoke(nameof(ReleaseBite), biteInterval);
        }

        void ReleaseBite() => attached = false;

        public void OnRatDeath()
        {
            // Wird vom Rolf-Skript über Events registriert.
        }
    }
}
