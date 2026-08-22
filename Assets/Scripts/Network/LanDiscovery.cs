using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace PennerKombat
{
    /// <summary>
    /// (7) Auto-Discovery im lokalen Netz — docs/CONTROLS.md §4.3.
    /// Der Host sendet alle 2 s einen UDP-Broadcast mit
    /// <c>PK1|ip|port|room|name</c>, Clients hören auf demselben Port.
    ///
    /// Der Empfang läuft in einem Hintergrund-Thread; gefundene Hosts werden
    /// über eine Queue auf den Main-Thread gereicht (Unity-API ist nicht
    /// thread-sicher).
    /// </summary>
    public class LanDiscovery : MonoBehaviour
    {
        public static LanDiscovery Instance { get; private set; }

        [Header("Netzwerk")]
        public int port = 47654;
        public float broadcastInterval = 2f;
        public float hostTimeout = 8f;

        [Serializable]
        public class Host
        {
            public string ip;
            public int port;
            public string room;
            public string name;
            public float lastSeen;
        }

        public event Action<Host> OnHostFound;
        public event Action OnHostListChanged;

        private readonly List<Host> hosts = new List<Host>();
        private readonly Queue<string> inbox = new Queue<string>();
        private readonly object inboxLock = new object();

        private UdpClient listener;
        private Thread listenThread;
        private volatile bool running;
        private bool advertising;
        private string advertisedRoom;
        private float nextBroadcast;

        public IReadOnlyList<Host> Hosts => hosts;
        public bool IsAdvertising => advertising;

        public static LanDiscovery Ensure()
        {
            if (Instance == null)
            {
                var go = new GameObject("~LanDiscovery");
                Instance = go.AddComponent<LanDiscovery>();
                DontDestroyOnLoad(go);
            }
            return Instance;
        }

        void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
        }

        void OnDestroy() => StopAll();
        void OnApplicationQuit() => StopAll();

        // ==================================================================
        //  Host: Raum bekanntmachen
        // ==================================================================
        public void StartAdvertising(string room)
        {
            advertisedRoom = room;
            advertising = true;
            nextBroadcast = 0f;
        }

        public void StopAdvertising() => advertising = false;

        // ==================================================================
        //  Client: nach Hosts suchen
        // ==================================================================
        public void StartListening()
        {
            if (running) return;
            running = true;
            listenThread = new Thread(ListenLoop) { IsBackground = true };
            listenThread.Start();
        }

        public void StopAll()
        {
            running = false;
            advertising = false;
            try { listener?.Close(); } catch { }
            listener = null;
        }

        void ListenLoop()
        {
            try
            {
                listener = new UdpClient();
                listener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                listener.Client.Bind(new IPEndPoint(IPAddress.Any, port));

                while (running)
                {
                    var remote = new IPEndPoint(IPAddress.Any, 0);
                    byte[] data = listener.Receive(ref remote);
                    string msg = Encoding.UTF8.GetString(data);
                    lock (inboxLock) inbox.Enqueue(msg);
                }
            }
            catch (SocketException)
            {
                // Port belegt oder Socket geschlossen — kein Discovery möglich
            }
            catch (ObjectDisposedException) { }
        }

        void Update()
        {
            // Broadcast senden
            if (advertising && Time.time >= nextBroadcast)
            {
                nextBroadcast = Time.time + broadcastInterval;
                Broadcast();
            }

            // Empfangene Nachrichten auf dem Main-Thread verarbeiten
            while (true)
            {
                string msg = null;
                lock (inboxLock) if (inbox.Count > 0) msg = inbox.Dequeue();
                if (msg == null) break;
                Parse(msg);
            }

            // Veraltete Hosts entfernen
            int before = hosts.Count;
            hosts.RemoveAll(h => Time.time - h.lastSeen > hostTimeout);
            if (hosts.Count != before) OnHostListChanged?.Invoke();
        }

        void Broadcast()
        {
            try
            {
                using (var udp = new UdpClient())
                {
                    udp.EnableBroadcast = true;
                    string payload = $"PK1|{LocalIPv4()}|{MultiplayerConfig.Current.port}|" +
                                     $"{advertisedRoom}|{MultiplayerConfig.Current.playerName}";
                    byte[] data = Encoding.UTF8.GetBytes(payload);
                    udp.Send(data, data.Length, new IPEndPoint(IPAddress.Broadcast, port));
                }
            }
            catch (SocketException e)
            {
                Debug.LogWarning($"[LanDiscovery] Broadcast fehlgeschlagen: {e.Message}");
            }
        }

        void Parse(string msg)
        {
            if (!msg.StartsWith("PK1|")) return;
            var parts = msg.Split('|');
            if (parts.Length < 5) return;
            if (!int.TryParse(parts[2], out int hostPort)) return;

            string ip = parts[1];
            string room = parts[3];

            // eigener Broadcast? ignorieren
            if (advertising && room == advertisedRoom) return;

            foreach (var h in hosts)
            {
                if (h.ip != ip || h.room != room) continue;
                h.lastSeen = Time.time;
                return;
            }

            var host = new Host { ip = ip, port = hostPort, room = room, name = parts[4], lastSeen = Time.time };
            hosts.Add(host);
            OnHostFound?.Invoke(host);
            OnHostListChanged?.Invoke();
        }

        public static string LocalIPv4()
        {
            try
            {
                // Trick ohne DNS: UDP-Socket „verbinden", lokale Adresse ablesen
                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
                {
                    socket.Connect("8.8.8.8", 65530);
                    if (socket.LocalEndPoint is IPEndPoint ep) return ep.Address.ToString();
                }
            }
            catch { }
            return "127.0.0.1";
        }
    }
}
