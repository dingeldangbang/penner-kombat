extends GPUParticles3D
class_name FireEffect

## Orange/red flame particles for TetraPak and breath attacks.

@export var flame_color: Color = Color(1.0, 0.27, 0.0, 1.0)
@export var auto_free_delay: float = 1.4


func _ready() -> void:
	one_shot = true
	explosiveness = 0.35
	if process_material == null:
		process_material = _create_material()
	if draw_pass_1 == null:
		draw_pass_1 = _create_mesh()


func initialize(power: float = 50.0, direction: Vector3 = Vector3.UP) -> void:
	var factor: float = clampf(power / 100.0, 0.2, 1.0)
	var material: ParticleProcessMaterial = process_material as ParticleProcessMaterial
	if material:
		material.direction = direction.normalized() if direction.length() > 0.001 else Vector3.UP
		material.initial_velocity_min = lerpf(0.8, 2.5, factor)
		material.initial_velocity_max = lerpf(2.5, 6.5, factor)
	amount = int(lerpf(22.0, 85.0, factor))
	lifetime = lerpf(0.35, 1.0, factor)
	restart()
	emitting = true
	_auto_free()


func _create_material() -> ParticleProcessMaterial:
	var material: ParticleProcessMaterial = ParticleProcessMaterial.new()
	material.direction = Vector3.UP
	material.spread = 50.0
	material.gravity = Vector3(0.0, 1.8, 0.0)
	material.initial_velocity_min = 0.8
	material.initial_velocity_max = 4.5
	material.radial_velocity_min = -0.5
	material.radial_velocity_max = 1.2
	material.turbulence_enabled = true
	material.turbulence_noise_strength = 1.4
	material.scale_min = 0.1
	material.scale_max = 0.5
	material.color = flame_color
	return material


func _create_mesh() -> Mesh:
	var quad: QuadMesh = QuadMesh.new()
	quad.size = Vector2(0.35, 0.35)
	var mat: StandardMaterial3D = StandardMaterial3D.new()
	mat.albedo_color = Color(1.0, 0.36, 0.0, 0.85)
	mat.emission_enabled = true
	mat.emission = Color(1.0, 0.2, 0.0, 1.0)
	mat.emission_energy_multiplier = 2.0
	mat.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	mat.billboard_mode = BaseMaterial3D.BILLBOARD_ENABLED
	quad.material = mat
	return quad


func _auto_free() -> void:
	await get_tree().create_timer(auto_free_delay).timeout
	if is_instance_valid(self):
		queue_free()
