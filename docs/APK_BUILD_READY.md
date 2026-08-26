# Android APK build via GitHub Actions

The native Godot 4 project is configured to export an installable Android APK.

## GitHub workflow

[`.github/workflows/android-apk.yml`](../.github/workflows/android-apk.yml) uses
[`barichello/godot-ci:4.2.2`](https://github.com/abarichello/godot-ci), the
Godot CI image containing the Godot 4.2.2 export templates, Android SDK,
build-tools, JDK and a debug keystore.

No Unity licence, Android SDK installation, or repository secret is required.
The workflow:

1. imports the project headlessly, so broken scenes/scripts fail early;
2. exports the `Android` preset from `export_presets.cfg` as
   `build/PennerKombat-debug.apk`;
3. verifies that the APK was written; and
4. stores it as a downloadable GitHub Actions artifact for 30 days.

It runs on a pull request, on relevant pushes to `main` or `arena/**`, for
version tags (`v*`), and can always be started from **Actions → Game CI —
Android APK → Run workflow**.

## Activating the workflow (one-time)

GitHub only accepts workflow files from tokens with `workflows` permission.
If the automated push of `.github/workflows/android-apk.yml` was rejected,
activate the workflow manually once:

1. Open
   <https://github.com/dingeldangbang/penner-kombat/new/main?filename=.github/workflows/android-apk.yml>.
2. Copy the raw content of [`ci/android-apk-workflow.yml`](../ci/android-apk-workflow.yml)
   (pushable copy of the exact same file) into the editor.
3. Click **Commit changes** — the push itself triggers the first APK build.

## Downloading the APK

1. Open the successful **Game CI — Android APK** workflow run on GitHub.
2. Under **Artifacts**, download `PennerKombat-Android-debug-<run number>`.
3. Unzip it and install `PennerKombat-debug.apk` on an Android device.
4. If Android asks, allow the browser/file manager to install apps from this
   source. The build is debug-signed and intended for testing/sideloading.

## Local equivalent

With Godot 4.2.2, Android export templates and Android build dependencies
installed, the CI command is:

```bash
mkdir -p build
godot --headless --path . --editor --quit
godot --headless --path . --export-debug "Android" build/PennerKombat-debug.apk
```

## Release signing

The committed workflow deliberately produces a debug APK so it works without
secrets. A Play Store or production release must use a private release keystore
outside the repository and `--export-release`; never commit a keystore or its
passwords. Configure the release keystore fields in `export_presets.cfg` at CI
runtime from GitHub Secrets before adding a separate signed-release job.
