using System.Collections;
using UnityEngine;

namespace PennerKombat
{
    /// <summary>
    /// Kamera-Feedback laut Spec Abschnitt 7: Screen-Shake (Amplitude in Pixel-
    /// Äquivalent), Combo-Zoom, Zeitlupe und Frame-Freeze.
    /// Hängt sich automatisch an die Hauptkamera und arbeitet additiv zum
    /// <see cref="CameraController"/> (der die Position in LateUpdate setzt —
    /// dieser Shake läuft danach in einem eigenen LateUpdate mit höherer
    /// Ausführungsreihenfolge).
    /// </summary>
    [DefaultExecutionOrder(500)]
    public class CameraShake : MonoBehaviour
    {
        public static CameraShake Instance { get; private set; }

        [Header("Umrechnung")]
        [Tooltip("Welt-Einheiten pro 'Pixel' Shake-Amplitude aus der Spec.")]
        public float pixelToWorld = 0.01f;

        [Header("Combo-Zoom (Spec 7)")]
        public float zoomLerpSpeed = 4f;

        private float shakeAmplitude;
        private float shakeTimer;
        private float shakeDuration;
        private float targetZoom = 1f;
        private float currentZoom = 1f;
        private Camera cam;
        private float baseFov;
        private Coroutine hitStopRoutine;

        public static CameraShake Ensure()
        {
            if (Instance == null)
            {
                var camGo = Camera.main != null ? Camera.main.gameObject : new GameObject("Main Camera");
                Instance = camGo.GetComponent<CameraShake>() ?? camGo.AddComponent<CameraShake>();
            }
            return Instance;
        }

        void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) { Destroy(this); return; }
            cam = GetComponent<Camera>();
            if (cam != null) baseFov = cam.fieldOfView;
        }

        // ---------------- statische Kurz-API ----------------

        /// <summary>Screen-Shake. <paramref name="pixels"/> laut Spec-Tabelle (2/5/8/15/20).</summary>
        public static void Shake(float pixels, float duration)
        {
            if (Instance == null) Ensure();
            Instance?.DoShake(pixels, duration);
        }

        /// <summary>Kurzer Hitstop in Sekunden (Realtime), Zeit friert fast ein.</summary>
        public static void HitStop(float seconds, float scale = 0.05f)
        {
            if (Instance == null) Ensure();
            Instance?.DoHitStop(seconds, scale);
        }

        /// <summary>Frame-Freeze für n Frames (Krit-Treffer, Spec 5.4).</summary>
        public static void FrameFreeze(int frames)
        {
            if (Instance == null) Ensure();
            Instance?.DoFrameFreeze(frames);
        }

        /// <summary>Zeitlupe (Mops-Kommando 0,1× · X-Ray 0,5× · Fatality 0,3×).</summary>
        public static void SlowMotion(float scale, float realSeconds)
        {
            if (Instance == null) Ensure();
            Instance?.DoHitStop(realSeconds, scale);
        }

        /// <summary>Zoomfaktor aus der Combo-Stufe (1,05× / 1,10× / 1,15× / 1,25×).</summary>
        public static void SetComboZoom(int combo)
        {
            if (Instance == null) Ensure();
            if (Instance == null) return;
            float z = 1f;
            if (combo >= 20) z = 1.25f;
            else if (combo >= 15) z = 1.15f;
            else if (combo >= 10) z = 1.10f;
            else if (combo >= 5) z = 1.05f;
            Instance.targetZoom = z;
        }

        // ---------------- Implementierung ----------------

        void DoShake(float pixels, float duration)
        {
            // stärkerer Shake überschreibt einen laufenden schwächeren
            float amp = pixels * pixelToWorld;
            if (amp >= shakeAmplitude || shakeTimer <= 0f)
            {
                shakeAmplitude = amp;
                shakeDuration = Mathf.Max(0.01f, duration);
                shakeTimer = shakeDuration;
            }
        }

        void DoHitStop(float seconds, float scale)
        {
            if (hitStopRoutine != null) StopCoroutine(hitStopRoutine);
            hitStopRoutine = StartCoroutine(HitStopRoutine(seconds, scale));
        }

        IEnumerator HitStopRoutine(float seconds, float scale)
        {
            float prev = Time.timeScale;
            Time.timeScale = scale;
            Time.fixedDeltaTime = 0.02f * Mathf.Max(0.01f, scale);
            yield return new WaitForSecondsRealtime(seconds);
            Time.timeScale = Mathf.Approximately(prev, 0f) ? 1f : 1f;
            Time.fixedDeltaTime = 0.02f;
            hitStopRoutine = null;
        }

        void DoFrameFreeze(int frames)
        {
            StartCoroutine(FreezeRoutine(frames));
        }

        IEnumerator FreezeRoutine(int frames)
        {
            float prev = Time.timeScale;
            Time.timeScale = 0f;
            for (int i = 0; i < Mathf.Max(1, frames); i++)
                yield return new WaitForEndOfFrame();
            Time.timeScale = Mathf.Approximately(prev, 0f) ? 1f : prev;
        }

        void LateUpdate()
        {
            // --- Shake (unskaliert, damit er auch im Hitstop läuft) ---
            if (shakeTimer > 0f)
            {
                shakeTimer -= Time.unscaledDeltaTime;
                float falloff = Mathf.Clamp01(shakeTimer / shakeDuration);
                Vector3 offset = Random.insideUnitSphere * (shakeAmplitude * falloff);
                offset.z *= 0.3f;
                transform.position += offset;
                if (shakeTimer <= 0f) shakeAmplitude = 0f;
            }

            // --- Combo-Zoom über FOV (0,3 s weich) ---
            if (cam != null)
            {
                currentZoom = Mathf.Lerp(currentZoom, targetZoom, zoomLerpSpeed * Time.unscaledDeltaTime);
                cam.fieldOfView = baseFov / Mathf.Max(0.5f, currentZoom);
            }
        }

        /// <summary>Setzt Zoom/Shake bei Rundenende zurück.</summary>
        public void ResetAll()
        {
            targetZoom = 1f;
            shakeTimer = 0f;
            shakeAmplitude = 0f;
            Time.timeScale = 1f;
        }
    }
}
