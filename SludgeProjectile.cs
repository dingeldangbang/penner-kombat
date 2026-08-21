using System.Collections;
using UnityEngine;

namespace PennerKombat
{
    /// <summary>
    /// Mells "Pampe" — ein Klumpen Straßenschlick. Trifft er, klebt der
    /// Gegner fest (kein Dash/Sprung) und eine Pfütze bleibt zurück.
    /// </summary>
    public class SludgeProjectile : MonoBehaviour
    {
        [Header("Sludge")]
        public float speed = 8f;
        public float damage = 6f;
        public float lifetime = 4f;
        public float stickDuration = 2f;
        public GameObject puddlePrefab;
        public float puddleDuration = 8f;

        private Vector3 direction;
        private FighterController owner;

        public void Initialize(Vector3 dir, FighterController owner)
        {
            direction = dir.normalized;
            this.owner = owner;
            Destroy(gameObject, lifetime);
        }

        void Update() => transform.Translate(direction * speed * Time.deltaTime, Space.World);

        void OnTriggerEnter(Collider other)
        {
            var target = other.GetComponent<FighterController>();
            if (target != null && target != owner)
            {
                target.TakeDamage(damage, direction, owner);
                target.BroadcastMessage("ApplySludge", stickDuration, SendMessageOptions.DontRequireReceiver);
            }

            if (puddlePrefab != null)
            {
                var puddle = Instantiate(puddlePrefab, transform.position, Quaternion.identity);
                Destroy(puddle, puddleDuration);
            }
            Destroy(gameObject);
        }
    }

    /// <summary>Komponente, die einen Kämpfer für eine Dauer festklebt (Dash/Sprung blockiert).</summary>
    public class SludgeStick : MonoBehaviour
    {
        public float remaining;
        public void Apply(float duration) => remaining = duration;
        void Update() { if (remaining > 0f) remaining -= Time.deltaTime; }
        public bool IsStuck => remaining > 0f;
    }
}
