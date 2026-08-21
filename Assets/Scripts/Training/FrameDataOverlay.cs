using System.Text;
using UnityEngine;

namespace PennerKombat
{
    /// <summary>
    /// Frame-Daten-Overlay (Taste F4). Zeigt pro Kämpfer Startup/Active/Recovery
    /// des laufenden Angriffs in Frames (60 fps Referenz), Block-/Hit-Stun,
    /// Combo-Timer und Abstand. Zeichnet zusätzlich die Angriffs-Hitbox als
    /// Gizmo in der Szenenansicht.
    ///
    /// Braucht keinerlei Assets — reines IMGUI. docs/SPIELEN.md §Training
    /// </summary>
    public class FrameDataOverlay : MonoBehaviour
    {
        public static FrameDataOverlay Instance { get; private set; }

        [Tooltip("Overlay sichtbar?")]
        public bool visible;
        [Tooltip("Hitboxen als Gizmo zeichnen (nur Szenenansicht).")]
        public bool drawHitboxes = true;

        const float FrameSeconds = 1f / 60f;

        readonly StringBuilder sb = new StringBuilder();
        GUIStyle style;

        public static FrameDataOverlay Ensure()
        {
            if (Instance == null)
            {
                var go = new GameObject("~FrameDataOverlay");
                Instance = go.AddComponent<FrameDataOverlay>();
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
#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.f4Key.wasPressedThisFrame) visible = !visible;
#endif
        }

        void OnGUI()
        {
            if (!visible) return;

            if (style == null)
            {
                style = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 14,
                    richText = false,
                    alignment = TextAnchor.UpperLeft
                };
                style.normal.textColor = Color.white;
            }

            var fighters = FindObjectsOfType<FighterController>();
            sb.Length = 0;
            sb.AppendLine($"FRAME-DATEN  ·  {(1f / Mathf.Max(Time.smoothDeltaTime, 0.0001f)):F0} fps  ·  F4 = aus");
            sb.AppendLine(new string('-', 54));

            foreach (var f in fighters)
            {
                if (f == null) continue;
                var enemy = f.GetEnemy();
                float dist = enemy != null ? Vector3.Distance(f.transform.position, enemy.transform.position) : 0f;

                sb.AppendLine($"[P{f.playerIndex + 1}] {f.displayName}  ({f.fighterId})");
                sb.AppendLine($"   HP {f.currentHP:F0}/{f.maxHP:F0}   Combo {f.comboCount}   Abstand {dist:F2} m");
                sb.AppendLine($"   Leicht: Startup {Frames(0.12f)}f · Active {Frames(0.06f)}f · Recovery {Frames(f.lightCooldown - 0.18f)}f");
                sb.AppendLine($"   Schwer: Startup {Frames(0.2f)}f · Active {Frames(0.08f)}f · Recovery {Frames(f.heavyCooldown - 0.28f)}f");
                sb.AppendLine($"   Block {(f.isBlocking ? "AN " : "aus")}   Rolle {(f.IsRolling ? "AN " : "aus")}   "
                            + $"i-Frames {(f.IsInvulnerable ? "JA" : "nein")}");
                sb.AppendLine($"   Fatal Blow {f.fatalBlowMeter:F0}/{GameConstants.FatalBlowMeterMax:F0}"
                            + $"{(f.fatalBlowReady ? "  BEREIT" : "")}");
                sb.AppendLine();
            }

            sb.AppendLine($"Combo-Fenster: {GameConstants.ComboWindow:F2} s  ·  "
                        + $"Blockreduktion: {(1f - GameConstants.BlockDamageReduction) * 100f:F0} %");

            var rect = new Rect(14f, 14f, 560f, 26f + fighters.Length * 116f);
            GUI.Box(rect, GUIContent.none);
            GUI.Label(new Rect(rect.x + 10f, rect.y + 8f, rect.width - 20f, rect.height - 16f), sb.ToString(), style);
        }

        static int Frames(float seconds) => Mathf.Max(1, Mathf.RoundToInt(seconds / FrameSeconds));

        void OnDrawGizmos()
        {
            if (!drawHitboxes) return;
            foreach (var f in FindObjectsOfType<FighterController>())
            {
                if (f == null || f.attackPoint == null) continue;
                Gizmos.color = new Color(1f, 0.2f, 0.1f, 0.55f);
                Gizmos.matrix = Matrix4x4.TRS(f.attackPoint.position, f.attackPoint.rotation, Vector3.one);
                Gizmos.DrawWireCube(Vector3.zero, f.attackBoxSize);
                Gizmos.matrix = Matrix4x4.identity;
                Gizmos.color = new Color(0f, 0.75f, 1f, 0.35f);
                Gizmos.DrawWireSphere(f.attackPoint.position, f.attackRange);
            }
        }
    }
}
