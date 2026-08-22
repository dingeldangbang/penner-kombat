using UnityEngine;

namespace PennerKombat
{
    /// <summary>
    /// (7) Netzwerk-Interpolation im 3D-Raum — docs/3D.md §8.
    /// Hängt an einem *entfernten* Kämpfer und glättet die über den Relay
    /// eintreffenden Zustände (Position, Rotation, HP, Combo, Block).
    ///
    /// Der lokale Spieler sendet in `sendRate` Hz; der entfernte wird zwischen
    /// den Paketen interpoliert und bei Bedarf extrapoliert (kurze Aussetzer).
    /// Ein echtes Rollback-Netcode ist das ausdrücklich nicht — siehe docs/SERVER.md.
    /// </summary>
    [RequireComponent(typeof(FighterController))]
    public class NetworkSync3D : MonoBehaviour
    {
        [Header("Rolle")]
        [Tooltip("true = dieser Kämpfer wird lokal gesteuert und gesendet.")]
        public bool isLocal;
        public string remotePlayerId;

        [Header("Takt")]
        public float sendRate = 20f;              // Pakete pro Sekunde
        public float interpolationSpeed = 12f;
        public float extrapolationLimit = 0.25f;  // s, danach einfrieren

        private FighterController fighter;
        private Rigidbody rb;

        private Vector3 targetPosition;
        private Quaternion targetRotation;
        private Vector3 targetVelocity;
        private float targetHp;
        private int targetCombo;
        private bool targetBlocking;
        private float lastPacketTime;
        private float nextSend;

        void Awake()
        {
            fighter = GetComponent<FighterController>();
            rb = GetComponent<Rigidbody>();
            targetPosition = transform.position;
            targetRotation = transform.rotation;
            targetHp = fighter.currentHP;
        }

        void OnEnable()
        {
            var nm = NetworkManager.Instance;
            if (nm == null) return;
            nm.OnPeerUpdate += HandleUpdate;
            nm.OnPeerAction += HandleAction;
        }

        void OnDisable()
        {
            var nm = NetworkManager.Instance;
            if (nm == null) return;
            nm.OnPeerUpdate -= HandleUpdate;
            nm.OnPeerAction -= HandleAction;
        }

        void Update()
        {
            if (isLocal) { TickSend(); return; }
            TickRemote();
        }

        // ==================================================================
        //  Lokal: senden
        // ==================================================================
        void TickSend()
        {
            if (Time.time < nextSend) return;
            nextSend = Time.time + 1f / Mathf.Max(1f, sendRate);

            var nm = NetworkManager.Instance;
            if (nm == null || string.IsNullOrEmpty(nm.RoomName)) return;

            Vector3 p = transform.position;
            Vector3 v = rb != null ? rb.velocity : Vector3.zero;
            float yaw = transform.eulerAngles.y;

            nm.Send($"{{\"type\":\"update\"," +
                    $"\"pos\":[{p.x:F2},{p.y:F2},{p.z:F2}]," +
                    $"\"vel\":[{v.x:F2},{v.y:F2},{v.z:F2}]," +
                    $"\"yaw\":{yaw:F1},\"hp\":{fighter.currentHP:F1}," +
                    $"\"combo\":{fighter.comboCount}," +
                    $"\"block\":{(fighter.isBlocking ? "true" : "false")}}}");
        }

        /// <summary>Aktion melden (wird vom Kampfsystem gerufen).</summary>
        public void SendAction(string action)
        {
            if (!isLocal) return;
            NetworkManager.Instance?.SendAction(action);
        }

        // ==================================================================
        //  Entfernt: empfangen und glätten
        // ==================================================================
        void HandleUpdate(string json)
        {
            if (isLocal) return;
            string sender = NetworkManager.Field(json, "player");
            if (!string.IsNullOrEmpty(remotePlayerId) && sender != remotePlayerId) return;

            if (TryVector(json, "pos", out Vector3 pos)) targetPosition = pos;
            if (TryVector(json, "vel", out Vector3 vel)) targetVelocity = vel;

            string yaw = NetworkManager.Field(json, "yaw");
            if (float.TryParse(yaw, out float y)) targetRotation = Quaternion.Euler(0f, y, 0f);

            string hp = NetworkManager.Field(json, "hp");
            if (float.TryParse(hp, out float h)) targetHp = h;

            string combo = NetworkManager.Field(json, "combo");
            if (int.TryParse(combo, out int c)) targetCombo = c;

            targetBlocking = NetworkManager.Field(json, "block") == "true";
            lastPacketTime = Time.time;
        }

        void HandleAction(string action)
        {
            if (isLocal || string.IsNullOrEmpty(action)) return;
            switch (action)
            {
                case "light":    fighter.StartAttack(false); break;
                case "heavy":    fighter.StartAttack(true);  break;
                case "jump":     fighter.Jump();             break;
                case "roll":     fighter.StartRoll(fighter.transform.forward); break;
                case "special1": fighter.Special1();         break;
                case "special2": fighter.Special2();         break;
            }
        }

        void TickRemote()
        {
            float age = Time.time - lastPacketTime;

            // Kurze Aussetzer überbrücken: Position mit letzter Geschwindigkeit fortschreiben
            Vector3 goal = targetPosition;
            if (age > 0f && age < extrapolationLimit)
                goal += targetVelocity * age;

            transform.position = Vector3.Lerp(transform.position, goal, interpolationSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, interpolationSpeed * Time.deltaTime);

            if (rb != null && !rb.isKinematic) rb.velocity = Vector3.zero;   // Physik nicht gegenrechnen

            fighter.currentHP = Mathf.Lerp(fighter.currentHP, targetHp, interpolationSpeed * Time.deltaTime);
            fighter.comboCount = targetCombo;
            fighter.isBlocking = targetBlocking;
        }

        static bool TryVector(string json, string key, out Vector3 result)
        {
            result = Vector3.zero;
            string pattern = $"\"{key}\":[";
            int start = json.IndexOf(pattern, System.StringComparison.Ordinal);
            if (start < 0) return false;
            start += pattern.Length;
            int end = json.IndexOf(']', start);
            if (end < 0) return false;

            var parts = json.Substring(start, end - start).Split(',');
            if (parts.Length < 3) return false;

            return float.TryParse(parts[0], out result.x)
                 & float.TryParse(parts[1], out result.y)
                 & float.TryParse(parts[2], out result.z);
        }
    }
}
