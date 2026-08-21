using UnityEngine;

namespace PennerKombat
{
    /// <summary>
    /// Stellt sicher, dass alle Singletons in einer Szene existieren, auch
    /// wenn der Entwickler vergisst, sie manuell zu platzieren. Wird in der
    /// Boot-/Arena-Szene an ein Empty-GameObject gehängt.
    /// </summary>
    public class Bootstrapper : MonoBehaviour
    {
        [Header("Automatisch erzeugen")]
        public bool createInput = true;
        public bool createAudio = true;
        public bool createCamera = true;
        public bool createArena = true;
        public bool createGameManager = true;
        public bool createFatalBlow = true;
        public bool createFatality = true;

        void Awake()
        {
            if (createInput && FighterInput.Instance == null) gameObject.AddComponent<FighterInput>();
            if (createAudio && AudioManager.Instance == null) gameObject.AddComponent<AudioManager>();
            if (createArena && ArenaManager.Instance == null) gameObject.AddComponent<ArenaManager>();
            if (createCamera && CameraController.Instance == null)
            {
                var camGo = Camera.main != null ? Camera.main.gameObject : new GameObject("Main Camera");
                if (Camera.main == null)
                {
                    camGo.tag = "MainCamera";
                    if (camGo.GetComponent<Camera>() == null) camGo.AddComponent<Camera>();
                    if (camGo.GetComponent<AudioListener>() == null) camGo.AddComponent<AudioListener>();
                }
                var cc = camGo.AddComponent<CameraController>();
                cc.transform.position = new Vector3(0f, 15f, -12f);
            }
            if (createFatalBlow) gameObject.AddComponent<FatalBlowSystem>();
            if (createFatality) gameObject.AddComponent<FatalitySystem>();
        }

        void Start()
        {
            // GameManager erst in Start, damit andere Singletons zuerst da sind
            if (createGameManager && GameManager.Instance == null)
            {
                var gm = gameObject.AddComponent<GameManager>();
                // Standard-Werte: 3 Runden, 99s
                gm.bestOfRounds = GameConstants.DefaultBestOfRounds;
                gm.roundTime = GameConstants.DefaultRoundTime;
            }
        }
    }
}
