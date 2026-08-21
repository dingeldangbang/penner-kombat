using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace PennerKombat
{
    /// <summary>
    /// (6) Geräte-Kopplung per Code bzw. QR — docs/CONTROLS.md §4.2.
    ///
    /// **Ohne Zusatzpaket** zeigt der Host einen sechsstelligen Raumcode; der
    /// zweite Spieler tippt ihn ein oder wählt den Host aus der LAN-Liste.
    /// **Mit ZXing** (Scripting-Define <c>PK_ZXING</c>) wird derselbe Payload
    /// zusätzlich als QR-Bild gerendert und lässt sich per Kamera scannen.
    /// </summary>
    public class QrCoupling : MonoBehaviour
    {
        public static QrCoupling Instance { get; private set; }

        [Header("UI (optional)")]
        public RawImage qrDisplay;
        public Text codeLabel;
        public TMPro.TextMeshProUGUI codeLabelTMP;

        [Header("Kamera-Scan (nur mit PK_ZXING)")]
        public RawImage cameraPreview;
        public float scanInterval = 0.25f;

        public RoomCode Current { get; private set; }
        public bool IsScanning { get; private set; }

        public System.Action<RoomCode> OnCodeScanned;

        public static QrCoupling Ensure()
        {
            if (Instance == null)
            {
                var go = new GameObject("~QrCoupling");
                Instance = go.AddComponent<QrCoupling>();
            }
            return Instance;
        }

        void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(this); return; }
        }

        // ==================================================================
        //  Host
        // ==================================================================

        /// <summary>Erzeugt einen Raumcode, zeigt ihn an und startet den Broadcast.</summary>
        public RoomCode HostRoom()
        {
            Current = RoomCode.Create();
            ShowCode(Current);

            LanDiscovery.Ensure().StartAdvertising(Current.room);
            NetworkManager.Instance?.JoinRoom(Current.room);
            return Current;
        }

        void ShowCode(RoomCode code)
        {
            string text = $"RAUMCODE\n{code.room}\n{code.ip}:{code.port}";
            if (codeLabel != null) codeLabel.text = text;
            if (codeLabelTMP != null) codeLabelTMP.text = text;

            var tex = RenderPayload(code.ToPayload());
            if (qrDisplay != null && tex != null)
            {
                qrDisplay.texture = tex;
                qrDisplay.gameObject.SetActive(true);
            }
        }

        /// <summary>QR-Bild erzeugen — nur mit ZXing, sonst null.</summary>
        public Texture2D RenderPayload(string payload)
        {
#if PK_ZXING
            var writer = new ZXing.BarcodeWriterGeneric
            {
                Format = ZXing.BarcodeFormat.QR_CODE,
                Options = new ZXing.QrCode.QrCodeEncodingOptions { Width = 512, Height = 512, Margin = 1 }
            };
            var matrix = writer.Encode(payload);
            var tex = new Texture2D(matrix.Width, matrix.Height, TextureFormat.RGBA32, false);
            for (int y = 0; y < matrix.Height; y++)
                for (int x = 0; x < matrix.Width; x++)
                    tex.SetPixel(x, matrix.Height - 1 - y, matrix[x, y] ? Color.black : Color.white);
            tex.filterMode = FilterMode.Point;
            tex.Apply();
            return tex;
#else
            Debug.Log($"[QrCoupling] Kein QR-Encoder im Projekt (Define PK_ZXING fehlt). " +
                      $"Nutze den Raumcode: {payload}");
            return null;
#endif
        }

        // ==================================================================
        //  Client
        // ==================================================================

        /// <summary>Eingetippten Code oder QR-Payload auswerten und verbinden.</summary>
        public bool JoinByCode(string input)
        {
            RoomCode code;
            bool ok = RoomCode.TryParse(input, out code) || RoomCode.TryFindByRoomId(input, out code);
            if (!ok)
            {
                Debug.LogWarning($"[QrCoupling] Code nicht erkannt: {input}");
                return false;
            }

            Current = code;
            Connect(code);
            return true;
        }

        public void Connect(RoomCode code)
        {
            var nm = NetworkManager.Instance;
            if (nm == null)
            {
                Debug.LogWarning("[QrCoupling] Kein NetworkManager in der Szene.");
                return;
            }
            if (nm.client != null) nm.client.serverUrl = code.ToWebSocketUrl();
            nm.JoinRoom(code.room);
        }

        /// <summary>Kamera-Scan (benötigt ZXing + Kameraberechtigung).</summary>
        public void StartScan()
        {
#if PK_ZXING
            if (IsScanning) return;
            StartCoroutine(ScanRoutine());
#else
            Debug.Log("[QrCoupling] Scannen benötigt ZXing (Define PK_ZXING). " +
                      "Bitte den Raumcode eintippen oder den Host aus der LAN-Liste wählen.");
#endif
        }

#if PK_ZXING
        IEnumerator ScanRoutine()
        {
            IsScanning = true;
            var cam = new WebCamTexture(640, 480, 30);
            cam.Play();
            if (cameraPreview != null) cameraPreview.texture = cam;

            var reader = new ZXing.BarcodeReaderGeneric();
            float next = 0f;

            while (IsScanning)
            {
                if (Time.time >= next && cam.width > 100)
                {
                    next = Time.time + scanInterval;
                    var result = reader.Decode(cam.GetPixels32(), cam.width, cam.height);
                    if (result != null && RoomCode.TryParse(result.Text, out RoomCode code))
                    {
                        OnCodeScanned?.Invoke(code);
                        Connect(code);
                        break;
                    }
                }
                yield return null;
            }

            cam.Stop();
            IsScanning = false;
        }
#endif

        public void StopScan() => IsScanning = false;
    }
}
