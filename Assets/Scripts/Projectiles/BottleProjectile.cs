using System.Collections;
using UnityEngine;

namespace PennerKombat
{
    /// <summary>
    /// Le Bindes Bierflasche. Richtet beim Treffen Blutung (DoT) an; in der
    /// EX-Version bleiben Scherben liegen (Zonen-Kontrolle).
    /// </summary>
    public class BottleProjectile : MonoBehaviour
    {
        [Header("Bottle")]
        public float speed = 15f;
        public float damage = 11f;
        public float lifetime = 3f;
        public bool leaveShards;
        public GameObject breakEffect;
        public GameObject shardPrefab;
        public AudioClip breakSound;

        private Vector3 direction;
        private FighterController owner;
        private bool hasHit;

        public void Initialize(Vector3 dir, FighterController owner, bool exVersion)
        {
            direction = dir.normalized;
            this.owner = owner;
            leaveShards = exVersion;
            Destroy(gameObject, lifetime);
        }

        void Update()
        {
            if (hasHit) return;
            transform.Translate(direction * speed * Time.deltaTime, Space.World);
        }

        void OnTriggerEnter(Collider other)
        {
            if (hasHit) return;
            var target = other.GetComponent<FighterController>();
            if (target != null && target != owner)
            {
                target.TakeDamage(damage, direction, owner);
                StartCoroutine(Bleed(target, 6f, 5));
                Break();
            }
            else if (!other.CompareTag(GameConstants.TagFighter))
            {
                Break();
            }
        }

        void Break()
        {
            if (hasHit) return;
            hasHit = true;
            if (breakEffect != null) Instantiate(breakEffect, transform.position, Quaternion.identity);
            if (breakSound != null) AudioSource.PlayClipAtPoint(breakSound, transform.position);
            if (leaveShards && shardPrefab != null)
                for (int i = 0; i < 5; i++)
                    Instantiate(shardPrefab, transform.position, Random.rotation);
            Destroy(gameObject);
        }

        IEnumerator Bleed(FighterController target, float duration, int ticks)
        {
            for (int i = 0; i < ticks; i++)
            {
                yield return new WaitForSeconds(duration / ticks);
                if (target != null && target.gameObject.activeSelf)
                    target.TakeDamage(1f, Vector3.zero, owner);
            }
        }
    }
}
