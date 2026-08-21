using UnityEngine;

namespace PennerKombat
{
    /// <summary>
    /// Ein verbogener Löffel aus Mojo Bobs "Löffelsturm". Jeder Löffel
    /// würfelt separat auf Krit; kritische Löffel melden den Treffer an
    /// Mojo Bobs Gamble-System zurück.
    /// </summary>
    public class SpoonProjectile : MonoBehaviour
    {
        [Header("Spoon")]
        public float speed = 12f;
        public float damage = 1.5f;
        public float lifetime = 2f;
        public bool isCritical;
        public AudioClip hitSound;

        private Vector3 direction;
        private FighterController owner;
        private MojoBob mojoBob;

        public void Initialize(Vector3 dir, FighterController owner, bool crit)
        {
            direction = dir.normalized;
            this.owner = owner;
            isCritical = crit;
            mojoBob = owner as MojoBob;
            transform.Rotate(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360));
            Destroy(gameObject, lifetime);
        }

        void Update()
        {
            transform.Translate(direction * speed * Time.deltaTime, Space.World);
            transform.Rotate(Random.Range(-5f, 5f), Random.Range(-5f, 5f), Random.Range(-5f, 5f));
        }

        void OnTriggerEnter(Collider other)
        {
            var target = other.GetComponent<FighterController>();
            if (target != null && target != owner)
            {
                float finalDamage = isCritical ? damage * 2.4f : damage;
                target.TakeDamage(finalDamage, direction, owner);
                if (hitSound != null) AudioSource.PlayClipAtPoint(hitSound, transform.position);
                if (isCritical && mojoBob != null) mojoBob.RecordCrit(true);
            }
            Destroy(gameObject);
        }
    }
}
