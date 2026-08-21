using System.Collections;
using UnityEngine;

namespace PennerKombat
{
    /// <summary>
    /// (5) Räumliche Explosionen — docs/3D.md §7.2.
    /// Erzeugt eine echte 3D-Druckwelle: expandierende Kugel + Bodenring +
    /// Funken + Staub, dazu optional physikalischer Rückstoß auf Kämpfer und
    /// lose Arena-Objekte. Alles prozedural, kein Prefab nötig.
    /// </summary>
    public class ComboExplosion3D : MonoBehaviour
    {
        public static ComboExplosion3D Instance { get; private set; }

        [Header("Standardwerte")]
        public float defaultDuration = 0.6f;
        public LayerMask forceLayers = ~0;

        public static ComboExplosion3D Ensure()
        {
            if (Instance == null)
            {
                var go = new GameObject("~ComboExplosion3D");
                Instance = go.AddComponent<ComboExplosion3D>();
            }
            return Instance;
        }

        void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(this); return; }
        }

        /// <summary>
        /// Explosion an <paramref name="position"/>.
        /// <paramref name="size"/> in Metern (Endradius der Druckwelle).
        /// </summary>
        public void Spawn(Vector3 position, float size, Color? tint = null,
                          float damage = 0f, float force = 0f, FighterController source = null)
        {
            Color color = tint ?? PennerPalette.WarmOrange;
            StartCoroutine(Blast(position, Mathf.Max(0.3f, size), color));

            var vfx = VFXManager.Instance;
            vfx?.PlayHit(position, Vector3.up, damage > 0f ? damage : size * 6f,
                         size > 3f ? HitTier.Ex : HitTier.Special, null);
            vfx?.PlayDust(position, size * 0.6f);

            CameraShake.Shake(Mathf.Clamp(size * 2.5f, 2f, 14f), 0.12f + size * 0.02f);

            if (damage > 0f || force > 0f) ApplyForce(position, size, damage, force, source);
        }

        /// <summary>Combo-Stufe → Explosion (wird vom ComboSystem ab Stufe 11 genutzt).</summary>
        public void SpawnForCombo(Vector3 position, int combo)
        {
            float size = Mathf.Lerp(1.2f, 5f, Mathf.Clamp01(combo / 25f));
            Spawn(position, size, PennerPalette.ForCombo(combo));
        }

        // ------------------------------------------------------------------

        IEnumerator Blast(Vector3 pos, float size, Color color)
        {
            // Kugelwelle
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(sphere.GetComponent<Collider>());
            sphere.name = "PK_Blast";
            sphere.transform.position = pos;
            var sphereRenderer = sphere.GetComponent<MeshRenderer>();
            sphereRenderer.material = TransparentMaterial(color);
            sphereRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // Bodenring
            var ring = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(ring.GetComponent<Collider>());
            ring.name = "PK_BlastRing";
            ring.transform.position = new Vector3(pos.x, 0.06f, pos.z);
            ring.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            var ringRenderer = ring.GetComponent<MeshRenderer>();
            ringRenderer.material = TransparentMaterial(color);
            ringRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            float t = 0f;
            while (t < defaultDuration)
            {
                t += Time.unscaledDeltaTime;
                float k = t / defaultDuration;

                float radius = Mathf.Lerp(0.2f, size, Mathf.Sqrt(k));   // schnell auf, dann auslaufen
                sphere.transform.localScale = Vector3.one * radius * 2f;
                ring.transform.localScale = Vector3.one * radius * 3f;

                float alpha = Mathf.Lerp(0.55f, 0f, k);
                SetAlpha(sphereRenderer, color, alpha);
                SetAlpha(ringRenderer, color, alpha * 0.8f);
                yield return null;
            }

            Destroy(sphere);
            Destroy(ring);
        }

        void ApplyForce(Vector3 pos, float radius, float damage, float force, FighterController source)
        {
            var hits = Physics.OverlapSphere(pos, radius, forceLayers, QueryTriggerInteraction.Ignore);
            var pushed = new System.Collections.Generic.HashSet<Rigidbody>();

            foreach (var col in hits)
            {
                var rb = col.attachedRigidbody;
                if (rb == null || pushed.Contains(rb)) continue;
                pushed.Add(rb);

                float falloff = 1f - Mathf.Clamp01(Vector3.Distance(rb.position, pos) / radius);

                var fighter = col.GetComponentInParent<FighterController>();
                if (fighter != null && fighter != source && damage > 0f)
                {
                    Vector3 dir = (fighter.transform.position - pos).normalized;
                    fighter.TakeDamage(damage * falloff, dir, source);
                }

                if (force > 0f && !rb.isKinematic)
                    rb.AddExplosionForce(force, pos, radius, 0.6f, ForceMode.Impulse);
            }
        }

        static Material TransparentMaterial(Color color)
        {
            var shader = Shader.Find("Sprites/Default");
            var mat = new Material(shader) { color = color.WithAlpha(0.5f) };
            return mat;
        }

        static void SetAlpha(Renderer renderer, Color baseColor, float alpha)
        {
            if (renderer == null || renderer.material == null) return;
            renderer.material.color = baseColor.WithAlpha(alpha);
        }
    }
}
