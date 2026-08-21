# Changelog

## [Unveröffentlicht]
### Gameplay-Visuals, Effekte & Combo-Feedback (Cover-Look)
- **Neue VFX-Schicht** `Assets/Scripts/VFX/` — komplett prozedural, keine Art-Assets nötig:
  `PennerPalette` (Farbpalette vom Cover), `VFXManager` (HitSpark/Blut/Schockwelle/GoldKrit/Staub),
  `CameraShake` (Shake, Combo-Zoom, Hitstop, Frame-Freeze, Zeitlupe), `ScreenEffects`
  (Vignette, Blitze, Puls-Rand, Blackout, Linsen-Splatter), `ComboSystem` + `ComboCounterUI`
  (Eskalation 1–4/5–9/10+ mit Sound-Pings), `CharacterVisuals` (Auren/Trails je Kämpfer),
  `ArenaVisuals` (Laternen, Neon-Flackern, Nacht-Ambient, Atmosphäre-Partikel),
  `HudStatusBars` (Mojo/Puls/Schmier-Schlüppa), `FloatingText` (Callouts),
  `UrpPostProcessingDriver` (optional via Define `PK_URP`).
- **Kampfsystem verdrahtet**: `FighterController.PlayHitFeedback(...)` mit Trefferklassen
  (`HitTier`), Block-, X-Ray- und Fatality-Präsentation.
- **Charakter-Effekte**: Bobs Krit (Goldblitz + Frame-Freeze + „DINGENELDANG!"),
  Mells Defi/Blackout/Puls-Rand, Le Bindes Mops-Kommando (Zeitlupe + Props kippen),
  REIF!-Glow, Flaschenhals-Glassplitter.
- **Bootstrapper & Setup-Wizard** erzeugen die Visual-Schicht automatisch; `TestRunner`
  prüft sie mit drei neuen Checks.
- **Signatur-Combos aller 9 Charaktere** (`SignatureFx`): Le Bindes Fett/Flaschenhals/REIF!/Pfanne,
  Mells Doppelschicht/Defi/„Sechzehn Stunden" (11 Regenbogen-Nachbilder), Bobs Würfel-Wette,
  Löffelsturm und Dingeneldang!, dazu Dieter, Uschi, TetraPak, Sigi, Rolf und Kalle.
- **Mops-Kommando als 180-Frame-Sequenz** (`MopsKommandoSequence`): Zeitlupe, Paula, Pfotenabdrücke,
  Häufchen, Debuff-Banner, kippende Arena, Staub — frame-genau nach Spec §3.2.
- **Interaktive Arena-Props** (`ArenaProp`, `ArenaHazard`): Bierkasten-Turm, Gasflasche (4 Treffer →
  Explosion), Wäscheleine (Stun), Mülltonne (rollt), Paula-Napf, Baugerüst; Glassplitter- und
  Brand-Hazards am Boden. `ArenaVisuals.BuildYard()` stellt das Set auf.
- **Charakter-Shader** `Assets/Shaders/PennerCharacter.shader` (Grease/Pulse/Gold/Fire/Matrix/Rat +
  Treffer-Flash) mit `CharacterShaderBinder` (MaterialPropertyBlock, zustandsgesteuert).
- **Benannte Screen-States** (`ScreenState`): Dingeneldang, MojoLockout, Fatality, Fusel, Matrix,
  RatSwarm, Monochrome — inkl. URP-Profilen; Combo-Tabelle bis Stufe 21+ (Feuerwerk).
- **Neue Partikeltypen**: Sludge, PulseGlow, Grease, Fire, Poison, Fireworks, Rainbow-Hits.
- **Doku**: `docs/VISUALS.md` (vollständige Spezifikation inkl. Umsetzungs-Mapping).

## [1.3.0] — 2026-08-21
### Trophäen, Soundtrack, Fatalities, GitHub-Bereitstellung
- **Vollständiges Trophäensystem (56)**: `TrophyManager` registriert alle 56
  Trophäen (50 Basis + 6 DLC 51–56); Doku `docs/TROPHIES.md`.
- **Fatality-System**: `FatalitySystem` (Eingabe bei niedrigem Gegner-HP) +
  `PerformFatality` in Basis/Le Binde/Mell/Mojo Bob; verdrahtet die DLC-Trophäen
  (51 „Guten Appetit", 53 „Auf der Kante", 54 „Der ehrliche Betrug", 56 „Die stabile Seitenlage").
- **Soundtrack-Doku**: `docs/SOUNDTRACK.md` (31 Tracks, inkl. 29–31).
- **Movesets-Doku**: `docs/MOVESETS.md` (alle 9 Charaktere mit Frame-Daten).
- **Editor-Wizard**: `Tools → Penner Kombat → Setup-Szene erzeugen` (`GameSetupWizard`).
- **GitHub-Bereitstellung**: `CONTRIBUTING.md`, `docs/RELEASE.md`.

## [1.2.0] — 2026-08-21
### Strategieguide, Training-Modus & Lobby
- **docs/STRATEGY.md**: vollständiger Strategieguide für alle 9 Charaktere.
- **Training-Modus**: `TrainingMode` (Dummy-Verhalten, Anzeige-Optionen, Optionen),
  `InputRecorder` (Aufzeichnung/Playback), `TutorialManager` (10 Lektionen, Fortschritt via PlayerPrefs).
- **Lobby & Matchmaking**: `LobbySystem` (Räume erstellen/beitreten, Privat/Öffentlich,
  Passwort, 2–4 Spieler, Ready-System, Text-Chat), verdrahtet mit `NetworkManager`.

## [1.1.0] — 2026-08-21
### Komplette Movesets + Command-Input-System
- Neues **Command-Input-System** (`CommandInput`, `MoveData`, `MoveCatalog`): erkennt
  Richtungs-Sequenzen in Numpad-Notation (↓↘→, ↓↙←, →↘↓↙←, …) + Button mit Frame-Daten.
- Alle **9 Charaktere** auf die dokumentierten Movesets umgestellt (Frame-Daten & Effekte):
  Le Binde, Mell, Mojo Bob, Dieter, Uschi, TetraPak, Sigi, Rolf, Kalle.
- Neue Debuffs/Effekte: `SlowDebuff` (TetraPak), `Fassungslosigkeit`-Eingabeverzögerung (Sigi DDoS),
  Block-Buffs (Uschi/Sigi), Gift-DoT (Rolf), Trink-Mechanik (TetraPak).
- AI nutzt Spezialbewegungen (`specialChance`).

## [1.0.0] — 2026-08-21
### Initialer Release (Code-komplett)

**Kampfsystem**
- `FighterController` als abstrakte, sauber überschreibbare Basis (virtual/protected) — behebt alle Compile-Blocker der Vorgängerversion.
- Hitbox-System: Box-Overlap im Controller + wiederverwendbares `Hitbox`/`HitboxManager`-Trigger-Modul.
- Blocken (78 % Reduktion), Combos, Knockback, Stun, Fatal-Blow-Leiste.

**Charaktere (9/9)**
- Le Binde, Mell, Mojo Bob (neue, vollständig) + Dieter, Uschi, TetraPak, Sigi, Rolf, Kalle.
- Beschwörungen: `MopsController` (Paula), `Herta`, `RatController`.

**Systeme**
- `FatalBlowSystem` (X-Ray-Auslösung + Cinematic), Projektile, KI, Story (8-Kapitel-Gerüst, Dialoge, 4 Enden), WebSocket-Netzwerk, Trophäen (6 DLC), Audio, Menü/Charakterauswahl/Pause, `Bootstrapper`, `TestRunner`.

**Doku**
- README, SETUP-Anleitung, DESIGN-Dokument, Lizenz.
