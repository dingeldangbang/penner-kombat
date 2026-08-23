extends Node3D
class_name GreaseEffect

## Oily shader and drip particle controller for Le Binde.

@export var grease_material: ShaderMaterial
@export var drip_particles: GPUParticles3D
@export var max_drip_amount: int = 24

var _grease_intensity: float = 0.0
var _target_intensity: float = 0.0


func update_grease(charges: int, max_charges: int) -> void:
	_target_intensity = clampf(float(charges) / float(maxi(max_charges, 1)), 0.0, 1.0)
	if grease_material:
		grease_material.set_shader_parameter("intensity", _target_intensity)
		grease_material.set_shader_parameter("color", Color(0.9, 0.7, 0.2, 1.0))


func _process(delta: float) -> void:
	_grease_intensity = lerpf(_grease_intensity, _target_intensity, clampf(delta * 5.0, 0.0, 1.0))
	if drip_particles:
		drip_particles.emitting = _grease_intensity > 0.1
		drip_particles.amount = int(_grease_intensity * max_drip_amount)
