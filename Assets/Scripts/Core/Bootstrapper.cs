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
        public bool createSaveSystem = true;
        [Tooltip("HUD (Lebensbalken, Timer, Combo, Ergebnis) zur Laufzeit bauen, falls keins in der Szene liegt.")]
        public bool createHud = true;
        [Tooltip("Boden + Begrenzungswände erzeugen, falls die Szene leer ist.")]
        public bool createGround = true;
        [Tooltip("Frame-Daten-Overlay bereitstellen (im Spiel mit F4 einblenden).")]
        public bool createFrameDataOverlay = true;

        [Header("Extras (docs/EXTRAS.md)")]
        public bool createMusicSync = true;
        public bool createArenaDestruction = true;
        public bool createPowerUps = true;
        public bool createCrowd = true;
        public bool createAllies = true;
        [Tooltip("Waffen (Schraubenzieher, Rohrzange …) im Hof verteilen.")]
        public bool spawnWeapons = true;

        [Header("Touch")]
        [Tooltip("On-Screen-Steuerung auf Handhelds erzeugen (Android/iOS).")]
        public bool createTouchControls = true;
        [Tooltip("Touch-Layout auch am Desktop zeigen (zum Testen).")]
        public bool touchControlsOnDesktop = false;

        [Header("Visuals & VFX (docs/VISUALS.md)")]
        public bool createVfx = true;
        public bool createComboFeedback = true;
        public bool createScreenEffects = true;
        public bool createArenaVisuals = true;

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
            if (createSaveSystem && SaveSystem.Instance == null) SaveSystem.Ensure();
            if (createGround) EnsureGround();
            if (createHud) HudBuilder.Ensure();
            if (createFrameDataOverlay) FrameDataOverlay.Ensure();
            if (createTouchControls && TouchControls.Ensure(touchControlsOnDesktop) != null)
            {
                AntiGhosting.Ensure();
                InputBuffer.Ensure();
            }
            if (createFatalBlow) gameObject.AddComponent<FatalBlowSystem>();
            if (createFatality) gameObject.AddComponent<FatalitySystem>();

            // --- Visuelle Schicht ---
            if (createVfx && VFXManager.Instance == null) VFXManager.Ensure();
            if (createScreenEffects && ScreenEffects.Instance == null) ScreenEffects.Ensure();
            if (createComboFeedback)
            {
                if (ComboSystem.Instance == null) ComboSystem.Ensure();
                if (ComboCounterUI.Instance == null) ComboCounterUI.Ensure();
            }
            if (createArenaVisuals && ArenaVisuals.Instance == null) ArenaVisuals.Ensure();
            if (createComboFeedback && HudStatusBars.Instance == null) HudStatusBars.Ensure();
            if (createVfx && SignatureFx.Instance == null) SignatureFx.Ensure();
            if (createVfx && ComboExplosion3D.Instance == null) ComboExplosion3D.Ensure();
            if (createMusicSync && MusicSync.Instance == null) MusicSync.Ensure();
            if (createArenaDestruction && ArenaDestruction.Instance == null) ArenaDestruction.Ensure();
            if (createPowerUps && PowerUpSystem.Instance == null) PowerUpSystem.Ensure();
            if (createCrowd && CrowdReactions.Instance == null) CrowdReactions.Ensure();
            if (createAllies && AllySummon.Instance == null) AllySummon.Ensure();
            if (spawnWeapons) SpawnArsenal();
            CameraShake.Ensure();
#if PK_URP
            UrpPostProcessingDriver.Ensure();
#endif
        }

        /// <summary>
        /// Legt Boden und Begrenzungswände an, falls die Szene noch keinen
        /// Boden hat — sonst fallen die Kämpfer beim Start ins Nichts.
        /// </summary>
        void EnsureGround()
        {
            if (GameObject.Find("~Ground") != null) return;
            if (Physics.Raycast(new Vector3(0f, 25f, 0f), Vector3.down, 60f)) return;

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "~Ground";
            ground.transform.localScale = new Vector3(3f, 1f, 3f);   // 30 x 30 Meter
            try { ground.tag = GameConstants.TagGround; } catch (UnityException) { }
            var groundRenderer = ground.GetComponent<MeshRenderer>();
            if (groundRenderer != null)
            {
                var mat = new Material(FighterFactory.DefaultShader());
                Color asphalt = Color.Lerp(PennerPalette.NightBlue, Color.black, 0.35f);
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", asphalt);
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", asphalt);
                groundRenderer.sharedMaterial = mat;
            }

            var walls = new GameObject("~Walls");
            float half = 13f;
            (Vector3 pos, Vector3 scale)[] defs =
            {
                (new Vector3(0f, 2f,  half), new Vector3(half * 2f, 4f, 0.6f)),
                (new Vector3(0f, 2f, -half), new Vector3(half * 2f, 4f, 0.6f)),
                (new Vector3( half, 2f, 0f), new Vector3(0.6f, 4f, half * 2f)),
                (new Vector3(-half, 2f, 0f), new Vector3(0.6f, 4f, half * 2f))
            };
            foreach (var (wPos, wScale) in defs)
            {
                var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.name = "Wall";
                wall.transform.SetParent(walls.transform, false);
                wall.transform.position = wPos;
                wall.transform.localScale = wScale;
                var wr = wall.GetComponent<MeshRenderer>();
                if (wr != null)
                {
                    var wm = new Material(FighterFactory.DefaultShader());
                    Color brick = Color.Lerp(PennerPalette.Earth, Color.black, 0.45f);
                    if (wm.HasProperty("_BaseColor")) wm.SetColor("_BaseColor", brick);
                    if (wm.HasProperty("_Color")) wm.SetColor("_Color", brick);
                    wr.sharedMaterial = wm;
                }
            }
        }

        /// <summary>Verteilt das Standard-Arsenal im Hinterhof.</summary>
        void SpawnArsenal()
        {
            var arsenal = WeaponPickup.Arsenal;
            for (int i = 0; i < arsenal.Length; i++)
            {
                float angle = (360f / arsenal.Length) * i * Mathf.Deg2Rad;
                Vector3 pos = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 6.5f;
                WeaponPickup.Spawn(arsenal[i], pos);
            }
        }

        void Start()
        {
            // Gespeicherte Optionen (Lautstärke, Qualität, VFX-Regler) anwenden
            SaveSystem.Instance?.ApplyOptions();

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
