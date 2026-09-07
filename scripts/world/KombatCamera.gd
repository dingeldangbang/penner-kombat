extends Node
class_name KombatCamera

## Cinematic Fight-Kamera (MKX-Referenz):
##  - automatisches Framing beider Fighter (Mittelpunkt + Distanz)
##  - dynamisches FOV (näher = dramatischer, weiter = Übersicht)
##  - Punch-Zoom bei Treffern, Finisher-Zoom bei Knockout
##  - sanfte Pfad-Lerps statt harter Sprünge
##  - Side-Offset, damit die Kämpfer nicht mittig "kleben"

@export var camera: Camera3D
@export var base_fov: float = 55.0
@export var smooth: float = 0.08
@export var side_offset_factor: float = 0.35

var target_a: Node3D
var target_b: Node3D

var _punch: float = 0.0
var _punch_decay: float = 6.0
var _finisher_zoom: float = 0.0
var _initial_snapped: bool = false
var _trauma: float = 0.0


func configure(fighter_a: Node3D, fighter_b: Node3D) -> void:
	target_a = fighter_a
	target_b = fighter_b


func has_framed() -> bool:
	return _initial_snapped


func snap() -> void:
	_initial_snapped = false
	if camera == null:
		return
	var focus: Vector3 = _focus_point()
	camera.global_position = _desired_position(focus)
	camera.look_at(focus + Vector3.UP * 0.9, Vector3.UP)


func punch_zoom(amount: float = 1.0) -> void:
	_punch = clampf(_punch + amount, 0.0, 2.5)


func add_trauma(amount: float = 0.5) -> void:
	_trauma = clampf(_trauma + amount, 0.0, 1.5)


func activate_finisher_zoom() -> void:
	_finisher_zoom = 1.0
	_punch = 1.4


func reset() -> void:
	_punch = 0.0
	_finisher_zoom = 0.0
	_initial_snapped = false


func _process(delta: float) -> void:
	if camera == null or target_a == null or target_b == null or not is_instance_valid(target_a) or not is_instance_valid(target_b):
		return

	var focus: Vector3 = _focus_point()
	var desired: Vector3 = _desired_position(focus)

	if not _initial_snapped:
		camera.global_position = desired
		_initial_snapped = true
	else:
		camera.global_position = camera.global_position.lerp(desired, smooth)

	camera.look_at(focus + Vector3.UP * 0.9, Vector3.UP)

	_punch = maxf(0.0, _punch - _punch_decay * delta)
	_finisher_zoom = maxf(0.0, _finisher_zoom - delta * 0.35)

	var distance: float = target_a.global_position.distance_to(target_b.global_position)
	var target_fov: float = base_fov + clampf(distance - 4.0, 0.0, 8.0) * 1.1
	target_fov -= _punch * 7.0
	target_fov -= _finisher_zoom * 12.0
	camera.fov = lerpf(camera.fov, target_fov, smooth * 1.4)

	# Trauma-Shake (Impact) — bewusst NACH dem Framing, kollidiert nicht mit ScreenShake
	_trauma = maxf(0.0, _trauma - delta * 2.4)
	if _trauma > 0.001:
		camera.global_position += Vector3(
			randf_range(-1.0, 1.0),
			randf_range(-1.0, 1.0) * 0.6,
			randf_range(-1.0, 1.0)
		) * _trauma * 0.38
		camera.rotation.z += randf_range(-1.0, 1.0) * _trauma * 0.035


func _focus_point() -> Vector3:
	var mid: Vector3 = (target_a.global_position + target_b.global_position) * 0.5
	# Leichte Vorausschau in Richtung des aktiven Kämpfers (Bewegungsdynamik)
	mid += (target_a.global_position - target_b.global_position).normalized() * 0.35
	return mid


func _desired_position(focus: Vector3) -> Vector3:
	var distance: float = clampf(target_a.global_position.distance_to(target_b.global_position), 4.0, 14.0)
	var side: Vector3 = (target_b.global_position - target_a.global_position).normalized()
	var lateral: Vector3 = side.cross(Vector3.UP).normalized()
	var offset: Vector3 = Vector3(0, 5.2 + distance * 0.3, 8.4 + distance * 0.42)
	offset += lateral * side_offset_factor * clampf(distance * 0.35, 0.0, 1.6)
	return focus + offset
