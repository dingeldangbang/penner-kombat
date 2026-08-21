using UnityEngine;

namespace PennerKombat
{
    /// <summary>
    /// Hochstufige Netzwerk-Verwaltung (Lobby, Room-Join, Ready-Sync,
    /// Spielzustands-Austausch). Baut auf WebSocketClient auf.
    /// </summary>
    public class NetworkManager : MonoBehaviour
    {
        public static NetworkManager Instance;

        [Header("Netzwerk")]
        public string playerName = "Spieler";
        public WebSocketClient client;

        private string roomName;
        private bool isReady;
        private bool isHost;

        public string RoomName => roomName;
        public bool IsReady => isReady;
        public bool IsHost => isHost;

        void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(this); return; }
            DontDestroyOnLoad(gameObject);

            if (client == null) client = gameObject.AddComponent<WebSocketClient>();
            // Name und Relay aus der Multiplayer-Konfiguration übernehmen
            var cfg = MultiplayerConfig.Current;
            if (!string.IsNullOrEmpty(cfg.playerName)) playerName = cfg.playerName;
            if (!string.IsNullOrEmpty(cfg.relayServer)) client.serverUrl = cfg.relayServer;
            client.OnConnected += OnConnected;
            client.OnMessage += HandleMessage;
        }

        void OnDestroy()
        {
            if (client != null)
            {
                client.OnConnected -= OnConnected;
                client.OnMessage -= HandleMessage;
            }
        }

        public void JoinRoom(string room)
        {
            roomName = room;
            isReady = false;
            client.Connect();
        }

        /// <summary>Als Host einen Raum eröffnen und ihn im LAN bekanntmachen.</summary>
        public RoomCode HostRoom()
        {
            var code = RoomCode.Create();
            isHost = true;
            LanDiscovery.Ensure().StartAdvertising(code.room);
            JoinRoom(code.room);
            return code;
        }

        /// <summary>Einem per Code/QR/LAN gefundenen Host beitreten.</summary>
        public void JoinHost(RoomCode code)
        {
            isHost = false;
            if (client != null) client.serverUrl = code.ToWebSocketUrl();
            JoinRoom(code.room);
        }

        public void LeaveRoom()
        {
            LanDiscovery.Instance?.StopAdvertising();
            Send($"{{\"type\":\"leave\",\"room\":\"{roomName}\",\"player\":\"{playerName}\"}}");
            client?.Close();
            roomName = null;
            isHost = false;
        }

        void OnConnected()
        {
            if (string.IsNullOrEmpty(roomName)) return;
            Send($"{{\"type\":\"join\",\"room\":\"{roomName}\",\"player\":\"{playerName}\"}}");
        }

        public void SetReady(bool ready)
        {
            isReady = ready;
            Send($"{{\"type\":\"ready\",\"room\":\"{roomName}\",\"ready\":{ready.ToString().ToLower()}}}");
        }

        public void SendPlayerState(Vector3 pos, Vector3 vel, float hp, int combo)
        {
            Send($"{{\"type\":\"update\",\"pos\":[{pos.x:F2},{pos.y:F2},{pos.z:F2}]," +
                 $"\"vel\":[{vel.x:F2},{vel.y:F2},{vel.z:F2}],\"hp\":{hp:F1},\"combo\":{combo}}}");
        }

        void HandleMessage(string raw)
        {
            // JSON-Parsing (z.B. Newtonsoft) hier einhängen.
            // Beispielhafte Verarbeitung für gegnerischen Spielzustand.
            if (raw.Contains("\"type\":\"start\""))
            {
                Debug.Log("Match gestartet (Netzwerk).");
            }
            else if (raw.Contains("\"type\":\"state\""))
            {
                // Gegnerposition/-HP anwenden
            }
            else
            {
                // Fallback: als Chat anzeigen
                if (LobbySystem.Instance != null) LobbySystem.Instance.AddChat(raw);
            }
        }

        /// <summary>Sendet eine Nachricht über den verbundenen Client.</summary>
        public void Send(string msg) => client?.Send(msg);
    }
}
