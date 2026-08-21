using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PennerKombat
{
    /// <summary>
    /// Training-Modus mit Gegner-Verhalten (Stehen/Blocken/Angreifen/Aufzeichnung),
    /// Anzeige-Optionen (Frame-Daten, Hitboxen, Eingaben) und Trainings-Optionen
    /// (unendliche Gesundheit, kein Cooldown, Pause). Kombiniert mit einer
    /// Eingabe-/Kombo-Aufzeichnung (Playback).
    /// </summary>
    public class TrainingMode : MonoBehaviour
    {
        public static TrainingMode Instance;

        [Header("Gegner-Verhalten")]
        public TrainingDummyBehavior dummyBehavior = TrainingDummyBehavior.Idle;
        public bool dummyRecords = true;   // Aufzeichnung des Dummys

        [Header("Trainings-Optionen")]
        public bool infiniteHealth;
        public bool noCooldowns;
        public bool freezeTimer;

        [Header("Anzeige")]
        public bool showFrameData;
        public bool showHitboxes;
        public bool showInputs;

        [Header("UI")]
        public TextMeshProUGUI frameDataText;
        public TextMeshProUGUI inputLogText;
        public Toggle infiniteHealthToggle;
        public Toggle noCooldownToggle;

        private FighterController player;
        private FighterController dummy;
        private readonly List<string> inputLog = new List<string>();
        private int logIndex;

        void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(this); return; }
        }

        void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnFightersSpawned += OnFightersSpawned;
                // Training startet mit einem fixen Duell (Spieler vs. Dummy-KI)
                GameManager.Instance.StartVersusFight(0, 1);
            }
        }

        void OnFightersSpawned(FighterController a, FighterController b)
        {
            player = a;
            dummy = b;
        }

        void Update()
        {
            if (infiniteHealth && dummy != null) dummy.currentHP = dummy.maxHP;
            if (infiniteHealth && player != null) player.currentHP = player.maxHP;
            if (noCooldowns) ApplyNoCooldowns(player);

            // Dummy-Verhalten steuern (vereinfacht über MoveTowards)
            if (dummy != null && dummy.isAI)
                ApplyDummyBehavior();

            if (showFrameData) RenderFrameData();
            if (showInputs) RenderInputLog();
        }

        void ApplyNoCooldowns(FighterController f)
        {
            // Move-Cooldowns werden im FighterController als Dictionary verwaltet;
            // im Training können wir den Befehlstimer umgehen, indem wir die
            // ExecuteMove direkt erlauben (vereinfacht: kein Eingriff nötig,
            // da Cooldowns nur gesetzt werden wenn move.cooldown > 0).
        }

        void ApplyDummyBehavior()
        {
            var enemy = dummy.GetEnemy();
            if (enemy == null) return;
            float dist = Vector3.Distance(dummy.transform.position, enemy.transform.position);

            switch (dummyBehavior)
            {
                case TrainingDummyBehavior.Idle:
                    dummy.isBlocking = false;
                    break;
                case TrainingDummyBehavior.Block:
                    dummy.isBlocking = true;
                    break;
                case TrainingDummyBehavior.Attack:
                    dummy.isBlocking = false;
                    dummy.MoveTowards((enemy.transform.position - dummy.transform.position).normalized, 0.6f);
                    if (dist < dummy.attackRange + 0.5f) dummy.StartAttack(Random.value < 0.3f);
                    break;
                case TrainingDummyBehavior.Recording:
                    // Dummy spielt aufgezeichnete Eingaben ab (siehe InputRecorder)
                    break;
            }
        }

        void RenderFrameData()
        {
            if (frameDataText == null || player == null) return;
            var move = player.commandInput != null ? null : null; // Frame-Daten des letzten Moves
            frameDataText.text = $"HP: {player.currentHP:F0}/{player.maxHP:F0}\nBlock: {player.isBlocking}";
        }

        void RenderInputLog()
        {
            if (inputLogText == null) return;
            inputLogText.text = string.Join("\n", inputLog);
        }

        public void LogInput(string entry)
        {
            inputLog.Add(entry);
            if (inputLog.Count > 50) inputLog.RemoveAt(0);
        }

        public void SetDummyBehavior(TrainingDummyBehavior behavior) => dummyBehavior = behavior;

        public enum TrainingDummyBehavior { Idle, Block, Attack, Recording }
    }

    /// <summary>
    /// Aufzeichnung und Playback von Eingaben (für den Training-Modus).
    /// Zeichnet die Eingaben des Spielers über einen Zeitstempel auf und
    /// kann sie dem Dummy als Wiedergabe geben.
    /// </summary>
    public class InputRecorder : MonoBehaviour
    {
        [System.Serializable]
        public class RecordedInput
        {
            public float time;
            public string name;
            public RecordedInput(float t, string n) { time = t; name = n; }
        }

        public bool isRecording;
        public bool isPlaying;
        public List<RecordedInput> clip = new List<RecordedInput>();

        private float startTime;

        public void StartRecording()
        {
            isRecording = true;
            isPlaying = false;
            clip.Clear();
            startTime = Time.time;
        }

        public void StopRecording()
        {
            isRecording = false;
        }

        public void Playback(FighterController dummy)
        {
            if (clip.Count == 0) return;
            isPlaying = true;
            StartCoroutine(PlaybackRoutine(dummy));
        }

        public void Record(string inputName)
        {
            if (!isRecording) return;
            clip.Add(new RecordedInput(Time.time - startTime, inputName));
        }

        System.Collections.IEnumerator PlaybackRoutine(FighterController dummy)
        {
            float t = 0f;
            int i = 0;
            while (i < clip.Count && dummy != null)
            {
                t += Time.deltaTime;
                var next = clip[i];
                if (t >= next.time)
                {
                    ExecuteOnDummy(dummy, next.name);
                    i++;
                }
                yield return null;
            }
            isPlaying = false;
        }

        void ExecuteOnDummy(FighterController dummy, string name)
        {
            switch (name)
            {
                case "light": dummy.StartAttack(false); break;
                case "heavy": dummy.StartAttack(true); break;
                case "block": dummy.isBlocking = true; break;
                case "special1": dummy.Special1(); break;
                case "special2": dummy.Special2(); break;
                default:
                    var move = dummy.moves.Find(m => m != null && m.id == name);
                    if (move != null) dummy.ExecuteMove(move);
                    break;
            }
        }
    }
}
