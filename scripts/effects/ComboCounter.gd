extends Control
class_name ComboCounter

## Standalone combo counter widget with timeout, color thresholds and pulse animation.

signal combo_changed(count: int)
signal combo_finished(final_count: int)

@export var timeout: float = 2.0
@export var normal_color: Color = Color.WHITE
@export var high_color: Color = Color(1.0, 0.84, 0.0)
@export var max_color: Color = Color(1.0, 0.15, 0.1)

var count: int = 0
var _timer: float = 0.0
var _label: Label


func _ready() -> void:
	_label = get_node_or_null("Label") as Label
	if _label == null:
		_label = Label.new()
		_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
		_label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
		_label.add_theme_font_size_override("font_size", 56)
		add_child(_label)
	_reset_visual()


func _process(delta: float) -> void:
	if count <= 0:
		return
	_timer -= delta
	if _timer <= 0.0:
		var final_count: int = count
		reset()
		combo_finished.emit(final_count)


func add_hit(amount: int = 1) -> void:
	count += max(amount, 1)
	_timer = timeout
	_update_visual()
	combo_changed.emit(count)


func set_combo(value: int) -> void:
	count = max(value, 0)
	_timer = timeout if count > 0 else 0.0
	_update_visual()
	combo_changed.emit(count)


func reset() -> void:
	count = 0
	_timer = 0.0
	_reset_visual()
	combo_changed.emit(count)


func _update_visual() -> void:
	visible = count > 0
	_label.text = "%d KOMBO!" % count
	_label.modulate = max_color if count >= 10 else high_color if count >= 5 else normal_color
	var tween: Tween = create_tween()
	_label.scale = Vector2.ONE
	tween.tween_property(_label, "scale", Vector2.ONE * (1.5 if count >= 10 else 1.25), 0.08).set_trans(Tween.TRANS_BACK).set_ease(Tween.EASE_OUT)
	tween.tween_property(_label, "scale", Vector2.ONE, 0.14)


func _reset_visual() -> void:
	visible = false
	if _label:
		_label.text = ""
		_label.scale = Vector2.ONE
