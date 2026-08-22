using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace PennerKombat
{
    /// <summary>
    /// Findet GLB-/glTF-Modelle und merkt sich, welches Modell zu welchem
    /// Charakter gehört. Speicherung in PlayerPrefs (<c>pk_model_&lt;id&gt;</c>),
    /// zusätzlich Skalierung, Drehung und Höhenversatz pro Charakter.
    ///
    /// Suchreihenfolge:
    ///   1. <c>Application.persistentDataPath/Models</c>  (Handy, nachträglich befüllbar)
    ///   2. <c>Application.streamingAssetsPath/Models</c> (mit dem Build ausgeliefert)
    ///   3. <c>&lt;Projektordner&gt;/Models</c>          (nur im Editor / Desktop)
    ///
    /// Siehe docs/MODELLE.md
    /// </summary>
    public static class GlbLibrary
    {
        public const string FolderName = "Models";

        static readonly string[] Extensions = { ".glb", ".gltf" };

        // ---------- Ordner ----------

        public static string UserFolder
        {
            get
            {
                string path = Path.Combine(Application.persistentDataPath, FolderName);
                if (!Directory.Exists(path))
                {
                    try { Directory.CreateDirectory(path); } catch (Exception) { }
                }
                return path;
            }
        }

        public static IEnumerable<string> SearchFolders()
        {
            yield return UserFolder;
            yield return Path.Combine(Application.streamingAssetsPath, FolderName);
#if UNITY_EDITOR || UNITY_STANDALONE
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (!string.IsNullOrEmpty(projectRoot))
                yield return Path.Combine(projectRoot, FolderName);
#endif
        }

        /// <summary>Alle gefundenen Modelldateien (voller Pfad), alphabetisch.</summary>
        public static List<string> FindAll()
        {
            var result = new List<string>();
            foreach (string folder in SearchFolders())
            {
                if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder)) continue;
                foreach (string file in Directory.GetFiles(folder))
                {
                    string ext = Path.GetExtension(file).ToLowerInvariant();
                    if (Array.IndexOf(Extensions, ext) < 0) continue;
                    if (!result.Contains(file)) result.Add(file);
                }
            }
            result.Sort(StringComparer.OrdinalIgnoreCase);
            return result;
        }

        /// <summary>
        /// Erraten, welches Modell zu welchem Charakter gehört: Dateiname
        /// enthält die Charakter-ID (z.B. <c>le_binde.glb</c>, <c>mell_v2.glb</c>).
        /// </summary>
        public static string GuessForCharacter(string fighterId, List<string> candidates = null)
        {
            if (string.IsNullOrEmpty(fighterId)) return null;
            candidates ??= FindAll();
            string needle = fighterId.Replace("_", "").ToLowerInvariant();

            foreach (string file in candidates)
            {
                string name = Path.GetFileNameWithoutExtension(file).Replace("_", "").Replace("-", "").ToLowerInvariant();
                if (name.Contains(needle)) return file;
            }
            return null;
        }

        // ---------- Zuordnung ----------

        static string Key(string fighterId) => $"pk_model_{fighterId}";

        /// <summary>Zugewiesener Modellpfad, oder null wenn Platzhalter gewünscht.</summary>
        public static string GetAssigned(string fighterId)
        {
            if (string.IsNullOrEmpty(fighterId)) return null;
            string path = PlayerPrefs.GetString(Key(fighterId), "");
            if (string.IsNullOrEmpty(path)) return null;
            if (!File.Exists(path))
            {
                // Datei umgezogen? Über den Dateinamen erneut suchen.
                string wanted = Path.GetFileName(path);
                foreach (string candidate in FindAll())
                    if (string.Equals(Path.GetFileName(candidate), wanted, StringComparison.OrdinalIgnoreCase))
                        return candidate;
                return null;
            }
            return path;
        }

        public static void Assign(string fighterId, string path)
        {
            if (string.IsNullOrEmpty(fighterId)) return;
            if (string.IsNullOrEmpty(path)) PlayerPrefs.DeleteKey(Key(fighterId));
            else PlayerPrefs.SetString(Key(fighterId), path);
            PlayerPrefs.Save();
        }

        public static void Clear(string fighterId) => Assign(fighterId, null);

        /// <summary>Ordnet allen 9 Charakteren per Namensraten ein Modell zu.</summary>
        public static int AutoAssignAll()
        {
            var files = FindAll();
            int count = 0;
            foreach (string id in GameConstants.AllCharacterIds)
            {
                string guess = GuessForCharacter(id, files);
                if (guess == null) continue;
                Assign(id, guess);
                count++;
            }
            return count;
        }

        // ---------- Feinjustage pro Charakter ----------

        public static float GetScale(string id) => PlayerPrefs.GetFloat($"pk_model_{id}_scale", 0f);   // 0 = automatisch
        public static void SetScale(string id, float v) { PlayerPrefs.SetFloat($"pk_model_{id}_scale", v); PlayerPrefs.Save(); }

        public static float GetYaw(string id) => PlayerPrefs.GetFloat($"pk_model_{id}_yaw", 0f);
        public static void SetYaw(string id, float v) { PlayerPrefs.SetFloat($"pk_model_{id}_yaw", v); PlayerPrefs.Save(); }

        public static float GetYOffset(string id) => PlayerPrefs.GetFloat($"pk_model_{id}_yoff", 0f);
        public static void SetYOffset(string id, float v) { PlayerPrefs.SetFloat($"pk_model_{id}_yoff", v); PlayerPrefs.Save(); }

        /// <summary>Zielhöhe eines Kämpfers in Metern (Kapselhöhe).</summary>
        public const float TargetHeight = 1.8f;
    }
}
