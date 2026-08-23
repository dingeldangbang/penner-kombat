extends Node
class_name LodManager

## Mobile-friendly visibility/LOD manager for MeshInstance3D collections.

@export var distances: Array[float] = [8.0, 18.0, 35.0]
@export var camera_node: Node3D
@export var update_interval: float = 0.25
@export var far_visibility_distance: float = 50.0

var _meshes: Array[MeshInstance3D] = []
var _timer: float = 0.0


func register_mesh(mesh: MeshInstance3D) -> void:
	if mesh and not _meshes.has(mesh):
		_meshes.append(mesh)


func register_tree(root: Node) -> void:
	if root is MeshInstance3D:
		register_mesh(root as MeshInstance3D)
	for child in root.get_children():
		register_tree(child)


func unregister_mesh(mesh: MeshInstance3D) -> void:
	_meshes.erase(mesh)


func clear() -> void:
	_meshes.clear()


func _process(delta: float) -> void:
	_timer -= delta
	if _timer > 0.0:
		return
	_timer = update_interval
	_update_lod()


func _update_lod() -> void:
	var camera: Node3D = camera_node
	if camera == null and get_viewport():
		camera = get_viewport().get_camera_3d()
	if camera == null:
		return

	var camera_pos: Vector3 = camera.global_position
	for mesh in _meshes:
		if not is_instance_valid(mesh):
			continue
		var distance: float = mesh.global_position.distance_to(camera_pos)
		mesh.visible = distance <= far_visibility_distance
		var shadow_distance: float = distances[min(1, distances.size() - 1)] if not distances.is_empty() else far_visibility_distance
		mesh.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_ON if distance <= shadow_distance else GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
		mesh.extra_cull_margin = _get_cull_margin(distance)


func _get_cull_margin(distance: float) -> float:
	if distances.is_empty():
		return 0.0
	if distance < distances[0]:
		return 1.0
	if distances.size() > 1 and distance < distances[1]:
		return 0.5
	return 0.0
