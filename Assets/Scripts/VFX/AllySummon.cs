using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PennerKombat
{
    /// <summary>
    /// Verbündete, die kurz in den Kampf eingreifen (docs/EXTRAS.md §8):
    /// die **drei Atzen** aus dem Mercedes 190e und die **Punker-Crowd**
    /// („Kantenkanten Nackenschelle"). Beide sind Platzhalter-Kapseln mit
    /// echtem Verhalten — Modelle ersetzen später nur die Renderer.
    /// </summary>
    public class AllySummon : MonoBehaviour
    {
        public static AllySummon Instance { get; private set; }

        [Header("Atzen (Mercedes 190e)")]
        public int atzenCount = 3;
        public float atzeHp = 30f;
        public float atzeDamage = 8f;
        public float atzeLifetime = 12f;
        public float mercedesCooldown = 45f;

        [Header("Punker-Crowd")]
        public int punkCount = 5;
        public int punkComboRequirement = 5;
        public float punkCooldown = 30f;

        private float mercedesTimer;
        private float punkTimer;

        public static AllySummon Ensure()
        {
            if (Instance == null)
            {
                var go = new GameObject("~AllySummon");
                Instance = go.AddComponent<AllySummon>();
            }
            return Instance;
        }

        void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(this); return; }
        }

        void Update()
        {
            if (mercedesTimer > 0f) mercedesTimer -= Time.deltaTime;
            if (punkTimer > 0f) punkTimer -= Time.deltaTime;
        }

        public bool MercedesReady => mercedesTimer <= 0f;
        public bool PunksReady => punkTimer <= 0f;

        // ==================================================================
        //  Mercedes 190e — drei Atzen steigen aus
        // ==================================================================
        public bool CallMercedes(FighterController caller)
        {
            if (!MercedesReady || caller == null) return false;
            var target = caller.GetEnemy();
            if (target == null) return false;

            mercedesTimer = mercedesCooldown;
            StartCoroutine(MercedesRoutine(caller, target));
            return true;
        }

        IEnumerator MercedesRoutine(FighterController caller, FighterController target)
        {
            // Auto fährt vor
            Vector3 entry = caller.transform.position - caller.transform.forward * 10f;
            var car = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(car.GetComponent<Collider>());
            car.name = "PK_Mercedes190e";
            car.transform.position = entry + Vector3.up * 0.7f;
            car.transform.localScale = new Vector3(1.9f, 1.2f, 4.4f);
            Paint(car, new Color(0.08f, 0.08f, 0.1f));

            FloatingText.Show(entry + Vector3.up * 2.5f, "EINMAL UM DEN BLOCK", PennerPalette.Gold, 2f);
            MusicSync.Instance?.Duck(1.2f);

            Vector3 stop = caller.transform.position - caller.transform.forward * 5f;
            float t = 0f;
            while (t < 1.6f)
            {
                t += Time.deltaTime;
                car.transform.position = Vector3.Lerp(entry, stop, t / 1.6f) + Vector3.up * 0.7f;
                yield return null;
            }
            VFXManager.Instance?.PlayDust(car.transform.position, 1.4f);

            // Drei Atzen steigen aus
            var atzen = new List<Ally>();
            for (int i = 0; i < atzenCount; i++)
            {
                Vector3 pos = stop + car.transform.right * (i - 1) * 1.3f;
                atzen.Add(Ally.Spawn($"Atze {i + 1}", pos, target, caller,
                                     atzeHp, atzeDamage, atzeLifetime,
                                     new Color(0.2f, 0.25f, 0.35f)));
                yield return new WaitForSeconds(0.25f);
            }

            // Warten, bis alle weg sind
            yield return new WaitForSeconds(atzeLifetime);

            // Auto fährt davon
            t = 0f;
            Vector3 from = car.transform.position;
            while (t < 1.5f)
            {
                t += Time.deltaTime;
                car.transform.position = Vector3.Lerp(from, entry, t / 1.5f);
                yield return null;
            }
            Destroy(car);
        }

        // ==================================================================
        //  Punker-Crowd — „Kantenkanten Nackenschelle"
        // ==================================================================
        public bool CallPunks(FighterController caller)
        {
            if (!PunksReady || caller == null) return false;
            if (caller.comboCount < punkComboRequirement) return false;
            var target = caller.GetEnemy();
            if (target == null) return false;

            punkTimer = punkCooldown;
            StartCoroutine(PunkRoutine(caller, target));
            return true;
        }

        IEnumerator PunkRoutine(FighterController caller, FighterController target)
        {
            FloatingText.Show(target.transform.position + Vector3.up * 2.8f,
                              "KANTENKANTEN NACKENSCHELLE", PennerPalette.PoisonGrn, 2f);
            CrowdReactions.Instance?.React(CrowdMood.Wild);
            MusicSync.Instance?.Duck(0.6f);

            float[] damages = { 8f, 10f, 12f, 10f, 15f };
            Color[] colors =
            {
                new Color(0.9f, 0.2f, 0.2f), new Color(0.2f, 0.8f, 0.3f),
                new Color(0.9f, 0.9f, 0.2f), new Color(0.6f, 0.3f, 0.8f),
                new Color(0.2f, 0.7f, 0.9f)
            };

            for (int i = 0; i < punkCount; i++)
            {
                float angle = (360f / punkCount) * i * Mathf.Deg2Rad;
                Vector3 from = target.transform.position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 6f;

                var punk = Ally.Spawn($"Punk {i + 1}", from, target, caller,
                                      20f, damages[i % damages.Length], 3.5f,
                                      colors[i % colors.Length]);
                punk.chargeOnce = true;

                yield return new WaitForSeconds(0.18f);
            }

            // Finaler Gemeinschaftstritt
            yield return new WaitForSeconds(1.2f);
            if (target != null && target.gameObject.activeInHierarchy)
            {
                target.TakeDamage(15f, Vector3.up, caller);
                target.ApplyImpulse(Vector3.up * 9f + caller.transform.forward * 5f);
                CameraShake.Shake(14f, 0.25f);
                ComboExplosion3D.Ensure().Spawn(target.transform.position, 3f, PennerPalette.PoisonGrn);
                CrowdReactions.Instance?.React(CrowdMood.Ekstase);
            }
        }

        static void Paint(GameObject go, Color c)
        {
            var mr = go.GetComponent<MeshRenderer>();
            if (mr == null) return;
            mr.material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mr.material.color = c;
        }
    }

    /// <summary>
    /// Ein einzelner Verbündeter (Atze oder Punk): läuft zum Ziel, schlägt zu,
    /// verschwindet nach Ablauf oder wenn er umgehauen wird.
    /// </summary>
    public class Ally : MonoBehaviour
    {
        public float hp = 30f;
        public float damage = 8f;
        public float speed = 4.5f;
        public float attackRange = 1.8f;
        public float attackInterval = 1.2f;
        [Tooltip("Nur ein einziger Angriff, dann verschwinden (Punks).")]
        public bool chargeOnce;

        private FighterController target;
        private FighterController owner;
        private float nextAttack;
        private bool finished;

        public static Ally Spawn(string name, Vector3 position, FighterController target,
                                 FighterController owner, float hp, float damage,
                                 float lifetime, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            Destroy(go.GetComponent<Collider>());
            go.name = "PK_" + name;
            go.transform.position = new Vector3(position.x, 1f, position.z);
            go.transform.localScale = new Vector3(0.55f, 0.9f, 0.55f);

            var mr = go.GetComponent<MeshRenderer>();
            mr.material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mr.material.color = color;

            var ally = go.AddComponent<Ally>();
            ally.target = target;
            ally.owner = owner;
            ally.hp = hp;
            ally.damage = damage;
            Destroy(go, lifetime);
            return ally;
        }

        void Update()
        {
            if (finished || target == null || !target.gameObject.activeInHierarchy) return;

            Vector3 to = target.transform.position - transform.position;
            to.y = 0f;
            float dist = to.magnitude;

            if (dist > attackRange)
            {
                transform.position += to.normalized * speed * Time.deltaTime;
                transform.rotation = Quaternion.LookRotation(to.normalized);
                return;
            }

            if (Time.time < nextAttack) return;
            nextAttack = Time.time + attackInterval;

            target.TakeDamage(damage, to.normalized, owner);
            target.ApplyGrabStun(0.18f);
            VFXManager.Instance?.PlayHit(target.transform.position + Vector3.up * 1.1f,
                                         to.normalized, damage, HitTier.Heavy, null);
            CameraShake.Shake(3f, 0.06f);

            if (chargeOnce)
            {
                finished = true;
                Destroy(gameObject, 0.4f);
            }
        }

        /// <summary>Verbündete können umgehauen werden.</summary>
        public void Hit(float amount)
        {
            hp -= amount;
            if (hp > 0f) return;
            VFXManager.Instance?.PlayDust(transform.position, 0.6f);
            Destroy(gameObject);
        }
    }
}
