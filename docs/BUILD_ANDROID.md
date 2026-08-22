# 📱 Android-Build (APK / AAB)

Diese Anleitung führt vom Repo zum installierbaren `.apk`.

**Zwei Wege:**

| Weg | Voraussetzung | Anleitung |
|---|---|---|
| **A — Unity auf dem eigenen Rechner** | Unity 2022.3 LTS + Android-Modul | diese Seite |
| **B — GitHub baut das APK** | kostenlose Unity-Lizenz als Secret | [`docs/ci/README.md`](ci/README.md) → `android-build.yml` |

**macOS Big Sur (11.x) reicht** — Unity 2022.3 LTS verlangt Mojave 10.14+ (Intel)
bzw. Big Sur 11.0+ (Apple Silicon). Für Weg A im Hub das Modul *Android Build Support*
inklusive **OpenJDK** und **Android SDK & NDK Tools** mitinstallieren.

**Nicht nötig:** PS3-Emulatoren, WebView, Capacitor oder ein Browser-Export.
Penner Kombat ist ein Unity-Projekt in C# — Android ist ein normales Build-Ziel.

**Hinweis:** Seit v1.4.0 liegen `ProjectSettings/` und `Packages/manifest.json`
im Repo — Schritt 1 („Projekt anlegen") entfällt, du kannst den Ordner direkt
in Unity Hub öffnen.

---

## 0. Was hier fehlt (und warum es kein Skript ersetzt)

| Fehlt | Warum es nicht generierbar ist |
|---|---|
| `ProjectSettings/`, `Packages/manifest.json` | Werden von Unity beim Projektanlegen erzeugt |
| Charakter-Modelle, Rigs, Animationen | Art-Assets; Frame-Daten stehen in `docs/MOVESETS.md` |
| Audio (31 Tracks, Voice) | siehe `docs/SOUNDTRACK.md` |
| Szenen `MainMenu`/`Arena` | `Arena` erzeugt der Setup-Wizard, `MainMenu` musst du bauen |

Die VFX-Schicht (`Assets/Scripts/VFX/`) ist bewusst so gebaut, dass sie
**ohne Art-Assets** läuft — für einen ersten spielbaren Build reichen also
Primitive als Charaktere.

---

## 1. Projekt anlegen

1. **Unity Hub → New Project → 3D (URP)**, Unity **2022.3 LTS** oder **Unity 6**
2. Projektname z. B. `PennerKombat`
3. Aus diesem Repo hineinkopieren:
   - `Assets/Scripts/` → `Assets/Scripts/`
   - `Assets/Shaders/` → `Assets/Shaders/`
   - `Assets/Plugins/Android/AndroidManifest.xml` → gleicher Pfad

## 2. Pakete & Defines

| Paket | Zweck |
|---|---|
| **Input System** | `FighterInput` nutzt `UnityEngine.InputSystem` |
| **TextMeshPro** | HUD, Combo-Zähler, Callouts (Essentials importieren!) |
| **Universal RP** | Shader + Post-Processing |

- Project Settings → Player → **Active Input Handling = Both**
- Project Settings → Player → **Scripting Define Symbols**: `PK_URP` hinzufügen
  (aktiviert `UrpPostProcessingDriver`)

## 3. Szene erzeugen

```
Tools → Penner Kombat → Asset-Ordner anlegen
Tools → Penner Kombat → Setup-Szene erzeugen
```

Erzeugt `Assets/Scenes/Arena.unity` mit Boden (nasses Pflaster), Spawns,
`Bootstrapper` und `ArenaVisuals`. Der Bootstrapper zieht beim Start die
komplette VFX-Schicht hoch.

Danach in **Build Settings** beide Szenen eintragen (`Arena` als Index 0,
solange kein Hauptmenü existiert).

## 4. Player Settings für Android

| Bereich | Wert |
|---|---|
| Company / Product | `Dingelang Games` / `Penner Kombat` |
| Package Name | `com.dingelanggames.pennerkombat` |
| Orientation | Landscape Left + Right (Auto Rotation) |
| Graphics API | Vulkan, Fallback OpenGLES3 |
| Minimum API Level | **29** (Android 10) |
| Target API Level | Höchste installierte (Play Store: ≥ 34) |
| Scripting Backend | **IL2CPP** |
| Target Architectures | **ARM64** (ARMv7 optional) |
| Managed Stripping | Low (High kann Reflection in `BroadcastMessage` brechen) |

> `BroadcastMessage` wird in `Mell.Defi` und den Debuffs genutzt — bei
> aggressivem Stripping unbedingt eine `link.xml` anlegen oder Stripping auf
> Low stellen.

## 5. Build

**Über die GUI:** File → Build Settings → Android → Switch Platform → Build.

**Per Kommandozeile (CI-tauglich):**

```bash
"/opt/Unity/Editor/Unity" -quit -batchmode -nographics \
  -projectPath "$PWD" \
  -buildTarget Android \
  -executeMethod BuildScript.BuildAndroid \
  -logFile build.log
```

Ein passendes `BuildScript` gehört nach `Assets/Editor/` — Beispiel:

```csharp
using UnityEditor;
public static class BuildScript
{
    public static void BuildAndroid()
    {
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        BuildPipeline.BuildPlayer(
            new[] { "Assets/Scenes/Arena.unity" },
            "Builds/PennerKombat.apk",
            BuildTarget.Android,
            BuildOptions.None);
    }
}
```

## 6. Signieren (Release)

1. Player Settings → Publishing Settings → **Keystore Manager → Create New**
2. Keystore + Passwörter **außerhalb** des Repos ablegen (nie committen!)
3. Build Settings → Häkchen bei **Build App Bundle (Google Play)** für `.aab`

## 7. CI (optional)

Ein Android-Build in GitHub Actions braucht `game-ci/unity-builder` **und** eine
gültige Unity-Lizenz als Secret (`UNITY_LICENSE` / `UNITY_EMAIL` / `UNITY_PASSWORD`).
Vorlage und Hinweise: `docs/ci/README.md`.

---

## Touch-Steuerung

`FighterInput` deckt Tastatur und Gamepad ab — **eine Touch-Belegung fehlt noch**.
Für Handhelds braucht es entweder ein On-Screen-Control-Overlay (Input System:
`On-Screen Button` / `On-Screen Stick`) oder ein eigenes Touch-Layout.
Ohne das ist der APK-Build nur mit angeschlossenem Bluetooth-Gamepad spielbar.
