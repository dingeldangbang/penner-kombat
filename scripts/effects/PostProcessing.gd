extends Node
class_name PostProcessing

## Godot 4 post-processing controller: glow, tonemap, color adjustment, vignette overlay and slow-motion.

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


func _ready() -> void:
	if camera == null and get_viewport():
		camera = get_viewport().get_camera_3d()
	_setup_environment()
	_setup_vignette()


func set_camera(new_camera: Camera3D) -> void:
	camera = new_camera
	_setup_environment()


func _setup_environment() -> void:
	if camera == null:
		return
	_environment = Environment.new()
	_environment.background_mode = Environment.BG_COLOR
	_environment.background_color = Color(0.08, 0.06, 0.1)
	_environment.glow_enabled = true
	_environment.glow_intensity = base_bloom
	_environment.glow_hdr_threshold = 0.85
	_environment.tonemap_mode = Environment.TONE_MAPPER_ACES
	_environment.tonemap_exposure = 1.0
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
void fragment() {
	vec2 centered = UV - vec2(0.5);
	float d = length(centered) * 1.45;
	float v = smoothstep(0.35, 0.92, d) * intensity;
	COLOR = vec4(tint.rgb, v);
}
"""
	_vignette_material.shader = shader
	_vignette_material.set_shader_parameter("intensity", base_vignette)
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
	ScreenShake.shake(0.5, 1.2)
	await get_tree().create_timer(2.0, true, false, true).timeout
	_reset_effects()


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
