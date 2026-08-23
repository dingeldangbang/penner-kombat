extends Node

## Scene-transfer storage for editor-to-arena handoff.

var profile: SkillData = null
var entity: Node3D = null
var source_file_path: String = ""
var enemy_profile: SkillData = null
var enemy_entity: Node3D = null
var enemy_source_file_path: String = ""
var arena_type: String = "default"
var is_multiplayer: bool = false
var current_profile_name: String = ""

var player_stats: Dictionary = {
	"wins": 0,
	"losses": 0,
	"total_damage_dealt": 0.0,
	"total_combos": 0
}


func set_player_data(new_profile: SkillData, new_entity: Node3D, file_path: String = "", profile_name: String = "") -> void:
	profile = new_profile.duplicate_data() if new_profile else null
	entity = new_entity.duplicate() as Node3D if new_entity else null
	source_file_path = file_path
	current_profile_name = profile_name


func set_enemy_data(new_profile: SkillData, new_entity: Node3D, file_path: String = "") -> void:
	enemy_profile = new_profile.duplicate_data() if new_profile else null
	enemy_entity = new_entity.duplicate() as Node3D if new_entity else null
	enemy_source_file_path = file_path


func reset() -> void:
	profile = null
	entity = null
	source_file_path = ""
	enemy_profile = null
	enemy_entity = null
	enemy_source_file_path = ""
	arena_type = "default"
	is_multiplayer = false
	current_profile_name = ""


func add_damage_dealt(amount: float) -> void:
	player_stats["total_damage_dealt"] = float(player_stats.get("total_damage_dealt", 0.0)) + amount


func add_combo() -> void:
	player_stats["total_combos"] = int(player_stats.get("total_combos", 0)) + 1
