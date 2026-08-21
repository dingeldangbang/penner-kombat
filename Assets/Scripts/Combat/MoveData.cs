using System;
using System.Collections.Generic;
using UnityEngine;

namespace PennerKombat
{
    /// <summary>Welcher Button eine Bewegung auslöst.</summary>
    public enum MoveButton { Light, Heavy, SpecialA, SpecialB, Taunt }

    /// <summary>
    /// Datenbasierte Beschreibung einer Spezialbewegung.
    /// Richtungen werden in Numpad-Notation angegeben (relativ zum Gegner):
    ///   6 = vorwärts · 4 = rückwärts · 2 = unten · 8 = oben
    ///   3 = unten-vorwärts · 1 = unten-rückwärts · 9 = oben-vorwärts · 7 = oben-rückwärts · 5 = neutral
    /// Beispiele: ↓↘→ = {2,3,6} · ↓↙← = {2,1,4} · →↘↓↙← = {6,3,2,1,4} · ←↙↓↘→ = {4,1,2,3,6}
    /// </summary>
    [Serializable]
    public class MoveData
    {
        public string id;
        public string displayName;
        public int[] dirs;          // Numpad-Sequenz (leer = nur Button)
        public MoveButton button;
        public bool isEX;

        [Header("Frame-Daten (1 Frame = 1/60 s)")]
        public int startupFrames;
        public int activeFrames;
        public int recoveryFrames;
        public float damage;        // Referenz-Schaden (Execution übernimmt exakte Werte)

        [Header("Kosten")]
        public float cooldown;
        public int mojoCost;        // Mojo Bob
        public float pulseRequirement; // Mell: minimaler Puls

        public float StartupSeconds => startupFrames / 60f;
        public float ActiveSeconds => activeFrames / 60f;
        public float RecoverySeconds => recoveryFrames / 60f;
        public float TotalSeconds => (startupFrames + activeFrames + recoveryFrames) / 60f;

        public static MoveData Make(string id, string name, int[] dirs, MoveButton btn,
            int startup, int active, int recovery, float dmg, bool ex = false,
            float cd = 0f, int mojo = 0, float pulseReq = 0f)
        {
            return new MoveData
            {
                id = id, displayName = name, dirs = dirs, button = btn,
                startupFrames = startup, activeFrames = active, recoveryFrames = recovery,
                damage = dmg, isEX = ex, cooldown = cd, mojoCost = mojo,
                pulseRequirement = pulseReq
            };
        }

        /// <summary>Prüft, ob die jüngsten eingegebenen Richtungen zur Sequenz passen.</summary>
        public bool Matches(IReadOnlyList<int> recentDirs)
        {
            if (dirs == null || dirs.Length == 0) return true;
            if (recentDirs.Count < dirs.Length) return false;
            int offset = recentDirs.Count - dirs.Length;
            for (int i = 0; i < dirs.Length; i++)
                if (recentDirs[offset + i] != dirs[i])
                    return false;
            return true;
        }

        /// <summary>Zusätzlicher optionaler Requirement-Check (Mojo/Puls), wird in ExecuteMove der Charaktere geprüft.</summary>
        public bool MeetsRequirements(MojoBob bob) => bob != null && bob.MojoPoints >= mojoCost;
    }

    /// <summary>Statische Helfer für Numpad-Konvertierung.</summary>
    public static class DirectionUtil
    {
        /// <summary>
        /// Wandelt eine horizontale Eingaberichtung (relativ zur Linie zum Gegner)
        /// in eine Numpad-Richtung (1–9) um. 0° = Richtung zum Gegner = 6.
        /// </summary>
        public static int ToNumpad(Vector3 moveFlat, Vector3 toEnemyFlat)
        {
            if (moveFlat.sqrMagnitude < 0.0001f) return 5;

            Vector3 m = moveFlat.normalized;
            Vector3 e = toEnemyFlat.sqrMagnitude > 0.0001f ? toEnemyFlat.normalized : Vector3.forward;

            // Winkel zwischen Eingabe und Richtung zum Gegner
            float dot = Mathf.Clamp(Vector3.Dot(m, e), -1f, 1f);
            Vector3 cross = Vector3.Cross(e, m);
            float angle = Mathf.Atan2(cross.y, dot) * Mathf.Rad2Deg; // -180..180, 0 = zum Gegner

            angle = ((angle + 180f) % 360f) - 180f; // normalisieren
            if (angle < -157.5f || angle >= 157.5f) return 4;          // rückwärts
            if (angle < -112.5f) return 7;                              // oben-rück
            if (angle < -67.5f) return 8;                               // oben
            if (angle < -22.5f) return 9;                               // oben-vor
            if (angle < 22.5f) return 6;                                // vorwärts
            if (angle < 67.5f) return 3;                                // unten-vor
            if (angle < 112.5f) return 2;                               // unten
            if (angle < 157.5f) return 1;                               // unten-rück
            return 4;
        }
    }
}
