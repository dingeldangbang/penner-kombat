extends Node
class_name ScreenShake

## Camera shake singleton for impact feedback.

static var instance: ScreenShake

@export var camera: Camera3D

var _shake_intensity: float = 0.0
var _shake_duration: float = 0.0
var _original_position: Vector3 = Vector3.ZERO
var _has_original_position: bool = false


func _ready() -> void:
	instance = self
	if camera == null and get_viewport():
		camera = get_viewport().get_camera_3d()
	_capture_original_position()


static func shake(duration: float = 0.1, intensity: float = 0.3) -> void:
	if instance:
		instance._shake(duration, intensity)


func set_camera(new_camera: Camera3D) -> void:
	camera = new_camera
	_capture_original_position()


func _shake(duration: float, intensity: float) -> void:
	if camera == null and get_viewport():
		camera = get_viewport().get_camera_3d()
	_capture_original_position()
	_shake_duration = maxf(_shake_duration, duration)
	_shake_intensity = maxf(_shake_intensity, intensity)


func _process(delta: float) -> void:
	if camera == null:
		return
	if _shake_duration > 0.0:
		_shake_duration -= delta
		var fade: float = clampf(_shake_duration / maxf(_shake_duration + delta, 0.001), 0.0, 1.0)
		var offset: Vector3 = Vector3(
			randf_range(-1.0, 1.0) * _shake_intensity * fade,
			randf_range(-1.0, 1.0) * _shake_intensity * fade,
			0.0
		)
		camera.position = _original_position + offset
	else:
		camera.position = _original_position
		_shake_intensity = 0.0


func _capture_original_position() -> void:
	if camera and not _has_original_position:
		_original_position = camera.position
		_has_original_position = true
