extends Node
class_name GlbImporter

## GLB/GLTF runtime importer with scale normalization and collision generation.
## Supports res://, user://, and any platform-accessible absolute path.

signal import_completed(node: Node3D)
signal import_failed(error: String)
signal import_log(message: String)


## Load a .glb or .gltf file from any accessible path.
## Returns the imported Node3D or null on failure.
func load_glb(file_path: String) -> Node3D:
	if file_path.strip_edges().is_empty():
		_fail("File path is empty")
		return null

	_log("Loading file: " + file_path)
	if not FileAccess.file_exists(file_path):
		_fail("File not found: " + file_path)
		return null

	var extension: String = file_path.get_extension().to_lower()
	if extension != "glb" and extension != "gltf":
		_fail("Unsupported file type: ." + extension)
		return null

	var gltf_document: GLTFDocument = GLTFDocument.new()
	var gltf_state: GLTFState = GLTFState.new()
	var err: Error = gltf_document.append_from_file(file_path, gltf_state)
	if err != OK:
		_fail("Failed to load GLB/GLTF: " + error_string(err))
		return null

	_log("Parsed GLB/GLTF state successfully")
	var generated: Node = gltf_document.generate_scene(gltf_state)
	if generated == null or not generated is Node3D:
		_fail("Failed to generate a 3D scene from GLB/GLTF")
		return null

	var root_node: Node3D = generated as Node3D
	root_node.name = file_path.get_file().get_basename()
	_log("Generated " + str(_get_node_count(root_node)) + " nodes")
	_log("Parsed " + str(get_mesh_count(root_node)) + " meshes")
	_normalize_scale(root_node)
	var shape_count: int = _generate_collisions(root_node)
	_log("Added " + str(shape_count) + " collision shapes")

	_log("Import completed: " + root_node.name)
	import_completed.emit(root_node)
	return root_node


func _fail(message: String) -> void:
	push_error(message)
	import_log.emit("ERROR: " + message)
	import_failed.emit(message)


func _log(message: String) -> void:
	print("GlbImporter: " + message)
	import_log.emit(message)


## Normalize imported mesh scale to approximately 2.0 world units high.
func _normalize_scale(node: Node3D) -> void:
	var aabb: AABB = _compute_aabb(node)
	if aabb.size.length() < 0.01:
		return

	var height: float = max(aabb.size.y, 0.001)
	var scale_factor: float = 2.0 / height
	if is_finite(scale_factor) and scale_factor > 0.0:
		_log("Scale factor: " + str(scale_factor))
		node.scale *= scale_factor
		node.position.y -= aabb.position.y * scale_factor


func _compute_aabb(node: Node3D) -> AABB:
	var state: Dictionary = {"aabb": AABB(), "has": false}
	_collect_aabb_state(node, Transform3D.IDENTITY, state)
	return state["aabb"] if state["has"] else AABB()


func _collect_aabb_state(node: Node3D, parent_transform: Transform3D, state: Dictionary) -> void:
	var local_transform: Transform3D = parent_transform * node.transform
	if node is MeshInstance3D:
		var mesh_instance: MeshInstance3D = node as MeshInstance3D
		if mesh_instance.mesh:
			var transformed: AABB = local_transform * mesh_instance.mesh.get_aabb()
			if state["has"]:
				state["aabb"] = (state["aabb"] as AABB).merge(transformed)
			else:
				state["aabb"] = transformed
				state["has"] = true

	for child in node.get_children():
		if child is Node3D:
			_collect_aabb_state(child as Node3D, local_transform, state)


## Generate StaticBody3D/ConvexPolygonShape3D collision for all meshes.
func _generate_collisions(node: Node3D) -> int:
	var created_count: int = 0
	if node is MeshInstance3D:
		var mesh_instance: MeshInstance3D = node as MeshInstance3D
		var mesh: Mesh = mesh_instance.mesh
		if mesh:
			var faces: PackedVector3Array = mesh.get_faces()
			if faces.size() >= 3:
				var body: StaticBody3D = StaticBody3D.new()
				body.name = mesh_instance.name + "_CollisionBody"

				var collision_shape: CollisionShape3D = CollisionShape3D.new()
				collision_shape.name = mesh_instance.name + "_Collision"
				var convex_shape: ConvexPolygonShape3D = ConvexPolygonShape3D.new()
				convex_shape.points = _extract_vertices(faces)
				collision_shape.shape = convex_shape
				body.add_child(collision_shape)

				var parent: Node = mesh_instance.get_parent()
				if parent:
					parent.add_child(body)
					body.transform = mesh_instance.transform
					created_count += 1

	for child in node.get_children():
		if child is Node3D and not child.name.ends_with("_CollisionBody"):
			created_count += _generate_collisions(child as Node3D)
	return created_count


func _extract_vertices(faces: PackedVector3Array) -> PackedVector3Array:
	var vertices: PackedVector3Array = PackedVector3Array()
	var seen: Dictionary = {}
	for v in faces:
		var key: String = "%.5f,%.5f,%.5f" % [v.x, v.y, v.z]
		if not seen.has(key):
			seen[key] = true
			vertices.append(v)
	return vertices


func _get_node_count(node: Node) -> int:
	var count: int = 1
	for child in node.get_children():
		count += _get_node_count(child)
	return count


## Get mesh node count for diagnostics.
func get_mesh_count(node: Node3D) -> int:
	var count: int = 1 if node is MeshInstance3D else 0
	for child in node.get_children():
		if child is Node3D:
			count += get_mesh_count(child as Node3D)
	return count


## Get all mesh nodes.
func get_all_meshes(node: Node3D) -> Array[MeshInstance3D]:
	var meshes: Array[MeshInstance3D] = []
	if node is MeshInstance3D:
		meshes.append(node as MeshInstance3D)
	for child in node.get_children():
		if child is Node3D:
			meshes.append_array(get_all_meshes(child as Node3D))
	return meshes
