using UnityEngine;

namespace PennerKombat
{
    /// <summary>
    /// Mojo Bobs "Fünfzig Cent". Fängt der Gegner die Münze (Kalle/Dieter),
    /// verliert er 20 Frames; fängt er sie nicht, bekommt Bob +1 Mojo.
    /// </summary>
    public class CoinProjectile : MonoBehaviour
    {
        [Header("Coin")]
        public float speed = 10f;
        public float lifetime = 3f;
        public float caughtFrameLoss = 20f;
        public AudioClip coinSound;

        private Vector3 direction;
        private MojoBob owner;
        private bool resolved;

        public void Initialize(Vector3 dir, MojoBob owner)
        {
            direction = dir.normalized;
            this.owner = owner;
            Destroy(gameObject, lifetime);
        }

        void Update()
        {
            if (resolved) return;
            transform.Translate(direction * speed * Time.deltaTime, Space.World);
            transform.Rotate(0f, 360f * Time.deltaTime * 2f, 0f);
        }

        void OnTriggerEnter(Collider other)
        {
            if (resolved) return;
            var target = other.GetComponent<FighterController>();
            if (target != null && target != owner)
            {
                // Kalle/Dieter können fangen (Greif-Animation)
                if (target is Kalle || target is Dieter)
                {
                    resolved = true;
                    target.BroadcastMessage("CatchCoin", SendMessageOptions.DontRequireReceiver);
                    if (coinSound != null) AudioSource.PlayClipAtPoint(coinSound, transform.position);
                    Destroy(gameObject);
                    return;
                }
            }

            // Nicht gefangen -> Mojo für Bob
            if (owner != null) owner.AddMojo(1);
            resolved = true;
            Destroy(gameObject);
        }
    }
}
