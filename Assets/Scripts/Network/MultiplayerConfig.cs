using System;
using UnityEngine;

namespace PennerKombat
{
    /// <summary>
    /// (9) Multiplayer-Konfiguration — docs/CONTROLS.md §7.
    /// Spielername, Farbe, Port, Relay-Server und der zuletzt gewählte Modus.
    /// Persistenz als JSON in den PlayerPrefs.
    /// </summary>
    [Serializable]
    public class MultiplayerConfig
    {
        public const string PrefsKey = "pk_mp_config";

        public string playerName = "Penner";
        public int playerColor = 0;                 // Index in PennerPalette-Auswahl
        public int port = 47654;
        public string relayServer = "ws://localhost:5000/kombat";
        public int maxPlayers = 2;
        public float pingTimeout = 5f;

        /// <summary>Local, QrCode, Lan, Online</summary>
        public string lastMode = "Local";
        public int aiDifficulty = (int)AIDifficulty.Medium;

        private static MultiplayerConfig current;

        public static MultiplayerConfig Current
        {
            get
            {
                if (current == null) current = Load();
                return current;
            }
        }

        public static MultiplayerConfig Load()
        {
            string json = PlayerPrefs.GetString(PrefsKey, "");
            if (string.IsNullOrEmpty(json)) return new MultiplayerConfig();
            try { return JsonUtility.FromJson<MultiplayerConfig>(json) ?? new MultiplayerConfig(); }
            catch { return new MultiplayerConfig(); }
        }

        public void Save()
        {
            current = this;
            PlayerPrefs.SetString(PrefsKey, JsonUtility.ToJson(this));
            PlayerPrefs.Save();
        }

        public AIDifficulty Difficulty
        {
            get => (AIDifficulty)Mathf.Clamp(aiDifficulty, 0, 5);
            set { aiDifficulty = (int)value; Save(); }
        }

        public Color Color => PlayerColors[Mathf.Clamp(playerColor, 0, PlayerColors.Length - 1)];

        public static readonly Color[] PlayerColors =
        {
            PennerPalette.BloodRed,
            PennerPalette.NeonBlue,
            PennerPalette.PoisonGrn,
            PennerPalette.Gold,
            PennerPalette.WarmOrange,
            PennerPalette.Earth
        };
    }
}
