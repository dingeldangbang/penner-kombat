# 🐙 GitHub CLI / CD / CI — Android 11–15 ohne lokales SDK

**Kurzfassung:** Statt Godot, Android SDK, NDK und JDK lokal zu installieren,
baut die **GitHub Actions-Workflow** in der Cloud das APK (und optional das
AAB) und der **GitHub-CLI-Befehl** `gh` steuert Build, Download und Release —
auf jedem Rechner mit nur `gh`, `git` und Netzwerk.

Alle Befehle sind in einem Tool gebündelt: **`./Tools/gh_android.sh`**

---

## 1. Voraussetzungen

| Komponente | Warum |
|---|---|
| `gh` (≥ 2.23) | Workflow triggern, Run überwachen, Artefakte laden, Release erstellen |
| `git` | Repo-Checkout, Branch/Tag |
| `adb` (nur für `install`) | APK aufs angeschlossene Gerät spielen |
| Kein Godot / SDK / NDK / JDK nötig | alles läuft in der CI-Container (4.7.2 + SDK) |

```bash
gh auth login          # einmalig (repo, workflow, read:org reichen)
git pull               # aktuelle Branch mit Tools/gh_android.sh
```

## 2. Workflow einmalig aktivieren

Ist `.github/workflows/android-apk.yml` noch nicht im Repo (oder wurde der
Push vom Token ohne `workflows`-Scope abgelehnt), legt der Befehl ihn direkt
per `gh api` an — kein Klick im Browser nötig:

```bash
./Tools/gh_android.sh activate
```

## 3. Bauen (CI)

| Befehl | Ergebnis |
|---|---|
| `./Tools/gh_android.sh status` | Auth + Workflow + letzte Runs |
| `./Tools/gh_android.sh build apk` | Debug-APK → wartet → `build/PennerKombat-debug.apk` |
| `./Tools/gh_android.sh build aab` | APK + **AAB** (Gradle, minSdk 30 / targetSdk 35) |
| `./Tools/gh_android.sh build all` | beides |
| `./Tools/gh_android.sh download <run-id>` | Artefakte eines alten Runs nachladen |
| `./Tools/gh_android.sh install` | `adb install -r` der heruntergeladenen APK |

Ablauf dahinter: `gh workflow run` → `gh run watch --exit-status` →
`gh run download -n PennerKombat-Android-debug-<run>` → liegt in `build/`.

## 4. Android 11–15 im Emulator testen (CI-Check)

Der Workflow hat einen optionalen Emulator-Job für **API 30, 31, 33, 34, 35**:

```bash
./Tools/gh_android.sh emulator 35    # Android 15
./Tools/gh_android.sh emulator 30    # Android 11
```

Der Job installiert das APK und prüft, ob der Prozess
`com.pennerkombat.mobile` nach dem Start läuft (Logcat-Fallback bei Fehler).

## 5. Release mit der GitHub CLI

```bash
./Tools/gh_android.sh release v1.1.0
```

Das taggt lokal, pusht den Tag (Workflow läuft auf `v*`), wartet auf den
Run und zeigt danach das **GitHub Release** `v1.1.0` mit angehängter,
**signierter APK** an (`Signed APK → GitHub Release`-Job). Der Job exportiert
mit `--export-release` (Release-Keystore aus den Secrets
`ANDROID_KEYSTORE_BASE64` / `ANDROID_KEYSTORE_PASSWORD` / `ANDROID_KEY_ALIAS`,
siehe `docs/APK_BUILD_READY.md`) — fehlen die Secrets, wird eine
debug-signierte APK mit Warnung veröffentlicht.

## 5b. Signatur prüfen

```bash
# APK aus dem Release herunterladen und mit apksigner (SDK-Build-Tools) prüfen:
$ANDROID_HOME/build-tools/35.0.1/apksigner verify --print-certs PennerKombat-release.apk
# Aus dem GitHub-Release: gh release download v1.1.0 -p '*.apk' -D build
```

## 6. Matrix Android 11–15 (CI deckt das ab)

> **Verifiziert (2026-09-07, Branch `arena/01a07925-penner-kombat`):** APK- und
> AAB-Export laufen in der CI grün (`barichello/godot-ci:4.7.2`, SDK-36-
> und NDK-r29-Bootstrap automatisch). Der Push-Trigger baut bei jedem Push auf
> `main`/`arena/**` das APK — der letzte erfolgreiche Run
> `PennerKombat-Android-debug-8` (~57 MB) liegt als Artefakt auf GitHub.

| Android | API | CI-Emulator-Job | APK | AAB |
|---|---|---|---|---|
| 11 | 30 | `emulator 30` (Default, bewährt) | ✅ | minSdk 30 ✅ |
| 12 / 12L | 31–32 | `emulator 31` | ✅ | ✅ |
| 13 | 33 | `emulator 33` | ✅ | ✅ |
| 14 | 34 | `emulator 34` | ✅ | ✅ |
| 15 | 35 | `emulator 35` (braucht viel RAM/Bootzeit; siehe Hinweis) | ✅ | targetSdk 35 ✅ |

> Hinweis Emulator: GitHub-Hosted-Runner haben **kein KVM** — der Emulator bootet
> im Software-Modus. API 30/31 sind damit zuverlässig testbar; für API 34/35
> (und für die verbindliche Gerätematrix) ist **Firebase Test Lab** oder ein
> Self-Hosted-Runner mit KVM die robustere Alternative.

## 7. Grenzen / Produktion

- **Debug-Signing:** Der CI-Artefakt ist debug-signiert (Keystore im
  CI-Image) — für Tests/Sideload. Play-Store-AABs brauchen einen privaten
  Release-Keystore als GitHub-Secret und einen `--export-release`-Schritt
  (siehe `docs/APK_BUILD_READY.md`).
- **Token-Scope:** Das Pushen von `.github/workflows/…` erfordert den
  `workflows`-Scope. Ohne ihn: `./Tools/gh_android.sh activate` (via
  `gh api`, gleiche Anforderung) oder Workflow einmalig manuell anlegen.
- **Artefakt-Retention:** GitHub löscht Artefakte nach 30 Tagen — Release
  anlegen, wenn das APK dauerhaft bleiben soll.

## 8. Fallback-Log

```bash
gh run list --workflow android-apk.yml --limit 10   # was lief?
gh run view <id>                                    # Job-Zusammenfassung
gh run view <id> --log --job <job_id>               # volles Log
gh api repos/dingeldangbang/penner-kombat/actions/runs/<id>/logs
```
