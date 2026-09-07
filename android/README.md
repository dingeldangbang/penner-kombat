# Android-Ordner (Godot 4.7) — Android 11–15 (API 30–35)

Dieser Ordner ist die **Godot-4-Schnittstelle** für den Android-Build und ersetzt
das frühere Unity-Manifest (`Assets/Plugins/Android/AndroidManifest.xml`).

## Was Godot selbst erzeugt

- **Prebuilt-Template-Export (APK, Preset „Android“)**: Godot patcht beim Export
  Paketname, Version, Berechtigungen, `uses-feature`, Orientation usw. direkt in
  das Template-APK (`EditorExportPlatformAndroid::_fix_manifest`). Ein eigenes
  Manifest ist dafür **nicht** nötig und wird auch nicht gelesen.
- **Gradle-Build (AAB, Preset „Android AAB“)**: Godot installiert das
  Android-Build-Template nach `android/build/` (siehe unten) und schreibt die
  aus dem Export-Preset generierten Manifeste nach
  `android/build/src/debug/AndroidManifest.xml` bzw. `src/release/…`.
  Die Template-Hauptdatei `android/build/src/main/AndroidManifest.xml` wird
  damit **gemerged**.

## Build-Template installieren

```bash
./Tools/install_android_build_template.sh          # aus Template-Order der Export-Templates
./Tools/install_android_build_template.sh --relay  # zusätzlich usesCleartextTraffic=true setzen
```

Das Skript entpackt `android_source.zip` nach `android/build/`, schreibt
`android/.build_version` (muss zur Godot-Version passen, z. B. `4.7.2.stable`)
und legt `.gdignore` an. `android/build/` wird nicht versioniert
(siehe `.gitignore`), es ist reproduzierbar aus den Export-Templates erzeugbar.

## Berechtigungen (Android 11–15)

Beide Export-Presets setzen die Minimalmenge:

| Permission | Zweck | Android 11+ |
|---|---|---|
| `INTERNET` | WebSocket-Relay (`NetworkManager`, `WebSocketClient`), Asset-Downloads (`AdvancedAssetImporter`) | ohne Runtime-Prompt |
| `ACCESS_NETWORK_STATE` | Netzwerkstatus erkennen | ohne Runtime-Prompt |
| `VIBRATE` | Touch-Haptik | ohne Runtime-Prompt |
| `WAKE_LOCK` | Bildschirm im Kampf wach halten | ohne Runtime-Prompt |

`READ/WRITE_EXTERNAL_STORAGE` werden **bewusst nicht** mehr gesetzt: Seit
Android 11 (API 30) sind sie für `user://` Speicher überflüssig (Scoped
Storage), Godot nutzt das App-Verzeichnis. Das alte Unity-Manifest mit diesen
Permissions ist damit entfernt.

## Relay-Anbindung (`ws://`)

- **Empfohlen (Android 11–15, Produktion):** `wss://` (TLS) verwenden — ohne
  Cleartext-Ausnahme, ohne Manifest-Patch. Siehe `docs/SERVER.md`.
- **Lokal/Entwicklung (`ws://localhost` bzw. LAN-IP):** Android blockiert
  Klartext ab API 28. Mit installiertem Build-Template einmalig
  `./Tools/install_android_build_template.sh --relay` ausführen — das Skript
  ergänzt `android:usesCleartextTraffic="true"` in
  `android/build/src/main/AndroidManifest.xml` (idempotent).
- Ohne Gradle-Build (Prebuilt-APK) gilt: `usesCleartextTraffic` kann nur über
  einen Gradle-/Custom-Template-Build gesetzt werden — gleichwertige Alternative
  ist `wss://` oder der in `web/` vorhandene Capacitor-Wrapper.

## Dateien

| Datei | Inhalt |
|---|---|
| `README.md` | diese Übersicht |
| `build/` | **generiert** — Gradle-Build-Template (nicht versioniert) |
| `.build_version` | **generiert** — Godot-Version des Templates (nicht versioniert) |
