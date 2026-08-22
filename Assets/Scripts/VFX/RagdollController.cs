using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PennerKombat
{
    /// <summary>
    /// Ragdoll beim K.o. (docs/EXTRAS.md §3). Ist am Charakter ein Ragdoll-Rig
    /// vorhanden (Collider + Rigidbodies an den Knochen), wird es aktiviert;
    /// sonst baut die Komponente aus den vorhandenen Renderern eine einfache
    /// „Bruchbude" aus Primitives, damit auch Platzhalter-Kapseln glaubwürdig
    /// umfallen.
    /// </summary>
    [RequireComponent(typeof(FighterController))]
    public class RagdollController : MonoBehaviour
    {
        [Header("Verhalten")]
        public float force = 9f;
        public float upwards = 4f;
        public float lifetime = 5f;
        [Tooltip("Ohne echtes Rig: Ersatzkörper aus Primitives werfen.")]
        public bool fallbackChunks = true;
        public int chunkCount = 5;

        private FighterController fighter;
        private Animator anim;
        private Rigidbody body;
        private Collider mainCollider;

        private readonly List<Rigidbody> bones = new List<Rigidbody>();
        private readonly List<Collider> boneColliders = new List<Collider>();
        private readonly List<GameObject> chunks = new List<GameObject>();
        private bool active;

        void Awake()
        {
            fighter = GetComponent<FighterController>();
            anim = GetComponent<Animator>();
            body = GetComponent<Rigidbody>();
            mainCollider = GetComponent<Collider>();

            // Vorhandenes Rig einsammeln (alles außer dem Wurzelobjekt)
            foreach (var rb in GetComponentsInChildren<Rigidbody>(true))
                if (rb.gameObject != gameObject) bones.Add(rb);
            foreach (var col in GetComponentsInChildren<Collider>(true))
                if (col.gameObject != gameObject) boneColliders.Add(col);

            SetRigActive(false);
        }

        void OnEnable()
        {
            if (fighter != null) fighter.OnDeath += HandleDeath;
        }

        void OnDisable()
        {
            if (fighter != null) fighter.OnDeath -= HandleDeath;
        }

        void HandleDeath(FighterController dead)
        {
            Activate(-transform.forward);
        }

        /// <summary>Ragdoll auslösen (auch von Fatalities nutzbar).</summary>
        public void Activate(Vector3 direction)
        {
            if (active) return;
            active = true;

            if (anim != null) anim.enabled = false;
            if (mainCollider != null) mainCollider.enabled = false;
            if (body != null) body.isKinematic = true;

            if (bones.Count > 0)
            {
                SetRigActive(true);
                foreach (var rb in bones)
                {
                    Vector3 dir = direction.normalized + Random.insideUnitSphere * 0.4f;
                    rb.AddForce(dir * force + Vector3.up * upwards, ForceMode.Impulse);
                }
            }
            else if (fallbackChunks)
            {
                SpawnChunks(direction);
            }

            VFXManager.Instance?.PlayDust(transform.position, 1.2f);
            CameraShake.Shake(6f, 0.15f);
            StartCoroutine(Cleanup());
        }

        void SpawnChunks(Vector3 direction)
        {
            // Grobe Bruchstücke in der Signaturfarbe des Kämpfers
            Color color = PennerPalette.ForCharacter(fighter.fighterId);

            for (int i = 0; i < chunkCount; i++)
            {
                var go = GameObject.CreatePrimitive(i == 0 ? PrimitiveType.Sphere : PrimitiveType.Capsule);
                go.name = "PK_Chunk";
                go.transform.position = transform.position + Vector3.up * (0.4f + i * 0.35f);
                go.transform.rotation = Random.rotation;
                go.transform.localScale = i == 0
                    ? Vector3.one * 0.45f
                    : new Vector3(0.28f, Random.Range(0.3f, 0.55f), 0.28f);

                var mr = go.GetComponent<MeshRenderer>();
                mr.material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                mr.material.color = Color.Lerp(color, PennerPalette.Earth, 0.35f);

                var rb = go.AddComponent<Rigidbody>();
                rb.mass = 1.2f;
                rb.AddForce(direction.normalized * force + Vector3.up * upwards
                            + Random.insideUnitSphere * 2f, ForceMode.Impulse);
                rb.AddTorque(Random.insideUnitSphere * 6f, ForceMode.Impulse);

                chunks.Add(go);
            }

            // Der Kämpfer selbst verschwindet, die Teile bleiben liegen
            foreach (var r in GetComponentsInChildren<Renderer>())
                if (!(r is ParticleSystemRenderer) && !(r is TrailRenderer)) r.enabled = false;
        }

        void SetRigActive(bool value)
        {
            foreach (var rb in bones) if (rb != null) rb.isKinematic = !value;
            foreach (var col in boneColliders) if (col != null) col.enabled = value;
        }

        IEnumerator Cleanup()
        {
            yield return new WaitForSeconds(lifetime);
            Deactivate();
        }

        /// <summary>Ragdoll zurücksetzen (Rundenstart).</summary>
        public void Deactivate()
        {
            if (!active) return;
            active = false;

            foreach (var c in chunks) if (c != null) Destroy(c);
            chunks.Clear();

            SetRigActive(false);
            foreach (var r in GetComponentsInChildren<Renderer>(true))
                if (!(r is ParticleSystemRenderer) && !(r is TrailRenderer)) r.enabled = true;

            if (anim != null) anim.enabled = true;
            if (mainCollider != null) mainCollider.enabled = true;
            if (body != null)
            {
                body.isKinematic = false;
                body.velocity = Vector3.zero;
            }
        }
    }
}
