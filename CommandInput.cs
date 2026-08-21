using System.Collections.Generic;
using UnityEngine;

namespace PennerKombat
{
    /// <summary>
    /// Erkennt Spezialeingaben (Richtungs-Sequenzen + Button) für einen Kämpfer.
    /// Puffert die letzten Richtungswechsel und prüft sie gegen die MoveData-Liste
    /// des Besitzers. Der Besitzer (FighterController) ruft pro Frame ProcessInput().
    /// </summary>
    [RequireComponent(typeof(FighterController))]
    public class CommandInput : MonoBehaviour
    {
        [Header("Buffer")]
        public int maxBuffer = 10;          // max. Richtungswechsel im Speicher
        public float directionWindow = 0.45f; // Sekunden, in denen die Sequenz eingegeben werden muss

        private FighterController owner;
        private readonly List<int> dirBuffer = new List<int>(); // Numpad-Sequenz
        private int lastDir = 5;
        private float lastDirTime = -1f;

        void Awake()
        {
            owner = GetComponent<FighterController>();
        }

        /// <summary>Pro Frame aufrufen. Gibt die aktuell erkannte Bewegung zurück (oder null).</summary>
        public MoveData ProcessInput()
        {
            if (owner == null || FighterInput.Instance == null) return null;

            int dir = ReadDirection();
            UpdateBuffer(dir);

            var enemy = owner.GetEnemy();
            if (enemy == null) return null;

            foreach (var move in owner.moves)
            {
                if (move == null) continue;
                if (!move.Matches(dirBuffer)) continue;
                if (!IsButtonPressed(move.button, owner.playerIndex)) continue;
                return move;
            }
            return null;
        }

        int ReadDirection()
        {
            Vector3 input = FighterInput.Instance.GetMoveDirection(owner.playerIndex);
            if (input.sqrMagnitude < 0.0001f) return 5;

            Vector3 flat = new Vector3(input.x, 0f, input.z).normalized;
            var enemy = owner.GetEnemy();
            Vector3 toEnemy = Vector3.forward;
            if (enemy != null)
            {
                Vector3 diff = enemy.transform.position - transform.position;
                diff.y = 0f;
                if (diff.sqrMagnitude > 0.0001f) toEnemy = diff.normalized;
            }
            return DirectionUtil.ToNumpad(flat, toEnemy);
        }

        void UpdateBuffer(int dir)
        {
            if (dir == 5)
            {
                // Neutral unterbricht die Sequenz
                if (dirBuffer.Count > 0) dirBuffer.Clear();
                lastDir = 5;
                return;
            }

            float now = Time.time;
            if (dir != lastDir)
            {
                // Sequenz verwerfen, wenn zu lange ohne neue Eingabe
                if (lastDirTime >= 0f && (now - lastDirTime) > directionWindow)
                    dirBuffer.Clear();
                dirBuffer.Add(dir);
                if (dirBuffer.Count > maxBuffer)
                    dirBuffer.RemoveAt(0);
                lastDir = dir;
                lastDirTime = now;
            }
            // Wiederholung desselben Dirs (z.B. →→) wird über Time-Window erfasst
            else if (lastDirTime >= 0f && (now - lastDirTime) < 0.12f)
            {
                dirBuffer.Add(dir);
                if (dirBuffer.Count > maxBuffer)
                    dirBuffer.RemoveAt(0);
                lastDirTime = now;
            }
        }

        bool IsButtonPressed(MoveButton button, int playerIndex)
        {
            var input = FighterInput.Instance;
            switch (button)
            {
                case MoveButton.Light: return input.GetLightAttack(playerIndex);
                case MoveButton.Heavy: return input.GetHeavyAttack(playerIndex);
                case MoveButton.SpecialA: return input.GetSpecial1(playerIndex);
                case MoveButton.SpecialB: return input.GetSpecial2(playerIndex);
                default: return false;
            }
        }
    }
}
