# ✅ Abnahme: KI-Werkstatt & die vier Kampfprofile

Stand: 23.08.2026 · Branch `arena/01a03067-penner-kombat`

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
| Laufzeit-Engine mit echtem three.js | `web/test/engine.test.mjs` | 16 | ✅ grün |
| Oberfläche in jsdom | `web/test/ui.dom.test.mjs` | 12 | ✅ grün |
| **Summe** | | **65** | **✅ 65/65** |

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

### Oberfläche
* Vier Profil-Buttons injizieren per Klick, Profil-Label und JSON-Tab folgen.
* Tastatur `A · D · G` löst *Magnet Grab* aus, vier Touch-Taps auf `LP` lösen
  *Overload* aus — beide Eingabewege landen im selben Puffer.
* „▶ Testen“ startet einen Move, „Entfernen“ löscht ihn aus Katalog und Liste.
* Reset räumt Katalog, Label und Liste auf; Material kehrt exakt zum
  Ausgangszustand zurück (Farbe, Metalness, Roughness).
* Konfiguration überlebt einen Reload (`localStorage: pk_lab_v1`).
* Asset-Bibliothek zeigt 14 VFX, 15 Audio-Pools, 9 Hitbox-Formen; Tabs schalten.

### Server
* `GET /api/ai/health` → `{"ok":true,"remoteEnabled":false,…}`
* `POST /api/ai/config` ohne Key → `503 no_api_key` (Client wechselt still auf
  den lokalen Parser)
* `lab.html`, `src/lab/main.js`, `src/lab/lab.css` werden mit `200` ausgeliefert.

## 3 · Was hier *nicht* geprüft werden konnte

| Punkt | Grund | Wie du es prüfst |
|---|---|---|
| Echtes WebGL-Bild, GPU-Framerate | Kein Browser installierbar — Chrome-Download (storage.googleapis.com) und Debian-Repos sind in dieser Sandbox gesperrt | `node Tools/lab_browser_check.mjs` nach `npm install puppeteer` — prüft Puffergröße, Rendering, Profile, Chat, Tastatur, Auflösung und legt einen Screenshot ab |
| GLB-Upload mit echter Datei | Kein Datei-Dialog, kein Testmodell im Repo; `GLTFLoader` ist im DOM-Test gestubbt | Beliebiges `.glb` in die Werkstatt ziehen — Modell wird auf 1,8 m normalisiert, Clips erscheinen unter „Charakter laden“, vorhandene Moves bleiben gebunden |
| Remote-LLM gegen echte API | Kein API-Key in der Sandbox (die Fallback- und Klemm-Logik ist mit Mock-`fetch` getestet) | `OPENAI_API_KEY=sk-… node server/src/ai-proxy.js`, dann in der UI auf „Remote LLM“ stellen |
| Audio | jsdom hat keinen `AudioContext` (Code prüft darauf und bleibt still) | Im Browser: jeder Treffer und Move spielt seinen Pool-Sound |

## 4 · Bekannte Grenzen

* Der Trainingsdummy ist ein Distanz-Check gegen die Hitbox-Reichweite, keine
  volle Kollisionssimulation wie `web/src/sim3d.js`.
* `statusEffect` wirkt im Lab visuell und im Treffer-Event; die Damage-over-Time-
  Ticks des Hauptspiels laufen dort, nicht in der Werkstatt.
* Die KI erzeugt weiterhin keine Geometrie und keine Shader — bewusst, siehe
  „Asset-Bibliothek“ in [AI_LAB.md](AI_LAB.md).
