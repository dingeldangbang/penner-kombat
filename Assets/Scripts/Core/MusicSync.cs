using System;
using UnityEngine;

namespace PennerKombat
{
    /// <summary>
    /// Beat-Synchronisation für den Soundtrack (docs/SOUNDTRACK.md §Dynamik).
    /// Zählt die Beats des laufenden Tracks, feuert Events auf Beat, Halbtakt
    /// und Takt und zieht das Tempo bei hohen Combos an — düstere 808-Beats,
    /// die mit dem Kampf mitgehen.
    /// </summary>
    public class MusicSync : MonoBehaviour
    {
        public static MusicSync Instance { get; private set; }

        [Header("Tempo")]
        public float baseBpm = 140f;
        public float maxBpm = 180f;
        [Tooltip("BPM-Zuwachs pro Combo-Treffer.")]
        public float bpmPerCombo = 0.6f;
        [Tooltip("Pitch folgt dem Tempo (808-Bass wird härter).")]
        public bool pitchFollowsTempo = true;

        [Header("Ducking")]
        public float duckVolume = 0.25f;
        public float duckFade = 0.25f;

        public event Action<int> OnBeat;       // jeder Beat
        public event Action<int> OnHalfBar;    // jeder 2. Beat
        public event Action<int> OnBar;        // jeder 4. Beat

        public float CurrentBpm { get; private set; }
        public int BeatCount { get; private set; }
        public float BeatInterval => 60f / Mathf.Max(1f, CurrentBpm);

        private AudioSource music;
        private float nextBeatTime;
        private float baseVolume = 0.7f;
        private float duckTimer;

        public static MusicSync Ensure()
        {
            if (Instance == null)
            {
                var go = new GameObject("~MusicSync");
                Instance = go.AddComponent<MusicSync>();
            }
            return Instance;
        }

        void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(this); return; }
            CurrentBpm = baseBpm;
        }

        void Start()
        {
            music = FindMusicSource();
            if (music != null) baseVolume = music.volume;
            nextBeatTime = Time.time;

            if (ComboSystem.Instance != null)
                ComboSystem.Instance.OnComboChanged += HandleCombo;
        }

        void OnDestroy()
        {
            if (ComboSystem.Instance != null)
                ComboSystem.Instance.OnComboChanged -= HandleCombo;
        }

        AudioSource FindMusicSource()
        {
            var am = AudioManager.Instance;
            if (am == null) return null;
            // Die erste geloopte Quelle am AudioManager ist die Musik
            foreach (var src in am.GetComponents<AudioSource>())
                if (src.loop) return src;
            return null;
        }

        void Update()
        {
            if (music == null) music = FindMusicSource();

            // --- Beat-Takt ---
            if (Time.time >= nextBeatTime)
            {
                nextBeatTime += BeatInterval;
                BeatCount++;

                OnBeat?.Invoke(BeatCount);
                if (BeatCount % 2 == 0) OnHalfBar?.Invoke(BeatCount);
                if (BeatCount % 4 == 0) OnBar?.Invoke(BeatCount);
            }

            // --- Ducking auslaufen lassen ---
            if (duckTimer > 0f)
            {
                duckTimer -= Time.unscaledDeltaTime;
                if (duckTimer <= 0f && music != null)
                    music.volume = baseVolume;
            }
        }

        /// <summary>Combo treibt das Tempo — je länger die Kette, desto härter der Beat.</summary>
        void HandleCombo(FighterController fighter, int combo)
        {
            SetTempo(baseBpm + combo * bpmPerCombo);
        }

        public void SetTempo(float bpm)
        {
            CurrentBpm = Mathf.Clamp(bpm, baseBpm * 0.5f, maxBpm);
            if (pitchFollowsTempo && music != null)
                music.pitch = Mathf.Clamp(CurrentBpm / baseBpm, 0.8f, 1.35f);
        }

        public void ResetTempo()
        {
            CurrentBpm = baseBpm;
            if (music != null) music.pitch = 1f;
        }

        /// <summary>Musik kurz absenken (Fatality, X-Ray, Cinematic).</summary>
        public void Duck(float seconds)
        {
            if (music == null) return;
            music.volume = baseVolume * duckVolume;
            duckTimer = Mathf.Max(duckTimer, seconds);
        }

        /// <summary>Track hart stoppen und nach `seconds` wieder einsetzen (Fatality-Cut).</summary>
        public void CutAndResume(float seconds)
        {
            if (music == null) return;
            music.Pause();
            Invoke(nameof(ResumeMusic), seconds);
        }

        void ResumeMusic()
        {
            if (music != null) music.UnPause();
        }
    }
}
