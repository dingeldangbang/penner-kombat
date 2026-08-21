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
