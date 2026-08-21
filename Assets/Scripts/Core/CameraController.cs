using UnityEngine;

namespace PennerKombat
{
    /// <summary>
    /// Fighting-Game-Kamera: verfolgt den Mittelpunkt beider Kämpfer und
    /// zoomt ab, je weiter sie auseinanderstehen. Für die Cinematic-Cam
    /// (Fatality) lässt sich der Ziel-Modus umschalten.
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        public static CameraController Instance;

        public Transform target1;
        public Transform target2;

        void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(this); return; }
        }


        public float smoothSpeed = 6f;
        public float minHeight = 8f;
        public float maxHeight = 20f;
        public float baseDistance = 10f;
        public float distanceFactor = 0.5f;
        public float baseYaw = 0f;        // Drehung um die Hochachse (0 = Blick von +Z)
        public float pitch = 30f;         // Kameraneigung in Grad

        [Header("Cinematic (Fatal Blow / Fatality)")]
        public bool cinematicMode;
        public Transform cinematicTarget;
        public float cinematicDistance = 4f;

        void LateUpdate()
        {
            if (cinematicMode)
            {
                if (cinematicTarget == null) return;
                Vector3 dir = (cinematicTarget.position - transform.position).normalized;
                Vector3 desired = cinematicTarget.position - dir * cinematicDistance + Vector3.up * 1.5f;
                transform.position = Vector3.Lerp(transform.position, desired, smoothSpeed * Time.deltaTime);
                transform.LookAt(cinematicTarget.position + Vector3.up * 1.2f);
                return;
            }

            if (target1 == null || target2 == null) return;

            Vector3 mid = (target1.position + target2.position) / 2f;
            float dist = Vector3.Distance(target1.position, target2.position);
            float height = Mathf.Clamp(minHeight + dist * 0.3f, minHeight, maxHeight);
            float distance = baseDistance + dist * distanceFactor;

            // Kamerarichtung um baseYaw drehen, dann um pitch nach unten kippen
            Quaternion yawRot = Quaternion.Euler(0f, baseYaw, 0f);
            Vector3 forward = yawRot * Vector3.forward;
            Quaternion pitchRot = Quaternion.AngleAxis(pitch, Vector3.right);
            Vector3 lookDir = pitchRot * forward;

            Vector3 desiredPos = mid - lookDir * distance + Vector3.up * (height - distance * Mathf.Sin(pitch * Mathf.Deg2Rad));
            desiredPos.y = mid.y + height;

            transform.position = Vector3.Lerp(transform.position, desiredPos, smoothSpeed * Time.deltaTime);
            transform.LookAt(mid + Vector3.up * 1.0f);
        }

        public void SetCinematic(Transform target, bool active)
        {
            cinematicMode = active;
            cinematicTarget = target;
        }
    }
}
