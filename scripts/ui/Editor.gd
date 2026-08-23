extends Control
class_name Editor

## Penner Kombat editor: asset upload, URL import, AI skill generation, manual tuning,
## live 3D preview, .penner profile library, spawn/bind and play handoff.

@onready var file_input: LineEdit = $MainPanel/LeftPanel/VBox/AssetContainer/FileInput
@onready var file_button: Button = $MainPanel/LeftPanel/VBox/AssetContainer/FileButton
@onready var url_button: Button = $MainPanel/LeftPanel/VBox/AssetContainer/UrlButton
@onready var asset_status: Label = $MainPanel/LeftPanel/VBox/AssetStatus
@onready var api_key_input: LineEdit = $MainPanel/LeftPanel/VBox/ApiKeyInput
@onready var skill_input: TextEdit = $MainPanel/LeftPanel/VBox/SkillInput
@onready var skill_preview: Label = $MainPanel/LeftPanel/VBox/SkillPreview
@onready var generate_btn: Button = $MainPanel/LeftPanel/VBox/ActionButtons/GenerateBtn
@onready var manual_btn: Button = $MainPanel/LeftPanel/VBox/ActionButtons/ManualBtn
@onready var combo_list: ItemList = $MainPanel/LeftPanel/VBox/ComboList
@onready var status_label: Label = $MainPanel/LeftPanel/VBox/StatusBar/StatusLabel
@onready var progress_bar: ProgressBar = $MainPanel/LeftPanel/VBox/StatusBar/ProgressBar
@onready var spawn_btn: Button = $MainPanel/LeftPanel/VBox/SpawnButtons/SpawnBtn
@onready var save_btn: Button = $MainPanel/LeftPanel/VBox/SpawnButtons/SaveBtn
@onready var play_btn: Button = $MainPanel/LeftPanel/VBox/SpawnButtons/PlayBtn
@onready var preview_viewport: SubViewport = $MainPanel/RightPanel/PreviewContainer/Preview
@onready var library_list: ItemList = $MainPanel/RightPanel/LibraryPanel/VBox/LibraryList
@onready var library_load_btn: Button = $MainPanel/RightPanel/LibraryPanel/VBox/LibraryButtons/LibraryLoadBtn
@onready var library_delete_btn: Button = $MainPanel/RightPanel/LibraryPanel/VBox/LibraryButtons/LibraryDeleteBtn

var _glb_importer: GlbImporter
var _ai_service: AiService
var _dynamic_rigger: DynamicRigger
var _skill_data: SkillData
var _combat_controller: CombatController
var _loaded_entity: Node3D
var _preview_entity: Node3D
var _active_spawn_entity: Node3D
var _arena_node: Node3D
var _camera_3d: Camera3D
var _camera_light: DirectionalLight3D
var _download_request: HTTPRequest
var _screen_shake: ScreenShake
var _post_processing: PostProcessing
var _audio_manager: AudioManager

var _current_file_path: String = ""
var _is_loaded: bool = false
var _is_spawned: bool = false
var _current_profile_name: String = ""
var _library_profiles: Array[Dictionary] = []

const PROFILE_PATH: String = "user://profiles/"
const PROFILE_EXTENSION: String = ".penner"
const DOWNLOAD_PATH: String = "user://downloads/"
const DEFAULT_PROMPT: String = "Ein schwerer Bär mit Feuer-Attacken und einem verheerenden Aufwärtshaken. Rote Funken-Effekte."


func _ready() -> void:
	_setup_systems()
	_connect_signals()
	_setup_preview_scene()
	_load_library()
	_load_config()
	_set_default_prompt()
	_update_skill_preview()


func _setup_systems() -> void:
	_glb_importer = GlbImporter.new()
	add_child(_glb_importer)
	_ai_service = AiService.new()
	add_child(_ai_service)
	_dynamic_rigger = DynamicRigger.new()
	add_child(_dynamic_rigger)
	_skill_data = _create_default_skill_data()
	_download_request = HTTPRequest.new()
	_download_request.name = "DownloadRequest"
	add_child(_download_request)
	_audio_manager = AudioManager.new()
	_audio_manager.name = "AudioManager"
	add_child(_audio_manager)


func _connect_signals() -> void:
	_glb_importer.import_completed.connect(_on_import_completed)
	_glb_importer.import_failed.connect(_on_import_failed)
	_glb_importer.import_log.connect(func(message: String) -> void: _set_status(message, Color(0.6, 0.8, 1.0)))
	_ai_service.profile_received.connect(_on_profile_received)
	_ai_service.request_failed.connect(_on_ai_failed)
	_dynamic_rigger.rigging_completed.connect(_on_rigging_completed)
	_dynamic_rigger.rigging_failed.connect(_on_rigging_failed)
	_download_request.request_completed.connect(_on_http_download_complete)
	file_button.pressed.connect(_on_file_browse)
	url_button.pressed.connect(_on_url_import)
	generate_btn.pressed.connect(_on_generate_ai)
	manual_btn.pressed.connect(_on_manual_edit)
	spawn_btn.pressed.connect(_on_spawn_pressed)
	save_btn.pressed.connect(_on_save_pressed)
	play_btn.pressed.connect(_on_play_pressed)
	library_load_btn.pressed.connect(_on_library_load)
	library_delete_btn.pressed.connect(_on_library_delete)
	combo_list.item_selected.connect(_on_combo_selected)


func _setup_preview_scene() -> void:
	_arena_node = Node3D.new()
	_arena_node.name = "PreviewArena"
	preview_viewport.add_child(_arena_node)

	var world_environment: WorldEnvironment = WorldEnvironment.new()
	var env: Environment = Environment.new()
	env.background_mode = Environment.BG_COLOR
	env.background_color = Color(0.08, 0.06, 0.1)
	env.ambient_light_source = Environment.AMBIENT_SOURCE_COLOR
	env.ambient_light_color = Color(0.3, 0.3, 0.4)
	env.ambient_light_energy = 0.75
	env.glow_enabled = true
	env.glow_intensity = 0.35
	world_environment.environment = env
	_arena_node.add_child(world_environment)

	var ground: MeshInstance3D = MeshInstance3D.new()
	ground.name = "Ground"
	var ground_mesh: PlaneMesh = PlaneMesh.new()
	ground_mesh.size = Vector2(14, 14)
	ground.mesh = ground_mesh
	ground.position.y = -0.5
	var ground_mat: StandardMaterial3D = StandardMaterial3D.new()
	ground_mat.albedo_color = Color(0.15, 0.12, 0.18)
	ground.material_override = ground_mat
	_arena_node.add_child(ground)

	_camera_3d = preview_viewport.get_node_or_null("PreviewCamera3D") as Camera3D
	if _camera_3d == null:
		_camera_3d = Camera3D.new()
		_camera_3d.name = "PreviewCamera3D"
		preview_viewport.add_child(_camera_3d)
	_camera_3d.position = Vector3(0, 3, 6)
	_camera_3d.look_at(Vector3.ZERO, Vector3.UP)
	_camera_3d.make_current()

	_camera_light = preview_viewport.get_node_or_null("PreviewLight") as DirectionalLight3D
	if _camera_light == null:
		_camera_light = DirectionalLight3D.new()
		_camera_light.name = "PreviewLight"
		preview_viewport.add_child(_camera_light)
	_camera_light.rotation_degrees = Vector3(-45, 30, 0)
	_camera_light.light_energy = 1.7
	_camera_light.shadow_enabled = true

	_screen_shake = ScreenShake.new()
	_screen_shake.camera = _camera_3d
	_arena_node.add_child(_screen_shake)
	_post_processing = PostProcessing.new()
	_post_processing.camera = _camera_3d
	add_child(_post_processing)


func _load_config() -> void:
	var config: ConfigFile = ConfigFile.new()
	if config.load("user://config.cfg") == OK:
		api_key_input.text = str(config.get_value("settings", "api_key", ""))


func _save_config() -> void:
	var config: ConfigFile = ConfigFile.new()
	config.set_value("settings", "api_key", api_key_input.text)
	config.save("user://config.cfg")


func _set_default_prompt() -> void:
	if skill_input.text.strip_edges().is_empty():
		skill_input.text = DEFAULT_PROMPT


func _on_file_browse() -> void:
	var file_dialog: FileDialog = FileDialog.new()
	file_dialog.file_mode = FileDialog.FILE_MODE_OPEN_FILE
	file_dialog.access = FileDialog.ACCESS_FILESYSTEM
	file_dialog.add_filter("*.glb ; GLB Model Files")
	file_dialog.add_filter("*.gltf ; GLTF Model Files")
	file_dialog.title = "GLB/GLTF Datei auswählen"
	add_child(file_dialog)
	file_dialog.file_selected.connect(_on_file_selected.bind(file_dialog))
	file_dialog.canceled.connect(file_dialog.queue_free)
	file_dialog.popup_centered_ratio(0.8)


func _on_file_selected(path: String, dialog: FileDialog) -> void:
	dialog.queue_free()
	file_input.text = path
	_current_file_path = path
	_load_asset(path)


func _on_url_import() -> void:
	var input: String = file_input.text.strip_edges()
	if input.is_empty():
		_set_status("Bitte URL oder lokalen Pfad eingeben", Color(1, 0.3, 0.3))
		return
	if input.begins_with("http://") or input.begins_with("https://"):
		_download_asset(input)
	else:
		_current_file_path = input
		_load_asset(input)


func _download_asset(url: String) -> void:
	_ensure_user_subdir("downloads")
	_set_status("Download von URL...", Color(0.5, 0.8, 1.0))
	progress_bar.value = 5
	var err: Error = _download_request.request(url)
	if err != OK:
		_set_status("Download konnte nicht gestartet werden: " + error_string(err), Color(1, 0.3, 0.3))


func _on_http_download_complete(_result: int, response_code: int, _headers: PackedStringArray, body: PackedByteArray) -> void:
	if response_code < 200 or response_code >= 300:
		_set_status("Download fehlgeschlagen: HTTP " + str(response_code), Color(1, 0.3, 0.3))
		return
	var source_name: String = file_input.text.get_file()
	if source_name.get_extension().to_lower() not in ["glb", "gltf"]:
		source_name = "downloaded_asset.glb"
	var output_path: String = DOWNLOAD_PATH + _sanitize_file_name(source_name)
	var file: FileAccess = FileAccess.open(output_path, FileAccess.WRITE)
	if file == null:
		_set_status("Kann Download nicht speichern", Color(1, 0.3, 0.3))
		return
	file.store_buffer(body)
	file.close()
	_current_file_path = output_path
	file_input.text = output_path
	_load_asset(output_path)


func _load_asset(path: String) -> void:
	_set_status("Lade GLB/GLTF: " + path, Color(0.5, 0.8, 1))
	progress_bar.value = 10
	if not FileAccess.file_exists(path):
		_set_status("Datei nicht gefunden: " + path, Color(1, 0.3, 0.3))
		progress_bar.value = 0
		return
	_clear_loaded_asset()
	var entity: Node3D = _glb_importer.load_glb(path)
	if entity == null:
		_set_status("Fehler beim Laden der GLB/GLTF", Color(1, 0.3, 0.3))
		progress_bar.value = 0
		return
	_is_loaded = true
	_loaded_entity = entity
	_update_preview(entity)


func _clear_loaded_asset() -> void:
	if _loaded_entity and is_instance_valid(_loaded_entity):
		_loaded_entity.queue_free()
	_loaded_entity = null
	if _preview_entity and is_instance_valid(_preview_entity):
		_preview_entity.queue_free()
	_preview_entity = null
	_is_loaded = false
	_is_spawned = false


func _on_import_completed(node: Node3D) -> void:
	_set_status("GLB geladen: " + node.name, Color(0.3, 1, 0.3))
	progress_bar.value = 30
	asset_status.text = "✅ " + node.name + " (" + str(_glb_importer.get_mesh_count(node)) + " Meshes)"


func _on_import_failed(error: String) -> void:
	_set_status("Import fehlgeschlagen: " + error, Color(1, 0.3, 0.3))
	progress_bar.value = 0


func _update_preview(entity: Node3D) -> void:
	if _preview_entity and is_instance_valid(_preview_entity):
		_preview_entity.queue_free()
	_preview_entity = entity.duplicate() as Node3D
	_arena_node.add_child(_preview_entity)
	_preview_entity.position = Vector3.ZERO
	_focus_camera_on(_preview_entity)


func _focus_camera_on(entity: Node3D) -> void:
	var aabb: AABB = _glb_importer._compute_aabb(entity)
	var center: Vector3 = aabb.get_center()
	var size_len: float = maxf(aabb.size.length(), 2.0)
	var distance: float = maxf(size_len * 1.8, 4.0)
	_camera_3d.position = center + Vector3(0, distance * 0.45, distance)
	_camera_3d.look_at(center, Vector3.UP)


func _on_generate_ai() -> void:
	_save_config()
	var api_key: String = _get_api_key()
	if api_key.is_empty():
		_set_status("Bitte OpenAI API-Key eingeben", Color(1, 0.3, 0.3))
		return
	var prompt: String = skill_input.text.strip_edges()
	if prompt.is_empty():
		_set_status("Bitte eine Beschreibung eingeben", Color(1, 0.3, 0.3))
		return
	var node_names: Array[String] = _get_node_names(_loaded_entity) if _loaded_entity else ["Root", "Body", "Head", "RightHand", "LeftHand"]
	_set_status("Sende Anfrage an KI...", Color(0.5, 0.8, 1))
	progress_bar.value = 40
	_ai_service.request_entity_profile(node_names, prompt, api_key)


func _on_profile_received(profile_data: Dictionary) -> void:
	_skill_data = SkillData.new()
	_skill_data.load_from_json(profile_data)
	_update_combo_list()
	_update_skill_preview()
	_preview_effects()
	progress_bar.value = 60
	_set_status("KI-Profil geladen: " + str(_skill_data.combos.size()) + " Combos", Color(0.3, 1, 0.3))


func _on_ai_failed(error: String) -> void:
	_set_status("KI-Fehler: " + error, Color(1, 0.3, 0.3))
	progress_bar.value = 0


func _update_combo_list() -> void:
	combo_list.clear()
	for combo in _skill_data.combos:
		var name: String = str(combo.get("name", "Unnamed"))
		var damage: float = float(combo.get("damage_multiplier", 1.0))
		var delay: float = float(combo.get("delay", 0.2))
		var color: String = str(combo.get("vfx_color", "#FF0000"))
		combo_list.add_item("%s (%.1fx, %.2fs, %s)" % [name, damage, delay, color])


func _update_skill_preview() -> void:
	var preview_text: String = "⚡ Speed: %.1f | 💥 Power: %.1f\n" % [_skill_data.move_speed, _skill_data.attack_power]
	preview_text += "🔧 Combos: " + str(_skill_data.combos.size())
	skill_preview.text = preview_text


func _preview_effects() -> void:
	if _preview_entity == null or _skill_data.combos.is_empty():
		return
	var combo: Dictionary = _skill_data.combos[0]
	var spark: HitSpark = HitSpark.new()
	_arena_node.add_child(spark)
	spark.global_position = _preview_entity.global_position + Vector3(0, 1.2, 0.8)
	spark.initialize(_skill_data.attack_power * float(combo.get("damage_multiplier", 1.0)), Vector3.UP, false)


func _on_manual_edit() -> void:
	var dialog: ConfirmationDialog = ConfirmationDialog.new()
	dialog.title = "Manuelle Bearbeitung"
	dialog.ok_button_text = "Übernehmen"
	var vbox: VBoxContainer = VBoxContainer.new()
	vbox.custom_minimum_size = Vector2(420, 340)
	dialog.add_child(vbox)

	var speed_input: SpinBox = _make_spinbox("Move Speed", _skill_data.move_speed, 0.1, 20.0, 0.1, vbox)
	var power_input: SpinBox = _make_spinbox("Attack Power", _skill_data.attack_power, 0.1, 200.0, 0.5, vbox)
	var combo_json: TextEdit = TextEdit.new()
	combo_json.custom_minimum_size = Vector2(400, 210)
	combo_json.text = JSON.stringify(_skill_data.combos, "\t")
	vbox.add_child(Label.new())
	(vbox.get_child(vbox.get_child_count() - 1) as Label).text = "Combos JSON"
	vbox.add_child(combo_json)

	dialog.confirmed.connect(func() -> void:
		_skill_data.move_speed = float(speed_input.value)
		_skill_data.attack_power = float(power_input.value)
		var parsed: Variant = JSON.parse_string(combo_json.text)
		if parsed is Array:
			_skill_data.combos.clear()
			var parsed_combos: Array = parsed as Array
			for item in parsed_combos:
				if item is Dictionary:
					_skill_data.combos.append(item as Dictionary)
		_update_combo_list()
		_update_skill_preview()
		_set_status("Manuelle Werte übernommen", Color(0.3, 1, 0.3))
	)
	add_child(dialog)
	dialog.popup_centered()


func _make_spinbox(label_text: String, value: float, min_value: float, max_value: float, step: float, parent: VBoxContainer) -> SpinBox:
	var label: Label = Label.new()
	label.text = label_text
	parent.add_child(label)
	var spin: SpinBox = SpinBox.new()
	spin.min_value = min_value
	spin.max_value = max_value
	spin.step = step
	spin.value = value
	parent.add_child(spin)
	return spin


func _on_combo_selected(index: int) -> void:
	if index < 0 or index >= _skill_data.combos.size():
		return
	var combo: Dictionary = _skill_data.combos[index]
	var details: String = "Name: %s | Delay: %.2fs | Damage: %.1fx | Radius: %.1f | VFX: %s | Bone: %s" % [
		str(combo.get("name", "")),
		float(combo.get("delay", 0.0)),
		float(combo.get("damage_multiplier", 0.0)),
		float(combo.get("hitbox_radius", 0.0)),
		str(combo.get("vfx_color", "#FFFFFF")),
		str(combo.get("attach_bone", "Root"))
	]
	_set_status(details, Color(0.8, 0.8, 0.5))


func _on_spawn_pressed() -> void:
	if not _is_loaded or _loaded_entity == null:
		_set_status("Bitte zuerst ein GLB laden", Color(1, 0.3, 0.3))
		return
	if not _skill_data.is_valid():
		_set_status("Bitte zuerst ein gültiges Profil generieren oder bearbeiten", Color(1, 0.3, 0.3))
		return
	_clear_spawned_entity()
	_set_status("Rigge Entity...", Color(0.5, 0.8, 1))
	progress_bar.value = 70
	_active_spawn_entity = _loaded_entity.duplicate() as Node3D
	_arena_node.add_child(_active_spawn_entity)
	_active_spawn_entity.position = Vector3.ZERO
	_dynamic_rigger.rig_entity(_active_spawn_entity, _skill_data)


func _clear_spawned_entity() -> void:
	if _combat_controller and is_instance_valid(_combat_controller):
		_combat_controller.queue_free()
		_combat_controller = null
	elif _active_spawn_entity and is_instance_valid(_active_spawn_entity):
		_active_spawn_entity.queue_free()
	_active_spawn_entity = null
	_dynamic_rigger.cleanup()
	_is_spawned = false


func _on_rigging_completed() -> void:
	if _active_spawn_entity == null:
		return
	_combat_controller = CombatController.new()
	_combat_controller.name = "PreviewCombatController"
	_combat_controller.skill_data = _skill_data
	_combat_controller.rigger = _dynamic_rigger
	_combat_controller.position = Vector3.ZERO
	var shape: CollisionShape3D = CollisionShape3D.new()
	var capsule: CapsuleShape3D = CapsuleShape3D.new()
	capsule.radius = 0.45
	capsule.height = 1.8
	shape.shape = capsule
	shape.position.y = 0.4
	_combat_controller.add_child(shape)
	_arena_node.add_child(_combat_controller)
	_active_spawn_entity.reparent(_combat_controller)
	_active_spawn_entity.position = Vector3.ZERO
	_is_spawned = true
	progress_bar.value = 100
	_set_status("✅ Bereit für den Kampf! Drücke 'Spielen'", Color(0.3, 1, 0.3))


func _on_rigging_failed(error: String) -> void:
	_set_status("Rigging fehlgeschlagen: " + error, Color(1, 0.3, 0.3))
	progress_bar.value = 0


func _on_save_pressed() -> void:
	if not _is_loaded or not _skill_data.is_valid():
		_set_status("Bitte zuerst Asset und Profil laden/generieren", Color(1, 0.3, 0.3))
		return
	var dialog: ConfirmationDialog = ConfirmationDialog.new()
	dialog.title = "Profil speichern"
	dialog.ok_button_text = "Speichern"
	var vbox: VBoxContainer = VBoxContainer.new()
	vbox.custom_minimum_size = Vector2(360, 80)
	var name_input: LineEdit = LineEdit.new()
	name_input.placeholder_text = "Name des Charakters..."
	name_input.text = _current_profile_name
	vbox.add_child(name_input)
	dialog.add_child(vbox)
	dialog.confirmed.connect(func() -> void:
		var profile_name: String = name_input.text.strip_edges()
		if profile_name.is_empty():
			profile_name = "Charakter_" + Time.get_datetime_string_from_system().replace(":", "-").replace(" ", "_")
		_save_profile(profile_name)
	)
	add_child(dialog)
	dialog.popup_centered()


func _save_profile(name: String) -> void:
	_ensure_user_subdir("profiles")
	var safe_name: String = _sanitize_file_name(name)
	var profile: Dictionary = {
		"name": name,
		"file_path": _current_file_path,
		"skill_data": _skill_to_dictionary(),
		"vfx_config": _get_vfx_config(),
		"timestamp": Time.get_datetime_string_from_system()
	}
	var file_path: String = PROFILE_PATH + safe_name + PROFILE_EXTENSION
	var file: FileAccess = FileAccess.open(file_path, FileAccess.WRITE)
	if file == null:
		_set_status("Profil konnte nicht gespeichert werden", Color(1, 0.3, 0.3))
		return
	file.store_string(JSON.stringify(profile, "\t"))
	file.close()
	_current_profile_name = name
	_set_status("Profil gespeichert: " + name, Color(0.3, 1, 0.3))
	_load_library()


func _skill_to_dictionary() -> Dictionary:
	return {"move_speed": _skill_data.move_speed, "attack_power": _skill_data.attack_power, "combos": _skill_data.combos.duplicate(true)}


func _get_vfx_config() -> Dictionary:
	var config: Dictionary = {}
	for combo in _skill_data.combos:
		var combo_name: String = str(combo.get("name", ""))
		if not combo_name.is_empty():
			config[combo_name] = {"color": str(combo.get("vfx_color", "#FF0000")), "attach_bone": str(combo.get("attach_bone", "Root"))}
	return config


func _load_library() -> void:
	library_list.clear()
	_library_profiles.clear()
	_ensure_user_subdir("profiles")
	var dir: DirAccess = DirAccess.open(PROFILE_PATH)
	if dir == null:
		return
	for file_name in dir.get_files():
		if file_name.ends_with(PROFILE_EXTENSION):
			var data: String = FileAccess.get_file_as_string(PROFILE_PATH + file_name)
			var parsed: Variant = JSON.parse_string(data)
			if parsed is Dictionary and (parsed as Dictionary).has("name"):
				var profile: Dictionary = parsed as Dictionary
				profile["_file_name"] = file_name
				_library_profiles.append(profile)
				library_list.add_item(str(profile["name"]))
	_set_status(str(_library_profiles.size()) + " Profile in Bibliothek", Color(0.5, 0.8, 1))


func _on_library_load() -> void:
	var selected: PackedInt32Array = library_list.get_selected_items()
	if selected.is_empty():
		return
	var idx: int = selected[0]
	if idx >= 0 and idx < _library_profiles.size():
		_load_profile(_library_profiles[idx])


func _on_library_delete() -> void:
	var selected: PackedInt32Array = library_list.get_selected_items()
	if selected.is_empty():
		return
	var idx: int = selected[0]
	if idx < 0 or idx >= _library_profiles.size():
		return
	var profile: Dictionary = _library_profiles[idx]
	var file_name: String = str(profile.get("_file_name", _sanitize_file_name(str(profile.get("name", "profile"))) + PROFILE_EXTENSION))
	DirAccess.remove_absolute(PROFILE_PATH + file_name)
	_load_library()
	_set_status("Profil gelöscht: " + str(profile.get("name", "")), Color(1, 0.5, 0.5))


func _load_profile(profile: Dictionary) -> void:
	if profile.has("skill_data") and profile["skill_data"] is Dictionary:
		_skill_data = SkillData.new()
		_skill_data.load_from_json(profile["skill_data"] as Dictionary)
		_update_combo_list()
		_update_skill_preview()
	if profile.has("file_path"):
		var path: String = str(profile["file_path"])
		file_input.text = path
		_current_file_path = path
		if FileAccess.file_exists(path):
			_load_asset(path)
	_current_profile_name = str(profile.get("name", ""))
	_set_status("Profil geladen: " + _current_profile_name, Color(0.3, 1, 0.3))


func _on_play_pressed() -> void:
	if not _is_loaded or _loaded_entity == null:
		_set_status("Bitte zuerst Asset laden", Color(1, 0.3, 0.3))
		return
	if not _skill_data.is_valid():
		_set_status("Bitte zuerst gültiges Skill-Profil erstellen", Color(1, 0.3, 0.3))
		return
	GlobalData.set_player_data(_skill_data, _loaded_entity, _current_file_path, _current_profile_name)
	get_tree().change_scene_to_file("res://scenes/combat/Arena.tscn")


func _set_status(text: String, color: Color = Color.WHITE) -> void:
	status_label.text = text
	status_label.modulate = color


func _get_api_key() -> String:
	if api_key_input and not api_key_input.text.strip_edges().is_empty():
		return api_key_input.text.strip_edges()
	var config: ConfigFile = ConfigFile.new()
	if config.load("user://config.cfg") == OK:
		return str(config.get_value("settings", "api_key", ""))
	return ""


func _get_node_names(node: Node) -> Array[String]:
	var names: Array[String] = []
	if node:
		_get_node_names_recursive(node, names)
	return names


func _get_node_names_recursive(node: Node, names: Array[String]) -> void:
	names.append(node.name)
	for child in node.get_children():
		_get_node_names_recursive(child, names)


func _create_default_skill_data() -> SkillData:
	var skill: SkillData = SkillData.new()
	skill.move_speed = 4.2
	skill.attack_power = 18.5
	skill.combos = [
		{"name": "Claw Swipe", "delay": 0.2, "damage_multiplier": 1.2, "hitbox_radius": 1.8, "vfx_color": "#FF4400", "attach_bone": "Hand_R"},
		{"name": "Fire Breath", "delay": 0.5, "damage_multiplier": 2.5, "hitbox_radius": 3.0, "vfx_color": "#FF2200", "attach_bone": "Head"}
	]
	return skill


func _ensure_user_subdir(dir_name: String) -> void:
	var dir: DirAccess = DirAccess.open("user://")
	if dir:
		dir.make_dir_recursive(dir_name)


func _sanitize_file_name(value: String) -> String:
	var result: String = value.strip_edges()
	for c in ["/", "\\", ":", "*", "?", "\"", "<", ">", "|", " "]:
		result = result.replace(c, "_")
	return result if not result.is_empty() else "profile"
