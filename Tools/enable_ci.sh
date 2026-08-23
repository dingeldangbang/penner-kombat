#!/usr/bin/env bash
# Aktiviert den Godot-Android-Workflow.
#
# Warum dieser Umweg? GitHub-Apps und Agents ohne `workflows`-Berechtigung
# dürfen keine Dateien unter .github/workflows/ pushen -- der Push wird mit
# "refusing to allow a GitHub App to create or update workflow ... without
# workflows permission" abgelehnt. Deshalb liegt die Workflow-Datei als
# Vorlage unter docs/ci/ und wird hier von dir (= ein normaler Benutzer mit
# vollen Rechten) an ihren Platz kopiert.
#
# Aufruf aus dem Repo-Wurzelverzeichnis:
#     bash Tools/enable_ci.sh
#
set -euo pipefail
cd "$(dirname "$0")/.."

SRC="docs/ci/godot-android.yml"
DST=".github/workflows/godot-android.yml"

if [ ! -f "$SRC" ]; then
  echo "Vorlage $SRC nicht gefunden -- falsches Verzeichnis?" >&2
  exit 1
fi

echo "== Preflight =="
python3 Tools/godot_preflight.py

echo
echo "== Workflow installieren =="
mkdir -p .github/workflows
cp "$SRC" "$DST"
git add "$DST"

if git diff --cached --quiet -- "$DST"; then
  echo "$DST ist bereits aktuell -- nichts zu committen."
else
  git commit -m "ci: Godot-Android-APK-Workflow aktivieren"
  echo "Committet. Jetzt pushen:"
  echo "    git push"
fi

echo
cat <<'EOF'
Danach auf GitHub:

  Actions -> "Android Build (Godot)" -> Run workflow

Das APK liegt anschliessend unter Artifacts -> pennerkombat-debug.
EOF
