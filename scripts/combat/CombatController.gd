extends CharacterBody3D
class_name CombatController

## 3D spatial combat controller with finite state machine and dynamic combos.

enum State {
	IDLE,
	RUN,
	ATTACK,
	STUN
}

signal attack_executed(damage: float, area: Area3D)
signal state_changed(old_state: int, new_state: int)
signal health_changed(current_health: float, max_health: float)
signal combo_executed(combo_name: String, damage: float)

@export var max_health: float = 100.0
@export var rotation_speed: float = 10.0
@export var ground_acceleration: float = 15.0
@export var friction: float = 8.0
@export var gravity: float = 24.0

@export var skill_data: SkillData
@export var rigger: DynamicRigger

var current_state: int = State.IDLE
var current_health: float = 100.0
var current_combo_index: int = 0
var is_blocking: bool = false
var is_attacking: bool = false
var attack_timer: float = 0.0
var stun_timer: float = 0.0
var move_input: Vector2 = Vector2.ZERO
var velocity_3d: Vector3 = Vector3.ZERO
var facing_direction: Vector3 = Vector3.FORWARD

var _combo_queue: Array[Dictionary] = []
var _combo_index: int = 0
var _active_hitbox_name: String = ""
var _hit_targets_this_attack: Dictionary = {}


func _ready() -> void:
	add_to_group("fighter")
	add_to_group("fighters")
	current_health = max_health
	if skill_data == null:
		push_warning("CombatController: skill_data is null; defaults will be used")
	else:
		_setup_from_skill_data()

	if rigger == null:
		push_warning("CombatController: rigger is null; hitboxes/VFX will be skipped")


func _setup_from_skill_data() -> void:
	_combo_queue = skill_data.combos.duplicate(true)


func _physics_process(delta: float) -> void:
	_handle_state(delta)
	_handle_movement(delta)
	_handle_combos(delta)
	move_and_slide()


func _unhandled_input(event: InputEvent) -> void:
	if event.is_action_pressed("ui_accept") or event.is_action_pressed("ui_select"):
		execute_next_combo()


func _handle_state(delta: float) -> void:
	match current_state:
		State.IDLE:
			if move_input.length() > 0.1 and not is_blocking:
				_set_state(State.RUN)
		State.RUN:
			if move_input.length() < 0.1:
				_set_state(State.IDLE)
		State.ATTACK:
			attack_timer -= delta
			if attack_timer <= 0.0:
				_finish_attack()
		State.STUN:
			stun_timer -= delta
			if stun_timer <= 0.0:
				_set_state(State.IDLE)


func _handle_movement(delta: float) -> void:
	if not is_on_floor():
		velocity_3d.y -= gravity * delta
	else:
		velocity_3d.y = 0.0

	if current_state == State.STUN:
		velocity_3d.x = lerpf(velocity_3d.x, 0.0, clampf(friction * delta, 0.0, 1.0))
		velocity_3d.z = lerpf(velocity_3d.z, 0.0, clampf(friction * delta, 0.0, 1.0))
		velocity = velocity_3d
		return

	var move_speed: float = skill_data.move_speed if skill_data else 5.0
	var speed_factor: float = 0.35 if is_blocking else 1.0
	var target_velocity: Vector3 = Vector3.ZERO
	if move_input.length() > 0.1:
		var input_direction: Vector3 = Vector3(move_input.x, 0.0, move_input.y).normalized()
		var camera: Camera3D = get_viewport().get_camera_3d()
		var camera_basis: Basis = camera.global_transform.basis if camera else Basis.IDENTITY
		var forward: Vector3 = -camera_basis.z
		var right: Vector3 = camera_basis.x
		forward.y = 0.0
		right.y = 0.0
		forward = forward.normalized()
		right = right.normalized()
		target_velocity = (forward * input_direction.z + right * input_direction.x).normalized() * move_speed * speed_factor
		facing_direction = target_velocity.normalized()

	var blend: float = clampf((ground_acceleration if target_velocity.length() > 0.1 else friction) * delta, 0.0, 1.0)
	velocity_3d.x = lerpf(velocity_3d.x, target_velocity.x, blend)
	velocity_3d.z = lerpf(velocity_3d.z, target_velocity.z, blend)

	if Vector2(velocity_3d.x, velocity_3d.z).length() > 0.5:
		var target_yaw: float = atan2(-velocity_3d.x, -velocity_3d.z)
		rotation.y = lerp_angle(rotation.y, target_yaw, clampf(rotation_speed * delta, 0.0, 1.0))

	velocity = velocity_3d


func _handle_combos(_delta: float) -> void:
	if current_state == State.IDLE and _combo_index >= _combo_queue.size():
		_combo_index = 0


func execute_combo(combo_name: String) -> void:
	if current_state == State.STUN or is_attacking:
		return
	var combo: Dictionary = _find_combo(combo_name)
	if not combo.is_empty():
		_start_attack(combo)


func execute_next_combo() -> void:
	if current_state == State.STUN or is_attacking or _combo_queue.is_empty():
		return
	if _combo_index >= _combo_queue.size():
		_combo_index = 0
	var combo: Dictionary = _combo_queue[_combo_index]
	_combo_index = (_combo_index + 1) % _combo_queue.size()
	if not combo.is_empty():
		_start_attack(combo)


func _start_attack(combo: Dictionary) -> void:
	is_attacking = true
	attack_timer = float(combo.get("delay", 0.2))
	var combo_name: String = str(combo.get("name", "Hitbox"))
	var damage: float = (skill_data.attack_power if skill_data else 15.0) * float(combo.get("damage_multiplier", 1.0))

	_active_hitbox_name = combo_name
	_hit_targets_this_attack.clear()
	_activate_hitbox(combo_name, damage)
	if rigger:
		rigger.emit_vfx(combo_name)

	_set_state(State.ATTACK)
	attack_executed.emit(damage, null)
	combo_executed.emit(combo_name, damage)


func _finish_attack() -> void:
	if rigger and not _active_hitbox_name.is_empty():
		rigger.set_hitbox_monitoring(_active_hitbox_name, false)
	_active_hitbox_name = ""
	_hit_targets_this_attack.clear()
	is_attacking = false
	_set_state(State.RUN if move_input.length() > 0.1 else State.IDLE)


func _activate_hitbox(hitbox_name: String, damage: float) -> void:
	if not rigger:
		return
	for area in rigger.get_hitboxes():
		if area.name == hitbox_name + "_Hitbox":
			area.set_meta("damage", damage)
			var callback: Callable = Callable(self, "_on_hitbox_entered").bind(area)
			if not area.area_entered.is_connected(callback):
				area.area_entered.connect(callback)
			var body_callback: Callable = Callable(self, "_on_hitbox_body_entered").bind(area)
			if not area.body_entered.is_connected(body_callback):
				area.body_entered.connect(body_callback)
			area.monitoring = true
			area.monitorable = true
		else:
			area.monitoring = false


func _on_hitbox_entered(other_area: Area3D, source_hitbox: Area3D) -> void:
	_apply_hit_to_node(other_area, source_hitbox)


func _on_hitbox_body_entered(body: Node3D, source_hitbox: Area3D) -> void:
	_apply_hit_to_node(body, source_hitbox)


func _apply_hit_to_node(node: Node, source_hitbox: Area3D) -> void:
	var damage: float = float(source_hitbox.get_meta("damage", 0.0))
	var parent: Node = node
	while parent:
		if parent is CombatController and parent != self:
			var id: int = parent.get_instance_id()
			if _hit_targets_this_attack.has(id):
				return
			_hit_targets_this_attack[id] = true
			var target: CombatController = parent as CombatController
			target.take_damage(damage)
			target.apply_knockback((target.global_position - global_position).normalized() * 3.0 + Vector3.UP * 1.5)
			_spawn_impact_effects(source_hitbox.global_position, damage, target.global_position - global_position)
			return
		parent = parent.get_parent()


func _spawn_impact_effects(world_position: Vector3, damage: float, direction: Vector3) -> void:
	var parent: Node = get_tree().current_scene if get_tree().current_scene else get_tree().root
	var hit_spark: HitSpark = HitSpark.new()
	parent.add_child(hit_spark)
	hit_spark.global_position = world_position
	hit_spark.initialize(damage, direction.normalized(), damage >= 35.0)

	var blood: BloodSplat = BloodSplat.new()
	parent.add_child(blood)
	blood.global_position = world_position
	blood.initialize(damage, direction.normalized() + Vector3.UP * 0.35)

	var shockwave: Shockwave = Shockwave.new()
	parent.add_child(shockwave)
	shockwave.global_position = world_position
	shockwave.end_size = 2.0 + clampf(damage / 60.0, 0.0, 2.0)
	shockwave.initialize(direction)

	if damage >= 35.0:
		var crit: CritGold = CritGold.new()
		parent.add_child(crit)
		crit.initialize(world_position + Vector3.UP * 0.4)
	ScreenShake.shake(0.08 + damage / 500.0, clampf(damage / 120.0, 0.08, 0.55))


func take_damage(damage: float) -> void:
	if is_blocking:
		damage *= 0.2
	current_health = maxf(current_health - damage, 0.0)
	health_changed.emit(current_health, max_health)
	if current_health <= 0.0:
		_die()
	else:
		stun_timer = 0.3
		_set_state(State.STUN)


func heal(amount: float) -> void:
	current_health = minf(current_health + amount, max_health)
	health_changed.emit(current_health, max_health)
	var heal_effect: HealingEffect = HealingEffect.new()
	var parent: Node = get_tree().current_scene if get_tree().current_scene else get_tree().root
	parent.add_child(heal_effect)
	heal_effect.global_position = global_position + Vector3.UP
	heal_effect.initialize(amount)


func stun(duration: float) -> void:
	stun_timer = maxf(stun_timer, duration)
	_set_state(State.STUN)


func apply_knockback(force: Vector3) -> void:
	velocity_3d += force
	velocity = velocity_3d


func jump() -> void:
	if is_on_floor() and current_state != State.STUN:
		velocity_3d.y = 8.0
		velocity.y = velocity_3d.y


func _die() -> void:
	queue_free()


func set_move_input(input: Vector2) -> void:
	move_input = input.limit_length(1.0)


func _set_state(new_state: int) -> void:
	if new_state == current_state:
		return
	var old_state: int = current_state
	current_state = new_state
	state_changed.emit(old_state, new_state)


func _find_combo(name: String) -> Dictionary:
	for combo in _combo_queue:
		if combo.get("name", "") == name:
			return combo
	return {}


func reset_combo_queue() -> void:
	_combo_index = 0


func get_state_string() -> String:
	return State.keys()[current_state]


func get_health_percentage() -> float:
	return current_health / max_health if max_health > 0.0 else 0.0
