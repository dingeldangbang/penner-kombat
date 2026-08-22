using System;
using System.IO;
using UnityEngine;

namespace PennerKombat
{
    /// <summary>
    /// Persistente Statistik- und Fortschrittsspeicherung als JSON unter
    /// <c>Application.persistentDataPath/save.json</c>.
    ///
    /// Trophäen laufen weiterhin über den <see cref="TrophyManager"/>
    /// (PlayerPrefs) — hier liegen Match-Statistiken, Rekorde und Optionen,
    /// die zwischen Sessions überleben sollen.
    /// </summary>
    public class SaveSystem : MonoBehaviour
    {
        public static SaveSystem Instance { get; private set; }

        [Serializable]
        public class SaveData
        {
            public int saveVersion = 1;

            // Match-Statistik
            public int totalMatches;
            public int totalWins;
            public int totalLosses;
            public int totalRounds;
            public float totalPlayTime;

            // Rekorde
            public int comboRecord;
            public float highestDamageDealt;
            public int fatalityCount;
            public int fatalBlowCount;

            // Zuletzt gespielt
            public string lastPlayedCharacter = GameConstants.CharLeBinde;
            public string lastOpponent = "";

            // Optionen (siehe OptionsMenu)
            public float masterVolume = 0.8f;
            public float musicVolume = 0.7f;
            public float sfxVolume = 0.8f;
            public float vfxIntensity = 1f;
            public bool gore = true;
            public bool fullscreen = true;
            public int qualityLevel = 2;
            public bool vsync = true;
        }

        public bool autoSaveOnQuit = true;

        private SaveData data = new SaveData();
        private string path;
        private float sessionStart;

        public static SaveSystem Ensure()
        {
            if (Instance == null)
            {
                var go = new GameObject("~SaveSystem");
                Instance = go.AddComponent<SaveSystem>();
            }
            return Instance;
        }

        void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
            DontDestroyOnLoad(gameObject);

            path = Path.Combine(Application.persistentDataPath, "save.json");
            sessionStart = Time.realtimeSinceStartup;
            Load();
        }

        void OnApplicationQuit()
        {
            if (!autoSaveOnQuit) return;
            data.totalPlayTime += Time.realtimeSinceStartup - sessionStart;
            Save();
        }

        void OnApplicationPause(bool paused)
        {
            if (paused) Save();     // Android: App im Hintergrund
        }

        public SaveData Data => data;

        // ------------------------------------------------------------------
        //  I/O
        // ------------------------------------------------------------------

        public void Save()
        {
            try
            {
                File.WriteAllText(path, JsonUtility.ToJson(data, true));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveSystem] Speichern fehlgeschlagen: {e.Message}");
            }
        }

        public void Load()
        {
            try
            {
                if (File.Exists(path))
                {
                    var json = File.ReadAllText(path);
                    var loaded = JsonUtility.FromJson<SaveData>(json);
                    if (loaded != null) data = loaded;
                }
                else Save();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveSystem] Laden fehlgeschlagen, starte frisch: {e.Message}");
                data = new SaveData();
            }
        }

        /// <summary>Löscht Statistiken (Optionen bleiben erhalten).</summary>
        public void ResetStats()
        {
            var opts = data;
            data = new SaveData
            {
                masterVolume = opts.masterVolume,
                musicVolume = opts.musicVolume,
                sfxVolume = opts.sfxVolume,
                vfxIntensity = opts.vfxIntensity,
                gore = opts.gore,
                fullscreen = opts.fullscreen,
                qualityLevel = opts.qualityLevel,
                vsync = opts.vsync
            };
            Save();
        }

        // ------------------------------------------------------------------
        //  Statistik-API (wird vom GameManager / Kampfsystem gerufen)
        // ------------------------------------------------------------------

        public void RecordMatch(bool won, string character, string opponent)
        {
            data.totalMatches++;
            if (won) data.totalWins++; else data.totalLosses++;
            data.lastPlayedCharacter = character ?? data.lastPlayedCharacter;
            data.lastOpponent = opponent ?? data.lastOpponent;
            Save();
        }

        public void RecordRound() => data.totalRounds++;

        public void RecordCombo(int combo)
        {
            if (combo <= data.comboRecord) return;
            data.comboRecord = combo;
            Save();
        }

        public void RecordDamage(float damage)
        {
            if (damage <= data.highestDamageDealt) return;
            data.highestDamageDealt = damage;
            Save();
        }

        public void RecordFatality()
        {
            data.fatalityCount++;
            Save();
        }

        public void RecordFatalBlow()
        {
            data.fatalBlowCount++;
            Save();
        }

        /// <summary>Wendet die gespeicherten Optionen auf Audio, Qualität und VFX an.</summary>
        public void ApplyOptions()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetMasterVolume(data.masterVolume);
                AudioManager.Instance.SetMusicVolume(data.musicVolume);
                AudioManager.Instance.SetSFXVolume(data.sfxVolume);
            }
            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.intensity = data.vfxIntensity;
                VFXManager.Instance.gore = data.gore;
            }
            QualitySettings.SetQualityLevel(Mathf.Clamp(data.qualityLevel, 0, QualitySettings.names.Length - 1), true);
            QualitySettings.vSyncCount = data.vsync ? 1 : 0;
#if !UNITY_ANDROID && !UNITY_IOS
            Screen.fullScreen = data.fullscreen;
#endif
        }
    }
}
