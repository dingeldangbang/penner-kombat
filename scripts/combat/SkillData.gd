extends Resource
class_name SkillData

## Combat skill/attribute data container with JSON parsing.

@export var move_speed: float = 5.0
@export var attack_power: float = 15.0
@export var combos: Array[Dictionary] = []


func load_from_json(dict: Dictionary) -> void:
	if dict.is_empty():
		push_warning("Empty dictionary provided to SkillData.load_from_json")
		return

	if dict.has("move_speed"):
		move_speed = clampf(float(dict["move_speed"]), 0.1, 50.0)
	if dict.has("attack_power"):
		attack_power = clampf(float(dict["attack_power"]), 0.1, 500.0)

	if dict.has("combos") and dict["combos"] is Array:
		combos.clear()
		for combo_dict in dict["combos"]:
			if combo_dict is Dictionary:
				combos.append(_sanitize_combo(combo_dict as Dictionary))


func _sanitize_combo(combo: Dictionary) -> Dictionary:
	return {
		"name": str(combo.get("name", "Unnamed")),
		"delay": clampf(float(combo.get("delay", 0.2)), 0.05, 5.0),
		"damage_multiplier": clampf(float(combo.get("damage_multiplier", 1.0)), 0.01, 20.0),
		"hitbox_radius": clampf(float(combo.get("hitbox_radius", 1.0)), 0.05, 20.0),
		"vfx_color": str(combo.get("vfx_color", "#FF0000")),
		"attach_bone": str(combo.get("attach_bone", "Root"))
	}


func get_combo(name: String) -> Dictionary:
	for combo in combos:
		if combo.get("name", "") == name:
			return combo
	return {}


func get_combo_names() -> Array[String]:
	var names: Array[String] = []
	for combo in combos:
		names.append(str(combo.get("name", "Unnamed")))
	return names


func duplicate_data() -> SkillData:
	var copy: SkillData = SkillData.new()
	copy.move_speed = move_speed
	copy.attack_power = attack_power
	copy.combos = combos.duplicate(true)
	return copy


func is_valid() -> bool:
	return move_speed > 0.0 and attack_power > 0.0 and not combos.is_empty()


func get_max_damage_multiplier() -> float:
	var max_dmg: float = 0.0
	for combo in combos:
		max_dmg = maxf(max_dmg, float(combo.get("damage_multiplier", 0.0)))
	return max_dmg


func get_combo_delays() -> Array[float]:
	var delays: Array[float] = []
	for combo in combos:
		delays.append(float(combo.get("delay", 0.2)))
	return delays
