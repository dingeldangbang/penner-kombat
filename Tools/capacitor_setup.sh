#!/usr/bin/env bash
# Capacitor Setup — verpackt web/3d.html als native Android App
# Voraussetzung: Node + npm
set -e
cd "$(dirname "$0")/.."
WEB_DIR="web"

echo "=== Capacitor Setup für Penner Kombat 3D ==="

if ! command -v node >/dev/null 2>&1; then echo "❌ Node fehlt — https://nodejs.org installieren"; exit 1; fi
if ! command -v npm >/dev/null 2>&1; then echo "❌ npm fehlt"; exit 1; fi

cd "$WEB_DIR"

if [ ! -f "package.json" ]; then
  echo "→ package.json anlegen"
  cat > package.json <<'JSON'
{
  "name": "penner-kombat-3d",
  "version": "1.0.0",
  "description": "Penner Kombat 3D — Le Binde vs Mojo Bob Pose Arena",
  "type": "module",
  "scripts": {
    "build": "echo 'no build needed'",
    "serve": "python3 -m http.server 8000"
  },
  "dependencies": {
    "@capacitor/core": "^5.7.0",
    "@capacitor/android": "^5.7.0"
  },
  "devDependencies": {
    "@capacitor/cli": "^5.7.0"
  }
}
JSON
fi

echo "→ npm install"
npm install

if [ ! -f "capacitor.config.json" ] && [ ! -f "capacitor.config.ts" ]; then
  echo "→ cap init"
  npx cap init "Penner Kombat 3D" com.dingelanggames.pennerkombat --web-dir=. || true
fi

# capacitor.config.json anpassen für 3d.html als start
if [ -f "capacitor.config.json" ]; then
  echo "→ config anpassen (start 3d.html)"
  cat capacitor.config.json | python3 -c "
import json,sys
data=json.load(sys.stdin)
data['webDir']='.'
data['server']={'androidScheme':'https'}
print(json.dumps(data, indent=2))
" > capacitor.config.json.tmp && mv capacitor.config.json.tmp capacitor.config.json
fi

# Android Plattform hinzufügen
if [ ! -d "android" ]; then
  echo "→ Android Plattform hinzufügen"
  npx cap add android || echo "⚠ Android add fehlgeschlagen — Android Studio / SDK prüfen"
else
  echo "✅ Android Plattform vorhanden"
fi

# Icon / Splash: nutze vorhandenes SVG
mkdir -p android/app/src/main/res/drawable 2>/dev/null || true

echo "→ Sync"
npx cap sync android || true

echo
echo "=== FERTIG ==="
echo "Web 3D läuft bereits: http://localhost:8000/3d.html (python3 -m http.server 8000 --directory web)"
echo "Für native APK:"
echo "  cd web"
echo "  npx cap open android   # öffnet Android Studio"
echo "  # In Android Studio: Build → Build Bundle(s) / APK(s) → Build APK(s)"
echo
echo "Voraussetzungen Android Studio:"
echo "  - Android SDK 24-34"
echo "  - NDK r23+"
echo "  - JDK 17"
echo "  Siehe docs/NATIVE_PREREQ.md Abschnitt A-2"
