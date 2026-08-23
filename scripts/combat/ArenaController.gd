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
var _best_combo: int = 0
var _screen_shake: ScreenShake
var _post_processing: PostProcessing
var _audio_manager: AudioManager

var _block_button: Button
var _jump_button: Button
var _pause_button: Button
var _overlay: Control


func _ready() -> void:
	_setup_systems()
	_build_extra_touch_controls()
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


## Blocken, Springen und Pause gab es nur per Tastatur bzw. gar nicht --
## auf dem Handy waren die Faehigkeiten damit unerreichbar.
func _build_extra_touch_controls() -> void:
	var layer: CanvasLayer = $TouchLayer as CanvasLayer
	var s: float = clampf(GameState.touch_scale, 0.6, 2.0)

	# Der vorhandene Angriffsknopf wird mitskaliert.
	attack_button.add_theme_font_size_override("font_size", int(24 * s))
	attack_button.offset_left = -240.0 * s
	attack_button.offset_top = -190.0 * s

	_block_button = Button.new()
	_block_button.name = "BlockButton"
	_block_button.text = "BLOCK"
	_block_button.set_anchors_preset(Control.PRESET_BOTTOM_RIGHT)
	_block_button.offset_left = -240.0 * s
	_block_button.offset_top = -330.0 * s
	_block_button.offset_right = -130.0 * s
	_block_button.offset_bottom = -200.0 * s
	_block_button.grow_horizontal = Control.GROW_DIRECTION_BEGIN
	_block_button.grow_vertical = Control.GROW_DIRECTION_BEGIN
	_block_button.add_theme_font_size_override("font_size", int(20 * s))
	# Halten statt tippen: Blocken soll nur wirken, solange der Finger liegt.
	_block_button.button_down.connect(func() -> void:
		if _player1: _player1.is_blocking = true)
	_block_button.button_up.connect(func() -> void:
		if _player1: _player1.is_blocking = false)
	layer.add_child(_block_button)

	_jump_button = Button.new()
	_jump_button.name = "JumpButton"
	_jump_button.text = "SPRUNG"
	_jump_button.set_anchors_preset(Control.PRESET_BOTTOM_RIGHT)
	_jump_button.offset_left = -120.0 * s
	_jump_button.offset_top = -330.0 * s
	_jump_button.offset_right = -10.0 * s
	_jump_button.offset_bottom = -200.0 * s
	_jump_button.grow_horizontal = Control.GROW_DIRECTION_BEGIN
	_jump_button.grow_vertical = Control.GROW_DIRECTION_BEGIN
	_jump_button.add_theme_font_size_override("font_size", int(20 * s))
	_jump_button.pressed.connect(func() -> void:
		if _player1: _player1.jump())
	layer.add_child(_jump_button)

	_pause_button = Button.new()
	_pause_button.name = "PauseButton"
	_pause_button.text = "II"
	_pause_button.set_anchors_preset(Control.PRESET_TOP_RIGHT)
	_pause_button.offset_left = -96.0
	_pause_button.offset_top = 16.0
	_pause_button.offset_right = -16.0
	_pause_button.offset_bottom = 80.0
	_pause_button.grow_horizontal = Control.GROW_DIRECTION_BEGIN
	_pause_button.add_theme_font_size_override("font_size", 28)
	_pause_button.pressed.connect(_toggle_pause)
	layer.add_child(_pause_button)


func _announce(text: String) -> void:
	if hud and hud.has_method("announce"):
		hud.announce(text)


func _start_match() -> void:
	_clear_existing_fighters()

	# Spieler: entweder die im Editor gebaute Figur oder ein Kaempfer aus der
	# eingebauten Riege. Frueher gab es nur "Editor-Figur oder grauer Dummy" --
	# und ohne GLB-Datei kam man gar nicht erst hierher.
	if GameState.use_custom_player and GlobalData.entity and GlobalData.profile:
		_player1 = _create_combatant(
			GlobalData.entity.duplicate() as Node3D,
			GlobalData.profile.duplicate_data(),
			spawn_p1.global_position, "Player1")
	else:
		_player1 = _create_roster_combatant(
			GameState.selected_fighter, spawn_p1.global_position, "Player1")

	var opponent_id: String = GameState.opponent_fighter
	if GameState.mode == "arcade":
		opponent_id = GameState.arcade_current_opponent()

	if GlobalData.enemy_entity and GlobalData.enemy_profile:
		_player2 = _create_combatant(
			GlobalData.enemy_entity.duplicate() as Node3D,
			GlobalData.enemy_profile.duplicate_data(),
			spawn_p2.global_position, "Player2")
	else:
		_player2 = _create_roster_combatant(
			opponent_id, spawn_p2.global_position, "AI")

	_ai_controller = AIController.new()
	_ai_controller.fighter = _player2
	_ai_controller.target = _player1
	_ai_controller.difficulty = _ai_difficulty()
	_player2.add_child(_ai_controller)
	# Im Trainingsmodus steht der Gegner still, damit man Combos ueben kann.
	# set_process() erst NACH add_child(): _ready() der KI wuerde sonst
	# dazwischenfunken, und ein noch nicht im Baum haengender Node ignoriert
	# die Einstellung ohnehin.
	if GameState.mode == "training":
		_ai_controller.set_process(false)
		_player2.is_blocking = false

	_connect_fighter_signals()
	_max_rounds = GameState.rounds_to_win * 2 - 1
	_match_timer = GameState.round_seconds
	_combo_count = 0
	_best_combo = 0
	_is_match_active = true
	_update_camera()
	if hud and hud.has_method("set_fighter_names"):
		var p1n: String = "DEINE FIGUR" if GameState.use_custom_player \
			else FighterRoster.by_id(GameState.selected_fighter).display_name
		hud.set_fighter_names(p1n, FighterRoster.by_id(opponent_id).display_name)
	_update_hud()
	_announce(_match_intro_text())


func _ai_difficulty() -> int:
	match GameState.difficulty:
		0: return AIController.Difficulty.EASY
		1: return AIController.Difficulty.MEDIUM
		2: return AIController.Difficulty.HARD
		3: return AIController.Difficulty.VERY_HARD
		_: return AIController.Difficulty.BOSS


func _match_intro_text() -> String:
	if GameState.mode == "arcade":
		return "KAMPF %d / %d" % [GameState.arcade_stage + 1, maxi(1, GameState.arcade_total())]
	if GameState.mode == "training":
		return "TRAINING"
	return "KÄMPFT!"


## Baut einen Kaempfer aus der eingebauten Riege.
func _create_roster_combatant(fighter_id: String, spawn_position: Vector3,
		fighter_name: String) -> CombatController:
	var f: FighterRoster.Fighter = FighterRoster.by_id(fighter_id)
	var controller: CombatController = _create_combatant(
		FighterRoster.build_body(f), f.to_skill_data(), spawn_position, fighter_name)
	controller.max_health = f.max_health
	controller.current_health = f.max_health
	return controller


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
		_best_combo = maxi(_best_combo, _combo_count)
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
		hud.update_health(_player1.current_health, _player2.current_health,
			_player1.max_health, _player2.max_health)
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
	# Nicht fest "2": die Rundenzahl ist in den Einstellungen waehlbar.
	var needed: int = maxi(1, GameState.rounds_to_win)
	if _p1_wins >= needed or _p2_wins >= needed or _round >= _max_rounds:
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
	# Rundenzeit kommt aus den Einstellungen, nicht mehr fest 99 s.
	_match_timer = GameState.round_seconds
	_combo_count = 0
	# Blocken zuruecksetzen: sonst startet die neue Runde mit gedruecktem Block,
	# falls der Finger beim K.O. noch auf dem Knopf lag.
	_player1.is_blocking = false
	_player2.is_blocking = false
	if hud:
		hud.update_combo(0)
	_is_match_active = true
	_announce("RUNDE %d" % _round)


func _end_match() -> void:
	var p1_won: bool = _p1_wins > _p2_wins
	if p1_won:
		GlobalData.player_stats["wins"] = int(GlobalData.player_stats.get("wins", 0)) + 1
	else:
		GlobalData.player_stats["losses"] = int(GlobalData.player_stats.get("losses", 0)) + 1
	if _post_processing:
		_post_processing.activate_fatality_effects()

	GameState.record_match(p1_won, _best_combo)

	var player_name: String = _fighter_display_name(GameState.selected_fighter)
	var enemy_id: String = GameState.arcade_current_opponent() if GameState.mode == "arcade" \
		else GameState.opponent_fighter
	var enemy_name: String = _fighter_display_name(enemy_id)
	var title: String = "%s SIEGT!" % (player_name if p1_won else enemy_name)

	# Frueher blieb hier nur ein Text stehen -- ohne Tastatur kam man aus der
	# Arena nicht mehr heraus. Jetzt gibt es immer einen Weg weiter.
	await get_tree().create_timer(1.2).timeout

	if GameState.mode == "arcade" and p1_won:
		if GameState.arcade_advance():
			_show_overlay(title, [
				{"text": "NÄCHSTER GEGNER", "action": func() -> void:
					_close_overlay()
					_restart_match()},
				{"text": "HAUPTMENÜ", "action": _leave_to_menu},
			])
		else:
			_show_overlay("ARCADE GESCHAFFT!", [
				{"text": "HAUPTMENÜ", "action": _leave_to_menu},
			])
		return

	_show_overlay(title, [
		{"text": "REVANCHE", "action": func() -> void:
			_close_overlay()
			_restart_match()},
		{"text": "HAUPTMENÜ", "action": _leave_to_menu},
	])


func _fighter_display_name(fighter_id: String) -> String:
	if GameState.use_custom_player and fighter_id == GameState.selected_fighter:
		return "DEINE FIGUR"
	return FighterRoster.by_id(fighter_id).display_name


# --------------------------------------------------------------- Pause / Ende

func _unhandled_input(event: InputEvent) -> void:
	# Zurueck-Taste (Android) und ESC oeffnen die Pause statt die App zu verlassen.
	if event.is_action_pressed("ui_cancel"):
		if _overlay and is_instance_valid(_overlay):
			_close_overlay()
			get_tree().paused = false
			_is_match_active = true
		else:
			_toggle_pause()
		get_viewport().set_input_as_handled()


func _toggle_pause() -> void:
	if _overlay and is_instance_valid(_overlay):
		_close_overlay()
		get_tree().paused = false
		return
	if not _is_match_active:
		return
	get_tree().paused = true
	_show_overlay("PAUSE", [
		{"text": "WEITER", "action": func() -> void:
			_close_overlay()
			get_tree().paused = false},
		{"text": "NEUSTART", "action": func() -> void:
			_close_overlay()
			get_tree().paused = false
			_restart_match()},
		{"text": "HAUPTMENÜ", "action": func() -> void:
			get_tree().paused = false
			_leave_to_menu()},
	])


func _close_overlay() -> void:
	if _overlay and is_instance_valid(_overlay):
		_overlay.queue_free()
	_overlay = null


func _leave_to_menu() -> void:
	GlobalData.reset()
	get_tree().change_scene_to_file("res://scenes/ui/MainMenu.tscn")


func _restart_match() -> void:
	_round = 1
	_p1_wins = 0
	_p2_wins = 0
	_start_match()


## Vollflaechiges Menue ueber der Arena. Laeuft im Pause-Modus weiter, deshalb
## PROCESS_MODE_ALWAYS -- sonst reagieren die Knoepfe nicht.
func _show_overlay(title: String, buttons: Array) -> void:
	_close_overlay()

	var layer: CanvasLayer = CanvasLayer.new()
	layer.name = "OverlayLayer"
	layer.layer = 40
	layer.process_mode = Node.PROCESS_MODE_ALWAYS
	add_child(layer)

	var root: Control = Control.new()
	root.name = "Overlay"
	root.set_anchors_preset(Control.PRESET_FULL_RECT)
	root.process_mode = Node.PROCESS_MODE_ALWAYS
	layer.add_child(root)
	_overlay = layer

	var dim: ColorRect = ColorRect.new()
	dim.set_anchors_preset(Control.PRESET_FULL_RECT)
	dim.color = Color(0.02, 0.01, 0.03, 0.82)
	root.add_child(dim)

	var center: CenterContainer = CenterContainer.new()
	center.set_anchors_preset(Control.PRESET_FULL_RECT)
	root.add_child(center)

	var col: VBoxContainer = VBoxContainer.new()
	col.add_theme_constant_override("separation", 14)
	center.add_child(col)

	var label: Label = Label.new()
	label.text = title
	label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	label.add_theme_font_size_override("font_size", 58)
	label.add_theme_color_override("font_color", Color(0.95, 0.2, 0.18))
	col.add_child(label)

	for entry in buttons:
		var spec: Dictionary = entry as Dictionary
		var b: Button = Button.new()
		b.text = str(spec.get("text", "?"))
		b.custom_minimum_size = Vector2(360, 76)
		b.add_theme_font_size_override("font_size", 30)
		b.process_mode = Node.PROCESS_MODE_ALWAYS
		b.pressed.connect(spec.get("action") as Callable)
		col.add_child(b)
