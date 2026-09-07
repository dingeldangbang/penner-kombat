#!/usr/bin/env bash
# Penner Kombat — GitHub-CLI/CD/CI-Alternative für Android-11–15-Builds.
#
# Baut APK/AAB komplett auf GitHub Actions (+ optionaler
# Emulator-Smoke-Test) und lädt die Artefakte herunter —
# ohne lokales Godot, Android SDK, NDK oder JDK.
#
# Aufruf (aus dem Repo-Wurzelverzeichnis):
#   ./Tools/gh_android.sh status
#   ./Tools/gh_android.sh activate            # einmalig: Workflow auf GitHub anlegen
#   ./Tools/gh_android.sh build apk           # APK bauen, abwarten, herunterladen
#   ./Tools/gh_android.sh build aab           # APK + AAB bauen (Gradle-Export)
#   ./Tools/gh_android.sh build all           # beides
#   ./Tools/gh_android.sh emulator [35]       # Build + Emulator-Smoke-Test (API 30–35)
#   ./Tools/gh_android.sh download <run-id>   # Artefakte eines bestehenden Runs holen
#   ./Tools/gh_android.sh release v1.1.0      # Tag → CI-Build → GitHub Release
#   ./Tools/gh_android.sh install             # adb install -r der heruntergeladenen APK
#
# Umgebungsvariablen:
#   REPO=dingeldangbang/penner-kombat   (Default: aus git remote)
#   BRANCH=arena/…                      (Default: aktuelle Branch)
set -euo pipefail
cd "$(dirname "$0")/.."

# ---------- Konfiguration ----------
REPO="${REPO:-$(git remote get-url origin | sed -E 's#.*github.com[:/]([^/]+/[^/]+?)(\.git)?$#\1#' | head -n1)}"
BRANCH="${BRANCH:-$(git branch --show-current)}"
WORKFLOW="android-apk.yml"
APK_PREFIX="PennerKombat-Android-debug"
AAB_PREFIX="PennerKombat-Android-debug-aab"
CMD="${1:-status}"
ARG="${2:-}"

# ---------- Hilfsfunktionen ----------
die() { echo "❌ $*" >&2; exit 1; }
info() { echo "ℹ  $*"; }
ok()   { echo "✅ $*"; }

require_gh() {
  command -v gh >/dev/null 2>&1 || die "gh CLI fehlt — https://cli.github.com installieren"
  gh auth status >/dev/null 2>&1 || die "gh nicht angemeldet — 'gh auth login' ausführen"
  [ -n "$REPO" ] || die "REPO konnte nicht aus git remote abgeleitet werden"
}

workflow_exists() {
  gh workflow list --all 2>/dev/null | grep -q "$WORKFLOW"
}

latest_run_id() { # $1 = Branch/Tag, optional
  gh run list --workflow "$WORKFLOW" --limit 1 --json databaseId,displayTitle,status,conclusion \
    --jq '.[0] | "\(.databaseId)\t\(.status)\t\(.conclusion // "-")\t\(.displayTitle)"'
}

watch_and_download() { # $1 = run id, $2 = artifact prefix
  local id="$1" prefix="$2"
  info "Warte auf Run #$id (Ctrl+C bricht nur das Warten ab) …"
  gh run watch "$id" --exit-status || die "Run #$id fehlgeschlagen — Details: gh run view $id --log"
  mkdir -p build
  # Artefaktname = <prefix>-<run_number> (nicht die Run-databaseId) —
  # aus der Runs-API auflösen, Fallback auf prefix-id.
  local name
  name="$(gh api "/repos/$REPO/actions/runs/$id/artifacts" \
    --jq "[.artifacts[] | select(.name | startswith(\"$prefix\"))][0].name" 2>/dev/null \
    || true)"
  [ -n "$name" ] || name="$prefix-$id"
  info "Lade Artefakt '$name' nach build/ …"
  if gh run download "$id" -n "$name" -D build; then
    ok "Artefakt liegt unter build/ (siehe oben)."
  else
    echo "⚠  Download fehlgeschlagen (Blob/Netzwerk). Browser-Fallback:"
    echo "    gh run view $id --web   →  Artefakte → herunterladen"
    echo "    (oder: gh api /repos/$REPO/actions/runs/$id/artifacts)"
    return 1
  fi
}

# ---------- Status ----------
if [ "$CMD" = "status" ]; then
  require_gh
  info "Repo: $REPO · Branch: $BRANCH · Workflow: $WORKFLOW"
  if workflow_exists; then
    ok "Workflow '$WORKFLOW' ist aktiv."
  else
    die "Workflow fehlt — einmalig: ./Tools/gh_android.sh activate"
  fi
  echo
  gh run list --workflow "$WORKFLOW" --limit 5
  exit 0
fi

# ---------- Workflow aktivieren (einmalig) ----------
if [ "$CMD" = "activate" ]; then
  require_gh
  if workflow_exists; then
    ok "Workflow '$WORKFLOW' existiert bereits."
    exit 0
  fi
  [ -f ".github/workflows/$WORKFLOW" ] || die ".github/workflows/$WORKFLOW fehlt lokal"
  info "Lege Workflow per gh api in '$REPO' an (Branch $BRANCH) …"
  B64="$(base64 -w0 ".github/workflows/$WORKFLOW" 2>/dev/null || base64 ".github/workflows/$WORKFLOW" | tr -d '\n')"
  gh api -X PUT "/repos/$REPO/contents/.github/workflows/$WORKFLOW" \
    -f "message=Add Android-11–15 CI workflow (gh CLI)" \
    -f "content=$B64" \
    -f "branch=$BRANCH" >/dev/null
  ok "Workflow angelegt. Nächster Schritt: ./Tools/gh_android.sh build apk"
  exit 0
fi

# ---------- Build ----------
trigger_build() { # $1 = build_aab (true/false)
  require_gh
  workflow_exists || die "Workflow fehlt — ./Tools/gh_android.sh activate"
  info "Starte Workflow '$WORKFLOW' auf Branch '$BRANCH' (build_aab=$1) …"
  gh workflow run "$WORKFLOW" --ref "$BRANCH" -f "build_aab=$1"
  sleep 8
  local id
  id="$(gh run list --workflow "$WORKFLOW" --branch "$BRANCH" --limit 1 --json databaseId -q '.[0].databaseId')"
  [ -n "$id" ] || die "Kein Run gefunden — Start verzögert? 'gh run list --workflow $WORKFLOW' prüfen"
  echo "$id" > build/.last_run_id
  info "Run-ID: $id"
}

if [ "$CMD" = "build" ]; then
  case "$ARG" in
    apk) trigger_build false; watch_and_download "$(cat build/.last_run_id)" "$APK_PREFIX" ;;
    aab) trigger_build true;  watch_and_download "$(cat build/.last_run_id)" "$AAB_PREFIX" ;;
    all) trigger_build true
         watch_and_download "$(cat build/.last_run_id)" "$APK_PREFIX"
         watch_and_download "$(cat build/.last_run_id)" "$AAB_PREFIX" ;;
    *) der "Nutzung: build apk|aab|all" ;;
  esac
  ls -lh build/*.apk build/*.aab 2>/dev/null || true
  exit 0
fi

# ---------- Emulator-Smoke-Test (Android 11–15) ----------
if [ "$CMD" = "emulator" ]; then
  API="${ARG:-35}"
  case "$API" in 30|31|32|33|34|35) ;; *) die "API muss 30–35 sein (Android 11–15)";; esac
  require_gh
  workflow_exists || die "Workflow fehlt — ./Tools/gh_android.sh activate"
  info "Starte Build + Emulator-Test auf API $API …"
  gh workflow run "$WORKFLOW" --ref "$BRANCH" \
    -f "build_aab=false" -f "emulator_test=true" -f "emulator_api=$API"
  sleep 8
  id="$(gh run list --workflow "$WORKFLOW" --branch "$BRANCH" --limit 1 --json databaseId -q '.[0].databaseId')"
  echo "$id" > build/.last_run_id
  watch_and_download "$id" "$APK_PREFIX"
  exit 0
fi

# ---------- Download ----------
if [ "$CMD" = "download" ]; then
  require_gh
  id="${ARG:-$(cat build/.last_run_id 2>/dev/null || die 'run-id angeben oder zuerst: ./Tools/gh_android.sh build apk')}"
  gh run download "$id" -n "$APK_PREFIX-$id" -D build || true
  gh run download "$id" -n "$AAB_PREFIX-$id" -D build || true
  ls -lh build/*.apk build/*.aab 2>/dev/null || true
  exit 0
fi

# ---------- Release ----------
if [ "$CMD" = "release" ]; then
  require_gh
  TAG="${ARG:?Nutzung: release vX.Y.Z}"
  workflow_exists || die "Workflow fehlt — ./Tools/gh_android.sh activate"
  info "Tagge $TAG und pushe (Workflow läuft auf Tags v*) …"
  git tag -f "$TAG"
  git push -f origin "$TAG"
  sleep 8
  id="$(gh run list --workflow "$WORKFLOW" --branch "$TAG" --limit 1 --json databaseId -q '.[0].databaseId')"
  [ -n "$id" ] || die "Kein Tag-Run gefunden"
  echo "$id" > build/.last_run_id
  watch_and_download "$id" "$APK_PREFIX"
  gh release create "$TAG" \
    build/PennerKombat-debug.apk \
    --title "Penner Kombat $TAG" \
    --notes "Android 11–15 (API 30–35) Debug-APK — via GitHub CLI/CI gebaut." \
    --verify-tag
  ok "Release '$TAG' erstellt (APK angehängt)."
  exit 0
fi

# ---------- Installieren ----------
if [ "$CMD" = "install" ]; then
  command -v adb >/dev/null 2>&1 || die "adb (Android platform-tools) fehlt"
  APK="${ARG:-build/PennerKombat-debug.apk}"
  [ -f "$APK" ] || die "$APK fehlt — zuerst: ./Tools/gh_android.sh build apk"
  adb install -r "$APK"
  ok "Installiert. Start: adb shell monkey -p com.pennerkombat.mobile -c android.intent.category.LAUNCHER 1"
  exit 0
fi

die "Unbekanntes Kommando '$CMD' — status|activate|build apk|build aab|build all|emulator [30–35]|download <id>|release <tag>|install"
