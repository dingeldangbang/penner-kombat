#!/usr/bin/env bash
# Native App Voraussetzungen prüfen — ohne Unity, nur Repo-Check
# Aufruf aus Repo-Wurzel: ./Tools/native_check.sh
set -e
cd "$(dirname "$0")/.."
echo "=== PENNER KOMBAT — NATIVE PREREQ CHECK ==="
echo "Datum: $(date)  Repo: $(pwd)"
echo

fail=0
ok=0
warn=0

check_ok(){ echo "✅ $1"; ok=$((ok+1)); }
check_fail(){ echo "❌ $1"; fail=$((fail+1)); }
check_warn(){ echo "⚠  $1"; warn=$((warn+1)); }

# 1. Unity Version
echo "--- Unity Version ---"
if [ -f "ProjectSettings/ProjectVersion.txt" ]; then
  ver=$(cat ProjectSettings/ProjectVersion.txt | grep m_EditorVersion | awk '{print $2}')
  echo "Gefunden: $ver"
  if [[ "$ver" == "2022.3.62f1" ]]; then check_ok "Unity Version 2022.3.62f1 LTS (erwartet)"; else check_warn "Unity Version $ver (erwartet 2022.3.62f1, Unity 6 migriert auto)"; fi
else
  check_fail "ProjectSettings/ProjectVersion.txt fehlt — kein Unity Projekt?"
fi

# 2. Packages
echo
echo "--- Packages ---"
if [ -f "Packages/manifest.json" ]; then
  check_ok "Packages/manifest.json vorhanden"
  for pkg in "com.unity.render-pipelines.universal" "com.unity.inputsystem" "com.unity.textmeshpro"; do
    if grep -q "$pkg" Packages/manifest.json; then check_ok "Paket $pkg"; else check_fail "Paket $pkg fehlt in manifest.json"; fi
  done
else
  check_fail "Packages/manifest.json fehlt"
fi

# 3. Android Manifest
echo
echo "--- Android ---"
if [ -f "Assets/Plugins/Android/AndroidManifest.xml" ]; then
  check_ok "AndroidManifest.xml vorhanden"
  for perm in "INTERNET" "VIBRATE" "ACCESS_NETWORK_STATE"; do
    if grep -q "$perm" Assets/Plugins/Android/AndroidManifest.xml; then check_ok "Permission $perm"; else check_warn "Permission $perm fehlt"; fi
  done
else
  check_fail "Assets/Plugins/Android/AndroidManifest.xml fehlt"
fi

# 4. Tags & Layers
echo
echo "--- Tags & Layers ---"
if [ -f "ProjectSettings/TagManager.asset" ]; then
  for tag in Ground Fighter Interactable Projectile; do
    if grep -q "$tag" ProjectSettings/TagManager.asset; then check_ok "Tag $tag"; else check_fail "Tag $tag fehlt"; fi
  done
  for layer in Fighter Interactable Projectile; do
    if grep -q "$layer" ProjectSettings/TagManager.asset; then check_ok "Layer $layer"; else check_warn "Layer $layer fehlt (wird von PkProjectSetup erzeugt)"; fi
  done
else
  check_fail "TagManager.asset fehlt"
fi

# 5. Szenen
echo
echo "--- Szenen ---"
if [ -f "Assets/Scenes/Arena.unity" ]; then
  check_ok "Arena.unity vorhanden"
else
  check_warn "Assets/Scenes/Arena.unity fehlt — wird von GameSetupWizard / PkBuildPreparer erzeugt"
  if ls Assets/*.unity 2>/dev/null || find Assets -name "*.unity" | grep -q .; then
    check_ok "Andere Szenen vorhanden: $(find Assets -name "*.unity" | head -n 3)"
  else
    check_warn "Keine Szenen im Projekt — Build wäre leer"
  fi
fi

# 6. FighterDatabase
echo
echo "--- FighterDatabase ---"
if find Assets -name "FighterDatabase.asset" | grep -q .; then
  check_ok "FighterDatabase.asset vorhanden"
else
  check_warn "FighterDatabase.asset fehlt — wird zur Laufzeit via EnsureDefaultRoster() erzeugt"
fi

# 7. Editor Scripts
echo
echo "--- Editor Build Tools ---"
for f in PkProjectSetup.cs PkBuildPreparer.cs PkQuickStart.cs PkReadinessCheck.cs GameSetupWizard.cs BuildScript.cs; do
  if [ -f "Assets/Scripts/Editor/$f" ]; then check_ok "Editor/$f"; else check_fail "Editor/$f fehlt"; fi
done

# 8. Core Scripts
echo
echo "--- Core / Kampf / Mobile ---"
for f in FighterController.cs FighterFactory.cs FighterInput.cs MobileTuning.cs TouchControls.cs GameManager.cs Bootstrapper.cs; do
  if find Assets -name "$f" | grep -q .; then check_ok "$f"; else check_fail "$f fehlt"; fi
done

# 9. Web 3D
echo
echo "--- Web 3D Arena (Le Binde vs Mojo Bob) ---"
if [ -f "web/3d.html" ]; then check_ok "web/3d.html vorhanden"; else check_fail "web/3d.html fehlt"; fi
for f in pose.js fighter3d.js arena3d.js render3d.js sim3d.js input3d.js main3d.js data.js; do
  if [ -f "web/src/$f" ]; then check_ok "web/src/$f"; else check_fail "web/src/$f fehlt"; fi
done
if grep -q "three" web/3d.html; then check_ok "Three.js ImportMap in 3d.html"; else check_fail "Three.js ImportMap fehlt"; fi
if [ -f "web/manifest.webmanifest" ]; then check_ok "PWA Manifest"; else check_warn "Manifest fehlt"; fi

# 10. Node / npm / Relay
echo
echo "--- Node / Relay Server ---"
if command -v node >/dev/null 2>&1; then check_ok "Node $(node --version)"; else check_warn "Node nicht installiert — für Relay Server & Capacitor nötig"; fi
if command -v npm >/dev/null 2>&1; then check_ok "npm $(npm --version)"; else check_warn "npm fehlt"; fi
if [ -f "server/package.json" ]; then check_ok "server/package.json vorhanden"; else check_warn "server/package.json fehlt"; fi

# 11. Capacitor (optional)
echo
echo "--- Capacitor (Web als native App) ---"
if [ -f "web/package.json" ] && grep -q "capacitor" web/package.json 2>/dev/null; then check_ok "Capacitor in web/package.json"; else check_warn "Capacitor nicht in web/package.json — ./Tools/capacitor_setup.sh ausführen für native Wrapper"; fi

# 12. Syntax Check
echo
echo "--- Syntax Check (statisch) ---"
if command -v python3 >/dev/null 2>&1; then
  if python3 Tools/check_braces.py 2>&1 | grep -q "OK"; then check_ok "Klammern Check OK"; else check_warn "Klammern Check meldet Probleme"; fi
else
  check_warn "python3 fehlt für check_braces.py"
fi
if command -v node >/dev/null 2>&1; then
  for f in web/src/pose.js web/src/sim3d.js web/src/data.js; do
    if node --check "$f" 2>&1; then check_ok "Syntax $f"; else check_fail "Syntax $f fehlerhaft"; fi
  done
fi

echo
echo "=== ZUSAMMENFASSUNG ==="
echo "✅ OK: $ok  ⚠ Warn: $warn  ❌ Fail: $fail"
if [ $fail -eq 0 ]; then
  echo "▶ Native App Voraussetzungen ERFÜLLT (mit $warn Warnungen, die automatisch behoben werden)."
  echo "  Nächste Schritte:"
  echo "  - Unity: Tools → Penner Kombat → ✔ Spielbereitschaft prüfen"
  echo "  - Unity: Tools → Penner Kombat → ▶ Alles einrichten und spielen"
  echo "  - Unity: Tools → Penner Kombat → Build/Android APK (Release)"
  echo "  - Web: python3 -m http.server 8000 --directory web → http://localhost:8000/3d.html"
  echo "  - Capacitor: ./Tools/capacitor_setup.sh"
else
  echo "❌ Es fehlen $fail wesentliche Dateien — siehe oben."
fi

# Exit code 0 wenn nur Warnungen, 1 wenn Fails
exit $fail
