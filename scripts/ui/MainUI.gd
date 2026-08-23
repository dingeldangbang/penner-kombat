extends Control
class_name MainUI

## Main UI controller for Penner Kombat mobile.

@onready var api_key_input: LineEdit = $MainPanel/VBoxContainer/ApiKeyInput
@onready var description_input: TextEdit = $MainPanel/VBoxContainer/DescriptionInput
@onready var file_input: LineEdit = $MainPanel/VBoxContainer/FileContainer/FileInput
@onready var status_label: Label = $MainPanel/VBoxContainer/StatusLabel
@onready var progress_bar: ProgressBar = $MainPanel/VBoxContainer/ProgressBar
@onready var log_output: TextEdit = $MainPanel/VBoxContainer/LogOutput
@onready var spawn_button: Button = $MainPanel/VBoxContainer/ActionContainer/SpawnButton
@onready var test_button: Button = $MainPanel/VBoxContainer/ActionContainer/TestButton
@onready var file_button: Button = $MainPanel/VBoxContainer/FileContainer/FileButton
@onready var virtual_joystick: VirtualJoystick = $TouchControls/VirtualJoystick
@onready var attack_button: Button = $TouchControls/AttackButton

var _glb_importer: GlbImporter
var _ai_service: AiService
var _dynamic_rigger: DynamicRigger
var _skill_data: SkillData
var _combat_controller: CombatController
var _spawned_entity: Node3D
var _arena_node: Node3D
var _error_handler: ErrorHandler
var _lod_manager: LodManager
var _arena_lighting: ArenaLighting
var _screen_shake: ScreenShake
var _post_processing: PostProcessing
var _performance_manager: PerformanceManager
var _localization: Localization
var _audio_manager: AudioManager
var _hud: HUD
var _combo_count: int = 0
var _is_testing_ai: bool = false

const CONFIG_PATH: String = "user://config.cfg"
const DEFAULT_DESCRIPTION: String = "A slow but powerful brawler with heavy punches, fire VFX, and a devastating uppercut combo."


func _ready() -> void:
	_setup_systems()
	_load_config()
	_connect_signals()
	_setup_arena()
	_set_status("Ready", Color.WHITE)


func _setup_systems() -> void:
	_glb_importer = GlbImporter.new()
	add_child(_glb_importer)

	_ai_service = AiService.new()
	add_child(_ai_service)

	_dynamic_rigger = DynamicRigger.new()
	add_child(_dynamic_rigger)

	_skill_data = SkillData.new()

	_error_handler = ErrorHandler.new()
	_error_handler.name = "ErrorHandler"
	add_child(_error_handler)
	_error_handler.set_ui_target(log_output)

	_lod_manager = LodManager.new()
	_lod_manager.name = "LodManager"
	add_child(_lod_manager)

	_performance_manager = PerformanceManager.new()
	_performance_manager.name = "PerformanceManager"
	add_child(_performance_manager)

	_localization = Localization.new()
	_localization.name = "Localization"
	add_child(_localization)

	_audio_manager = AudioManager.new()
	_audio_manager.name = "AudioManager"
	add_child(_audio_manager)

	var hud_scene: PackedScene = load("res://scenes/ui/HUD.tscn") as PackedScene
	if hud_scene:
		_hud = hud_scene.instantiate() as HUD
		_hud.visible = false
		add_child(_hud)


func _connect_signals() -> void:
	_glb_importer.import_completed.connect(_on_import_completed)
	_glb_importer.import_failed.connect(_on_import_failed)
	_glb_importer.import_log.connect(_log_message)
	_ai_service.profile_received.connect(_on_profile_received)
	_ai_service.request_failed.connect(_on_ai_failed)
	_dynamic_rigger.rigging_completed.connect(_on_rigging_completed)
	_dynamic_rigger.rigging_failed.connect(_on_rigging_failed)
	spawn_button.pressed.connect(_on_spawn_pressed)
	test_button.pressed.connect(_on_test_pressed)
	file_button.pressed.connect(_on_file_browse_pressed)
	virtual_joystick.joystick_moved.connect(_on_joystick_moved)
	virtual_joystick.joystick_released.connect(_on_joystick_released)
	attack_button.pressed.connect(_on_attack_pressed)


func _load_config() -> void:
	description_input.text = DEFAULT_DESCRIPTION
	if not FileAccess.file_exists(CONFIG_PATH):
		return
	var config: ConfigFile = ConfigFile.new()
	if config.load(CONFIG_PATH) != OK:
		return
	api_key_input.text = str(config.get_value("settings", "api_key", ""))
	file_input.text = str(config.get_value("settings", "last_file", ""))
	description_input.text = str(config.get_value("settings", "description", DEFAULT_DESCRIPTION))


func _save_config() -> void:
	var config: ConfigFile = ConfigFile.new()
	config.set_value("settings", "api_key", api_key_input.text)
	config.set_value("settings", "last_file", file_input.text)
	config.set_value("settings", "description", description_input.text)
	var err: Error = config.save(CONFIG_PATH)
	if err != OK:
		_log_message("Warning: could not save config: " + error_string(err))


func _setup_arena() -> void:
	_arena_node = Node3D.new()
	_arena_node.name = "Arena"
	add_child(_arena_node)

	_arena_lighting = ArenaLighting.new()
	_arena_lighting.name = "ArenaLighting"
	_arena_node.add_child(_arena_lighting)

	var ground: MeshInstance3D = MeshInstance3D.new()
	ground.name = "Ground"
	var ground_mesh: PlaneMesh = PlaneMesh.new()
	ground_mesh.size = Vector2(20, 20)
	ground.mesh = ground_mesh
	ground.position.y = -0.5
	var material: StandardMaterial3D = StandardMaterial3D.new()
	material.albedo_color = Color(0.12, 0.1, 0.13)
	ground.material_override = material
	_arena_node.add_child(ground)

	var ground_body: StaticBody3D = StaticBody3D.new()
	ground_body.name = "GroundBody"
	ground_body.position.y = -0.5
	var ground_shape: CollisionShape3D = CollisionShape3D.new()
	var box: BoxShape3D = BoxShape3D.new()
	box.size = Vector3(20, 0.1, 20)
	ground_shape.shape = box
	ground_body.add_child(ground_shape)
	_arena_node.add_child(ground_body)

	var camera: Camera3D = Camera3D.new()
	camera.name = "Camera3D"
	camera.position = Vector3(0, 4.5, 8)
	camera.look_at(Vector3.ZERO, Vector3.UP)
	camera.make_current()
	_arena_node.add_child(camera)

	_screen_shake = ScreenShake.new()
	_screen_shake.name = "ScreenShake"
	_screen_shake.camera = camera
	_arena_node.add_child(_screen_shake)

	_post_processing = PostProcessing.new()
	_post_processing.name = "PostProcessing"
	_post_processing.camera = camera
	add_child(_post_processing)


func _on_spawn_pressed() -> void:
	_save_config()
	_is_testing_ai = false

	if file_input.text.strip_edges().is_empty():
		_set_status("Please select a GLB/GLTF file", Color(1, 0.3, 0.3))
		return
	if api_key_input.text.strip_edges().is_empty():
		_set_status("Please enter your OpenAI API key", Color(1, 0.3, 0.3))
		return

	_clear_existing_entity()
	_set_status("Loading GLB file...", Color(0.5, 0.8, 1))
	progress_bar.value = 10

	var file_path: String = _resolve_model_path(file_input.text.strip_edges())
	if file_path.is_empty():
		_set_status("File not found: " + file_input.text, Color(1, 0.3, 0.3))
		progress_bar.value = 0
		return

	var entity: Node3D = _glb_importer.load_glb(file_path)
	if entity == null:
		_set_status("Failed to load GLB/GLTF file", Color(1, 0.3, 0.3))
		progress_bar.value = 0
		return

	_spawned_entity = entity
	_arena_node.add_child(entity)
	entity.position = Vector3.ZERO
	_set_status("Model loaded. Requesting AI profile...", Color(0.5, 0.8, 1))
	progress_bar.value = 30

	_ai_service.request_entity_profile(_get_node_names(entity), description_input.text, api_key_input.text)


func _resolve_model_path(input_path: String) -> String:
	if FileAccess.file_exists(input_path):
		return input_path
	var user_path: String = "user://" + input_path.get_file()
	if FileAccess.file_exists(user_path):
		return user_path
	var res_path: String = "res://" + input_path.get_file()
	if FileAccess.file_exists(res_path):
		return res_path
	return ""


func _clear_existing_entity() -> void:
	if _combat_controller and is_instance_valid(_combat_controller):
		_combat_controller.queue_free()
		_combat_controller = null
		_combo_count = 0
		if _hud:
			_hud.update_combo(0)
			_hud.visible = false
	elif _spawned_entity and is_instance_valid(_spawned_entity):
		_spawned_entity.queue_free()
	_spawned_entity = null
	_dynamic_rigger.cleanup()
	if _lod_manager:
		_lod_manager.clear()


func _on_profile_received(profile_data: Dictionary) -> void:
	if _is_testing_ai:
		_is_testing_ai = false
		progress_bar.value = 100
		_set_status("✅ AI connection works", Color(0.3, 1, 0.3))
		_log_message("Test profile received: " + JSON.stringify(profile_data))
		return

	_set_status("AI profile received. Parsing data...", Color(0.5, 0.8, 1))
	progress_bar.value = 60
	_skill_data = SkillData.new()
	_skill_data.load_from_json(profile_data)
	if not _skill_data.is_valid():
		_set_status("Invalid skill data received", Color(1, 0.3, 0.3))
		progress_bar.value = 0
		return

	_set_status("Rigging entity...", Color(0.5, 0.8, 1))
	progress_bar.value = 70
	_dynamic_rigger.rig_entity(_spawned_entity, _skill_data)


func _on_ai_failed(error: String) -> void:
	_is_testing_ai = false
	_set_status("AI request failed: " + error, Color(1, 0.3, 0.3))
	progress_bar.value = 0
	_log_message("ERROR: " + error)


func _on_import_completed(node: Node3D) -> void:
	_log_message("GLB/GLTF import completed: " + node.name)
	_log_message("Found " + str(_glb_importer.get_mesh_count(node)) + " meshes")


func _on_import_failed(error: String) -> void:
	_set_status("Import failed: " + error, Color(1, 0.3, 0.3))
	progress_bar.value = 0
	_log_message("ERROR: " + error)


func _on_rigging_completed() -> void:
	_set_status("Entity rigged successfully. Adding CombatController...", Color(0.3, 1, 0.3))
	progress_bar.value = 90

	_combat_controller = CombatController.new()
	_combat_controller.name = "CombatController"
	_combat_controller.skill_data = _skill_data
	_combat_controller.rigger = _dynamic_rigger
	_combat_controller.position = Vector3.ZERO
	_combat_controller.health_changed.connect(_on_player_health_changed)
	_combat_controller.combo_executed.connect(_on_player_combo_executed)

	var body_shape: CollisionShape3D = CollisionShape3D.new()
	var capsule: CapsuleShape3D = CapsuleShape3D.new()
	capsule.radius = 0.45
	capsule.height = 1.8
	body_shape.shape = capsule
	body_shape.position.y = 0.4
	_combat_controller.add_child(body_shape)

	_arena_node.add_child(_combat_controller)
	_spawned_entity.reparent(_combat_controller)
	_spawned_entity.position = Vector3.ZERO

	_combo_count = 0
	progress_bar.value = 100
	_set_status("✅ Entity ready for combat!", Color(0.3, 1, 0.3))
	if _lod_manager:
		_lod_manager.register_tree(_combat_controller)
	if _hud:
		_hud.visible = true
		_hud.update_health(_combat_controller.current_health, 100.0, _combat_controller.max_health)
	_log_message("Entity ready with " + str(_skill_data.combos.size()) + " combos: " + str(_skill_data.get_combo_names()))


func _on_rigging_failed(error: String) -> void:
	_set_status("Rigging failed: " + error, Color(1, 0.3, 0.3))
	progress_bar.value = 0
	_log_message("ERROR: " + error)


func _on_test_pressed() -> void:
	if api_key_input.text.strip_edges().is_empty():
		_set_status("Please enter your OpenAI API key", Color(1, 0.3, 0.3))
		return
	_is_testing_ai = true
	progress_bar.value = 20
	_set_status("Testing AI connection...", Color(0.5, 0.8, 1))
	_log_message("Testing AI connection...")
	_ai_service.test_connection(api_key_input.text)


func _on_file_browse_pressed() -> void:
	var file_dialog: FileDialog = FileDialog.new()
	file_dialog.file_mode = FileDialog.FILE_MODE_OPEN_FILE
	file_dialog.access = FileDialog.ACCESS_FILESYSTEM
	file_dialog.add_filter("*.glb ; GLB Model Files")
	file_dialog.add_filter("*.gltf ; GLTF Model Files")
	file_dialog.title = "Select GLB/GLTF File"
	add_child(file_dialog)
	file_dialog.file_selected.connect(_on_file_selected.bind(file_dialog))
	file_dialog.canceled.connect(file_dialog.queue_free)
	file_dialog.popup_centered_ratio(0.8)


func _on_file_selected(path: String, file_dialog: FileDialog) -> void:
	file_input.text = path
	_log_message("Selected file: " + path)
	file_dialog.queue_free()


func _on_player_health_changed(current_health: float, max_health: float) -> void:
	if _hud:
		_hud.update_health(current_health, 100.0, max_health)


func _on_player_combo_executed(_combo_name: String, damage: float) -> void:
	_combo_count += 1
	if _hud:
		_hud.update_combo(_combo_count)
	if _post_processing:
		_post_processing.apply_combo_effects(_combo_count)
	if damage >= 35.0 and _post_processing:
		_post_processing.activate_slow_mo(0.18, 0.35)


func _on_joystick_moved(input: Vector2) -> void:
	if _combat_controller and is_instance_valid(_combat_controller):
		_combat_controller.set_move_input(input)


func _on_joystick_released() -> void:
	if _combat_controller and is_instance_valid(_combat_controller):
		_combat_controller.set_move_input(Vector2.ZERO)


func _on_attack_pressed() -> void:
	if _combat_controller and is_instance_valid(_combat_controller):
		_combat_controller.execute_next_combo()


func _set_status(text: String, color: Color = Color.WHITE) -> void:
	status_label.text = text
	status_label.modulate = color


func _log_message(text: String) -> void:
	if _error_handler:
		_error_handler.log_info(text)
	else:
		log_output.text += text + "\n"
		log_output.scroll_vertical = log_output.get_line_count()


func _get_node_names(node: Node) -> Array[String]:
	var names: Array[String] = []
	_get_node_names_recursive(node, names)
	return names


func _get_node_names_recursive(node: Node, names: Array[String]) -> void:
	names.append(node.name)
	for child in node.get_children():
		_get_node_names_recursive(child, names)
