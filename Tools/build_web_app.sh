#!/usr/bin/env bash
#
# build_web_app.sh — baut die auslieferbare Web-App aus web/
#
# Ergebnis:
#   release/penner-kombat-web/            entpackter Ordner (auf jeden Static-Host kopierbar)
#   release/penner-kombat-web-<ver>.zip   fertiges Paket
#   release/BUILD_INFO.txt                Version, Commit, Datum, Dateiliste
#
# Zusätzlich werden harte Auslieferungs-Kriterien geprüft:
#   1. keine externen CDN-Referenzen (die App muss offline laufen)
#   2. jede Datei aus der Service-Worker-Liste existiert wirklich
#   3. manifest.webmanifest ist gültiges JSON mit Pflichtfeldern
#   4. jede .js/.mjs-Datei ist syntaktisch gültig
#
# Aufruf:  Tools/build_web_app.sh  [--with-tests]
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
WEB="$ROOT/web"
OUT="$ROOT/release"
NAME="penner-kombat-web"
VERSION="$(grep -o "APP_VERSION = '[^']*'" "$WEB/src/app.js" | head -1 | cut -d"'" -f2)"
[ -n "$VERSION" ] || VERSION="0.0.0"

red()  { printf '\033[31m%s\033[0m\n' "$*"; }
grn()  { printf '\033[32m%s\033[0m\n' "$*"; }
info() { printf '\033[36m%s\033[0m\n' "$*"; }

fail=0
check() { # check "Name" "Bedingung erfüllt?"(0/1) ["Detail"]
  if [ "$2" -eq 0 ]; then grn "  ✅ $1${3:+ — $3}"; else red "  ❌ $1${3:+ — $3}"; fail=1; fi
}

info "▶ Penner Kombat Web-App v$VERSION"
echo

# ---------------------------------------------------------------------------
info "1/5 Auslieferungs-Prüfungen"

# 1 — keine CDN-Abhängigkeiten
cdn=$(grep -rlE 'https?://(unpkg\.com|cdn\.|cdnjs|jsdelivr)' "$WEB" \
        --include='*.html' --include='*.js' --include='*.css' \
        --exclude-dir=node_modules --exclude-dir=test --exclude-dir=vendor 2>/dev/null || true)
check "Keine CDN-Referenzen (offline-fähig)" "$([ -z "$cdn" ] && echo 0 || echo 1)" "${cdn:-alles lokal}"

# 2 — Service-Worker-Liste vollständig
missing=""
while read -r f; do
  [ -z "$f" ] && continue
  [ -e "$WEB/$f" ] || missing="$missing $f"
done < <(sed -n "/^const FILES = \[/,/^\];/p" "$WEB/sw.js" | grep -o "'\./[^']*'" | tr -d "'" | sed 's|^\./||' | grep -v '^$')
check "Alle Service-Worker-Dateien vorhanden" "$([ -z "$missing" ] && echo 0 || echo 1)" "${missing:-vollständig}"

# 3 — Manifest gültig
manifest_msg=$(node -e "
  const fs = require('fs');
  const m = JSON.parse(fs.readFileSync('$WEB/manifest.webmanifest', 'utf8'));
  const need = ['name','short_name','start_url','display','icons','shortcuts'];
  const miss = need.filter(k => !m[k]);
  if (miss.length) { console.error('fehlende Felder: ' + miss.join(', ')); process.exit(1); }
  console.log(m.icons.length + ' Icons, ' + m.shortcuts.length + ' Shortcuts');
" 2>&1) && rc=0 || rc=1
check "manifest.webmanifest gültig" "$rc" "$manifest_msg"

# 4 — Syntax aller Skripte
syntax_bad=""
count=0
while IFS= read -r -d '' f; do
  count=$((count + 1))
  if ! node --check "$f" >/dev/null 2>&1; then syntax_bad="$syntax_bad $(basename "$f")"; fi
done < <(find "$WEB/src" "$WEB/sw.js" -name '*.js' -not -path '*/node_modules/*' -print0)
check "JavaScript-Syntax fehlerfrei" "$([ -z "$syntax_bad" ] && echo 0 || echo 1)" "${syntax_bad:-$count Dateien}"

# 5 — optionale Testläufe
if [ "${1:-}" = "--with-tests" ]; then
  info "     Tests laufen …"
  (cd "$WEB" && npm test >/dev/null 2>&1) && rc=0 || rc=1
  check "Unit- und Engine-Tests" "$rc"
  (cd "$WEB" && npm run test:ui >/dev/null 2>&1) && rc=0 || rc=1
  check "Oberflächen-Tests (jsdom)" "$rc"
fi

[ "$fail" -eq 0 ] || { echo; red "Build abgebrochen — bitte obige Punkte beheben."; exit 1; }

# ---------------------------------------------------------------------------
echo
info "2/5 Dateien kopieren"
rm -rf "$OUT/$NAME"
mkdir -p "$OUT/$NAME"
tar -C "$WEB" \
  --exclude='node_modules' --exclude='test' --exclude='package-lock.json' \
  --exclude='.DS_Store' --exclude='*.log' \
  -cf - . | tar -C "$OUT/$NAME" -xf -
files=$(find "$OUT/$NAME" -type f | wc -l | tr -d ' ')
grn "  $files Dateien kopiert"

# ---------------------------------------------------------------------------
info "3/5 Build-Info schreiben"
commit="$(git -C "$ROOT" rev-parse --short HEAD 2>/dev/null || echo 'unbekannt')"
branch="$(git -C "$ROOT" rev-parse --abbrev-ref HEAD 2>/dev/null || echo '-')"
{
  echo "Penner Kombat — Web-App"
  echo "Version : $VERSION"
  echo "Commit  : $commit ($branch)"
  echo "Datum   : $(date -u '+%Y-%m-%d %H:%M UTC')"
  echo "Dateien : $files"
  echo
  echo "Start:"
  echo "  python3 -m http.server 8000    # im entpackten Ordner"
  echo "  oder: node server/src/ai-proxy.js (mit KI-Proxy)"
  echo
  echo "Inhalt:"
  (cd "$OUT/$NAME" && find . -type f | sort | sed 's|^\./|  |')
} > "$OUT/BUILD_INFO.txt"
cp "$OUT/BUILD_INFO.txt" "$OUT/$NAME/BUILD_INFO.txt"
grn "  release/BUILD_INFO.txt"

# ---------------------------------------------------------------------------
info "4/5 Paket schnüren"
zip="$OUT/$NAME-$VERSION.zip"
rm -f "$zip"
(cd "$OUT" && zip -qr "$(basename "$zip")" "$NAME")
grn "  $(basename "$zip") — $(du -h "$zip" | cut -f1)"

# ---------------------------------------------------------------------------
info "5/5 Größenübersicht"
(cd "$OUT/$NAME" && du -sh vendor src assets . 2>/dev/null | awk '{printf "  %-8s %s\n", $1, $2}')

echo
grn "▶ Fertig: $zip"
echo "  Entpackt starten:  cd release/$NAME && python3 -m http.server 8000"
echo "  Mit KI-Proxy:      node server/src/ai-proxy.js   (WEB_ROOT=release/$NAME)"
