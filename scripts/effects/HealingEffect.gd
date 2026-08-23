extends GPUParticles3D
class_name HealingEffect

## Green upward spiral healing particles.

@export var heal_color: Color = Color(0.0, 1.0, 0.4, 1.0)
@export var auto_free_delay: float = 1.8


func _ready() -> void:
	one_shot = true
	explosiveness = 0.25
	if process_material == null:
		process_material = _create_material()
	if draw_pass_1 == null:
		draw_pass_1 = _create_mesh()


func initialize(amount_healed: float = 25.0) -> void:
	var factor: float = clampf(amount_healed / 100.0, 0.2, 1.0)
	amount = int(lerpf(18.0, 55.0, factor))
	lifetime = lerpf(0.6, 1.5, factor)
	restart()
	emitting = true
	_auto_free()


func _create_material() -> ParticleProcessMaterial:
	var material: ParticleProcessMaterial = ParticleProcessMaterial.new()
	material.direction = Vector3.UP
	material.spread = 35.0
	material.gravity = Vector3(0.0, 0.8, 0.0)
	material.initial_velocity_min = 0.6
	material.initial_velocity_max = 2.2
	material.angular_velocity_min = -180.0
	material.angular_velocity_max = 180.0
	material.orbit_velocity_min = 0.2
	material.orbit_velocity_max = 1.0
	material.scale_min = 0.18
	material.scale_max = 0.6
	material.color = heal_color
	return material


func _create_mesh() -> Mesh:
	var sphere: SphereMesh = SphereMesh.new()
	sphere.radius = 0.08
	sphere.height = 0.08
	var mat: StandardMaterial3D = StandardMaterial3D.new()
	mat.albedo_color = heal_color
	mat.emission_enabled = true
	mat.emission = heal_color
	mat.emission_energy_multiplier = 1.4
	sphere.material = mat
	return sphere


func _auto_free() -> void:
	await get_tree().create_timer(auto_free_delay).timeout
	if is_instance_valid(self):
		queue_free()
