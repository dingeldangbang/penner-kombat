extends GPUParticles3D
class_name CritGold

## Golden star burst for critical Mojo-style hits.

@export var star_count: int = 20
@export var star_lifetime: float = 0.8
@export var gold_color: Color = Color(1.0, 0.84, 0.0, 1.0)


func _ready() -> void:
	one_shot = true
	explosiveness = 0.85
	visibility_aabb = AABB(Vector3(-5, -2, -5), Vector3(10, 8, 10))
	if process_material == null:
		process_material = _create_process_material()
	if draw_pass_1 == null:
		draw_pass_1 = _create_star_mesh()


func initialize(world_position: Vector3) -> void:
	global_position = world_position
	amount = star_count
	lifetime = star_lifetime
	restart()
	emitting = true
	_critical_freeze()
	_auto_free()


func _create_process_material() -> ParticleProcessMaterial:
	var material: ParticleProcessMaterial = ParticleProcessMaterial.new()
	material.color = gold_color
	material.scale_min = 0.2
	material.scale_max = 0.8
	material.initial_velocity_min = 5.0
	material.initial_velocity_max = 15.0
	material.direction = Vector3.UP
	material.spread = 180.0
	material.gravity = Vector3(0.0, -2.0, 0.0)
	return material


func _create_star_mesh() -> Mesh:
	var quad: QuadMesh = QuadMesh.new()
	quad.size = Vector2(0.35, 0.35)
	var mat: StandardMaterial3D = StandardMaterial3D.new()
	mat.albedo_texture = _create_star_texture()
	mat.albedo_color = gold_color
	mat.emission_enabled = true
	mat.emission = gold_color
	mat.emission_energy_multiplier = 2.0
	mat.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	mat.billboard_mode = BaseMaterial3D.BILLBOARD_ENABLED
	quad.material = mat
	return quad


func _create_star_texture() -> Texture2D:
	var size_px: int = 64
	var image: Image = Image.create(size_px, size_px, false, Image.FORMAT_RGBA8)
	var center: Vector2 = Vector2(size_px, size_px) * 0.5
	var outer_radius: float = size_px * 0.46
	var inner_radius: float = outer_radius * 0.42
	for y in range(size_px):
		for x in range(size_px):
			var point: Vector2 = Vector2(x, y) - center
			var angle: float = atan2(point.y, point.x) + PI * 0.5
			var sector: float = fmod(angle + TAU, TAU) / (TAU / 10.0)
			var target_radius: float = lerpf(outer_radius, inner_radius, abs(fmod(sector, 2.0) - 1.0))
			var alpha: float = 1.0 if point.length() <= target_radius else 0.0
			image.set_pixel(x, y, Color(1, 1, 1, alpha))
	return ImageTexture.create_from_image(image)


func _critical_freeze() -> void:
	var previous_scale: float = Engine.time_scale
	Engine.time_scale = 0.01
	await get_tree().process_frame
	await get_tree().process_frame
	Engine.time_scale = previous_scale


func _auto_free() -> void:
	await get_tree().create_timer(star_lifetime + 0.4).timeout
	if is_instance_valid(self):
		queue_free()
