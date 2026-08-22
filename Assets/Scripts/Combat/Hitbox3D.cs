using System.Collections.Generic;
using UnityEngine;

namespace PennerKombat
{
    /// <summary>
    /// (3) Räumliche Hitbox — docs/3D.md §3.2.
    /// Vier Formen: Kugel, Box, Kapsel und **Kegel** (Frontalangriffe mit
    /// Öffnungswinkel). Wird an einen Kinderknoten des Kämpfers gehängt
    /// (z. B. an die Faust) und über <see cref="Activate"/> für die aktiven
    /// Frames scharf geschaltet.
    ///
    /// Trifft jedes Ziel nur einmal pro Aktivierung und meldet den Treffer
    /// über <see cref="FighterController.PlayHitFeedback"/> an die VFX-Schicht.
    /// </summary>
    public class Hitbox3D : MonoBehaviour
    {
        public enum Shape { Sphere, Box, Capsule, Cone }

        [Header("Form")]
        public Shape shape = Shape.Sphere;
        public float radius = 1.2f;                     // Sphere / Capsule / Cone
        public Vector3 boxSize = new Vector3(2f, 1.5f, 1.5f);
        public float capsuleHeight = 2f;
        [Range(10f, 180f)] public float coneAngle = 70f;

        [Header("Wirkung")]
        public float damage = 10f;
        public HitTier tier = HitTier.Light;
        public Vector3 knockback = new Vector3(0f, 2f, 6f);   // lokal: x seitlich, y hoch, z vorwärts
        public float hitStun = 0.2f;
        public LayerMask targetLayer = ~0;

        [Header("Debug")]
        public bool drawGizmo = true;

        private FighterController owner;
        private readonly HashSet<FighterController> alreadyHit = new HashSet<FighterController>();
        private readonly Collider[] buffer = new Collider[16];
        private bool active;

        void Awake()
        {
            owner = GetComponentInParent<FighterController>();
        }

        /// <summary>Hitbox scharf schalten (Trefferliste wird geleert).</summary>
        public void Activate()
        {
            alreadyHit.Clear();
            active = true;
            Check();
        }

        public void Deactivate() => active = false;

        /// <summary>Einmalige Prüfung ohne Dauerbetrieb (z. B. aus einem Animation Event).</summary>
        public int Strike()
        {
            alreadyHit.Clear();
            active = true;
            int hits = Check();
            active = false;
            return hits;
        }

        void FixedUpdate()
        {
            if (active) Check();
        }

        int Check()
        {
            int count = 0;
            Vector3 pos = transform.position;
            Quaternion rot = transform.rotation;

            int found;
            switch (shape)
            {
                case Shape.Box:
                    found = Physics.OverlapBoxNonAlloc(pos, boxSize * 0.5f, buffer, rot, targetLayer,
                                                       QueryTriggerInteraction.Ignore);
                    break;

                case Shape.Capsule:
                    Vector3 up = rot * Vector3.up * (capsuleHeight * 0.5f);
                    found = Physics.OverlapCapsuleNonAlloc(pos + up, pos - up, radius, buffer, targetLayer,
                                                           QueryTriggerInteraction.Ignore);
                    break;

                default:    // Sphere und Cone starten beide als Kugel
                    found = Physics.OverlapSphereNonAlloc(pos, radius, buffer, targetLayer,
                                                          QueryTriggerInteraction.Ignore);
                    break;
            }

            for (int i = 0; i < found; i++)
            {
                var col = buffer[i];
                if (col == null) continue;

                var target = col.GetComponentInParent<FighterController>();
                if (target == null || target == owner || alreadyHit.Contains(target)) continue;

                // Kegel: zusätzlich den Öffnungswinkel prüfen
                if (shape == Shape.Cone)
                {
                    Vector3 toTarget = target.transform.position - pos;
                    toTarget.y = 0f;
                    if (toTarget.sqrMagnitude < 0.0001f) continue;
                    float angle = Vector3.Angle(transform.forward, toTarget.normalized);
                    if (angle > coneAngle * 0.5f) continue;
                }

                alreadyHit.Add(target);
                ApplyHit(target, col);
                count++;
            }
            return count;
        }

        void ApplyHit(FighterController target, Collider col)
        {
            Vector3 dir = transform.forward * knockback.z
                        + transform.right * knockback.x
                        + Vector3.up * knockback.y;

            target.TakeDamage(damage, dir.normalized, owner);
            if (hitStun > 0f) target.ApplyGrabStun(hitStun);

            Vector3 impact = col.ClosestPoint(transform.position);
            owner?.PlayHitFeedback(target, impact, damage, tier);
        }

        void OnDrawGizmosSelected()
        {
            if (!drawGizmo) return;
            Gizmos.color = active ? PennerPalette.BloodRed : PennerPalette.Gold.WithAlpha(0.5f);
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);

            switch (shape)
            {
                case Shape.Box:
                    Gizmos.DrawWireCube(Vector3.zero, boxSize);
                    break;

                case Shape.Capsule:
                    Gizmos.DrawWireSphere(Vector3.up * capsuleHeight * 0.5f, radius);
                    Gizmos.DrawWireSphere(Vector3.down * capsuleHeight * 0.5f, radius);
                    break;

                case Shape.Cone:
                    Gizmos.DrawWireSphere(Vector3.zero, radius);
                    float half = coneAngle * 0.5f * Mathf.Deg2Rad;
                    Vector3 a = new Vector3(Mathf.Sin(half), 0f, Mathf.Cos(half)) * radius;
                    Vector3 b = new Vector3(-Mathf.Sin(half), 0f, Mathf.Cos(half)) * radius;
                    Gizmos.DrawLine(Vector3.zero, a);
                    Gizmos.DrawLine(Vector3.zero, b);
                    break;

                default:
                    Gizmos.DrawWireSphere(Vector3.zero, radius);
                    break;
            }
        }
    }
}
