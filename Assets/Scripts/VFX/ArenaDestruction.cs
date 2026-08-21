using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PennerKombat
{
    /// <summary>
    /// Fortschreitende Arena-Zerstörung in fünf Stufen (docs/EXTRAS.md §1).
    /// Sammelt allen im Kampf ausgeteilten Schaden; bei jedem Schwellwert
    /// verändert sich der Hinterhof sichtbar: Risse, Trümmer, Feuer, Rauch —
    /// bis am Ende nur noch eine brennende Ruine steht.
    /// </summary>
    public class ArenaDestruction : MonoBehaviour
    {
        public static ArenaDestruction Instance { get; private set; }

        public enum Stage { Intact, Damaged, Heavy, NearlyDestroyed, Total }

        [Header("Schwellwerte (kumulierter Schaden)")]
        public float stage1 = 80f;
        public float stage2 = 200f;
        public float stage3 = 350f;
        public float stage4 = 550f;

        [Header("Wirkung")]
        [Tooltip("Licht wird mit jeder Stufe düsterer und roter.")]
        public bool darkenLighting = true;
        public int debrisPerStage = 8;

        public Stage Current { get; private set; } = Stage.Intact;
        public float TotalDamage { get; private set; }
        public float Progress => Mathf.Clamp01(TotalDamage / stage4);

        private readonly List<GameObject> debris = new List<GameObject>();

        public static ArenaDestruction Ensure()
        {
            if (Instance == null)
            {
                var go = new GameObject("~ArenaDestruction");
                Instance = go.AddComponent<ArenaDestruction>();
            }
            return Instance;
        }

        void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(this); return; }
        }

        /// <summary>Wird vom Kampfsystem bei jedem Treffer gerufen.</summary>
        public void RegisterDamage(Vector3 position, float amount)
        {
            TotalDamage += amount;

            Stage next = StageFor(TotalDamage);
            if (next == Current) return;

            Current = next;
            StartCoroutine(ApplyStage(next, position));
        }

        Stage StageFor(float damage)
        {
            if (damage >= stage4) return Stage.Total;
            if (damage >= stage3) return Stage.NearlyDestroyed;
            if (damage >= stage2) return Stage.Heavy;
            if (damage >= stage1) return Stage.Damaged;
            return Stage.Intact;
        }

        IEnumerator ApplyStage(Stage stage, Vector3 epicentre)
        {
            FloatingText.Show(epicentre + Vector3.up * 3f, StageName(stage), PennerPalette.WarmOrange, 1.4f);
            CameraShake.Shake(6f + (int)stage * 2f, 0.2f);
            ArenaVisuals.Instance?.PanicNeon(1.5f);

            var props = FindObjectsOfType<ArenaProp>();

            switch (stage)
            {
                case Stage.Damaged:
                    foreach (var p in props)
                        if (Random.value < 0.3f) p.Tilt();
                    SpawnDebris(debrisPerStage);
                    break;

                case Stage.Heavy:
                    foreach (var p in props)
                        if (Random.value < 0.6f) p.Tilt();
                    SpawnDebris(debrisPerStage * 2);
                    Dust(6);
                    break;

                case Stage.NearlyDestroyed:
                    foreach (var p in props)
                    {
                        if (p.kind == ArenaProp.PropKind.GasBottle) p.Explode(null);
                        else p.Tilt();
                        yield return null;
                    }
                    SpawnDebris(debrisPerStage * 3);
                    Fires(4);
                    break;

                case Stage.Total:
                    ArenaVisuals.Instance?.TrashTheYard();
                    SpawnDebris(debrisPerStage * 4);
                    Fires(8);
                    Dust(12);
                    ScreenEffects.FlashColor(PennerPalette.WarmOrange, 0.5f, 0.4f);
                    break;
            }

            if (darkenLighting) ApplyLighting(stage);
            yield return null;
        }

        void ApplyLighting(Stage stage)
        {
            float t = (int)stage / 4f;
            RenderSettings.fogDensity = Mathf.Lerp(0.018f, 0.05f, t);
            RenderSettings.fogColor = Color.Lerp(PennerPalette.NightBlue,
                                                 PennerPalette.BloodRed * 0.4f, t);
            RenderSettings.ambientSkyColor = Color.Lerp(PennerPalette.NightBlue * 1.4f,
                                                        PennerPalette.WarmOrange * 0.5f, t);
        }

        void SpawnDebris(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = "PK_Debris";
                go.transform.SetParent(transform, false);
                go.transform.position = Extensions.RandomHorizontalPoint(Vector3.zero, 9f) + Vector3.up * 0.2f;
                go.transform.rotation = Random.rotation;
                go.transform.localScale = Vector3.one * Random.Range(0.15f, 0.5f);

                var mr = go.GetComponent<MeshRenderer>();
                mr.material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                mr.material.color = Color.Lerp(PennerPalette.Earth, PennerPalette.NightBlue, Random.value);

                var rb = go.AddComponent<Rigidbody>();
                rb.mass = 2f;
                rb.AddForce(Random.insideUnitSphere * 3f, ForceMode.Impulse);

                debris.Add(go);
            }
        }

        void Dust(int count)
        {
            for (int i = 0; i < count; i++)
                VFXManager.Instance?.PlayDust(Extensions.RandomHorizontalPoint(Vector3.zero, 8f), 1.6f);
        }

        void Fires(int count)
        {
            for (int i = 0; i < count; i++)
            {
                Vector3 p = Extensions.RandomHorizontalPoint(Vector3.zero, 8f) + Vector3.up * 0.3f;
                VFXManager.Instance?.PlayFire(p, Vector3.up, 22);

                var light = new GameObject("PK_Feuer").AddComponent<Light>();
                light.transform.SetParent(transform, false);
                light.transform.position = p;
                light.type = LightType.Point;
                light.color = PennerPalette.WarmOrange;
                light.range = 6f;
                light.intensity = 2.2f;
            }
        }

        static string StageName(Stage stage)
        {
            switch (stage)
            {
                case Stage.Damaged:          return "HOF BESCHÄDIGT";
                case Stage.Heavy:            return "ALLES KAPUTT";
                case Stage.NearlyDestroyed:  return "ES BRENNT";
                case Stage.Total:            return "NUR NOCH SCHUTT";
                default:                     return "";
            }
        }

        /// <summary>Neue Runde: Trümmer weg, Licht zurück.</summary>
        public void ResetArena()
        {
            foreach (var d in debris) if (d != null) Destroy(d);
            debris.Clear();

            foreach (Transform child in transform)
                if (child.name == "PK_Feuer") Destroy(child.gameObject);

            TotalDamage = 0f;
            Current = Stage.Intact;
            ApplyLighting(Stage.Intact);
        }
    }
}
