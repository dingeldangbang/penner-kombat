using System.Collections.Generic;
using UnityEngine;

namespace PennerKombat
{
    /// <summary>
    /// Verwaltet alle 56 Trophäen (IDs 51–56 aus dem Design-Dokument).
    /// Bietet Freischaltung, Fortschritt und Benachrichtigungs-UI.
    /// </summary>
    public class TrophyManager : MonoBehaviour
    {
        public static TrophyManager Instance;

        // Offizielle Trophäen-IDs (51–56)
        public const string TROPHY_GUTEN_APPETIT = "Guten Appetit";            // 51 Silber
        public const string TROPHY_BRAVES_MADCHEN = "Wer ist ein braves Mädchen?"; // 52 Silber
        public const string TROPHY_AUF_DER_KANTE = "Auf der Kante";            // 53 Gold
        public const string TROPHY_EHRLICHER_BETRUG = "Der ehrliche Betrug";   // 54 Gold
        public const string TROPHY_DINGENELDANG = "Dingeneldang!";             // 55 Gold
        public const string TROPHY_STABILE_SEITENLAGE = "Die stabile Seitenlage"; // 56 Platin (geheim)

        [Header("UI")]
        public GameObject trophyNotificationPrefab;
        public Transform notificationParent;
        public AudioClip trophySound;

        private readonly Dictionary<string, TrophyEntry> trophies = new Dictionary<string, TrophyEntry>();

        public event System.Action<string> OnTrophyUnlocked;

        void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(this); return; }
            RegisterDefaults();
            LoadProgress();
        }

        void RegisterDefaults()
        {
            // --- Basis-Trophäen (1–50) ---
            // Story / Arcade (1–12)
            Add("Der erste Schlag", TrophyRarity.Bronze);
            Add("Raus aus dem Leergut", TrophyRarity.Bronze);
            Add("Kanaldeckel-König", TrophyRarity.Bronze);
            Add("Blaue-Eimer-Bewohner", TrophyRarity.Bronze);
            Add("Pankows schnellste Frau", TrophyRarity.Bronze);
            Add("Rattenfreund", TrophyRarity.Bronze);
            Add("Glück ist Ausdauer", TrophyRarity.Bronze);
            Add("Turnierheld", TrophyRarity.Silver);
            Add("Leeres Glück", TrophyRarity.Silver);
            Add("Schichtenende", TrophyRarity.Silver);
            Add("Pfandtiger", TrophyRarity.Silver);
            Add("Leergut Libre", TrophyRarity.Gold);

            // Kampf / Moveset (13–24)
            Add("Trocken gelegt", TrophyRarity.Bronze);
            Add("Kettenreaktion", TrophyRarity.Silver);
            Add("Blockwart", TrophyRarity.Bronze);
            Add("Erste Hilfe", TrophyRarity.Bronze);
            Add("X-Ray-Veteran", TrophyRarity.Silver);
            Add("Röntgenblick", TrophyRarity.Gold);
            Add("Fatality-Fan", TrophyRarity.Silver);
            Add("Brutal", TrophyRarity.Silver);
            Add("Spezialist", TrophyRarity.Silver);
            Add("Konter-Legende", TrophyRarity.Bronze);
            Add("Perfekte Runde", TrophyRarity.Gold);
            Add("Doppeltes Pech", TrophyRarity.Silver);

            // Charakter-spezifisch Basis (25–30)
            Add("Kanal-König", TrophyRarity.Silver);
            Add("Gute Seele", TrophyRarity.Silver);
            Add("Schnapsdrossel", TrophyRarity.Bronze);
            Add("Systemadministrator", TrophyRarity.Silver);
            Add("Rattenkönig", TrophyRarity.Silver);
            Add("Klempner-Meister", TrophyRarity.Silver);

            // Online / Sammeln (31–36)
            Add("Erste Runde Online", TrophyRarity.Bronze);
            Add("Ranglistenspieler", TrophyRarity.Silver);
            Add("Ausgeglichen", TrophyRarity.Bronze);
            Add("Unantastbar", TrophyRarity.Gold);
            Add("Sammler", TrophyRarity.Silver);
            Add("Glücksspieler", TrophyRarity.Bronze);

            // Geheim / Verschiedenes (37–50)
            Add("Mops-Liebe", TrophyRarity.Silver);
            Add("Suppe für alle", TrophyRarity.Gold);
            Add("Pfandmaster", TrophyRarity.Gold);
            Add("Wer lacht zuletzt?", TrophyRarity.Silver);
            Add("Katerstimmung", TrophyRarity.Bronze);
            Add("Kein Rückspulgerät", TrophyRarity.Silver);
            Add("Auf Kante", TrophyRarity.Gold);
            Add("Pausenlos", TrophyRarity.Gold);
            Add("Unverwundbar", TrophyRarity.Silver);
            Add("Vollbremsung", TrophyRarity.Gold);
            Add("Fassungslos", TrophyRarity.Bronze);
            Add("Doppel-Debuff", TrophyRarity.Silver);
            Add("Pfützenmeister", TrophyRarity.Bronze);
            Add("Blaue-Eimer-Legende", TrophyRarity.Gold);

            // --- DLC-Trophäen (51–56) ---
            Add(TROPHY_GUTEN_APPETIT, TrophyRarity.Silver);
            Add(TROPHY_BRAVES_MADCHEN, TrophyRarity.Silver);
            Add(TROPHY_AUF_DER_KANTE, TrophyRarity.Gold);
            Add(TROPHY_EHRLICHER_BETRUG, TrophyRarity.Gold);
            Add(TROPHY_DINGENELDANG, TrophyRarity.Gold);
            Add(TROPHY_STABILE_SEITENLAGE, TrophyRarity.Platinum, isSecret: true);
        }

        void Add(string id, TrophyRarity rarity, bool isSecret = false)
        {
            if (!trophies.ContainsKey(id))
                trophies[id] = new TrophyEntry { id = id, rarity = rarity, isSecret = isSecret };
        }

        public void Unlock(string id)
        {
            if (!trophies.TryGetValue(id, out var t) || t.isUnlocked) return;
            t.isUnlocked = true;
            t.progress = 1f;
            Save(id);
            OnTrophyUnlocked?.Invoke(id);
            if (trophySound != null) AudioManager.Instance?.PlaySFX(trophySound);
            Debug.Log($"🏆 Trophäe freigeschaltet: {id}");
        }

        public void UpdateProgress(string id, float value)
        {
            if (!trophies.TryGetValue(id, out var t) || t.isUnlocked) return;
            t.progress = Mathf.Clamp01(value);
            if (t.progress >= 1f) Unlock(id);
        }

        public bool IsUnlocked(string id) => trophies.TryGetValue(id, out var t) && t.isUnlocked;
        public float GetProgress(string id) => trophies.TryGetValue(id, out var t) ? t.progress : 0f;

        void Save(string id) => PlayerPrefs.SetInt("trophy_" + id, 1);
        void LoadProgress()
        {
            foreach (var kv in trophies)
                if (PlayerPrefs.HasKey("trophy_" + kv.Key))
                {
                    kv.Value.isUnlocked = PlayerPrefs.GetInt("trophy_" + kv.Key) == 1;
                    if (kv.Value.isUnlocked) kv.Value.progress = 1f;
                }
        }

        // ===== Trigger für die DLC-Trophäen =====
        public void NotifyLeBindeFatalities() => Unlock(TROPHY_GUTEN_APPETIT);
        public void NotifyHerta100Uses() => Unlock(TROPHY_BRAVES_MADCHEN);
        public void NotifyMellKante() => Unlock(TROPHY_AUF_DER_KANTE);
        public void NotifyMojoBetrug() => Unlock(TROPHY_EHRLICHER_BETRUG);
        public void NotifyMellRematch() => Unlock(TROPHY_STABILE_SEITENLAGE);

        [System.Serializable]
        class TrophyEntry
        {
            public string id;
            public TrophyRarity rarity;
            public bool isSecret;
            public bool isUnlocked;
            public float progress;
        }
    }
}
