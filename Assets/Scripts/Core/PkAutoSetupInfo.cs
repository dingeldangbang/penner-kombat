using UnityEngine;

namespace PennerKombat
{
    /// <summary>
    /// Merkzettel auf automatisch erzeugten Kämpfer-Prefabs: woher das Modell
    /// kam, wann es eingerichtet wurde und ob eigene Änderungen geschützt sind.
    ///
    /// <b>Manuelle Änderungen schützen:</b> Häkchen bei
    /// <see cref="lockManualEdits"/> setzen — dann fasst der Import-Wächter
    /// dieses Prefab nicht mehr an, egal wie oft das Modell neu importiert wird.
    ///
    /// Siehe docs/MODELLE.md
    /// </summary>
    public class PkAutoSetupInfo : MonoBehaviour
    {
        /// <summary>Version der Einrichtungslogik — steigt, wenn sich der Aufbau ändert.</summary>
        public const int CurrentVersion = 1;

        [Tooltip("Pfad des Modells, aus dem dieser Kämpfer gebaut wurde.")]
        public string sourceModel;

        [Tooltip("Charakter-ID laut GameConstants.")]
        public string fighterId;

        [Tooltip("Zeitpunkt der letzten automatischen Einrichtung.")]
        public string setupDate;

        [Tooltip("Version der Einrichtungslogik.")]
        public int setupVersion = CurrentVersion;

        [Header("Schutz")]
        [Tooltip("An: dieses Prefab wird von der Automatik nicht mehr überschrieben.")]
        public bool lockManualEdits;
    }
}
