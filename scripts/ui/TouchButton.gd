extends Control
class_name TouchButton

## Touch button with press/release state, optional vibration and editor mouse fallback.

signal pressed(button_id: String)
signal released(button_id: String)

@export var button_id: String = "light"
@export var icon: Texture2D
@export var label: String = ""
@export var radius: float = 44.0
@export var color_normal: Color = Color(0.3, 0.3, 0.4, 0.6)
@export var color_pressed: Color = Color(0.8, 0.3, 0.3, 0.85)
@export var border_color: Color = Color(1.0, 1.0, 1.0, 0.45)
@export var vibrate_on_press: bool = true

var _touch_index: int = -1
var _is_pressed: bool = false


func _ready() -> void:
	custom_minimum_size = Vector2(radius * 2.0, radius * 2.0)
	mouse_filter = Control.MOUSE_FILTER_STOP
	queue_redraw()


func _gui_input(event: InputEvent) -> void:
	if event is InputEventScreenTouch:
		var touch: InputEventScreenTouch = event as InputEventScreenTouch
		if touch.pressed and _touch_index == -1 and _is_point_inside(touch.position):
			_press(touch.index)
		elif not touch.pressed and touch.index == _touch_index:
			_release()
	elif event is InputEventScreenDrag:
		var drag: InputEventScreenDrag = event as InputEventScreenDrag
		if drag.index == _touch_index and not _is_point_inside(drag.position):
			_release()
	elif event is InputEventMouseButton and (event as InputEventMouseButton).button_index == MOUSE_BUTTON_LEFT:
		var mouse: InputEventMouseButton = event as InputEventMouseButton
		if mouse.pressed and _is_point_inside(mouse.global_position):
			_press(-2)
		elif not mouse.pressed and _is_pressed:
			_release()


func _press(index: int) -> void:
	_touch_index = index
	_is_pressed = true
	if vibrate_on_press:
		Input.vibrate_handheld(50)
	pressed.emit(button_id)
	queue_redraw()


func _release() -> void:
	var was_pressed: bool = _is_pressed
	_touch_index = -1
	_is_pressed = false
	if was_pressed:
		released.emit(button_id)
	queue_redraw()


func _is_point_inside(global_point: Vector2) -> bool:
	return (global_point - (global_position + size * 0.5)).length() <= radius


func is_pressed() -> bool:
	return _is_pressed


func _draw() -> void:
	var center: Vector2 = size * 0.5
	draw_circle(center, radius, color_pressed if _is_pressed else color_normal)
	draw_arc(center, radius, 0.0, TAU, 48, border_color, 2.0, true)
	if icon:
		draw_texture_rect(icon, Rect2(center - Vector2(radius * 0.45, radius * 0.45), Vector2(radius * 0.9, radius * 0.9)), false)
	if not label.is_empty():
		draw_string(get_theme_default_font(), center + Vector2(-radius * 0.35, 5), label, HORIZONTAL_ALIGNMENT_LEFT, radius * 0.8, 16, Color.WHITE)
