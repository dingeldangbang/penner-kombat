# CI-Vorlagen

> **Zuerst lesen:** Das Repo enthält **zwei** Codebasen.
>
> | Ordner | Engine | Status |
> |---|---|---|
> | `project.godot`, `scenes/`, `scripts/` | **Godot 4.2** (GDScript) | vollständig, baubar → **`godot-android.yml`** |
> | `Assets/Scripts/` | Unity (C#) | nur Skripte, kein baubares Projekt (keine Szenen/Prefabs/Art) |
> | `web/` | Three.js | läuft ohne Build im Browser |
>
> **Für ein APK nimmst du `godot-android.yml`.** Die Unity-Workflows unten
> (`android-build.yml`, `webgl-pages.yml`, `unity-activation.yml`) gehören zur
> Unity-Codebasis, brauchen eine Unity-Lizenz und bauen dieses Godot-Spiel
> *nicht*.

## `godot-android.yml` — APK aus dem Godot-Projekt (der aktive Weg)

Baut ein installierbares Debug-APK. Keine Lizenz, keine Secrets, nichts lokal
zu installieren.

```bash
bash Tools/enable_ci.sh
git push
```

Danach: **Actions → „Android Build (Godot)" → Run workflow** →
**Artifacts → `pennerkombat-debug`**.

Vollständige Anleitung samt Fehlerdiagnose: [`../APK_BUILD_READY.md`](../APK_BUILD_READY.md).

---

## `release-zip.yml` — ZIP automatisch ans Release hängen

GitHub-Apps/Agents dürfen ohne `workflows`-Berechtigung keine Dateien unter
`.github/workflows/` pushen. Deshalb liegt die Workflow-Datei hier als Vorlage.

**Aktivieren (einmalig, lokal):**

```bash
mkdir -p .github/workflows
cp docs/ci/release-zip.yml .github/workflows/release-zip.yml
git add .github/workflows/release-zip.yml
git commit -m "ci: Release-ZIP automatisch anhängen"
git push
```

**Danach:**

- Jeder gepushte Tag `v*.*.*` bekommt automatisch `penner-kombat-release.zip`
  ans Release gehängt.
- Für bereits bestehende Releases (z. B. `v1.3.0`):
  **Actions → „Release-ZIP anhängen" → Run workflow → Tag eintragen**.

**Alternative ohne CI (lokal, mit `gh` eingeloggt):**

```bash
git archive --format=zip --prefix=penner-kombat/ -o penner-kombat-release.zip v1.3.0
gh release upload v1.3.0 penner-kombat-release.zip --clobber
```

## Kein APK aus der *Unity*-Codebasis

> Veraltet, soweit es das Wort „kein APK" betrifft: Aus dem **Godot**-Projekt
> entsteht sehr wohl ein APK — siehe `godot-android.yml` oben. Der folgende
> Absatz gilt nur für den Unity-Zweig unter `Assets/Scripts/`.

Aus dem Unity-Teil lässt sich kein spielbares **`.apk`** erzeugen: Dort liegen nur
`Assets/Scripts` (C#), aber **kein vollständiges Unity-Projekt** (Szenen, Prefabs,
Art/Audio fehlen).
Ein Unity-Build braucht außerdem den Unity-Editor + Android-SDK/NDK und eine
gültige Unity-Lizenz (z. B. via `game-ci/unity-builder` mit `UNITY_LICENSE`-Secret).
Schritte dorthin siehe [../SETUP.md](../SETUP.md) und [../RELEASE.md](../RELEASE.md).


---

## `android-build.yml` — APK automatisch bauen

Baut mit [`game-ci/unity-builder`](https://game.ci) ein installierbares APK auf
GitHubs Servern. **Damit brauchst du kein Unity auf dem eigenen Rechner** — nur
eine (kostenlose) Unity-Lizenz.

### Schritt 1 — Lizenzdatei erzeugen (ohne Unity auf dem eigenen Rechner)

Die kostenlose *Personal*-Lizenz reicht. Die Aktivierungsdatei erzeugt GitHub selbst:

```bash
mkdir -p .github/workflows
cp docs/ci/unity-activation.yml .github/workflows/unity-activation.yml
git add .github/workflows/unity-activation.yml
git commit -m "ci: Unity-Aktivierung"
git push
```

1. **Actions → „Unity-Lizenz anfordern (.alf)" → Run workflow**
2. Artefakt herunterladen, entpacken → `Unity_v2022.x.alf`
3. Datei auf <https://license.unity3d.com/manual> hochladen, *Unity Personal* wählen
4. Die zurückgegebene `.ulf`-Datei im Texteditor öffnen, **kompletten Inhalt** kopieren

*Wer Docker zur Hand hat, kann die `.alf` auch lokal erzeugen — nötig ist es nicht.*

### Schritt 2 — Secrets anlegen

**Repo → Settings → Secrets and variables → Actions → New repository secret**

| Name | Inhalt |
|---|---|
| `UNITY_LICENSE` | kompletter Inhalt der `.ulf`-Datei |
| `UNITY_EMAIL` | E-Mail des Unity-Kontos |
| `UNITY_PASSWORD` | Passwort des Unity-Kontos |

### Schritt 3 — Workflow aktivieren

```bash
mkdir -p .github/workflows
cp docs/ci/android-build.yml .github/workflows/android-build.yml
git add .github/workflows/android-build.yml
git commit -m "ci: Android-APK-Build"
git push
```

### Schritt 4 — Bauen

- **Manuell:** Actions → „Android-APK bauen" → *Run workflow*.
  Das APK liegt danach unter *Artifacts* zum Download.
- **Automatisch:** Tag `v1.5.0` pushen → APK hängt am Release.

### Was dabei schiefgehen kann

| Symptom | Ursache |
|---|---|
| `License is not activated` | `UNITY_LICENSE` unvollständig kopiert (inkl. XML-Kopfzeile!) |
| Build bricht bei „Building Il2CPP" ab | Runner-Speicher; im Workflow ist bereits Aufräumen drin, sonst `IL2CPP` → `Mono` testen |
| APK startet, zeigt aber nichts | TMP-Ressourcen fehlen im Repo — einmal im Editor importieren und `Assets/TextMesh Pro/` committen |
| Kompilierfehler | Der C#-Code dieses Repos wurde **nie kompiliert** — der erste CI-Lauf ist zugleich der erste echte Compiler-Test |

> **Erwartungsmanagement:** Der erste Lauf schlägt mit hoher Wahrscheinlichkeit fehl.
> Das Log der Action ist dann die Liste, die wir abarbeiten — genau dafür ist er da.


---

## `webgl-pages.yml` — im Browser spielen, ganz ohne Installation

Der schnellste Weg zum Spielen, wenn der eigene Rechner Unity nicht packt:
GitHub baut die WebGL-Version und veröffentlicht sie auf GitHub Pages.

```bash
mkdir -p .github/workflows
cp docs/ci/webgl-pages.yml .github/workflows/webgl-pages.yml
git add .github/workflows/webgl-pages.yml
git commit -m "ci: WebGL auf GitHub Pages"
git push
```

Einmalig: **Settings → Pages → Source = „GitHub Actions"**.
Dann **Actions → „WebGL bauen …" → Run workflow**.

Ergebnis: `https://dingeldangbang.github.io/penner-kombat/` — läuft auf Handy,
Tablet und jedem Rechner mit aktuellem Browser. Kein APK, kein Sideload, kein Store.

**Grenzen der WebGL-Fassung:**

| Punkt | Auswirkung |
|---|---|
| Kein Multithreading | etwas weniger Bilder pro Sekunde als nativ |
| Kein UDP | LAN-Erkennung fällt weg; der WebSocket-Relay funktioniert |
| Ladezeit | erster Aufruf lädt einige MB |
| Touch | funktioniert, die On-Screen-Steuerung erscheint auf Handhelds |
| Browser | Chrome/Edge/Firefox aktuell, Safari 15+ |

---

## Kein tauglicher Rechner? Der komplette Weg ohne lokales Unity

1. `unity-activation.yml` aktivieren → `.alf` erzeugen → `.ulf` holen → Secret `UNITY_LICENSE` setzen
2. `webgl-pages.yml` aktivieren → Pages-Quelle auf „GitHub Actions" stellen → Run workflow
3. Im Browser spielen. Für ein Handy-APK zusätzlich `android-build.yml` aktivieren.

Alles, was du dafür brauchst, ist ein Browser und ein kostenloses Unity-Konto.
