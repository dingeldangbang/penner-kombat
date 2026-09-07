extends Node
class_name CharacterShaderBinder

## Wendet den MKX-artigen Charakter-Shader (shaders/PennerCharacter3D.gdshader)
## auf alle Mesh-Instanzen eines geriggten Modells an. Bestehende
## Albedo-Texturen, Farben und Emissionen werden übernommen, damit das
## Modell weiterhin authentisch aussieht, aber Rim-Light, Toon-Bänderung
## und Outline dazukommen.

const CHARACTER_SHADER_PATH: String = "res://shaders/PennerCharacter3D.gdshader"

@export var rim_strength: float = 0.9
@export var rim_color: Color = Color(1.0, 0.62, 0.2)
@export var toon_bands: float = 2.0
@export var outline_strength: float = 0.3

var _shader: Shader
var _applied_meshes: int = 0


func _ready() -> void:
	if ResourceLoader.exists(CHARACTER_SHADER_PATH):
		_shader = load(CHARACTER_SHADER_PATH) as Shader


## Wandelt alle Materialien unterhalb von `entity` um. Liefert Anzahl der Meshes.
func apply_to(entity: Node3D) -> int:
	_applied_meshes = 0
	if _shader == null:
		_shader = load(CHARACTER_SHADER_PATH) as Shader
	if _shader == null or entity == null:
		return 0
	_walk(entity)
	return _applied_meshes


func _walk(node: Node) -> void:
	if node is MeshInstance3D:
		_bind_mesh(node as MeshInstance3D)
	for child in node.get_children():
		_walk(child)


func _bind_mesh(mesh: MeshInstance3D) -> void:
	var applied_any: bool = false
	if mesh.material_override != null:
		if _bind_material(mesh.material_override, mesh):
			applied_any = true
	for i in range(mesh.get_surface_override_material_count()):
		var surface_mat: Material = mesh.get_surface_override_material(i)
		if surface_mat != null:
			applied_any = _bind_material(surface_mat, mesh) or applied_any

	# GLB/GLTF-Importe legen Materialien in den Mesh-Surfaces ab (kein Override).
	# Mesh-Ressource duplizieren, damit andere Instanzen unverändert bleiben.
	if mesh.mesh != null and mesh.mesh.get_surface_count() > 0:
		var converted_any: bool = false
		var needs_duplicate: bool = false
		for i in range(mesh.mesh.get_surface_count()):
			var base_mat: Material = mesh.mesh.surface_get_material(i)
			if base_mat is StandardMaterial3D:
				needs_duplicate = true
				converted_any = true
		if needs_duplicate:
			var owned: Mesh = mesh.mesh.duplicate() as Mesh
			for i in range(owned.get_surface_count()):
				var base_mat: Material = owned.surface_get_material(i)
				if base_mat is StandardMaterial3D:
					owned.surface_set_material(i, _convert_material(base_mat as StandardMaterial3D))
			mesh.mesh = owned
			applied_any = true

	if applied_any:
		_applied_meshes += 1


func _bind_material(material: Material, mesh: MeshInstance3D) -> bool:
	# Bereits gebunden? Dann nur Uniforms aktualisieren.
	if material is ShaderMaterial:
		var shader_mat: ShaderMaterial = material as ShaderMaterial
		if shader_mat.shader == _shader:
			_update_uniforms(shader_mat)
			return true
		return false

	# Nur Materialien umwandeln, die wir sinnvoll übernehmen können.
	var source: BaseMaterial3D = material as BaseMaterial3D
	if source == null:
		return false

	var target: ShaderMaterial = _convert_material(source)

	# Override-Material ersetzen (nicht das geteilte Mesh-Ressourcen-Material).
	if mesh.material_override == material:
		mesh.material_override = target
	else:
		for i in range(mesh.get_surface_override_material_count()):
			if mesh.get_surface_override_material(i) == material:
				mesh.set_surface_override_material(i, target)
	return true


func _convert_material(source: BaseMaterial3D) -> ShaderMaterial:
	var target: ShaderMaterial = ShaderMaterial.new()
	target.shader = _shader
	target.set_shader_parameter("albedo_tint", source.albedo_color if source.albedo_color.a > 0.0 else Color.WHITE)
	if source.albedo_texture != null:
		target.set_shader_parameter("albedo_texture", source.albedo_texture)
	target.set_shader_parameter("roughness", source.roughness)
	target.set_shader_parameter("metallic", source.metallic)
	if source.emission_enabled:
		target.set_shader_parameter("emission_energy", source.emission_energy_multiplier)
		target.set_shader_parameter("emission_color", source.emission)
	target.set_shader_parameter("rim_strength", rim_strength)
	target.set_shader_parameter("rim_color", rim_color)
	target.set_shader_parameter("toon_bands", toon_bands)
	target.set_shader_parameter("outline_strength", outline_strength)
	return target


func _update_uniforms(shader_mat: ShaderMaterial) -> void:
	shader_mat.set_shader_parameter("rim_strength", rim_strength)
	shader_mat.set_shader_parameter("rim_color", rim_color)
	shader_mat.set_shader_parameter("toon_bands", toon_bands)
	shader_mat.set_shader_parameter("outline_strength", outline_strength)


## Kurzer Treffer-Blitz auf allen unterhalb von `entity` gebundenen Shadern.
func flash_hit(entity: Node3D, intensity: float = 1.0) -> void:
	if entity == null:
		return
	for shader_mat in _collect_shader_materials(entity):
		shader_mat.set_shader_parameter("hit_flash", clampf(intensity, 0.0, 1.0))


func _collect_shader_materials(node: Node, out: Array = []) -> Array:
	if node is MeshInstance3D:
		var mesh: MeshInstance3D = node as MeshInstance3D
		if mesh.material_override is ShaderMaterial:
			var sm: ShaderMaterial = mesh.material_override as ShaderMaterial
			if sm.shader == _shader:
				out.append(sm)
		for i in range(mesh.get_surface_override_material_count()):
			var sm2: ShaderMaterial = mesh.get_surface_override_material(i) as ShaderMaterial
			if sm2 != null and sm2.shader == _shader:
				out.append(sm2)
		if mesh.mesh != null:
			for i in range(mesh.mesh.get_surface_count()):
				var sm3: ShaderMaterial = mesh.mesh.surface_get_material(i) as ShaderMaterial
				if sm3 != null and sm3.shader == _shader:
					out.append(sm3)
	for child in node.get_children():
		_collect_shader_materials(child, out)
	return out
