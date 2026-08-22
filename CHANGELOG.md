# Changelog

## [Unveröffentlicht]
### Browser-Fassung als Alternative (`web/`)
- **Vollständig spielbare Web-Version** ohne Unity: Hauptmenü, Charakterauswahl aller 9 Kämpfer,
  KI-Stufen, Runden, HUD, 2,5D-Canvas-Darstellung, Touch-Steuerung (Joystick + 9 Buttons),
  synthetisierter Ton inklusive 808-Beat, dessen Tempo die Combo treibt.
- **Kampfwerte Zahl für Zahl aus dem C#-Code gespiegelt** (`web/src/data.js` nennt je Block die
  Quelldatei): Roster, Statur-Tabelle, 78-%-Block, Combo-Fenster, Rolle mit i-Frames, Fatal Blow,
  Med-Kapsel, alle sechs KI-Profile.
- **`web/test/sim.test.mjs`**: 11 Tests, alle grün (`node --test web/test/sim.test.mjs`) — damit ist
  die Kampflogik erstmals in diesem Projekt tatsächlich ausgeführt und geprüft.
- **Neue Doku `docs/WEB.md`** mit ehrlichem Abgleich, was identisch ist und was fehlt.

### Handy als Hauptplattform
- **`Editor/PkBuildPreparer.cs`**: bereitet Builds automatisch vor — im Batchmode (CI) ohne
  Zutun. Legt die Arena-Szene an und trägt sie in die **Build Settings** ein (ohne das baut
  Unity ein leeres Spiel), setzt Paketname, min. Android 7, IL2CPP, ARM64+ARMv7, Vulkan/GLES3,
  Querformat, Vollbild sowie WebGL-Kompression und gedeckelte Qualitätseinstellungen.
- **`Core/MobileTuning.cs`**: stuft das Gerät nach RAM und Kernen ein (Schwach/Mittel/Stark) und
  passt Bildrate, Schatten, Auflösungsskalierung und teure Extras an; Bildschirm schläft im
  Kampf nicht ein.
- **`FrontEnd`** zeigt auf Handhelds Touch-Hinweise statt Tastenbelegung.
- **Neue Doku `docs/HANDY.md`**: WebGL gegen APK abgewogen, Einrichtung in fünf Schritten,
  Touch-Verhalten, Leistungsstufen, Sideload-Anleitung und die Grenzen.

### Bauen und spielen ohne eigenen Unity-Rechner
- **`docs/ci/unity-activation.yml`**: erzeugt die Unity-Aktivierungsdatei (`.alf`) direkt auf
  GitHubs Runnern — kein lokales Unity, kein Docker nötig.
- **`docs/ci/webgl-pages.yml`**: baut die WebGL-Fassung und veröffentlicht sie auf GitHub Pages;
  spielbar im Browser auf Handy, Tablet oder fremdem Rechner.
- `docs/ci/README.md` beschreibt den kompletten Weg ohne lokalen Editor sowie die Grenzen der
  WebGL-Fassung (kein UDP → keine LAN-Erkennung, Relay funktioniert; kein Multithreading).
- `docs/SPIELEN.md` §1d: was zu tun ist, wenn der eigene Rechner keine Metal-GPU hat.

### macOS-Hinweise
- `docs/SPIELEN.md` und `docs/BUILD_ANDROID.md` halten fest, dass **macOS Big Sur 11.x genügt**
  (Unity 2022.3 LTS: Mojave 10.14+ auf Intel, Big Sur 11.0+ auf Apple Silicon), inklusive
  Hinweisen zu Rosetta 2, Mono-Build ohne Xcode und den nötigen Android-Modulen.

### Android-APK aus der CI
- **`docs/ci/android-build.yml`**: Workflow-Vorlage für `game-ci/unity-builder` — baut ein APK
  auf GitHubs Runnern (manuell oder bei jedem Tag `v*.*.*`, dann hängt es am Release).
  Library-Cache, Speicheraufräumen und optionale Keystore-Signierung sind vorbereitet.
- **`docs/ci/README.md`**: Schritt-für-Schritt zur Unity-Lizenzdatei, den drei Secrets und den
  typischen Fehlerbildern.
- **`docs/BUILD_ANDROID.md`**: zwei klar getrennte Wege (lokal / CI) und der Hinweis, dass die
  Projektdateien seit v1.4.0 im Repo liegen.

### Balance greift auch bei handgebauten Prefabs
- **`FighterController.ApplyConfig(FighterConfig)`**: überträgt HP, Tempo, Schaden und Reichweite
  aus der `FighterDatabase` auf gespawnte Kämpfer. Vorher galten bei eigenen Prefabs die Werte,
  die zufällig im Inspector standen — Balance-Änderungen in der Datenbank blieben wirkungslos.
  Abschaltbar pro Prefab über `ignoreConfigBalance`.
- **`FighterController.InterruptAttack()`**: bricht einen laufenden Angriff sauber ab
  (Invokes stoppen, Trefferliste leeren, `Interrupt`-Trigger) — für Konter, Fatal Blow, Rundenende.

### Frontend und Ton
- **`UI/FrontEnd.cs`** (Taste **F1**): prozedurales Hauptmenü (Versus · Training · Modelle · Beenden)
  und Charakterauswahl mit allen 9 Kämpfern für P1 und P2, Umschalter Mensch/KI, KI-Stufe
  (Sehr leicht bis Boss) und Rundenzahl 1/3/5. Kein Prefab, keine zweite Szene nötig.
- **`GameManager`**: `autoStart`-Schalter, Überladung `StartVersusFight(p1, p2, p2IsAI)`,
  `aiDifficulty` wird beim Anhängen des `AIController` angewendet, `RestartMatch()` behält
  Charakterwahl und Gegnertyp bei.
- **`Core/ProceduralAudio.cs`**: synthetisierte SFX aus Code — Treffer leicht/schwer, Block,
  Schmerzlaut, UI-Klick, 808-Kick und eine schäbige Siegesfanfare.
- **`AudioManager`** füllt leere SFX-Listen damit auf (`useProceduralFallback`), sobald echte
  Clips zugewiesen sind, greift der Fallback nicht mehr.

### Werkzeuge zur Absicherung
- **`Tools/check_symbols.py`**: statische Referenzprüfung ohne Unity — findet Aufrufe auf
  projekteigene Typen, deren Mitglied es nicht gibt, unbekannte generische Typparameter und
  fehlende Basisklassen. Läuft aktuell sauber über 120 Dateien / 171 Typen.
- **`Tools/check_braces.py`** + **`Tools/check.sh`**: Klammer-/String-Bilanz und Sammellauf.
- **`Editor/PkReadinessCheck.cs`**: Menü *✔ Spielbereitschaft prüfen* — acht Prüfungen
  (Render-Pipeline, Input System, Active Input Handling, TMP, Tags, Layer, Datenbank, Szene,
  glTFast) mit konkretem Menüpfad zur Behebung und Urteil „SPIELBEREIT" oder nicht.

### Statur als Balance-Größe
- **`FighterConfig.stature`** (`Hager`, `Normal`, `Breit`, `Adipoes`) plus zentrale `StatureTable`
  für Höhe, Kapselradius, Masse und Hitbox-Faktor — eine Quelle für alle Systeme.
- Standard-Roster belegt: Le Binde adipös, Mell und Sigi hager, Dieter/TetraPak/Kalle breit.
- Wirkt auf Platzhalter-Kapseln (Maße, Proportionen, AttackPoint-Position) **und** auf importierte
  Modelle (Zielhöhe, Masse, Mindest-Radius, Angriffs-Hitbox).

### Ragdoll-Automatik
- **`Editor/FighterRagdollBuilder.cs`**: erzeugt aus einem Humanoid-Rig ein komplettes Ragdoll
  (12 Knochen: Hüfte, Spine, Chest, Kopf, Ober-/Unterarme, Ober-/Unterschenkel) mit Rigidbodies,
  passenden Collidern (Box/Sphere/Capsule aus der Knochenlänge) und `CharacterJoint`-Limits.
- Knochen starten kinematisch mit deaktivierten Collidern — `RagdollController` schaltet sie beim
  K.o. scharf und beim Rundenstart wieder ab; ohne Rig bleibt es beim Primitiv-Fallback.
- Läuft automatisch im Auto-Setup mit; Menü zum Nachrüsten:
  *GLB → Ragdoll für alle Kämpfer bauen*.

### Auto-Setup: benannte Sockets, Waffenhand, Überschreibschutz
- **`AttackPoint` und `WeaponSlot` aus dem Modell** werden bevorzugt verwendet, wenn sie als
  Kind-Objekte existieren; sonst greift die Rig-Erkennung, sonst ein Fallback mit Warnung.
- **`WeaponHolder.socket`**: Waffen erscheinen jetzt in der Hand statt an der Hüfte, sobald ein
  Slot bekannt ist (`WeaponSlot`-Kind oder Handknochen).
- **`Core/PkAutoSetupInfo.cs`**: Merkzettel auf jedem erzeugten Prefab (Quellmodell, ID, Datum,
  Version) mit Schalter **Lock Manual Edits** — gesperrte Prefabs überspringt der Import-Wächter.

### Animator-Controller aus Clips generieren
- **`Editor/FighterAnimatorBuilder.cs`**: baut pro Charakter einen Animator-Controller
  (`Assets/Animations/Controllers/PK_<id>.controller`) mit allen Parametern, die der Kampfcode
  ansteuert, und der Zustandsmaschine `Idle ⇄ Walk`, `Block`-Halten sowie Any-State-Aktionen
  (`LightAttack`, `HeavyAttack`, `Jump`, `Roll`, `HitReact`, `Death`, `Special`).
- **Clip-Zuordnung über Schlüsselwörter** im Dateinamen (Mixamo-tauglich: „Punching",
  „Falling Back Death", „Standing Melee Attack …"); Quellen sind das Modell selbst und
  `Assets/Animations`. T-Pose-Clips werden übersprungen.
- Der Auto-Setup-Wächter hängt den generierten Controller direkt an das Kämpfer-Prefab;
  Menü zum Neubauen: *GLB → Animator-Controller für alle Kämpfer bauen*.
- **`FighterController`** prüft Animator-Parameter jetzt vor dem Setzen (`AnimTrigger`,
  `AnimBool`, `AnimFloat`) — fremde Controller lösen keine Warnungsflut mehr aus.

### Modelle automatisch spielfertig machen
- **`Editor/FighterAutoSetup.cs`**: baut aus einem Modell einen kompletten Kämpfer —
  Charakterskript nach ID, Balance-Werte aus der Datenbank, Rigidbody, CapsuleCollider aus
  den **echten Modellmaßen**, `AttackPoint` am **rechten Handknochen** (Humanoid-Avatar oder
  Knochennamen à la `mixamorig:RightHand`), Hitbox-Maße, Animator-Übernahme, Tag/Layer,
  Prefab-Ablage und Eintrag in die `FighterDatabase`.
- **Import-Wächter `FighterModelPostprocessor`**: jede neue Datei in `Assets/Models/Fighters`
  wird sofort verarbeitet (`.glb`, `.gltf`, `.fbx`); vorhandene Prefabs werden aktualisiert
  statt dupliziert. FBX wird beim Erstimport gleich auf Humanoid gestellt.
- Neue Menüpunkte: *Automatik: neue Modelle sofort einrichten* (Häkchen) und
  *Ausgewähltes Modell zu Kämpfer machen*.
- `GlbImportWizard` nutzt jetzt dieselbe Bauroutine — keine zweite, abweichende Implementierung.

### GLB-Modellimport über Menüs
- **`Utils/GlbLibrary.cs`**: findet `.glb`/`.gltf` in `persistentDataPath/Models`,
  `StreamingAssets/Models` und `<Projekt>/Models`, ordnet Modelle Charakteren zu
  (PlayerPrefs `pk_model_<id>` inkl. Skalierung, Drehung, Höhenversatz), Auto-Zuordnung
  über Dateinamen.
- **`Core/GlbModelLoader.cs`**: lädt Modelle zur Laufzeit (glTFast, abgesichert über
  `#if PK_GLTFAST`), normiert sie auf 1,80 m, stellt sie auf den Boden, dreht sie nach vorn,
  blendet die Kapsel aus und kann im laufenden Kampf austauschen (`RefreshAll`).
- **`UI/ModelMenuUI.cs`** (Taste **F7**): prozedurales Menü mit allen neun Charakteren,
  Modellwechsel per `<`/`>`, Größe ±5 %, 90°-Drehung, Leeren, Auto-Zuordnen, Übernehmen,
  Ordner zeigen.
- **`Editor/GlbImportWizard.cs`**: `Tools → Penner Kombat → GLB → …` — glTFast per Package
  Manager installieren, Modell-Ordner anlegen, aus GLB fertige Kämpfer-Prefabs bauen
  (Physik, Charakterskript, AttackPoint, Animator-Übernahme) und in die FighterDatabase
  eintragen, zurück zu Platzhaltern.
- **`PkProjectSetup`** setzt/entfernt das Define `PK_GLTFAST` automatisch.
- **Neue Doku** `docs/MODELLE.md`.

### Spielbar ohne Handarbeit (MVP)
- **Unity-Projektdateien ergänzt**: `Packages/manifest.json` (URP, Input System, TMP, uGUI),
  `ProjectSettings/ProjectVersion.txt` und `TagManager.asset` (Tags `Ground`/`Fighter`/
  `Interactable`/`Projectile`, gleichnamige Layer). Das Repo öffnet damit direkt als Unity-Projekt.
- **`Core/FighterFactory.cs`**: baut komplette, spielbare Kämpfer aus Code (Kapsel-Körper mit Kopf,
  Armen und Blickrichtung, Rigidbody, Collider, AttackPoint, Charakterskript, Palettenfarben).
  Greift immer dann, wenn in der `FighterDatabase` kein echtes Prefab hinterlegt ist.
- **`UI/HudBuilder.cs`**: HUD zur Laufzeit — HP-Balken, Fatal-Blow-Leisten, Timer, Rundenanzeige,
  Combo-Texte, Namen, Steuerungs-Hinweis, Ergebnis-Panel mit Revanche-Knopf, EventSystem.
- **`Bootstrapper`** legt bei Bedarf Boden (30×30 m) und vier Begrenzungswände an und erzeugt HUD
  sowie Frame-Daten-Overlay.
- **`GameManager.EnsureDependencies()`**: Datenbank, Spawn-Punkte, HUD, Kamera, Arena und Audio
  werden selbst beschafft — ein Match startet auch in einer nackten Szene.
- **Treffer-Fix**: ein leerer `enemyLayer` bedeutete bisher, dass *kein* Angriff jemals trifft.
  `FighterController.Awake()` setzt jetzt einen sinnvollen Fallback und legt fehlenden Collider,
  Rigidbody und AttackPoint an.
- **`Training/FrameDataOverlay.cs`** (Taste **F4**): Startup/Active/Recovery in Frames, Block-,
  Rollen- und i-Frame-Status, Combo, Abstand, Fatal-Blow-Stand; Hitbox-Gizmos in der Szenenansicht.
- **`Editor/PkProjectSetup.cs`**: richtet Tags, Layer, Define `PK_URP`, TMP-Grundressourcen und
  *Active Input Handling = Both* beim ersten Öffnen automatisch ein.
- **`Editor/PkQuickStart.cs`**: `Tools → Penner Kombat → ▶ Alles einrichten und spielen` —
  Projekt-Setup, Szene, 9 Platzhalter-Prefabs inkl. Material-Assets, Datenbank-Zuweisung, Play.
  Roster-Einträge werden als Unter-Assets gespeichert (überlebten vorher keinen Editor-Neustart).
- **Neue Doku** `docs/SPIELEN.md` (MVP-Anleitung, Steuerungstabelle, Fehlerbehebung, Modell-Umstieg).

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
