# 📊 STATUS — was ist da, was fehlt wirklich

Stand: 2026-08-22 · Branch `arena/01a0264e-penner-kombat`

Diese Datei gleicht die kursierenden „Was fehlt noch?"-Checklisten mit dem
**tatsächlichen Repo-Inhalt** ab. Kurzfassung: **Code ist praktisch vollständig,
es fehlen Art-Assets, Prefabs und Szenen** — also genau das, was sich nicht per
Skript erzeugen lässt.

---

## ✅ Vorhanden (mit Pfad)

| Bereich | Dateien |
|---|---|
| **Core** | `Core/FighterController.cs` (abstrakt, virtual/protected), `FighterInput.cs` (Tastatur P1+P2, Gamepad, Touch), `VirtualInput.cs`, `MedSystem.cs`, `GameManager.cs`, `UIManager.cs`, `ArenaManager.cs`, `CameraController.cs`, `AudioManager.cs`, `Bootstrapper.cs`, `GameConstants.cs`, `HitboxManager.cs`, `ObjectPool.cs`, `Timer.cs` |
| **Kampf** | `Combat/CommandInput.cs` (Numpad-Notation), `MoveData.cs`, `MoveCatalog.cs`, `FatalBlowSystem.cs`, `FatalitySystem.cs`, `Hitbox3D.cs` (Sphere/Box/Capsule/Cone) |
| **Extras** | `ArenaDestruction`, `MusicSync`, `RagdollController`, `DamageVisuals`, `ParrySystem`, `WeaponSystem`, `PowerUpSystem`, `AllySummon`, `CrowdReactions`, `BossController` |
| **3D** | Kamera mit 4 Modi, 360°-Bewegung + Ausweichrolle, `ArenaObject3D`, `ComboTrail3D`, `ComboExplosion3D`, `AnimationsController3D`, `NetworkSync3D` |
| **Charaktere** | alle 9 in `Characters/` + Summons (`MopsController`, `Herta`, `RatController`) |
| **Projektile** | Bottle, Coin, LuckyBag, Sludge, Spoon |
| **KI** | `AI/AIController.cs` — 6 Schwierigkeitsstufen, Combo-Ketten, Med-Nutzung |
| **VFX** | `VFX/`: `PennerPalette`, `VFXManager`, `CameraShake`, `ScreenEffects`, `ComboSystem`, `ComboCounterUI`, `CharacterVisuals`, `CharacterShaderBinder`, `ArenaVisuals`, `ArenaProp`, `HudStatusBars`, `FloatingText`, `SignatureFx`, `MopsKommandoSequence`, `UrpPostProcessingDriver` |
| **Shader** | `Shaders/PennerCharacter.shader`, `Outline.shader`, `Glow.shader` |
| **UI** | `UI/MainMenu.cs`, `PauseMenu.cs`, `CharacterSelectUI.cs`, `CharacterSlotUI.cs`, `OptionsMenu.cs`, **`TouchControls.cs`** |
| **Story** | `Story/StoryManager.cs`, `DialogueSystem.cs`, `ChapterData.cs`, `EndingSystem.cs` (8 Kapitel, 4 Enden) |
| **Fortschritt** | `Trophies/TrophyManager.cs` (**56** Trophäen), `TrophyNotificationUI.cs`, `Utils/SaveSystem.cs` |
| **Training** | `Training/TrainingMode.cs`, `TutorialManager.cs` (10 Lektionen) |
| **Netzwerk** | `Network/WebSocketClient.cs`, `NetworkManager.cs`, `LobbySystem.cs`, `LanDiscovery.cs`, `RoomCode.cs`, `QrCoupling.cs`, `MultiplayerConfig.cs` |
| **Relay-Server** | `server/` (Node + `ws`): Räume, Spiegelung, Ready→Start, Heartbeat — **getestet, 18/18 grün** (`npm test`) |
| **Test/Editor** | `Testing/TestRunner.cs`, `Editor/GameSetupWizard.cs` |
| **Android** | `Assets/Plugins/Android/AndroidManifest.xml`, `docs/BUILD_ANDROID.md` |

**Wichtig:** Partikel, Screen-FX, Auren, Arena-Licht, Combo-Zähler, Callouts und
die Touch-Buttons werden **prozedural zur Laufzeit erzeugt** — dafür sind keine
Prefabs oder Sprites nötig (siehe `docs/VISUALS.md`).

---

## ❌ Was wirklich fehlt

| # | Fehlt | Warum kein Skript hilft | Priorität |
|---|---|---|---|
| 1 | **Unity-Projekt selbst** (`ProjectSettings/`, `Packages/manifest.json`) | Erzeugt Unity beim Projektanlegen | 🔴 |
| 2 | **Charakter-Modelle + Rigs** (9×) | Art-Asset; Proportionen stehen in `docs/DESIGN.md` | 🔴 |
| 3 | **Animationen** (Idle, Walk, Attacks, Block, Jump, HitReact, Death, Specials, Fatality) | Art-Asset; Frame-Daten liegen in `docs/MOVESETS.md` | 🔴 |
| 4 | **Animator-Controller** mit den Parametern `Walk`, `Block`, `Jump`, `LightAttack`, `HeavyAttack`, `HitReact`, `Death` | Muss im Editor verdrahtet werden — Namen sind im Code fix | 🔴 |
| 5 | **Charakter-Prefabs** (Capsule + Rigidbody + Collider + `attackPoint` + Character-Script) | Editor-Arbeit, danach in `FighterDatabase` zuweisen | 🔴 |
| 6 | **Szenen** `MainMenu`, `Arena`, `StoryMode` | `Arena` erzeugt der Setup-Wizard, die anderen zwei nicht | 🟡 |
| 7 | **Audio** (31 Tracks, SFX, Voice) | Siehe `docs/SOUNDTRACK.md`; ohne Clips greifen die prozeduralen Fallback-Pings | 🟡 |
| 8 | **Arena-Meshes** (Bierkästen, Wäscheleine, Neonschild) | `ArenaProp` funktioniert mit Platzhalter-Würfeln; Optik fehlt | 🟡 |
| 9 | **Noise-Textur** für die Shader-Modi Fire/Matrix/Rat (`_NoiseTex`) | Ohne sie bleiben diese drei Effekte unsichtbar | 🟡 |
| 11 | **Unity-Lizenz für CI** (`UNITY_LICENSE`-Secret) | Nur damit lässt sich das APK automatisiert bauen | 🟢 |

---

## 🎮 Touch-Steuerung — komplett (Details: docs/TOUCH.md)

`Assets/Scripts/UI/TouchControls.cs` + `Core/VirtualInput.cs`:

Zehn Bausteine, alle prozedural: `VirtualJoystick`, `TouchButton`, `TouchInputManager`,
`TouchGestureDetector`, `TouchLayoutEditor`, `AntiGhosting`, `InputBuffer`,
`TouchTutorial`, `TouchSettings` (+ Optionen-Sektion) und `TouchDebug` (F3).

- Belegung: `□` leicht · `△` schwer · `○` Spezial 1 · `✕` Sprung · `BLOCK` (halten) · `S2` · `X-RAY`
- Layouts: Standard, Fighting, Simple, LeftHanded — im Editor frei verschiebbar
- Wischgesten werden in Numpad-Sequenzen übersetzt und vom vorhandenen `CommandInput` ausgewertet
- `FighterInput` verodert Touch mit Tastatur/Gamepad — Charaktere merken nichts davon
- Der `Bootstrapper` erzeugt alles automatisch auf Handhelds
  (`touchControlsOnDesktop = true` zum Testen am PC)

Damit ist die letzte **Code**-Lücke für einen spielbaren APK geschlossen; ab hier
hängt alles an Punkt 1–5 der Tabelle oben, und das ist Editor-/Art-Arbeit.

---

## 🚀 Empfohlene Reihenfolge

1. Unity-Projekt (URP) anlegen, `Assets/Scripts`, `Assets/Shaders`, `Assets/Plugins` hineinkopieren
2. `Tools → Penner Kombat → Setup-Szene erzeugen`
3. **Ein** Charakter-Prefab bauen (Capsule reicht) und in `FighterDatabase` eintragen
4. Play drücken → `TestRunner`-Log prüfen (✅/❌) → Fehler an mich zurückmelden
5. Erst danach Art, Audio und die restlichen acht Prefabs
