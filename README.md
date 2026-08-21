# 🩸 LETAL PENNER KOMBAT

> **„Kombat der städtischen Randfiguren"** — ein humorvoller, brutaler 2.5D-Fighter in Unity URP.

[![Unity](https://img.shields.io/badge/Unity-2023%20LTS%20%2F%206-blue)](https://unity.com/releases)
[![License](https://img.shields.io/badge/License-CC_BY--NC--4.0-lightgrey)](LICENSE)
[![Language](https://img.shields.io/badge/C%23-.NET%20Standard%202.1-green)]()

---

## ⚠️ Wichtiger Hinweis vor dem Start

Dieses Repository enthält den **vollständigen, codekompletten C#-Quellcode** des Spiels, ergänzt um alle zuvor fehlenden Systeme (Hitboxen, Fatal Blow/Fatality/Brutality, Beschwörungen, Story, Netzwerk, Trophäen). Der Code ist **statisch geprüft und intern konsistent**, wurde aber in einer **kopf- und testlosen Umgebung** erstellt — es gibt **keinen Unity-Editor-Build** in diesem Repo.

**Das heißt:** Du musst das Projekt in Unity öffnen, die Szene/Prefabs/Animations-Assets anlegen (Anleitung unten) und einen Kontroll-Build fahren. Der Code ist darauf ausgelegt, mit minimalem Setup zu kompilieren.

---

## 🎮 Features

### Kampfsystem
- **9 spielbare Charaktere** mit je einzigartiger Mechanik (siehe unten)
- Hitbox-System (Box-Overlap + optionales Trigger-`Hitbox`-Modul)
- Blocken mit 78 % Schadensreduktion, Combos, Knockback, Stun
- **Fatal Blow (X-Ray)**: volle Leiste → Cinematic + hoher Schaden (Y / West+East)
- Zustands-KI (`AIController`) mit Nähern/Angriff/Block/Rückzug

### Die 9 Charaktere
| ID | Name | Archetyp | Kernmechanik |
|----|------|----------|--------------|
| `le_binde` | **Le Binde** | Grappler/Zoner-Killer | Schmier-Schlüppa (Fett-Rüstung), Mops-Kommando |
| `mell` | **Mell** | Rushdown/Glass Cannon | „Der Puls" (60–220 bpm, HP-Drain, Blackout) |
| `mojo_bob` | **Mojo Bob** | RNG/Krit | „Das Mojo" (Krits, Mojo-Wetten, Dingeneldang!) |
| `dieter` | Dieter | Brawler | Schraubenschlüssel, Abflussreiniger-Buff |
| `uschi` | Uschi | Healer/Zoner | Handtasche, Gurke (heilt) |
| `tetrapak` | TetraPak | DoT/Buff | Fusel-Atem (Brand), Zweiter Wind |
| `sigi` | Sigi | Zoner/Debuff | Root-Zugriff (Eingabe-Invertierung) |
| `rolf` | Rolf | Summoner | Ratten beschwören |
| `kalle` | Kalle | Grappler | Greifattacken, Münze fangen |

### Systeme
- **Arena** `ArenaManager`: umkippbare Props, Explosionen, Stage-Fatality, Mops-Napf
- **Story-Modus**: `StoryManager`, `DialogueSystem`, `ChapterData`, 4 Enden (A–D)
- **Movesets**: `MoveData`/`CommandInput`/`MoveCatalog` — Numpad-Kommandoeingaben + Frame-Daten für alle 9 Charaktere
- **Training-Modus**: `TrainingMode` (Dummy-Verhalten, Anzeige, Optionen) + `InputRecorder` + `TutorialManager` (10 Lektionen)
- **Lobby & Matchmaking**: `LobbySystem` (Räume, Passwort, 2–4 Spieler, Ready, Chat)
- **Fatalities**: `FatalitySystem` (Eingabe + Trophäen-Trigger für die DLC-Fatalities)
- **Netzwerk**: `WebSocketClient` (Relay) + `NetworkManager` (Ausbauweg → Mirror/Netcode)
- **Trophäen**: alle 56 Trophäen (inkl. 6 DLC 51–56), Speicherung via `PlayerPrefs`
- **Audio**: `AudioManager` (Musik/SFX/Voice/Ambient, 31 Tracks, Mixer-Support)
- **Bootstrap**: `Bootstrapper` erzeugt alle Manager automatisch
- **Editor-Wizard**: `Tools → Penner Kombat → Setup-Szene erzeugen`

### Extras — „nix für Pussys"
- **Arena-Zerstörung** in 5 Stufen (Trümmer, Feuer, düsterer werdendes Licht)
- **Beat-Sync** `MusicSync`: Combo treibt Tempo und Pitch der 808-Beats, Ducking/Cut bei Fatalities
- **Ragdoll** beim K.o., **Wunden + Blutlachen**, **Konter/Parry** mit Bestrafungsfenster
- **Waffen** (Schraubenzieher, Maulschlüssel, Rohrzange, Kochlöffel, Rattengift) zum Aufheben
- **Power-Ups** (Bier, Med, Speed, Schild, Feuer, Eis, Pfand, Todeskuss)
- **Verbündete**: Mercedes 190e mit drei Atzen, Punker-Crowd „Kantenkanten Nackenschelle"
- **Zuschauer**, die im Takt wippen und auf Combos reagieren · **Bosse** mit 3 Phasen

### 3D-Kampfraum
- **Kamera** `CameraController`: Dynamic / Follow / TopDown / Cinematic + Kamerafahrten
- **360°-Bewegung**: kamerarelativ, geglättete Drehung, Strafe beim Blocken, **Ausweichrolle mit i-Frames**
- **3D-Hitboxen** `Hitbox3D`: Sphere, Box, Capsule, **Cone** (Winkeltest) mit Gizmos
- **Arena-Objekte** `ArenaObject3D`: werfen, sprengen, Trampolin, Falle — Interaktion per `E`
- **Räumliche Effekte**: `ComboTrail3D` (LineRenderer-Spur), `ComboExplosion3D` (Druckwelle + Rückstoß)
- **Netzwerk** `NetworkSync3D`: 20 Hz Positions-/Rotations-Sync mit Interpolation

### Visuals & Effekte (Cover-Look)
- **Farbpalette** `PennerPalette`: Blutrot/Nachtblau/Laternen-Orange/Gold/Neonblau — direkt vom Cover abgeleitet
- **Treffer-VFX** `VFXManager`: Funken, Blut (schadensskaliert), Schockwellen, Goldsterne, Staub — **prozedural, ohne Art-Assets**
- **Combo-Feedback** `ComboSystem` + `ComboCounterUI`: Zähler 1–4 weiß / 5–9 gold / 10+ rot flackernd, Ping-Leiter bei 3/5/8/10+
- **Kamera** `CameraShake`: Shake 2–20 px, Combo-Zoom bis 1,25×, Hitstop, Frame-Freeze, Zeitlupe (Mops 0,1× · X-Ray 0,5× · Fatality 0,3×)
- **Screen-FX** `ScreenEffects`: Vignette, Rotblitze, Mells Puls-Rand, Blackout, Blut auf der Linse
- **Arena-Look** `ArenaVisuals`: Laternen-Spotlights, Mondlicht, flackerndes Neonschild, Mücken/Staub/Rauch, nasses Pflaster
- **Charakter-Auren** `CharacterVisuals`: Le Bindes Fettglanz, Mells pulsabhängiges Flackern, Bobs Gold-Staub je Mojo-Punkt
- **Status-HUD** `HudStatusBars`: Mojo-Punkte, Puls-Balken (schlägt im Takt), Schmier-Schlüppa, Fatal-Blow
- **Signatur-Combos** `SignatureFx`: jede Spezial-Combo aller 9 Charaktere als fertige Inszenierung
- **Mops-Kommando** `MopsKommandoSequence`: 180-Frame-Cinematic (Zeitlupe, Paula, Häufchen, kippende Arena)
- **Arena-Props** `ArenaProp`: Bierkasten-Turm, Gasflasche (4 Treffer → Explosion), Wäscheleine (Stun), Mülltonne, Gerüst + Boden-Hazards
- **Charakter-Shader** `Assets/Shaders/PennerCharacter.shader`: Grease, Pulse, Gold, Fire, Matrix, Rat + Treffer-Flash
- **Relay-Server** `server/` (Node + `ws`): Räume, Nachrichtenspiegelung, Ready→Start, Heartbeat — `cd server && npm install && npm start`
- **Optional URP** `UrpPostProcessingDriver` (Define `PK_URP`): Bloom, ACES, Vignette, CA, DoF, Film Grain

---

## 📁 Projektstruktur

```
Assets/
├── Scripts/
│   ├── Core/         FighterController, FighterInput, GameManager, UIManager,
│   │                 ArenaManager, CameraController, AudioManager, HitboxManager,
│   │                 GameConstants, Timer, Extensions, ObjectPool, Bootstrapper
│   ├── Characters/   LeBinde, Mell, MojoBob, Dieter, Uschi, TetraPak, Sigi, Rolf, Kalle
│   │   └── Summons/  MopsController (Paula), Herta, RatController
│   ├── Combat/       FatalBlowSystem
│   ├── Projectiles/  BottleProjectile, SludgeProjectile, SpoonProjectile, CoinProjectile, LuckyBag
│   ├── AI/           AIController
│   ├── Story/        StoryManager, DialogueSystem, ChapterData, EndingSystem
│   ├── Network/      WebSocketClient, NetworkManager
│   ├── Trophies/     TrophyManager, TrophyNotificationUI
│   ├── UI/           MainMenu, PauseMenu, CharacterSelectUI, CharacterSlotUI
│   ├── Data/         FighterConfig, FighterDatabase
│   └── Testing/      TestRunner
└── Scenes/           (anzulegen, siehe SETUP)
```

---

## 🚀 So startest du (Unity-Setup)

1. **Unity Hub** → neues Projekt **„3D (URP)"** mit **Unity 2023 LTS** oder **Unity 6**.
2. `Assets/Scripts` aus diesem Repo in das Projekt kopieren.
3. **Package Manager** aktivieren:
   - **Input System** (`com.unity.inputsystem`) — Voraussetzung für `FighterInput`.
   - **TextMeshPro** (TMP Essentials importieren).
4. **Layers/Tags** anlegen: `Ground`, `Fighter`, `Interactable`, `Projectile`. Die `enemyLayer`-Masken der Kämpfer auf die `Fighter`-Layer setzen.
5. **Eingabe-Modus** in *Player Settings → Active Input Handling* auf **„Both"** (oder „Input System Package").
6. **Minimal-Szene aufbauen** (siehe `docs/SETUP.md`): Ebene + `Bootstrapper`-Object.
7. Kontroll-Build starten.

> Details, Prefab-Anforderungen und Empfehlungen: **[docs/SETUP.md](docs/SETUP.md)**

## 📚 Dokumentation

| Datei | Inhalt |
|-------|--------|
| [docs/DESIGN.md](docs/DESIGN.md) | Design-Dokument der Roster-Erweiterung (Le Binde, Mell, Mojo Bob, Arena) |
| [docs/MOVESETS.md](docs/DESIGN.md) | Movesets/Frame-Daten aller Charaktere (Design-Kapitel §1.1–§1.9) |
| [docs/VISUALS.md](docs/VISUALS.md) | Gameplay-Visuals, Effekte & Combo-Feedback (Cover-Look) |
| [docs/TROPHIES.md](docs/TROPHIES.md) | Alle 56 Trophäen |
| [docs/SOUNDTRACK.md](docs/SOUNDTRACK.md) | Alle 31 Musik-Tracks |
| [docs/STRATEGY.md](docs/STRATEGY.md) | Strategieguide für jeden Charakter |
| [docs/SETUP.md](docs/SETUP.md) | Unity-Setup-Anleitung |
| [docs/EXTRAS.md](docs/EXTRAS.md) | Arena-Zerstörung, Waffen, Power-Ups, Konter, Ragdoll, Bosse, Crowd, Beat-Sync |
| [docs/3D.md](docs/3D.md) | 3D-Kampfraum: Kameramodi, 360°-Bewegung, Rolle, 3D-Hitboxen, Arena-Objekte |
| [docs/SERVER.md](docs/SERVER.md) | Relay-Server: Start, Protokoll, Deployment, Grenzen |
| [docs/CONTROLS.md](docs/CONTROLS.md) | Tastatur (P1/P2), Gamepad, KI-Stufen, Multiplayer-Modi |
| [docs/TOUCH.md](docs/TOUCH.md) | Touch-Steuerung: Joystick, Buttons, Gesten, Layout-Editor, Optionen |
| [docs/STATUS.md](docs/STATUS.md) | Soll/Ist: was im Repo steckt und was wirklich noch fehlt |
| [docs/BUILD_ANDROID.md](docs/BUILD_ANDROID.md) | Android-Build (APK/AAB): Setup, Player Settings, Signierung |
| [docs/RELEASE.md](docs/RELEASE.md) | GitHub-Release-Anleitung |

---

## 🧪 Tests

`TestRunner` (an ein Object gehängt) führt beim Start Prüfungen durch:
- FighterDatabase-Roster (9 Charaktere)
- Singletons vorhanden
- Schadensmodell stabil
- KI vorhanden

Ergebnisse erscheinen als `[Test] ✅/❌`-Logzeilen.

---

## 📜 Spielsteuerung (Tastatur, Player 1)

| Aktion | Taste |
|--------|-------|
| Bewegen | W A S D |
| Leichter Angriff | J |
| Schwerer Angriff | K |
| Blocken | Shift |
| Springen | Space |
| Spezial 1 | U |
| Spezial 2 | H |
| Fatal Blow | Y |

Charakter-Spezials (zusätzliche Bindings): **1/2/3/4**-Tasten pro Charakter (siehe Code-Kommentare).

---

## 🔧 Fehlende Assets / Ausbau

| Fehlt | Hinweis |
|-------|---------|
| `.prefab`-Dateien | müssen im Editor aus Capsules + Materialien gebaut werden |
| Animationscontroller/-Clips | Idle/Walk/Attack/Block/HitReact etc. (Parameter siehe `FighterController`) |
| Audio-Clips (Tracks 29–31, SFX, Voice) | Clips in `Assets/Resources/Audio/` ablegen |
| Shader/Materialien | URP-Standard-Materialien oder eigene |
| Kapitel-Daten | `StoryManager.chapters` im Editor befüllen oder `ChapterData`-Assets |
| WebSocket-Server | eigener Relay-Server für Online-Modus |

---

## 📄 Lizenz

Dieses Projekt ist ein Fan-/Kunstprojekt. Siehe **[LICENSE](LICENSE)** (CC BY-NC 4.0 — nicht-kommerziell).

---

## 🙌 Mitwirkung

Issues und Pull-Requests sind willkommen — insbesondere für **Balance-Werte**, **Fatalities** und **Story-Inhalte**.
