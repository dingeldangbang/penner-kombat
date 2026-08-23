extends Node
class_name WeaponSystem

## Runtime weapon inventory/equip system that modifies combo damage and can spawn simple weapon meshes.

signal weapon_equipped(weapon_id: String)
signal weapon_unequipped(weapon_id: String)
signal durability_changed(weapon_id: String, durability: float)

@export var owner_fighter: CombatController
@export var attach_path: NodePath

var weapons: Dictionary = {
	"screwdriver": {"name": "Schraubenzieher", "damage_bonus": 1.15, "range_bonus": 0.15, "durability": 100.0, "color": Color(0.7, 0.7, 0.85)},
	"wrench": {"name": "Maulschlüssel", "damage_bonus": 1.3, "range_bonus": 0.25, "durability": 100.0, "color": Color(0.55, 0.55, 0.6)},
	"bottle": {"name": "Bierflasche", "damage_bonus": 1.22, "range_bonus": 0.2, "durability": 45.0, "color": Color(0.1, 0.6, 0.25)},
	"pipe_wrench": {"name": "Rohrzange", "damage_bonus": 1.45, "range_bonus": 0.35, "durability": 120.0, "color": Color(0.45, 0.45, 0.5)}
}

var equipped_weapon_id: String = ""
var _weapon_node: MeshInstance3D


func equip(weapon_id: String) -> bool:
	if not weapons.has(weapon_id):
		return false
	unequip()
	equipped_weapon_id = weapon_id
	_spawn_visual(weapons[weapon_id])
	weapon_equipped.emit(weapon_id)
	return true


func unequip() -> void:
	if equipped_weapon_id.is_empty():
		return
	var old: String = equipped_weapon_id
	equipped_weapon_id = ""
	if _weapon_node and is_instance_valid(_weapon_node):
		_weapon_node.queue_free()
	_weapon_node = null
	weapon_unequipped.emit(old)


func apply_damage_bonus(base_damage: float) -> float:
	if equipped_weapon_id.is_empty():
		return base_damage
	return base_damage * float(weapons[equipped_weapon_id].get("damage_bonus", 1.0))


func apply_range_bonus(base_radius: float) -> float:
	if equipped_weapon_id.is_empty():
		return base_radius
	return base_radius + float(weapons[equipped_weapon_id].get("range_bonus", 0.0))


func consume_durability(amount: float) -> void:
	if equipped_weapon_id.is_empty():
		return
	var data: Dictionary = weapons[equipped_weapon_id]
	data["durability"] = maxf(float(data.get("durability", 0.0)) - amount, 0.0)
	weapons[equipped_weapon_id] = data
	durability_changed.emit(equipped_weapon_id, float(data["durability"]))
	if float(data["durability"]) <= 0.0:
		unequip()


func _spawn_visual(data: Dictionary) -> void:
	var attach: Node3D = get_node_or_null(attach_path) as Node3D
	if attach == null and owner_fighter:
		attach = owner_fighter
	if attach == null:
		return
	_weapon_node = MeshInstance3D.new()
	_weapon_node.name = "EquippedWeapon"
	var box: BoxMesh = BoxMesh.new()
	box.size = Vector3(0.12, 0.65, 0.12)
	_weapon_node.mesh = box
	_weapon_node.position = Vector3(0.35, 1.0, -0.25)
	_weapon_node.rotation_degrees = Vector3(35, 0, 20)
	var mat: StandardMaterial3D = StandardMaterial3D.new()
	mat.albedo_color = data.get("color", Color.GRAY)
	mat.metallic = 0.4
	mat.roughness = 0.45
	_weapon_node.material_override = mat
	attach.add_child(_weapon_node)
