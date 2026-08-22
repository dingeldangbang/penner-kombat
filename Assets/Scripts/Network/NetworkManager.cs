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
        private string localPlayerId;
        private string lastRoomState;
        private float lastPongTime;

        public string RoomName => roomName;
        public string LocalPlayerId => localPlayerId;
        public string LastRoomState => lastRoomState;
        public bool IsReady => isReady;
        public bool IsHost => isHost;

        // --- Ereignisse für UI und Spiel (Protokoll: docs/SERVER.md) ---
        public event System.Action<string> OnJoined;              // eigene Raum-ID
        public event System.Action<string, string> OnPeerJoined;  // id, name
        public event System.Action<string> OnPeerLeft;            // id
        public event System.Action OnMatchStart;
        public event System.Action<string> OnPeerUpdate;          // rohe JSON-Zeile
        public event System.Action<string> OnPeerAction;          // Aktionsname
        public event System.Action<string> OnServerError;

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
            string type = Field(raw, "type");
            switch (type)
            {
                case "welcome":
                    localPlayerId = Field(raw, "player");
                    break;

                case "joined":
                    roomName = Field(raw, "room");
                    isHost = raw.Contains("\"host\":true");
                    OnJoined?.Invoke(roomName);
                    break;

                case "peer_joined":
                    OnPeerJoined?.Invoke(Field(raw, "player"), Field(raw, "name"));
                    LobbySystem.Instance?.AddChat($"{Field(raw, "name")} ist beigetreten.");
                    break;

                case "peer_left":
                    OnPeerLeft?.Invoke(Field(raw, "player"));
                    LobbySystem.Instance?.AddChat($"{Field(raw, "name")} hat den Raum verlassen.");
                    break;

                case "room_state":
                    lastRoomState = raw;
                    break;

                case "start":
                    Debug.Log("Match gestartet (Netzwerk).");
                    OnMatchStart?.Invoke();
                    break;

                case "update":
                case "state":
                    OnPeerUpdate?.Invoke(raw);
                    break;

                case "action":
                    OnPeerAction?.Invoke(Field(raw, "action"));
                    break;

                case "chat":
                    LobbySystem.Instance?.AddChat($"{Field(raw, "name")}: {Field(raw, "text")}");
                    break;

                case "error":
                    string err = Field(raw, "error");
                    Debug.LogWarning($"[Relay] Fehler: {err}");
                    OnServerError?.Invoke(err);
                    break;

                case "pong":
                    lastPongTime = Time.time;
                    break;
            }
        }

        /// <summary>Minimaler Feld-Extraktor — reicht für das flache Relay-Protokoll.</summary>
        public static string Field(string json, string key)
        {
            if (string.IsNullOrEmpty(json)) return null;
            string pattern = $"\"{key}\":";
            int start = json.IndexOf(pattern, System.StringComparison.Ordinal);
            if (start < 0) return null;
            start += pattern.Length;
            while (start < json.Length && json[start] == ' ') start++;
            if (start >= json.Length) return null;

            if (json[start] == '"')
            {
                start++;
                int end = json.IndexOf('"', start);
                return end < 0 ? null : json.Substring(start, end - start);
            }

            int stop = json.IndexOfAny(new[] { ',', '}' }, start);
            return stop < 0 ? null : json.Substring(start, stop - start).Trim();
        }

        /// <summary>Chatnachricht in den Raum schicken.</summary>
        public void SendChat(string text)
            => Send($"{{\"type\":\"chat\",\"text\":\"{Escape(text)}\"}}");

        /// <summary>Aktion (Angriff, Spezial, Sprung …) an die Gegenseite melden.</summary>
        public void SendAction(string action)
            => Send($"{{\"type\":\"action\",\"action\":\"{Escape(action)}\"}}");

        /// <summary>Latenzmessung gegen den Relay.</summary>
        public void SendPing() => Send($"{{\"type\":\"ping\",\"t\":{Time.time:F3}}}");

        static string Escape(string s)
            => string.IsNullOrEmpty(s) ? "" : s.Replace("\\", "\\\\").Replace("\"", "\\\"");

        /// <summary>Sendet eine Nachricht über den verbundenen Client.</summary>
        public void Send(string msg) => client?.Send(msg);
    }
}
