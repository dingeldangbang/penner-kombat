#!/usr/bin/env bash
# Installiert/verifiziert die Android-11–15-Abhängigkeiten für Penner Kombat
# (Godot 4.7 + Android API 30–35):
#   - OpenJDK 17 (prüft; Hinweise falls fehlt)
#   - Android SDK: platform-tools, build-tools 35.0.1 + 36.1.0,
#     platforms;android-35 + android-36, cmake 3.22.1, NDK 29.0.14206865
#   - Godot Editor Settings (export/android/java_sdk_path, android_sdk_path)
#
# Aufruf aus dem Repo-Wurzelverzeichnis:
#   ./Tools/android_setup.sh                 # SDK-Download nach $ANDROID_HOME
#   ANDROID_HOME=/opt/android-sdk ./Tools/android_setup.sh
set -euo pipefail
cd "$(dirname "$0")/.."

SDK_HOME="${ANDROID_HOME:-${ANDROID_SDK_ROOT:-$HOME/Android/Sdk}}"
OS_NAME="$(uname -s)"
if [ "$OS_NAME" = "Darwin" ]; then
  CMD_TOOLS_URL="https://dl.google.com/android/repository/commandlinetools-mac-11076708_latest.zip"
else
  CMD_TOOLS_URL="https://dl.google.com/android/repository/commandlinetools-linux-11076708_latest.zip"
fi
PYTHON_BIN="${PYTHON:-python3}"

echo "=== PENNER KOMBAT — ANDROID 11–15 SETUP ==="
echo "Android SDK: $SDK_HOME"
echo

# ---------- 1. Java ----------
echo "--- 1. OpenJDK 17 ---"
if command -v java >/dev/null 2>&1; then
  JAVA_VER="$(java -version 2>&1 | head -n1)"
  echo "java gefunden: $JAVA_VER"
  if ! java -version 2>&1 | grep -qiE '"(17|21|23|25)'; then
    echo "WARNUNG: Godot empfiehlt OpenJDK 17 (andere Versionen möglich)."
  fi
else
  echo "❌ java fehlt. Installiere OpenJDK 17, z. B.:"
  echo "   Debian/Ubuntu : sudo apt install openjdk-17-jdk"
  echo "   macOS/Homebrew: brew install openjdk@17"
  echo "   Windows       : https://adoptium.net/temurin/releases/?variant=openjdk17"
  exit 1
fi

# ---------- 2. Android SDK ----------
echo
echo "--- 2. Android SDK (API 30–35) ---"
mkdir -p "$SDK_HOME"
if [ ! -x "$SDK_HOME/cmdline-tools/latest/bin/sdkmanager" ]; then
  echo "cmdline-tools werden installiert (einmalig, ~150 MB) …"
  TMP="$(mktemp -d)"
  curl -fsSL -o "$TMP/clt.zip" "$CMD_TOOLS_URL"
  mkdir -p "$SDK_HOME/cmdline-tools"
  unzip -q "$TMP/clt.zip" -d "$TMP/clt"
  rm -rf "$SDK_HOME/cmdline-tools/latest"
  mv "$TMP/clt/cmdline-tools" "$SDK_HOME/cmdline-tools/latest"
  rm -rf "$TMP"
fi
SDKMANAGER="$SDK_HOME/cmdline-tools/latest/bin/sdkmanager"

# Lizenzstände
yes | "$SDKMANAGER" --licenses >/dev/null 2>&1 || true

echo "Pakete werden installiert (Build-Tools, Plattformen, CMake, NDK r29) …"
# NDK r29 wird vom Godot-4.7-Gradle-Template verlangt (config.gradle);
# Build-Tools 35.0.1 liefert apksigner/zipalign für den Template-APK-Export,
# 36.1.0 wird vom Gradle-Build (compileSdk 36) verwendet.
"$SDKMANAGER" \
  "platform-tools" \
  "build-tools;35.0.1" \
  "build-tools;36.1.0" \
  "platforms;android-35" \
  "platforms;android-36" \
  "cmake;3.22.1" \
  "ndk;29.0.14206865"

echo "✅ Android-SDK bereit: $SDK_HOME"

# ---------- 3. Godot ----------
echo
echo "--- 3. Godot 4.7.x ---"
if command -v godot >/dev/null 2>&1; then
  echo "godot: $(godot --version 2>/dev/null | head -n1)"
else
  echo "⚠  godot (CLI) nicht im PATH. Editor/Headless von"
  echo "    https://godotengine.org/download/windows/ (4.7.x) installieren und"
  echo "    den Binärpfad in PATH aufnehmen; Export-Templates ebenfalls laden."
fi

# ---------- 4. Editor-Settings ----------
echo
echo "--- 4. Godot Editor-Settings (Android-Pfade) ---"
SETTINGS_DIR="$HOME/.config/godot"
SETTINGS_FILE="$SETTINGS_DIR/editor_settings-4.7.tres"
mkdir -p "$SETTINGS_DIR"
JAVA_HOME_DIR="$(dirname "$(dirname "$(readlink -f "$(command -v java)")")")"
# Nur schreiben, wenn noch nicht vorhanden (überschreibt keine User-Werte)
if [ ! -f "$SETTINGS_FILE" ]; then
  {
    echo '[gd_resource type="EditorSettings" format=3]'
    echo '[resource]'
    echo "export/android/java_sdk_path = \"$JAVA_HOME_DIR\""
    echo "export/android/android_sdk_path = \"$SDK_HOME\""
  } > "$SETTINGS_FILE"
  echo "geschrieben: $SETTINGS_FILE"
else
  echo "vorhanden (nicht überschrieben): $SETTINGS_FILE"
  echo "Bitte prüfen: export/android/java_sdk_path + android_sdk_path"
fi

# ---------- 5. Build-Template (für AAB) ----------
echo
echo "--- 5. Gradle-Build-Template ---"
if [ -f android/build/build.gradle ]; then
  echo "✅ android/build schon installiert"
else
  echo "Nächster Schritt für AAB-Builds (optional):"
  echo "  ./Tools/install_android_build_template.sh --relay"
fi

echo
echo "=== Fertig. Nächste Schritte ==="
echo "  ./Tools/android_check.sh                 # alles prüfen"
echo "  ./Tools/install_android_build_template.sh --relay   # nur für AAB / ws://-Relay"
echo "  godot --headless --path . --export-debug \"Android\" build/PennerKombat-debug.apk"
