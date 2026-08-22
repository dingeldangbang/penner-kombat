using UnityEngine;

namespace PennerKombat
{
    /// <summary>
    /// 3D-Kampfkamera mit vier Modi (docs/3D.md §2):
    /// **Dynamic** (Standard: hält beide Kämpfer im Bild, zoomt mit dem Abstand),
    /// **Follow** (über der Schulter, für Story/Solo),
    /// **TopDown** (Vogelperspektive) und
    /// **Cinematic** (Fatality, X-Ray, Mops-Kommando).
    ///
    /// Der Blickwinkel ist frei wählbar (<see cref="baseYaw"/>, <see cref="pitch"/>),
    /// damit die Bewegung kamerarelativ bleibt — siehe <see cref="CameraRelative"/>.
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        public static CameraController Instance;

        public enum CameraMode { Dynamic, Follow, TopDown, Cinematic }

        [Header("Modus")]
        public CameraMode mode = CameraMode.Dynamic;

        [Header("Ziele")]
        public Transform target1;
        public Transform target2;

        [Header("Allgemein")]
        public float smoothSpeed = 6f;
        public float baseYaw = 0f;        // Drehung um die Hochachse (0 = Blick von +Z)
        public float pitch = 30f;         // Neigung in Grad
        public float lookHeight = 1.2f;   // Blickpunkt über dem Boden

        [Header("Dynamic")]
        public float minHeight = 8f;
        public float maxHeight = 20f;
        public float baseDistance = 10f;
        public float distanceFactor = 0.5f;
        public float dynamicSmooth = 0.12f;

        [Header("Follow (über der Schulter)")]
        public float followDistance = 6f;
        public float followHeight = 2.6f;
        public float followSideOffset = 0.8f;

        [Header("TopDown")]
        public float topDownHeight = 14f;
        public float topDownPadding = 4f;

        [Header("Cinematic")]
        public bool cinematicMode;
        public Transform cinematicTarget;
        public float cinematicDistance = 4f;
        public AnimationCurve cinematicCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private float currentHeight;
        private float currentDistance;

        // Kamerafahrt
        private bool travelling;
        private float travelTimer, travelDuration;
        private Vector3 travelFrom, travelTo;
        private Quaternion travelFromRot, travelToRot;

        void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(this); return; }

            currentHeight = minHeight;
            currentDistance = baseDistance;
        }

        void LateUpdate()
        {
            if (travelling) { UpdateTravel(); return; }

            switch (mode)
            {
                case CameraMode.Cinematic: UpdateCinematic(); break;
                case CameraMode.Follow:    UpdateFollow();    break;
                case CameraMode.TopDown:   UpdateTopDown();   break;
                default:                   UpdateDynamic();   break;
            }
        }

        // ==================================================================
        //  Modi
        // ==================================================================

        void UpdateDynamic()
        {
            if (target1 == null || target2 == null) return;

            Vector3 mid = (target1.position + target2.position) * 0.5f;
            float spread = Vector3.Distance(target1.position, target2.position);

            float wantHeight = Mathf.Clamp(minHeight + spread * 0.3f, minHeight, maxHeight);
            float wantDistance = baseDistance + spread * distanceFactor;

            currentHeight = Mathf.Lerp(currentHeight, wantHeight, dynamicSmooth);
            currentDistance = Mathf.Lerp(currentDistance, wantDistance, dynamicSmooth);

            Vector3 dir = Quaternion.Euler(0f, baseYaw, 0f) * Vector3.forward;
            Vector3 desired = mid - dir * currentDistance + Vector3.up * currentHeight;

            transform.position = Vector3.Lerp(transform.position, desired, smoothSpeed * Time.deltaTime);
            transform.LookAt(mid + Vector3.up * lookHeight);
        }

        void UpdateFollow()
        {
            if (target1 == null) return;

            Vector3 back = -target1.forward * followDistance;
            Vector3 side = target1.right * followSideOffset;
            Vector3 desired = target1.position + back + side + Vector3.up * followHeight;

            transform.position = Vector3.Lerp(transform.position, desired, smoothSpeed * Time.deltaTime);

            Vector3 look = target2 != null
                ? Vector3.Lerp(target1.position, target2.position, 0.65f)
                : target1.position + target1.forward * 3f;
            transform.LookAt(look + Vector3.up * lookHeight);
        }

        void UpdateTopDown()
        {
            if (target1 == null) return;

            Vector3 mid = target2 != null
                ? (target1.position + target2.position) * 0.5f
                : target1.position;
            float spread = target2 != null ? Vector3.Distance(target1.position, target2.position) : 0f;

            float h = topDownHeight + spread * 0.5f + topDownPadding;
            Vector3 desired = mid + Vector3.up * h;

            transform.position = Vector3.Lerp(transform.position, desired, smoothSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.Euler(90f, baseYaw, 0f), smoothSpeed * Time.deltaTime);
        }

        void UpdateCinematic()
        {
            if (cinematicTarget == null) return;
            Vector3 dir = (cinematicTarget.position - transform.position).normalized;
            Vector3 desired = cinematicTarget.position - dir * cinematicDistance + Vector3.up * 1.5f;
            transform.position = Vector3.Lerp(transform.position, desired, smoothSpeed * Time.deltaTime);
            transform.LookAt(cinematicTarget.position + Vector3.up * lookHeight);
        }

        // ==================================================================
        //  API
        // ==================================================================

        public void SetMode(CameraMode newMode)
        {
            if (mode == CameraMode.Cinematic && newMode != CameraMode.Cinematic) cinematicMode = false;
            mode = newMode;
        }

        public void SetTargets(Transform t1, Transform t2)
        {
            target1 = t1;
            target2 = t2;
        }

        /// <summary>Kompatibel zur bisherigen API: Cinematic ein/aus.</summary>
        public void SetCinematic(Transform target, bool active)
        {
            cinematicMode = active;
            cinematicTarget = target;
            mode = active ? CameraMode.Cinematic : CameraMode.Dynamic;
        }

        /// <summary>Fahrt von A nach B (Fatality-Inszenierung, Rundenintro).</summary>
        public void Travel(Vector3 fromPos, Quaternion fromRot, Vector3 toPos, Quaternion toRot, float duration = 2f)
        {
            travelling = true;
            travelTimer = 0f;
            travelDuration = Mathf.Max(0.01f, duration);
            travelFrom = fromPos; travelTo = toPos;
            travelFromRot = fromRot; travelToRot = toRot;
            transform.SetPositionAndRotation(fromPos, fromRot);
        }

        void UpdateTravel()
        {
            travelTimer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(travelTimer / travelDuration);
            float k = cinematicCurve != null ? cinematicCurve.Evaluate(t) : t;

            transform.position = Vector3.Lerp(travelFrom, travelTo, k);
            transform.rotation = Quaternion.Slerp(travelFromRot, travelToRot, k);

            if (t >= 1f)
            {
                travelling = false;
                if (mode == CameraMode.Cinematic) mode = CameraMode.Dynamic;
            }
        }

        /// <summary>
        /// Rechnet eine Eingabe (x = seitlich, y = vor/zurück) in eine
        /// Weltrichtung relativ zur Kamera um — Grundlage der 360°-Bewegung.
        /// </summary>
        public Vector3 CameraRelative(Vector2 input)
        {
            Vector3 forward = transform.forward;
            Vector3 right = transform.right;
            forward.y = 0f; right.y = 0f;
            forward.Normalize(); right.Normalize();
            Vector3 dir = forward * input.y + right * input.x;
            return dir.sqrMagnitude > 1f ? dir.normalized : dir;
        }

        /// <summary>Statischer Helfer: fällt auf Weltachsen zurück, wenn keine Kamera da ist.</summary>
        public static Vector3 ToWorld(Vector2 input)
        {
            if (Instance != null) return Instance.CameraRelative(input);
            var cam = Camera.main;
            if (cam == null) return new Vector3(input.x, 0f, input.y);

            Vector3 f = cam.transform.forward; f.y = 0f; f.Normalize();
            Vector3 r = cam.transform.right;   r.y = 0f; r.Normalize();
            return f * input.y + r * input.x;
        }
    }
}
