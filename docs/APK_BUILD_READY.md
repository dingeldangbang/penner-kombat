# Penner Kombat — APK Build Ready

The Godot 4 Android project is prepared for APK export.

## Current branch / PR

- Branch: `arena/01a02f73-penner-kombat`
- PR: https://github.com/dingeldangbang/penner-kombat/pull/4

## Local APK export

Install Godot 4.2.x with Android export templates, then run:

```bash
mkdir -p build
godot --headless --path . --export-debug "Android" build/pennerkombat_debug.apk
```

Or from the Godot editor:

1. Open the repository as a Godot project.
2. Go to `Project > Export`.
3. Select `Android`.
4. Export as `build/pennerkombat_debug.apk`.

## Android files included

- `project.godot`
- `export_presets.cfg`
- `android/AndroidManifest.xml`
- `android/res/xml/file_paths.xml`
- `icon.svg`

## GitHub Actions note

A workflow file has been prepared at `.github/workflows/android_build.yml`, but GitHub rejected pushing workflow files from the current Arena GitHub App token because it lacks `workflows` permission.

Reconnect GitHub in Arena with workflow permission, then commit/push the workflow file to enable automatic APK artifact builds.
