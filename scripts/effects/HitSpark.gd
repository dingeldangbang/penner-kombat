extends GPUParticles3D
class_name HitSpark

## Impact spark particles with critical-hit freeze support.

@export var min_size: float = 0.05
@export var max_size: float = 0.3
@export var spark_color: Color = Color(1.0, 0.8, 0.2, 1.0)
@export var auto_free_delay: float = 1.0


func _ready() -> void:
	one_shot = true
	explosiveness = 1.0
	visibility_aabb = AABB(Vector3(-3, -3, -3), Vector3(6, 6, 6))
	if process_material == null:
		process_material = _create_process_material()
	if draw_pass_1 == null:
		draw_pass_1 = _create_particle_mesh()


func initialize(damage: float, direction: Vector3 = Vector3.UP, is_critical: bool = false) -> void:
	if process_material == null:
		process_material = _create_process_material()
	var material: ParticleProcessMaterial = process_material as ParticleProcessMaterial
	if material == null:
		return

	var size_factor: float = clampf(damage / 100.0, 0.1, 1.0)
	var start_size: float = lerpf(min_size, max_size, size_factor)
	var color: Color = Color(1.0, 0.84, 0.0, 1.0) if is_critical else Color(1.0, 0.42, 0.0, 1.0)

	material.color = color
	material.scale_min = start_size * 0.5
	material.scale_max = start_size
	material.initial_velocity_min = 2.0 * (0.5 + size_factor * 0.5)
	material.initial_velocity_max = 8.0 * (0.5 + size_factor * 0.5)
	material.direction = direction.normalized() if direction.length() > 0.001 else Vector3.UP
	material.spread = 35.0
	material.gravity = Vector3.ZERO

	lifetime = lerpf(0.1, 0.4, size_factor)
	amount = int(lerpf(7.0, 24.0, size_factor))
	restart()
	emitting = true
	if is_critical:
		_critical_freeze()
	_auto_free()


func _create_process_material() -> ParticleProcessMaterial:
	var material: ParticleProcessMaterial = ParticleProcessMaterial.new()
	material.direction = Vector3.UP
	material.spread = 35.0
	material.gravity = Vector3.ZERO
	material.initial_velocity_min = 2.0
	material.initial_velocity_max = 8.0
	material.scale_min = min_size
	material.scale_max = max_size
	material.color = spark_color
	return material


func _create_particle_mesh() -> Mesh:
	var quad: QuadMesh = QuadMesh.new()
	quad.size = Vector2(0.16, 0.16)
	var mat: StandardMaterial3D = StandardMaterial3D.new()
	mat.albedo_color = Color(1.0, 0.55, 0.0, 1.0)
	mat.emission_enabled = true
	mat.emission = Color(1.0, 0.45, 0.0, 1.0)
	mat.emission_energy_multiplier = 2.5
	mat.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	mat.billboard_mode = BaseMaterial3D.BILLBOARD_ENABLED
	quad.material = mat
	return quad


func _critical_freeze() -> void:
	var previous_scale: float = Engine.time_scale
	Engine.time_scale = 0.01
	await get_tree().process_frame
	await get_tree().process_frame
	Engine.time_scale = previous_scale


func _auto_free() -> void:
	await get_tree().create_timer(maxf(auto_free_delay, lifetime + 0.2)).timeout
	if is_instance_valid(self):
		queue_free()
