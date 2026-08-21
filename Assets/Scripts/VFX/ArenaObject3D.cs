using System.Collections;
using UnityEngine;

namespace PennerKombat
{
    /// <summary>
    /// (6) Interaktives Objekt im 3D-Raum — docs/3D.md §4.2.
    /// Ergänzt die statischen Props aus <see cref="ArenaProp"/> um Dinge, die
    /// der Spieler aktiv benutzt: aufheben und werfen, Trampolin, Falle,
    /// Deckung. Aufgerufen wird das über die Interaktionstaste
    /// (`E` bzw. `Num .`) via <see cref="ArenaInteraction"/>.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class ArenaObject3D : MonoBehaviour
    {
        public enum ObjectKind { Throwable, Explosive, Trampoline, Trap, Cover }

        [Header("Typ")]
        public ObjectKind kind = ObjectKind.Throwable;

        [Header("Werte")]
        public float weight = 10f;
        public float throwForce = 14f;
        public float throwDamage = 12f;
        public float explosionRadius = 4f;
        public float explosionDamage = 25f;
        public float bounceForce = 15f;
        public float trapStun = 0.5f;
        public float trapDamage = 5f;
        public float respawnTime = 6f;

        [Header("Highlight")]
        public Color highlightColor = PennerPalette.WarmOrange;

        private Rigidbody rb;
        private Renderer[] renderers;
        private Color[] originalColors;
        private Vector3 startPos;
        private Quaternion startRot;
        private FighterController thrower;
        private bool inFlight;
        private bool consumed;

        public bool CanInteract => !consumed && gameObject.activeInHierarchy;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.mass = weight;
            rb.isKinematic = true;

            renderers = GetComponentsInChildren<Renderer>();
            originalColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
                originalColors[i] = renderers[i].material != null ? renderers[i].material.color : Color.white;

            startPos = transform.position;
            startRot = transform.rotation;
        }

        // ==================================================================
        //  Benutzung
        // ==================================================================

        public void Interact(FighterController user)
        {
            if (!CanInteract || user == null) return;

            switch (kind)
            {
                case ObjectKind.Throwable:  Throw(user);      break;
                case ObjectKind.Explosive:  Explode(user);    break;
                case ObjectKind.Trampoline: Bounce(user);     break;
                case ObjectKind.Trap:       Trigger(user);    break;
                case ObjectKind.Cover:      /* nur Deckung */ break;
            }
        }

        void Throw(FighterController user)
        {
            thrower = user;
            inFlight = true;
            consumed = true;

            transform.position = user.transform.position + user.transform.forward * 1.2f + Vector3.up * 1.2f;
            rb.isKinematic = false;
            rb.velocity = Vector3.zero;
            rb.AddForce((user.transform.forward + Vector3.up * 0.25f).normalized * throwForce, ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * 8f, ForceMode.Impulse);

            FloatingText.Show(transform.position + Vector3.up, "GEWORFEN", PennerPalette.Gold, 0.8f);
            StartCoroutine(Respawn());
        }

        void Explode(FighterController source)
        {
            consumed = true;
            ComboExplosion3D.Ensure().Spawn(transform.position, explosionRadius,
                                            PennerPalette.WarmOrange, explosionDamage, 12f, source);
            gameObject.SetActive(false);
            StartCoroutine(Respawn());
        }

        void Bounce(FighterController user)
        {
            var urb = user.GetComponent<Rigidbody>();
            if (urb == null) return;
            urb.velocity = new Vector3(urb.velocity.x, 0f, urb.velocity.z);
            urb.AddForce(Vector3.up * bounceForce + user.transform.forward * 3f, ForceMode.Impulse);
            VFXManager.Instance?.PlayDust(transform.position, 0.8f);
            FloatingText.Show(transform.position + Vector3.up, "BOING", PennerPalette.NeonBlue, 0.7f);
        }

        void Trigger(FighterController victim)
        {
            consumed = true;
            victim.ApplyGrabStun(trapStun);
            victim.TakeDamage(trapDamage, Vector3.up, null);
            VFXManager.Instance?.PlayGlass(victim.transform.position, Vector3.up);
            StartCoroutine(Respawn());
        }

        // ==================================================================
        //  Geworfenes Objekt trifft
        // ==================================================================

        void OnCollisionEnter(Collision collision)
        {
            if (!inFlight) return;

            var target = collision.collider.GetComponentInParent<FighterController>();
            if (target != null && target != thrower)
            {
                Vector3 dir = (target.transform.position - transform.position).normalized;
                target.TakeDamage(throwDamage, dir, thrower);
                thrower?.PlayHitFeedback(target, collision.GetContact(0).point, throwDamage, HitTier.Heavy);
                inFlight = false;
            }
            else if (collision.relativeVelocity.magnitude > 4f)
            {
                VFXManager.Instance?.PlayDust(transform.position, 0.5f);
                inFlight = false;
            }
        }

        // ==================================================================
        //  Highlight & Respawn
        // ==================================================================

        public void Highlight(bool on)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null || renderers[i].material == null) continue;
                renderers[i].material.color = on
                    ? Color.Lerp(originalColors[i], highlightColor, 0.65f)
                    : originalColors[i];
            }
        }

        IEnumerator Respawn()
        {
            yield return new WaitForSeconds(respawnTime);

            transform.SetPositionAndRotation(startPos, startRot);
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            inFlight = false;
            consumed = false;
            thrower = null;
            gameObject.SetActive(true);
            Highlight(false);
        }
    }

    /// <summary>
    /// Hängt am Kämpfer: sucht das nächste benutzbare Objekt in Reichweite,
    /// hebt es optisch hervor und löst es auf Tastendruck aus.
    /// </summary>
    [RequireComponent(typeof(FighterController))]
    public class ArenaInteraction : MonoBehaviour
    {
        public float reach = 2.2f;

        private FighterController fighter;
        private ArenaObject3D current;
        private readonly Collider[] buffer = new Collider[12];

        void Awake() => fighter = GetComponent<FighterController>();

        void Update()
        {
            ArenaObject3D nearest = FindNearest();

            if (nearest != current)
            {
                current?.Highlight(false);
                current = nearest;
                current?.Highlight(true);
            }

            if (current == null || fighter.isAI || FighterInput.Instance == null) return;
            if (FighterInput.Instance.GetInteract(fighter.playerIndex))
                current.Interact(fighter);
        }

        ArenaObject3D FindNearest()
        {
            int found = Physics.OverlapSphereNonAlloc(transform.position, reach, buffer,
                                                      ~0, QueryTriggerInteraction.Ignore);
            ArenaObject3D best = null;
            float bestDist = float.MaxValue;

            for (int i = 0; i < found; i++)
            {
                var obj = buffer[i] != null ? buffer[i].GetComponentInParent<ArenaObject3D>() : null;
                if (obj == null || !obj.CanInteract) continue;

                float d = Vector3.Distance(transform.position, obj.transform.position);
                if (d >= bestDist) continue;
                bestDist = d;
                best = obj;
            }
            return best;
        }

        void OnDisable() => current?.Highlight(false);
    }
}
