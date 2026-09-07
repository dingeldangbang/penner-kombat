#!/usr/bin/env bash
# Penner Kombat — Grafik-/Pipeline-Verifikation in echter Godot-Umgebung.
#
#   GODOT_BIN=/pfad/zu/godot ./Tools/verify_graphics.sh
#   # oder: godot im PATH
#
# Führt (identisch zur GitHub-Action .github/workflows/godot-smoke.yml) aus:
#   1. Projekt-Import       2. Arena-Boot (300 Frames)
#   3. Pipeline-Test (Metarig → Rigging → Shader → Qualität)
set -e
cd "$(dirname "$0")/.."

GODOT_BIN="${GODOT_BIN:-godot}"
LOG_DIR="${TMPDIR:-/tmp}"

echo "== 1/3 Projekt-Import =="
"$GODOT_BIN" --headless --path . --import 2>&1 | tee "$LOG_DIR/pk_import.log"

echo
echo "== 2/3 Arena-Boot (300 Frames) =="
set +e
"$GODOT_BIN" --headless --path . res://scenes/combat/Arena.tscn --quit-after 300 2>&1 | tee "$LOG_DIR/pk_arena.log"
if grep -E "SCRIPT ERROR|^ERROR:" "$LOG_DIR/pk_arena.log"; then
    echo "❌ Arena-Boot meldet Fehler — Log: $LOG_DIR/pk_arena.log"
    exit 1
fi
echo "✅ Arena-Boot OK (keine Engine-/Skript-Fehler)"
set -e

echo
echo "== 3/3 Pipeline-Test =="
"$GODOT_BIN" --headless --path . --script res://scripts/Testing/PipelineSmoke.gd

echo
echo "✅ Verifikation abgeschlossen. Logs: $LOG_DIR/pk_*.log"
