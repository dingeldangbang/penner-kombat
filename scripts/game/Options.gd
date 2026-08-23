extends Control
class_name OptionsScreen

## Einstellungen: Schwierigkeit, Rundenanzahl, Rundenzeit, Touch-Größe, Sound.
## Alles landet in GameState und wird sofort gespeichert.

const ACCENT := Color(0.85, 0.16, 0.16)

var _rows: VBoxContainer


func _ready() -> void:
	set_anchors_preset(Control.PRESET_FULL_RECT)
	_build()


func _build() -> void:
	var bg: ColorRect = ColorRect.new()
	bg.set_anchors_preset(Control.PRESET_FULL_RECT)
	bg.color = Color(0.04, 0.03, 0.06)
	bg.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(bg)

	var margin: MarginContainer = MarginContainer.new()
	margin.set_anchors_preset(Control.PRESET_FULL_RECT)
	for side in ["left", "right", "top", "bottom"]:
		margin.add_theme_constant_override("margin_" + side, 40)
	add_child(margin)

	var col: VBoxContainer = VBoxContainer.new()
	col.add_theme_constant_override("separation", 12)
	margin.add_child(col)

	var title: Label = Label.new()
	title.text = "EINSTELLUNGEN"
	title.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	title.add_theme_font_size_override("font_size", 42)
	title.add_theme_color_override("font_color", ACCENT)
	col.add_child(title)

	_rows = VBoxContainer.new()
	_rows.add_theme_constant_override("separation", 10)
	_rows.size_flags_vertical = Control.SIZE_EXPAND_FILL
	col.add_child(_rows)

	_refresh()

	var back: Button = Button.new()
	back.text = "ZURÜCK"
	back.custom_minimum_size = Vector2(0, 72)
	back.add_theme_font_size_override("font_size", 30)
	back.pressed.connect(func() -> void:
		GameState.save_game()
		get_tree().change_scene_to_file("res://scenes/ui/MainMenu.tscn"))
	col.add_child(back)


func _refresh() -> void:
	for child in _rows.get_children():
		child.queue_free()

	_add_cycle("Schwierigkeit", GameState.difficulty_name(), func() -> void:
		GameState.difficulty = wrapi(GameState.difficulty + 1, 0, 5)
		GameState.save_game()
		_refresh())

	_add_cycle("Runden zum Sieg", str(GameState.rounds_to_win), func() -> void:
		# 1 -> 2 -> 3 -> 1
		GameState.rounds_to_win = wrapi(GameState.rounds_to_win + 1, 1, 4)
		GameState.save_game()
		_refresh())

	_add_cycle("Rundenzeit", "%d s" % int(GameState.round_seconds), func() -> void:
		var steps: Array[float] = [30.0, 60.0, 99.0, 180.0]
		var i: int = steps.find(GameState.round_seconds)
		GameState.round_seconds = steps[wrapi(i + 1, 0, steps.size())]
		GameState.save_game()
		_refresh())

	_add_cycle("Touch-Größe", "%d %%" % int(GameState.touch_scale * 100.0), func() -> void:
		var steps: Array[float] = [0.8, 1.0, 1.2, 1.45]
		var i: int = steps.find(GameState.touch_scale)
		GameState.touch_scale = steps[wrapi(i + 1, 0, steps.size())]
		GameState.save_game()
		_refresh())

	_add_cycle("Sound", "an" if GameState.sfx_enabled else "aus", func() -> void:
		GameState.sfx_enabled = not GameState.sfx_enabled
		GameState.save_game()
		_refresh())

	var s: Label = Label.new()
	s.text = "\nKämpfe: %d   Siege: %d   Niederlagen: %d   Beste Combo: %d" % [
		int(GameState.stats.get("matches", 0)),
		int(GameState.stats.get("wins", 0)),
		int(GameState.stats.get("losses", 0)),
		int(GameState.stats.get("best_combo", 0))]
	s.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	s.add_theme_font_size_override("font_size", 20)
	s.add_theme_color_override("font_color", Color(0.6, 0.58, 0.65))
	_rows.add_child(s)


func _add_cycle(label_text: String, value_text: String, handler: Callable) -> void:
	var row: HBoxContainer = HBoxContainer.new()
	row.custom_minimum_size = Vector2(0, 68)
	row.add_theme_constant_override("separation", 12)
	_rows.add_child(row)

	var l: Label = Label.new()
	l.text = label_text
	l.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	l.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	l.add_theme_font_size_override("font_size", 26)
	row.add_child(l)

	var b: Button = Button.new()
	b.text = value_text
	b.custom_minimum_size = Vector2(220, 64)
	b.add_theme_font_size_override("font_size", 26)
	b.add_theme_color_override("font_color", ACCENT)
	b.pressed.connect(handler)
	row.add_child(b)


func _unhandled_input(event: InputEvent) -> void:
	if event.is_action_pressed("ui_cancel"):
		GameState.save_game()
		get_tree().change_scene_to_file("res://scenes/ui/MainMenu.tscn")
