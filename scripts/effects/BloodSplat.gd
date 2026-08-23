extends GPUParticles3D
class_name BloodSplat

## Blood spray particles with damage-scaled size, speed and lifetime.

@export var min_size: float = 0.1
@export var max_size: float = 0.8
@export var min_lifetime: float = 0.5
@export var max_lifetime: float = 2.0
@export var min_speed: float = 1.0
@export var max_speed: float = 5.0
@export var blood_color: Color = Color(0.545, 0.0, 0.0, 1.0)
@export var auto_free_delay: float = 3.0
@export var enable_secondary_splashes: bool = true


func _ready() -> void:
	one_shot = true
	explosiveness = 0.85
	visibility_aabb = AABB(Vector3(-4, -3, -4), Vector3(8, 7, 8))
	if process_material == null:
		process_material = _create_process_material()
	if draw_pass_1 == null:
		draw_pass_1 = _create_particle_mesh()


func initialize(damage: float, direction: Vector3 = Vector3.UP) -> void:
	if process_material == null:
		process_material = _create_process_material()
	var material: ParticleProcessMaterial = process_material as ParticleProcessMaterial
	if material == null:
		return

	var size_factor: float = clampf(damage / 100.0, 0.1, 1.0)
	var start_size: float = lerpf(min_size, max_size, size_factor)
	var color_variation: Color = Color(
		clampf(blood_color.r + randf_range(-0.08, 0.06), 0.0, 1.0),
		clampf(blood_color.g + randf_range(0.0, 0.03), 0.0, 1.0),
		clampf(blood_color.b + randf_range(0.0, 0.03), 0.0, 1.0),
		1.0
	)

	material.color = color_variation
	material.scale_min = start_size * 0.45
	material.scale_max = start_size
	material.initial_velocity_min = min_speed * (0.5 + size_factor * 0.5)
	material.initial_velocity_max = max_speed * (0.5 + size_factor * 0.5)
	material.direction = direction.normalized() if direction.length() > 0.001 else Vector3.UP
	material.spread = 65.0
	material.gravity = Vector3(0.0, -5.5, 0.0)

	lifetime = lerpf(min_lifetime, max_lifetime, size_factor)
	amount = int(lerpf(8.0, 38.0, size_factor))
	restart()
	emitting = true
	if enable_secondary_splashes and damage >= 15.0:
		_spawn_secondary_splashes(damage, direction)
	_auto_free()


func _spawn_secondary_splashes(damage: float, direction: Vector3) -> void:
	await get_tree().create_timer(0.08).timeout
	if not is_instance_valid(self) or get_parent() == null:
		return
	var secondary: BloodSplat = BloodSplat.new()
	secondary.enable_secondary_splashes = false
	get_parent().add_child(secondary)
	secondary.global_position = global_position + direction.normalized() * 0.25 + Vector3.UP * 0.1
	secondary.initialize(damage * 0.35, (direction + Vector3(randf_range(-0.4, 0.4), 0.6, randf_range(-0.4, 0.4))).normalized())


func _create_process_material() -> ParticleProcessMaterial:
	var material: ParticleProcessMaterial = ParticleProcessMaterial.new()
	material.direction = Vector3.UP
	material.spread = 65.0
	material.gravity = Vector3(0.0, -5.5, 0.0)
	material.initial_velocity_min = min_speed
	material.initial_velocity_max = max_speed
	material.scale_min = min_size
	material.scale_max = max_size
	material.color = blood_color
	return material


func _create_particle_mesh() -> Mesh:
	var sphere: SphereMesh = SphereMesh.new()
	sphere.radius = 0.08
	sphere.height = 0.12
	sphere.radial_segments = 8
	sphere.rings = 4
	var mat: StandardMaterial3D = StandardMaterial3D.new()
	mat.albedo_color = blood_color
	mat.roughness = 0.85
	sphere.material = mat
	return sphere


func _auto_free() -> void:
	await get_tree().create_timer(maxf(auto_free_delay, lifetime + 0.25)).timeout
	if is_instance_valid(self):
		queue_free()
