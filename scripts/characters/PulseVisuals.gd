extends Node3D
class_name PulseVisuals

## Pulse and speed-line visualizer for Mell.

@export var pulse_material: ShaderMaterial
@export var speed_lines: GPUParticles3D
@export var warning_material: ShaderMaterial
@export var max_pulse: float = 220.0

var _pulse: float = 60.0


func update_pulse(bpm: float) -> void:
	_pulse = bpm
	var intensity: float = clampf(bpm / maxf(max_pulse, 1.0), 0.0, 1.3)
	if pulse_material:
		pulse_material.set_shader_parameter("pulse_intensity", intensity)
		pulse_material.set_shader_parameter("pulse_color", _get_pulse_color(bpm))
	if speed_lines:
		speed_lines.emitting = bpm > 160.0
		speed_lines.amount = int(bpm / 10.0) * 2
	if warning_material:
		warning_material.set_shader_parameter("is_warning", bpm > 190.0)


func _get_pulse_color(bpm: float) -> Color:
	if bpm > 200.0:
		return Color.RED
	elif bpm > 160.0:
		return Color(1.0, 0.5, 0.0)
	elif bpm > 100.0:
		return Color.YELLOW
	return Color(0.2, 0.8, 0.2)
