extends Node
class_name PerformanceManager

## Mobile performance optimizer for frame pacing, shadows and resolution presets.

enum Quality {
	LOW,
	MEDIUM,
	HIGH
}

@export var target_fps: int = 60
@export var use_vsync: bool = false
@export var quality_preset: Quality = Quality.HIGH
@export var apply_only_on_mobile: bool = true


func _ready() -> void:
	_apply_performance_settings()
	_setup_fps_limit()


func _apply_performance_settings() -> void:
	var is_mobile: bool = OS.get_name() == "Android" or OS.get_name() == "iOS"
	if apply_only_on_mobile and not is_mobile:
		return
	match quality_preset:
		Quality.LOW:
			_set_low_quality()
		Quality.MEDIUM:
			_set_medium_quality()
		Quality.HIGH:
			_set_high_quality()


func _set_low_quality() -> void:
	DisplayServer.window_set_vsync_mode(DisplayServer.VSYNC_DISABLED)
	DisplayServer.window_set_size(Vector2i(1280, 720))
	ProjectSettings.set_setting("rendering/textures/vram_compression/import_etc2_astc", true)
	ProjectSettings.set_setting("rendering/lights_and_shadows/directional_shadow/size", 1024)
	ProjectSettings.set_setting("rendering/lights_and_shadows/positional_shadow/atlas_size", 1024)


func _set_medium_quality() -> void:
	DisplayServer.window_set_vsync_mode(DisplayServer.VSYNC_DISABLED if not use_vsync else DisplayServer.VSYNC_ENABLED)
	DisplayServer.window_set_size(Vector2i(1600, 900))
	ProjectSettings.set_setting("rendering/lights_and_shadows/directional_shadow/size", 2048)
	ProjectSettings.set_setting("rendering/lights_and_shadows/positional_shadow/atlas_size", 2048)


func _set_high_quality() -> void:
	DisplayServer.window_set_vsync_mode(DisplayServer.VSYNC_DISABLED if not use_vsync else DisplayServer.VSYNC_ENABLED)
	ProjectSettings.set_setting("rendering/lights_and_shadows/directional_shadow/size", 4096)
	ProjectSettings.set_setting("rendering/lights_and_shadows/positional_shadow/atlas_size", 4096)


func _setup_fps_limit() -> void:
	Engine.max_fps = max(target_fps, 0)
	DisplayServer.window_set_vsync_mode(DisplayServer.VSYNC_ENABLED if use_vsync else DisplayServer.VSYNC_DISABLED)


func set_quality(preset: Quality) -> void:
	quality_preset = preset
	_apply_performance_settings()


func set_target_fps(fps: int) -> void:
	target_fps = fps
	_setup_fps_limit()
