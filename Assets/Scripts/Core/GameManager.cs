using System.Collections;
using UnityEngine;
using PennerKombat;

namespace PennerKombat
{
    /// <summary>
    /// Steuert den Ablauf eines Matches: Best-of-N-Runden, Timer, Spawning,
    /// Rundenende, Match-Ende. Bietet öffentliche Einstiegspunkte für
    /// Versus, Story und (später) Netzwerk an.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance;

        [Header("Match Settings")]
        public int bestOfRounds = GameConstants.DefaultBestOfRounds;
        public float roundTime = GameConstants.DefaultRoundTime;

        [Header("Roster")]
        public FighterDatabase database;
        public Transform spawnPoint1;
        public Transform spawnPoint2;

        [Header("References")]
        public UIManager uiManager;
        public AudioManager audioManager;
        public CameraController cameraController;
        public ArenaManager arenaManager;

        private FighterController player1;
        private FighterController player2;
        private int player1Wins;
        private int player2Wins;
        private int currentRound = 1;
        private float timer;
        private bool roundActive;
        private bool matchEnded;
        private bool isStoryMatch;
        private string storyOpponentId;

        // Events
        public event System.Action<string> OnMatchEnded;   // Gewinner-Name
        public event System.Action<FighterController, FighterController> OnFightersSpawned;

        void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
            DontDestroyOnLoad(gameObject);
        }

        void Start()
        {
            EnsureDependencies();

            // Standard: Le Binde (Spieler) vs. Mojo Bob (KI) für schnellen Test.
            if (player1 == null && !matchEnded)
                StartVersusFight(0, 2);
        }

        /// <summary>
        /// Sorgt dafür, dass ein Match auch dann startet, wenn in der Szene
        /// nichts von Hand verdrahtet wurde: Datenbank, Spawn-Punkte, HUD,
        /// Kamera, Arena und Audio werden bei Bedarf selbst erzeugt.
        /// (docs/SPIELEN.md)
        /// </summary>
        public void EnsureDependencies()
        {
            if (database == null)
            {
                database = Resources.Load<FighterDatabase>("FighterDatabase");
                if (database == null)
                {
                    database = ScriptableObject.CreateInstance<FighterDatabase>();
                    Debug.Log("[Penner Kombat] Keine FighterDatabase gefunden — Standard-Roster wird zur Laufzeit erzeugt.");
                }
            }
            if (database.fighters.Count == 0) database.EnsureDefaultRoster();

            if (spawnPoint1 == null)
            {
                var go = new GameObject("SpawnPoint1");
                go.transform.position = new Vector3(-4f, 0.1f, 0f);
                go.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                spawnPoint1 = go.transform;
            }
            if (spawnPoint2 == null)
            {
                var go = new GameObject("SpawnPoint2");
                go.transform.position = new Vector3(4f, 0.1f, 0f);
                go.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
                spawnPoint2 = go.transform;
            }

            if (uiManager == null) uiManager = UIManager.Instance != null ? UIManager.Instance : HudBuilder.Ensure();
            if (cameraController == null) cameraController = CameraController.Instance;
            if (arenaManager == null) arenaManager = ArenaManager.Instance;
            if (audioManager == null) audioManager = AudioManager.Instance;
        }

        // ===== Öffentliche Einstiegspunkte =====

        public void StartVersusFight(int p1Index, int p2Index)
        {
            isStoryMatch = false;
            currentRound = 1;
            player1Wins = 0;
            player2Wins = 0;
            matchEnded = false;
            SpawnFighters(p1Index, p2Index, isAI: false);
            StartNewRound();
        }

        public void StartStoryFight(string opponentId, string arenaId, int difficulty, bool isBoss)
        {
            isStoryMatch = true;
            storyOpponentId = opponentId;
            currentRound = 1;
            player1Wins = 0;
            player2Wins = 0;
            matchEnded = false;

            int playerChar = 0; // Story: Spieler spielt Le Binde (konfigurierbar)
            int oppIndex = database != null ? database.GetIndexById(opponentId) : 0;
            SpawnFighters(playerChar, oppIndex, isAI: true);
            StartNewRound();
        }

        public void StartMatch(int opponentIndex) // Rückwärtskompatibler Alias
            => StartStoryFight(database != null ? database.GetFighter(opponentIndex).id : "le_binde", "", 1, false);

        // ===== Spawning =====

        void SpawnFighters(int p1Idx, int p2Idx, bool isAI)
        {
            EnsureDependencies();
            if (database == null || database.fighters.Count == 0)
            {
                Debug.LogError("GameManager: keine FighterDatabase konfiguriert.");
                return;
            }
            var d1 = database.GetFighter(p1Idx);
            var d2 = database.GetFighter(p2Idx);
            if (d1 == null || d2 == null) { Debug.LogError("GameManager: ungültiger Charakter-Index."); return; }

            if (player1 != null) Destroy(player1.gameObject);
            if (player2 != null) Destroy(player2.gameObject);

            GameObject p1Obj = SpawnOne(d1, spawnPoint1);
            player1 = p1Obj.GetComponent<FighterController>();
            player1.playerIndex = 0;
            player1.isAI = false;

            GameObject p2Obj = SpawnOne(d2, spawnPoint2);
            player2 = p2Obj.GetComponent<FighterController>();
            player2.playerIndex = 1;
            player2.isAI = isAI;

            if (uiManager != null) uiManager.SetNames(player1.displayName, player2.displayName);

            // Zugewiesene GLB-Modelle nachladen (docs/MODELLE.md) — asynchron,
            // der Kampf startet sofort mit der Kapsel.
            GlbModelLoader.ApplyTo(player1);
            GlbModelLoader.ApplyTo(player2);

            // Events verdrahten
            player1.OnDeath += OnFighterDeath;
            player2.OnDeath += OnFighterDeath;
            player1.OnDamageDealt += (a, t, dmg) => UIManager.NotifyDamage(a, t, dmg);
            player2.OnDamageDealt += (a, t, dmg) => UIManager.NotifyDamage(a, t, dmg);

            // AI anhängen, falls KI-gesteuert
            if (player2.isAI && player2.GetComponent<AIController>() == null)
                player2.gameObject.AddComponent<AIController>();

            if (cameraController != null)
            {
                cameraController.target1 = player1.transform;
                cameraController.target2 = player2.transform;
            }

            OnFightersSpawned?.Invoke(player1, player2);
        }

        /// <summary>
        /// Spawnt einen Kämpfer: echtes Prefab, falls hinterlegt — sonst den
        /// prozeduralen Platzhalter aus <see cref="FighterFactory"/>.
        /// </summary>
        GameObject SpawnOne(FighterConfig cfg, Transform spawn)
        {
            if (cfg.prefab != null)
            {
                var go = Instantiate(cfg.prefab, spawn.position, spawn.rotation);
                if (go.GetComponent<FighterController>() == null)
                {
                    Debug.LogWarning($"[Penner Kombat] Prefab von '{cfg.id}' hat keinen FighterController — "
                                   + "es wird stattdessen ein Platzhalter erzeugt.");
                    Destroy(go);
                }
                else return go;
            }
            return FighterFactory.CreatePlaceholder(cfg, spawn.position, spawn.rotation);
        }

        void StartNewRound()
        {
            if (matchEnded) return;

            if (player1 == null || player2 == null) return;

            player1.ResetForRound();
            player2.ResetForRound();
            player1.transform.position = spawnPoint1.position;
            player2.transform.position = spawnPoint2.position;
            player1.transform.rotation = spawnPoint1.rotation;
            player2.transform.rotation = spawnPoint2.rotation;

            timer = roundTime;
            roundActive = true;

            if (uiManager != null)
            {
                uiManager.UpdateRoundDisplay(currentRound, bestOfRounds);
                uiManager.UpdateTimer(timer);
                uiManager.ResetCombo();
            }

            if (arenaManager != null) arenaManager.ResetArena();
            if (audioManager != null) audioManager.PlayBattleMusic(currentRound - 1);
        }

        void Update()
        {
            if (!roundActive || matchEnded) return;

            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                timer = 0f;
                RoundEnd();
            }

            if (uiManager != null)
            {
                uiManager.UpdateTimer(timer);
                uiManager.UpdateHealth(player1.currentHP / player1.maxHP, player2.currentHP / player2.maxHP);
                uiManager.UpdateCombo(player1.comboCount, player2.comboCount);
                uiManager.UpdateFatalBlow(player1.fatalBlowMeter / GameConstants.FatalBlowMeterMax,
                                          player2.fatalBlowMeter / GameConstants.FatalBlowMeterMax);
            }
        }

        void RoundEnd()
        {
            if (!roundActive) return;
            roundActive = false;

            if (player1.currentHP > player2.currentHP) player1Wins++;
            else if (player2.currentHP > player1.currentHP) player2Wins++;
            else
            {
                // Unentschieden -> nächste Runde ohne Punkt
                currentRound++;
                if (currentRound <= bestOfRounds) { Invoke(nameof(StartNewRound), GameConstants.RoundEndDelay); }
                else { MatchEnd("Unentschieden"); }
                return;
            }

            int needed = Mathf.CeilToInt(bestOfRounds / 2f);
            if (player1Wins >= needed || player2Wins >= needed)
            {
                string winner = player1Wins >= needed ? player1.displayName : player2.displayName;
                MatchEnd(winner);
            }
            else
            {
                currentRound++;
                Invoke(nameof(StartNewRound), GameConstants.RoundEndDelay);
            }
        }

        /// <summary>Arena und Extras für die nächste Runde säubern.</summary>
        void ResetExtras()
        {
            ArenaDestruction.Instance?.ResetArena();
            PowerUpSystem.Instance?.ClearAll();
            MusicSync.Instance?.ResetTempo();
        }

        void MatchEnd(string winner)
        {
            matchEnded = true;

            // Statistik persistieren (docs: Assets/Scripts/Utils/SaveSystem.cs)
            if (SaveSystem.Instance != null && player1 != null && player2 != null)
            {
                SaveSystem.Instance.RecordMatch(
                    winner == player1.displayName, player1.fighterId, player2.fighterId);
                SaveSystem.Instance.RecordCombo(Mathf.Max(player1.comboCount, player2.comboCount));
            }

            ResetExtras();
            if (uiManager != null) uiManager.ShowMatchResult(winner);
            if (audioManager != null) audioManager.PlayVictoryMusic();
            OnMatchEnded?.Invoke(winner);

            if (isStoryMatch && StoryManager.Instance != null)
            {
                bool won = winner == player1.displayName;
                StoryManager.Instance.OnFightResult(won);
            }
        }

        void OnFighterDeath(FighterController dead)
        {
            if (roundActive) RoundEnd();
        }

        public void RestartMatch() => StartVersusFight(0, 2);
    }
}
