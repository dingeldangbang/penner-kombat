extends Node
class_name TouchInputManager

## Centralized touch input aggregator for joystick and action buttons.

static var instance: TouchInputManager

@export var joystick: PennerJoystick
@export var light_attack_btn: TouchButton
@export var heavy_attack_btn: TouchButton
@export var block_btn: TouchButton
@export var jump_btn: TouchButton
@export var special1_btn: TouchButton
@export var special2_btn: TouchButton

var _move_input: Vector2 = Vector2.ZERO
var _button_states: Dictionary = {}
var _button_press_times: Dictionary = {}
var _button_down_consumed: Dictionary = {}


func _ready() -> void:
	instance = self
	_button_states = {
		"light": false,
		"heavy": false,
		"block": false,
		"jump": false,
		"special1": false,
		"special2": false
	}
	_connect_buttons()


func _connect_buttons() -> void:
	_connect_button(light_attack_btn)
	_connect_button(heavy_attack_btn)
	_connect_button(block_btn)
	_connect_button(jump_btn)
	_connect_button(special1_btn)
	_connect_button(special2_btn)
	if joystick:
		joystick.joystick_moved.connect(_on_joystick_moved)
		joystick.joystick_released.connect(_on_joystick_released)


func _connect_button(button: TouchButton) -> void:
	if button == null:
		return
	button.pressed.connect(_on_button_pressed)
	button.released.connect(_on_button_released)
	_button_states[button.button_id] = false


func _on_joystick_moved(input: Vector2) -> void:
	_move_input = input


func _on_joystick_released() -> void:
	_move_input = Vector2.ZERO


func _on_button_pressed(button: String) -> void:
	_button_states[button] = true
	_button_press_times[button] = Time.get_ticks_msec()
	_button_down_consumed[button] = false


func _on_button_released(button: String) -> void:
	_button_states[button] = false
	_button_down_consumed[button] = false


func get_move() -> Vector2:
	return _move_input


func get_button(button: String) -> bool:
	return bool(_button_states.get(button, false))


func get_button_down(button: String) -> bool:
	if not bool(_button_states.get(button, false)):
		return false
	if bool(_button_down_consumed.get(button, false)):
		return false
	var press_time: int = int(_button_press_times.get(button, 0))
	if Time.get_ticks_msec() - press_time < 150:
		_button_down_consumed[button] = true
		return true
	return false


func apply_to_fighter(fighter: CombatController) -> void:
	if fighter == null:
		return
	fighter.set_move_input(get_move())
	fighter.is_blocking = get_button("block")
	if get_button_down("light"):
		fighter.execute_next_combo()
	if get_button_down("heavy"):
		fighter.execute_next_combo()
	if get_button_down("jump"):
		fighter.jump()
