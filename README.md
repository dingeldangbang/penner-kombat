# 🩸 LETAL PENNER KOMBAT

> **„Kombat der städtischen Randfiguren\"** — ein humorvoller, brutaler 2.5D-Fighter in **Godot 4** (Forward+ / Mobile).

[![Godot](https://img.shields.io/badge/Godot-4.3%20%2B-blue)](https://godotengine.org)
[![License](https://img.shields.io/badge/License-CC_BY--NC--4.0-lightgrey)](LICENSE)
[![Language](https://img.shields.io/badge/GDScript-Godot%204-green)]()

---

## ✅ Aktueller Stand (2026-08-24)

Das Repository enthält jetzt die **vollständige Godot 4 Mobile/Editor-Version**:

- Dynamischer GLB/GLTF-Import + KI-gestützte Skill-Generierung
- Vollständiges Combat-System mit `CombatController`, `DynamicRigger`, `ArenaController`
- Editor-Szene als Main Scene (`scenes/ui/Editor.tscn`)
- Alle 9 Charaktere + KI + Arena + VFX + Audio
- Mobile-Optimierungen (LOD, PerformanceManager, Touch-Emulation)

---

## 🚀 Schnellstart

```bash
godot project.godot
# oder doppelklick auf project.godot
```

**Main Scene**: `res://scenes/ui/Editor.tscn`  
**Spiel starten**: Editor → „Play“ → Modell laden → Arena

---

## 📁 Projektstruktur

```
scenes/
├── combat/          # Arena.tscn, ArenaController.gd
└── ui/              # Editor.tscn + Editor.gd

scripts/
├── autoload/        # GlobalData.gd
├── characters/      # Visual Effects (GoldGlow, GreaseEffect, PulseVisuals)
├── combat/          # CombatController, AIController, SkillData, ArenaController
├── core/            # GlbImporter, DynamicRigger, AiService, PerformanceManager
├── effects/         # BloodSplat, ComboCounter, CritGold
└── ...              # audio, network, story, trophies, weapons, world

web/                 # Browser-Version (3d.html)
server/              # Node.js Relay (optional)
```

---

## 🧠 Neue Features (Godot)

- **GLB-Editor**: Upload, OpenAI-Prompt → SkillData, Vorschau, Speichern als `.penner`
- **DynamicRigger**: Automatische Hitbox-Erzeugung aus GLB-Skelett
- **CombatController**: Combo-System, Fatal Blow, X-Ray, Beat-Sync
- **PerformanceManager + LOD**: Mobile-Optimierung
- **GlobalData**: Übergabe von Profilen in die Arena

---

## 📚 Dokumentation

- `docs/GODOT_EDITOR_WORKFLOW.md` – Editor-Workflow & Profilformat
- `docs/GODOT_MOBILE_DETAILAUSBAU.md` – Mobile-Details
- `docs/GODOT_EFFECTS_DETAILS.md` – VFX & Shader
- `docs/STATUS.md` – Vollständigkeits-Check

---

## 📄 Lizenz

CC BY-NC 4.0 — nicht-kommerziell.

---

## 🙌 Mitwirkung

Issues und PRs gerne – besonders für neue Charaktere, Balance und Godot-Features!