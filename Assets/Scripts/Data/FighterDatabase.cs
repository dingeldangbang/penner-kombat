using System.Collections.Generic;
using UnityEngine;

namespace PennerKombat
{
    /// <summary>
    /// Zentrale Datenbank für alle 9 Charaktere (ScriptableObject).
    /// Bietet Nachschlagen nach ID und Index sowie das Anlegen der
    /// Standard-Roster-Beispiele zur Laufzeit (für den Editor/Test).
    /// </summary>
    [CreateAssetMenu(fileName = "FighterDatabase", menuName = "PennerKombat/FighterDatabase")]
    public class FighterDatabase : ScriptableObject
    {
        public List<FighterConfig> fighters = new List<FighterConfig>();

        public FighterConfig GetFighter(string id)
            => fighters.Find(f => f != null && f.id == id);

        public FighterConfig GetFighter(int index)
            => index >= 0 && index < fighters.Count ? fighters[index] : null;

        public int GetIndexById(string id)
        {
            for (int i = 0; i < fighters.Count; i++)
                if (fighters[i] != null && fighters[i].id == id) return i;
            return 0;
        }

        public string GetName(string id)
        {
            var f = GetFighter(id);
            return f != null ? f.displayName : id;
        }

        /// <summary>
        /// Falls im Editor keine Datenbank befüllt wurde, legt diese Methode
        /// Dummy-Einträge für die 9 Charaktere an (nutzt die eingebauten
        /// Defaults aus GameConstants). Aufruf z.B. in einem Editor-Skript
        /// oder im GameManager-Start, wenn database.fighters.Count == 0.
        /// </summary>
        public void EnsureDefaultRoster()
        {
            if (fighters.Count >= GameConstants.AllCharacterIds.Length) return;

            var defaults = new[]
            {
                (GameConstants.CharLeBinde, "Le Binde", 130f, 4.2f, 11f, 18f, 2.8f, Stature.Adipoes),
                (GameConstants.CharMell, "Mell", 85f, 6.5f, 7f, 12f, 2.0f, Stature.Hager),
                (GameConstants.CharMojoBob, "Mojo Bob", 100f, 4.8f, 9f, 14f, 3.5f, Stature.Normal),
                (GameConstants.CharDieter, "Dieter", 110f, 4.5f, 10f, 16f, 2.5f, Stature.Breit),
                (GameConstants.CharUschi, "Uschi", 95f, 5.2f, 8f, 13f, 2.2f, Stature.Normal),
                (GameConstants.CharTetraPak, "TetraPak", 115f, 4.0f, 9f, 15f, 2.7f, Stature.Breit),
                (GameConstants.CharSigi, "Sigi", 90f, 5.5f, 7f, 11f, 2.3f, Stature.Hager),
                (GameConstants.CharRolf, "Rolf", 105f, 4.7f, 8f, 14f, 2.6f, Stature.Normal),
                (GameConstants.CharKalle, "Kalle", 120f, 4.3f, 10f, 17f, 2.4f, Stature.Breit),
            };

            fighters.Clear();
            foreach (var d in defaults)
            {
                var cfg = ScriptableObject.CreateInstance<FighterConfig>();
                cfg.id = d.Item1;
                cfg.displayName = d.Item2;
                cfg.maxHP = d.Item3;
                cfg.moveSpeed = d.Item4;
                cfg.lightDamage = d.Item5;
                cfg.heavyDamage = d.Item6;
                cfg.attackRange = d.Item7;
                cfg.stature = d.Item8;
                fighters.Add(cfg);
            }
        }
    }
}
