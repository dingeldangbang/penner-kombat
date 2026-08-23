extends Node3D
class_name GoldGlow

## Golden aura controller for Mojo Bob.

@export var glow_material: ShaderMaterial
@export var sparkle_particles: GPUParticles3D
@export var ring_mesh: MeshInstance3D

var _gold_intensity: float = 0.0
var _is_super_mode: bool = false


func update_mojo(mojo: int, max_mojo: int) -> void:
	_gold_intensity = clampf(float(mojo) / float(maxi(max_mojo, 1)), 0.0, 1.0)
	if glow_material:
		glow_material.set_shader_parameter("intensity", _gold_intensity)
		glow_material.set_shader_parameter("gold_color", Color(1.0, 0.84, 0.0, 1.0))
	if sparkle_particles:
		sparkle_particles.emitting = _gold_intensity > 0.2
		sparkle_particles.amount = int(_gold_intensity * 30.0)
	if ring_mesh:
		ring_mesh.visible = _gold_intensity > 0.5
		if ring_mesh.material_override is ShaderMaterial:
			(ring_mesh.material_override as ShaderMaterial).set_shader_parameter("intensity", _gold_intensity)


func activate_super_mode() -> void:
	_is_super_mode = true
	if glow_material:
		glow_material.set_shader_parameter("intensity", 2.0)
		glow_material.set_shader_parameter("gold_color", Color(1.0, 0.9, 0.3, 1.0))
	if sparkle_particles:
		sparkle_particles.emitting = true
		sparkle_particles.amount = 100


func deactivate_super_mode() -> void:
	_is_super_mode = false
	update_mojo(0, 7)


func is_super_mode() -> bool:
	return _is_super_mode
