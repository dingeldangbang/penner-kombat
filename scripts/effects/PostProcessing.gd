extends Node
class_name PostProcessing

## Godot 4 Post-Processing-Controller (MKX-Stil):
## ACES-Tonemapping, Glow, Color-Grading, Vignette, Film-Grain,
## Chromatic Aberration, Letterbox-Balken und Treffer-Flash.
## Alle Effekte laufen in EINEM Canvas-Shader (mobil-freundlich).

@export var camera: Camera3D
@export var base_bloom: float = 0.45
@export var base_vignette: float = 0.25

var _environment: Environment
var _vignette_layer: CanvasLayer
var _vignette_rect: ColorRect
var _vignette_material: ShaderMaterial
var _default_saturation: float = 1.05
var _default_contrast: float = 1.15
var _default_brightness: float = 1.0

var _cinematic_fx: bool = true
var _grain_strength: float = 0.12
var _aberration: float = 0.0018
var _letterbox: float = 0.0
var _flash: float = 0.0
var _flash_decay: float = 5.0


func _ready() -> void:
	if camera == null and get_viewport():
		camera = get_viewport().get_camera_3d()
	_setup_environment()
	_setup_vignette()
	# Grafikeinstellungen des globalen Managers übernehmen, falls vorhanden.
	var graphics: Node = get_node_or_null("/root/GraphicsQuality")
	if graphics != null and graphics.has_method("are_cinematic_fx_enabled"):
		set_cinematic_fx(bool(graphics.are_cinematic_fx_enabled()))


func set_camera(new_camera: Camera3D) -> void:
	camera = new_camera
	_setup_environment()


func set_cinematic_fx(enabled: bool) -> void:
	_cinematic_fx = enabled
	if not _cinematic_fx:
		_flash = 0.0


func are_cinematic_fx_enabled() -> bool:
	return _cinematic_fx


func _setup_environment() -> void:
	if camera == null:
		return
	_environment = Environment.new()
	_environment.background_mode = Environment.BG_COLOR
	_environment.background_color = Color(0.05, 0.04, 0.08)
	_environment.glow_enabled = true
	_environment.glow_intensity = base_bloom
	_environment.glow_hdr_threshold = 0.85
	_environment.tonemap_mode = Environment.TONE_MAPPER_ACES
	_environment.tonemap_exposure = 1.05
	_environment.adjustment_enabled = true
	_environment.adjustment_saturation = _default_saturation
	_environment.adjustment_contrast = _default_contrast
	_environment.adjustment_brightness = _default_brightness
	camera.environment = _environment


func _setup_vignette() -> void:
	if _vignette_layer:
		return
	_vignette_layer = CanvasLayer.new()
	_vignette_layer.layer = 90
	_vignette_rect = ColorRect.new()
	_vignette_rect.set_anchors_preset(Control.PRESET_FULL_RECT)
	_vignette_rect.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_vignette_material = ShaderMaterial.new()
	var shader: Shader = Shader.new()
	shader.code = """
shader_type canvas_item;

uniform float intensity = 0.25;
uniform vec4 tint : source_color = vec4(0.35, 0.0, 0.0, 1.0);
uniform float grain_strength = 0.12;
uniform float aberration = 0.0018;
uniform float flash = 0.0;
uniform vec4 flash_color : source_color = vec4(1.0, 0.92, 0.8, 1.0);
uniform float letterbox = 0.0;

float hash01(vec2 p) {
	return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453123);
}

void fragment() {
	vec2 centered = SCREEN_UV - vec2(0.5);
	float d = length(centered) * 1.45;
	float v = smoothstep(0.35, 0.92, d) * intensity;

	// Chromatic Aberration: Farbkanäle radial leicht versetzen
	vec2 dir = normalize(centered + vec2(0.0001));
	vec2 offset = dir * aberration * (0.25 + d * d);
	vec4 tex = texture(SCREEN_TEXTURE, SCREEN_UV + offset);
	vec4 tex_r = texture(SCREEN_TEXTURE, SCREEN_UV - offset * 0.6);

	float alpha = v + flash * 0.45;
	vec3 col = mix(tex_r.rgb, tex.rgb, 0.5);
	col = mix(vec3(0.0), col, 1.0 - v * 0.35);

	// Treffer-Flash (weiß-warm)
	col = mix(col, flash_color.rgb, clamp(flash, 0.0, 1.0) * 0.55);

	// Letterbox-Kino-Balken
	float bar = smoothstep(0.055, 0.07, min(SCREEN_UV.y, 1.0 - SCREEN_UV.y));
	float lb = (1.0 - bar) * letterbox;
	col = mix(col, vec3(0.0), clamp(lb, 0.0, 1.0));
	alpha = max(alpha, lb * 0.98);

	// Vignette-Tönung
	col = mix(col, tint.rgb, v * 0.30);

	// Film-Grain
	float g = hash01(floor(SCREEN_UV * (1.0 / max(SCREEN_PIXEL_SIZE.x, 0.0001)) + vec2(TIME * 61.0, TIME * 47.0)));
	col += (g - 0.5) * grain_strength * (0.5 + v);

	COLOR = vec4(col, alpha);
}
"""
	_vignette_material.shader = shader
	_vignette_material.set_shader_parameter("intensity", base_vignette)
	_vignette_material.set_shader_parameter("grain_strength", _grain_strength)
	_vignette_material.set_shader_parameter("aberration", _aberration)
	_vignette_rect.material = _vignette_material
	_vignette_layer.add_child(_vignette_rect)
	get_tree().root.call_deferred("add_child", _vignette_layer)


func apply_combo_effects(combo: int) -> void:
	if _environment == null:
		return
	var intensity: float = clampf(float(combo) / 20.0, 0.0, 1.0)
	_environment.glow_intensity = lerpf(base_bloom, 2.5, intensity)
	_environment.adjustment_contrast = lerpf(_default_contrast, 1.45, intensity)
	_environment.adjustment_saturation = lerpf(_default_saturation, 0.85, intensity)
	_environment.adjustment_brightness = lerpf(_default_brightness, 0.92, intensity)
	if _vignette_material:
		_vignette_material.set_shader_parameter("intensity", lerpf(base_vignette, 0.85, intensity))


func apply_hit_flash(amount: float) -> void:
	_flash = clampf(maxf(_flash, amount), 0.0, 1.0)


func set_letterbox(amount: float) -> void:
	_letterbox = clampf(amount, 0.0, 1.0)


func _process(delta: float) -> void:
	if _flash > 0.0:
		_flash = maxf(0.0, _flash - _flash_decay * delta)
	if _vignette_material:
		_vignette_material.set_shader_parameter("flash", _flash * (1.0 if _cinematic_fx else 0.5))
		_vignette_material.set_shader_parameter("grain_strength", _grain_strength if _cinematic_fx else 0.0)
		_vignette_material.set_shader_parameter("aberration", _aberration if _cinematic_fx else 0.0)
		_vignette_material.set_shader_parameter("letterbox", _letterbox if _cinematic_fx else 0.0)


func activate_slow_mo(duration: float = 1.5, scale: float = 0.2) -> void:
	var previous_scale: float = Engine.time_scale
	Engine.time_scale = clampf(scale, 0.02, 1.0)
	await get_tree().create_timer(duration, true, false, true).timeout
	Engine.time_scale = previous_scale


func activate_fatality_effects() -> void:
	if _environment:
		_environment.glow_intensity = 2.2
		_environment.adjustment_saturation = 0.75
		_environment.adjustment_contrast = 1.55
		_environment.adjustment_brightness = 0.78
	if _vignette_material:
		_vignette_material.set_shader_parameter("intensity", 0.95)
	set_letterbox(0.42)
	ScreenShake.shake(0.5, 1.2)
	await get_tree().create_timer(2.0, true, false, true).timeout
	_reset_effects()
	set_letterbox(0.0)


func activate_xray_effects() -> void:
	if _environment:
		_environment.adjustment_saturation = 0.0
		_environment.glow_intensity = 1.6
	RenderingServer.global_shader_parameter_set("xray_uniform", Color(1.0, 0.0, 0.0, 0.0))
	await get_tree().create_timer(2.0, true, false, true).timeout
	RenderingServer.global_shader_parameter_set("xray_uniform", Color(0.0, 0.0, 0.0, 0.0))
	_reset_effects()


func _reset_effects() -> void:
	if _environment:
		_environment.glow_intensity = base_bloom
		_environment.adjustment_saturation = _default_saturation
		_environment.adjustment_contrast = _default_contrast
		_environment.adjustment_brightness = _default_brightness
	if _vignette_material:
		_vignette_material.set_shader_parameter("intensity", base_vignette)
