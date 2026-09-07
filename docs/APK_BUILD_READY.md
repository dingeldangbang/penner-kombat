# Android APK build via GitHub Actions

The native Godot 4 project is configured to export an installable Android APK
for **Android 11–15 (API 30–35)**. See [ANDROID_11_15.md](ANDROID_11_15.md)
for the full dependency/integration matrix.

## GitHub workflow

[`.github/workflows/android-apk.yml`](../.github/workflows/android-apk.yml) uses
[`barichello/godot-ci:4.7.2`](https://github.com/abarichello/godot-ci), the
Godot CI image containing the Godot 4.7.2 export templates, Android SDK,
build-tools, JDK 17 and a debug keystore.

No Unity licence, Android SDK installation, or repository secret is required.
The workflow:

1. imports the project headlessly, so broken scenes/scripts fail early;
2. exports the `Android` preset from `export_presets.cfg` as
   `build/PennerKombat-debug.apk`;
3. verifies that the APK was written; and
4. stores it as a downloadable GitHub Actions artifact for 30 days.

It runs on a pull request, on relevant pushes to `main` or `arena/**`, for
version tags (`v*`), and can always be started from **Actions → Game CI —
Android APK → Run workflow**. With the workflow-dispatch input **build_aab**
the same run additionally exports a debug-signed **AAB** (`Android AAB` preset,
minSdk 30 / targetSdk 35, Gradle build incl. SDK Platform 36, Build-Tools
36.1.0 and NDK r29).

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

With Godot 4.7.2, Android export templates and the Android dependencies
installed (`./Tools/android_setup.sh`), the CI command is:

```bash
mkdir -p build
godot --headless --path . --editor --quit
godot --headless --path . --export-debug "Android" build/PennerKombat-debug.apk
```

AAB (Google Play, minSdk 30 / targetSdk 35):

```bash
./Tools/install_android_build_template.sh            # once
godot --headless --path . --export-debug "Android AAB" build/PennerKombat.aab
```

## Release signing

The committed workflow deliberately produces a debug APK on every push so it
works without secrets. On **version tags (`v*`)** a dedicated job
`Signed APK → GitHub Release` exports a signed APK, creates a GitHub Release
and attaches the APK as a downloadable asset:

`git tag v1.1.0 && git push origin v1.1.0` — or `./Tools/gh_android.sh release v1.1.0`.

### Real release signing (recommended for distribution)

Create a private keystore once, **never commit it**, and add three repository
secrets. Godot reads them at export time via `GODOT_ANDROID_KEYSTORE_RELEASE_*`:

```bash
keytool -v -genkeypair -keystore release.keystore -alias pennerkombat \
  -keyalg RSA -keysize 2048 -validity 10000
# keystore-Passwort == Key-Passwort (Godot-Anforderung)

base64 -w0 release.keystore > release.keystore.b64     # macOS: base64 -i …-o …

gh secret set ANDROID_KEYSTORE_BASE64 < release.keystore.b64
gh secret set ANDROID_KEYSTORE_PASSWORD
gh secret set ANDROID_KEY_ALIAS        # z. B. pennerkombat
```

With all three secrets set, every `v*` tag produces a **release-signed** APK;
the job fails loudly if the secrets are incomplete. Without them the job emits
a warning and uploads a **debug-signed** APK (fine for sideloading/tests, not
for Play Store distribution). Keep the keystore file and passwords outside the
repository — never commit a keystore or its passwords; the project never
requires them in `export_presets.cfg`.
