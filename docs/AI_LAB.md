# 🤖 KI-Werkstatt — Runtime-AI-Config-Pipeline (480p)

> `web/lab.html` · Module in `web/src/lab/` · Server-Proxy `server/src/ai-proxy.js`

Die KI-Werkstatt setzt exakt die beschriebene Architektur um: ein hochgeladenes
`.glb` wird in einer **hart auf 480p gesperrten** Three.js-Szene gerendert, und
ein Chat-Interface übersetzt natürliche Sprache in **strukturierte JSON-Daten**,
die die laufende Game-Loop sofort übernimmt. Es wird **niemals KI-Code
ausgeführt** — die KI ist ein Daten-Übersetzer, kein Code-Generator.

```
[ User lädt .glb ] ──> [ Three.js Canvas (854×480 WebGL, CSS-gestreckt) ]
                                   │
[ User tippt im Chat ] ─> [ AI Engine ] ─> [ Text → JSON geparst ]
 "Feueratem + schwere              │
  Kick-Kombo"                      └───────> [ Direkt in den Move-Katalog ]
```

## Schnellstart

```bash
# Variante A: statisch (nur lokaler Parser, kein Key nötig)
cd web && python3 -m http.server 8000     # → http://localhost:8000/lab.html

# Variante B: mit Proxy + optionalem echtem LLM
node server/src/ai-proxy.js                       # → http://localhost:8080/lab.html
PORT=8080 OPENAI_API_KEY=sk-… node server/src/ai-proxy.js
```

Tests: `cd web && npm test` (26 Tests allein für die Pipeline).

---

## 1 · Die 480p-Auflösungssperre

`web/src/lab/res480.js`

```js
renderer.setPixelRatio(1);              // sonst multipliziert DPR den Puffer wieder hoch
renderer.setSize(854, 480, false);      // false = CSS-Größe NICHT anfassen
canvas.style.width = '100%';            // CSS streckt weich über den Bildschirm
canvas.style.height = '100%';
```

* `antialias: false` — Kantenglättung kostet Füllrate und nimmt den Retro-Look.
* `clampTo480p(w, h)` klemmt jeden Wunsch auf das 480p-Pixelbudget (409 920 px)
  und behält dabei das Seitenverhältnis.
* Presets: `480p` (854×480), `360p`, `240p`, `144p` — mehr als 480p ist nicht
  wählbar, weder über UI noch über API.
* `installResolutionLock()` hängt sich an `resize` und `orientationchange`, damit
  Drehen des Handys den Puffer nicht heimlich vergrößert.

Ergebnis: identische GPU-Last auf 4K-Monitor und Billig-Android.

## 2 · Die Chat-Engine

`web/src/lab/aiEngine.js` — zwei Backends, ein Ausgabeformat:

| Modus | Beschreibung |
|---|---|
| `local` (Standard) | Regelbasierter Parser für Deutsch **und** Englisch. Kein API-Key, offline, deterministisch, in Tests abgesichert. |
| `remote` | Beliebiger OpenAI-kompatibler `/chat/completions`-Endpunkt. `response_format: json_object` + Systemprompt mit Asset-Manifest. Bei Ausfall automatischer Rückfall auf `local`. |

Der Systemprompt (`buildSystemPrompt()`) enthält das komplette Manifest, das
Schema und die Balance-Grenzen — die KI kann also gar nicht erst Assetnamen
erfinden, die es nicht gibt.

Der lokale Parser erkennt u. a.:

* **Elemente** → VFX: Feuer/Feueratem, Eis, Blitz, Gift, Schatten, Blut, Bier, Münze, Bodenstampfer
* **Zahlen**: „25 Schaden“, „12 Startup“, „30 Active“, „HP auf 150“, „Tempo 7“
* **Trefferzahlen**: „3-Treffer-Kombo“, „4 Treffer“, „Kombo mit drei“
* **Eingabefolgen**: „runter vorne schwerer Schlag“ oder Numpad `236HP`
* **Optik**: Farbwörter, Hex (`#ff0000`), leuchten, größer/kleiner, Wireframe, Aura
* **Löschen**: „entferne den Feueratem“

## 3 · Das erwartete JSON-Format

```json
{
  "requestType": "UPDATE_ABILITIES",
  "combos": [
    {
      "name": "Triple Jab",
      "inputSequence": ["LIGHT_PUNCH", "LIGHT_PUNCH", "LIGHT_PUNCH"],
      "totalDamage": 15,
      "hits": 3,
      "animationClipName": "punch_combo_1",
      "hitboxShape": "medium_sphere",
      "sfxAsset": "whoosh_light",
      "cancelWindowFrames": 14
    }
  ],
  "specialAttacks": [
    {
      "name": "Fire Breath",
      "inputSequence": ["DOWN", "FORWARD", "HEAVY_PUNCH"],
      "vfxAsset": "fire_particle_stream",
      "sfxAsset": "fire_roar",
      "hitboxShape": "wide_cone",
      "damage": 25,
      "startupFrames": 12,
      "activeFrames": 30,
      "recoveryFrames": 18,
      "projectile": false,
      "statusEffect": "burn",
      "animationClipName": "cast_forward"
    }
  ],
  "visualOverrides": { "tintColor": "#cc2222", "emissiveIntensity": 0.9, "scale": 1.0, "auraVfx": "shadow_aura" },
  "stats": { "maxHP": 150, "moveSpeed": 7 },
  "removeMoves": ["Alter Move"],
  "notes": "kurze Erklärung fürs Chat-Log"
}
```

### Härtung (`web/src/lab/schema.js`)

Jede KI-Antwort — lokal wie remote — läuft durch `validateConfig()`:

* Framedaten & Schaden werden auf Balance-Grenzen **geklemmt**
  (damage 0–60, totalDamage 0–120, startup 1–60, active 1–90, maxHP 50–400).
* Unbekannte Assetnamen werden per `resolveAsset()` auf den nächstbesten
  echten Baustein gemappt (`"super_mega_explosion"` → `fire_blast`) und geloggt.
* Eingabe-Tokens werden normalisiert (`"236HP"`, `"runter vorne schwerer Schlag"`
  → `["DOWN","FORWARD","HEAVY_PUNCH"]`).
* Unbekannte Felder (z. B. `onUpdate: "while(true){}"`) fallen ersatzlos raus —
  es existiert kein Pfad von KI-Text zu `eval`, `Function` oder DOM.
* JSON aus Markdown-Fences oder Fließtext wird per `extractJson()` herausgelöst.

## 4 · Laufzeit-Asset-Bindung

`web/src/lab/fighter.js` — `AIConfigurableFighter.applyAiConfiguration(cfg)`:

1. **Specials** → `moveCatalog[name] = { sequence, damage, startup, active, recovery, vfxType, sfx, hitbox, status, clip }`
2. **Combos** → gleicher Katalog, `kind: 'combo'`, Schaden auf die Trefferzahl verteilt
3. **Optik** → `model.traverse()` setzt `material.color`, `emissive`, `wireframe`, `scale`
4. **Werte** → `stats` (maxHP, moveSpeed, defense, jumpForce)
5. **Löschen** → Einträge fliegen aus dem Katalog

Eingaben laufen über `InputBuffer` (`web/src/lab/inputBuffer.js`): Ring-Puffer mit
Zeitfenster (700 ms pro Token), `bestMatch()` bevorzugt die **längste** passende
Sequenz, damit `DOWN,FORWARD,HP` nicht vom simplen `HP` geschluckt wird.

Ausführung folgt echten Framedaten: `startup → active → recovery` bei 60 fps.
Im `active`-Fenster werden VFX gespawnt, die Hitbox aufgebaut und der Treffer
gegen den Trainingsdummy ausgewertet.

Animationen: Bringt das GLB Clips mit, spielt ein `AnimationMixer` den in
`animationClipName` genannten Clip (Fuzzy-Match auf Clipnamen). Ohne Clips
laufen VFX, Frames und Hitboxen trotzdem.

## 5 · Die Asset-Bibliothek (entscheidender Schritt)

`web/src/lab/assetLibrary.js` — das Manifest, das die KI liest:

| Kategorie | Einträge |
|---|---|
| **VFX-Pakete** | `fire_blast`, `fire_particle_stream`, `ice_shards`, `shadow_aura`, `blood_splatter`, `electricity`, `poison_cloud`, `dust_burst`, `gold_sparks`, `beer_spray` |
| **Audio-Pools** | `whoosh_light`, `whoosh_heavy`, `impact_flesh`, `impact_metal`, `grunt_male`, `energy_charge`, `fire_roar`, `ice_crack`, `zap`, `coin_ding` |
| **Hitbox-Formen** | `small_sphere` (schnelle Stiche), `medium_sphere`, `wide_cone` (Bodenschläge/Atem), `long_box` (Schwerthiebe), `ground_slam` (Ring), `full_body` (Grab) |
| **Eingabe-Tokens** | `UP DOWN FORWARD BACK LIGHT_PUNCH HEAVY_PUNCH LIGHT_KICK HEAVY_KICK BLOCK GRAB SPECIAL` |
| **Status-Effekte** | `burn bleed poison slow stun launch armor invert` |

VFX sind GPU-Points mit Pooling (`web/src/lab/vfx.js`), Audio wird per WebAudio
synthetisiert (`web/src/lab/audioPool.js`) — keine Downloads, keine Dateien.

## Steuerung im Lab

| Aktion | Taste |
|---|---|
| Richtungen | `W A S D` / Pfeiltasten |
| Leichter / schwerer Schlag | `J` / `K` |
| Leichter / schwerer Tritt | `N` / `M` |
| Block / Grab / Special | `Shift` / `G` / `Space` |

Zusätzlich Touch-Buttons unter dem Viewport und ein „▶ Testen“-Knopf pro Move.

## Server-Proxy

`server/src/ai-proxy.js` liefert `web/` statisch aus **und** bietet:

| Route | Zweck |
|---|---|
| `GET /api/ai/health` | Status, ob ein Remote-LLM konfiguriert ist |
| `POST /api/ai/config` | Weiterleitung an `OPENAI_BASE_URL` mit serverseitigem Key |

Umgebungsvariablen: `PORT`, `HOST`, `WEB_ROOT`, `OPENAI_API_KEY`,
`OPENAI_BASE_URL` (OpenAI, OpenRouter, Groq, lokales llama.cpp …), `LLM_MODEL`.
Ohne Key antwortet der Proxy mit `503 no_api_key` — der Client wechselt dann
still auf den lokalen Parser, das Lab bleibt voll benutzbar.

## Vier fertige Kampfprofile

`web/src/lab/presets.js` · als reine Daten auch unter
[`web/assets/fighter-profiles.json`](../web/assets/fighter-profiles.json)

In der Werkstatt links per Klick, im Chat per Namen („Lade das Profil
Cyber-Scorpion“) oder direkt im Code:

```js
fighter.injectAiConfiguration(PRESETS.cyber_scorpion.config);
// Alias von applyAiConfiguration(), nimmt rohes JSON und validiert es zuerst.
```

| Profil | Rolle | Optik | Special | Combo |
|---|---|---|---|---|
| 🦂 **Cyber-Scorpion** | Nano-Ninja | Chrom (`metalness 1.0`, `roughness 0.12`) + Elektro-Aura | **Magnet Grab** — `screen_long_box`, `statusEffect: pull`, zieht den Gegner quer über den Screen | **Overload** — 4× `LIGHT_PUNCH`, 24 DMG, 4 Startup-Frames |
| 🧟 **Toxic Blood-Ghoulem** | Heavy Bruiser | Karmesin-Violett, nass (`roughness 0.08`) | **Acid Vomit** — `wide_cone`, `guardBreak: true`, zersetzt normales Blocken | **Brutal Carnage** — 16 Startup-Frames, 34 Frames Hitstun, 42 DMG |
| 🕯️ **Voodoo Shadow-Priest** | Zoning Teleporter | Pechschwarz + Void-Aura | **Abyssal Rift** — `aoe_sphere` mit `spawnAt: ground_target`, also exakt unter den Koordinaten des Gegners | **Soul Reap** — leichter Kick aus Distanz, 30 Frames Cancel-Fenster |
| ☢️ **Radioactive Bio-Mech** | Juggernaut | Neongrün, `emissiveIntensity 1.4`, Scale 1.3 | **Meltdown Slam** — `shockwave_ring`, 26/20/**46** Frames, 38 DMG, 50 Meter | **Heavy Core Smash** — `wallBounce: true`, 34 DMG |

Dafür wurden Bibliothek und Schema erweitert:

* **Neue VFX**: `magnet_pull` (Sog nach innen), `void_rift`, `radiation_burst`, `acid_spray`
* **Neue Audio-Pools**: `magnet_hum`, `acid_sizzle`, `void_whisper`, `geiger_click`, `quake_boom`
* **Neue Hitboxen**: `screen_long_box` (7 m), `aoe_sphere`, `shockwave_ring`
* **Neue Status-Effekte**: `pull`, `guard_break`, `wallbounce`, `corrode`, `knockdown`
* **Neue Felder**: `spawnAt` (`self` | `target` | `ground_target`), `guardBreak`, `pullStrength`,
  Combo-`startupFrames`/`hitStunFrames`/`recoveryFrames`/`wallBounce`,
  Material-`metalness`/`roughness`

Die Reaktion des Gegners ist sichtbar: `pull` zieht den Dummy heran,
`wallbounce`/`launch` schleudert ihn weg, `knockdown` kippt ihn.

## Export / Import

Der komplette Katalog lässt sich als `pk-fighter-config.json` exportieren und
wieder importieren (läuft durch dieselbe Validierung). Zusätzlich merkt sich das
Lab die letzte Konfiguration in `localStorage` (`pk_lab_v1`).

## Grenzen (ehrlich)

* Die KI erzeugt **keine** neue Geometrie, keine Shader, keine Sounds — nur
  Referenzen auf vorhandene Bausteine. Das ist Absicht, siehe Schritt 5.
* Der Trainingsdummy ist ein Distanz-Check, keine vollständige Hitbox-Simulation
  des Hauptspiels (`web/src/sim3d.js`).
* Netcode/Rollback ist nicht Teil des Labs — Konfigurationen werden per JSON
  geteilt, nicht live synchronisiert.
