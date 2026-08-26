extends Control
class_name CharacterAssetSuite

## Vollständige Character & Asset Creation Suite
## Mit Chat-Oberfläche + massivem Archive-Support + Atomic Chunking
## + Integriertem Arena + Konfigurator Tab

@onready var tab_container: TabContainer = $TabContainer

# Tab 1: Asset + Chat
@onready var file_input: LineEdit = $TabContainer/Asset/FileContainer/FileInput
@onready var file_btn: Button = $TabContainer/Asset/FileContainer/FileButton
@onready var url_btn: Button = $TabContainer/Asset/FileContainer/UrlButton
@onready var archive_btn: Button = $TabContainer/Asset/FileContainer/ArchiveButton
@onready var asset_status: Label = $TabContainer/Asset/AssetStatus
@onready var api_key_input: LineEdit = $TabContainer/Asset/ApiKeyInput
@onready var progress_bar: ProgressBar = $TabContainer/Asset/ProgressBar
@onready var status_label: Label = $TabContainer/Asset/StatusLabel

@onready var preview_viewport: SubViewport = $TabContainer/Asset/PreviewContainer/Preview

@onready var chat_log: RichTextLabel = $TabContainer/Asset/ChatPanel/ChatLog
@onready var chat_input: LineEdit = $TabContainer/Asset/ChatPanel/ChatInput
@onready var send_btn: Button = $TabContainer/Asset/ChatPanel/SendBtn
@onready var generate_btn: Button = $TabContainer/Asset/ChatPanel/GenerateBtn
@onready var spawn_btn: Button = $TabContainer/Asset/ChatPanel/SpawnBtn
@onready var save_btn: Button = $TabContainer/Asset/ChatPanel/SaveBtn
@onready var play_btn: Button = $TabContainer/Asset/ChatPanel/PlayBtn

# Tab 2: Arena + Konfigurator
@onready var arena_config: ArenaConfigurator = $TabContainer/Arena/ArenaConfigurator

var _importer: AdvancedAssetImporter
var _ai_service: AiService
var _dynamic_rigger: DynamicRigger
var _skill_data: SkillData
var _loaded_entity: Node3D
var _preview_arena: Node3D
var _camera: Camera3D
var _current_profile_name: String = ""

const PROFILE_PATH := "user://profiles/"

func _ready() -> void:
	_setup_services()
	_setup_preview_scene()
	_connect_signals()
	_load_config()
	chat_log.append_text("[color=cyan]=== Penner Kombat Asset Suite ===[/color]\n")
	chat_log.append_text("Lade GLB, ZIP-Archive oder gib eine URL ein.\n")
	
	# Arena Config verbinden
	arena_config.arena_changed.connect(_on_arena_config_changed)

func _setup_services() -> void:
	_importer = AdvancedAssetImporter.new()
	add_child(_importer)
	_ai_service = AiService.new()
	add_child(_ai_service)
	_dynamic_rigger = DynamicRigger.new()
	add_child(_dynamic_rigger)
	_skill_data = _create_default_skill_data()

	_importer.import_started.connect(_on_import_started)
	_importer.import_progress.connect(_on_import_progress)
	_importer.import_completed.connect(_on_import_completed)
	_importer.import_failed.connect(_on_import_failed)
	_importer.archive_extracted.connect(_on_archive_extracted)

	_ai_service.profile_received.connect(_on_ai_profile_received)
	_ai_service.request_failed.connect(_on_ai_failed)

func _setup_preview_scene() -> void:
	_preview_arena = Node3D.new()
	_preview_arena.name = "PreviewArena"
	preview_viewport.add_child(_preview_arena)

	_camera = Camera3D.new()
	_camera.position = Vector3(0, 4, 8)
	_camera.look_at(Vector3.ZERO)
	preview_viewport.add_child(_camera)

	var light := DirectionalLight3D.new()
	light.rotation_degrees = Vector3(-45, 30, 0)
	light.shadow_enabled = true
	_preview_arena.add_child(light)

func _connect_signals() -> void:
	file_btn.pressed.connect(_on_file_browse)
	url_btn.pressed.connect(_on_url_import)
	archive_btn.pressed.connect(_on_archive_import)
	
	send_btn.pressed.connect(_send_chat)
	chat_input.text_submitted.connect(_send_chat)
	generate_btn.pressed.connect(_on_generate_ai)
	spawn_btn.pressed.connect(_on_spawn)
	save_btn.pressed.connect(_on_save)
	play_btn.pressed.connect(_on_play)

func _load_config() -> void:
	var cfg := ConfigFile.new()
	if cfg.load("user://config.cfg") == OK:
		api_key_input.text = str(cfg.get_value("settings", "api_key", ""))

func _save_config() -> void:
	var cfg := ConfigFile.new()
	cfg.set_value("settings", "api_key", api_key_input.text)
	cfg.save("user://config.cfg")

# ====================== IMPORT ======================

func _on_file_browse() -> void:
	var dialog := FileDialog.new()
	dialog.file_mode = FileDialog.FILE_MODE_OPEN_FILE
	dialog.add_filter("*.glb;*.gltf")
	dialog.title = "GLB/GLTF auswählen"
	add_child(dialog)
	dialog.file_selected.connect(func(p): dialog.queue_free(); _importer.import_from_local(p))
	dialog.popup_centered_ratio(0.7)

func _on_url_import() -> void:
	var url := file_input.text.strip_edges()
	if url.begins_with("http"):
		_importer.import_from_url(url)
	else:
		chat_log.append_text("[color=red]Ungültige URL[/color]\n")

func _on_archive_import() -> void:
	var dialog := FileDialog.new()
	dialog.file_mode = FileDialog.FILE_MODE_OPEN_FILE
	dialog.add_filter("*.zip")
	dialog.title = "ZIP-Archiv mit GLBs auswählen"
	add_child(dialog)
	dialog.file_selected.connect(func(p): dialog.queue_free(); _importer.import_archive(p))
	dialog.popup_centered_ratio(0.7)

func _on_import_started(path: String) -> void:
	chat_log.append_text("[color=yellow]Import gestartet:[/color] %s\n" % path.get_file())

func _on_import_progress(current: int, total: int, msg: String) -> void:
	progress_bar.value = float(current) / total * 100
	status_label.text = msg

func _on_import_completed(entity: Node3D, path: String) -> void:
	if _loaded_entity:
		_loaded_entity.queue_free()
	_loaded_entity = entity
	_preview_arena.add_child(_loaded_entity)
	asset_status.text = "✅ " + path.get_file()
	chat_log.append_text("[color=green]Asset erfolgreich geladen![/color]\n")
	progress_bar.value = 100

func _on_import_failed(error: String) -> void:
	chat_log.append_text("[color=red]Fehler: %s[/color]\n" % error)
	status_label.text = "Fehler"

func _on_archive_extracted(files: Array[String]) -> void:
	chat_log.append_text("[color=cyan]%d GLB-Dateien aus Archiv extrahiert[/color]\n" % files.size())

# ====================== CHAT + KI ======================

func _send_chat(_text: String = "") -> void:
	var msg := chat_input.text.strip_edges()
	if msg.is_empty():
		return
	chat_log.append_text("[color=#ffcc00]Du:[/color] %s\n" % msg)
	chat_input.text = ""
	
	var prompt := "Erstelle einen Penner-Kombat Charakter basierend auf: " + msg
	var api_key := _get_api_key()
	if api_key.is_empty():
		chat_log.append_text("[color=red]Kein API-Key![/color]\n")
		return
	_ai_service.request_entity_profile(["Body", "Head", "Hand_R"], prompt, api_key)

func _on_generate_ai() -> void:
	_send_chat("Generiere ein cooles Profil für den aktuellen Charakter")

func _on_ai_profile_received(profile: Dictionary) -> void:
	chat_log.append_text("[color=#00ff88]KI:[/color] Neues Skill-Profil erhalten!\n")
	_skill_data = SkillData.new()
	_skill_data.load_from_json(profile)
	chat_log.append_text("  → %d Combos, Speed: %.1f, Power: %.1f\n" % [
		_skill_data.combos.size(), _skill_data.move_speed, _skill_data.attack_power
	])

func _on_ai_failed(error: String) -> void:
	chat_log.append_text("[color=red]KI-Fehler: %s[/color]\n" % error)

# ====================== SPAWN / SAVE / PLAY ======================

func _on_spawn() -> void:
	if not _loaded_entity or not _skill_data.is_valid():
		chat_log.append_text("[color=red]Asset und Profil benötigt![/color]\n")
		return
	chat_log.append_text("[color=cyan]Rigge Entity...[/color]\n")
	_dynamic_rigger.rig_entity(_loaded_entity, _skill_data)

func _on_save() -> void:
	if not _loaded_entity or not _skill_data.is_valid():
		return
	var name := _current_profile_name if not _current_profile_name.is_empty() else "Charakter_" + str(Time.get_unix_time_from_system())
	_save_profile(name)

func _save_profile(name: String) -> void:
	var dir := DirAccess.open("user://")
	dir.make_dir_recursive("profiles")
	
	var profile := {
		"name": name,
		"file_path": _importer._current_file_path,
		"skill_data": _skill_data_to_dict(),
		"timestamp": Time.get_datetime_string_from_system()
	}
	var path := PROFILE_PATH + name.replace(" ", "_") + ".penner"
	var f := FileAccess.open(path, FileAccess.WRITE)
	f.store_string(JSON.stringify(profile, "\t"))
	f.close()
	chat_log.append_text("[color=green]Profil gespeichert: %s[/color]\n" % name)

func _on_play() -> void:
	if not _loaded_entity or not _skill_data.is_valid():
		return
	GlobalData.set_player_data(_skill_data, _loaded_entity, _importer._current_file_path, _current_profile_name)
	get_tree().change_scene_to_file("res://scenes/combat/Arena.tscn")

# ====================== ARENA KONFIGURATOR ======================

func _on_arena_config_changed(arena_name: String, config: Dictionary) -> void:
	chat_log.append_text("[color=magenta]Arena gewechselt:[/color] %s\n" % arena_name)
	# Hier könnte man später die Preview-Arena austauschen oder GlobalData updaten
	GlobalData.set_arena_config(config)

# ====================== HELPER ======================

func _get_api_key() -> String:
	if not api_key_input.text.strip_edges().is_empty():
		return api_key_input.text.strip_edges()
	var cfg := ConfigFile.new()
	if cfg.load("user://config.cfg") == OK:
		return str(cfg.get_value("settings", "api_key", ""))
	return ""

func _skill_data_to_dict() -> Dictionary:
	return {
		"move_speed": _skill_data.move_speed,
		"attack_power": _skill_data.attack_power,
		"combos": _skill_data.combos.duplicate(true)
	}

func _create_default_skill_data() -> SkillData:
	var s := SkillData.new()
	s.move_speed = 4.5
	s.attack_power = 19.0
	s.combos = [
		{"name": "Street Punch", "delay": 0.15, "damage_multiplier": 1.3, "hitbox_radius": 1.6, "vfx_color": "#FFAA00"},
		{"name": "Bottle Smash", "delay": 0.35, "damage_multiplier": 2.1, "hitbox_radius": 2.2, "vfx_color": "#00FF88"}
	]
	return s