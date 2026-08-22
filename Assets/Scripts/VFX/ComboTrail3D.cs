using System.Collections.Generic;
using UnityEngine;

namespace PennerKombat
{
    /// <summary>
    /// (4) Räumlicher Combo-Trail — docs/3D.md §7.1.
    /// Zeichnet die Bewegungsspur des Kämpfers als LineRenderer im Weltraum;
    /// Farbe und Breite wachsen mit der Combo-Stufe. Aktiviert sich selbst,
    /// sobald der <see cref="ComboSystem"/> eine Combo ≥ Schwelle meldet.
    /// </summary>
    [RequireComponent(typeof(FighterController))]
    public class ComboTrail3D : MonoBehaviour
    {
        [Header("Trail")]
        public float pointInterval = 0.03f;
        public int maxPoints = 28;
        public float baseWidth = 0.18f;
        public int activateAtCombo = 3;
        public float fadeOutAfter = 0.6f;

        private FighterController fighter;
        private LineRenderer line;
        private readonly List<Vector3> points = new List<Vector3>();
        private float nextPoint;
        private float lastComboTime;
        private int combo;

        void Awake()
        {
            fighter = GetComponent<FighterController>();

            var go = new GameObject("PK_ComboTrail");
            go.transform.SetParent(transform, false);
            line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 0;
            line.numCapVertices = 4;
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
        }

        void OnEnable()
        {
            if (ComboSystem.Instance != null) ComboSystem.Instance.OnComboChanged += HandleCombo;
        }

        void OnDisable()
        {
            if (ComboSystem.Instance != null) ComboSystem.Instance.OnComboChanged -= HandleCombo;
            Clear();
        }

        void HandleCombo(FighterController who, int value)
        {
            if (who != fighter) return;
            combo = value;
            lastComboTime = Time.time;
            if (combo < activateAtCombo) Clear();
        }

        void LateUpdate()
        {
            bool active = combo >= activateAtCombo && Time.time - lastComboTime < fadeOutAfter;

            if (!active)
            {
                if (points.Count > 0) Shrink();
                return;
            }

            if (Time.time >= nextPoint)
            {
                nextPoint = Time.time + pointInterval;
                points.Add(transform.position + Vector3.up * 1.0f);
                if (points.Count > maxPoints) points.RemoveAt(0);
            }

            Paint();
        }

        void Paint()
        {
            Color c = PennerPalette.ForCombo(combo);
            float width = baseWidth * (1f + Mathf.Clamp01(combo / 20f));

            line.positionCount = points.Count;
            for (int i = 0; i < points.Count; i++) line.SetPosition(i, points[i]);

            line.startWidth = width * 0.25f;   // hinten dünn
            line.endWidth = width;             // vorne breit
            line.startColor = c.WithAlpha(0f);
            line.endColor = c.WithAlpha(0.75f);
        }

        void Shrink()
        {
            points.RemoveAt(0);
            if (points.Count < 2) { Clear(); return; }
            Paint();
        }

        void Clear()
        {
            points.Clear();
            if (line != null) line.positionCount = 0;
        }
    }
}
