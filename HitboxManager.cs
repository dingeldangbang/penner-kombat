using System.Collections.Generic;
using UnityEngine;

namespace PennerKombat
{
    /// <summary>
    /// Zentraler Manager für Hitboxen. Statt OverlapSphere/Box direkt im
    /// FighterController zu verteilen, können Hitboxen hier als Trigger-Collider
    /// registriert und überprüft werden. Der FighterController nutzt für den
    /// Basis-Angriff weiterhin die einfache Box (siehe EnableHitbox); dieses
    /// Modul ergänzt erweiterte, projektil- und waffenbasierte Hitboxen.
    /// </summary>
    public class HitboxManager : MonoBehaviour
    {
        public static HitboxManager Instance;

        private readonly List<Hitbox> hitboxes = new List<Hitbox>();

        void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(this); return; }
        }

        /// <summary>Registriert eine aktive Hitbox (Collider muss Trigger sein).</summary>
        public void Register(Hitbox hb)
        {
            if (hb != null && !hitboxes.Contains(hb)) hitboxes.Add(hb);
        }

        public void Unregister(Hitbox hb)
        {
            if (hb != null) hitboxes.Remove(hb);
        }

        /// <summary>Lässt alle registrierten Hitboxes ihre Überlappungen gegen Ziele prüfen.</summary>
        public void TickAll()
        {
            for (int i = hitboxes.Count - 1; i >= 0; i--)
            {
                var hb = hitboxes[i];
                if (hb == null) { hitboxes.RemoveAt(i); continue; }
                if (!hb.gameObject.activeInHierarchy) { hitboxes.RemoveAt(i); continue; }
                hb.CheckOverlaps();
            }
        }
    }

    /// <summary>
    /// Komponente für eine einzelne Hitbox. Wird auf einem Trigger-Collider
    /// platziert und kann Schaden an allen überlappenden Kämpfern anrichten.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Hitbox : MonoBehaviour
    {
        [Header("Hitbox")]
        public FighterController owner;
        public LayerMask targetLayer;
        public float damage = 10f;
        public bool isHeavy;
        public Vector3 knockbackDir = Vector3.forward;
        public float lifetime = 0.3f;

        private readonly HashSet<Collider> processed = new HashSet<Collider>();
        private float alive = 0f;

        public void Initialize(FighterController owner, float dmg, bool heavy, Vector3 kb)
        {
            this.owner = owner;
            damage = dmg;
            isHeavy = heavy;
            knockbackDir = kb;
            processed.Clear();
            alive = 0f;
            HitboxManager.Instance.Register(this);
        }

        void Update()
        {
            alive += Time.deltaTime;
            if (alive >= lifetime)
            {
                HitboxManager.Instance.Unregister(this);
                Destroy(gameObject);
            }
        }

        public void CheckOverlaps()
        {
            var col = GetComponent<Collider>();
            Collider[] hits = Physics.OverlapBox(
                col.bounds.center, col.bounds.extents, transform.rotation, targetLayer);
            foreach (var h in hits)
            {
                if (processed.Contains(h)) continue;
                processed.Add(h);
                var target = h.GetComponent<FighterController>();
                if (target == null || target == owner) continue;
                target.TakeDamage(damage, knockbackDir, owner);
            }
        }

        void OnDestroy()
        {
            HitboxManager.Instance?.Unregister(this);
        }
    }
}
