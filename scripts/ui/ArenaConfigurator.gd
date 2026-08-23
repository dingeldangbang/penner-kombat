extends VBoxContainer
class_name ArenaConfigurator

## Arena Konfigurator – Stage-Auswahl, Beleuchtung, Props, Zerstörung

@onready var arena_select: OptionButton = $ArenaSelect
@onready var light_energy: HSlider = $LightEnergy
@onready var light_color: ColorPickerButton = $LightColor
@onready var enable_destruction: CheckBox = $EnableDestruction
@onready var prop_density: HSlider = $PropDensity
@onready var apply_btn: Button = $ApplyBtn

signal arena_changed(arena_name: String, config: Dictionary)

var _current_config: Dictionary = {}

func _ready() -> void:
	_populate_arenas()
	_connect_signals()
	_load_default_config()

func _populate_arenas() -> void:
	arena_select.clear()
	arena_select.add_item("Kiez-Hinterhof")
	arena_select.add_item("U-Bahn Station")
	arena_select.add_item("Schrottplatz")
	arena_select.add_item("Keller-Rave")
	arena_select.add_item("Dachgarten")

func _connect_signals() -> void:
	apply_btn.pressed.connect(_apply_config)
	arena_select.item_selected.connect(_on_arena_selected)

func _load_default_config() -> void:
	_current_config = {
		"arena": "Kiez-Hinterhof",
		"light_energy": 1.2,
		"light_color": Color(1.0, 0.95, 0.8),
		"destruction": true,
		"prop_density": 0.7
	}
	light_energy.value = _current_config.light_energy
	light_color.color = _current_config.light_color
	enable_destruction.button_pressed = _current_config.destruction
	prop_density.value = _current_config.prop_density

func _on_arena_selected(index: int) -> void:
	_current_config.arena = arena_select.get_item_text(index)

func _apply_config() -> void:
	_current_config.light_energy = light_energy.value
	_current_config.light_color = light_color.color
	_current_config.destruction = enable_destruction.button_pressed
	_current_config.prop_density = prop_density.value
	
	arena_changed.emit(_current_config.arena, _current_config)
	print("[ArenaConfigurator] Config applied: ", _current_config)

func get_config() -> Dictionary:
	return _current_config.duplicate(true)