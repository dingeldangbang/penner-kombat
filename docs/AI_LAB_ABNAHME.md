# ✅ Abnahme: KI-Werkstatt & die vier Kampfprofile

Stand: 23.08.2026 · Branch `arena/01a03067-penner-kombat` · **92 Tests grün** · App-Version **1.1.0**

Was hier steht, wurde **ausgeführt**, nicht behauptet. Reproduzieren:

```bash
cd web
npm install            # three (Engine-Tests) + jsdom (UI-Tests)
npm test               # 53 Tests: Simulation, Pipeline, Laufzeit-Engine
npm run test:ui        # 12 Tests: Oberfläche in jsdom
node ../server/src/ai-proxy.js   # http://localhost:8080/lab.html
```

## 1 · Testergebnis

| Suite | Datei | Tests | Status |
|---|---|---:|---|
| Kampfsimulation (Bestand) | `web/test/sim.test.mjs` | 11 | ✅ grün |
| KI-Pipeline (Parser, Schema, 480p, Puffer) | `web/test/lab.test.mjs` | 26 | ✅ grün |
| Laufzeit-Engine mit echtem three.js (inkl. X-Ray, Fatality, Ragdoll, Stage, Hitstop) | `web/test/engine.test.mjs` | 27 | ✅ grün |
| Oberfläche in jsdom (inkl. FINISH HIM, Arena-Wurf, Kino-Banner) | `web/test/ui.dom.test.mjs` | 17 | ✅ grün |
| Auslieferung/PWA (Manifest, Offline-Cache, Vendor, App-Shell) | `web/test/app.test.mjs` | 11 | ✅ grün |
| **Summe** | | **92** | **✅ 92/92** |

## 2 · Nachgewiesene Funktionalität

### Render-Sperre
* WebGL-Puffer ist nach dem Booten exakt **854×480**, CSS-Breite `100 %`,
  `setPixelRatio(1)` — nachgewiesen im DOM-Test, nicht nur im Code.
* Umschalten auf 240p ändert den Puffer real auf 426×240 und wieder zurück.
* `clampTo480p(3840, 2160)` bleibt im 480p-Pixelbudget und behält 16:9.

### Chat → JSON → Game Loop
* Prompt „Gib ihm einen Feueratem mit 25 Schaden, Eingabe runter vorne schwerer
  Schlag“ erzeugt einen Move mit `damage 25`, `["DOWN","FORWARD","HEAVY_PUNCH"]`,
  `vfxAsset fire_particle_stream` — und die Karte erscheint in der Move-Liste.
* Unverständliche Eingaben („hallo wie geht es dir“) erzeugen **keinen** Move,
  sondern eine Fehlermeldung im Chat (`NOOP`).
* Remote-LLM: Antwort wird geparst **und** geklemmt (`maxHP 9999 → 400`);
  fällt der Endpunkt aus, übernimmt der lokale Parser (`source: local-fallback`).
* Kein Pfad von KI-Text zu Code: Felder wie `onUpdate: "while(true){}"` fallen
  in der Validierung ersatzlos raus.

### Die vier Profile
| Prüfung | Ergebnis |
|---|---|
| Validierung aller vier Blöcke | ✅ ok, **0 Warnungen** |
| Katalogbindung je Profil | ✅ 2 Specials + 1 Combo, Karten = Katalogeinträge |
| Cyber-Scorpion: Chrom-Material | ✅ `metalness 1`, `roughness 0.12`, Farbe `c8d2dc`, Emissive `00bfff` |
| Magnet Grab zieht den Gegner | ✅ Dummy von x = 7,0 auf < 6,0 gezogen, `magnet_pull` gespawnt |
| Overload 4-Treffer-Kette | ✅ feuert erst beim 4. `LIGHT_PUNCH`, 24 DMG; whifft korrekt auf 2,6 m |
| Acid Vomit bricht den Block | ✅ Treffer meldet `guardBreak: true`, `acid_spray` im Kegel |
| Brutal Carnage träge + hoher Hitstun | ✅ 16 Startup-, 34 Hitstun-Frames |
| Abyssal Rift unter dem Gegner | ✅ VFX exakt auf `x/z` des Gegners, `y < 0,3` (Boden) |
| Soul Reap als Distanz-Starter | ✅ `LIGHT_KICK`-Sequenz, 30 Frames Cancel-Fenster |
| Meltdown Slam Framedaten | ✅ gemessene **92 Frames** = 26 Startup + 20 Active + 46 Recovery |
| Heavy Core Smash Wandbounce | ✅ `wallBounce: true` gemeldet, Dummy > 1 m weggeschleudert |
| Keine kollidierenden Eingaben | ✅ pro Profil eindeutige Sequenzen |
| Alle vier gleichzeitig geladen | ✅ 12 Moves, Export → Reimport verlustfrei |

### Hardcore-Stufe (X-Ray, Fatality, Ragdoll, Arena, Hitstop)
| Prüfung | Ergebnis |
|---|---|
| Doku-JSON (Spine Shatter + Acid Meltdown) 1:1 übernommen | ✅ Werte unverändert, `acid_spit_corrosive` → `acid_corrosive` gemappt |
| Irrsinnige Kino-Werte geklemmt | ✅ `damage 9999 → 90`, `hitstopFrames 999 → 30`, `boneTarget LEBER → SPINE_T3`, `finisherType TELEPORT_TO_MARS → EXPLOSION` |
| X-Ray ohne Leiste | ✅ feuert **nicht**, meldet „Leiste erst bei 100 %" |
| X-Ray mit voller Leiste | ✅ Zeitlupe 0,15×, Kamerafahrt läuft, Leiste wird auf 0 verbraucht, Treffer sitzt am Zoom-Frame |
| Leiste füllt sich durch Treffer | ✅ 12 pro Special, 6 + 2/Treffer pro Combo |
| Fatality im Normalzustand | ✅ zündet **nicht** |
| Fatality nach „FINISH HIM!" | ✅ zündet, `DISMEMBERMENT`, Ragdoll an, Ende sauber |
| Ragdoll-Physik | ✅ `EXPLOSION` = 6 Teile, Opfer unsichtbar, Teile fallen und bleiben auf dem Boden (kein Durchfallen), `clear()` stellt alles wieder her |
| Stage: Wurf | ✅ Fass fliegt ballistisch, trifft den Dummy, meldet Schaden |
| Stage: Wandsprung / nichts in Reichweite | ✅ `ESCAPE` bzw. `null` + Meldung |
| Hitstop friert die Spielzeit | ✅ `director.update()` liefert exakt `0` während des Hitstops, danach wieder normal |
| Zeitlupe skaliert dt | ✅ `1/60 × 0,2`; Kamera bewegt sich auf der Kurve und kehrt exakt in die Ruhelage zurück |
| Chat-Prompts | ✅ „X-Ray auf die Rippen" → `RIBCAGE`, „Fatality mit Säure" → `MELTDOWN`, „Fässer und Gasflasche in die Arena" → 2 Objekte |
| Export/Reimport der Hardcore-Blöcke | ✅ X-Ray, Fatality, Arena und Wucht-Profil verlustfrei |

### Oberfläche
* Vier Profil-Buttons injizieren per Klick, Profil-Label und JSON-Tab folgen.
* Tastatur `A · D · G` löst *Magnet Grab* aus, vier Touch-Taps auf `LP` lösen
  *Overload* aus — beide Eingabewege landen im selben Puffer.
* „▶ Testen“ startet einen Move, „Entfernen“ löscht ihn aus Katalog und Liste.
* Reset räumt Katalog, Label und Liste auf; Material kehrt exakt zum
  Ausgangszustand zurück (Farbe, Metalness, Roughness).
* Konfiguration überlebt einen Reload (`localStorage: pk_lab_v1`).
* Asset-Bibliothek zeigt 20 VFX, 21 Audio-Pools, 9 Hitbox-Formen; Tabs schalten.
* Hardcore-Tab: X-Ray-, Fatality- und Arena-Karten mit Framedaten und Testknöpfen.
* Dummy auf 0 HP → **FINISH HIM!**-Banner mit Eingabeliste, Statuszeile folgt;
  Fatality-Eingabe `S · S · D · M` startet Kamerafahrt und Ragdoll.
* <kbd>E</kbd> wirft ein Arena-Objekt; ohne Objekt in Reichweite kommt eine Meldung.
* X-Ray blendet Letterbox und Kino-Banner ein (`body.cinema`).

### Auslieferbare App (v1.1.0)
| Prüfung | Ergebnis |
|---|---|
| Keine CDN-Referenzen | ✅ three.js liegt lokal in `web/vendor/` (0.160.0, MIT) — geprüft über alle HTML/JS/CSS |
| Import-Maps zeigen lokal | ✅ `lab.html` und `3d.html` mappen `three` und `three/addons/` auf `./vendor/three/…` |
| three + GLTFLoader ladbar | ✅ real importiert: `REVISION 160`, `GLTFLoader` ist eine Klasse; GLTFLoader importiert nur `three` und relative Pfade |
| Offline-Cache vollständig | ✅ 40 Dateien im Service Worker, jede existiert; umgekehrt ist jede `src/lab/*`-Datei gelistet |
| Service Worker | ✅ versioniert (`v5`), `/api/` nie gecacht, Offline-Fallback, Update-Pfad (`skipWaiting`) |
| Manifest installierbar | ✅ `id`, `scope`, `display`, maskable Icon, 3 Shortcuts — alle Ziele existieren |
| App-Shell eingebunden | ✅ alle drei Seiten laden `src/app.js` (SW-Registrierung, Install-Prompt, WebGL-Check, Version) |
| Keine doppelte SW-Registrierung | ✅ die alte Inline-Registrierung in `index.html` ist entfernt |
| Build-Skript | ✅ `Tools/build_web_app.sh --with-tests` → 46 Dateien, 1,3 MB entpackt, **324 KB ZIP**, bricht bei verletzten Kriterien ab |
| Gebautes Paket läuft eigenständig | ✅ aus `release/penner-kombat-web/` ausgeliefert: alle Seiten, Vendor, Icons, `BUILD_INFO.txt` → HTTP 200 |
| MIME-Typen | ✅ `text/javascript`, `application/manifest+json` — Voraussetzung für Module und Installation |

### Server
* `GET /api/ai/health` → `{"ok":true,"remoteEnabled":false,…}`
* `POST /api/ai/config` ohne Key → `503 no_api_key` (Client wechselt still auf
  den lokalen Parser)
* `lab.html`, `src/lab/main.js`, `src/lab/lab.css` werden mit `200` ausgeliefert.

## 3 · Was hier *nicht* geprüft werden konnte

| Punkt | Grund | Wie du es prüfst |
|---|---|---|
| Echtes WebGL-Bild, GPU-Framerate, Kamerafahrt in Bewegung | Kein Browser installierbar — Chrome-Download (storage.googleapis.com) und Debian-Repos sind in dieser Sandbox gesperrt | `node Tools/lab_browser_check.mjs` nach `npm install puppeteer` — prüft Puffergröße, Rendering, Profile, Chat, Tastatur, Auflösung, X-Ray-Kamerafahrt und Fatality-Ragdoll und legt Screenshots ab |
| GLB-Upload mit echter Datei | Kein Datei-Dialog, kein Testmodell im Repo; `GLTFLoader` ist im DOM-Test gestubbt | Beliebiges `.glb` in die Werkstatt ziehen — Modell wird auf 1,8 m normalisiert, Clips erscheinen unter „Charakter laden“, vorhandene Moves bleiben gebunden |
| Remote-LLM gegen echte API | Kein API-Key in der Sandbox (die Fallback- und Klemm-Logik ist mit Mock-`fetch` getestet) | `OPENAI_API_KEY=sk-… node server/src/ai-proxy.js`, dann in der UI auf „Remote LLM“ stellen |
| Audio | jsdom hat keinen `AudioContext` (Code prüft darauf und bleibt still) | Im Browser: jeder Treffer und Move spielt seinen Pool-Sound |
| Echte Installation als PWA + Offline-Start | Braucht einen Browser mit Service-Worker-Unterstützung (in dieser Sandbox nicht installierbar); Manifest, Cache-Liste und Registrierung sind statisch geprüft | App über HTTPS oder `localhost` öffnen → „App installieren", danach Netz trennen und neu starten |
| GitHub-Pages-Deploy | Der Sandbox-Token hat keine Admin-/Workflow-Rechte, Pages ist im Repo nicht aktiviert | `cp docs/ci/pages-deploy.yml .github/workflows/pages.yml`, Settings → Pages → „GitHub Actions" |

## 4 · Bekannte Grenzen

* Der Trainingsdummy ist ein Distanz-Check gegen die Hitbox-Reichweite, keine
  volle Kollisionssimulation wie `web/src/sim3d.js`.
* `statusEffect` wirkt im Lab visuell und im Treffer-Event; die Damage-over-Time-
  Ticks des Hauptspiels laufen dort, nicht in der Werkstatt.
* Die KI erzeugt weiterhin keine Geometrie und keine Shader — bewusst, siehe
  „Asset-Bibliothek“ in [AI_LAB.md](AI_LAB.md).
