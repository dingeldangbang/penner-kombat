using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PennerKombat
{
    /// <summary>
    /// Aufsammelbare Waffen (docs/EXTRAS.md §6): Schraubenzieher, Maulschlüssel,
    /// Rohrzange & Co. liegen in der Arena, werden mit der Interaktionstaste
    /// aufgenommen und ersetzen für einige Schläge den Standardangriff.
    /// </summary>
    public enum WeaponEffect { None, Bleed, Wallbounce, Stun, AntiAir, Poison }

    [System.Serializable]
    public class WeaponDefinition
    {
        public string id = "schraubenzieher";
        public string displayName = "Schraubenzieher";
        public float damage = 14f;
        public float range = 1.6f;
        public int uses = 6;
        public WeaponEffect effect = WeaponEffect.Bleed;
        public Color color = Color.gray;
        public HitTier tier = HitTier.Heavy;
    }

    /// <summary>Am Kämpfer: hält die aktuell getragene Waffe und schlägt mit ihr zu.</summary>
    [RequireComponent(typeof(FighterController))]
    public class WeaponHolder : MonoBehaviour
    {
        public WeaponDefinition weapon;
        public int remainingUses;

        [Tooltip("Halte-Punkt für die Waffe (z.B. Hand-Knochen oder ein Kind namens 'WeaponSlot'). "
               + "Leer = Standardposition an der Hüfte.")]
        public Transform socket;

        private FighterController fighter;
        private GameObject visual;

        public bool HasWeapon => weapon != null && remainingUses > 0;

        void Awake() => fighter = GetComponent<FighterController>();

        public void Equip(WeaponDefinition def)
        {
            weapon = def;
            remainingUses = def.uses;
            BuildVisual();
            FloatingText.Show(transform.position + Vector3.up * 2.4f,
                              def.displayName.ToUpperInvariant(), def.color, 1.1f);
        }

        void BuildVisual()
        {
            if (visual != null) Destroy(visual);
            visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(visual.GetComponent<Collider>());
            visual.name = "PK_Waffe";
            if (socket != null)
            {
                visual.transform.SetParent(socket, false);
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.Euler(20f, 0f, 60f);
            }
            else
            {
                visual.transform.SetParent(transform, false);
                visual.transform.localPosition = new Vector3(0.35f, 1.0f, 0.35f);
                visual.transform.localRotation = Quaternion.Euler(20f, 0f, 60f);
            }
            visual.transform.localScale = new Vector3(0.07f, 0.5f, 0.07f);

            var mr = visual.GetComponent<MeshRenderer>();
            mr.material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mr.material.color = weapon.color;
        }

        /// <summary>Schlag mit der Waffe. Gibt false zurück, wenn keine da ist.</summary>
        public bool Strike()
        {
            if (!HasWeapon) return false;

            var enemy = fighter.GetEnemy();
            if (enemy == null) return false;
            if (Vector3.Distance(transform.position, enemy.transform.position) > weapon.range + 1f) return false;

            remainingUses--;
            Vector3 dir = (enemy.transform.position - transform.position).normalized;

            enemy.TakeDamage(weapon.damage, dir, fighter);
            fighter.PlayHitFeedback(enemy, weapon.damage, weapon.tier);
            ApplyEffect(enemy, dir);

            if (remainingUses <= 0) Drop();
            return true;
        }

        void ApplyEffect(FighterController enemy, Vector3 dir)
        {
            switch (weapon.effect)
            {
                case WeaponEffect.Bleed:
                    StartCoroutine(Bleed(enemy, 5f, 5));
                    break;
                case WeaponEffect.Wallbounce:
                    enemy.ApplyImpulse(dir * 16f + Vector3.up * 3f);
                    VFXManager.Instance?.PlayDust(enemy.transform.position, 1.3f);
                    break;
                case WeaponEffect.Stun:
                    enemy.ApplyGrabStun(0.4f);
                    break;
                case WeaponEffect.AntiAir:
                    enemy.ApplyImpulse(Vector3.up * 11f);
                    break;
                case WeaponEffect.Poison:
                    VFXManager.Instance?.PlayPoison(enemy.transform.position + Vector3.up, 1.2f);
                    StartCoroutine(Bleed(enemy, 6f, 6));
                    break;
            }
        }

        IEnumerator Bleed(FighterController enemy, float duration, int ticks)
        {
            float step = duration / Mathf.Max(1, ticks);
            for (int i = 0; i < ticks; i++)
            {
                yield return new WaitForSeconds(step);
                if (enemy == null || !enemy.gameObject.activeInHierarchy) yield break;
                enemy.TakeDamage(1.5f, Vector3.down, fighter);
            }
        }

        public void Drop()
        {
            weapon = null;
            remainingUses = 0;
            if (visual != null) Destroy(visual);
        }
    }

    /// <summary>In der Arena liegende Waffe — Aufheben per Interaktionstaste.</summary>
    public class WeaponPickup : MonoBehaviour
    {
        public WeaponDefinition definition = new WeaponDefinition();
        public float pickupRange = 1.6f;
        public float respawnTime = 15f;

        private bool taken;

        /// <summary>Erzeugt eine liegende Waffe an der Position.</summary>
        public static WeaponPickup Spawn(WeaponDefinition def, Vector3 position)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Waffe_" + def.id;
            go.transform.position = position + Vector3.up * 0.2f;
            go.transform.localScale = new Vector3(0.1f, 0.1f, 0.6f);
            Destroy(go.GetComponent<Collider>());

            var mr = go.GetComponent<MeshRenderer>();
            mr.material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mr.material.color = def.color;

            var pickup = go.AddComponent<WeaponPickup>();
            pickup.definition = def;
            return pickup;
        }

        void Update()
        {
            if (taken) return;
            transform.Rotate(Vector3.up, 60f * Time.deltaTime, Space.World);

            foreach (var f in FindObjectsOfType<FighterController>())
            {
                if (Vector3.Distance(f.transform.position, transform.position) > pickupRange) continue;
                bool wants = f.isAI || (FighterInput.Instance != null &&
                                        FighterInput.Instance.GetInteract(f.playerIndex));
                if (!wants) continue;

                var holder = f.GetComponent<WeaponHolder>() ?? f.gameObject.AddComponent<WeaponHolder>();
                holder.Equip(definition);
                Take();
                break;
            }
        }

        void Take()
        {
            taken = true;
            foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;
            Invoke(nameof(Respawn), respawnTime);
        }

        void Respawn()
        {
            taken = false;
            foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = true;
            VFXManager.Instance?.PlayDust(transform.position, 0.4f);
        }

        /// <summary>Standard-Arsenal des Hinterhofs.</summary>
        public static readonly WeaponDefinition[] Arsenal =
        {
            new WeaponDefinition { id = "schraubenzieher", displayName = "Schraubenzieher", damage = 14f, effect = WeaponEffect.Bleed,      color = new Color(0.75f, 0.2f, 0.2f) },
            new WeaponDefinition { id = "maulschluessel", displayName = "Maulschlüssel",   damage = 16f, effect = WeaponEffect.Wallbounce, color = new Color(0.7f, 0.7f, 0.75f) },
            new WeaponDefinition { id = "rohrzange",      displayName = "Rohrzange",       damage = 18f, effect = WeaponEffect.AntiAir,    color = new Color(0.4f, 0.45f, 0.5f) },
            new WeaponDefinition { id = "kochloeffel",    displayName = "Kochlöffel",      damage = 9f,  effect = WeaponEffect.Stun,       color = new Color(0.8f, 0.6f, 0.3f), tier = HitTier.Light },
            new WeaponDefinition { id = "ratengift",      displayName = "Rattengift",      damage = 8f,  effect = WeaponEffect.Poison,     color = new Color(0.2f, 0.6f, 0.2f), tier = HitTier.Special }
        };
    }
}
