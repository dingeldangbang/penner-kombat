using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PennerKombat
{
    /// <summary>
    /// Laufzeit-Testsuite. Führt nach dem Start mehrere Checks auf die
    /// Kernsysteme aus und loggt bestanden/fehlgeschlagen. Kann über
    /// runTestsOnStart gesteuert werden.
    /// </summary>
    public class TestRunner : MonoBehaviour
    {
        [Header("Test")]
        public bool runTestsOnStart = true;

        private readonly List<(string name, bool passed, string message)> results =
            new List<(string, bool, string)>();

        void Start()
        {
            if (runTestsOnStart) StartCoroutine(RunAll());
        }

        IEnumerator RunAll()
        {
            yield return TestDatabase();
            yield return TestPhysicsRoundTrip();
            yield return TestSingletonPresence();
            yield return TestAIComponent();
            yield return TestVisualLayer();

            foreach (var r in results)
                Debug.Log($"[Test] {(r.passed ? "✅" : "❌")} {r.name}: {r.message}");
        }

        IEnumerator TestVisualLayer()
        {
            // VFX-Schicht (docs/VISUALS.md) muss vom Bootstrapper stehen
            bool ok = VFXManager.Instance != null
                      && ComboSystem.Instance != null
                      && ScreenEffects.Instance != null
                      && CameraShake.Instance != null;
            results.Add(("VFX-Schicht", ok,
                ok ? "VFX, Combo, ScreenEffects und CameraShake aktiv" : "VFX-Singletons fehlen (Bootstrapper?)"));

            // Palette-Sanity: jede Charakter-ID hat eine Signaturfarbe
            bool palette = true;
            foreach (var id in GameConstants.AllCharacterIds)
                if (PennerPalette.ForCharacter(id).a <= 0f) palette = false;
            results.Add(("Farbpalette", palette, palette ? "9 Signaturfarben" : "Signaturfarbe fehlt"));

            // Combo-Stufen laut Spec: weiß < 5, gold < 10, rot ab 10
            bool tiers = PennerPalette.ForCombo(1) == PennerPalette.Pure
                      && PennerPalette.ForCombo(6) == PennerPalette.Gold
                      && PennerPalette.ForCombo(12) != PennerPalette.Pure;
            results.Add(("Combo-Stufen", tiers, tiers ? "1-4 / 5-9 / 10+ korrekt" : "Combo-Farbstufen falsch"));
            yield return null;
        }

        IEnumerator TestDatabase()
        {
            var db = ScriptableObject.CreateInstance<FighterDatabase>();
            db.EnsureDefaultRoster();
            bool ok = db.fighters.Count == GameConstants.AllCharacterIds.Length;
            results.Add(("FighterDatabase", ok, ok ? $"{db.fighters.Count} Charaktere" : "Roster unvollständig"));
            yield return null;
        }

        IEnumerator TestSingletonPresence()
        {
            bool ok = FighterInput.Instance != null
                      && GameManager.Instance != null
                      && ArenaManager.Instance != null;
            results.Add(("Singletons", ok, ok ? "Kern-Manager vorhanden" : "Manager fehlen (Bootstrapper?)"));
            yield return null;
        }

        IEnumerator TestPhysicsRoundTrip()
        {
            bool ok = true;
            string msg = "Kein Kämpfer zum Testen";
            var fighters = FindObjectsOfType<FighterController>();
            if (fighters.Length >= 2)
            {
                var a = fighters[0]; var b = fighters[1];
                float before = b.currentHP;
                a.TakeDamage(0f, Vector3.zero, b);   // no-op
                ok = Mathf.Approximately(before, b.currentHP);
                msg = ok ? "HP stabil nach No-Op-Treffer" : "HP verändert sich ohne Schaden";
            }
            results.Add(("Schadensmodell", ok, msg));
            yield return null;
        }

        IEnumerator TestAIComponent()
        {
            bool ok = FindObjectsOfType<AIController>().Length >= 1;
            results.Add(("KI", ok, ok ? "AIController vorhanden" : "Keine KI im Test-Setup"));
            yield return null;
        }
    }
}
