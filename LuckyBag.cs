using UnityEngine;

namespace PennerKombat
{
    /// <summary>
    /// Mojo Bobs "Beutelchen". Der Effekt wird beim Aufprall gewürfelt:
    /// 1-2 Nichts · 3-4 9% Schaden · 5 Eingabe-Invertierung ·
    /// 6 +2 Mojo · kritisch (5%) ein Klavier (40% Schaden).
    /// </summary>
    public class LuckyBag : MonoBehaviour
    {
        [Header("Bag")]
        public float speed = 8f;
        public float lifetime = 3f;
        public GameObject pianoPrefab;
        public AudioClip pianoSound;
        public AudioClip dustSound;

        private Vector3 direction;
        private MojoBob owner;
        private bool hasLanded;

        public void Initialize(Vector3 dir, MojoBob owner)
        {
            direction = dir.normalized;
            this.owner = owner;
            Destroy(gameObject, lifetime);
        }

        void Update()
        {
            if (hasLanded) return;
            transform.Translate(direction * speed * Time.deltaTime, Space.World);
        }

        void OnTriggerEnter(Collider other)
        {
            if (hasLanded) return;
            hasLanded = true;

            int roll = Random.Range(0, 100);

            if (roll < 5) // Kritisch: Klavier
            {
                if (pianoPrefab != null) Instantiate(pianoPrefab, transform.position, Quaternion.identity);
                if (pianoSound != null) AudioSource.PlayClipAtPoint(pianoSound, transform.position);
                DamageNearby(40f, 3f);
            }
            else if (roll < 15) // +2 Mojo
            {
                if (owner != null) owner.AddMojo(2);
            }
            else if (roll < 35) // 9% Schaden
            {
                DamageNearby(9f, 2f);
            }
            else if (roll < 55) // Eingaben invertieren (3s)
            {
                InvertInputsNearby(3f);
            }
            else // Nichts — nur Staub
            {
                if (dustSound != null) AudioSource.PlayClipAtPoint(dustSound, transform.position);
            }

            Destroy(gameObject);
        }

        void DamageNearby(float dmg, float radius)
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, radius);
            foreach (var hit in hits)
            {
                var f = hit.GetComponent<FighterController>();
                if (f != null && f != owner)
                    f.TakeDamage(dmg, transform.forward, owner);
            }
        }

        void InvertInputsNearby(float duration)
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, 2.5f);
            foreach (var hit in hits)
            {
                var f = hit.GetComponent<FighterController>();
                if (f != null && f != owner)
                    f.BroadcastMessage("InvertInputs", duration, SendMessageOptions.DontRequireReceiver);
            }
        }
    }
}
