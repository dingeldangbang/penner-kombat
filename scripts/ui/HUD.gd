extends CanvasLayer
class_name HUD

## Full combat HUD with animated bars, combo counter, status icons and damage popups.

@onready var health_bar_p1: ProgressBar = $Root/HealthBars/P1/HealthBar
@onready var health_bar_p2: ProgressBar = $Root/HealthBars/P2/HealthBar
@onready var combo_counter: Label = $Root/ComboCounter/ComboLabel
@onready var combo_animation: AnimationPlayer = $Root/ComboCounter/AnimationPlayer
@onready var timer_label: Label = $Root/Timer/TimerLabel
@onready var round_label: Label = $Root/Round/RoundLabel
@onready var status_icons: HBoxContainer = $Root/StatusIcons
@onready var damage_popup: Control = $Root/DamagePopup
@onready var mojo_bar: ProgressBar = $Root/SpecialBars/MojoPanel/MojoBar
@onready var pulse_bar: ProgressBar = $Root/SpecialBars/PulsePanel/PulseBar
@onready var grease_bar: ProgressBar = $Root/SpecialBars/GreasePanel/GreaseBar

var _combo_count: int = 0
var _is_combo_active: bool = false
var _combo_timer: float = 0.0
var _combo_timeout: float = 2.0


func _ready() -> void:
	_setup_damage_popup()
	_reset_combo()
	update_health(100, 100, 100)
	update_timer(99.0)
	update_round(1, 3)


func _process(delta: float) -> void:
	if _is_combo_active:
		_combo_timer -= delta
		if _combo_timer <= 0.0:
			_reset_combo()


func update_health(health_p1: float, health_p2: float, max_health: float = 100.0,
		max_health_p2: float = -1.0) -> void:
	# Die Kaempfer der Riege haben unterschiedliche Lebenspunkte (70 bis 150).
	# Mit einem gemeinsamen Maximum zeigte der Balken des schwaecheren Kaempfers
	# Unsinn an. max_health_p2 < 0 heisst "wie P1" (alte Aufrufe bleiben gueltig).
	var safe_max_p1: float = maxf(max_health, 0.001)
	var safe_max_p2: float = safe_max_p1 if max_health_p2 < 0.0 else maxf(max_health_p2, 0.001)
	health_bar_p1.value = health_p1 / safe_max_p1 * 100.0
	health_bar_p2.value = health_p2 / safe_max_p2 * 100.0
	_update_health_color(health_bar_p1, health_p1, safe_max_p1)
	_update_health_color(health_bar_p2, health_p2, safe_max_p2)


func _update_health_color(bar: ProgressBar, health: float, max_health: float) -> void:
	var percentage: float = health / max_health
	var style: StyleBoxFlat = bar.get_theme_stylebox("fill").duplicate() as StyleBoxFlat
	if style:
		if percentage > 0.5:
			style.bg_color = Color(0.2, 0.8, 0.2)
		elif percentage > 0.25:
			style.bg_color = Color(0.9, 0.8, 0.15)
		else:
			style.bg_color = Color(0.9, 0.15, 0.12)
		bar.add_theme_stylebox_override("fill", style)


func update_combo(combo: int) -> void:
	_combo_count = combo
	_combo_timer = _combo_timeout
	_is_combo_active = combo > 0
	if combo <= 0:
		_reset_combo()
		return

	combo_counter.text = str(combo) + " KOMBO!"
	combo_counter.pivot_offset = combo_counter.size * 0.5
	if combo >= 10:
		combo_counter.modulate = Color(1.0, 0.2, 0.2)
		_play_combo_tween(1.55)
		ScreenShake.shake(0.05, 0.2)
	elif combo >= 5:
		combo_counter.modulate = Color(1.0, 0.84, 0.0)
		_play_combo_tween(1.32)
	else:
		combo_counter.modulate = Color.WHITE
		_play_combo_tween(1.15)


func _play_combo_tween(scale_peak: float) -> void:
	if combo_animation and combo_animation.has_animation("combo_normal"):
		combo_animation.play("combo_normal")
		return
	var tween: Tween = create_tween()
	combo_counter.scale = Vector2.ONE
	tween.tween_property(combo_counter, "scale", Vector2.ONE * scale_peak, 0.08).set_trans(Tween.TRANS_BACK).set_ease(Tween.EASE_OUT)
	tween.tween_property(combo_counter, "scale", Vector2.ONE, 0.14)


func _reset_combo() -> void:
	_combo_count = 0
	_is_combo_active = false
	combo_counter.text = ""
	combo_counter.scale = Vector2.ONE


func update_timer(time: float) -> void:
	var minutes: int = int(floor(time / 60.0))
	var seconds: int = int(floor(fmod(time, 60.0)))
	timer_label.text = "%02d:%02d" % [minutes, seconds]
	timer_label.modulate = Color(1.0, 0.2, 0.2) if time < 10.0 else Color.WHITE


func update_round(current: int, max_rounds: int) -> void:
	round_label.text = "Runde %d von %d" % [current, max_rounds]


func _setup_damage_popup() -> void:
	damage_popup.visible = false


func show_damage(amount: float, screen_position: Vector2, color: Color = Color(1.0, 0.2, 0.2)) -> void:
	var label: Label = damage_popup.get_node("DamageLabel") as Label
	damage_popup.global_position = screen_position
	label.text = "-%d" % int(round(amount))
	label.modulate = color
	damage_popup.visible = true
	var tween: Tween = create_tween()
	tween.tween_property(damage_popup, "position:y", damage_popup.position.y - 50.0, 0.8)
	tween.parallel().tween_property(label, "modulate:a", 0.0, 0.8)
	await tween.finished
	if is_instance_valid(damage_popup):
		damage_popup.visible = false
		label.modulate.a = 1.0


func add_status_icon(icon: Texture2D, tooltip: String = "") -> void:
	var icon_node: TextureRect = TextureRect.new()
	icon_node.texture = icon
	icon_node.tooltip_text = tooltip
	icon_node.custom_minimum_size = Vector2(32, 32)
	icon_node.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
	icon_node.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
	status_icons.add_child(icon_node)


func clear_status_icons() -> void:
	for child in status_icons.get_children():
		child.queue_free()


func update_mojo(amount: int, max_amount: int = 7) -> void:
	var safe_max: int = maxi(max_amount, 1)
	mojo_bar.value = float(amount) / float(safe_max) * 100.0
	(mojo_bar.get_node("Label") as Label).text = "MOJO: %d/%d" % [amount, safe_max]
	mojo_bar.modulate = Color(1.0, 0.84, 0.0) if amount >= safe_max else Color.WHITE


func update_pulse(bpm: float, max_bpm: float = 220.0) -> void:
	pulse_bar.value = bpm / maxf(max_bpm, 1.0) * 100.0
	(pulse_bar.get_node("Label") as Label).text = "PULS: %d bpm" % int(bpm)
	if bpm >= 200.0:
		pulse_bar.modulate = Color.RED
	elif bpm >= 160.0:
		pulse_bar.modulate = Color(1.0, 0.5, 0.0)
	elif bpm >= 100.0:
		pulse_bar.modulate = Color.YELLOW
	else:
		pulse_bar.modulate = Color(0.2, 0.8, 0.2)


func update_grease(charges: int, max_charges: int = 3) -> void:
	var safe_max: int = maxi(max_charges, 1)
	grease_bar.value = float(charges) / float(safe_max) * 100.0
	(grease_bar.get_node("Label") as Label).text = "FETT: %d/%d" % [charges, safe_max]
	if charges <= 0:
		grease_bar.modulate = Color(0.5, 0.5, 0.5)
	elif charges == 1:
		grease_bar.modulate = Color(0.8, 0.6, 0.0)
	else:
		grease_bar.modulate = Color(1.0, 0.8, 0.0)


func show_match_result(winner: String) -> void:
	var overlay: ColorRect = ColorRect.new()
	overlay.name = "MatchResultOverlay"
	overlay.set_anchors_preset(Control.PRESET_FULL_RECT)
	overlay.color = Color(0.02, 0.0, 0.0, 0.65)
	$Root.add_child(overlay)

	var center: CenterContainer = CenterContainer.new()
	center.set_anchors_preset(Control.PRESET_FULL_RECT)
	overlay.add_child(center)

	var label: Label = Label.new()
	label.text = winner + " SIEGT!"
	label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	label.add_theme_font_size_override("font_size", 72)
	label.modulate = Color(1.0, 0.15, 0.1, 1.0)
	center.add_child(label)

	var tween: Tween = create_tween()
	label.scale = Vector2.ZERO
	tween.tween_property(label, "scale", Vector2.ONE * 1.15, 0.25).set_trans(Tween.TRANS_BACK).set_ease(Tween.EASE_OUT)
	tween.tween_property(label, "scale", Vector2.ONE, 0.2)


func set_fighter_names(p1_name: String, p2_name: String) -> void:
	var l1: Label = get_node_or_null("Root/HealthBars/P1/P1Label") as Label
	var l2: Label = get_node_or_null("Root/HealthBars/P2/P2Label") as Label
	if l1:
		l1.text = p1_name
	if l2:
		l2.text = p2_name


func announce(text: String, hold: float = 1.1) -> void:
	"""Kurzer Einblender in der Bildmitte (Rundenstart, K.O., Arcade-Fortschritt)."""
	var existing: Node = $Root.get_node_or_null("Announcer")
	if existing:
		existing.queue_free()

	var center: CenterContainer = CenterContainer.new()
	center.name = "Announcer"
	center.set_anchors_preset(Control.PRESET_FULL_RECT)
	center.mouse_filter = Control.MOUSE_FILTER_IGNORE
	$Root.add_child(center)

	var label: Label = Label.new()
	label.text = text
	label.add_theme_font_size_override("font_size", 64)
	label.add_theme_color_override("font_color", Color(1.0, 0.85, 0.25))
	label.add_theme_color_override("font_outline_color", Color(0, 0, 0))
	label.add_theme_constant_override("outline_size", 10)
	center.add_child(label)

	label.scale = Vector2(0.6, 0.6)
	label.modulate.a = 0.0
	var tween: Tween = create_tween()
	tween.tween_property(label, "modulate:a", 1.0, 0.15)
	tween.parallel().tween_property(label, "scale", Vector2.ONE, 0.25) \
		.set_trans(Tween.TRANS_BACK).set_ease(Tween.EASE_OUT)
	tween.tween_interval(hold)
	tween.tween_property(label, "modulate:a", 0.0, 0.3)
	tween.tween_callback(center.queue_free)
