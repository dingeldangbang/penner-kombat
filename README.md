# 🩸 LETAL PENNER KOMBAT

> **„Kombat der städtischen Randfiguren“** — ein humorvoller, brutaler 2.5D-Fighter in **Godot 4** (Forward+ / Mobile).

[![Godot](https://img.shields.io/badge/Godot-4.7%20%2B-blue)](https://godotengine.org)
[![License](https://img.shields.io/badge/License-CC_BY--NC--4.0-lightgrey)](LICENSE)
[![Language](https://img.shields.io/badge/GDScript-Godot%204-green)]()

---

## ✅ Aktueller Stand (2026-09-07)

Das Repository enthält die **vollständige Godot 4 Mobile/Editor-Version** mit
einer modernen, professionellen Asset-Pipeline — plus **MKX-Grafik-Upgrade
(Phase 0)** und **Mobile-Server v1.1** (Matchmaking + TLS-Deployment).
Fahrplan & Detailüberlegungen: **[docs/MASTERPLAN_3D_MKX.md](docs/MASTERPLAN_3D_MKX.md)**.

### Kern-Highlights
- **Advanced Asset Pipeline** mit Atomic Chunking (auch für >1 GB große ZIP-Archive)
- **Character & Asset Suite** (`CharacterAssetSuite.tscn`) – zentrale Arbeitsoberfläche mit Chat und Arena-Konfigurator
- KI-gestützte Charakter-Erstellung via OpenAI
- Vollständiges Kampf-System (`CombatController`, `DynamicRigger`, `ArenaController`)
- Mobile-Optimierungen (LOD, PerformanceManager, Touch)
- **Android 11–15 (API 30–35)**: Export-Presets, SDK-Setup, CI-APK + optionales AAB, 16-KB-Page-Size, `ws://`/`wss://`-Relay-Anbindung — Details in [docs/ANDROID_11_15.md](docs/ANDROID_11_15.md)
- 9 spielbare Charaktere + KI + VFX + Audio

### 🎬 MKX-Grafik-Upgrade (Phase 0) — kurz
- **`GraphicsQuality`** (Autoload): 5 Stufen (Niedrig→Kino), FSR/TSR, **adaptive Auflösung**;
  Hotkeys im Kampf: `F1` Stufe · `F2` Adaptive · `F3` Kino-FX
- **Neue Shader:** Charakter Toon-PBR (Rim/Outline), nasser Arena-Boden, Nachthimmel
- **Arena-Dressing:** `CinematicArenaBuilder` — Neon, Crowd, Nebel, Spotlights, Props
- **Game-Feel:** `KombatCamera` (Punch-/Finisher-Zoom) + `ImpactFeedback` (Hit-Stop, Sparks, Shockwave)
- **HUD:** „RUNDE 1 / KAMPF!“-Ansagen, Info-Toasts
- **Server v1.1:** Matchmaking + REST-Status-API + Docker/Caddy-TLS → `./Tools/server_deploy.sh tls`

---

## 📱 Android 11–15 — Schnellstart

```bash
./Tools/android_setup.sh                     # JDK-17-Check + Android SDK 30–35 installieren
./Tools/android_check.sh                     # Bereitschaft prüfen
./Tools/install_android_build_template.sh    # nur für AAB-/Gradle-Build (--relay für ws://)
godot --headless --path . --export-debug "Android" build/PennerKombat-debug.apk
```

### 🐙 Alternativ: GitHub CLI / CD / CI (ohne lokales SDK)

```bash
./Tools/gh_android.sh status        # Workflow + letzte Runs
./Tools/gh_android.sh activate      # einmalig: Workflow via gh api anlegen
./Tools/gh_android.sh build apk     # baut in der Cloud & lädt nach build/
./Tools/gh_android.sh emulator 35   # optional: Android-15-Emulator-Test
./Tools/gh_android.sh release v1.1.0
```

Details: [docs/GH_CLI_CI.md](docs/GH_CLI_CI.md) · lokal:
[docs/ANDROID_11_15.md](docs/ANDROID_11_15.md).

## 🚀 Schnellstart (5 Minuten)

```bash
godot project.godot
```

**Empfohlene Main Scene**: `res://scenes/ui/CharacterAssetSuite.tscn`

### Erster Kampf in 6 Schritten

1. **Editor starten** → `CharacterAssetSuite.tscn` öffnen
2. **Asset laden**:
   - Lokale `.glb`/`.gltf` Datei
   - Oder ZIP-Archiv (auch sehr große)
   - Oder direkte URL
3. **Im Chat beschreiben** (z.B. „Ein fetter Penner mit Bierflaschen-Wurf und Schmier-Schlüppa“)
4. **KI generiert** Skill-Profil (Combos, Damage, VFX-Farben)
5. **Arena konfigurieren** (Licht, Zerstörung, Props) im zweiten Tab
6. **Spawn → Play** → Kampf in der Arena

---

## 🧩 Detaillierte Projektstruktur

```
scenes/
├── combat/
│   ├── Arena.tscn
│   └── ArenaController.gd
└── ui/
    ├── CharacterAssetSuite.tscn          ← Haupt-Editor (empfohlen)
    └── Editor.tscn                       ← Alte Version

scripts/
├── autoload/
│   └── GlobalData.gd                     ← Zentrale Datenübergabe
├── combat/
│   ├── CombatController.gd
│   ├── AIController.gd
│   ├── SkillData.gd
│   └── ArenaController.gd
├── core/
│   ├── AdvancedAssetImporter.gd          ← Atomic Chunking + ZIP + URL
│   ├── GlbImporter.gd
│   ├── DynamicRigger.gd                  ← Automatisches Hitbox-Rigging
│   ├── AiService.gd                      ← OpenAI Integration
│   └── PerformanceManager.gd
├── ui/
│   ├── CharacterAssetSuite.gd            ← Hauptlogik (2 Tabs)
│   ├── ArenaConfigurator.gd              ← Arena-Einstellungen
│   └── ChatPanel.gd
└── effects/                              ← VFX (Blood, Combo, Fire, etc.)
```

---

## 🛠️ Wichtige Systeme – Detaillierte Beschreibung

### 1. AdvancedAssetImporter (Atomic Chunking)

**Datei**: `scripts/core/AdvancedAssetImporter.gd`

Funktionen:
- **Lokaler Upload**
- **HTTP/HTTPS Download**
- **ZIP-Archive** (auch > 2 GB)
- **Atomic Chunking** mit 4 MB Blöcken:
  - Unterbrechungssicher
  - Echtzeit-Fortschrittsanzeige
  - Kein UI-Freeze bei großen Dateien
  - Automatische Extraktion von GLB/GLTF aus Archiven

**Vorteil**: Auch auf schwachen Geräten oder bei langsamer Verbindung stabil.

### 2. CharacterAssetSuite (zwei Tabs)

**Datei**: `scripts/ui/CharacterAssetSuite.gd` + `scenes/ui/CharacterAssetSuite.tscn`

#### Tab 1 – „Asset“

- Datei-Input (Lokal / URL / ZIP)
- Live 3D-Vorschau (SubViewport)
- Integrierter **Chat** mit KI
- Buttons: KI-Generieren, Spawn, Speichern, In Arena spielen
- Fortschrittsbalken + Status

#### Tab 2 – „Arena + Konfigurator“

**Datei**: `scripts/ui/ArenaConfigurator.gd`

Einstellungen:
- **Arena-Auswahl** (5 Bühnen):
  - Kiez-Hinterhof
  - U-Bahn Station
  - Schrottplatz
  - Keller-Rave
  - Dachgarten
- **Licht**: Energie + Farbe
- **Zerstörung**: Ein/Aus
- **Prop-Dichte**: 0–100 %
- Button „Konfiguration übernehmen“ → sendet Signal an `GlobalData`

### 3. Chat-Oberfläche & KI-Integration

- Direkte Kommunikation mit OpenAI
- Beschreibe den Charakter im Chat → KI erstellt `SkillData`
- Automatische Erzeugung von:
  - Combos mit Delay, Damage, Hitbox-Radius
  - VFX-Farben
  - Move Speed & Attack Power

Beispiel-Prompt:
> „Ein dicker Penner mit Schmier-Schlüppa, der Mops-Kommandos ruft und Bierflaschen wirft.“

### 4. DynamicRigger

Automatische Erzeugung von:
- Hitboxen aus GLB-Skelett
- Collision Shapes
- VFX-Attachment-Punkte

### 5. GlobalData (Autoload)

Zentrale Übergabe von:
- `SkillData`
- Geladenes 3D-Modell
- Dateipfad
- Arena-Konfiguration

---

## 📚 Dokumentation

| Datei | Inhalt |
|-------|--------|
| `docs/GODOT_EDITOR_WORKFLOW.md` | Detaillierter Editor-Workflow & `.penner`-Profilformat |
| `docs/GODOT_MOBILE_DETAILAUSBAU.md` | Mobile-Optimierungen & Performance |
| `docs/GODOT_EFFECTS_DETAILS.md` | Alle VFX & Shader-Erklärungen |
| `docs/STATUS.md` | Was ist fertig, was fehlt noch |

---

## 📄 Lizenz

CC BY-NC 4.0 — nicht-kommerziell.

---

## 🙌 Mitwirkung

Gerne Issues und Pull Requests zu:
- Neuen Charakteren & Moves
- Weiteren Arenen
- Multiplayer (WebSocket)
- Balance & KI-Verbesserungen
- Weitere Godot-Features

---

*Letzte Aktualisierung: 2026-08-24*