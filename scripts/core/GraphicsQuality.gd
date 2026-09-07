extends Node
## Globale Grafik-/Performance-Verwaltung (Autoload "GraphicsQuality").
##
## Ziel: "Mortal-Kombat-X"-Look auf Desktop UND eine solide, flüssige
## Darstellung auf Mobilgeräten. Der Manager hält 5 Qualitätsstufen,
## erkennt Mobilgeräte automatisch, skaliert Rendering-Aufwand (MSAA,
## Upscaling, Shadow-Maps, SSAO/SSR/Fog/Glow) und passt bei Bedarf die
## Auflösung dynamisch an (40–60 FPS Regelung, GPU-Schonung).
##
## Steuerung im Kampf:
##   F1 – Qualitätsstufe wechseln
##   F2 – Dynamische Auflösung an/aus
##   F3 – Kino-FX (Grain, Chromatic Aberration, Letterbox) an/aus
##
## Einstellungen werden unter user://graphics.cfg gespeichert.

signal quality_changed(tier: int, name: String)
signal cinematic_fx_changed(enabled: bool)
signal adaptive_resolution_changed(enabled: bool)

const SAVE_PATH: String = "user://graphics.cfg"
const QUALITY_NAMES: Array[String] = ["Niedrig", "Mittel", "Hoch", "Ultra", "Kino"]
const TIER_COUNT: int = 5

const TIER_LOW: int = 0
const TIER_MEDIUM: int = 1
const TIER_HIGH: int = 2
const TIER_ULTRA: int = 3
const TIER_CINEMATIC: int = 4

const DEFAULT_TARGET_FPS: int = 60
const ADAPTIVE_MIN_SCALE: float = 0.6
const ADAPTIVE_STEP: float = 0.05

var _tier: int = TIER_HIGH
var _target_fps: int = DEFAULT_TARGET_FPS
var _vsync: bool = false
var _adaptive: bool = true
var _cinematic_fx: bool = true
var _is_mobile: bool = false

var _current_scale: float = 1.0
var _adapt_timer: float = 0.0
var _env_scan_timer: float = 0.0
var _registered_environments: Array[Environment] = []


func _ready() -> void:
	_is_mobile = OS.get_name() == "Android" or OS.get_name() == "iOS"
	_load_config()
	# Mobile: Standard "Mittel/Hoch" – Kino-Modus bleibt Desktop/Win/Mac/Linux vorbehalten.
	if _is_mobile and _tier > TIER_HIGH:
		_tier = TIER_HIGH
	apply()


func _process(delta: float) -> void:
	# Neue WorldEnvironment/Camera-Environments (z. B. dynamisch gebaute Arenas) aufnehmen.
	_env_scan_timer -= delta
	if _env_scan_timer <= 0.0:
		_env_scan_timer = 2.5
		_refresh_environment_registry()

	# Adaptive Auflösung: FPS im Blick behalten und 3D-Scale sanft anpassen.
	if _adaptive:
		_adapt_timer -= delta
		if _adapt_timer <= 0.0:
			_adapt_timer = 1.25
			_tick_adaptive_resolution()


# ---------------------------------------------------------------------------
#  Öffentliche API
# ---------------------------------------------------------------------------

func get_quality_index() -> int:
	return _tier


func get_quality_name(tier: int = -1) -> String:
	if tier < 0:
		tier = _tier
	return QUALITY_NAMES[clampi(tier, 0, TIER_COUNT - 1)]


func is_mobile() -> bool:
	return _is_mobile


func is_adaptive_enabled() -> bool:
	return _adaptive


func are_cinematic_fx_enabled() -> bool:
	return _cinematic_fx


func set_quality(tier: int, save: bool = true) -> void:
	_tier = clampi(tier, 0, TIER_COUNT - 1)
	# Kino-Stufe auf Mobilgeräten nicht erlauben (zu teuer).
	if _is_mobile and _tier > TIER_HIGH:
		_tier = TIER_HIGH
	# Neue Stufe startet mit der zugehörigen Ziel-Auflösung (adaptive regelt danach).
	_current_scale = _get_configured_scale()
	apply()
	if save:
		_save_config()
	quality_changed.emit(_tier, get_quality_name())


func cycle_quality() -> String:
	set_quality((_tier + 1) % TIER_COUNT)
	return get_quality_name()


func set_target_fps(fps: int) -> void:
	_target_fps = maxi(fps, 24)
	_save_config()
	apply()


func set_vsync(enabled: bool) -> void:
	_vsync = enabled
	_save_config()
	apply()


func set_adaptive(enabled: bool, save: bool = true) -> void:
	_adaptive = enabled
	_current_scale = _get_configured_scale()
	_apply_viewport_settings()
	if save:
		_save_config()
	adaptive_resolution_changed.emit(_adaptive)


func set_cinematic_fx(enabled: bool, save: bool = true) -> void:
	_cinematic_fx = enabled
	if save:
		_save_config()
	cinematic_fx_changed.emit(_cinematic_fx)


func toggle_cinematic_fx() -> bool:
	set_cinematic_fx(not _cinematic_fx)
	return _cinematic_fx


## Wendet alle derzeit gültigen Einstellungen sofort an.
func apply() -> void:
	_apply_engine_settings()
	_apply_shadow_settings()
	_apply_viewport_settings()
	_refresh_environment_registry()
	_apply_registered_environments()


## Neu erstellte Environments (Arena-Builder, PostProcessing) registrieren.
func refresh() -> void:
	_refresh_environment_registry()
	_apply_registered_environments()


# ---------------------------------------------------------------------------
#  Qualitätsstufen → konkrete Werte
# ---------------------------------------------------------------------------

func _get_configured_scale() -> float:
	match _tier:
		TIER_LOW:
			return 0.66
		TIER_MEDIUM:
			return 0.75
		TIER_HIGH:
			return 0.85
		TIER_ULTRA:
			return 1.0
		_:
			return 1.0


func _get_shadow_atlas_size() -> int:
	match _tier:
		TIER_LOW:
			return 1024
		TIER_MEDIUM:
			return 2048
		TIER_HIGH:
			return 4096
		_:
			return 4096


func _get_msaa() -> int:
	match _tier:
		TIER_LOW:
			return Viewport.MSAA_DISABLED
		TIER_MEDIUM:
			return Viewport.MSAA_DISABLED if _is_mobile else Viewport.MSAA_2X
		TIER_HIGH:
			return Viewport.MSAA_2X if _is_mobile else Viewport.MSAA_4X
		TIER_ULTRA:
			return Viewport.MSAA_4X
		_:
			return Viewport.MSAA_4X


func _get_shadow_quality() -> int:
	match _tier:
		TIER_LOW:
			return RenderingServer.SHADOW_QUALITY_HARD
		TIER_MEDIUM:
			return RenderingServer.SHADOW_QUALITY_SOFT_LOW
		TIER_HIGH:
			return RenderingServer.SHADOW_QUALITY_SOFT_MEDIUM
		_:
			return RenderingServer.SHADOW_QUALITY_SOFT_HIGH


# ---------------------------------------------------------------------------
#  Anwendung
# ---------------------------------------------------------------------------

func _apply_engine_settings() -> void:
	Engine.max_fps = _target_fps
	DisplayServer.window_set_vsync_mode(
		DisplayServer.VSYNC_ENABLED if _vsync else DisplayServer.VSYNC_DISABLED)


func _apply_shadow_settings() -> void:
	var atlas_size: int = _get_shadow_atlas_size()
	var quality: int = _get_shadow_quality()
	if RenderingServer.has_method("directional_shadow_atlas_set_size"):
		RenderingServer.directional_shadow_atlas_set_size(atlas_size, false)
	if RenderingServer.has_method("positional_shadow_atlas_set_size"):
		RenderingServer.positional_shadow_atlas_set_size(atlas_size, false)
	if RenderingServer.has_method("directional_shadow_quality_set"):
		RenderingServer.directional_shadow_quality_set(quality)
	if RenderingServer.has_method("directional_soft_shadow_filter_set_quality"):
		RenderingServer.directional_soft_shadow_filter_set_quality(quality)


func _apply_viewport_settings() -> void:
	var viewport: Viewport = get_viewport()
	if viewport == null:
		return
	viewport.msaa_3d = _get_msaa()
	viewport.msaa_2d = Viewport.MSAA_DISABLED
	viewport.screen_space_aa = Viewport.SCREEN_SPACE_AA_DISABLED if _tier <= TIER_MEDIUM else Viewport.SCREEN_SPACE_AA_FXAA
	viewport.use_debanding = _tier >= TIER_ULTRA

	# TAA erst ab Ultra (mobil nicht anbieten).
	if viewport.get("use_taa") != null:
		viewport.use_taa = _tier >= TIER_ULTRA and not _is_mobile

	# Upscaling: FSR ab Hoch, Bilinear bei Niedrig/Mittel, TSR nur Desktop-Ultra.
	if viewport.get("scaling_3d_mode") != null:
		var mode: int = Viewport.SCALING_3D_MODE_BILINEAR
		if _tier >= TIER_HIGH or _is_mobile:
			mode = Viewport.SCALING_3D_MODE_FSR
		if _tier >= TIER_ULTRA and not _is_mobile:
			mode = Viewport.SCALING_3D_MODE_TSR
		viewport.scaling_3d_mode = mode
		viewport.scaling_3d_scale = _current_scale if _adaptive else _get_configured_scale()
		if viewport.get("scaling_3d_sharpness") != null:
			viewport.scaling_3d_sharpness = 0.55 if mode == Viewport.SCALING_3D_MODE_BILINEAR else 0.9


func _refresh_environment_registry() -> void:
	var found: Array[Environment] = []
	_collect_environments(get_tree().root, found)
	_registered_environments = found


func _collect_environments(node: Node, out: Array[Environment]) -> void:
	if node == null:
		return
	if node is WorldEnvironment:
		var world_env: WorldEnvironment = node as WorldEnvironment
		if world_env.environment and not out.has(world_env.environment):
			out.append(world_env.environment)
	if node is Camera3D:
		var camera: Camera3D = node as Camera3D
		if camera.environment and not out.has(camera.environment):
			out.append(camera.environment)
	for child in node.get_children():
		_collect_environments(child, out)


func _apply_registered_environments() -> void:
	for env in _registered_environments:
		if env and is_instance_valid(env):
			_apply_environment(env)


func _apply_environment(env: Environment) -> void:
	# Gemeinsame Basis (alle Stufen).
	env.tonemap_mode = Environment.TONE_MAPPER_ACES
	env.tonemap_exposure = 1.05
	env.adjustment_enabled = true
	env.adjustment_saturation = 1.08
	env.adjustment_contrast = 1.18
	env.adjustment_brightness = 1.0

	match _tier:
		TIER_LOW:
			env.glow_enabled = false
			env.ssao_enabled = false
			env.ssr_enabled = false
			env.volumetric_fog_enabled = false
			env.fog_enabled = false
		TIER_MEDIUM:
			env.glow_enabled = true
			env.glow_intensity = 0.6
			env.glow_hdr_threshold = 1.0
			env.ssao_enabled = false
			env.ssr_enabled = false
			env.volumetric_fog_enabled = false
			env.fog_enabled = true
			env.fog_density = 0.005
		TIER_HIGH:
			env.glow_enabled = true
			env.glow_intensity = 0.95
			env.glow_hdr_threshold = 0.9
			env.ssao_enabled = not _is_mobile
			env.ssao_radius = 1.1
			env.ssao_intensity = 1.4
			env.ssr_enabled = false
			env.volumetric_fog_enabled = false
			env.fog_enabled = true
			env.fog_density = 0.008
		TIER_ULTRA:
			env.glow_enabled = true
			env.glow_intensity = 1.25
			env.glow_hdr_threshold = 0.85
			env.ssao_enabled = true
			env.ssao_radius = 1.5
			env.ssao_intensity = 1.8
			env.ssr_enabled = true
			env.ssr_max_steps = 32
			env.volumetric_fog_enabled = true
			env.volumetric_fog_density = 0.012
			env.fog_enabled = true
			env.fog_density = 0.01
		_:
			env.glow_enabled = true
			env.glow_intensity = 1.6
			env.glow_hdr_threshold = 0.8
			env.ssao_enabled = true
			env.ssao_radius = 2.0
			env.ssao_intensity = 2.2
			env.ssr_enabled = true
			env.ssr_max_steps = 64
			env.volumetric_fog_enabled = true
			env.volumetric_fog_density = 0.018
			env.fog_enabled = true
			env.fog_density = 0.014
			env.fog_light_color = Color(0.55, 0.25, 0.2)


func _tick_adaptive_resolution() -> void:
	var fps: float = Engine.get_frames_per_second()
	var viewport: Viewport = get_viewport()
	var mode_set: bool = viewport != null and viewport.get("scaling_3d_mode") != null
	if not mode_set:
		return
	var configured: float = _get_configured_scale()
	if fps > 0.0 and fps < float(_target_fps) - 8.0:
		_current_scale = maxf(ADAPTIVE_MIN_SCALE, _current_scale - ADAPTIVE_STEP)
		_apply_scale()
	elif fps > float(_target_fps) + 5.0:
		_current_scale = minf(configured, _current_scale + ADAPTIVE_STEP)
		_apply_scale()


func _apply_scale() -> void:
	var viewport: Viewport = get_viewport()
	if viewport != null and viewport.get("scaling_3d_mode") != null:
		viewport.scaling_3d_scale = _current_scale


# ---------------------------------------------------------------------------
#  Persistenz
# ---------------------------------------------------------------------------

func _load_config() -> void:
	var config: ConfigFile = ConfigFile.new()
	if config.load(SAVE_PATH) != OK:
		# Standardwerte: mobil konservativ, Desktop ambitioniert.
		_tier = TIER_MEDIUM if _is_mobile else TIER_HIGH
		_target_fps = 60 if _is_mobile else 120
		_vsync = _is_mobile
		_adaptive = true
		_cinematic_fx = true
	else:
		_tier = clampi(int(config.get_value("graphics", "tier", _tier)), 0, TIER_COUNT - 1)
		_target_fps = maxi(int(config.get_value("graphics", "target_fps", _target_fps)), 24)
		_vsync = bool(config.get_value("graphics", "vsync", _vsync))
		_adaptive = bool(config.get_value("graphics", "adaptive", true))
		_cinematic_fx = bool(config.get_value("graphics", "cinematic_fx", true))
	_current_scale = _get_configured_scale()


func _save_config() -> void:
	var config: ConfigFile = ConfigFile.new()
	config.set_value("graphics", "tier", _tier)
	config.set_value("graphics", "target_fps", _target_fps)
	config.set_value("graphics", "vsync", _vsync)
	config.set_value("graphics", "adaptive", _adaptive)
	config.set_value("graphics", "cinematic_fx", _cinematic_fx)
	config.save(SAVE_PATH)
