using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PennerKombat
{
    /// <summary>
    /// Power-Ups, die während des Kampfes im Hinterhof auftauchen
    /// (docs/EXTRAS.md §7). Wer zuerst drüberläuft, kassiert den Effekt.
    /// </summary>
    public enum PowerUpKind { Bier, Med, Speed, Schild, Feuer, Eis, Muenze, Todeskuss }

    public class PowerUpSystem : MonoBehaviour
    {
        public static PowerUpSystem Instance { get; private set; }

        [Header("Spawn")]
        public float interval = 12f;
        public int maxActive = 3;
        public float arenaRadius = 8f;

        private readonly List<PowerUp> active = new List<PowerUp>();

        public static PowerUpSystem Ensure()
        {
            if (Instance == null)
            {
                var go = new GameObject("~PowerUps");
                Instance = go.AddComponent<PowerUpSystem>();
            }
            return Instance;
        }

        void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(this); return; }
        }

        void Start() => StartCoroutine(SpawnLoop());

        IEnumerator SpawnLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(interval);
                active.RemoveAll(p => p == null);
                if (active.Count >= maxActive) continue;

                var kind = (PowerUpKind)Random.Range(0, System.Enum.GetValues(typeof(PowerUpKind)).Length);
                Vector3 pos = Extensions.RandomHorizontalPoint(Vector3.zero, arenaRadius) + Vector3.up * 0.6f;
                active.Add(PowerUp.Spawn(kind, pos));
            }
        }

        public void ClearAll()
        {
            foreach (var p in active) if (p != null) Destroy(p.gameObject);
            active.Clear();
        }
    }

    /// <summary>Einzelnes Power-Up in der Arena.</summary>
    public class PowerUp : MonoBehaviour
    {
        public PowerUpKind kind = PowerUpKind.Bier;
        public float pickupRadius = 1.1f;
        public float lifetime = 20f;

        public static PowerUp Spawn(PowerUpKind kind, Vector3 position)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(go.GetComponent<Collider>());
            go.name = "PowerUp_" + kind;
            go.transform.position = position;
            go.transform.localScale = Vector3.one * 0.5f;

            var mr = go.GetComponent<MeshRenderer>();
            mr.material = new Material(Shader.Find("Sprites/Default"));
            mr.material.color = ColorFor(kind).WithAlpha(0.85f);

            var light = go.AddComponent<Light>();
            light.color = ColorFor(kind);
            light.range = 3.5f;
            light.intensity = 1.6f;

            var pu = go.AddComponent<PowerUp>();
            pu.kind = kind;
            FloatingText.Show(position + Vector3.up * 0.8f, Label(kind), ColorFor(kind), 1.4f);
            return pu;
        }

        void Start() => Destroy(gameObject, lifetime);

        void Update()
        {
            transform.Rotate(Vector3.up, 90f * Time.deltaTime);
            transform.position += Vector3.up * Mathf.Sin(Time.time * 2f) * 0.002f;

            foreach (var f in FindObjectsOfType<FighterController>())
            {
                if (!f.gameObject.activeInHierarchy) continue;
                if (Vector3.Distance(f.transform.position, transform.position) > pickupRadius) continue;
                Collect(f);
                return;
            }
        }

        void Collect(FighterController f)
        {
            switch (kind)
            {
                case PowerUpKind.Bier:
                    f.StartCoroutine(TempDamage(f, 1.25f, 10f));
                    break;
                case PowerUpKind.Med:
                    f.HealSelf(30f);
                    break;
                case PowerUpKind.Speed:
                    f.StartCoroutine(TempSpeed(f, 1.5f, 8f));
                    break;
                case PowerUpKind.Schild:
                    f.blockReductionBonus = 0.5f;
                    f.StartCoroutine(ResetBlockBonus(f, 8f));
                    break;
                case PowerUpKind.Feuer:
                    VFXManager.Instance?.PlayFire(f.transform.position + Vector3.up, Vector3.up, 20);
                    f.StartCoroutine(TempDamage(f, 1.15f, 10f));
                    break;
                case PowerUpKind.Eis:
                    var foe = f.GetEnemy();
                    if (foe != null)
                    {
                        var slow = foe.GetComponent<SlowDebuff>() ?? foe.gameObject.AddComponent<SlowDebuff>();
                        slow.Apply(5f, 0.6f);
                    }
                    break;
                case PowerUpKind.Muenze:
                    SaveSystem.Instance?.RecordDamage(0f);   // Statistik-Hook
                    break;
                case PowerUpKind.Todeskuss:
                    f.StartCoroutine(TempDamage(f, 2.5f, 5f));
                    ScreenEffects.FlashColor(PennerPalette.BloodRed, 0.4f, 0.3f);
                    break;
            }

            f.GetComponent<CharacterVisuals>()?.BuffFlash(ColorFor(kind), 0.8f);
            FloatingText.Show(f.transform.position + Vector3.up * 2.4f, Label(kind), ColorFor(kind), 1.2f);
            VFXManager.Instance?.PlayHit(transform.position, Vector3.up, 5f, HitTier.Special, f.fighterId);
            Destroy(gameObject);
        }

        static IEnumerator TempDamage(FighterController f, float factor, float seconds)
        {
            f.lightDamage *= factor;
            f.heavyDamage *= factor;
            yield return new WaitForSeconds(seconds);
            if (f == null) yield break;
            f.lightDamage /= factor;
            f.heavyDamage /= factor;
        }

        static IEnumerator TempSpeed(FighterController f, float factor, float seconds)
        {
            f.moveSpeed *= factor;
            yield return new WaitForSeconds(seconds);
            if (f == null) yield break;
            f.moveSpeed /= factor;
        }

        static IEnumerator ResetBlockBonus(FighterController f, float seconds)
        {
            yield return new WaitForSeconds(seconds);
            if (f != null) f.blockReductionBonus = 0f;
        }

        static Color ColorFor(PowerUpKind kind)
        {
            switch (kind)
            {
                case PowerUpKind.Bier:      return PennerPalette.Gold;
                case PowerUpKind.Med:       return PennerPalette.PoisonGrn;
                case PowerUpKind.Speed:     return PennerPalette.NeonBlue;
                case PowerUpKind.Schild:    return PennerPalette.Earth;
                case PowerUpKind.Feuer:     return PennerPalette.WarmOrange;
                case PowerUpKind.Eis:       return new Color(0.6f, 0.9f, 1f);
                case PowerUpKind.Muenze:    return PennerPalette.Gold;
                default:                    return PennerPalette.BloodRed;
            }
        }

        static string Label(PowerUpKind kind)
        {
            switch (kind)
            {
                case PowerUpKind.Bier:      return "BIER +25 %";
                case PowerUpKind.Med:       return "MED +30 HP";
                case PowerUpKind.Speed:     return "SPEED";
                case PowerUpKind.Schild:    return "SCHILD";
                case PowerUpKind.Feuer:     return "FEUER";
                case PowerUpKind.Eis:       return "EIS";
                case PowerUpKind.Muenze:    return "PFAND";
                default:                    return "TODESKUSS";
            }
        }
    }
}
