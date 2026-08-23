extends Node
class_name ErrorHandler

## Central runtime logging with optional UI TextEdit integration.

signal error_occurred(error: String)
signal warning_logged(warning: String)
signal info_logged(message: String)

@export var max_log_entries: int = 100

var _log_buffer: Array[String] = []
var _ui_log_text: TextEdit = null


func log_info(message: String) -> void:
	var entry: String = _format_entry("INFO", message)
	_append(entry)
	info_logged.emit(entry)


func log_error(error: String) -> void:
	var entry: String = _format_entry("ERROR", error)
	_append(entry)
	push_error(error)
	error_occurred.emit(error)


func log_warning(warning: String) -> void:
	var entry: String = _format_entry("WARN", warning)
	_append(entry)
	push_warning(warning)
	warning_logged.emit(warning)


func set_ui_target(text_edit: TextEdit) -> void:
	_ui_log_text = text_edit
	_update_ui()


func get_log() -> String:
	var lines: PackedStringArray = PackedStringArray()
	for entry in _log_buffer:
		lines.append(entry)
	return "\n".join(lines)


func get_entries() -> Array[String]:
	return _log_buffer.duplicate()


func clear_log() -> void:
	_log_buffer.clear()
	_update_ui()


func _format_entry(level: String, message: String) -> String:
	return "[%s] %s: %s" % [Time.get_datetime_string_from_system(), level, message]


func _append(entry: String) -> void:
	_log_buffer.append(entry)
	while _log_buffer.size() > max_log_entries:
		_log_buffer.pop_front()
	_update_ui()


func _update_ui() -> void:
	if _ui_log_text and is_instance_valid(_ui_log_text):
		_ui_log_text.text = get_log()
		_ui_log_text.scroll_vertical = _ui_log_text.get_line_count()
