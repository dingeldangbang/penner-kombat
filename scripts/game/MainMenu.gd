extends Control
class_name MainMenu

## Startbildschirm. Ersetzt den Editor als Einstiegspunkt.
##
## Der Editor bleibt erreichbar ("Figur bauen"), ist aber nicht mehr die
## Huerde vor dem Spiel: vorher musste man erst eine GLB-Datei laden, bevor
## irgendetwas passierte -- auf einem frischen Handy also nie.
##
## Die UI wird komplett im Code aufgebaut. Das haelt die Szenendatei trivial
## und vermeidet, dass @onready-Pfade und .tscn auseinanderlaufen.

const BG_TOP := Color(0.09, 0.05, 0.10)
const BG_BOTTOM := Color(0.03, 0.02, 0.05)
const ACCENT := Color(0.85, 0.16, 0.16)


func _ready() -> void:
	set_anchors_preset(Control.PRESET_FULL_RECT)
	_build()


func _build() -> void:
	var bg: ColorRect = ColorRect.new()
	bg.set_anchors_preset(Control.PRESET_FULL_RECT)
	bg.color = BG_BOTTOM
	bg.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(bg)

	var margin: MarginContainer = MarginContainer.new()
	margin.set_anchors_preset(Control.PRESET_FULL_RECT)
	for side in ["left", "right"]:
		margin.add_theme_constant_override("margin_" + side, 48)
	margin.add_theme_constant_override("margin_top", 36)
	margin.add_theme_constant_override("margin_bottom", 36)
	add_child(margin)

	var col: VBoxContainer = VBoxContainer.new()
	col.alignment = BoxContainer.ALIGNMENT_CENTER
	col.add_theme_constant_override("separation", 14)
	margin.add_child(col)

	var title: Label = Label.new()
	title.text = "PENNER KOMBAT"
	title.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	title.add_theme_font_size_override("font_size", 68)
	title.add_theme_color_override("font_color", ACCENT)
	col.add_child(title)

	var sub: Label = Label.new()
	sub.text = "Prügeleien in der Unterführung"
	sub.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	sub.add_theme_font_size_override("font_size", 24)
	sub.add_theme_color_override("font_color", Color(0.75, 0.72, 0.78))
	col.add_child(sub)

	col.add_child(_spacer(18))

	col.add_child(_button("KAMPF STARTEN", _on_quick_fight, 42, ACCENT))
	col.add_child(_button("ARCADE-MODUS", _on_arcade, 32))
	col.add_child(_button("KÄMPFER WÄHLEN", _on_select, 32))
	col.add_child(_button("TRAINING", _on_training, 32))
	col.add_child(_button("EINSTELLUNGEN", _on_options, 28))
	col.add_child(_button("FIGUR BAUEN (GLB / KI)", _on_editor, 24,
		Color(0.55, 0.55, 0.62)))

	col.add_child(_spacer(10))

	var info: Label = Label.new()
	var f: FighterRoster.Fighter = FighterRoster.by_id(GameState.selected_fighter)
	info.text = "Gewählt: %s  ·  Schwierigkeit: %s  ·  Siege: %d" % [
		f.display_name, GameState.difficulty_name(),
		int(GameState.stats.get("wins", 0))]
	info.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	info.add_theme_font_size_override("font_size", 20)
	info.add_theme_color_override("font_color", Color(0.6, 0.58, 0.65))
	col.add_child(info)

	# Beenden nur dort anbieten, wo es sinnvoll ist. Auf Android beendet man
	# per Zurück-Taste; ein "Beenden"-Knopf ist dort unüblich.
	if not OS.has_feature("mobile"):
		col.add_child(_button("BEENDEN", func() -> void: get_tree().quit(), 22,
			Color(0.5, 0.5, 0.55)))


func _spacer(height: int) -> Control:
	var c: Control = Control.new()
	c.custom_minimum_size = Vector2(0, height)
	return c


func _button(text: String, handler: Callable, font_size: int = 30,
		color: Color = Color(0.92, 0.90, 0.95)) -> Button:
	var b: Button = Button.new()
	b.text = text
	b.custom_minimum_size = Vector2(0, maxf(64.0, font_size * 2.0))
	b.add_theme_font_size_override("font_size", font_size)
	b.add_theme_color_override("font_color", color)
	b.pressed.connect(handler)
	return b


# ------------------------------------------------------------------ Aktionen

func _on_quick_fight() -> void:
	GameState.mode = "versus"
	GameState.use_custom_player = false
	# Zufaelliger Gegner, aber nicht man selbst.
	var pool: Array[String] = FighterRoster.ids()
	pool.erase(GameState.selected_fighter)
	GameState.opponent_fighter = pool[randi() % pool.size()]
	_go("res://scenes/combat/Arena.tscn")


func _on_arcade() -> void:
	GameState.use_custom_player = false
	GameState.start_arcade()
	_go("res://scenes/combat/Arena.tscn")


func _on_training() -> void:
	GameState.mode = "training"
	GameState.use_custom_player = false
	var pool: Array[String] = FighterRoster.ids()
	pool.erase(GameState.selected_fighter)
	GameState.opponent_fighter = pool[randi() % pool.size()]
	_go("res://scenes/combat/Arena.tscn")


func _on_select() -> void:
	_go("res://scenes/ui/CharacterSelect.tscn")


func _on_options() -> void:
	_go("res://scenes/ui/Options.tscn")


func _on_editor() -> void:
	_go("res://scenes/ui/Editor.tscn")


func _go(path: String) -> void:
	var err: Error = get_tree().change_scene_to_file(path)
	if err != OK:
		push_error("Szenenwechsel fehlgeschlagen: %s (%s)" % [path, error_string(err)])
