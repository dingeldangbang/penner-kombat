extends Control
class_name CharacterSelect

## Kaempferauswahl mit 3D-Vorschau.
##
## Zeigt die eingebaute Riege aus FighterRoster. Die Vorschau ist dieselbe
## Figur, die spaeter in der Arena kaempft -- gebaut mit FighterRoster.build_body(),
## damit Auswahl und Kampf nicht auseinanderlaufen koennen.

const ACCENT := Color(0.85, 0.16, 0.16)

var _index: int = 0
var _roster: Array = []
var _preview_root: Node3D
var _viewport: SubViewport
var _name_label: Label
var _tag_label: Label
var _stats_label: Label
var _spin: float = 0.0


func _ready() -> void:
	set_anchors_preset(Control.PRESET_FULL_RECT)
	_roster = FighterRoster.all()
	_index = maxi(0, FighterRoster.ids().find(GameState.selected_fighter))
	_build()
	_refresh()


func _build() -> void:
	var bg: ColorRect = ColorRect.new()
	bg.set_anchors_preset(Control.PRESET_FULL_RECT)
	bg.color = Color(0.04, 0.03, 0.06)
	bg.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(bg)

	var root: VBoxContainer = VBoxContainer.new()
	root.set_anchors_preset(Control.PRESET_FULL_RECT)
	root.add_theme_constant_override("separation", 8)
	add_child(root)

	var title: Label = Label.new()
	title.text = "KÄMPFER WÄHLEN"
	title.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	title.add_theme_font_size_override("font_size", 40)
	title.add_theme_color_override("font_color", ACCENT)
	title.custom_minimum_size = Vector2(0, 64)
	root.add_child(title)

	# --- 3D-Vorschau -----------------------------------------------------
	var preview_row: HBoxContainer = HBoxContainer.new()
	preview_row.size_flags_vertical = Control.SIZE_EXPAND_FILL
	preview_row.add_theme_constant_override("separation", 4)
	root.add_child(preview_row)

	preview_row.add_child(_arrow("◀", -1))

	var vp_container: SubViewportContainer = SubViewportContainer.new()
	vp_container.stretch = true
	vp_container.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	vp_container.size_flags_vertical = Control.SIZE_EXPAND_FILL
	preview_row.add_child(vp_container)

	_viewport = SubViewport.new()
	_viewport.transparent_bg = true
	_viewport.render_target_update_mode = SubViewport.UPDATE_ALWAYS
	vp_container.add_child(_viewport)

	var world: Node3D = Node3D.new()
	_viewport.add_child(world)

	var cam: Camera3D = Camera3D.new()
	cam.position = Vector3(0, 1.15, 3.1)
	cam.rotation_degrees = Vector3(-8, 0, 0)
	world.add_child(cam)
	cam.make_current()

	var key: DirectionalLight3D = DirectionalLight3D.new()
	key.rotation_degrees = Vector3(-38, -32, 0)
	key.light_energy = 1.4
	world.add_child(key)

	var rim: OmniLight3D = OmniLight3D.new()
	rim.position = Vector3(-1.6, 1.6, -1.4)
	rim.light_color = ACCENT
	rim.light_energy = 2.4
	rim.omni_range = 7.0
	world.add_child(rim)

	_preview_root = Node3D.new()
	world.add_child(_preview_root)

	preview_row.add_child(_arrow("▶", 1))

	# --- Textblock -------------------------------------------------------
	_name_label = Label.new()
	_name_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_name_label.add_theme_font_size_override("font_size", 38)
	root.add_child(_name_label)

	_tag_label = Label.new()
	_tag_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_tag_label.add_theme_font_size_override("font_size", 22)
	_tag_label.add_theme_color_override("font_color", Color(0.72, 0.70, 0.76))
	root.add_child(_tag_label)

	_stats_label = Label.new()
	_stats_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_stats_label.add_theme_font_size_override("font_size", 20)
	_stats_label.add_theme_color_override("font_color", Color(0.62, 0.78, 0.62))
	root.add_child(_stats_label)

	# --- Knopfleiste -----------------------------------------------------
	var row: HBoxContainer = HBoxContainer.new()
	row.alignment = BoxContainer.ALIGNMENT_CENTER
	row.add_theme_constant_override("separation", 16)
	row.custom_minimum_size = Vector2(0, 84)
	root.add_child(row)

	row.add_child(_button("ZURÜCK", func() -> void:
		get_tree().change_scene_to_file("res://scenes/ui/MainMenu.tscn"),
		26, Color(0.7, 0.7, 0.75)))
	row.add_child(_button("AUSWÄHLEN", _choose, 32, ACCENT))


func _arrow(text: String, delta: int) -> Button:
	var b: Button = Button.new()
	b.text = text
	b.custom_minimum_size = Vector2(96, 0)
	b.size_flags_vertical = Control.SIZE_EXPAND_FILL
	b.add_theme_font_size_override("font_size", 44)
	b.pressed.connect(func() -> void: _step(delta))
	return b


func _button(text: String, handler: Callable, font_size: int,
		color: Color) -> Button:
	var b: Button = Button.new()
	b.text = text
	b.custom_minimum_size = Vector2(240, 68)
	b.add_theme_font_size_override("font_size", font_size)
	b.add_theme_color_override("font_color", color)
	b.pressed.connect(handler)
	return b


func _step(delta: int) -> void:
	_index = wrapi(_index + delta, 0, _roster.size())
	_refresh()


func _refresh() -> void:
	var f: FighterRoster.Fighter = _roster[_index] as FighterRoster.Fighter

	for child in _preview_root.get_children():
		child.queue_free()
	var body: Node3D = FighterRoster.build_body(f)
	_preview_root.add_child(body)

	_name_label.text = f.display_name
	_name_label.add_theme_color_override("font_color", f.accent)
	_tag_label.text = f.tagline
	_stats_label.text = "Tempo %.1f   ·   Schlagkraft %.0f   ·   Leben %.0f   ·   %d Combos" % [
		f.move_speed, f.attack_power, f.max_health, f.combos.size()]


func _process(delta: float) -> void:
	if _preview_root:
		_spin += delta * 0.7
		_preview_root.rotation.y = _spin


func _choose() -> void:
	var f: FighterRoster.Fighter = _roster[_index] as FighterRoster.Fighter
	GameState.selected_fighter = f.id
	GameState.use_custom_player = false
	GameState.save_game()
	get_tree().change_scene_to_file("res://scenes/ui/MainMenu.tscn")


func _unhandled_input(event: InputEvent) -> void:
	if event.is_action_pressed("ui_cancel"):
		get_tree().change_scene_to_file("res://scenes/ui/MainMenu.tscn")
	elif event.is_action_pressed("ui_left"):
		_step(-1)
	elif event.is_action_pressed("ui_right"):
		_step(1)
	elif event.is_action_pressed("ui_accept"):
		_choose()
