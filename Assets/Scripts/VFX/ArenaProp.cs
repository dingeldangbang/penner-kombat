using System.Collections;
using UnityEngine;

namespace PennerKombat
{
    /// <summary>
    /// Interaktives Arena-Objekt im Hinterhof „Zum Blauen Eimer" (Spec §3.1).
    /// Deckt alle Verhaltensweisen der Prop-Tabelle ab: umwerfbar, explosiv,
    /// Stolper-Hazard, Wäscheleine (Stun), kletterbar (Stage-Fatality-Zone)
    /// und rein kosmetisch.
    /// </summary>
    public class ArenaProp : MonoBehaviour
    {
        public enum PropKind
        {
            BeerCrateTower,   // 14 % Schaden, wird zum Boden-Hazard
            GasBottle,        // 4 Treffer → Explosion 25 %, 5 m Radius
            LaundryLine,      // 0,3 s Stun, Combo-Extender
            PaulaBowl,        // kosmetisch, „unheilvoller Ton"
            TrashCan,         // rollt, 7 % Schaden
            Scaffold,         // kletterbar, Stage-Fatality-Zone
            NeonSign,         // Atmosphäre, flackert bei Chaos schneller
            Cobblestones      // Boden, Ölflecken wachsen
        }

        [Header("Typ")]
        public PropKind kind = PropKind.BeerCrateTower;

        [Header("Werte")]
        public int hitsToBreak = 4;
        public float impactDamage = 14f;
        public float explosionDamage = 25f;
        public float explosionRadius = 5f;
        public float stunDuration = 0.3f;

        [Header("Zustand")]
        public bool tilted;
        public bool destroyed;

        private int hits;
        private Vector3 startPos;
        private Quaternion startRot;

        void Start()
        {
            startPos = transform.position;
            startRot = transform.rotation;
            ArenaManager.Instance?.interactiveProps.Add(gameObject);
            if (kind == PropKind.GasBottle)
                ArenaManager.Instance?.explosiveProps.Add(gameObject);
        }

        /// <summary>Treffer durch einen Kämpfer oder ein Projektil.</summary>
        public void RegisterHit(FighterController source)
        {
            if (destroyed) return;
            hits++;

            switch (kind)
            {
                case PropKind.GasBottle:
                    // Ventil zischt bei jedem Treffer, explodiert beim vierten
                    VFXManager.Instance?.PlayDust(transform.position + Vector3.up * 0.5f, 0.5f);
                    if (hits >= hitsToBreak) Explode(source);
                    break;

                case PropKind.BeerCrateTower:
                    Tilt();
                    VFXManager.Instance?.PlayGlass(transform.position + Vector3.up, Vector3.up);
                    DamageNearby(impactDamage, 2.5f, source);
                    SpawnHazard(HazardKind.Glass);
                    break;

                case PropKind.TrashCan:
                    Tilt();
                    StartCoroutine(RollAway());
                    DamageNearby(7f, 2f, source);
                    break;

                case PropKind.LaundryLine:
                    Snap();
                    break;

                case PropKind.PaulaBowl:
                    Tilt();
                    FloatingText.Show(transform.position + Vector3.up, "…", PennerPalette.NeonBlue, 1.4f);
                    break;

                default:
                    Tilt();
                    break;
            }
        }

        /// <summary>Kippen (auch vom Mops-Kommando aufgerufen).</summary>
        public void Tilt()
        {
            if (tilted || destroyed) return;
            tilted = true;
            StartCoroutine(TiltRoutine());
            VFXManager.Instance?.PlayDust(transform.position, 1f);
        }

        IEnumerator TiltRoutine()
        {
            float dur = Random.Range(0.3f, 0.8f);
            float t = 0f;
            Quaternion from = transform.rotation;
            Quaternion to = from * Quaternion.Euler(Random.Range(35f, 92f), Random.Range(-30f, 30f), 0f);
            while (t < dur)
            {
                t += Time.deltaTime;
                transform.rotation = Quaternion.Slerp(from, to, (t / dur).EaseOutCubic());
                yield return null;
            }
        }

        IEnumerator RollAway()
        {
            Vector3 dir = new Vector3(Random.value < 0.5f ? -1f : 1f, 0f, 0f);
            float t = 0f;
            while (t < 1.2f)
            {
                t += Time.deltaTime;
                transform.position += dir * 3f * Time.deltaTime;
                transform.Rotate(Vector3.forward, -dir.x * 360f * Time.deltaTime, Space.World);
                DamageNearby(7f, 1.2f, null, once: false);
                yield return null;
            }
        }

        void Snap()
        {
            // Wäscheleine reißt: Unterhosen fallen, kurzer Stun für alle darunter
            destroyed = true;
            foreach (Transform child in transform)
            {
                var rb = child.gameObject.GetComponent<Rigidbody>() ?? child.gameObject.AddComponent<Rigidbody>();
                rb.AddForce(Random.insideUnitSphere * 2f, ForceMode.Impulse);
            }
            foreach (var f in FindObjectsOfType<FighterController>())
                if (Vector3.Distance(f.transform.position, transform.position) < 3f)
                    f.ApplyGrabStun(stunDuration);
        }

        public void Explode(FighterController source)
        {
            if (destroyed) return;
            destroyed = true;

            VFXManager.Instance?.PlayFire(transform.position + Vector3.up * 0.6f, Vector3.up, 40);
            VFXManager.Instance?.PlayDust(transform.position, 2.5f);
            CameraShake.Shake(10f, 0.2f);
            ScreenEffects.FlashColor(PennerPalette.WarmOrange, 0.5f, 0.25f);
            DamageNearby(explosionDamage, explosionRadius, source);
            SpawnHazard(HazardKind.Fire);
            gameObject.SetActive(false);
        }

        void DamageNearby(float damage, float radius, FighterController source, bool once = true)
        {
            foreach (var f in FindObjectsOfType<FighterController>())
            {
                if (f == source && once) continue;
                float d = Vector3.Distance(f.transform.position, transform.position);
                if (d > radius) continue;
                Vector3 dir = (f.transform.position - transform.position).normalized;
                f.TakeDamage(damage, dir, source);
            }
        }

        enum HazardKind { Glass, Fire }

        void SpawnHazard(HazardKind hazardKind)
        {
            var go = new GameObject($"PK_Hazard_{hazardKind}");
            go.transform.position = new Vector3(transform.position.x, 0.05f, transform.position.z);
            var hz = go.AddComponent<ArenaHazard>();
            hz.damage = hazardKind == HazardKind.Glass ? 2f : 4f;
            hz.stun = hazardKind == HazardKind.Glass ? 0.2f : 0f;
            hz.radius = hazardKind == HazardKind.Glass ? 1.4f : 2.0f;
            hz.tint = hazardKind == HazardKind.Glass ? PennerPalette.PoisonGrn : PennerPalette.WarmOrange;
            hz.lifetime = hazardKind == HazardKind.Glass ? 12f : 6f;
        }

        public void ResetProp()
        {
            StopAllCoroutines();
            hits = 0;
            tilted = false;
            destroyed = false;
            transform.SetPositionAndRotation(startPos, startRot);
            gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Boden-Hazard aus zerstörten Props (Glassplitter, Brandfleck).
    /// Läuft ein Kämpfer hinein: kleiner Schaden + optionaler Stolper-Stun.
    /// </summary>
    public class ArenaHazard : MonoBehaviour
    {
        public float damage = 2f;
        public float stun = 0.2f;
        public float radius = 1.4f;
        public float lifetime = 12f;
        public float tickInterval = 0.8f;
        public Color tint = PennerPalette.PoisonGrn;

        private float nextTick;

        void Start()
        {
            // sichtbarer Fleck am Boden
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(quad.GetComponent<Collider>());
            quad.transform.SetParent(transform, false);
            quad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            quad.transform.localScale = Vector3.one * radius * 2f;
            var mr = quad.GetComponent<MeshRenderer>();
            mr.material = new Material(Shader.Find("Sprites/Default"));
            mr.material.color = tint.WithAlpha(0.35f);
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            Destroy(gameObject, lifetime);
        }

        void Update()
        {
            if (Time.time < nextTick) return;
            nextTick = Time.time + tickInterval;

            foreach (var f in FindObjectsOfType<FighterController>())
            {
                if (Vector3.Distance(f.transform.position, transform.position) > radius) continue;
                f.TakeDamage(damage, Vector3.up, null);
                if (stun > 0f) f.ApplyGrabStun(stun);
                VFXManager.Instance?.PlayGlass(f.transform.position, Vector3.up);
            }
        }
    }
}
