using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PennerKombat.Editor
{
    /// <summary>
    /// „Kann ich jetzt spielen?" — prüft in einem Rutsch alles, was zwischen
    /// dem Projekt und dem ersten Kampf stehen kann, und sagt bei jedem Punkt,
    /// was zu tun ist.
    ///
    /// Menü: Tools → Penner Kombat → ✔ Spielbereitschaft prüfen
    /// </summary>
    public static class PkReadinessCheck
    {
        [MenuItem("Tools/Penner Kombat/✔ Spielbereitschaft prüfen", priority = 3)]
        public static void Run()
        {
            var lines = new List<string>();
            bool blocker = false;

            // 1. Render-Pipeline
            var rp = UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline;
            if (rp == null)
                lines.Add("⚠  Kein Render-Pipeline-Asset (Built-in). Läuft, aber Shader/Post-FX sind auf URP ausgelegt.\n"
                        + "    → Project Settings → Graphics → URP-Asset zuweisen.");
            else
                lines.Add($"✅ Render-Pipeline: {rp.name}");

            // 2. Input System
            bool newInput = System.AppDomain.CurrentDomain.GetAssemblies()
                .Any(a => a.GetName().Name == "Unity.InputSystem");
            if (!newInput)
            {
                blocker = true;
                lines.Add("❌ Input System fehlt — ohne das reagiert keine Taste.\n"
                        + "    → Package Manager → com.unity.inputsystem installieren.");
            }
            else lines.Add("✅ Input System installiert");

#if !ENABLE_INPUT_SYSTEM
            blocker = true;
            lines.Add("❌ Active Input Handling steht nicht auf 'Input System' oder 'Both'.\n"
                    + "    → Project Settings → Player → Active Input Handling = Both, danach Unity neu starten.");
#else
            lines.Add("✅ Active Input Handling erlaubt das neue Input System");
#endif

            // 3. TextMeshPro
            var tmpSettings = Resources.Load("TMP Settings");
            if (tmpSettings == null)
            {
                blocker = true;
                lines.Add("❌ TMP-Grundressourcen fehlen — HUD bleibt leer.\n"
                        + "    → Window → TextMeshPro → Import TMP Essential Resources.");
            }
            else lines.Add("✅ TextMeshPro einsatzbereit");

            // 4. Tags & Layer
            var missingTags = new[] { "Ground", "Fighter", "Interactable", "Projectile" }
                .Where(t => !UnityEditorInternal.InternalEditorUtility.tags.Contains(t)).ToArray();
            if (missingTags.Length > 0)
                lines.Add($"⚠  Tags fehlen: {string.Join(", ", missingTags)}\n"
                        + "    → Tools → Penner Kombat → Projekt einrichten.");
            else lines.Add("✅ Tags vollständig");

            if (LayerMask.NameToLayer("Fighter") < 0)
                lines.Add("⚠  Layer 'Fighter' fehlt — Treffer laufen dann über alle Layer (funktioniert, ist nur unsauber).\n"
                        + "    → Tools → Penner Kombat → Projekt einrichten.");
            else lines.Add("✅ Layer 'Fighter' vorhanden");

            // 5. Datenbank
            var db = Resources.Load<FighterDatabase>("FighterDatabase");
            if (db == null)
                lines.Add("⚠  Keine FighterDatabase unter Assets/Resources — wird zur Laufzeit erzeugt.\n"
                        + "    → Tools → Penner Kombat → Alles einrichten (ohne Play).");
            else
            {
                int withPrefab = db.fighters.Count(f => f != null && f.prefab != null);
                lines.Add($"✅ FighterDatabase: {db.fighters.Count} Charaktere, davon {withPrefab} mit Prefab "
                        + $"({db.fighters.Count - withPrefab} nutzen die Platzhalter-Kapsel)");
            }

            // 6. Szene
            var scenes = AssetDatabase.FindAssets("t:Scene");
            if (scenes.Length == 0)
                lines.Add("⚠  Keine Szene im Projekt.\n"
                        + "    → Tools → Penner Kombat → ▶ Alles einrichten und spielen.");
            else
            {
                var names = scenes.Take(5).Select(g => System.IO.Path.GetFileNameWithoutExtension(
                    AssetDatabase.GUIDToAssetPath(g)));
                lines.Add($"✅ Szenen vorhanden: {string.Join(", ", names)}");
            }

            // 7. Bootstrapper in der offenen Szene
            var boot = Object.FindObjectOfType<Bootstrapper>();
            var gm = Object.FindObjectOfType<GameManager>();
            if (boot == null && gm == null)
                lines.Add("⚠  In der offenen Szene liegt weder Bootstrapper noch GameManager.\n"
                        + "    → Leeres GameObject anlegen und 'Bootstrapper' draufziehen — mehr braucht es nicht.");
            else
                lines.Add($"✅ Szene startklar (Bootstrapper: {(boot != null ? "ja" : "nein")}, "
                        + $"GameManager: {(gm != null ? "ja" : "nein")})");

            // 8. GLB-Kette (optional)
            bool gltfast = System.AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == "glTFast");
            lines.Add(gltfast
                ? "✅ glTFast installiert — GLB-Import steht bereit"
                : "ℹ  glTFast nicht installiert (nur nötig für eigene Modelle).\n"
                + "    → Tools → Penner Kombat → GLB → glTFast installieren.");

            string report = string.Join("\n", lines);
            string verdict = blocker
                ? "\n\n❌ ES FEHLT NOCH ETWAS WESENTLICHES — siehe die roten Punkte oben."
                : "\n\n▶ SPIELBEREIT. Play drücken. P1 = WASD · J · K · Shift · Space.";

            Debug.Log("=== PENNER KOMBAT — SPIELBEREITSCHAFT ===\n" + report + verdict);
            EditorUtility.DisplayDialog("Penner Kombat — Spielbereitschaft",
                report + verdict, "Alles klar");
        }
    }
}
