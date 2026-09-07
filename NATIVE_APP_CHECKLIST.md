# Native App Checklist — Penner Kombat

**Stand:** 2026-09-06 — **Godot 4.7.2 (Android 11–15 / API 30–35)** — Unity-Abschnitt ist Altbestand (Migration abgeschlossen).

## ✅ Android 11–15 — Godot 4.7 (aktuell)

- `export_presets.cfg` → Presets `Android` (Debug-APK) + `Android AAB` (Gradle: minSdk 30, targetSdk 35, 16-KB-Page-Size)
- `project.godot` → `[android]` minSdk 30 / targetSdk 35, Permissions INTERNET, ACCESS_NETWORK_STATE, VIBRATE, WAKE_LOCK, sensorLandscape
- `Tools/android_setup.sh` → OpenJDK 17, SDK Platform 35+36, Build-Tools 35.0.1+36.1.0, NDK r29, CMake 3.22.1
- `Tools/install_android_build_template.sh` → installiert `res://android/build` aus den Export-Templates (`--relay` für `ws://`)
- `Tools/android_check.sh` → Bereitschaftscheck (APK/AAB, Permissions, 16 KB, Toolchain)
- `ci/android-apk-workflow.yml` → `barichello/godot-ci:4.7.2`, APK + optionales AAB (workflow_dispatch `build_aab`) + optionaler Emulator-Smoke-Test (API 30–35)
- **GitHub CLI/CD/CI:** `Tools/gh_android.sh` → Workflow aktivieren (`activate`), bauen (`build apk|aab|all`), Emulator-Test (`emulator 30–35`), Artefakte laden (`download`), Release (`release <tag>`) und installieren (`install`) — ohne lokales SDK, Details `docs/GH_CLI_CI.md`
- Relay-Anbindung: `wss://` empfohlen; `ws://` nur mit `--relay` (Cleartext) im Gradle-Build
- Details & Verifikation: **`docs/ANDROID_11_15.md`** · Build: `docs/APK_BUILD_READY.md` · gh-Weg: `docs/GH_CLI_CI.md`



## ✅ Was bereits bereitgestellt ist

### Unity Native (APK/AAB)
- `ProjectSettings/ProjectVersion.txt` → 2022.3.62f1
- `Packages/manifest.json` → URP 14.0.11, InputSystem 1.7.0, TMP 3.0.6
- `ProjectSettings/TagManager.asset` → Tags Ground/Fighter/Interactable/Projectile, Layers Fighter/Interactable/Projectile
- `Assets/Plugins/Android/AndroidManifest.xml` → INTERNET, VIBRATE, ACCESS_NETWORK_STATE, Landscape, largeHeap, Vulkan optional
- `Assets/Scripts/Editor/` → PkProjectSetup (Tags/Layers/Defines), PkBuildPreparer (Szene in Build Settings, Player Settings), PkQuickStart (9 Platzhalter-Prefabs PK_<id>), GameSetupWizard (Arena Szene), PkReadinessCheck (✅/❌), BuildScript (APK/AAB/WebGL)
- `Assets/Scripts/Core/MobileTuning.cs` → Geräteklasse Schwach/Mittel/Stark/Desktop via RAM/Kerne, 30/60 fps, Schatten, Auflösung
- `Assets/Scripts/UI/TouchControls.cs` + Touch/ → Joystick, Buttons, Gesten, Layout-Editor, Anti-Ghosting, InputBuffer, Tutorial, Debug
- `Tools/native_check.sh` → prüft alles ohne Unity

### Web 3D Native Wrapper (Capacitor)
- `web/3d.html` → Le Binde vs Mojo Bob, Pose-System, Three.js ImportMap 0.160.0, 2 Spieler, alle Kombos
- `web/src/pose.js` → 20 Posen, JOINTS 15, lerpPose, selectPose
- `web/src/fighter3d.js` → FighterMesh3D aus Primitives wie FighterFactory, tail 6 Segmente
- `web/src/arena3d.js` → Boden 12m, Wände, Laternen WarmOrange 800lm, Fill Gold, Mond, Neon 50lm, Props 6x, Partikel
- `web/src/sim3d.js` → 6 Specials je Charakter 100% spielbar, Projektile, Buffs, Schmier-Schlüppa, Mojo, Effekte
- `web/src/render3d.js` → Kamera Dynamic Mitte+Spread, Shake, Hitstop, Burst, Floater, Projectile Meshes
- `web/src/input3d.js` → 6 Specials P1 U,I,L,M,N,',' + P2 NumPad, Touch 12 Buttons
- `web/src/main3d.js` → Loop 60 FPS, HUD mit Mojo/Fett/Buffs, Events
- `web/package.json` → three 0.160.0, capacitor core/android/cli 5.7.0
- `web/manifest.webmanifest` + `sw.js` v2 mit 3D Dateien (PWA offline)
- `Tools/capacitor_setup.sh` → init + add android + sync

## 📋 Voraussetzungen

### Unity Android
- Unity Hub + Editor 2022.3.62f1 LTS + Android Build Support + OpenJDK + SDK & NDK
- JDK 17, SDK API 24-34, NDK r23c+, Gradle 8.x (kommt mit Modul)
- Kein PlayStation/Xbox SDK nötig

### Web Native
- Node 18+ + npm 9+ (aktuell Node 22.22.3 vorhanden)
- Python3 für `http.server` (vorhanden)
- Android Studio für APK (optional)

## 🚀 Build Befehle

```bash
# Check
./Tools/check.sh
./Tools/native_check.sh

# Unity in Editor
Tools → Penner Kombat → ✔ Spielbereitschaft prüfen
Tools → Penner Kombat → ▶ Alles einrichten und spielen
Tools → Penner Kombat → Build/Android APK (Release)

# Unity CLI
Unity -quit -batchmode -nographics -projectPath . -buildTarget Android -executeMethod PennerKombat.Editor.BuildScript.BuildAndroid

# Web 3D
python3 -m http.server 8000 --directory web
# → http://localhost:8000/3d.html

# Capacitor
./Tools/capacitor_setup.sh
cd web && npx cap open android
# In Android Studio: Build APK
```

## 🎮 Le Binde vs Mojo Bob — 100% spielbar

Siehe `docs/NATIVE_PREREQ.md` Abschnitt D für Tabelle aller 12 Moves + Posen + Effekte.

**Kurz:** Flaschenhals Projektil + Bleed, EX, Großer Schwung Armor + Wallbounce, REIF! Buff +80%, Mops-Kommando kippt Arena + Krit, Pfanne Launch / Riesenschwanz 4.5m + Tail, EX Multi, Beutelchen Gamble 5% Klavier, Löffelsturm 17 Projektile, Fünfzig Cent Münze, Dingeneldang 7 Mojo → 100% Krit 8s.

Alle mit eigener Pose aus pose.js + Three.js Waffe.

## 🔗 Links

- `docs/NATIVE_PREREQ.md` → ausführlich
- `docs/BUILD_ANDROID.md` → Android
- `docs/HANDY.md` → Handy + CI
- `docs/ci/android-build.yml` → GitHub Actions APK
- `web/3d.html` → 3D Arena Live

Live Preview läuft auf Port 8000 (siehe Arena Tool).
