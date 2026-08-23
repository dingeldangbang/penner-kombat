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
| **Export-Templates fehlen** | Ohne `.tpz`-Templates bricht *jeder* Export ab: `No export template found at the expected path`. Der Editor allein genügt nicht. | Eigener Schritt lädt `Godot_v4.2.2-stable_export_templates.tpz` nach `~/.local/share/godot/export_templates/4.2.2.stable/` |
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
| Engine | Godot 4.2.2 (GDScript, kein Mono/C# nötig) |
| Hauptszene | `res://scenes/ui/Editor.tscn` |
| Paket | `com.pennerkombat.mobile` |
| Architekturen | `armeabi-v7a`, `arm64-v8a` |
| min/target SDK | 30 / 33 |
| Gradle-Build | aus — der vorgefertigte APK-Template wird verwendet (schneller, kein NDK nötig) |

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
