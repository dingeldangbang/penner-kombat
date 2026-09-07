extends Node
class_name CinematicArenaBuilder

## Baut zur Laufzeit das MKX-artige Arena-Dressing um die bestehende Box-Arena:
## Nachthimmel, nasser Reflexions-Boden, Neon-Bühnenrand, Licht-Strips,
## Zuschauer-Ränge, Nebel und Akzent-Lichter. Alles prozedural (keine
## Art-Assets nötig) und nach Qualitätsstufe skaliert (Kamera-Crowd,
## Lichtanzahl, Nebel).

const SKY_SHADER_PATH: String = "res://shaders/ArenaSky.gdshader"
const FLOOR_SHADER_PATH: String = "res://shaders/WetFloor.gdshader"
const STAGE_NAME: String = "CinematicArena"

# Qualitäts-/Tier-Schwellen (passend zu GraphicsQuality).
const TIER_MEDIUM: int = 1
const TIER_HIGH: int = 2
const TIER_ULTRA: int = 3


func build(root: Node3D, tier: int) -> Node3D:
	_clear_existing(root)
	var stage: Node3D = Node3D.new()
	stage.name = STAGE_NAME
	root.add_child(stage)

	_build_sky(stage)
	_build_floor(stage, tier)
	_build_stage_edges(stage, tier)
	_build_lights(stage, tier)
	_build_crowd(stage, tier)
	if tier >= TIER_MEDIUM:
		_build_fog(stage)
	_build_props(stage)
	return stage


func _clear_existing(root: Node3D) -> void:
	for child in root.get_children():
		if child.name == STAGE_NAME:
			root.remove_child(child)
			child.free()


# ---------------------------------------------------------------------------
#  Himmel & Boden
# ---------------------------------------------------------------------------

func _build_sky(stage: Node3D) -> void:
	var sky: MeshInstance3D = MeshInstance3D.new()
	sky.name = "SkyDome"
	var sphere: SphereMesh = SphereMesh.new()
	sphere.radius = 80.0
	sphere.height = 160.0
	sphere.radial_segments = 24
	sphere.rings = 12
	sky.mesh = sphere
	sky.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF

	var material: Material
	if ResourceLoader.exists(SKY_SHADER_PATH):
		var shader_material: ShaderMaterial = ShaderMaterial.new()
		shader_material.shader = load(SKY_SHADER_PATH) as Shader
		material = shader_material
	else:
		var fallback: StandardMaterial3D = StandardMaterial3D.new()
		fallback.albedo_color = Color(0.02, 0.02, 0.06)
		fallback.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
		material = fallback
	sky.material_override = material
	stage.add_child(sky)


func _build_floor(stage: Node3D, tier: int) -> void:
	var floor_mesh: MeshInstance3D = MeshInstance3D.new()
	floor_mesh.name = "WetStageFloor"
	var plane: PlaneMesh = PlaneMesh.new()
	plane.size = Vector2(24.0, 24.0)
	floor_mesh.mesh = plane
	floor_mesh.position = Vector3(0, 0.02, 0)
	floor_mesh.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF

	var material: ShaderMaterial = ShaderMaterial.new()
	if ResourceLoader.exists(FLOOR_SHADER_PATH):
		material.shader = load(FLOOR_SHADER_PATH) as Shader
		material.set_shader_parameter("wetness", 0.95 if tier >= TIER_HIGH else 0.75)
		material.set_shader_parameter("grid_glow", 0.6 if tier >= TIER_HIGH else 0.35)
		material.set_shader_parameter("reflection_strength", 0.8 if tier >= TIER_ULTRA else 0.55)
		material.set_shader_parameter("ripple_strength", 0.3 if tier >= TIER_MEDIUM else 0.1)
	floor_mesh.material_override = material
	stage.add_child(floor_mesh)


# ---------------------------------------------------------------------------
#  Bühnenrand, Neon & Lichter
# ---------------------------------------------------------------------------

func _build_stage_edges(stage: Node3D, tier: int) -> void:
	var neon_color: Color = Color(1.0, 0.16, 0.12)
	var edge_length: float = 22.0
	var edge_thickness: float = 0.28
	var y: float = 0.12

	_add_emissive_box(stage, "NeonSouth", Vector3(0, y, 10.5), Vector3(edge_length, edge_thickness, edge_thickness), neon_color, 3.2)
	_add_emissive_box(stage, "NeonNorth", Vector3(0, y, -10.5), Vector3(edge_length, edge_thickness, edge_thickness), neon_color, 3.2)
	_add_emissive_box(stage, "NeonEast", Vector3(10.5, y, 0), Vector3(edge_thickness, edge_thickness, edge_length), neon_color, 2.6)
	_add_emissive_box(stage, "NeonWest", Vector3(-10.5, y, 0), Vector3(edge_thickness, edge_thickness, edge_length), neon_color, 2.6)

	# Zwei zentrale "Kampflinie"-Strips auf dem Boden
	_add_emissive_box(stage, "CenterLineA", Vector3(-1.4, 0.06, 0), Vector3(0.12, 0.02, 20.0), Color(1.0, 0.5, 0.1), 1.8)
	_add_emissive_box(stage, "CenterLineB", Vector3(1.4, 0.06, 0), Vector3(0.12, 0.02, 20.0), Color(1.0, 0.5, 0.1), 1.8)

	if tier >= TIER_HIGH:
		_add_emissive_box(stage, "BackPanelGlow", Vector3(0, 3.4, -11.0), Vector3(20.0, 0.2, 0.2), Color(0.2, 0.5, 1.0), 2.4)


func _build_lights(stage: Node3D, tier: int) -> void:
	# Zentrale Arena-Flut (warm) — ohne Schatten (mobil-freundlich)
	var key: SpotLight3D = SpotLight3D.new()
	key.name = "CinematicSpot"
	key.position = Vector3(6.0, 8.5, 6.0)
	key.light_color = Color(1.0, 0.85, 0.7)
	key.light_energy = 6.0
	key.spot_range = 30.0
	key.spot_angle = 55.0
	key.shadow_enabled = false
	key.look_at(Vector3.ZERO, Vector3.UP)
	stage.add_child(key)

	var fill: PointLight3D = PointLight3D.new()
	fill.name = "CinematicFill"
	fill.position = Vector3(-5.0, 5.0, -2.0)
	fill.light_color = Color(0.3, 0.5, 1.0)
	fill.light_energy = 2.5
	fill.omni_range = 18.0
	fill.shadow_enabled = false
	stage.add_child(fill)

	if tier >= TIER_ULTRA:
		var rim: SpotLight3D = SpotLight3D.new()
		rim.name = "BackRim"
		rim.position = Vector3(0.0, 6.0, -12.0)
		rim.light_color = Color(1.0, 0.2, 0.15)
		rim.light_energy = 5.0
		rim.spot_range = 28.0
		rim.spot_angle = 45.0
		rim.shadow_enabled = false
		rim.look_at(Vector3(0, 1, 0), Vector3.UP)
		stage.add_child(rim)


# ---------------------------------------------------------------------------
#  Zuschauer (MKX-artige Ränge) — eine einzige InstancedMesh pro Rang
# ---------------------------------------------------------------------------

func _build_crowd(stage: Node3D, tier: int) -> void:
	var rows: int = 3 if tier >= TIER_HIGH else 2
	var per_row: int = 26 if tier >= TIER_HIGH else 16
	var count: int = rows * per_row
	if count <= 0:
		return

	var multimesh: MultiMesh = MultiMesh.new()
	multimesh.transform_format = MultiMesh.TRANSFORM_3D
	multimesh.use_colors = true
	multimesh.instance_count = count

	var box: BoxMesh = BoxMesh.new()
	box.size = Vector3(0.42, 0.85, 0.4)

	var body_mat: StandardMaterial3D = StandardMaterial3D.new()
	body_mat.roughness = 0.9
	body_mat.emission_enabled = true
	body_mat.emission_energy_multiplier = 0.35
	# Instanz-Farben der MultiMesh werden als Albedo genutzt
	body_mat.vertex_color_use_as_albedo = true

	var rng: RandomNumberGenerator = RandomNumberGenerator.new()
	rng.seed = 1337
	var palette: Array[Color] = [
		Color(0.7, 0.2, 0.2), Color(0.2, 0.3, 0.7), Color(0.8, 0.7, 0.2),
		Color(0.3, 0.6, 0.3), Color(0.9, 0.9, 0.85), Color(0.5, 0.2, 0.6)
	]

	var index: int = 0
	for row in range(rows):
		var z: float = -13.0 - float(row) * 1.6
		var lift: float = float(row) * 0.75
		for i in range(per_row):
			if index >= count:
				break
			var x: float = lerpf(-13.0, 13.0, float(i) / float(per_row - 1)) + rng.randf_range(-0.3, 0.3)
			var transform: Transform3D = Transform3D(Basis.IDENTITY, Vector3(x, 0.5 + lift, z))
			transform = transform.rotated(Vector3.UP, rng.randf_range(-0.5, 0.5))
			multimesh.set_instance_transform(index, transform)
			var color: Color = palette[rng.randi_range(0, palette.size() - 1)]
			multimesh.set_instance_color(index, Color(color.r * 0.5, color.g * 0.5, color.b * 0.5, 1.0))
			index += 1

	multimesh.instance_count = index

	var crowd: MultiMeshInstance3D = MultiMeshInstance3D.new()
	crowd.name = "CrowdStands"
	crowd.multimesh = multimesh
	crowd.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	stage.add_child(crowd)


# ---------------------------------------------------------------------------
#  Nebel & Props
# ---------------------------------------------------------------------------

func _build_fog(stage: Node3D) -> void:
	var fog: FogVolume = FogVolume.new()
	fog.name = "ArenaHaze"
	fog.size = Vector3(46.0, 12.0, 40.0)
	fog.position = Vector3(0, 4.0, -4.0)
	var material: FogMaterial = FogMaterial.new()
	material.albedo = Color(0.06, 0.08, 0.14)
	material.density = 0.014
	fog.material = material
	stage.add_child(fog)


func _build_props(stage: Node3D) -> void:
	# Bierkisten am Bühnenrand (Prozedural-Ersatz für echte Assets)
	_add_colored_box(stage, "Kiste1", Vector3(-9.2, 0.45, 8.6), Vector3(1.1, 0.9, 1.1), Color(0.16, 0.4, 0.18))
	_add_colored_box(stage, "Kiste2", Vector3(-8.0, 0.45, 9.0), Vector3(1.1, 0.9, 1.1), Color(0.16, 0.4, 0.18))
	_add_colored_box(stage, "Kiste3", Vector3(0.0, 0.9, -9.6), Vector3(1.3, 1.8, 1.3), Color(0.2, 0.2, 0.24))
	_add_colored_box(stage, "FassWest", Vector3(-10.3, 0.5, -2.0), Vector3(1.0, 1.0, 1.0), Color(0.35, 0.32, 0.3))
	_add_colored_box(stage, "FassOst", Vector3(10.3, 0.5, 2.4), Vector3(1.0, 1.0, 1.0), Color(0.35, 0.32, 0.3))


func _add_emissive_box(parent: Node3D, node_name: String, pos: Vector3, size: Vector3, color: Color, energy: float) -> void:
	var mesh: MeshInstance3D = MeshInstance3D.new()
	mesh.name = node_name
	var box: BoxMesh = BoxMesh.new()
	box.size = size
	mesh.mesh = box
	mesh.position = pos
	mesh.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	var material: StandardMaterial3D = StandardMaterial3D.new()
	material.albedo_color = color
	material.emission_enabled = true
	material.emission = color
	material.emission_energy_multiplier = energy
	material.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	mesh.material_override = material
	parent.add_child(mesh)


func _add_colored_box(parent: Node3D, node_name: String, pos: Vector3, size: Vector3, color: Color) -> void:
	var mesh: MeshInstance3D = MeshInstance3D.new()
	mesh.name = node_name
	var box: BoxMesh = BoxMesh.new()
	box.size = size
	mesh.mesh = box
	mesh.position = pos
	var material: StandardMaterial3D = StandardMaterial3D.new()
	material.albedo_color = color
	material.roughness = 0.8
	material.emission_enabled = true
	material.emission = color * 0.08
	mesh.material_override = material
	parent.add_child(mesh)
