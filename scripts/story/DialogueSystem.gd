extends CanvasLayer
class_name DialogueSystem

## Queue-based dialogue box with speaker name and next/skip controls.

signal line_started(speaker: String, text: String)
signal dialogue_finished()

var lines: Array[Dictionary] = []
var current_index: int = -1
var _panel: Panel
var _speaker_label: Label
var _text_label: Label


func _ready() -> void:
	_build_ui()
	visible = false


func start_dialogue(new_lines: Array[Dictionary]) -> void:
	lines = new_lines.duplicate(true)
	current_index = -1
	visible = true
	next_line()


func next_line() -> void:
	current_index += 1
	if current_index >= lines.size():
		visible = false
		dialogue_finished.emit()
		return
	var line: Dictionary = lines[current_index]
	_speaker_label.text = str(line.get("speaker", ""))
	_text_label.text = str(line.get("text", ""))
	line_started.emit(_speaker_label.text, _text_label.text)


func _unhandled_input(event: InputEvent) -> void:
	if visible and (event.is_action_pressed("ui_accept") or event.is_action_pressed("ui_select")):
		next_line()


func _build_ui() -> void:
	_panel = Panel.new()
	_panel.anchor_left = 0.1
	_panel.anchor_top = 0.72
	_panel.anchor_right = 0.9
	_panel.anchor_bottom = 0.95
	add_child(_panel)
	var vbox: VBoxContainer = VBoxContainer.new()
	vbox.set_anchors_preset(Control.PRESET_FULL_RECT)
	vbox.offset_left = 16
	vbox.offset_top = 12
	vbox.offset_right = -16
	vbox.offset_bottom = -12
	_panel.add_child(vbox)
	_speaker_label = Label.new()
	_speaker_label.add_theme_font_size_override("font_size", 22)
	vbox.add_child(_speaker_label)
	_text_label = Label.new()
	_text_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	_text_label.add_theme_font_size_override("font_size", 18)
	vbox.add_child(_text_label)
