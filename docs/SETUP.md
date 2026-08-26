# Setup-Anleitung (Godot 4)

Diese Anleitung führt durch den Aufbau einer **minimalen, spielbaren Godot-Editor-Szene**
mit dem Code aus diesem Repo.

---

## 1. Projekt anlegen

1. **Godot 4.3+** starten → **Import** → Ordner `penner-kombat` auswählen.
2. `project.godot` doppelklicken oder „Import“ bestätigen.
3. Main Scene ist bereits auf `scenes/ui/Editor.tscn` gesetzt.

## 2. Code ist bereits vorhanden

Alle GDScript-Dateien liegen unter `scripts/`, Szenen unter `scenes/`.
Nichts muss kopiert werden.

## 3. Godot ist sofort startklar

- Keine zusätzlichen Pakete nötig.
- Autoloads (`GlobalData`) und Rendering-Einstellungen sind bereits in `project.godot` hinterlegt.

## 4. Layers & Collision (optional)

Godot verwendet `project.godot` + Physics Layers. Die wichtigsten sind bereits vorkonfiguriert:

- Layer 1 = World
- Layer 2 = Player / Fighter
- Layer 3 = Projectile

Weitere Anpassungen in **Project Settings → General → Layer Names**.

## 5. Sofort starten

1. `scenes/ui/Editor.tscn` öffnen (ist bereits Main Scene).
2. ▶ Play drücken.
3. Im Editor: GLB laden → KI-Skills generieren → „Play in Arena“.

Die Arena-Szene (`scenes/combat/Arena.tscn`) wird automatisch vom Editor aus gestartet.

## 6. Kämpfer & Modelle

- GLB-Dateien direkt im Editor hochladen.
- `DynamicRigger` erzeugt automatisch Hitboxen und Rig.
- Profile werden als `.penner` in `user://profiles/` gespeichert.

## 7. Test

- Editor starten → Modell laden → „Play“ → Kampf beginnt automatisch.
- KI (`AIController.gd`) ist bereits aktiv.

---

## Balance-/Feinabstimmung

Werte liegen in `SkillData.gd`, `CombatController.gd` und den Charakter-Skripten unter `scripts/characters/`.