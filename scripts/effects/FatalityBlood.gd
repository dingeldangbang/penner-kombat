extends GPUParticles3D
class_name FatalityBlood

## Xtreme blood fountain for fatality impacts.

@export var auto_free_delay: float = 3.4


func _ready() -> void:
	one_shot = true
	explosiveness = 0.55
	amount = 140
	lifetime = 2.6
	visibility_aabb = AABB(Vector3(-6, -4, -6), Vector3(12, 12, 12))
	if process_material == null:
		process_material = _create_material()
	if draw_pass_1 == null:
		draw_pass_1 = _create_mesh()


func initialize(direction: Vector3 = Vector3.UP) -> void:
	var material: ParticleProcessMaterial = process_material as ParticleProcessMaterial
	if material:
		material.direction = direction.normalized() if direction.length() > 0.001 else Vector3.UP
	restart()
	emitting = true
	_auto_free()


func _create_material() -> ParticleProcessMaterial:
	var material: ParticleProcessMaterial = ParticleProcessMaterial.new()
	material.color = Color(0.545, 0.0, 0.0, 1.0)
	material.direction = Vector3.UP
	material.spread = 80.0
	material.gravity = Vector3(0.0, -8.0, 0.0)
	material.initial_velocity_min = 3.0
	material.initial_velocity_max = 11.0
	material.scale_min = 0.35
	material.scale_max = 1.2
	return material


func _create_mesh() -> Mesh:
	var sphere: SphereMesh = SphereMesh.new()
	sphere.radius = 0.11
	sphere.height = 0.16
	var mat: StandardMaterial3D = StandardMaterial3D.new()
	mat.albedo_color = Color(0.42, 0.0, 0.0, 1.0)
	sphere.material = mat
	return sphere


func _auto_free() -> void:
	await get_tree().create_timer(auto_free_delay).timeout
	if is_instance_valid(self):
		queue_free()
