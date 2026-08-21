using System;
using UnityEngine;

namespace PennerKombat
{
    /// <summary>
    /// (6) Kopplungs-Code für zwei Geräte — docs/CONTROLS.md §4.2.
    /// Trägt IP, Port und Raum-ID in einer kompakten Zeichenkette:
    /// <c>PK://192.168.1.10:47654/AB12CD</c>
    ///
    /// Dieselbe Zeichenkette ist der QR-Inhalt. Da das Projekt keine
    /// QR-Bibliothek mitbringt, rendert <see cref="QrCoupling"/> den Code
    /// standardmäßig als gut lesbaren Text-Code; mit ZXing im Projekt
    /// (Define <c>PK_ZXING</c>) wird daraus ein echtes QR-Bild.
    /// </summary>
    [Serializable]
    public struct RoomCode
    {
        public string ip;
        public int port;
        public string room;

        public const string Scheme = "PK://";

        public static RoomCode Create(string room = null)
        {
            return new RoomCode
            {
                ip = LanDiscovery.LocalIPv4(),
                port = MultiplayerConfig.Current.port,
                room = string.IsNullOrEmpty(room) ? NewRoomId() : room
            };
        }

        /// <summary>Sechsstelliger Code ohne verwechselbare Zeichen (kein O/0/I/1).</summary>
        public static string NewRoomId()
        {
            const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var chars = new char[6];
            for (int i = 0; i < chars.Length; i++)
                chars[i] = alphabet[UnityEngine.Random.Range(0, alphabet.Length)];
            return new string(chars);
        }

        public string ToPayload() => $"{Scheme}{ip}:{port}/{room}";

        public string ToWebSocketUrl() => $"ws://{ip}:{port}/kombat?room={room}";

        public override string ToString() => ToPayload();

        public static bool TryParse(string payload, out RoomCode code)
        {
            code = default;
            if (string.IsNullOrWhiteSpace(payload)) return false;

            string s = payload.Trim();
            if (s.StartsWith(Scheme, StringComparison.OrdinalIgnoreCase))
                s = s.Substring(Scheme.Length);

            // Form: ip:port/room
            int slash = s.LastIndexOf('/');
            if (slash <= 0 || slash >= s.Length - 1) return false;

            string hostPart = s.Substring(0, slash);
            string room = s.Substring(slash + 1).ToUpperInvariant();

            int colon = hostPart.LastIndexOf(':');
            if (colon <= 0) return false;

            string ip = hostPart.Substring(0, colon);
            if (!int.TryParse(hostPart.Substring(colon + 1), out int port)) return false;

            code = new RoomCode { ip = ip, port = port, room = room };
            return true;
        }

        /// <summary>Nur der Raumcode wurde eingetippt — Host per Discovery suchen.</summary>
        public static bool TryFindByRoomId(string roomId, out RoomCode code)
        {
            code = default;
            if (LanDiscovery.Instance == null || string.IsNullOrWhiteSpace(roomId)) return false;

            string wanted = roomId.Trim().ToUpperInvariant();
            foreach (var h in LanDiscovery.Instance.Hosts)
            {
                if (!string.Equals(h.room, wanted, StringComparison.OrdinalIgnoreCase)) continue;
                code = new RoomCode { ip = h.ip, port = h.port, room = h.room };
                return true;
            }
            return false;
        }
    }
}
