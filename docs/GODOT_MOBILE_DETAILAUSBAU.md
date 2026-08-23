# Penner Kombat — Godot 4 Mobile Detailausbau

Diese Implementierung ergänzt das Projekt um eine lauffähige Godot-4-Mobile-Schicht für dynamische GLB-Entitäten, OpenAI-generierte Combat-Profile, Runtime-Rigging, Touch-Steuerung und Android-Export.

## Datenfluss

1. `GlbImporter.load_glb(path)` lädt `.glb`/`.gltf`, normalisiert die Modellhöhe auf ca. 2.0 Einheiten und erzeugt `StaticBody3D`/`ConvexPolygonShape3D`-Kollisionen.
2. `AiService.request_entity_profile(node_names, prompt, api_key)` fragt OpenAI mit `response_format=json_object` an.
3. `SkillData.load_from_json(profile)` validiert `move_speed`, `attack_power` und Combo-Daten.
4. `DynamicRigger.rig_entity(entity, skill_data)` mappt Bones/Meshes auf `Head`, `Hand_R`, `Hand_L`, `Root` und erzeugt Hitboxen sowie `GPUParticles3D`-VFX.
5. `CombatController` steuert FSM, Bewegung, Combos, Treffer und HP.
6. `MainUI` verbindet File-Auswahl, OpenAI-Test, Spawn-Pipeline, Touch-Joystick und Attack-Button.

## Neue/erweiterte Module

- `scripts/core/GlbImporter.gd`
- `scripts/core/AiService.gd`
- `scripts/core/DynamicRigger.gd`
- `scripts/core/ErrorHandler.gd`
- `scripts/core/LodManager.gd`
- `scripts/combat/SkillData.gd`
- `scripts/combat/CombatController.gd`
- `scripts/ui/VirtualJoystick.gd`
- `scripts/network/NetworkManager.gd`
- `scripts/world/ArenaLighting.gd`
- `scripts/ui/MainUI.gd`
- `scenes/ui/MainUI.tscn`

## Android-Dateien

- `project.godot` — mobile Forward+/Mobile-Renderer-Konfiguration
- `export_presets.cfg` — Android-Debug-Preset mit ARMv7/ARM64, Internet, Storage, Vibration und Wake-Lock
- `android/AndroidManifest.xml` — Permissions, Vulkan/OpenGLES-Feature Flags, Godot Activity, FileProvider
- `android/res/xml/file_paths.xml` — externe Datei-Provider-Pfade
- `.github/workflows/android_build.yml` — Godot-CI Android Debug APK Export

## Bedienung

1. Projekt in Godot 4.2+ öffnen.
2. OpenAI API Key eintragen.
3. Skill-Beschreibung eingeben.
4. `.glb` oder `.gltf` auswählen.
5. `Spawn & Bind` drücken.
6. Mit virtuellem Joystick bewegen, mit `ATTACK` Combo-Kette ausführen.
