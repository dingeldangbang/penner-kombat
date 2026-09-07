#!/usr/bin/env bash
# Prüft die Android-11–15-Bereitschaft (API 30–35) des Repos und des Rechners.
# Aufruf aus dem Repo-Wurzelverzeichnis: ./Tools/android_check.sh
set -u
cd "$(dirname "$0")/.."

fail=0; ok=0; warn=0
okc(){ echo "✅ $1"; ok=$((ok+1)); }
warnc(){ echo "⚠  $1"; warn=$((warn+1)); }
failc(){ echo "❌ $1"; fail=$((fail+1)); }

echo "=== PENNER KOMBAT — ANDROID 11–15 CHECK (API 30–35) ==="

# 1. Projektwerte
echo
echo "--- Export-Preset ---"
for kv in 'min_sdk="30"' 'target_sdk="35"' 'use_gradle_build=true' 'permissions/internet=true' 'permissions/vibrate=true'; do
  grep -q "$kv" export_presets.cfg && okc "export_presets.cfg: $kv" || warnc "export_presets.cfg: $kv nicht gefunden"
done
grep -q 'compress_native_libraries=false' export_presets.cfg && okc "16-KB-Page-Size: native Libs unkomprimiert" || failc "compress_native_libraries fehlt / ist true (16-KB-Anforderung!)"

# 2. Manifest / Relay
echo
echo "--- Android-Manifest ---"
if grep -q 'usesCleartextTraffic="true"' android/build/src/main/AndroidManifest.xml 2>/dev/null; then
  okc "android/build: usesCleartextTraffic=true (ws://-Relay erlaubt)"
else
  warnc "android/build nicht installiert oder ohne Cleartext (für ws:// nötig; wss:// geht immer)"
fi
if [ -f android/.build_version ]; then
  okc "android/.build_version: $(cat android/.build_version)"
else
  warnc "android/.build_version fehlt — AAB-Build nicht möglich (Tools/install_android_build_template.sh)"
fi

# 3. Verbotene Legacy-Permissions
echo
echo "--- Legacy-Permissions ---"
if grep -qE 'READ_EXTERNAL_STORAGE|WRITE_EXTERNAL_STORAGE' android/AndroidManifest.xml export_presets.cfg 2>/dev/null; then
  failc "READ/WRITE_EXTERNAL_STORAGE sollte entfernt sein"
else
  okc "keine READ/WRITE_EXTERNAL_STORAGE (Scoped Storage, Android 11+)"
fi

# 4. Rechner
echo
echo "--- Toolchain ---"
if command -v godot >/dev/null 2>&1; then
  v="$(godot --version 2>/dev/null | head -n1)"
  case "$v" in *4.7*|*4.6*|*4.5*) okc "Godot $v (targetSdk 35/16 KB support)";; *) warnc "Godot $v — für targetSdk 35 wird 4.5+ empfohlen";; esac
else
  warnc "godot nicht im PATH (nur CI-Builds möglich)"
fi
if command -v java >/dev/null 2>&1; then
  j="$(java -version 2>&1 | head -n1)"
  echo "$j" | grep -q '"17' && okc "OpenJDK 17 gefunden" || warnc "$j (Godot empfiehlt JDK 17)"
else
  failc "java fehlt"
fi
SDK_HOME="${ANDROID_HOME:-${ANDROID_SDK_ROOT:-$HOME/Android/Sdk}}"
SM="$SDK_HOME/cmdline-tools/latest/bin/sdkmanager"
if [ -x "$SM" ]; then
  okc "sdkmanager: $SM"
else
  warnc "sdkmanager nicht unter $SDK_HOME — ./Tools/android_setup.sh ausführen"
fi

# 5. Zusammenfassung
echo
echo "Ergebnis: $ok ok, $warn Warnungen, $fail Fehler"
[ "$fail" -eq 0 ] || exit 1
exit 0
