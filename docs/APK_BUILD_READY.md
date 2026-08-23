# Penner Kombat — APK über GitHub Actions bauen

Kein Godot, kein Android SDK, kein JDK auf dem eigenen Rechner nötig.
GitHub baut das APK; du lädst es als Artefakt herunter.

---

## Der eine Schritt, den nur du machen kannst

Die Workflow-Datei liegt als Vorlage unter `docs/ci/godot-android.yml` und
**nicht** unter `.github/workflows/`. Grund:

```
! [remote rejected] refusing to allow a GitHub App to create or update
  workflow `.github/workflows/...` without `workflows` permission
```

GitHub verbietet Apps und Agents ohne `workflows`-Berechtigung das Anlegen von
Workflow-Dateien. Das ist eine Schutzfunktion von GitHub, kein Fehler im Repo.
Du als Benutzer hast diese Rechte.

**Variante A — Skript (empfohlen):**

```bash
bash Tools/enable_ci.sh
git push
```

**Variante B — GitHub-Weboberfläche, ohne Git-Client:**

1. Repo öffnen → **Add file → Create new file**
2. Als Dateinamen exakt `.github/workflows/godot-android.yml` eintragen
   (die Schrägstriche erzeugen die Ordner automatisch)
3. Inhalt von `docs/ci/godot-android.yml` hineinkopieren
4. **Commit changes**

## Bauen

**Actions → „Android Build (Godot)" → Run workflow**

Nach ca. 5–10 Minuten: **Artifacts → `pennerkombat-debug`** herunterladen,
entpacken, `pennerkombat-debug.apk` aufs Handy kopieren und installieren
(„Installation aus unbekannten Quellen" muss erlaubt sein).

Ein Tag `v1.2.3` hängt das APK zusätzlich automatisch ans Release.

---

## Was am naheliegenden Workflow nicht funktioniert hätte

Der oft kursierende Kurz-Workflow scheitert reproduzierbar. Die vier Gründe:

| Problem | Auswirkung | Lösung im Workflow |
|---|---|---|
| **`downloads.tuxfamily.org`** | Host ist seit Ende 2023 abgeschaltet, die Godot-Downloads liegen in `godotengine/godot-builds`. `wget` bricht ab. | Download von `github.com/godotengine/godot-builds/releases` |
| **Export-Templates fehlen** | Ohne `.tpz`-Templates bricht *jeder* Export ab: `No export template found at the expected path`. Der Editor allein genügt nicht. | Eigener Schritt lädt `Godot_v4.5.1-stable_export_templates.tpz` nach `~/.local/share/godot/export_templates/4.5.1.stable/` |
| **Kein Keystore** | Android installiert keine unsignierte App. Der Export meldet `'apksigner' returned with error`. | `keytool` erzeugt einen Debug-Keystore; Pfad/Benutzer/Passwort via `GODOT_ANDROID_KEYSTORE_DEBUG_*` |
| **`--export-release` ohne Release-Keystore** | Ein Release-Export ohne eigenen Keystore schlägt fehl. Für Sideloading ist Debug korrekt. | `--export-debug` |

Zwei weitere Fallstricke, die der Workflow ebenfalls abfängt:

- **SDK-/JDK-Pfad:** Godot liest diese nicht aus `$ANDROID_HOME`, sondern aus
  `~/.config/godot/editor_settings-4.tres`. Die Datei wird erzeugt.
- **Leeres Paket:** Beim allerersten Lauf existiert kein `.godot/`-Cache. Ohne
  Vorab-Import exportiert Godot gern ein unvollständiges Paket. Deshalb läuft
  vorher `godot --headless --editor --quit-after 200`, und danach prüft ein
  Schritt die APK-Größe.

## Preflight

Vor jedem Build (und lokal jederzeit) läuft:

```bash
python3 Tools/godot_preflight.py
```

Die Prüfung fängt genau die Fehler ab, die sonst erst nach 10 Minuten CI-Laufzeit
auffallen — oder gar erst als Absturz auf dem Handy:

- `res://`-Referenzen, die ins Leere zeigen
- `@onready var x = $Pfad` mit einem Node, den die Szene nicht enthält
  (der klassische Nil-Crash im ersten Frame)
- Autoload-Skripte, die fehlen
- Hauptszene fehlt, Export-Preset `Android` fehlt
- **Dateien, die zur Laufzeit von `res://` gelesen werden, aber nicht ins APK
  gepackt werden** — genau dieser Fall lag hier vor:
  `translations/translations.json` wurde von `Localization.gd` geladen, war aber
  vom `include_filter` der `export_presets.cfg` nicht erfasst. Ergebnis wäre ein
  APK ohne Übersetzungen gewesen. Der Filter ist jetzt korrigiert.

## Projektdaten

| | |
|---|---|
| Engine | Godot 4.5.1 im CI-Build (Projekt stammt aus 4.2.2; GDScript, kein Mono/C# nötig) |
| Hauptszene | `res://scenes/ui/Editor.tscn` |
| Paket | `com.pennerkombat.mobile` |
| Architekturen | `armeabi-v7a`, `arm64-v8a` |
| min/target SDK | 30 / 33 |
| Gradle-Build | aus — der vorgefertigte APK-Template wird verwendet (schneller, kein NDK nötig) |

## Android 11 bis 16

Das APK deckt **Android 11 bis 16 vollständig ab** — aber nur, weil der
Workflow mit Godot 4.5.1 baut. Mit dem ursprünglich eingetragenen 4.2.2 wäre
es auf neuen Geräten kaputt gewesen.

| Android | API | Status |
|---|---|---|
| 11 | 30 | läuft — genau die Installations-Untergrenze (`min_sdk 30`) |
| 12 / 12L | 31 / 32 | läuft |
| 13 | 33 | läuft (entspricht `target_sdk`) |
| 14 | 34 | läuft |
| 15 | 35 | läuft — **braucht 16-KB-Ausrichtung**, siehe unten |
| 16 | 36 | läuft — **braucht 16-KB-Ausrichtung**, siehe unten |

**Was auf 15/16 schiefgehen kann.** Android 15 kann und Android 16 wird auf
vielen Geräten mit 16-KB-Speicherseiten statt 4 KB betrieben. Native
Bibliotheken müssen dann auf 16 KB ausgerichtet sein. Godot < 4.5 baut mit
NDK r27 und richtet auf 4 KB aus; solche APKs lassen sich auf diesen Geräten
nicht installieren (`INSTALL_FAILED_INVALID_APK: unsupported ELF page size`)
oder stürzen beim Start ab (`UnsatisfiedLinkError`). Godot 4.5 nutzt NDK r28b
und richtet automatisch korrekt aus.

Der Schritt **„16-KB-Page-Size prüfen"** im Workflow verifiziert das am
fertigen APK und lässt den Build rot werden, falls die Ausrichtung nicht
stimmt. Manuell prüfbar mit:

```bash
python3 Tools/check_apk_16kb.py build/pennerkombat-debug.apk
```

Dass `armeabi-v7a` dabei bei `0x1000` bleibt, ist korrekt — die Anforderung
gilt nur für 64-Bit-Architekturen.

**Google Play** ist davon nicht betroffen, solange du das APK seitlich
installierst. Die Play-Pflicht zu 16 KB (seit 01.11.2025) greift erst beim
*Hochladen* in den Store. Dann brauchst du zusätzlich `target_sdk 35+`
(aktuell 33) und einen echten Release-Keystore — dafür ist Godot 4.7.2 die
bessere Wahl, das setzt target SDK 36 von sich aus.

## Wenn der Build doch rot wird

Das Log der Action ist die Arbeitsliste. Häufigste Meldungen:

| Meldung | Ursache |
|---|---|
| `No export template found` | Godot-Version und Template-Version weichen ab — beide stehen als `GODOT_VERSION` im Workflow und müssen exakt zusammenpassen |
| `Cannot export project with preset "Android" due to configuration errors` | Keystore-Variablen unvollständig: entweder alle drei `GODOT_ANDROID_KEYSTORE_DEBUG_*` oder keine |
| `Invalid package name` | `package/unique_name` braucht mindestens zwei durch Punkt getrennte Segmente |
| Parse-Fehler in einer `.gd`-Datei | Der GDScript-Code wurde nie von Godot geparst — der erste CI-Lauf ist zugleich der erste echte Syntax-Test |

> **Erwartungsmanagement:** Der Workflow ist gegen die bekannten Stolperstellen
> abgesichert, aber die GDScript-Dateien dieses Repos sind noch nie durch einen
> Godot-Parser gelaufen. Falls der erste Lauf an einem Syntaxfehler scheitert,
> steht die betroffene Datei mit Zeilennummer im Log.

## Kein APK nötig? Browser-Variante

`web/3d.html` ist eine eigenständige Three.js-Fassung, die ohne Build und ohne
Installation im Browser läuft:

```bash
cd web && python3 -m http.server 8000
```
