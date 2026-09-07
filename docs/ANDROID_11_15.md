# 📱 Android 11–15 (API 30–35) — Abhängigkeiten & Anbindungen

**Stand: 2026-09-06 · Engine: Godot 4.7.2 (Forward+/Mobile) · Ziel: Läuft auf
Android 11, 12, 13, 14 und 15 (API 30–35), debug-signiert per CI, AAB für
Google Play vorbereitet.**

---

## 1. Ziel-Matrix

| Android | API | minSdk 30 ✓ | targetSdk 35 ✓ | Hinweis |
|---|---|---|---|---|
| 11 (Red Velvet Cake) | 30 | ✅ | ✅ | Scoped Storage Pflicht |
| 12 / 12L | 31–32 | ✅ | ✅ | Exportierte Apps ink. Split-Screen |
| 13 | 33 | ✅ | ✅ | Benachrichtigungs-Runtime-Prompts (nicht genutzt) |
| 14 | 34 | ✅ | ✅ | Immer sichtbare „Notify“-Toggle (nicht genutzt) |
| 15 (Vanilla Ice Cream) | 35 | ✅ | ✅ | Edge-to-edge Pflicht → Immersive-Modus |

- **minSdk 30** = Android 11 ist die Untergrenze.
- **targetSdk 35** = Android 15. Godot 4.7 unterstützt bis API 36 (Android 16);
  ein späterer Schritt zu `target_sdk="36"` in `export_presets.cfg` ist damit
  nur eine Zeile (siehe §7 „Gleichwertige Alternativen“).
- Prebuilt-Template-Export (APK, Preset „Android“): min/target stammen aus dem
  Godot-4.7-Template (minSdk 24, targetSdk 36) und decken API 30–36 ab.
  **Gradle-Build (AAB, Preset „Android AAB“):** minSdk 30 / targetSdk 35 greifen
  exakt aus `export_presets.cfg`.

---

## 2. Abhängigkeiten (mit Versionen)

| Komponente | Version | Wofür |
|---|---|---|
| **Godot** | **4.7.2** (empfohlen, mind. 4.5) | targetSdk 35, 16-KB-Page-Size, NDK r29 Template |
| **OpenJDK** | **17** | Godot-Android-Gradle-Template (`config.gradle`) |
| **SDK Platform-Tools** | 35.0.0+ | adb, Installation |
| **Build-Tools** | 35.0.1 + 36.1.0 | apksigner/zipalign (35), Gradle-Compile (36.1.0) |
| **SDK Platforms** | android-35 + android-36 | target 35, compileSdk 36 |
| **NDK** | **29.0.14206865** (r29) | Gradle-Build der nativen Libs |
| **CMake** | 3.22.1 | vom SDK-Template referenziert |
| **AGP / Kotlin** | 8.6.1 / 2.1.21 | kommt mit dem Godot-Build-Template |
| **Gradle** | Wrapper im Template (8.10+) | automatisch geladen |
| **CI-Image** | `barichello/godot-ci:4.7.2` | Godot + Templates + SDK 33 + JDK 17 + Debug-Keystore |

`config.gradle` in Godot 4.7.2: compileSdk 36, minSdk 24, targetSdk 36,
buildTools 36.1.0, NDK 29.0.14206865, Java 17 — alle Werte müssen für den
Gradle-Build im SDK vorhanden sein, sonst lädt AGP sie nicht automatisch herunter.

## 3. Einrichtung (einmalig)

```bash
# 1) Lokale Toolchain (Linux/macOS/WSL):
./Tools/android_setup.sh
#    → OpenJDK-17-Check, SDK-Download (platform-tools, build-tools 35.0.1+36.1.0,
#      platforms 35+36, cmake 3.22.1, NDK r29) und Godot-Editor-Settings

# 2) Prüfen:
./Tools/android_check.sh

# 3) AAB-/Gradle-Vorbereitung (optional, für ws://-Relay mit --relay):
./Tools/install_android_build_template.sh --relay

# 4) Baue APK:
godot --headless --path . --editor --quit
godot --headless --path . --export-debug "Android" build/PennerKombat-debug.apk

# 5) Baue AAB (Google Play):
godot --headless --path . --export-debug "Android AAB" build/PennerKombat.aab
#    Release mit eigenem Keystore (niemals committen!):
#    godot --headless --path . --export-release "Android AAB" build/PennerKombat.aab
```

**CI (ohne lokale Toolchain):** Der Workflow `ci/android-apk-workflow.yml`
(≥ `.github/workflows/android-apk.yml`) baut das APK bei jedem Push auf
`main`/`arena/**`, Tags und Pull Requests. Das AAB wird bei
`workflow_dispatch` mit Input **build_aab = true** zusätzlich gebaut
(SDK-36-/NDK-r29-Schritte sind im Job enthalten).

**GitHub-CLI-Alternative (empfohlen ohne lokale Toolchain):** alles über
`./Tools/gh_android.sh` (status / activate / build apk|aab|all /
emulator 30–35 / download / release / install) — Trigger, Watch und
Artefakt-Download per `gh`, Emulator-Smoke-Test auf API 30–35 in der CI.
Ausführlich: [docs/GH_CLI_CI.md](GH_CLI_CI.md).

## 4. Anbindungen (Integrations)

### 4.1 Manifest & Berechtigungen
Godot 4 generiert das Manifest selbst (Permissions aus dem Export-Preset).
Gesetzt und ausreichend für Android 11–15:

| Permission | Zweck |
|---|---|
| INTERNET | WebSocket-Relay, GLB-Downloads (`AdvancedAssetImporter`), OpenAI (`AiService`) |
| ACCESS_NETWORK_STATE | Netzwerkstatus |
| VIBRATE | Touch-Haptik |
| WAKE_LOCK | Bildschirm im Kampf aktiv |

Entfernt: `READ/WRITE_EXTERNAL_STORAGE` (seit Android 11 wirkungslos/unnötig —
Godot nutzt `user://` = Scoped Storage), Legacy-`requestLegacyExternalStorage`,
FileProvider-Konfiguration aus der Unity-Zeit. Kein Runtime-Prompt nötig
(alle Permissions sind „install-time normal“).

### 4.2 Relay / Multiplayer
- `scripts/network/NetworkManager.gd` + `WebSocketClient.gd` (Godot
  `WebSocketPeer`), Relay: `server/` (Node + `ws`, Port 8080, Raum
  `/kombat`).
- **Android 11–15, Produktion: `wss://` (TLS).** Keine Ausnahme nötig, keine
  Manifest-Patches.
- **Lokal/LAN (`ws://`):** ab API 28 blockiert Android Klartext.
  `./Tools/install_android_build_template.sh --relay` setzt
  `android:usesCleartextTraffic="true"` in `android/build/src/main/AndroidManifest.xml`
  (idempotent). Gilt nur für den Gradle-Build (AAB); für das Prebuilt-APK ist
  `wss://` die gleichwertige Alternative.

### 4.3 Android 15 — Edge-to-edge
Ab targetSdk 35 erzwingt Android 15 Edge-to-edge. Für ein Vollbild-Spiel ist
die korrekte Behandlung **Immersive-Modus** (Systemleisten verschwinden), der
in beiden Presets aktiv ist (`screen/immersive_mode=true`,
`screen/edge_to_edge=false`). Kein UI ist hinter Status-/Navigationsleisten
versteckt.

### 4.4 16-KB-Page-Size (Google-Play-Pflicht, Android 15+)
Godot 4.7 (NDK r29, AGP 8.6.1) erfüllt das automatisch; native Libraries
müssen unkomprimiert bleiben → `gradle_build/compress_native_libraries=false`
(Default + im Preset gesetzt).

### 4.5 Haptik & Audio
- Vibration: Godot-`OS.vibrate_handheld()`, Permission VIBRATE gesetzt.
- Audio-Latenz: `[audio] driver/output_latency=15` in `project.godot`.
- Mobile-Renderer: `rendering_method.mobile="mobile"` (Forward+ Desktop,
  Mobile auf Android), ETC2/ASTC-Kompression aktiv.

### 4.6 Performance
`PerformanceManager.gd`, `LodManager.gd` und Touch (TouchInputManager,
VirtualJoystick) sind vorhanden; 1920×1080-Viewport wird vom PerformanceManager
skaliert. Empfehlung für schwache Geräte: `PerformanceManager`-Stufe,
siehe `docs/GODOT_MOBILE_DETAILAUSBAU.md`.

## 5. Verifikation

```bash
./Tools/android_check.sh                 # Repo + Toolchain

# Nach dem Export:
unzip -p build/PennerKombat-debug.apk AndroidManifest.xml | strings | grep -E "sdkVersion|targetSdkVersion"   # API 24/36 (Template)
aapt2 dump badging build/PennerKombat-debug.apk | grep -E "sdkVersion|targetSdkVersion|package"

# 16-KB-Alignment (Build-Tools 35.0.1+):
$ANDROID_HOME/build-tools/35.0.1/zipalign -c -P 16 -v 4 build/PennerKombat-debug.apk

# Installation/Tests
adb install -r build/PennerKombat-debug.apk
adb shell dumpsys package com.pennerkombat.mobile | grep -E "versionName|minSdk|targetSdk"
adb shell getconf PAGE_SIZE    # Gerät 4K oder 16K (Android-15-Dev-Option)
```

**Geräte-Matrix:** Emulatoren API 30, 31, 33, 34, 35
(`sdkmanager "system-images;android-35;google_apis;x86_64"` +
`avdmanager create avd …`), alternativ Firebase Test Lab / GitHub Actions
ReactiveCircus/android-emulator-runner.

## 6. Installations-/Release-Weg

1. Debug-APK aus CI herunterladen und sideloaden (`adb install` oder Dateimanager).
2. Für Google Play: AAB (Workflow-Input `build_aab`) → Play Console →
   Play-App-Signing (Keystore niemals ins Repo).
3. Versionierung: `package/version=1`, `version/code=1` in `project.godot` und
   in den Presets vor jedem Upload erhöhen.

## 7. Gleichwertige Alternativen („wenn nicht möglich“)

| Szenario | Alternative |
|---|---|
| Godot < 4.5 (kein targetSdk 35, kein 16 KB) | Engine auf **4.7.2** heben (Empfehlung); sonst targetSdk **34** (Android 14) setzen, Android 15 dann nur mit `wss://`/Immersive-Test — nicht Play-konform für API 35 |
| Kein Gradle/NDK auf dem Rechner | **CI-Aufbau** (APK immer; AAB auf Wunsch) |
| `ws://`-Relay nicht erlaubt (Play / kein `--relay`) | **`wss://`**-Relay (TLS-Caddy/nginx, `docs/SERVER.md`) oder **Capacitor-Wrapper** (`web/`, `Tools/capacitor_setup.sh`) — WebView-Variante |
| Godot 4.7 nicht installierbar | **Capacitor/PWA** aus `web/3d.html` (Android 11–15, `browser_version` im WebView) |
