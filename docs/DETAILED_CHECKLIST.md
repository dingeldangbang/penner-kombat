# PENNER KOMBAT — DETAILLIERTE CHECKLISTE (Godot 4 – Stand 2026-08-24)

**Vollständige Projekt-Übersicht nach der Migration auf Godot 4 mit CharacterAssetSuite**

---

## 1. PROJEKTGRUNDLAGEN

| # | Komponente | Status | Datei/Pfad | Beschreibung |
|---|------------|--------|------------|--------------|
| 1.1 | `project.godot` | ✅ | `project.godot` | Godot 4.3+ Konfiguration (Forward+, Mobile) |
| 1.2 | Export-Presets | ✅ | `export_presets.cfg` | Android APK/AAB konfiguriert |
| 1.3 | AndroidManifest | ✅ | `android/AndroidManifest.xml` | Internet, Storage, Vibration |
| 1.4 | Icon | ⚠️ | `icon.svg` | Platzhalter vorhanden |
| 1.5 | Main Scene | ✅ | `scenes/ui/CharacterAssetSuite.tscn` | Neue zentrale Arbeitsoberfläche |

---

## 2. CORE-SYSTEME

| # | Komponente | Status | Datei | Funktion |
|---|------------|--------|-------|----------|
| 2.1 | GlbImporter | ✅ | `scripts/core/GlbImporter.gd` | GLB/GLTF Import + Kollision + Skalierung |
| 2.2 | AdvancedAssetImporter | ✅ | `scripts/core/AdvancedAssetImporter.gd` | **Atomic Chunking**, URL, ZIP-Archive (auch >1GB) |
| 2.3 | AiService | ✅ | `scripts/core/AiService.gd` | OpenAI GPT-4o-mini JSON-Output |
| 2.4 | DynamicRigger | ✅ | `scripts/core/DynamicRigger.gd` | Bone-Mapping + Hitboxen + VFX-Bindung |
| 2.5 | SkillData | ✅ | `scripts/combat/SkillData.gd` | Combo-Daten + JSON-Parser |

---

## 3. KAMPF-SYSTEME

| # | Komponente | Status | Datei | Funktion |
|---|------------|--------|-------|----------|
| 3.1 | CombatController | ✅ | `scripts/combat/CombatController.gd` | FSM + Bewegung + Combos + Fatal Blow |
| 3.2 | AIController | ✅ | `scripts/combat/AIController.gd` | 6 Schwierigkeitsstufen (VeryEasy → Boss) |
| 3.3 | ArenaController | ✅ | `scripts/combat/ArenaController.gd` | Runden, Timer, Best-of-3 |
| 3.4 | Arena.tscn | ✅ | `scenes/combat/Arena.tscn` | 3D-Arena mit Boden, Wänden, Licht |

---

## 4. EDITOR & UI (CharacterAssetSuite)

| # | Komponente | Status | Datei | Funktion |
|---|------------|--------|-------|----------|
| 4.1 | CharacterAssetSuite | ✅ | `scripts/ui/CharacterAssetSuite.gd` | **Haupt-Editor** mit 2 Tabs |
| 4.2 | ArenaConfigurator | ✅ | `scripts/ui/ArenaConfigurator.gd` | Bühnenwahl, Licht, Zerstörung, Props |
| 4.3 | Chat-Oberfläche | ✅ | Integriert in Suite | KI-Charakter-Generierung via Chat |
| 4.4 | 3D-Vorschau | ✅ | SubViewport | Live-Preview mit Kamera & Licht |
| 4.5 | Profil-System | ✅ | `.penner` JSON | Speichern/Laden/Löschen von Charakteren |
| 4.6 | HUD | ⚠️ | `scripts/ui/HUD.gd` | Code vorhanden, muss an Godot angepasst werden |
| 4.7 | Touch-Steuerung | ✅ | VirtualJoystick + TouchButton | Vollständig implementiert |

---

## 5. GAMEPLAY & MECHANIKEN (Aktualisiert)

| # | Mechanik | Status | Beschreibung |
|---|----------|--------|--------------|
| 5.1 | 3D-Bewegung | ✅ | CharacterBody3D, kamera-relativ, 360° |
| 5.2 | Finite State Machine | ✅ | IDLE, RUN, ATTACK, BLOCK, STUN, FATALITY |
| 5.3 | Combos | ✅ | Numpad-Notation, Hitboxen, VFX, Damage-Multiplier |
| 5.4 | Schadensystem | ✅ | HP, Knockback, Stun, Blocken (78–80% Reduktion) |
| 5.5 | Sprung | ✅ | Y-Achsen-Bewegung mit Schwerkraft |
| 5.6 | Rollen / Ausweichen | ✅ | i-Frames + Roll-Animation |
| 5.7 | KI-Gegner | ✅ | 6 Stufen + Combo-Patterns + Med-Nutzung |
| 5.8 | Best-of-3 / Runden | ✅ | Timer (99s), Siegerermittlung |
| 5.9 | Fatal Blow (X-Ray) | ✅ | Volle Leiste → Cinematic + hoher Schaden |
| 5.10 | Fatalities | ⚠️ | Code-Struktur vorhanden, Animationen fehlen |
| 5.11 | Med-System | ⚠️ | Code vorhanden, visuelle Assets fehlen |
| 5.12 | Beat-Sync | ✅ | `BeatSync.gd` – Combo beeinflusst Musik-Tempo |

---

## 6. STEUERUNG — ALLE PLATTFORMEN (KOMPLETT)

| # | Plattform | Status | Details |
|---|-----------|--------|---------|
| 6.1 | **Tastatur P1** | ✅ | `WASD` Bewegung • `J` leicht • `K` schwer • `Shift` Block • `Space` Sprung • `U/I` Spezial • `Y` Fatal Blow • `H` Med |
| 6.2 | **Tastatur P2** | ✅ | Pfeiltasten + Numpad (1–9 für Angriffe) |
| 6.3 | **Gamepad** | ✅ | Linker Stick = Bewegung • A = leicht • X = schwer • B = Block • RB = Spezial • LT = Fatal Blow |
| 6.4 | **Touch-Joystick** | ✅ | `VirtualJoystick.gd` – 8 Richtungen + analoge Stärke |
| 6.5 | **Touch-Buttons** | ✅ | 8 Aktions-Buttons (leicht, schwer, Block, Sprung, 2× Spezial, Fatal Blow, Med) |
| 6.6 | **Gesten-Erkennung** | ✅ | Wischen nach oben = Sprung, Kreis = Fatal Blow, etc. |
| 6.7 | **Tastatur-Layout-UI** | ⚠️ | Grundlegende Anzeige vorhanden |
| 6.8 | **Touch-Layout-Editor** | ⚠️ | Code vorhanden, Editor-UI fehlt noch |
| 6.9 | **Input Buffer** | ✅ | `CommandInput.gd` – Numpad-Notation mit Toleranzfenster |
| 6.10 | **Rebinding** | ❌ | Noch nicht implementiert |

---

## 7. CHARAKTERE & MODELLE

| # | Komponente | Status | Beschreibung |
|---|------------|--------|--------------|
| 7.1 | GLB-Import | ✅ | Runtime-Import von beliebigen `.glb`/`.gltf` |
| 7.2 | Kollisionsgenerierung | ✅ | ConvexPolygonShape3D pro Mesh |
| 7.3 | Bone-Mapping | ✅ | Head, Hand_R, Hand_L, Root, etc. |
| 7.4 | Hitboxen | ✅ | SphereShape3D dynamisch an Bones gebunden |
| 7.5 | VFX-Bindung | ✅ | GPUParticles3D an Bones |
| 7.6 | Profil-Speicherung | ✅ | `.penner` JSON mit SkillData + VFX-Config |
| 7.7 | 9 Charaktere | ⚠️ | Nur Capsule-Fallbacks vorhanden (Modelle fehlen) |

---

## 8. WAFFEN & EXTRAS

| # | Komponente | Status | Bemerkung |
|---|------------|--------|-----------|
| 8.1 | Waffen-System | ⚠️ | Code-Struktur vorhanden |
| 8.2 | Bierflasche, Schraubenschlüssel, etc. | ❌ | Modelle + Animationen fehlen |
| 8.3 | Mercedes 190e + 3 Atzen | ❌ | Komplett fehlend |
| 8.4 | Punker-Crowd | ❌ | Komplett fehlend |
| 8.5 | Power-Ups (8 Stück) | ❌ | Komplett fehlend |

---

## 9. VISUELLE EFFEKTE

| # | Effekt | Status | Datei |
|---|--------|--------|-------|
| 9.1 | ScreenShake | ✅ | `scripts/effects/ScreenShake.gd` |
| 9.2 | ComboCounter | ✅ | `scripts/effects/ComboCounter.gd` |
| 9.3 | BloodSplat, HitSpark, Shockwave | ⚠️ | Code vorhanden, Partikel müssen erstellt werden |
| 9.4 | GreaseEffect, PulseVisuals, GoldGlow | ⚠️ | Shader vorhanden, Materialien fehlen |
| 9.5 | Post-Processing | ✅ | Environment + PostProcessing.gd |

---

## 10. AUDIO

| # | Komponente | Status | Bemerkung |
|---|------------|--------|-----------|
| 10.1 | AudioManager | ⚠️ | Code vorhanden |
| 10.2 | 31 Musik-Tracks | ❌ | Müssen produziert werden |
| 10.3 | SFX + Voice Lines | ❌ | Fehlen komplett |
| 10.4 | Beat-Sync | ✅ | Funktioniert |

---

## 11. NETZWERK & MULTIPLAYER

| # | Komponente | Status | Bemerkung |
|---|------------|--------|-----------|
| 11.1 | WebSocketClient + NetworkManager | ⚠️ | Code vorhanden (optional) |
| 11.2 | Lobby & Matchmaking | ❌ | Muss implementiert werden |
| 11.3 | QR-Code-Kopplung | ⚠️ | Code vorhanden |

---

## 12. STORY & KRYPTA

| # | Komponente | Status | Bemerkung |
|---|------------|--------|-----------|
| 12.1 | StoryManager + DialogSystem | ⚠️ | Code vorhanden |
| 12.2 | 8 Kapitel + 4 Enden | ❌ | Content fehlt komplett |
| 12.3 | Krypta + Freischalt-System | ❌ | Muss implementiert werden |

---

## 13. TROPHÄEN

| # | Komponente | Status | Bemerkung |
|---|------------|--------|-----------|
| 13.1 | TrophyManager | ⚠️ | Code vorhanden |
| 13.2 | 56 Trophäen | ❌ | Definition fehlt |

---

## 14. STEUERUNG & OPTIONEN (ZUSAMMENFASSUNG)

- **Tastatur + Gamepad + Touch**: Vollständig implementiert
- **Gesten & Input Buffer**: Implementiert
- **Rebinding & Touch-Layout-Editor**: Fehlen noch
- **Optionen-Menü**: Grundgerüst vorhanden, viele Einstellungen noch nicht verdrahtet

---

## 📊 AKTUALISIERTE STATUS-ZUSAMMENFASSUNG (Godot 4)

| Kategorie              | Fertig | Fehlt | Bemerkung |
|------------------------|--------|-------|---------|
| Core-Systeme           | 100%   | 0%    | Sehr gut |
| Kampf-Systeme          | 95%    | 5%    | Balance fehlt |
| Editor & UI            | 95%    | 5%    | Touch-Layout-Editor |
| **Steuerung**          | **95%**| **5%**| Rebinding fehlt |
| Gameplay               | 85%    | 15%   | Med-Assets, Fatalities |
| Charaktere             | 70%    | 30%   | Modelle fehlen |
| Waffen & Extras        | 15%    | 85%   | Fast alles fehlt |
| Visuelle Effekte       | 45%    | 55%   | Partikel & Shader-Materialien |
| Audio                  | 10%    | 90%   | Fast komplett fehlend |
| Netzwerk               | 30%    | 70%   | Lobby/Matchmaking fehlt |
| Story & Krypta         | 10%    | 90%   | Content fehlt |
| Trophäen               | 10%    | 90%   | Definition fehlt |
| Build & Deployment     | 85%    | 15%   | iOS fehlt |
| Performance            | 50%    | 50%   | Occlusion Culling fehlt |

---

**Fazit**:  
Das Projekt spiegelt die ursprüngliche Idee **sehr gut** wider — besonders durch die neue **CharacterAssetSuite** mit KI-Generierung und Atomic Chunking. Die Kern-Gameplay-Mechaniken (Combos, Fatal Blow, KI, Steuerung) sind bereits sehr weit fortgeschritten. Die größten Lücken liegen weiterhin bei **Assets** (Modelle, Audio, VFX) und **Content** (Story, Trophäen, Waffen).