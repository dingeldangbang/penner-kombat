extends Node
class_name TrophyManager

## Persistent trophy manager with 56 predefined trophies and progress tracking.

signal trophy_unlocked(trophy_id: String, trophy_name: String, tier: String)
signal trophy_progress(trophy_id: String, current: int, target: int)

@export var save_path: String = "user://trophies.json"

var trophies: Dictionary = {}


func _ready() -> void:
	_define_trophies()
	load_state()


func _define_trophies() -> void:
	if not trophies.is_empty():
		return
	_add("first_blood", "Erster Tropfen", "bronze", 1)
	_add("first_win", "Erster Sieg", "bronze", 1)
	_add("combo_5", "Fünferkette", "bronze", 5)
	_add("combo_10", "Zehnerkette", "silver", 10)
	_add("damage_1000", "Aua-Abo", "silver", 1000)
	_add("boss_win", "Boss der Platte", "gold", 1)
	_add("all_done", "Penner Platin", "platinum", 1)
	var bronze_needed: int = 35 - _count_tier("bronze")
	for i in range(bronze_needed):
		_add("bronze_%02d" % i, "Bronze Pfand %02d" % (i + 1), "bronze", 1)
	var silver_needed: int = 15 - _count_tier("silver")
	for i in range(silver_needed):
		_add("silver_%02d" % i, "Silber Dose %02d" % (i + 1), "silver", 5)
	var gold_needed: int = 5 - _count_tier("gold")
	for i in range(gold_needed):
		_add("gold_%02d" % i, "Goldener Eimer %02d" % (i + 1), "gold", 10)


func _add(id: String, name: String, tier: String, target: int) -> void:
	trophies[id] = {"name": name, "tier": tier, "target": target, "progress": 0, "unlocked": false}


func _count_tier(tier: String) -> int:
	var count: int = 0
	for trophy in trophies.values():
		if trophy.get("tier", "") == tier:
			count += 1
	return count


func add_progress(trophy_id: String, amount: int = 1) -> void:
	if not trophies.has(trophy_id):
		return
	var trophy: Dictionary = trophies[trophy_id]
	if bool(trophy.get("unlocked", false)):
		return
	trophy["progress"] = mini(int(trophy.get("progress", 0)) + amount, int(trophy.get("target", 1)))
	trophies[trophy_id] = trophy
	trophy_progress.emit(trophy_id, int(trophy["progress"]), int(trophy["target"]))
	if int(trophy["progress"]) >= int(trophy["target"]):
		unlock(trophy_id)
	else:
		save_state()


func unlock(trophy_id: String) -> void:
	if not trophies.has(trophy_id):
		return
	var trophy: Dictionary = trophies[trophy_id]
	if bool(trophy.get("unlocked", false)):
		return
	trophy["unlocked"] = true
	trophy["progress"] = int(trophy.get("target", 1))
	trophies[trophy_id] = trophy
	trophy_unlocked.emit(trophy_id, str(trophy.get("name", trophy_id)), str(trophy.get("tier", "bronze")))
	_check_platinum()
	save_state()


func _check_platinum() -> void:
	if not trophies.has("all_done"):
		return
	for id in trophies.keys():
		if id != "all_done" and not bool(trophies[id].get("unlocked", false)):
			return
	var platinum: Dictionary = trophies["all_done"]
	platinum["unlocked"] = true
	platinum["progress"] = 1
	trophies["all_done"] = platinum


func save_state() -> void:
	var file: FileAccess = FileAccess.open(save_path, FileAccess.WRITE)
	if file:
		file.store_string(JSON.stringify(trophies, "\t"))


func load_state() -> void:
	if not FileAccess.file_exists(save_path):
		return
	var parsed: Variant = JSON.parse_string(FileAccess.get_file_as_string(save_path))
	if parsed is Dictionary:
		for id in (parsed as Dictionary).keys():
			if trophies.has(id):
				trophies[id] = (parsed as Dictionary)[id]
