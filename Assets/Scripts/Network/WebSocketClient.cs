using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace PennerKombat
{
    /// <summary>
    /// Minimaler WebSocket-Client für den Online-Modus (Relay). Alle
    /// Unity-API-Aufrufe laufen über SynchronizationContext auf dem
    /// Haupt-Thread. Für ein produktives Release empfiehlt sich Mirror oder
    /// Netcode for GameObjects; dieser Client ist der dokumentierte Ausbauweg.
    /// </summary>
    public class WebSocketClient : MonoBehaviour
    {
        [Header("Connection")]
        public string serverUrl = "ws://localhost:5000/kombat";
        public float reconnectInterval = 3f;

        public event Action<string> OnMessage;
        public event Action OnConnected;
        public event Action OnDisconnected;

        private ClientWebSocket ws;
        private CancellationTokenSource cts;
        private bool connecting;

        public bool IsConnected => ws != null && ws.State == WebSocketState.Open;

        public async void Connect()
        {
            if (connecting || IsConnected) return;
            connecting = true;
            cts = new CancellationTokenSource();
            try
            {
                ws = new ClientWebSocket();
                await ws.ConnectAsync(new Uri(serverUrl), cts.Token);
                Debug.Log("WebSocket verbunden: " + serverUrl);
                RunOnMainThread(() => OnConnected?.Invoke());
                _ = ReceiveLoop(cts.Token);
            }
            catch (Exception e)
            {
                Debug.LogWarning("WebSocket-Fehler: " + e.Message);
                ws?.Dispose();
                ws = null;
                RunOnMainThread(() => OnDisconnected?.Invoke());
                // Reconnect
                if (!cts.IsCancellationRequested)
                    _ = ReconnectAfterDelay();
            }
            finally
            {
                connecting = false;
            }
        }

        async Task ReconnectAfterDelay()
        {
            await Task.Delay(TimeSpan.FromSeconds(reconnectInterval));
            if (!IsCancelled) Connect();
        }

        bool IsCancelled => cts == null || cts.IsCancellationRequested;

        async Task ReceiveLoop(CancellationToken token)
        {
            var buffer = new byte[4096];
            try
            {
                while (ws != null && ws.State == WebSocketState.Open)
                {
                    var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), token);
                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        string msg = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        RunOnMainThread(() => OnMessage?.Invoke(msg));
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        break;
                    }
                }
            }
            catch { /* Verbindung beendet */ }
            RunOnMainThread(() => OnDisconnected?.Invoke());
        }

        public async void Send(string message)
        {
            if (ws == null || ws.State != WebSocketState.Open) return;
            try
            {
                var bytes = Encoding.UTF8.GetBytes(message);
                await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cts.Token);
            }
            catch { /* ignorieren */ }
        }

        public async void Close()
        {
            if (ws != null && ws.State == WebSocketState.Open)
            {
                try { await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None); }
                catch { /* */ }
                ws.Dispose();
                ws = null;
            }
            cts?.Cancel();
        }

        void OnDestroy() => Close();

        /// <summary>Dispatcher auf den Unity-Haupt-Thread.</summary>
        static void RunOnMainThread(Action action)
        {
            var sync = SynchronizationContext.Current;
            if (sync != null) sync.Post(_ => action(), null);
            else action();
        }
    }
}
