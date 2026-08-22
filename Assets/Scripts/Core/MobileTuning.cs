using UnityEngine;

namespace PennerKombat
{
    /// <summary>
    /// Passt das Spiel zur Laufzeit an Handhelds an: Bildrate, Schatten,
    /// Effektmenge und Bildschirm-Timeout. Auf schwachen Geräten werden die
    /// teuersten Extras (Zuschauer, Verbündete, Post-Processing) abgeschaltet,
    /// damit der Kampf flüssig bleibt.
    ///
    /// Wird vom <see cref="Bootstrapper"/> automatisch erzeugt.
    /// Siehe docs/HANDY.md
    /// </summary>
    public class MobileTuning : MonoBehaviour
    {
        public static MobileTuning Instance { get; private set; }

        /// <summary>Grobe Einstufung des Geräts.</summary>
        public enum Tier { Schwach, Mittel, Stark, Desktop }

        public Tier DeviceTier { get; private set; } = Tier.Desktop;

        public static MobileTuning Ensure()
        {
            if (Instance == null)
            {
                var go = new GameObject("~MobileTuning");
                Instance = go.AddComponent<MobileTuning>();
            }
            return Instance;
        }

        void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }

            DeviceTier = Classify();
            Apply();
        }

        static Tier Classify()
        {
            if (!Application.isMobilePlatform) return Tier.Desktop;

            int ram = SystemInfo.systemMemorySize;         // MB
            int cores = SystemInfo.processorCount;

            if (ram >= 6000 && cores >= 8) return Tier.Stark;
            if (ram >= 3500 && cores >= 6) return Tier.Mittel;
            return Tier.Schwach;
        }

        void Apply()
        {
            // Bildschirm darf während des Kampfes nicht ausgehen
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            if (DeviceTier == Tier.Desktop)
            {
                Application.targetFrameRate = -1;
                return;
            }

            QualitySettings.vSyncCount = 0;

            switch (DeviceTier)
            {
                case Tier.Stark:
                    Application.targetFrameRate = 60;
                    QualitySettings.shadowDistance = 30f;
                    break;

                case Tier.Mittel:
                    Application.targetFrameRate = 60;
                    QualitySettings.shadowDistance = 18f;
                    QualitySettings.shadows = ShadowQuality.HardOnly;
                    DisableExtras(crowd: true, allies: false);
                    break;

                case Tier.Schwach:
                    Application.targetFrameRate = 30;
                    QualitySettings.shadows = ShadowQuality.Disable;
                    QualitySettings.shadowDistance = 0f;
                    QualitySettings.skinWeights = SkinWeights.TwoBones;
                    Screen.SetResolution(Mathf.RoundToInt(Screen.width * 0.75f),
                                         Mathf.RoundToInt(Screen.height * 0.75f), true);
                    DisableExtras(crowd: true, allies: true);
                    break;
            }

            Debug.Log($"[Penner Kombat] Geräteklasse: {DeviceTier} "
                    + $"({SystemInfo.systemMemorySize} MB RAM, {SystemInfo.processorCount} Kerne) — "
                    + $"Ziel: {Application.targetFrameRate} fps");
        }

        /// <summary>Die teuersten Nebenschauplätze abschalten.</summary>
        void DisableExtras(bool crowd, bool allies)
        {
            if (crowd && CrowdReactions.Instance != null)
                CrowdReactions.Instance.gameObject.SetActive(false);
            if (allies && AllySummon.Instance != null)
                AllySummon.Instance.gameObject.SetActive(false);
        }
    }
}
