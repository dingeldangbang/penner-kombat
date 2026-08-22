using System.Collections.Generic;
using UnityEngine;

namespace PennerKombat
{
    /// <summary>
    /// Sichtbare Wunden und Blutspuren (docs/EXTRAS.md §4).
    /// Jeder Treffer hinterlässt einen Fleck am Körper und eine Lache am Boden;
    /// je niedriger die HP, desto fahler wird der Kämpfer. Alles prozedural,
    /// keine Decal-Assets nötig.
    /// </summary>
    [RequireComponent(typeof(FighterController))]
    public class DamageVisuals : MonoBehaviour
    {
        [Header("Grenzen")]
        public int maxWounds = 12;
        public int maxPuddles = 10;
        public float minDamageForWound = 6f;
        public float puddleLifetime = 20f;

        [Header("Aussehen")]
        public bool paleWhenHurt = true;

        private FighterController fighter;
        private readonly List<GameObject> wounds = new List<GameObject>();
        private readonly Queue<GameObject> puddles = new Queue<GameObject>();
        private readonly List<Renderer> bodyRenderers = new List<Renderer>();
        private readonly List<Color> bodyBaseColors = new List<Color>();

        void Awake()
        {
            fighter = GetComponent<FighterController>();

            foreach (var r in GetComponentsInChildren<Renderer>())
            {
                if (r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer) continue;
                if (r.material == null) continue;
                bodyRenderers.Add(r);
                bodyBaseColors.Add(r.material.color);
            }
        }

        void OnEnable()
        {
            if (fighter != null) fighter.OnDamaged += HandleDamaged;
        }

        void OnDisable()
        {
            if (fighter != null) fighter.OnDamaged -= HandleDamaged;
        }

        void HandleDamaged(FighterController target, FighterController attacker, float damage)
        {
            Vector3 dir = attacker != null
                ? (transform.position - attacker.transform.position).normalized
                : -transform.forward;

            AddWound(damage, dir);
            AddPuddle(damage);
            UpdatePallor();
        }

        void AddWound(float damage, Vector3 fromDirection)
        {
            if (damage < minDamageForWound) return;

            // Ältere Wunde recyceln, statt endlos anzuhäufen
            if (wounds.Count >= maxWounds)
            {
                var oldest = wounds[0];
                wounds.RemoveAt(0);
                if (oldest != null) Destroy(oldest);
            }

            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(quad.GetComponent<Collider>());
            quad.name = "PK_Wunde";
            quad.transform.SetParent(transform, false);

            // Auf der Trefferseite, zufällig auf Rumpfhöhe
            Vector3 local = new Vector3(
                Random.Range(-0.25f, 0.25f),
                Random.Range(0.5f, 1.5f),
                0.28f);
            quad.transform.localPosition = local + transform.InverseTransformDirection(fromDirection) * 0.1f;
            quad.transform.localRotation = Quaternion.Euler(0f, 180f, Random.Range(0f, 360f));

            float size = Mathf.Lerp(0.12f, 0.4f, Mathf.Clamp01(damage / 35f));
            quad.transform.localScale = Vector3.one * size;

            var mr = quad.GetComponent<MeshRenderer>();
            mr.material = new Material(Shader.Find("Sprites/Default"));
            mr.material.color = Color.Lerp(PennerPalette.BloodRed, PennerPalette.Hex("4A0000"), Random.value)
                                     .WithAlpha(0.85f);
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            wounds.Add(quad);
        }

        void AddPuddle(float damage)
        {
            if (damage < minDamageForWound * 1.5f) return;

            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(quad.GetComponent<Collider>());
            quad.name = "PK_Blutlache";
            quad.transform.position = new Vector3(transform.position.x, 0.03f, transform.position.z)
                                    + new Vector3(Random.Range(-0.3f, 0.3f), 0f, Random.Range(-0.3f, 0.3f));
            quad.transform.rotation = Quaternion.Euler(90f, Random.Range(0f, 360f), 0f);
            quad.transform.localScale = Vector3.one * Mathf.Lerp(0.4f, 1.3f, Mathf.Clamp01(damage / 40f));

            var mr = quad.GetComponent<MeshRenderer>();
            mr.material = new Material(Shader.Find("Sprites/Default"));
            mr.material.color = PennerPalette.BloodRed.WithAlpha(0.55f);
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            puddles.Enqueue(quad);
            if (puddles.Count > maxPuddles)
            {
                var old = puddles.Dequeue();
                if (old != null) Destroy(old);
            }
            Destroy(quad, puddleLifetime);
        }

        /// <summary>Bei wenig HP wird der Kämpfer fahl und blutverschmiert.</summary>
        void UpdatePallor()
        {
            if (!paleWhenHurt || fighter == null) return;
            float health = Mathf.Clamp01(fighter.currentHP / Mathf.Max(1f, fighter.maxHP));

            for (int i = 0; i < bodyRenderers.Count; i++)
            {
                var r = bodyRenderers[i];
                if (r == null || r.material == null) continue;
                r.material.color = Color.Lerp(
                    Color.Lerp(bodyBaseColors[i], PennerPalette.BloodRed, 0.35f),   // schwer verletzt
                    bodyBaseColors[i],                                              // unversehrt
                    health);
            }
        }

        /// <summary>Rundenreset: Wunden und Lachen entfernen.</summary>
        public void Clear()
        {
            foreach (var w in wounds) if (w != null) Destroy(w);
            wounds.Clear();
            while (puddles.Count > 0)
            {
                var p = puddles.Dequeue();
                if (p != null) Destroy(p);
            }
            for (int i = 0; i < bodyRenderers.Count; i++)
                if (bodyRenderers[i] != null && bodyRenderers[i].material != null)
                    bodyRenderers[i].material.color = bodyBaseColors[i];
        }
    }
}
