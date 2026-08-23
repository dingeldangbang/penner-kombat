extends Node3D
class_name ArenaLighting

## Dynamic arena atmosphere using WorldEnvironment, DirectionalLight3D and optional fog.

@export var ambient_color: Color = Color(0.1, 0.08, 0.12)
@export var sun_color: Color = Color(0.8, 0.7, 0.6)
@export var sun_angle_degrees: Vector3 = Vector3(-45, 45, 0)
@export var enable_fog: bool = true
@export var fog_density: float = 0.015

var _directional_light: DirectionalLight3D
var _world_environment: WorldEnvironment
var _fog_volume: FogVolume


func _ready() -> void:
	_setup_lighting()


func _setup_lighting() -> void:
	_world_environment = WorldEnvironment.new()
	_world_environment.name = "ArenaWorldEnvironment"
	var env: Environment = Environment.new()
	env.background_mode = Environment.BG_COLOR
	env.background_color = ambient_color
	env.ambient_light_source = Environment.AMBIENT_SOURCE_COLOR
	env.ambient_light_color = ambient_color
	env.ambient_light_energy = 0.65
	env.glow_enabled = true
	env.glow_intensity = 0.25
	_world_environment.environment = env
	add_child(_world_environment)

	_directional_light = DirectionalLight3D.new()
	_directional_light.name = "ArenaDirectionalLight"
	_directional_light.light_color = sun_color
	_directional_light.rotation_degrees = sun_angle_degrees
	_directional_light.light_energy = 1.4
	_directional_light.shadow_enabled = true
	_directional_light.directional_shadow_mode = DirectionalLight3D.SHADOW_PARALLEL_4_SPLITS
	add_child(_directional_light)

	if enable_fog:
		_fog_volume = FogVolume.new()
		_fog_volume.name = "ArenaFog"
		_fog_volume.size = Vector3(30, 8, 30)
		_fog_volume.position = Vector3(0, 2, 0)
		_fog_volume.material = _create_fog_material()
		add_child(_fog_volume)


func _create_fog_material() -> FogMaterial:
	var material: FogMaterial = FogMaterial.new()
	material.albedo = Color(0.05, 0.04, 0.06)
	material.density = fog_density
	return material


func set_time_of_day(hours: float) -> void:
	if not _directional_light:
		return
	var normalized: float = wrapf(hours, 0.0, 24.0) / 24.0
	_directional_light.rotation_degrees = Vector3(-15.0 - sin(normalized * TAU) * 65.0, 45.0 + normalized * 180.0, 0.0)
	var brightness: float = clampf(sin(normalized * PI), 0.0, 1.0)
	_directional_light.light_energy = 0.25 + brightness * 1.6
	if _world_environment and _world_environment.environment:
		_world_environment.environment.ambient_light_energy = 0.25 + brightness * 0.55


func set_combat_alert(enabled: bool) -> void:
	if _directional_light:
		_directional_light.light_color = Color(1.0, 0.22, 0.18) if enabled else sun_color
		_directional_light.light_energy = 2.0 if enabled else 1.4
