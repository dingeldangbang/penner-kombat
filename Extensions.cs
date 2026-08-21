using UnityEngine;

namespace PennerKombat
{
    /// <summary>
    /// Nützliche Erweiterungsmethoden für das gesamte Projekt.
    /// </summary>
    public static class Extensions
    {
        /// <summary>Konvertiert eine Richtung in einen horizontalen Winkel (0..360).</summary>
        public static float ToAngle(this Vector3 dir)
        {
            Vector3 flat = new Vector3(dir.x, 0f, dir.z);
            if (flat.sqrMagnitude < 0.0001f) return 0f;
            float angle = Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg;
            return angle < 0f ? angle + 360f : angle;
        }

        /// <summary>Erzwingt, dass ein Wert in [min,max] liegt.</summary>
        public static float Clamp(this float value, float min, float max)
            => Mathf.Clamp(value, min, max);

        /// <summary>Lerp mit Ease-Out-Cubic für sanftes Abbremsen.</summary>
        public static float EaseOutCubic(this float t)
        {
            float p = 1f - t;
            return 1f - p * p * p;
        }

        /// <summary>Wandelt einen String in einen Spieler-Index um (für Netzwerk/Namen).</summary>
        public static bool TryParsePlayerIndex(this string s, out int index)
            => int.TryParse(s, out index);

        /// <summary>Setzt eine Layer-Maske auf Basis mehrerer Layer-Namen.</summary>
        public static LayerMask CreateLayerMask(params string[] layerNames)
        {
            LayerMask mask = 0;
            foreach (var name in layerNames)
            {
                int idx = LayerMask.NameToLayer(name);
                if (idx >= 0) mask |= 1 << idx;
            }
            return mask;
        }

        /// <summary>Zufälliger Punkt in einer horizontalen Kreisfläche.</summary>
        public static Vector3 RandomHorizontalPoint(Vector3 center, float radius)
        {
            Vector2 rand = Random.insideUnitCircle * radius;
            return new Vector3(center.x + rand.x, center.y, center.z + rand.y);
        }
    }
}
