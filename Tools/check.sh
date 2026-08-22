#!/usr/bin/env bash
# Statische Prüfungen ohne Unity — ersetzt keinen Compiler, fängt aber
# Klammer- und Referenzfehler ab. Aufruf aus dem Repo-Wurzelverzeichnis.
set -e
cd "$(dirname "$0")/.."
echo "== Klammern/Strings =="
python3 Tools/check_braces.py
echo
echo "== Referenzen =="
python3 Tools/check_symbols.py
