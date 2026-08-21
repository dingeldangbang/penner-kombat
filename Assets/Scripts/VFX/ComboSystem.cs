using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PennerKombat
{
    /// <summary>
    /// Combo-Feedback-System: reagiert auf jeden gelandeten Treffer und fährt
    /// die Eskalationsstufen aus der Spec (docs/VISUALS.md 4.2/4.3):
    /// Zählerfarbe/-größe, Sound-Pings (3/5/8/10+), Screen-Shake, Zoom,
    /// Vignette und Funken.
    /// </summary>
    [DefaultExecutionOrder(300)]
    public class ComboSystem : MonoBehaviour
    {
        public static ComboSystem Instance { get; private set; }

        [Header("Timing")]
        public float comboWindow = GameConstants.ComboWindow;

        [Header("Sounds (optional)")]
        public AudioClip ping3;
        public AudioClip ping5;
        public AudioClip ping8;
        public AudioClip komboVoice;   // „KOMBO!" ab 10

        public event System.Action<FighterController, int> OnComboChanged;

        private readonly Dictionary<FighterController, int> combos = new Dictionary<FighterController, int>();
        private readonly Dictionary<FighterController, float> lastHit = new Dictionary<FighterController, float>();

        public static ComboSystem Ensure()
        {
            if (Instance == null)
            {
                var go = new GameObject("~ComboSystem");
                Instance = go.AddComponent<ComboSystem>();
            }
            return Instance;
        }

        void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(this); return; }
        }

        /// <summary>Wird vom FighterController bei jedem gelandeten Treffer gerufen.</summary>
        public void RegisterHit(FighterController attacker, FighterController target, float damage, HitTier tier)
        {
            if (attacker == null) return;

            int combo = 1;
            if (lastHit.TryGetValue(attacker, out float t) && Time.time - t < comboWindow)
                combos.TryGetValue(attacker, out combo);
            else
                combo = 0;
            combo++;

            combos[attacker] = combo;
            lastHit[attacker] = Time.time;

            ApplyFeedback(attacker, combo, tier);
            OnComboChanged?.Invoke(attacker, combo);
        }

        public int GetCombo(FighterController f)
            => f != null && combos.TryGetValue(f, out int c) ? c : 0;

        public void ResetAll()
        {
            combos.Clear();
            lastHit.Clear();
            CameraShake.SetComboZoom(0);
            ScreenEffects.SetComboVignette(0);
            if (ComboCounterUI.Instance != null) ComboCounterUI.Instance.Clear();
        }

        void Update()
        {
            // abgelaufene Combos verfallen lassen
            if (lastHit.Count == 0) return;
            var expired = new List<FighterController>();
            foreach (var kv in lastHit)
                if (Time.time - kv.Value > comboWindow) expired.Add(kv.Key);

            foreach (var f in expired)
            {
                lastHit.Remove(f);
                combos.Remove(f);
                OnComboChanged?.Invoke(f, 0);
                if (ComboCounterUI.Instance != null) ComboCounterUI.Instance.Hide(f);
            }
            if (expired.Count > 0 && combos.Count == 0)
            {
                CameraShake.SetComboZoom(0);
                ScreenEffects.SetComboVignette(0);
            }
        }

        void ApplyFeedback(FighterController attacker, int combo, HitTier tier)
        {
            // --- HUD ---
            if (ComboCounterUI.Instance != null)
                ComboCounterUI.Instance.Show(attacker, combo);

            // --- Kamera & Bildschirm (Spec 4.3) ---
            CameraShake.SetComboZoom(combo);
            ScreenEffects.SetComboVignette(combo);

            Vector3 pos = attacker.transform.position + Vector3.up * 1.6f;
            var vfx = VFXManager.Instance;
#if PK_URP
            UrpPostProcessingDriver.Instance?.SetCombo(combo);
            UrpPostProcessingDriver.Instance?.FocusOn(attacker.transform);
#endif

            if (combo >= 21)
            {
                // Feuerwerk-Stufe (Spec 4.3) + räumliche Druckwelle
                CameraShake.Shake(12f, 0.18f);
                vfx?.PlayFireworks(pos, 5);
                ComboExplosion3D.Ensure().SpawnForCombo(pos, combo);
            }
            else if (combo >= 16)
            {
                CameraShake.Shake(12f, 0.15f);
                if (vfx != null) { vfx.PlayHit(pos, attacker.transform.forward, 10f, HitTier.Ex, attacker.fighterId); }
            }
            else if (combo >= 11)
            {
                CameraShake.Shake(8f, 0.12f);
                ComboExplosion3D.Ensure().SpawnForCombo(pos, combo);
            }
            else if (combo >= 8)
            {
                CameraShake.Shake(5f, 0.08f);
            }
            else if (combo >= 5)
            {
                CameraShake.Shake(2f, 0.05f);
            }

            // --- Rotverschiebung ab 5 (Spec-Tabelle) ---
            if (combo >= 5)
            {
                float amount = combo >= 21 ? 0.28f : combo >= 16 ? 0.22f : combo >= 11 ? 0.16f : combo >= 8 ? 0.10f : 0.05f;
                ScreenEffects.FlashColor(PennerPalette.BloodRed, amount, 0.12f);
            }

            // --- Sound-Feedback (3 / 5 / 8 / 10+) ---
            AudioClip clip = combo >= 10 ? komboVoice
                           : combo == 8 ? ping8
                           : combo == 5 ? ping5
                           : combo == 3 ? ping3
                           : null;
            if (clip != null) AudioSource.PlayClipAtPoint(clip, pos);
            else if (combo == 3 || combo == 5 || combo == 8 || combo >= 10)
                ProceduralPing(combo);
        }

        /// <summary>Fallback-„Pling" ohne Audio-Assets: kurzer Sinus, Tonhöhe steigt mit der Combo.</summary>
        void ProceduralPing(int combo)
        {
            float freq = combo >= 10 ? 1320f : combo >= 8 ? 1046f : combo >= 5 ? 880f : 660f;
            const int rate = 44100;
            int samples = rate / 12;
            var clip = AudioClip.Create($"pk_ping_{combo}", samples, 1, rate, false);
            var data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / rate;
                float env = Mathf.Exp(-18f * t);
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.35f;
            }
            clip.SetData(data, 0);
            var go = new GameObject("~ping");
            var src = go.AddComponent<AudioSource>();
            src.clip = clip;
            src.Play();
            Destroy(go, 0.4f);
        }
    }
}
