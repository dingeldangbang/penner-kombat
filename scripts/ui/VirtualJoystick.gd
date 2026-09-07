extends Control
class_name PennerJoystick

## Mobile on-screen joystick with mouse fallback for editor testing.

signal joystick_moved(input: Vector2)
signal joystick_released()

@export var max_radius_ratio: float = 0.42
@export var dead_zone: float = 0.08
@export var base_color: Color = Color(0.12, 0.09, 0.16, 0.70)
@export var knob_color: Color = Color(0.8, 0.2, 0.2, 0.90)
@export var border_color: Color = Color(0.6, 0.2, 0.2, 0.95)

var _touch_index: int = -1
var _input_vector: Vector2 = Vector2.ZERO
var _is_pressed: bool = false
var _knob_offset: Vector2 = Vector2.ZERO


func _ready() -> void:
	custom_minimum_size = Vector2(180, 180)
	mouse_filter = Control.MOUSE_FILTER_STOP
	queue_redraw()


func _gui_input(event: InputEvent) -> void:
	if event is InputEventScreenTouch:
		_handle_touch(event as InputEventScreenTouch)
	elif event is InputEventScreenDrag:
		_handle_drag(event as InputEventScreenDrag)
	elif event is InputEventMouseButton:
		_handle_mouse_button(event as InputEventMouseButton)
	elif event is InputEventMouseMotion and _is_pressed and _touch_index == -2:
		_update_joystick((event as InputEventMouseMotion).position)


func _handle_touch(event: InputEventScreenTouch) -> void:
	if event.pressed and _touch_index == -1:
		_touch_index = event.index
		_is_pressed = true
		_update_joystick(event.position)
	elif not event.pressed and event.index == _touch_index:
		_release_joystick()


func _handle_drag(event: InputEventScreenDrag) -> void:
	if event.index == _touch_index:
		_update_joystick(event.position)


func _handle_mouse_button(event: InputEventMouseButton) -> void:
	if event.button_index != MOUSE_BUTTON_LEFT:
		return
	if event.pressed:
		_touch_index = -2
		_is_pressed = true
		_update_joystick(event.position)
	else:
		_release_joystick()


func _update_joystick(local_position: Vector2) -> void:
	var center: Vector2 = size * 0.5
	var radius: float = minf(size.x, size.y) * max_radius_ratio
	var offset: Vector2 = local_position - center
	if offset.length() > radius:
		offset = offset.normalized() * radius

	_knob_offset = offset
	_input_vector = offset / radius if radius > 0.0 else Vector2.ZERO
	if _input_vector.length() < dead_zone:
		_input_vector = Vector2.ZERO
	joystick_moved.emit(_input_vector)
	queue_redraw()


func _release_joystick() -> void:
	_touch_index = -1
	_is_pressed = false
	_input_vector = Vector2.ZERO
	_knob_offset = Vector2.ZERO
	joystick_released.emit()
	queue_redraw()


func get_input() -> Vector2:
	return _input_vector


func is_pressed() -> bool:
	return _is_pressed


func _draw() -> void:
	var center: Vector2 = size * 0.5
	var radius: float = minf(size.x, size.y) * max_radius_ratio
	draw_circle(center, radius, base_color)
	draw_arc(center, radius, 0.0, TAU, 64, border_color, 3.0, true)
	draw_circle(center + _knob_offset, radius * 0.38, knob_color)
