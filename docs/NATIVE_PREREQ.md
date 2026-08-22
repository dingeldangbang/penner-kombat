# 📱 Native App — Voraussetzungen & Bereitstellung

Zwei native Wege sind im Repo vorbereitet:

1. **Unity Android (APK/AAB)** — vollwertige native App, beste Performance, Vibration, Offline
2. **Web 3D als native Wrapper (Capacitor)** — nutzt `web/3d.html` (Le Binde vs Mojo Bob Pose-System) als native Hülle

Beide Wege sind 100% spielbar ohne Art-Assets (Kapsel-Platzhalter + prozedurale VFX/Audio).

---

## A — Unity Android Native App

### 1. Unity Editor

| Voraussetzung | Version / Details | Prüfung |
|---|---|---|
| **Unity** | **2022.3.62f1 LTS** (steht in `ProjectSettings/ProjectVersion.txt`) — Unity 6 migriert automatisch | `Tools → Penner Kombat → ✔ Spielbereitschaft prüfen` |
| **Module** | Android Build Support + OpenJDK + Android SDK & NDK Tools (im Hub Haken setzen) | Hub → Installs → Zahnrad → Add Modules |
| **Render Pipeline** | URP 14.0.11 (liegt in `Packages/manifest.json`) | Package Manager |
| **Pakete** | `com.unity.inputsystem 1.7.0`, `com.unity.textmeshpro 3.0.6`, `com.unity.render-pipelines.universal 14.0.11`, `com.unity.ugui 1.0.0` | `Packages/manifest.json` |
| **Active Input Handling** | **Both** (Legacy + Input System) — sonst wirft `FighterInput` Exception | `Project Settings → Player → Active Input Handling` — `PkProjectSetup` setzt es automatisch auf Both |
| **TextMeshPro Essentials** | einmalig importieren, sonst HUD leer | `Window → TextMeshPro → Import TMP Essential Resources` |

### 2. Android SDK / NDK / JDK

| Tool | Empfohlen | Hinweis |
|---|---|---|
| **JDK** | 17 (kommt mit Unity Android Modul) | `java -version` |
| **Android SDK** | API 24 (min) bis 34 (target Auto) | `sdkmanager --list` |
| **NDK** | r23c+ (Unity 2022.3 nutzt NDK 23) | wird mit Modul installiert |
| **Gradle** | 8.x (Unity liefert passendes) | nicht manuell nötig |

Unity Hub installiert alles — du musst nichts manuell laden, wenn du das Modul *Android Build Support* mit *OpenJDK + SDK & NDK* wählst.

### 3. Projekt-Setup (automatisch, aber prüfbar)

Diese Schritte erledigt `PkProjectSetup` + `PkBuildPreparer` beim ersten Öffnen und vor jedem Build:

| Check | Was | Woher |
|---|---|---|
| **Tags** | `Ground, Fighter, Interactable, Projectile` | `TagManager.asset` — `PkProjectSetup.EnsureTag` |
| **Layer** | `Fighter (8), Interactable (9), Projectile (10)` | `TagManager.asset` — `PkProjectSetup.EnsureLayer` |
| **Defines** | `PK_URP` wenn URP installiert, `PK_GLTFAST` wenn glTFast installiert | `PlayerSettings.GetScriptingDefineSymbols` |
| **Input** | Active Input Handling = Both | `ProjectSettings/ProjectSettings.asset` |
| **Datenbank** | `Assets/Resources/FighterDatabase.asset` mit 9 Charakteren | `PkQuickStart.LoadOrCreateDatabase()` |
| **Szene** | `Assets/Scenes/Arena.unity` mit Ground Plane (Tag Ground), SpawnPoint1 (-4,0,0), SpawnPoint2 (4,0,0), Boot (Bootstrapper) | `GameSetupWizard.CreateArenaScene()` |
| **Build Settings** | Arena als erste Szene eingetragen, sonst leerer Build | `PkBuildPreparer.EnsureScene` |
| **Quality** | MSAA aus, Schatten 35m, VSync aus | `PkBuildPreparer.ConfigureQuality` |

Manuell prüfen: `Tools → Penner Kombat → ✔ Spielbereitschaft prüfen` → zeigt ✅/❌ für jeden Punkt.

### 4. Player Settings Android (vom Build-Preparer gesetzt)

| Bereich | Wert | Grund |
|---|---|---|
| Company / Product | Dingelang Games / Penner Kombat | Manifest |
| Package Name | `com.dingelanggames.pennerkombat` | Play Store eindeutig |
| Orientation | Landscape Left+Right, Portrait verboten | Fighter braucht Querformat |
| Graphics API | Vulkan, Fallback OpenGLES3 | Performance + Kompatibilität |
| Min API | 24 (Android 7) | 99% Geräte |
| Target API | Auto (höchste) | Play Store verlangt ≥34 |
| Scripting Backend | IL2CPP | Pflicht für ARM64 |
| Architectures | ARM64 + ARMv7 | ARM64 Pflicht, ARMv7 für alte Geräte |
| Managed Stripping | Low | High bricht `BroadcastMessage` in `Mell.Defi` + Debuffs |
| Permissions | INTERNET, ACCESS_NETWORK_STATE, VIBRATE | aus `AndroidManifest.xml` |
| Features | touchscreen optional, gamepad optional, vulkan optional | Manifest |

### 5. AndroidManifest.xml

Liegt in `Assets/Plugins/Android/AndroidManifest.xml`:

- `INTERNET` + `ACCESS_NETWORK_STATE` für Relay (`WebSocketClient`)
- `VIBRATE` für Touch-Haptik
- `largeHeap=true`, `hardwareAccelerated=true`, `usesCleartextTraffic=true` (für `ws://` Relay)
- Activity `UnityPlayerActivity` Landscape sensor, configChanges für Rotation
- `splash-mode 0` (kein Unity Splash in Personal?)

### 6. Build-Wege

#### Lokal (eigener Rechner)

```
Tools → Penner Kombat → Build vorbereiten
Tools → Penner Kombat → Build/Android APK (Debug) oder Release oder AAB
```
oder CLI:

```bash
/Applications/Unity/Hub/Editor/2022.3.62f1/Unity.app/Contents/MacOS/Unity \
  -quit -batchmode -nographics \
  -projectPath "$PWD" \
  -buildTarget Android \
  -executeMethod PennerKombat.Editor.BuildScript.BuildAndroid \
  -logFile build.log
```

Output: `Builds/PennerKombat.apk` (ca. 40-70 MB).

#### CI (ohne eigenen Rechner)

Siehe `docs/ci/README.md` + `docs/HANDY.md`:

1. Unity Konto kostenlos anlegen
2. `docs/ci/unity-activation.yml` → Actions → .alf erzeugen → auf license.unity3d.com hochladen → .ulf Inhalt kopieren
3. Secrets setzen: `UNITY_LICENSE` (ulf Inhalt), `UNITY_EMAIL`, `UNITY_PASSWORD`
4. `docs/ci/android-build.yml` aktivieren → Actions → Android-APK bauen → Artefakt downloaden

Erster Lauf schlägt meist fehl — Log ist die To-Do Liste.

#### Signierung Release

- Player Settings → Publishing Settings → Keystore Manager → Create New
- Keystore + Passwörter **außerhalb Repo** ablegen, nie committen!
- Für Play Store AAB: `Build App Bundle` Haken, Keystore Base64 als Secret `ANDROID_KEYSTORE_BASE64`

### 7. Touch-Steuerung (native)

`TouchControls.cs` + `VirtualInput.cs` bauen On-Screen-Layout prozedural:

- Joystick links (130px Radius, Deadzone 0.18)
- Buttons rechts: □ leicht, △ schwer, ○ Spezial1, ✕ Sprung, BLOCK halten, S2, X-RAY
- Layouts: Standard, Fighting, Simple, LeftHanded — frei verschiebbar via `TouchLayoutEditor`
- Gesten: Wischfolgen → Numpad-Sequenzen → `CommandInput`
- Anti-Ghosting, InputBuffer, Haptik (Vibration 8ms)
- `MobileTuning`: erkennt Geräteklasse (Schwach/Mittel/Stark) via RAM/Kerne, setzt 30/60 fps, Schatten, Auflösung, deaktiviert Crowd/Allies

Ohne Touch-Layout wäre APK nur mit Bluetooth-Gamepad spielbar — das ist jetzt gelöst.

### 8. Was noch fehlt (Art)

| Fehlt | Workaround | Pfad für echtes Asset |
|---|---|---|
| Charakter-Modelle + Rigs | Kapsel-Platzhalter aus `FighterFactory` | `Assets/Models/Fighters/le_binde.glb` etc → Auto-Setup via `Tools → Penner Kombat → GLB → ...` oder F7 im Spiel |
| Animationen | Pose-System in 3D-Web, im Unity Idle/Walk via Code | `Assets/Animations/` + Animator Controller |
| Audio 31 Tracks | ProceduralAudio Fallback (Hit, Block, Hurt, 808 Kick, Victory) | `Assets/Resources/Audio/Music`, `/SFX` |
| Arena Meshes | Würfel-Platzhalter in `ArenaVisuals.BuildYard()` | `Assets/Prefabs/Arena/Props` |

Spiel ist auch ohne diese Assets 100% spielbar — nur hässlich.

---

## B — Web 3D als native App (Capacitor Wrapper)

Die neue `web/3d.html` ist bereits eine vollwertige 3D-Arena:

- Three.js 0.160.0 via ImportMap, keine Build-Tools nötig
- `pose.js`: 20 Posen (idle, walk/walk2, light_punch, heavy_punch, block, jump, roll, hit_react, death, flaschenhals, grosser_schwung, reif, mops, pfanne, schwanz, schwanz_ex, beutelchen, loeffelsturm, fuenfzig, dingeneldang, fatal) — Euler Winkel, lerp 0.35 Blend
- `fighter3d.js`: Baut Fighter aus Kapseln/Boxen/Spheres wie `FighterFactory`, Gelenke: hips, spine, chest, head, lShoulder, lElbow, rShoulder, rElbow, lHip, lKnee, rHip, rKnee, lAnkle, rAnkle, tail (6 Segmente für Mojo Bob)
- `arena3d.js`: Boden Circle 12m, Pflasterlinien, 4 Wände, Laternen SpotLight WarmOrange 800lm, Fill Gold, Mond Directional, Neon PointLight 50lm + CanvasTexture Schild, Props: Bierkasten-Turm, Gasflasche, Mülltonne, Wäscheleine, Paula-Napf, Gerüst, Partikel Mücken
- `sim3d.js`: Alle 6 Specials pro Charakter 100% spielbar mit Effekten (siehe Tabelle unten), Projektile (Flasche 9 m/s, Löffel 17 Stück, Münze 11 m/s), Buffs (REIF! +80% 6s, Mops +50% Krit 8s, Dingeneldang 100% Krit 8s dann Reue -30% 15s), Schmier-Schlüppa -60% + 45% Rutsch + 3 Ladungen + Burn nach 3 Feuer 8s, Mojo 15% Krit x2.4 + Punkte 0-7, Effekte bleed 1.2 DPS 5s, burn 2 DPS 4s, poison 1.5 DPS 5s, slow 0.6x 3s, invert 4s, stun 0.6-1.5s, launch, armor 0.3x
- `input3d.js`: 6 Specials P1: U,I,L,M,N,',' + J/K/Shift/Space/O/Y/H, P2: NumPad 4,5,7,8,9,+/1,2,3,0,6,*,Enter + Touch 12 Buttons
- `render3d.js`: Kamera dynamisch Mitte + Spread (wie CameraController.Dynamic), Shake, Hitstop Freeze, Burst Sparks, Floating DOM Text, Projectile Meshes, Hazard Decals
- PWA: `manifest.webmanifest` + `sw.js` offline, `web/3d.html` als Start

### Capacitor Setup (einmalig)

```bash
cd web
npm init -y
npm install @capacitor/core @capacitor/cli @capacitor/android
npx cap init "Penner Kombat 3D" com.dingelanggames.pennerkombat --web-dir=.
npx cap add android
# Android Studio öffnen
npx cap open android
```

Oder via bereitgestelltem Script: `Tools/capacitor_setup.sh`

Danach in Android Studio Build → APK.

Vorteile: nutzt fertige 3D-Arena, keine Unity nötig, 2 Spieler an einem Gerät möglich, Touch 12 Buttons.

Nachteile: WebView Performance ~60-70% von nativ Unity, kein Vulkan.

---

## C — Checklisten & Tools

### Automatischer Check (ohne Unity)

```bash
./Tools/check.sh               # Klammern + Symbol-Referenzen
./Tools/native_check.sh        # Native Voraussetzungen prüfen
```

`native_check.sh` prüft:

- Unity Version (2022.3.62f1) vorhanden?
- `Packages/manifest.json` + `ProjectSettings/` vorhanden?
- `AndroidManifest.xml` vorhanden + Permissions?
- Tags/Layers in `TagManager.asset`?
- Szenen vorhanden? `Arena.unity`?
- `FighterDatabase` vorhanden?
- Node + npm für Relay + Capacitor?
- Three.js ImportMap in `3d.html`?
- `web/src/*.js` Syntax ok?
- `sw.js` enthält 3D Dateien?

### In Unity

```
Tools → Penner Kombat → ✔ Spielbereitschaft prüfen
```
Zeigt ✅/❌ für Render-Pipeline, Input System, TMP, Tags, Layer, Datenbank, Szenen, Bootstrapper, glTFast.

```
Tools → Penner Kombat → Build vorbereiten
```
Setzt alles für Android: Tags, Layer, Defines, Datenbank, Szene in Build Settings, Player Settings (PackageName, minSdk 24, IL2CPP, ARM64+ARMv7, Vulkan+OpenGLES3, Landscape, Stripping Low).

```
Tools → Penner Kombat → ▶ Alles einrichten und spielen
```
Erzeugt Szene, 9 Platzhalter-Prefabs `PK_<id>.prefab`, Materialien, trägt in Datenbank ein, startet Play → Le Binde (WASD/J/K/Shift/Space) vs Mojo Bob KI.

---

## D — 2-Spieler Le Binde vs Mojo Bob — 100% spielbare Kombos

| Charakter | Move | Pose | Schaden | Cooldown | Reichweite | Effekt 100% |
|---|---|---|---|---|---|---|
| **Le Binde** | Flaschenhals | flaschenhals | 11 | 5s | 3.5m | Projektil Flasche 9m/s + Bleed 5s 1.2DPS |
| | EX-Flaschenhals | flaschenhals | 15 | 6s | 3.8m | Bleed heavy 2DPS + Knock |
| | Großer Schwung | grosser_schwung | 16 | 7s | 3.2m | Armor Frames 8-20 (0.3x), Wallbounce Staub + Shake 12 |
| | REIF! | reif | 0 | 8s | 0 | Buff +80% nächster Treffer 6s |
| | Mops-Kommando | mops | 0 | 22s | 6m | +50% Krit 8s, kippt alle Props, Neon Panik 2.5s, Staub 6x |
| | Aus der Pfanne | pfanne | 14 | 6s | 2.9m | Anti-Air Launch, verbraucht Fett-Ladung |
| **Mojo Bob** | Riesenschwanz | schwanz | 12 | 6s | 4.5m | Längste Reichweite, Tail-Pose + Wag, Krit 15% x2.4 |
| | EX-Riesenschwanz | schwanz_ex | 18 | 7s | 5.0m | 18 Frames aktiv, Multi-Hit 3x, Tail Sinus 15Hz |
| | Beutelchen | beutelchen | 9+Random | 8s | 3.2m | Gamble: 5% Klavier 25 DMG + Stun 1.5s, sonst Heal 10, Burn, Poison, Slow, Invert, Mojo+1 |
| | Löffelsturm | loeffelsturm | 17x1.2 | 10s | 6.5m | 17 Löffel Projektile 8-12 m/s + Spread 0.6, jedes Krit-fähig |
| | Fünfzig Cent | fuenfzig | 8 | 5s | 5.5m | Münz-Projektil 11 m/s Gold emissive |
| | Dingeneldang! | dingeneldang | 0 | 20s | 0 | 7 Mojo -> 100% Krit 8s, danach Reue -30% 15s, Gold Burst 30 Sparks |

**System:** Combo-Fenster 2.0s, Meter +12 Hit / +8 Taken, Fatal Blow 28-44, Med 2x +25 nach 1.2s, Block 78% Reduktion, Rolle 0.2s invuln, Knockback 8 + Up 3, Gravity 26, Jump 9.5, Arena Radius 12, Stature Masse 0.85-1.45 HitboxScale 0.85-1.35.

Alle Moves haben eigene Pose aus `pose.js` + eigene Three.js Waffe (Flasche grün transparent, Pfanne metallisch, Löffel silber).

---

## E — Bereitstellung

### Web 3D (sofort spielbar)

```
cd web
python3 -m http.server 8000
# → http://localhost:8000/3d.html
```

Live Preview: via Arena Tool (Port 8000) bereits gestartet.

### Unity APK

```
Tools → Penner Kombat → Build/Android APK (Release) → Builds/PennerKombat.apk
```

### Capacitor APK (Web Wrapper)

```bash
./Tools/capacitor_setup.sh
cd web
npx cap open android
# In Android Studio: Build → Build Bundle(s) / APK(s)
```

### GitHub CI

- `docs/ci/android-build.yml` → APK Artefakt
- `docs/ci/webgl-pages.yml` → GitHub Pages `https://dingeldangbang.github.io/penner-kombat/`

---

## F — Minimaler Rechner?

Siehe `docs/HANDY.md`: ohne Unity → nur Browser nötig, CI baut APK.

1. Unity Konto kostenlos
2. `unity-activation.yml` → .alf → .ulf → Secret `UNITY_LICENSE`
3. `android-build.yml` → Run workflow → APK downloaden

Fertig.
