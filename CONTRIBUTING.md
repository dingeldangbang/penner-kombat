# Mitwirken an LETAL PENNER KOMBAT

Danke, dass du mithelfen willst! Dieses Projekt ist ein Fan-/Kunstprojekt unter
**CC BY-NC 4.0** (nicht-kommerziell). Alle Beiträge werden unter derselben Lizenz
übernommen.

## Wie du beiträgst

1. **Fork** dieses Repos und erstelle einen Branch:
   `git checkout -b feature/mein-feature`
2. **Änderungen** machen (Code, Docs, Balance).
3. **Dokumentation** aktualisieren, wenn du Verhalten änderst.
4. **Tests** laufen lassen (siehe unten) — wenn du Kampf-Systeme änderst.
5. **Pull Request** öffnen und im Beschreibungstext erklären, was und warum.

## Coding-Standards

- **Namensraum:** `PennerKombat` für alle Skripte.
- **Bezeichner:** ASCII (keine Umlaute/Unicode in Klassennamen/Methoden),
  z.B. `GroesserSchwung` statt `GroßerSchwung`.
- **Zugriff:** Kernfelder des `FighterController` als `protected`/`public`;
  niemals `private` Felder der Basis von Unterklassen anfassen.
- **Frame-Daten:** Neue Moves ins `MoveCatalog` eintragen, nicht hart in `ExecuteMove`.
- **Balance-Werte:** zentral in `GameConstants.cs` und den `[Header]`-Feldern der Charaktere.

## Tests

Der `TestRunner` (Unity-Szene) prüft beim Start:
- FighterDatabase-Roster (9 Charaktere)
- Singletons vorhanden
- Schadensmodell stabil
- KI vorhanden

Ergebnisse erscheinen als `[Test] ✅/❌`-Logzeilen.

## Auf jeden Fall vermeiden

- Kommerzielle Nutzung oder lizenzierte Marken ohne Freigabe.
- Unicode-Bezeichner (Encoding-Probleme).
- Doppelte Manager/Singletons (Bit teilen, nicht multiplizieren).

## Fragen?

Öffne ein Issue oder schreib in den Chat der Lobby (Demo).

---

Projektstruktur, Setup und Roadmap: [README.md](README.md) · [docs/SETUP.md](docs/SETUP.md)
