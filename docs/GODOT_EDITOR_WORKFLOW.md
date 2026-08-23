# Penner Kombat — GLB/Avatar Editor Workflow

Der Godot-Workflow ist jetzt als eigener Editor umgesetzt und als Main Scene eingetragen:

```ini
run/main_scene="res://scenes/ui/Editor.tscn"
```

## Workflow

1. **Upload / Import**
   - Lokale `.glb` / `.gltf` Datei über Dateidialog
   - Direkter Pfad im Eingabefeld
   - HTTP/HTTPS URL, Download nach `user://downloads/`

2. **Beschreiben / Generieren**
   - OpenAI API Key direkt im Editor oder aus `user://config.cfg`
   - Skill-Prompt im Textfeld
   - OpenAI-Profil über vorhandenen `AiService`
   - Fallback/manual edit via SpinBox + Combos-JSON

3. **Vorschau**
   - SubViewport mit Kamera, Licht, Ground und Live-Modell
   - Effektvorschau nach KI-Generierung
   - Combo-Liste zeigt Schaden, Delay und Farbe

4. **Spawn & Binden**
   - Entity wird dupliziert
   - `DynamicRigger` erzeugt Hitboxen und VFX
   - `CombatController` wird verbunden

5. **Speichern**
   - Profile werden als `.penner` JSON in `user://profiles/` gespeichert
   - Bibliothek kann Profile laden und löschen

6. **Spielen**
   - `GlobalData` übergibt `SkillData`, Entity-Duplikat und Quellpfad
   - Wechsel zu `res://scenes/combat/Arena.tscn`
   - Arena spawnt P1 plus KI-Dummy oder optionalen Gegner

## Neue Dateien

- `scenes/ui/Editor.tscn`
- `scripts/ui/Editor.gd`
- `scripts/autoload/GlobalData.gd`
- `scenes/combat/Arena.tscn`
- `scripts/combat/ArenaController.gd`
- `scripts/combat/AIController.gd`

## Profilformat

```json
{
  "name": "Feuer-Bär",
  "file_path": "user://downloads/bear.glb",
  "skill_data": {
    "move_speed": 4.2,
    "attack_power": 18.5,
    "combos": []
  },
  "vfx_config": {},
  "timestamp": "2026-08-23T16:00:00"
}
```
