#!/usr/bin/env bash
# Installiert das Godot-Android-Gradle-Build-Template nach res://android/build
# (= android/build im Repo). Nötig nur für den AAB-/Gradle-Build (Preset
# "Android AAB") bzw. für den ws://-Relay-Manifest-Patch.
#
# Aufruf:  ./Tools/install_android_build_template.sh [--relay] [--version 4.7.2.stable]
#
# --relay      setzt zusätzlich android:usesCleartextTraffic="true" in der
#              Template-Hauptdatei src/main/AndroidManifest.xml (idempotent)
# --version    erwartete Template-Version (Default: neueste unter 4.*)
set -euo pipefail
cd "$(dirname "$0")/.."

RELAY=0
VERSION_FILTER=""
while [ $# -gt 0 ]; do
  case "$1" in
    --relay) RELAY=1 ;;
    --version) VERSION_FILTER="${2:-}"; shift ;;
    *) echo "Unbekanntes Argument: $1"; exit 1 ;;
  esac
  shift
done

# ---------- Export-Template-Ordner finden ----------
if [ -n "$VERSION_FILTER" ]; then
  dir="$HOME/.local/share/godot/export_templates/$VERSION_FILTER"
  [ -d "$dir" ] || dir="$HOME/Library/Application Support/Godot/export_templates/$VERSION_FILTER"
  [ -d "$dir" ] || dir="$APPDATA/Godot/export_templates/$VERSION_FILTER"
else
  dir="$(ls -dt "$HOME/.local/share/godot/export_templates"/4.* 2>/dev/null | head -n1 || true)"
  [ -n "$dir" ] || dir="$(ls -dt "$HOME/Library/Application Support/Godot/export_templates"/4.* 2>/dev/null | head -n1 || true)"
  # Windows (Git-Bash): %APPDATA%\Godot\export_templates\<version>
  [ -n "$dir" ] || dir="$(ls -dt "$APPDATA/Godot/export_templates"/4.* 2>/dev/null | head -n1 || true)"
fi
if [ -z "$dir" ] || [ ! -f "$dir/android_source.zip" ]; then
  echo "❌ android_source.zip nicht gefunden."
  echo "   Export-Templates für Godot 4.7.x installieren:"
  echo "   Editor → Editor-Menü → Manage Export Templates → Download & Install"
  echo "   (oder --version <4.7.2.stable> angeben)"
  exit 1
fi
VERSION_TAG="$(basename "$dir")"
echo "Template-Quelle: $dir"

command -v unzip >/dev/null 2>&1 || { echo "❌ unzip fehlt"; exit 1; }

# ---------- installieren ----------
echo "Installiere nach android/build …"
rm -rf android/build
mkdir -p android/build
unzip -q -o "$dir/android_source.zip" -d android/build
touch android/build/.gdignore
printf '%s\n' "$VERSION_TAG" > android/.build_version
echo "✅ android/.build_version = $VERSION_TAG"

# ---------- optional: ws://-Relay (Cleartext) ----------
if [ "$RELAY" = 1 ]; then
  MANIFEST="android/build/src/main/AndroidManifest.xml"
  if [ ! -f "$MANIFEST" ]; then
    echo "❌ $MANIFEST fehlt — unerwartete Template-Struktur"
    exit 1
  fi
  PYTHON_BIN="${PYTHON:-python3}"
  if ! command -v "$PYTHON_BIN" >/dev/null 2>&1; then
    echo "⚠ python3 fehlt — Cleartext-Patch übersprungen (nur für ws:// nötig)"
  else
    "$PYTHON_BIN" - "$MANIFEST" <<'PY'
import re, sys
path = sys.argv[1]
with open(path, encoding="utf-8") as f:
    text = f.read()
if 'android:usesCleartextTraffic' in text:
    print("usesCleartextTraffic bereits gesetzt")
    sys.exit(0)
def add_attr(m):
    return "<application" + m.group(1) + ' android:usesCleartextTraffic="true"' + m.group(2) + ">"
new, n = re.subn(r"<application\b([^>]*?)(\s*/?)>", add_attr, text, count=1, flags=re.S)
if n != 1:
    print("⚠ application-Tag nicht gefunden")
    sys.exit(1)
with open(path, "w", encoding="utf-8") as f:
    f.write(new)
print("✅ usesCleartextTraffic=true ergänzt:", path)
PY
  fi
fi

echo
echo "Fertig. AAB-Build: "
echo "  godot --headless --path . --export-debug \"Android AAB\" build/PennerKombat.aab"
