# CI-Vorlagen

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

## Kein APK/Unity-Build in CI

Ein spielbares **`.apk`** lässt sich hier nicht erzeugen: Das Repo enthält nur
`Assets/Scripts` (C#), aber **kein vollständiges Unity-Projekt** (keine
`ProjectSettings/`, `Packages/manifest.json`, Szenen, Prefabs, Art/Audio).
Ein Unity-Build braucht außerdem den Unity-Editor + Android-SDK/NDK und eine
gültige Unity-Lizenz (z. B. via `game-ci/unity-builder` mit `UNITY_LICENSE`-Secret).
Schritte dorthin siehe [../SETUP.md](../SETUP.md) und [../RELEASE.md](../RELEASE.md).


---

## `android-build.yml` — APK automatisch bauen

Baut mit [`game-ci/unity-builder`](https://game.ci) ein installierbares APK auf
GitHubs Servern. **Damit brauchst du kein Unity auf dem eigenen Rechner** — nur
eine (kostenlose) Unity-Lizenz.

### Schritt 1 — Lizenzdatei erzeugen

Die kostenlose *Personal*-Lizenz reicht. Anleitung von game-ci, kurz gefasst:

```bash
# Einmalig lokal, mit Docker:
docker run -it --rm unityci/editor:ubuntu-2022.3.62f1-android-3 \
  unity-editor -quit -batchmode -nographics -logFile /dev/stdout -createManualActivationFile
```

Die entstandene `.alf`-Datei auf https://license.unity3d.com/manual hochladen,
die zurückgegebene `.ulf`-Datei öffnen und den **kompletten Inhalt** kopieren.

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
