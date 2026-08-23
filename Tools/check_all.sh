#!/usr/bin/env bash
# Alle Offline-Pruefungen in einem Rutsch. Genau die Schritte, die auch im
# GitHub-Workflow vor dem Export laufen.
#
#     bash Tools/check_all.sh
set -uo pipefail
cd "$(dirname "$0")/.."

fail=0
for check in godot_preflight gdscript_lint check_roster simulate_match; do
  printf '\n=== %s ===\n' "$check"
  if python3 "Tools/${check}.py"; then
    :
  else
    fail=1
  fi
done

printf '\n'
if [ "$fail" -ne 0 ]; then
  echo "Mindestens eine Pruefung ist fehlgeschlagen."
  exit 1
fi
echo "Alle Pruefungen bestanden."
