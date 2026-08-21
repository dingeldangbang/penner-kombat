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
- **SaveSystem** (`Assets/Scripts/Utils/SaveSystem.cs`): JSON-Speicherstand mit Match-Statistik,
  Combo-/Schadensrekorden, Fatality-Zählern und Optionen; wird vom `GameManager` bei Matchende
  und vom `FighterController` bei Fatality/X-Ray beschrieben.
- **Optionsmenü** (`Assets/Scripts/UI/OptionsMenu.cs`): Audio, Auflösung, Qualität, VSync sowie
  die VFX-Regler `intensity` und `gore` — alle Werte laufen über das SaveSystem.
- **Shader**: `Assets/Shaders/Outline.shader` (Zwei-Pass-Silhouette) und `Glow.shader`
  (additiver Radial-Glow mit optionalem Puls).
- **Android**: `Assets/Plugins/Android/AndroidManifest.xml` und `docs/BUILD_ANDROID.md`
  (Projektaufbau, Player Settings, IL2CPP/ARM64, Signierung, CI-Hinweise, Touch-Lücke).
- **Touch-Steuerung komplett** (`Core/VirtualInput.cs`, `UI/TouchControls.cs`, `UI/Touch/`):
  `VirtualJoystick` (fest/dynamisch/Snap, Totzone, Empfindlichkeit), `TouchButton`
  (Tap/Halten/Toggle, Vibration), `TouchInputManager` (EX-Combo Block+Spezial),
  `TouchGestureDetector` (8 Richtungen → Numpad-Sequenz, die der bestehende `CommandInput`
  auswertet), `TouchLayoutEditor` + `DragElement` (Drag & Drop, 4 Layout-Vorlagen),
  `AntiGhosting`, `InputBuffer` (0,15 s), `TouchTutorial` (7 Schritte), `TouchSettings`
  (JSON in PlayerPrefs) und `TouchDebug` (F3-Overlay). Alles prozedural, ohne Prefabs;
  `FighterInput` verodert Touch mit Tastatur und Gamepad, das Optionsmenü bekommt eine
  Touch-Sektion. Doku: `docs/TOUCH.md`.
- **Extras-Paket** (`docs/EXTRAS.md`): `ArenaDestruction` (5 Zerstörungsstufen mit Trümmern,
  Feuer und düsterer werdendem Licht), `MusicSync` (Beat-Events, combo-getriebenes Tempo,
  Ducking und Cut bei Fatalities), `RagdollController` (Ragdoll bzw. Bruchstücke beim K.o.),
  `DamageVisuals` (Wunden, Blutlachen, Blässe bei wenig HP), `ParrySystem` (Konter im
  0,16-s-Fenster mit Bestrafungsbonus), `WeaponSystem` (5 aufhebbare Waffen mit Effekten),
  `PowerUpSystem` (8 Power-Ups), `AllySummon` (Mercedes 190e mit drei Atzen, Punker-Crowd),
  `CrowdReactions` (Zuschauer wippen im Takt, reagieren auf Combos), `BossController`
  (3 Phasen mit Tempo- und Schwierigkeitssprung).
- **3D-Kampfraum**: `CameraController` mit vier Modi (Dynamic/Follow/TopDown/Cinematic) und
  Kamerafahrten; kamerarelative 360°-Bewegung, geglättete Drehung, Strafe beim Blocken und
  **Ausweichrolle mit 0,2 s Unverwundbarkeit**; `Combat/Hitbox3D.cs` (Sphere/Box/Capsule/Cone
  mit Gizmos); `VFX/ArenaObject3D.cs` + `ArenaInteraction` (werfen, sprengen, Trampolin, Falle);
  `VFX/ComboTrail3D.cs` und `VFX/ComboExplosion3D.cs` (Druckwelle mit AddExplosionForce);
  `Core/AnimationsController3D.cs` (setzt Mecanim-Parameter nur, wenn sie existieren);
  `Network/NetworkSync3D.cs` (20 Hz Sync mit Interpolation und kurzer Extrapolation).
  Setup-Wizard stellt Wände und ein werfbares Objekt auf. Doku: `docs/3D.md`.
- **Relay-Server** (`server/`, Node ≥ 18 + `ws`): verwaltet Räume und spiegelt Nachrichten
  zwischen den Spielern; Ready→`start`, Host-Übergabe beim Verlassen, Heartbeat gegen tote
  Verbindungen, Aufräumen leerer Räume, HTTP-Endpunkte `/health` und `/rooms`.
  Smoke-Test mit 18 Prüfungen (`npm test`) — alle grün. Doku: `docs/SERVER.md`.
- **NetworkManager spricht das Protokoll**: Ereignisse `OnJoined`, `OnPeerJoined`, `OnPeerLeft`,
  `OnMatchStart`, `OnPeerUpdate`, `OnPeerAction`, `OnServerError` sowie `SendChat`,
  `SendAction` und `SendPing`; schlanker Feld-Extraktor statt String-Contains-Prüfungen.
- **Tastatur für zwei Spieler**: `FighterInput` mit P1 (WASD + J/K/L/U/I/O/E/Y/H) und
  P2 (Pfeiltasten + Nummernblock), neue Aktionen EX, Interaktion und Med; Belegung frei
  umlegbar (`KeyboardLayoutUI`, Persistenz über `KeyBindings`).
- **Med-Kapsel** (`Core/MedSystem.cs`): 2 Ladungen, +25 HP, 1,2 s Anwendung, 8 s Cooldown,
  Rundenreset; wird auch von der KI genutzt.
- **KI mit sechs Schwierigkeitsstufen** (`AI/AIController.cs`): Sehr leicht bis Boss mit
  Reaktionszeit, Block-, Combo- und Spezialchance sowie Schadensmultiplikator; dynamische
  Combo-Ketten (3–13 Schritte) und Med-Nutzung unter 30 % HP.
- **Multiplayer-Kopplung**: `Network/LanDiscovery.cs` (UDP-Broadcast + Thread-Listener),
  `Network/RoomCode.cs` (Payload `PK://ip:port/raum`, 6-stelliger Code),
  `Network/QrCoupling.cs` (Textcode; QR-Bild und Kamera-Scan mit Define `PK_ZXING`),
  `Network/MultiplayerConfig.cs` (JSON in PlayerPrefs) und `UI/MultiplayerMenuUI.cs`
  (Lokal / Kopplung / LAN / Online). `NetworkManager` bekommt `HostRoom`, `JoinHost`
  und `LeaveRoom`. Doku: `docs/CONTROLS.md`.
- **Doku**: `docs/VISUALS.md` (vollständige Spezifikation inkl. Umsetzungs-Mapping),
  `docs/STATUS.md` (Soll/Ist-Abgleich: was existiert, was wirklich noch fehlt).

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
