extends Node
class_name Localization

## Runtime localization helper for labels, buttons and metadata-based UI translation.

static var instance: Localization

@export var default_language: String = "de"
@export var translations_path: String = "res://translations/translations.json"

var _current_language: String = "de"
var _translations: Dictionary = {}
var _loaded: bool = false


func _ready() -> void:
	instance = self
	_load_translations()
	set_language(default_language)


func _load_translations() -> void:
	if FileAccess.file_exists(translations_path):
		var data: String = FileAccess.get_file_as_string(translations_path)
		var parsed: Variant = JSON.parse_string(data)
		if parsed is Dictionary:
			_translations = parsed as Dictionary
	_loaded = true


func set_language(lang: String) -> void:
	_current_language = lang
	_localize_ui()


func get_language() -> String:
	return _current_language


func reload() -> void:
	_load_translations()
	_localize_ui()


func _localize_ui() -> void:
	if not _loaded or get_tree() == null:
		return
	_traverse_and_localize(get_tree().root)


func _traverse_and_localize(node: Node) -> void:
	if (node is Label or node is Button or node is LineEdit) and node.has_meta("tr_key"):
		var key: String = str(node.get_meta("tr_key"))
		if not key.is_empty():
			node.set("text", _translate(key))
	if node is TextEdit and node.has_meta("tr_placeholder"):
		(node as TextEdit).placeholder_text = _translate(str(node.get_meta("tr_placeholder")))
	if node is LineEdit and node.has_meta("tr_placeholder"):
		(node as LineEdit).placeholder_text = _translate(str(node.get_meta("tr_placeholder")))
	for child in node.get_children():
		_traverse_and_localize(child)


func _translate(key: String) -> String:
	if _translations.has(key):
		var entry: Variant = _translations[key]
		if entry is Dictionary and (entry as Dictionary).has(_current_language):
			return str((entry as Dictionary)[_current_language])
	return key


static func tr(key: String) -> String:
	if instance:
		return instance._translate(key)
	return key
