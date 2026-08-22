# 📱 HANDY — Penner Kombat auf dem Telefon spielen

Kein Unity auf dem eigenen Rechner nötig. GitHub baut, du spielst.

---

## 1. Zwei Wege, ehrlich verglichen

| | **WebGL im Browser** | **Android-APK** |
|---|---|---|
| Aufwand | Link öffnen | APK laden + „Unbekannte Quellen" erlauben |
| Leistung | ~60–70 % von nativ | volle Leistung |
| Touch-Steuerung | ✅ | ✅ |
| Vibration (Haptik) | ❌ | ✅ |
| Offline spielbar | nach dem ersten Laden teilweise | ✅ |
| Online-Relay | ✅ (WebSocket) | ✅ |
| LAN-Erkennung | ❌ (kein UDP im Browser) | ✅ |
| Ladezeit | einige MB beim ersten Aufruf | einmalig installieren |

**Empfehlung:** Zum schnellen Ausprobieren WebGL, zum echten Spielen das APK.

---

## 2. Einmalige Vorbereitung (ca. 15 Minuten, nur ein Browser nötig)

1. **Unity-Konto** anlegen (kostenlos): <https://id.unity.com>
2. Workflow-Vorlagen aktivieren:
   ```bash
   mkdir -p .github/workflows
   cp docs/ci/unity-activation.yml .github/workflows/
   cp docs/ci/android-build.yml   .github/workflows/
   cp docs/ci/webgl-pages.yml     .github/workflows/
   git add .github/workflows/ && git commit -m "ci: Builds" && git push
   ```
3. **Actions → „Unity-Lizenz anfordern (.alf)" → Run workflow**
   → Artefakt herunterladen, `.alf` auf <https://license.unity3d.com/manual> hochladen
   → `.ulf` öffnen, Inhalt kopieren
4. **Settings → Secrets and variables → Actions**: `UNITY_LICENSE` = Inhalt der `.ulf`
   (dazu `UNITY_EMAIL` und `UNITY_PASSWORD`)
5. **Actions → „Android-APK bauen" → Run workflow** → APK unter *Artifacts*

Details und Fehlerbilder: [`ci/README.md`](ci/README.md)

---

## 3. Was das Spiel auf dem Handy automatisch macht

| Bereich | Verhalten |
|---|---|
| **Steuerung** | On-Screen-Layout erscheint automatisch: Joystick links, `□ △ ○ ✕`, BLOCK halten, S2, X-RAY rechts |
| **Gesten** | Wischfolgen werden in Kommandoeingaben übersetzt (↓↘→ usw.), siehe `TOUCH.md` |
| **Layout** | in den Optionen frei verschiebbar, vier Vorlagen (Standard, Fighting, Simple, Linkshänder) |
| **Vibration** | bei Treffern, abschaltbar |
| **Ausrichtung** | Querformat erzwungen, Bildschirm schläft im Kampf nicht ein |
| **Leistung** | `MobileTuning` stuft das Gerät ein und passt an: |

| Geräteklasse | Erkennung | Anpassung |
|---|---|---|
| **Stark** | ≥ 6 GB RAM, ≥ 8 Kerne | 60 fps, Schatten bis 30 m |
| **Mittel** | ≥ 3,5 GB RAM, ≥ 6 Kerne | 60 fps, harte Schatten bis 18 m, keine Zuschauer |
| **Schwach** | alles darunter | 30 fps, keine Schatten, 75 % Auflösung, keine Zuschauer und Verbündeten |

Das Ergebnis steht beim Start im Log (`adb logcat`), falls du nachsehen willst.

---

## 4. Build-Einstellungen, die die CI selbst setzt

`Assets/Scripts/Editor/PkBuildPreparer.cs` läuft im Batchmode automatisch und erledigt,
was sonst jemand im Editor anklicken müsste:

- Tags, Layer, Defines (`PK_URP`, ggf. `PK_GLTFAST`)
- **Arena-Szene erzeugen und in die Build Settings eintragen** — ohne das baut Unity ein leeres Spiel
- FighterDatabase anlegen
- Paketname `com.dingelanggames.pennerkombat`, **min. Android 7 (API 24)**, IL2CPP, ARM64 + ARMv7
- Grafik-API Vulkan, sonst OpenGL ES3; Querformat; Vollbild
- WebGL: Gzip-Kompression, Exceptions aus, Daten-Caching an
- Qualität: MSAA aus, Schattendistanz gedeckelt, VSync aus

Im Editor erreichbar über `Tools → Penner Kombat → Build vorbereiten`.

---

## 5. APK aufs Handy bekommen

1. Artefakt-ZIP aus der Action herunterladen (geht auch direkt am Handy-Browser)
2. Entpacken → `PennerKombat.apk`
3. Beim Öffnen fragt Android nach der Erlaubnis „Apps aus dieser Quelle installieren" → erlauben
4. Installieren, starten, prügeln

Für den Play Store bräuchtest du ein signiertes AAB — der Keystore-Block liegt in
`android-build.yml` auskommentiert bereit.

---

## 6. Grenzen — ohne Beschönigung

- **Ungetestet.** Dieser Code wurde nie kompiliert und nie auf einem Gerät ausgeführt.
  Der erste CI-Lauf ist der erste echte Test.
- **Kapseln statt Figuren**, solange keine Modelle da sind. Spielbar, aber hässlich.
- **Zwei Spieler an einem Handy gehen nicht** — die Touch-Steuerung bedient aktuell nur
  Spieler 1. Zweiter Spieler geht über den Relay (online) oder Gamepad.
- **Kein Play-Store-Eintrag**, nur Sideload.
- **Prozeduraler Ton** statt echter Musik: Treffer, Block, 808-Kick und eine schäbige Fanfare.
