#!/usr/bin/env bash
#
# push.sh — Pusht das fertige Penner-Kombat-Repo nach GitHub.
#
# Voraussetzung (einmalig):
#   1. Erstelle ein Personal Access Token unter GitHub:
#      Settings -> Developer settings -> Personal access tokens
#      (Scope: "Contents" = write)
#   2. Lege es als Umgebungsvariable ab ODER trage es unten ein.
#
# Nutzung:
#   ./Tools/push.sh                     # Token aus $GITHUB_TOKEN
#   GITHUB_TOKEN=ghp_xxx ./Tools/push.sh
#
set -euo pipefail

REPO="dingeldangbang/penner-kombat"
REMOTE_URL="https://github.com/${REPO}.git"

# --- 1. Token laden ---
TOKEN="${GITHUB_TOKEN:-}"
if [[ -z "$TOKEN" ]]; then
  echo "⚠️  Kein Token gefunden. Setze GITHUB_TOKEN oder trage es unten ein."
  read -rsp "GitHub Personal Access Token: " TOKEN
  echo ""
fi
[[ -n "$TOKEN" ]] || { echo "❌ Abbruch: kein Token."; exit 1; }

cd "$(dirname "$0")/.."

# --- 2. Remote setzen (Token im URL für einmaligen Push) ---
git remote remove origin 2>/dev/null || true
git remote add origin "https://x-access-token:${TOKEN}@${REMOTE_URL#https://}"
git branch -M main

# --- 3. Pushen ---
echo "🚀 Push main + Tags nach ${REMOTE_URL}"
git push -u origin main
git push origin --tags

# --- 4. Remote wieder bereinigen (Token aus Config entfernen) ---
git remote set-url origin "${REMOTE_URL}"

echo ""
echo "✅ Fertig! Repo live: https://github.com/${REPO}"
echo "   Als nächstes (im Browser): Release aus Tag v1.3.0 erstellen und"
echo "   penner-kombat-release.zip anhängen."
