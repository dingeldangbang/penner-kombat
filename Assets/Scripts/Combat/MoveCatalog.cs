using System.Collections.Generic;

namespace PennerKombat
{
    /// <summary>
    /// Zentraler Katalog aller Spezialbewegungen mit Frame-Daten aus dem
    /// Design-Dokument (§1.1–§1.9). Jede Bewegung wird als MoveData geliefert;
    /// die tatsächliche Ausführung (Schaden/Effekt) erfolgt in der jeweiligen
    /// Charakter-Klasse über ExecuteMove(MoveData).
    /// </summary>
    public static class MoveCatalog
    {
        public static List<MoveData> LeBinde() => new List<MoveData>
        {
            MoveData.Make("flaschenhals", "Flaschenhals", new[]{2,3,6}, MoveButton.Light, 14, 4, 28, 11f),
            MoveData.Make("flaschenhals_ex", "EX-Flaschenhals", new[]{2,3,6}, MoveButton.Light, 14, 4, 28, 15f, true),
            MoveData.Make("grosser_schwung", "Der große Schwung", new[]{4,6}, MoveButton.Heavy, 21, 6, 22, 16f),
            MoveData.Make("reif", "REIF!", new[]{2,2}, MoveButton.SpecialA, 30, 0, 0, 0f),
            MoveData.Make("mops", "Mops-Kommando", new[]{2,3,6}, MoveButton.SpecialB, 50, 0, 0, 0f, false, 22f),
            MoveData.Make("pfanne", "Aus der Pfanne", new[]{6,6}, MoveButton.Heavy, 11, 4, 18, 14f),
        };

        public static List<MoveData> Mell() => new List<MoveData>
        {
            MoveData.Make("pampe", "Pampe", new[]{2,1,4}, MoveButton.Light, 16, 6, 20, 6f),
            MoveData.Make("doppelschicht", "Doppelschicht", new[]{6,6}, MoveButton.Light, 9, 4, 4, 7f),
            MoveData.Make("erste_hilfe", "Erste Hilfe", new[]{2,2}, MoveButton.Heavy, 40, 0, 0, 0f, false, 12f),
            MoveData.Make("defi", "Defi", new[]{4,1,2,3,6}, MoveButton.SpecialB, 13, 4, 18, 8f),
            MoveData.Make("sechzehn", "Sechzehn Stunden", new[]{2,3,6,2,3,6}, MoveButton.Heavy, 15, 50, 0, 38f, false, 0f, 0, 160f),
        };

        public static List<MoveData> MojoBob() => new List<MoveData>
        {
            MoveData.Make("schwanz", "Riesenschwanz", new[]{2,3,6}, MoveButton.Light, 19, 6, 24, 12f),
            MoveData.Make("schwanz_ex", "EX-Riesenschwanz", new[]{2,3,6}, MoveButton.Light, 19, 18, 24, 12f, true),
            MoveData.Make("beutelchen", "Beutelchen", new[]{4,1,2}, MoveButton.SpecialA, 22, 0, 0, 0f),
            MoveData.Make("loeffelsturm", "Löffelsturm", new[]{6,3,2,1,4}, MoveButton.Light, 24, 0, 0, 0f, false, 4f),
            MoveData.Make("fuenfzig", "Fünfzig Cent", new[]{2,2}, MoveButton.Light, 15, 0, 0, 0f),
            MoveData.Make("dingeneldang", "Dingeneldang!", new[]{6,6,2,2}, MoveButton.SpecialA, 30, 0, 0, 0f, false, 15f, 7),
        };

        public static List<MoveData> Dieter() => new List<MoveData>
        {
            MoveData.Make("schraubenschluessel", "Schraubenschlüssel", new[]{2,3,6}, MoveButton.Light, 15, 4, 20, 14f),
            MoveData.Make("abfluss", "Abflussreiniger", new[]{6,3,2,1,4}, MoveButton.Heavy, 25, 0, 0, 0f, false, 8f),
            MoveData.Make("rohrbruch", "Rohrbruch", new[]{2,2}, MoveButton.Heavy, 30, 0, 0, 0f, false, 8f),
            MoveData.Make("kanalisation", "Kanalisation", new[]{4,1,2,3,6}, MoveButton.Heavy, 20, 4, 20, 12f, false, 6f),
            MoveData.Make("wasserrohr", "Wasserrohr", new[]{6,6}, MoveButton.Heavy, 10, 6, 18, 18f),
        };

        public static List<MoveData> Uschi() => new List<MoveData>
        {
            MoveData.Make("handtasche", "Handtasche", new[]{2,3,6}, MoveButton.Light, 14, 4, 22, 12f, false, 1.5f),
            MoveData.Make("gurke", "Gurke", new[]{2,2}, MoveButton.Heavy, 35, 0, 0, 0f, false, 12f),
            MoveData.Make("kochloeffel", "Kochlöffel", new[]{4,1,2}, MoveButton.Light, 10, 4, 16, 9f),
            MoveData.Make("topfdeckel", "Topfdeckel", new[]{6,3,2,1,4}, MoveButton.Heavy, 25, 0, 0, 0f, false, 8f),
            MoveData.Make("suppe", "Suppe", new[]{2,3,6,2,3,6}, MoveButton.Heavy, 40, 0, 0, 0f, false, 20f),
        };

        public static List<MoveData> TetraPak() => new List<MoveData>
        {
            MoveData.Make("fusel", "Fusel-Atem", new[]{2,3,6}, MoveButton.Light, 18, 6, 24, 6f),
            MoveData.Make("leergut", "Leergut", new[]{4,1,2}, MoveButton.Heavy, 20, 4, 20, 10f, false, 5f),
            MoveData.Make("pfandflasche", "Pfandflasche", new[]{6,6}, MoveButton.Light, 14, 4, 18, 15f),
            MoveData.Make("kater", "Kater", new[]{2,2}, MoveButton.Heavy, 25, 0, 0, 0f, false, 6f),
            MoveData.Make("trinken", "Trinken", null, MoveButton.SpecialB, 20, 0, 0, 0f, false, 4f),
        };

        public static List<MoveData> Sigi() => new List<MoveData>
        {
            MoveData.Make("laptop", "Laptop", new[]{2,3,6}, MoveButton.Light, 16, 4, 20, 8f),
            MoveData.Make("root", "Root-Zugriff", new[]{4,1,2,3,6}, MoveButton.Heavy, 28, 0, 0, 0f, false, 20f),
            MoveData.Make("firewall", "Firewall", new[]{2,2}, MoveButton.Heavy, 20, 0, 0, 0f, false, 5f),
            MoveData.Make("sql", "SQL-Injection", new[]{6,3,2,1,4}, MoveButton.Light, 12, 4, 16, 12f),
            MoveData.Make("ddos", "DDoS", new[]{6,6}, MoveButton.Heavy, 30, 0, 0, 0f, false, 6f),
        };

        public static List<MoveData> Rolf() => new List<MoveData>
        {
            MoveData.Make("ratten", "Ratten", new[]{2,3,6}, MoveButton.Light, 22, 0, 0, 0f, false, 10f),
            MoveData.Make("rattenschwanz", "Rattenschwanz", new[]{4,1,2}, MoveButton.Heavy, 15, 4, 18, 13f),
            MoveData.Make("rattenkoenig", "Rattenkönig", new[]{6,3,2,1,4}, MoveButton.Heavy, 30, 0, 0, 20f, false, 25f),
            MoveData.Make("ratengift", "Ratengift", new[]{2,2}, MoveButton.Heavy, 20, 0, 0, 0f, false, 6f),
            MoveData.Make("kanalisation", "Kanalisation", new[]{6,6}, MoveButton.Heavy, 20, 4, 20, 12f),
        };

        public static List<MoveData> Kalle() => new List<MoveData>
        {
            MoveData.Make("handschuh", "Arbeitshandschuh", new[]{2,3,6}, MoveButton.Light, 14, 4, 20, 12f),
            MoveData.Make("feuerzeuggas", "Feuerzeuggas", new[]{4,1,2}, MoveButton.Heavy, 18, 6, 22, 0f),
            MoveData.Make("rohrzange", "Rohrzange", new[]{6,3,2,1,4}, MoveButton.Heavy, 10, 4, 18, 16f),
            MoveData.Make("ventil", "Ventil", new[]{2,2}, MoveButton.Heavy, 25, 0, 0, 0f, false, 8f),
            MoveData.Make("klempner", "Klempner", new[]{6,6}, MoveButton.Heavy, 30, 0, 0, 0f, false, 10f),
        };

        public static List<MoveData> Get(string fighterId)
        {
            switch (fighterId)
            {
                case GameConstants.CharLeBinde: return LeBinde();
                case GameConstants.CharMell: return Mell();
                case GameConstants.CharMojoBob: return MojoBob();
                case GameConstants.CharDieter: return Dieter();
                case GameConstants.CharUschi: return Uschi();
                case GameConstants.CharTetraPak: return TetraPak();
                case GameConstants.CharSigi: return Sigi();
                case GameConstants.CharRolf: return Rolf();
                case GameConstants.CharKalle: return Kalle();
                default: return new List<MoveData>();
            }
        }
    }
}
