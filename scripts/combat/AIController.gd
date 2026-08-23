extends Node
class_name AIController

## AI opponent with six difficulty levels, blocking, retreating and combo selection.

enum Difficulty {
	VERY_EASY,
	EASY,
	MEDIUM,
	HARD,
	VERY_HARD,
	BOSS
}

@export var difficulty: Difficulty = Difficulty.MEDIUM
@export var attack_range: float = 2.5
@export var think_interval: float = 0.5
@export var arena_radius: float = 8.0

var fighter: CombatController
var target: CombatController

var _think_timer: float = 0.0
var _attack_cooldown: float = 0.0
var _wander_target: Vector3 = Vector3.ZERO


func _ready() -> void:
	if fighter == null and get_parent() is CombatController:
		fighter = get_parent() as CombatController
	_find_target()
	_wander_target = _get_random_position()


func _process(delta: float) -> void:
	if fighter == null or not is_instance_valid(fighter):
		return
	if target == null or not is_instance_valid(target):
		_find_target()
		return
	_attack_cooldown = maxf(_attack_cooldown - delta, 0.0)
	_think_timer -= delta
	if _think_timer <= 0.0:
		_think_timer = think_interval * (1.0 + randf_range(-0.2, 0.2))
		_think()


func _find_target() -> void:
	var candidates: Array = get_tree().get_nodes_in_group("fighters")
	if candidates.is_empty():
		candidates = get_tree().get_nodes_in_group("fighter")
	for node in candidates:
		if node is CombatController and node != fighter:
			target = node as CombatController
			return


func _think() -> void:
	if fighter == null or target == null:
		return
	var offset: Vector3 = target.global_position - fighter.global_position
	var dist: float = offset.length()
	var dir: Vector3 = offset.normalized() if dist > 0.001 else Vector3.FORWARD
	match difficulty:
		Difficulty.VERY_EASY:
			_easy_think(dist, dir, 0.18, 0.05)
		Difficulty.EASY:
			_easy_think(dist, dir, 0.28, 0.14)
		Difficulty.MEDIUM:
			_medium_think(dist, dir)
		Difficulty.HARD:
			_hard_think(dist, dir)
		Difficulty.VERY_HARD:
			_very_hard_think(dist, dir)
		Difficulty.BOSS:
			_boss_think(dist, dir)


func _easy_think(dist: float, dir: Vector3, attack_chance: float, block_chance: float) -> void:
	if dist < attack_range:
		if _try_attack(attack_chance):
			return
		fighter.is_blocking = randf() < block_chance
		fighter.set_move_input(Vector2.ZERO)
	else:
		fighter.is_blocking = false
		fighter.set_move_input(Vector2(dir.x, dir.z).limit_length(1.0))


func _medium_think(dist: float, dir: Vector3) -> void:
	if dist < attack_range:
		if _try_attack(0.42):
			return
		fighter.is_blocking = randf() < 0.35
		fighter.set_move_input(Vector2(-dir.x, -dir.z).limit_length(0.55))
	else:
		fighter.is_blocking = false
		fighter.set_move_input(Vector2(dir.x, dir.z).limit_length(1.0))


func _hard_think(dist: float, dir: Vector3) -> void:
	if dist < attack_range:
		if _try_attack(0.56):
			return
		fighter.is_blocking = randf() < 0.50
		var strafe: Vector3 = dir.cross(Vector3.UP).normalized() * (1.0 if randf() > 0.5 else -1.0)
		fighter.set_move_input(Vector2(strafe.x, strafe.z).limit_length(0.7))
	else:
		fighter.is_blocking = false
		fighter.set_move_input(Vector2(dir.x, dir.z).limit_length(1.0))


func _very_hard_think(dist: float, dir: Vector3) -> void:
	if dist < attack_range * 1.15:
		if _try_attack(0.72):
			return
		fighter.is_blocking = randf() < 0.65
		var retreat: Vector3 = -dir * 0.35 + dir.cross(Vector3.UP).normalized() * randf_range(-0.5, 0.5)
		fighter.set_move_input(Vector2(retreat.x, retreat.z).limit_length(0.85))
	else:
		fighter.is_blocking = false
		fighter.set_move_input(Vector2(dir.x, dir.z).limit_length(1.0))


func _boss_think(dist: float, dir: Vector3) -> void:
	if dist < attack_range * 1.6:
		if _try_attack(0.88):
			_attack_cooldown = randf_range(0.12, 0.32)
			fighter.is_blocking = false
			return
		fighter.is_blocking = randf() < 0.80
		var circle: Vector3 = dir.cross(Vector3.UP).normalized()
		fighter.set_move_input(Vector2(circle.x, circle.z).limit_length(1.0))
	else:
		fighter.is_blocking = false
		fighter.set_move_input(Vector2(dir.x, dir.z).limit_length(1.0))


func _try_attack(chance: float) -> bool:
	if _attack_cooldown > 0.0 or randf() >= chance:
		return false
	var combo_name: String = _choose_combo_name()
	if combo_name.is_empty():
		return false
	fighter.is_blocking = false
	fighter.set_move_input(Vector2.ZERO)
	fighter.execute_combo(combo_name)
	_attack_cooldown = _cooldown_for_difficulty()
	return true


func _choose_combo_name() -> String:
	if fighter == null or fighter.skill_data == null or fighter.skill_data.combos.is_empty():
		return ""
	var combos: Array[Dictionary] = fighter.skill_data.combos
	if difficulty in [Difficulty.HARD, Difficulty.VERY_HARD, Difficulty.BOSS]:
		var best: Dictionary = combos[0]
		for combo in combos:
			if float(combo.get("damage_multiplier", 1.0)) > float(best.get("damage_multiplier", 1.0)):
				best = combo
		if randf() < 0.55:
			return str(best.get("name", ""))
	return str(combos[randi() % combos.size()].get("name", ""))


func _cooldown_for_difficulty() -> float:
	match difficulty:
		Difficulty.VERY_EASY:
			return randf_range(1.0, 1.8)
		Difficulty.EASY:
			return randf_range(0.8, 1.4)
		Difficulty.MEDIUM:
			return randf_range(0.55, 1.1)
		Difficulty.HARD:
			return randf_range(0.35, 0.85)
		Difficulty.VERY_HARD:
			return randf_range(0.25, 0.65)
		Difficulty.BOSS:
			return randf_range(0.12, 0.45)
	return 0.8


func _get_random_position() -> Vector3:
	return Vector3(randf_range(-arena_radius, arena_radius), 0.0, randf_range(-arena_radius, arena_radius))


func set_difficulty(new_difficulty: Difficulty) -> void:
	difficulty = new_difficulty
