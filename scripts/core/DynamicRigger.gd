extends Node
class_name DynamicRigger

## Dynamic skeleton/mesh rigging with hitbox and VFX binding.

signal rigging_completed()
signal rigging_failed(error: String)

var _target_node: Node3D
var _skill_data: SkillData
var _bone_map: Dictionary = {}
var _hitboxes: Array[Area3D] = []
var _vfx_nodes: Array[Node3D] = []
var _followers: Dictionary = {}
var _skeleton: Skeleton3D = null


func rig_entity(target: Node3D, skill_data: SkillData) -> void:
	cleanup()
	_target_node = target
	_skill_data = skill_data

	if target == null:
		rigging_failed.emit("Target node is null")
		return
	if skill_data == null or not skill_data.is_valid():
		rigging_failed.emit("Invalid skill data")
		return

	_build_bone_map(target)
	if _bone_map.is_empty():
		rigging_failed.emit("No bones or nodes found in target")
		return

	_create_hitboxes()
	_create_vfx_nodes()
	rigging_completed.emit()


func _build_bone_map(node: Node3D) -> void:
	var skeleton: Skeleton3D = _find_skeleton(node)
	if skeleton:
		_skeleton = skeleton
		for bone_idx in range(skeleton.get_bone_count()):
			var bone_name: String = skeleton.get_bone_name(bone_idx)
			var key: String = _classify_name(bone_name)
			if not key.is_empty() and not _bone_map.has(key):
				_bone_map[key] = {"type": "bone", "bone_idx": bone_idx, "bone": bone_name, "node": skeleton}

	for mesh in _find_mesh_nodes(node):
		var key: String = _classify_name(mesh.name)
		if not key.is_empty() and not _bone_map.has(key):
			_bone_map[key] = {"type": "mesh", "node": mesh}

	if not _bone_map.has("Root"):
		_bone_map["Root"] = {"type": "transform", "node": _target_node}


func _classify_name(name: String) -> String:
	var lower_name: String = name.to_lower().replace(".", "_").replace("-", "_")
	if "head" in lower_name:
		return "Head"
	if "hand" in lower_name or "wrist" in lower_name or "fist" in lower_name:
		if "right" in lower_name or lower_name.ends_with("_r") or lower_name.ends_with(".r") or " r " in lower_name:
			return "Hand_R"
		if "left" in lower_name or lower_name.ends_with("_l") or lower_name.ends_with(".l") or " l " in lower_name:
			return "Hand_L"
	if "root" in lower_name or "hips" in lower_name or "pelvis" in lower_name:
		return "Root"
	return ""


func _find_skeleton(node: Node3D) -> Skeleton3D:
	if node is Skeleton3D:
		return node as Skeleton3D
	for child in node.get_children():
		if child is Node3D:
			var result: Skeleton3D = _find_skeleton(child as Node3D)
			if result:
				return result
	return null


func _find_mesh_nodes(node: Node3D) -> Array[MeshInstance3D]:
	var meshes: Array[MeshInstance3D] = []
	if node is MeshInstance3D:
		meshes.append(node as MeshInstance3D)
	for child in node.get_children():
		if child is Node3D:
			meshes.append_array(_find_mesh_nodes(child as Node3D))
	return meshes


func _create_hitboxes() -> void:
	for combo in _skill_data.combos:
		var attach_bone: String = _normalize_attach_bone(str(combo.get("attach_bone", "Root")))
		var target_node: Node3D = _get_target_node(attach_bone)
		if target_node == null:
			continue

		var area: Area3D = Area3D.new()
		area.name = str(combo.get("name", "Hitbox")) + "_Hitbox"
		area.monitoring = false
		area.monitorable = true
		area.collision_layer = 2
		area.collision_mask = 3

		var collision_shape: CollisionShape3D = CollisionShape3D.new()
		var sphere_shape: SphereShape3D = SphereShape3D.new()
		sphere_shape.radius = float(combo.get("hitbox_radius", 1.5))
		collision_shape.shape = sphere_shape
		area.add_child(collision_shape)

		target_node.add_child(area)
		area.position = Vector3.ZERO
		_hitboxes.append(area)


func _create_vfx_nodes() -> void:
	for combo in _skill_data.combos:
		var attach_bone: String = _normalize_attach_bone(str(combo.get("attach_bone", "Root")))
		var target_node: Node3D = _get_target_node(attach_bone)
		if target_node == null:
			continue

		var particles: GPUParticles3D = GPUParticles3D.new()
		particles.name = str(combo.get("name", "VFX")) + "_VFX"

		var process_material: ParticleProcessMaterial = ParticleProcessMaterial.new()
		process_material.direction = Vector3(0, 1, 0)
		process_material.spread = 45.0
		process_material.gravity = Vector3(0, -1.5, 0)
		process_material.initial_velocity_min = 0.5
		process_material.initial_velocity_max = 1.5
		process_material.scale_min = 0.1
		process_material.scale_max = 0.3
		process_material.color = _parse_color(str(combo.get("vfx_color", "#FF0000")))

		particles.process_material = process_material
		particles.amount = 30
		particles.lifetime = 1.0
		particles.explosiveness = 0.5
		particles.one_shot = true
		particles.emitting = false
		particles.visibility_aabb = AABB(Vector3(-2, -2, -2), Vector3(4, 4, 4))

		target_node.add_child(particles)
		particles.position = Vector3.ZERO
		_vfx_nodes.append(particles)


func _normalize_attach_bone(name: String) -> String:
	var lower_name: String = name.to_lower()
	if lower_name in ["right", "righthand", "right_hand", "hand_r", "hand.r"]:
		return "Hand_R"
	if lower_name in ["left", "lefthand", "left_hand", "hand_l", "hand.l"]:
		return "Hand_L"
	if lower_name == "head":
		return "Head"
	return "Root" if not _bone_map.has(name) else name


func _get_target_node(bone_name: String) -> Node3D:
	if _bone_map.has(bone_name):
		var mapping: Dictionary = _bone_map[bone_name]
		match str(mapping.get("type", "")):
			"bone":
				return _get_or_create_bone_follower(mapping["node"] as Skeleton3D, int(mapping["bone_idx"]))
			"mesh", "transform":
				return mapping["node"] as Node3D
	return _target_node


func _get_or_create_bone_follower(skeleton: Skeleton3D, bone_idx: int) -> Node3D:
	var bone_name: String = skeleton.get_bone_name(bone_idx)
	if _followers.has(bone_name) and is_instance_valid(_followers[bone_name]):
		return _followers[bone_name]

	var follower: BoneAttachment3D = BoneAttachment3D.new()
	follower.name = "BoneFollower_" + bone_name
	follower.bone_name = bone_name
	skeleton.add_child(follower)
	_followers[bone_name] = follower
	return follower


func _parse_color(hex: String) -> Color:
	var value: String = hex.strip_edges()
	if not value.begins_with("#"):
		value = "#" + value
	if Color.html_is_valid(value):
		return Color.html(value)
	return Color(1, 0, 0, 1)


func get_hitboxes() -> Array[Area3D]:
	return _hitboxes


func get_vfx_nodes() -> Array[Node3D]:
	return _vfx_nodes


func set_hitbox_visibility(enabled: bool) -> void:
	for area in _hitboxes:
		if is_instance_valid(area):
			area.visible = enabled
			for child in area.get_children():
				if child is CollisionShape3D:
					(child as CollisionShape3D).disabled = not enabled


func set_hitbox_monitoring(hitbox_name: String, enabled: bool) -> void:
	for area in _hitboxes:
		if is_instance_valid(area) and area.name == hitbox_name + "_Hitbox":
			area.monitoring = enabled


func emit_vfx(hitbox_name: String) -> void:
	for vfx in _vfx_nodes:
		if is_instance_valid(vfx) and vfx.name == hitbox_name + "_VFX" and vfx is GPUParticles3D:
			var particles: GPUParticles3D = vfx as GPUParticles3D
			particles.restart()
			particles.emitting = true
			break


func cleanup() -> void:
	for area in _hitboxes:
		if is_instance_valid(area):
			area.queue_free()
	_hitboxes.clear()

	for vfx in _vfx_nodes:
		if is_instance_valid(vfx):
			vfx.queue_free()
	_vfx_nodes.clear()

	for follower in _followers.values():
		if is_instance_valid(follower):
			(follower as Node).queue_free()
	_followers.clear()
	_bone_map.clear()
	_skeleton = null
