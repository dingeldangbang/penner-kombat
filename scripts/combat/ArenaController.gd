extends Node3D
class_name ArenaController

## Combat arena: spawns editor-selected profile, dummy/loaded enemy, rounds, timer and HUD.

@onready var hud: HUD = $HUD
@onready var camera_3d: Camera3D = $CameraRig/Camera3D
@onready var spawn_p1: Marker3D = $SpawnP1
@onready var spawn_p2: Marker3D = $SpawnP2
@onready var virtual_joystick: VirtualJoystick = $TouchLayer/VirtualJoystick
@onready var attack_button: Button = $TouchLayer/AttackButton

var _player1: CombatController
var _player2: CombatController
var _ai_controller: AIController
var _match_timer: float = 99.0
var _is_match_active: bool = false
var _round: int = 1
var _max_rounds: int = 3
var _p1_wins: int = 0
var _p2_wins: int = 0
var _combo_count: int = 0
var _screen_shake: ScreenShake
var _post_processing: PostProcessing
var _audio_manager: AudioManager


func _ready() -> void:
	_setup_systems()
	_connect_touch_controls()
	_start_match()


func _setup_systems() -> void:
	_audio_manager = AudioManager.new()
	_audio_manager.name = "AudioManager"
	add_child(_audio_manager)
	_screen_shake = ScreenShake.new()
	_screen_shake.camera = camera_3d
	add_child(_screen_shake)
	_post_processing = PostProcessing.new()
	_post_processing.camera = camera_3d
	add_child(_post_processing)
	camera_3d.make_current()


func _connect_touch_controls() -> void:
	virtual_joystick.joystick_moved.connect(func(input: Vector2) -> void:
		if _player1: _player1.set_move_input(input)
	)
	virtual_joystick.joystick_released.connect(func() -> void:
		if _player1: _player1.set_move_input(Vector2.ZERO)
	)
	attack_button.pressed.connect(func() -> void:
		if _player1: _player1.execute_next_combo()
	)


func _start_match() -> void:
	_clear_existing_fighters()
	if GlobalData.entity and GlobalData.profile:
		_player1 = _create_combatant(GlobalData.entity.duplicate() as Node3D, GlobalData.profile.duplicate_data(), spawn_p1.global_position, "Player1")
	else:
		_player1 = _create_combatant(_create_dummy_entity(Color(0.2, 0.6, 1.0)), _create_dummy_profile(), spawn_p1.global_position, "Player1")

	if GlobalData.enemy_entity and GlobalData.enemy_profile:
		_player2 = _create_combatant(GlobalData.enemy_entity.duplicate() as Node3D, GlobalData.enemy_profile.duplicate_data(), spawn_p2.global_position, "Player2")
	else:
		_player2 = _create_combatant(_create_dummy_entity(Color(1.0, 0.25, 0.15)), _create_dummy_profile(), spawn_p2.global_position, "AI")

	_ai_controller = AIController.new()
	_ai_controller.fighter = _player2
	_ai_controller.target = _player1
	_player2.add_child(_ai_controller)

	_connect_fighter_signals()
	_match_timer = 99.0
	_combo_count = 0
	_is_match_active = true
	_update_camera()
	_update_hud()


func _clear_existing_fighters() -> void:
	for node in get_tree().get_nodes_in_group("fighter"):
		if is_instance_valid(node):
			node.queue_free()
	_player1 = null
	_player2 = null


func _create_combatant(entity: Node3D, profile: SkillData, spawn_position: Vector3, fighter_name: String) -> CombatController:
	var controller: CombatController = CombatController.new()
	controller.name = fighter_name
	controller.skill_data = profile
	controller.global_position = spawn_position

	var rigger: DynamicRigger = DynamicRigger.new()
	rigger.name = fighter_name + "Rigger"
	controller.rigger = rigger
	controller.add_child(rigger)

	var body_shape: CollisionShape3D = CollisionShape3D.new()
	var capsule: CapsuleShape3D = CapsuleShape3D.new()
	capsule.radius = 0.45
	capsule.height = 1.8
	body_shape.shape = capsule
	body_shape.position.y = 0.4
	controller.add_child(body_shape)

	add_child(controller)
	controller.add_child(entity)
	entity.position = Vector3.ZERO
	rigger.rig_entity(entity, profile)
	return controller


func _create_dummy_entity(color: Color = Color(1, 0.2, 0.2)) -> Node3D:
	var entity: Node3D = Node3D.new()
	entity.name = "DummyEntity"
	var mesh: MeshInstance3D = MeshInstance3D.new()
	mesh.name = "Body"
	var capsule: CapsuleMesh = CapsuleMesh.new()
	capsule.radius = 0.45
	capsule.height = 1.8
	mesh.mesh = capsule
	mesh.position.y = 0.4
	var material: StandardMaterial3D = StandardMaterial3D.new()
	material.albedo_color = color
	material.emission_enabled = true
	material.emission = color * 0.25
	mesh.material_override = material
	entity.add_child(mesh)
	return entity


func _create_dummy_profile() -> SkillData:
	var profile: SkillData = SkillData.new()
	profile.move_speed = 3.5
	profile.attack_power = 10.0
	profile.combos = [
		{"name": "Jab", "delay": 0.2, "damage_multiplier": 1.0, "hitbox_radius": 1.3, "vfx_color": "#FF0000", "attach_bone": "Root"},
		{"name": "Cross", "delay": 0.3, "damage_multiplier": 1.5, "hitbox_radius": 1.7, "vfx_color": "#FF6600", "attach_bone": "Root"}
	]
	return profile


func _connect_fighter_signals() -> void:
	_player1.health_changed.connect(func(_current: float, _max: float) -> void: _update_hud())
	_player2.health_changed.connect(func(_current: float, _max: float) -> void: _update_hud())
	_player1.combo_executed.connect(func(_name: String, damage: float) -> void:
		_combo_count += 1
		GlobalData.add_combo()
		GlobalData.add_damage_dealt(damage)
		if hud: hud.update_combo(_combo_count)
		if _post_processing: _post_processing.apply_combo_effects(_combo_count)
	)


func _physics_process(delta: float) -> void:
	if not _is_match_active:
		return
	_handle_keyboard_input()
	_match_timer -= delta
	if _match_timer <= 0.0 or _is_fighter_defeated(_player1) or _is_fighter_defeated(_player2):
		_end_round()
	_update_camera()
	_update_hud()


func _handle_keyboard_input() -> void:
	if Input.is_action_just_pressed("ui_cancel"):
		get_tree().change_scene_to_file("res://scenes/ui/Editor.tscn")
		return
	if _player1 == null:
		return
	var input: Vector2 = Input.get_vector("ui_left", "ui_right", "ui_up", "ui_down")
	if not virtual_joystick.is_pressed():
		_player1.set_move_input(input)
	if Input.is_action_just_pressed("ui_accept") or Input.is_action_just_pressed("ui_select"):
		_player1.execute_next_combo()


func _update_camera() -> void:
	if _player1 == null or _player2 == null:
		return
	var midpoint: Vector3 = (_player1.global_position + _player2.global_position) * 0.5
	var distance: float = clampf(_player1.global_position.distance_to(_player2.global_position), 4.0, 12.0)
	camera_3d.global_position = camera_3d.global_position.lerp(midpoint + Vector3(0, 5.0 + distance * 0.25, 8.0 + distance * 0.4), 0.08)
	camera_3d.look_at(midpoint + Vector3.UP, Vector3.UP)


func _update_hud() -> void:
	if hud and _player1 and _player2:
		hud.update_health(_player1.current_health, _player2.current_health, _player1.max_health)
		hud.update_timer(_match_timer)
		hud.update_round(_round, _max_rounds)


func _is_fighter_defeated(fighter: CombatController) -> bool:
	return fighter == null or not is_instance_valid(fighter) or fighter.current_health <= 0.0


func _end_round() -> void:
	_is_match_active = false
	var p1_hp: float = _player1.current_health if is_instance_valid(_player1) else 0.0
	var p2_hp: float = _player2.current_health if is_instance_valid(_player2) else 0.0
	if p1_hp >= p2_hp:
		_p1_wins += 1
	else:
		_p2_wins += 1
	if _p1_wins >= 2 or _p2_wins >= 2 or _round >= _max_rounds:
		_end_match()
	else:
		_round += 1
		_reset_round()


func _reset_round() -> void:
	await get_tree().create_timer(1.0).timeout
	if not is_instance_valid(_player1) or not is_instance_valid(_player2):
		_start_match()
		return
	_player1.current_health = _player1.max_health
	_player2.current_health = _player2.max_health
	_player1.global_position = spawn_p1.global_position
	_player2.global_position = spawn_p2.global_position
	_match_timer = 99.0
	_combo_count = 0
	if hud:
		hud.update_combo(0)
	_is_match_active = true


func _end_match() -> void:
	var p1_won: bool = _p1_wins > _p2_wins
	if p1_won:
		GlobalData.player_stats["wins"] = int(GlobalData.player_stats.get("wins", 0)) + 1
	else:
		GlobalData.player_stats["losses"] = int(GlobalData.player_stats.get("losses", 0)) + 1
	if hud:
		hud.show_match_result("P1" if p1_won else "P2")
	if _post_processing:
		_post_processing.activate_fatality_effects()
