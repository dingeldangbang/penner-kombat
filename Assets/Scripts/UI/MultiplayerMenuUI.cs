using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PennerKombat
{
    /// <summary>
    /// (8) Multiplayer-Menü — docs/CONTROLS.md §6.
    /// Vier Modi: Lokal (2 Spieler an einem Gerät), Kopplung per Raumcode/QR,
    /// LAN-Suche und Online-Relay. Alle UI-Felder sind optional; fehlende
    /// Referenzen werden übersprungen.
    /// </summary>
    public class MultiplayerMenuUI : MonoBehaviour
    {
        [Header("Modus-Buttons")]
        public Button localButton;
        public Button codeButton;
        public Button lanButton;
        public Button onlineButton;
        public Button backButton;

        [Header("Panels")]
        public GameObject localPanel;
        public GameObject codePanel;
        public GameObject lanPanel;
        public GameObject onlinePanel;

        [Header("Lokal")]
        public TMP_Dropdown player2Dropdown;      // Mensch / KI-Stufe
        public TextMeshProUGUI localHintText;

        [Header("Kopplung")]
        public Button hostButton;
        public Button scanButton;
        public TMP_InputField codeInput;
        public Button joinByCodeButton;
        public RawImage qrImage;
        public TextMeshProUGUI codeText;

        [Header("LAN")]
        public RectTransform hostListContainer;
        public Button refreshButton;
        public TextMeshProUGUI lanStatusText;

        [Header("Online")]
        public TMP_InputField relayInput;
        public TMP_InputField nameInput;
        public Button connectRelayButton;

        private readonly List<GameObject> hostEntries = new List<GameObject>();

        void Start()
        {
            var cfg = MultiplayerConfig.Current;
            LanDiscovery.Ensure().StartListening();
            LanDiscovery.Instance.OnHostListChanged += RefreshHostList;
            QrCoupling.Ensure();

            // --- Modi ---
            localButton?.onClick.AddListener(() => SelectMode("Local"));
            codeButton?.onClick.AddListener(() => SelectMode("QrCode"));
            lanButton?.onClick.AddListener(() => SelectMode("Lan"));
            onlineButton?.onClick.AddListener(() => SelectMode("Online"));
            backButton?.onClick.AddListener(() =>
                UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu"));

            // --- Lokal: Gegner wählen ---
            if (player2Dropdown != null)
            {
                var options = new List<string> { "Spieler 2 (Tastatur)" };
                for (int i = 0; i < 6; i++) options.Add("KI — " + AIController.DifficultyName((AIDifficulty)i));
                player2Dropdown.ClearOptions();
                player2Dropdown.AddOptions(options);
                player2Dropdown.value = cfg.aiDifficulty + 1;
                player2Dropdown.onValueChanged.AddListener(i =>
                {
                    if (i == 0) { cfg.aiDifficulty = -1; }
                    else { cfg.aiDifficulty = i - 1; }
                    cfg.Save();
                    UpdateLocalHint();
                });
            }
            UpdateLocalHint();

            // --- Kopplung ---
            hostButton?.onClick.AddListener(HostRoom);
            scanButton?.onClick.AddListener(() => QrCoupling.Instance.StartScan());
            joinByCodeButton?.onClick.AddListener(JoinByCode);

            // --- LAN ---
            refreshButton?.onClick.AddListener(RefreshHostList);

            // --- Online ---
            if (relayInput != null)
            {
                relayInput.text = cfg.relayServer;
                relayInput.onEndEdit.AddListener(v => { cfg.relayServer = v; cfg.Save(); });
            }
            if (nameInput != null)
            {
                nameInput.text = cfg.playerName;
                nameInput.onEndEdit.AddListener(v => { cfg.playerName = v; cfg.Save(); });
            }
            connectRelayButton?.onClick.AddListener(ConnectRelay);

            SelectMode(cfg.lastMode);
            RefreshHostList();
        }

        void OnDestroy()
        {
            if (LanDiscovery.Instance != null)
                LanDiscovery.Instance.OnHostListChanged -= RefreshHostList;
        }

        // ==================================================================

        void SelectMode(string mode)
        {
            var cfg = MultiplayerConfig.Current;
            cfg.lastMode = mode;
            cfg.Save();

            localPanel?.SetActive(mode == "Local");
            codePanel?.SetActive(mode == "QrCode");
            lanPanel?.SetActive(mode == "Lan");
            onlinePanel?.SetActive(mode == "Online");
        }

        void UpdateLocalHint()
        {
            if (localHintText == null) return;
            var cfg = MultiplayerConfig.Current;
            localHintText.text = cfg.aiDifficulty < 0
                ? "Spieler 1: WASD + J/K/L · Spieler 2: Pfeiltasten + Nummernblock"
                : $"Spieler 1: WASD + J/K/L · Gegner: KI ({AIController.DifficultyName(cfg.Difficulty)})";
        }

        // --- Kopplung ---
        void HostRoom()
        {
            var code = QrCoupling.Instance.HostRoom();
            if (codeText != null)
                codeText.text = $"RAUMCODE  <b>{code.room}</b>\n{code.ip}:{code.port}";
            if (qrImage != null && QrCoupling.Instance.qrDisplay == null)
                QrCoupling.Instance.qrDisplay = qrImage;
        }

        void JoinByCode()
        {
            string input = codeInput != null ? codeInput.text : "";
            bool ok = QrCoupling.Instance.JoinByCode(input);
            if (codeText != null)
                codeText.text = ok ? $"Verbinde mit {input.ToUpperInvariant()} …"
                                   : "Code nicht gefunden — läuft der Host schon?";
        }

        // --- LAN-Liste ---
        void RefreshHostList()
        {
            foreach (var go in hostEntries) Destroy(go);
            hostEntries.Clear();

            var discovery = LanDiscovery.Instance;
            if (discovery == null || hostListContainer == null) return;

            int i = 0;
            foreach (var host in discovery.Hosts)
            {
                var entry = new GameObject($"Host_{host.room}", typeof(RectTransform));
                entry.transform.SetParent(hostListContainer, false);
                var rt = (RectTransform)entry.transform;
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(0f, -i * 64f);
                rt.sizeDelta = new Vector2(0f, 58f);

                var img = entry.AddComponent<Image>();
                img.color = PennerPalette.NightBlue.WithAlpha(0.8f);

                var label = new GameObject("Label", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
                label.transform.SetParent(rt, false);
                label.rectTransform.anchorMin = Vector2.zero;
                label.rectTransform.anchorMax = Vector2.one;
                label.rectTransform.offsetMin = new Vector2(16f, 0f);
                label.rectTransform.offsetMax = new Vector2(-16f, 0f);
                label.alignment = TextAlignmentOptions.Left;
                label.fontSize = 24f;
                label.color = PennerPalette.Pure;
                label.text = $"{host.name}  ·  {host.ip}  ·  Raum {host.room}";

                var btn = entry.AddComponent<Button>();
                var captured = host;
                btn.onClick.AddListener(() =>
                {
                    QrCoupling.Instance.Connect(new RoomCode
                    { ip = captured.ip, port = captured.port, room = captured.room });
                    if (lanStatusText != null) lanStatusText.text = $"Verbinde mit {captured.name} …";
                });

                hostEntries.Add(entry);
                i++;
            }

            if (lanStatusText != null)
                lanStatusText.text = i == 0
                    ? "Keine Hosts im Netz gefunden. Gleiches WLAN? Host gestartet?"
                    : $"{i} Host(s) gefunden";
        }

        // --- Online ---
        void ConnectRelay()
        {
            var cfg = MultiplayerConfig.Current;
            var nm = NetworkManager.Instance;
            if (nm == null) return;
            if (nm.client != null) nm.client.serverUrl = cfg.relayServer;
            nm.playerName = cfg.playerName;
            nm.JoinRoom(RoomCode.NewRoomId());
        }
    }
}
