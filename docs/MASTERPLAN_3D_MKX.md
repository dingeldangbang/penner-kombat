# 🎬 MASTERPLAN: Grafik-Upgrade Richtung „Mortal Kombat X“ (3D) + Mobile-Server

**Stand:** 2026-09-07 · Branch `arena/01a079e0-penner-kombat`
**Zielbild:** Penner Kombat soll sich wie ein **MKX-Klasse 3D-Fighter** anfühlen
und aussehen — auf dem Desktop **und** auf Android 11–15 (API 30–35) mit
funktionierendem Online-Server (Matchmaking + TLS).

> Dieses Dokument ist die **Überlegungs- und Planungsbasis**. Die in diesem
> Branch bereits umgesetzten Bausteine sind mit ✅ markiert; alles andere ist
> die Roadmap mit Aufwandsschätzung.

---

## 1. Ausgangslage (ehrliche Analyse)

| Bereich | Heute | Problem | Ziel |
|---|---|---|---|
| Arena | Graue Box + ein Spot | Wirkt wie ein Prototyp, keine Atmosphäre | Cinematic Stage: Neon, Nebel, Crowd, Nassboden ✅ (Teil) |
| Post-Processing | Environment + Vignette | Flach, kein „Film-Look“ | ACES, Glow, Grain, Chromatic Aberration, Letterbox ✅ |
| Kamera | Fixer Sichtpunkt mit Lerp | Kein Game-Feel, kein Punch-Zoom | Cinematic Fight-Kamera ✅ |
| Treffer | Schaden + Screenshake | Fühlt sich „weich“ an | Hit-Stop, Sparks, Shockwave, Flash, Punch-Zoom ✅ |
| Charakter | GLB + Rig möglich, StandardMaterial | Kein MKX-Look (Rim-Light, Toon, Outline) | Charakter-Shader-Binder ✅ |
| Qualität | 3 grobe Presets, teils wirkungslos | Kein echtes Gerätemanagement | 5 Stufen + FSR/TSR + adaptive Auflösung ✅ |
| Server | Relay (Räume, Spiegelung) | Kein Matchmaking, kein TLS-Deployment, kein Monitoring | Matchmaking + REST + Docker + Caddy-TLS ✅ |
| **Art-Assets** | **Fehlen** (Modelle, Animationen, Arenen, SFX) | Das ist der **eigentliche Engpass** | Phase 1–3 (siehe unten) |

**Kernaussage:** Der Code-Anteil (Grafik-Systeme, Performance, Server) ist jetzt
an der Ziellinie angekommen. Was fehlt, ist **Content-Arbeit** — genau das, was
sich nicht prozedural erzeugen lässt (hochwertige Charakter-Modelle,
Animationen, Stage-Meshes). Dafür gibt es hier den Fahrplan.

---

## 2. Referenz: Was macht MKX-Grafik-Gameplay aus?

| Baustein | MKX-Charakteristik | Umsetzung Penner Kombat |
|---|---|---|
| **Kamera** | 3/4-Ansicht, dynamisches FOV, Punch-Zoom bei Treffern, Finisher-Zoom | `KombatCamera` ✅ (Phase 1-Feintuning) |
| **Hit-Stop** | 2–4 Frames kompletter Stillstand bei schweren Treffern | `ImpactFeedback._hitstop` ✅ |
| **Impact-VFX** | Sparks, Shockwave, Blut, Partikel-Flash | `HitSpark` + `Shockwave` + Hit-Flash ✅ |
| **Farbwelt** | Kalt-blauer Grundton, warme Neon-Akzente, Schwarz als Rahmen | Arena-Shader + Rim-Light ✅ |
| **Oberflächen** | Nasser Beton, Metall, Neon, Nebel, Raucheffekte | `WetFloor` + `ArenaSky` + FogVolume ✅ |
| **Charaktere** | Hochpolige Skulpturen, grobe PBR-Materialien, Rim-Light | Toon-PBR + Rim + Outline ✅; **Modelle fehlen** |
| **Bühnen** | Dichte Kulisse, Crowd, Zerstörung, mehrere Ebenen | Crowd + Props ✅; **Zerstörung systematisch ausbauen** |
| **UI** | Große Balken, Vitale Combo-Counter, „FIGHT!“-Ansagen | `HUD.announce`/`show_toast` ✅; **Skin-Polish** |
| **Sound** | Bass-Booms, knallende Treffer, Crowd-Raunen | `ProceduralAudio` vorhanden; **echte Tracks fehlen** |

---

## 3. Technische Architektur (Godot 4 / Forward+ / Mobile)

### 3.1 Rendering-Pipeline

- **Desktop:** `forward_plus` (SDF-basiertes GI, große Light-Arrays, SSR, SSAO).
- **Mobile:** `mobile`-Renderer (kein SDFGI, dafür FSR + optimierte Shadows) —
  bereits in `project.godot` konfiguriert (`renderer/rendering_method.mobile`).
- **Qualitätsstufen (neu ✅):** `GraphicsQuality` (Autoload) verwaltet 5 Stufen:

| Stufe | Auflösung | MSAA | SSAO | SSR | Vol. Fog | Shadows | Ziel |
|---|---|---|---|---|---|---|---|
| Niedrig | 0.66× Bilinear | aus | aus | aus | aus | 1024, hart | sehr alte Geräte (30–45 fps) |
| Mittel | 0.75× Bilinear | aus (mobil) | aus | aus | aus | 2048, weich | Android Mittelklasse (60 fps) |
| Hoch | 0.85× FSR | 2× (mobil) | Desktop | aus | aus | 4096, weich | Android Flaggschiff, PC |
| Ultra | 1.0× TSR | 4× | an | an | an | 4096 | PC |
| Kino | 1.0× TSR | 4× | an (2.0) | an (64 Steps) | an | 4096 | High-End-PC, Screenshots |

- **Adaptive Auflösung ✅:** misst FPS alle 1,25 s und skaliert die
  `scaling_3d_scale` zwischen 0.6 und Zielwert — beugt Thermal-Throttling vor.
- **Bedienung im Kampf:** `F1` Stufe wechseln · `F2` Adaptive an/aus ·
  `F3` Kino-FX an/aus (mit HUD-Toast).

### 3.2 Shader-Stack (neu ✅)

| Shader | Datei | Einsatz |
|---|---|---|
| Charakter Toon-PBR | `shaders/PennerCharacter3D.gdshader` | Rim-Light, Zellschattierung, Silhouetten-Outline, Hit-Flash |
| Nass-Boden | `shaders/WetFloor.gdshader` | Pfützen, Fresnel-Spiegelung, Neon-Gitter, Regen-Sparkle |
| Nachthimmel | `shaders/ArenaSky.gdshader` | Verlauf, Horizont-Glühen, Sterne |
| (bestand) | `GoldGlow`, `Grease`, `Pulse`, `XRay` | Ressourcen-FX, weiter nutzbar |

`CharacterShaderBinder` wandelt **zur Laufzeit** jedes geriggte GLB-Modell um
und erhält Textur/Farbe/Emission — kein Import-Reimport nötig.

### 3.3 Performance-Budget (mobil, Richtwerte)

| Metrik | Budget (Mittelklasse Android) | Methode |
|---|---|---|
| FPS | ≥ 55 stabil | Adaptive Auflösung, `Engine.max_fps` |
| Draw Calls | < 300 | Crowd als **eine** MultiMesh ✅, Props als Boxen |
| Dreiecke | < 150 k | LOD (LODManager) + Culling |
| Schatten | ≤ 2 Lichtquellen | Nur Sonne mit Schatten; Spot/Fill shadowless ✅ |
| Texturen | ETC2/ASTC | `project.godot` ✅ |
| State | < 900 MB | ASTC + Low-Tier-Regeln |

---

## 4. Umsetzungsfahrplan

### ✅ Phase 0 — Grundausbau (in diesem Branch erledigt)
- [x] `GraphicsQuality` (5 Stufen, FSR/TSR, adaptive Auflösung, Persistenz)
- [x] Arena-Dressing (`CinematicArenaBuilder`: Sky, WetFloor, Neon, Crowd, Fog, Props)
- [x] `KombatCamera` (Framing, FOV, Punch-/Finisher-Zoom)
- [x] `ImpactFeedback` (Hit-Stop, Sparks, Shockwave, Flash, Shake)
- [x] `PostProcessing` (ACES, Grain, Chromatic Aberration, Letterbox, Hit-Flash)
- [x] `CharacterShaderBinder` + `PennerCharacter3D.gdshader`
- [x] Server v1.1 (Matchmaking, REST, CORS) + Docker/Caddy/TLS

### 🟠 Phase 1 — Charaktere (2–4 Wochen, Art-Arbeit)
1. **Modell-Pipeline fixieren:** GLB/GLTF mit Standard-Mensch-Metarig
   (Root, Hips, Spine, Head, Shoulder.L/R, UpperArm, Forearm, Hand,
   UpperLeg, LowerLeg, Foot — der `DynamicRigger` erkennt diese Namen).
2. **9 Charaktere hochladen** (aus `docs/MODELLE.md` / `docs/DESIGN.md`):
   Kalle, Dieter, Rolf, Sigi, Mell, Uschi, Le Binde, MojoBob, TetraPak.
   Budget je Modell: 8–20 k Dreiecke, 1× Albedo-Atlas 2048, 1× Normal, 1× RMA.
3. **Animations-Set (Pflicht, 12 Clips je Charakter):** Idle, Walk, Run,
   LightAttack1–3, HeavyAttack, Block, HitReact, Knockdown, GetUp,
   Throw, Special1, Fatality. → `FighterAnimatorBuilder`/`AnimationsController3D`
   einbinden; Namen laut `docs/MOVESETS.md`.
4. **Material-Checks:** `CharacterShaderBinder`-Uniforms pro Charakter
   (Rim-Farbe, Toon-Bänder, Outline-Stärke) im `SkillData`-Profil ergänzen.
5. **Preview:** `CharacterAssetSuite.tscn` Vorschau auf ultimative
   Beleuchtung umstellen (Rim-Light-Kamera, Warm-Cool-Kontrast).

### 🟠 Phase 2 — Arenen (2–3 Wochen)
1. **5 Stage-Varianten** (`GlobalData.arena_type`): Kiez-Hinterhof, U-Bahn,
   Schrottplatz, Keller-Rave, Dachgarten — jeweils eigener Farb-LUT +
   Propset (Bierkisten, Wäscheleine, Neonschild, Müllcontainer).
2. **Halbe/volle Zerstörbarkeit:** `ArenaDestruction`-Regionen pro Stage,
   Trümmer-Partikel, Staub, prozedurales Splitting von Bodenplatten.
3. **Crowd-System:** pro Stage aktivieren; Beifall/Reaktionen an Combo-Count
   koppeln (`CrowdReactions`).
4. **UI-Skin:** Health-Bars mit Stage-Farbverlauf, Combo-Counter als
   Emblem, „FIGHT!“-Ansage mit Stage-Logo.

### 🟠 Phase 3 — Game-Feel-Feinschliff (1–2 Wochen)
1. **Hit-Stop-Daten:** pro Move `hitstop_ms` und `impact_power` in
   `MoveData`/`SkillData` (aktuelle Heuristik: `damage >= 14` → 45 ms).
2. **Blut/Ekstase-FX 2.0:** `FatalityBlood` auf Shader-Ebene (Particle-Mesh
   mit Velocity + Splat-Decals auf `WetFloor`), Blutdämpfe bei Kombos ≥ 8.
3. **Kameraschwenks:** Round-Intro (Kameraschwenk über beide Fighter),
   Fatality-Cam (Schulter), KO-Slow-Mo (bereits vorhanden, auf `KombatCamera` umziehen).
4. **Audio-Feedback:** Impact-Transienten (sub-bass Boom, „Thud“), Crowd-Gasps,
   Stage-Ambience — Lücken in `docs/SOUNDTRACK.md` schließen.

### 🟠 Phase 4 — Online-Vertiefung (2–3 Wochen, Server & Client)
1. **Matchmaking-UI** im Mobile-Menü: Mode (Ranked/Casual), Region,
   „Gegner suchen…“, Abbruch (`matchmaking_cancel`).
2. **Autorität:** aktueller Relay kennt keinen Spielzustand → entweder
   **deterministisches Lockstep** (RNG-seed + Input-Queue) oder **Client-Authority
   mit Plausibilitätsprüfung** (HP-Delta-Checks auf dem Server).
3. **Lobby-Chat + Ready-Flow** (existiert) auf Matchmaking-Räume mappen.
4. **Replay:** `state`-Nachrichten als kompaktes Event-Log auf dem Server
   puffern (max. 5 min/Raum), REST `GET /api/replays/:room`.
5. **Statistik:** `player_stats` (Wins/Losses/Damage/Combos) an REST
   `POST /api/stats` melden; einfaches Leaderboard als Tabelle.

### ✅ Phase 5 — Mobile-Server-Betrieb (in diesem Branch erledigt)
- [x] Matchmaking (`matchmaking`, `matchmaking_cancel`, Timeout 90 s)
- [x] REST: `/health`, `/api/status`, `/api/rooms` (CORS, für Dashboards)
- [x] Docker-Image + Healthcheck (`server/Dockerfile`)
- [x] Compose-Stack + Caddy-TLS (`docker-compose.yml`, `server/caddy/Caddyfile`)
- [x] 1-Befehl-Deploy (`Tools/server_deploy.sh`)

---

## 5. Mobile-Strategie im Detail

### 5.1 Geräteklassen

| Klasse | Beispiel | Stufe | Erwartung |
|---|---|---|---|
| Low (2018–2020) | Snapdragon 6xx, 3–4 GB | Niedrig | 30–45 fps, stabile 60 nur mit 0.66× |
| Mid (2021–2023) | Snapdragon 7xx, 6 GB | Mittel | 60 fps |
| High (2023+) | Snapdragon 8 Gen 2, 8 GB | Hoch | 60 fps, FSR an |
| Desktop | PC (Forward+) | Ultra/Kino | 120/144 fps, volle Effekte |

Erkennung läuft über `OS.get_name()`, gespeichert in `user://graphics.cfg`.
**Wichtig:** Die Stufe ist immer manuell übersteuerbar (F1) und wird
**nie** nach unten erzwungen — Thermal-Regelung übernimmt die adaptive Auflösung.

### 5.2 Android-Spezifika (API 30–35)

- **wss:// Pflicht:** `network/relay/url` in `project.godot` — Klartext ist
  ab API 28 blockiert (Doku: `docs/SERVER.md`, `docs/ANDROID_11_15.md`).
- **App-Pause:** Bei `pause` → WebSocket sauber schließen + `GraphicsQuality`
  pausieren (max_fps = 30), bei `resume` → reconnect + Qualität neu prüfen.
  (Hook in `NetworkManager`/`GraphicsQuality` vorsehen.)
- **Externe Storage/ODR:** große GLB-Modelle (oder komprimierte `.res`)
  per HTTP-Cache `user://` laden, nie im APK (APK-Größe < 150 MB Ziel).
- **Fingerabdruck:** Touch-Layer bereits vorhanden (`TouchInputManager`,
  `VirtualJoystick`) — in Phase 3 um „Swipe = Special“-Kombos und
  Vibrations-Feedback bei Hit-Stop ergänzen (`permissions/vibrate` vorhanden).

### 5.3 Dauerhafte FPS-Messung

```bash
adb logcat | grep PennerKombat
# oder im Spiel: F3-Toggle + Toast zeigt FPS & Scale (Phase 3: On-Screen-Debug)
```

---

## 6. Server-Integration (neu: v1.1)

```
Android/Desktop-Client
   │  ws:// oder wss://…/kombat
   ▼
Caddy (TLS, Auto-Cert) ──► Relay (Node, Docker)
                              ├── Räume + Spiegelung (unverändert)
                              ├── Matchmaking (Mode/Region, Timeout)
                              └── REST /api/status (Monitoring, ELO-UI)
```

**Neue Client-Nachrichten:**

| type | Felder | Wirkung |
|---|---|---|
| `matchmaking` | `mode`, `region`, `tag` | Warteschlange; 2 passende Spieler → Raum + `matched` |
| `matchmaking_cancel` | — | Aus der Warteschlange austreten |
| (Server→Client) | `matched` | `{room, opponent}` — danach normal `ready` → `start` |
| (Server→Client) | `matchmaking_update` | `waiting` / `paired` / `canceled` / `timeout` |

**Deployment (ein Befehl):**

```bash
PK_DOMAIN=kombat.example.de ./Tools/server_deploy.sh tls   # wss://kombat.example.de/kombat
curl http://localhost:5000/api/status                       # Monitoring
```

---

## 7. Definition of Done (Projekt „MKX-Polish“)

1. Alle 9 Charaktere mit 12-Clip-Animationsset in der Arena ✓
2. 5 Arenen mit Atmosphäre (Farb-LUT + Propset + Crowd) ✓
3. Desktop 60+ fps mit Ultra (SSAO/SSR/Vol-Fog) ✓
4. Android Flaggschiff 60 fps mit Hoch, Mittelklasse 60 fps mit Mittel ✓
5. Hit-Feel: Hit-Stop ≤ 4 Frames, Kamera-Punch, VFX-Sync ✓
6. Online: Matchmaking → Ready → Kampf; wss: auf Android-15-Gerät ✓
7. Server-Deploy per `docker compose` + Healthcheck ✓

---

## 8. Risiken & offene Punkte

| Risiko | Auswirkung | Gegenmaßnahme |
|---|---|---|
| Keine Lizenz für MKX-artige Assets | Verwechslung/Klage | Eigene prozedurale Optik, CC-BY-NC-Inhalte |
| GLB-Modelle ohne passende Bone-Namen | Rigger findet keine Hitboxen | Metarig-Doku in `docs/MODELLE.md` + `DynamicRigger`-Fallbacks |
| Hohe Partikelmenge auf Low-End | Hit-Stop-Frames → Ruckeln | Stufe „Niedrig“ reduziert Partikel via `ImpactFeedback`-Flag |
| Roaming/Matchmaking ohne Auth | Flut an Fake-Räumen | Rate-Limit + Token (Phase 4) |
| Viele Draw-Calls durch Stage-Details | FPS-Einbruch mobil | Alles instanziieren (`MultiMesh`), LODManager nutzen |

---

## 9. Empfohlene Reihenfolge ab jetzt

1. **Testen:** `godot --path .` → Arena starten → F1 durchschalten
   (Niedrig → Kino) und F2/F3 ausprobieren. Kriterium: Kino stellt
   SSAO/SSR/Vol-Fog, Mobil bleibt flüssig.
2. **Phase 1 starten:** 1 Charakter (z. B. Kalle) als GLB mit Metarig
   hochladen (`CharacterAssetSuite` → Asset → Spawn) — dann Pipeline vertiefen.
3. **Server live stellen:** `./Tools/server_deploy.sh tls` auf VPS;
   `network/relay/url` auf `wss://domain/kombat` setzen.
4. **Phase 3/4 priorisieren:** Game-Feel-Feintuning bringt den größten
   sichtbaren Sprung pro Arbeitsstunde.
