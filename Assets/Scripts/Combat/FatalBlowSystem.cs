using System.Collections;
using UnityEngine;

namespace PennerKombat
{
    /// <summary>
    /// Überwacht beide Kämpfer auf den X-Ray-/Fatal-Blow-Input (Taste Y bzw.
    /// West+East auf dem Gamepad). Wenn der eigene Kämpfer eine volle
    /// Fatal-Blow-Leiste hat und der Gegner in Reichweite ist, wird der
    /// Fatal Blow ausgeführt (Cinematic + hoher Schaden).
    /// </summary>
    public class FatalBlowSystem : MonoBehaviour
    {
        [Header("Settings")]
        public float fatalBlowRange = 4f;
        public float cinematicTime = 1.6f;

        private FighterController p1;
        private FighterController p2;
        private bool cinematicPlaying;

        void Start()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnFightersSpawned += OnFightersSpawned;
        }

        void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnFightersSpawned -= OnFightersSpawned;
        }

        void OnFightersSpawned(FighterController a, FighterController b)
        {
            p1 = a;
            p2 = b;
        }

        void Update()
        {
            if (cinematicPlaying || p1 == null || p2 == null) return;

            if (FighterInput.Instance != null)
            {
                if (FighterInput.Instance.GetFatalBlow(0))
                    TryFatalBlow(p1, p2);
                if (FighterInput.Instance.GetFatalBlow(1))
                    TryFatalBlow(p2, p1);
            }
        }

        void TryFatalBlow(FighterController attacker, FighterController target)
        {
            if (!attacker.fatalBlowReady) return;
            if (target == null || !target.gameObject.activeSelf) return;
            if (Vector3.Distance(attacker.transform.position, target.transform.position) > fatalBlowRange) return;

            if (attacker.ExecuteFatalBlow(target))
                StartCoroutine(PlayFatalBlowCinematic(attacker, target));
        }

        IEnumerator PlayFatalBlowCinematic(FighterController attacker, FighterController target)
        {
            cinematicPlaying = true;
            if (CameraController.Instance != null)
                CameraController.Instance.SetCinematic(attacker.transform, true);

            // Charakter-spezifischer Schaden (X-Ray) via Virtual-Methode
            float dmg = attacker is MojoBob
                ? (Random.value < 0.5f ? GameConstants.FatalBlowDamageMin : GameConstants.FatalBlowDamageMax)
                : 34f;

            yield return new WaitForSeconds(0.4f);
            target.TakeDamage(dmg, attacker.transform.forward, attacker);

            yield return new WaitForSeconds(cinematicTime);
            if (CameraController.Instance != null)
                CameraController.Instance.SetCinematic(null, false);
            cinematicPlaying = false;
        }
    }
}
