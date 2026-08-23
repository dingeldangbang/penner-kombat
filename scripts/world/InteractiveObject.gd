extends StaticBody3D
class_name InteractiveObject

## Interactive arena prop with hazards, explosions, collapse, flicker and respawn.

enum ObjectType {
	BEER_CRATE,
	GAS_CANISTER,
	CLOTHESLINE,
	TRASH_CAN,
	SCAFFOLDING,
	NEON_SIGN
}

@export var object_type: ObjectType = ObjectType.BEER_CRATE
@export var respawn_time: float = 5.0
@export var is_destroyed: bool = false
@export var explosion_radius: float = 5.0
@export var explosion_damage: float = 25.0
@export var impact_damage: float = 7.0
@export var throw_damage: float = 14.0
@export var particle_prefab: PackedScene
@export var debris_prefab: PackedScene
@export var audio_clip: AudioStream

var _original_position: Vector3
var _original_rotation: Vector3
var _is_active: bool = true
var _rigidbody: RigidBody3D
var _hit_area: Area3D
var _hit_count: int = 0


func _ready() -> void:
	_original_position = global_position
	_original_rotation = rotation
	_rigidbody = get_node_or_null("RigidBody3D") as RigidBody3D
	if _rigidbody:
		_rigidbody.freeze = true
	_setup_hit_area()


func interact(direction: Vector3 = Vector3.FORWARD, force: float = 10.0) -> void:
	if not _is_active:
		return
	match object_type:
		ObjectType.BEER_CRATE, ObjectType.TRASH_CAN:
			_throw_object(direction, force)
		ObjectType.GAS_CANISTER:
			_hit_count += 1
			if _hit_count >= 4:
				_explode()
			else:
				_flicker_warning()
		ObjectType.CLOTHESLINE:
			_trip_enemy()
		ObjectType.SCAFFOLDING:
			_collapse()
		ObjectType.NEON_SIGN:
			_flicker()


func _setup_hit_area() -> void:
	_hit_area = Area3D.new()
	_hit_area.name = "InteractionHitArea"
	_hit_area.monitoring = false
	_hit_area.collision_layer = 8
	_hit_area.collision_mask = 1
	var collision_shape: CollisionShape3D = CollisionShape3D.new()
	var sphere: SphereShape3D = SphereShape3D.new()
	sphere.radius = 1.0
	collision_shape.shape = sphere
	_hit_area.add_child(collision_shape)
	add_child(_hit_area)
	_hit_area.body_entered.connect(_on_hit_object)


func _throw_object(direction: Vector3, force: float) -> void:
	_is_active = false
	_hit_area.monitoring = true
	var normalized: Vector3 = direction.normalized() if direction.length() > 0.001 else -global_transform.basis.z
	if _rigidbody:
		_rigidbody.freeze = false
		_rigidbody.apply_impulse(normalized * force + Vector3(0, 2, 0))
	else:
		var tween: Tween = create_tween()
		tween.tween_property(self, "global_position", global_position + normalized * 3.0 + Vector3.UP, 0.45).set_trans(Tween.TRANS_QUAD)
		tween.parallel().tween_property(self, "rotation", rotation + Vector3(randf(), randf(), randf()) * TAU, 0.45)
	await get_tree().create_timer(respawn_time).timeout
	_reset_object()


func _explode() -> void:
	if is_destroyed:
		return
	is_destroyed = true
	_is_active = false
	visible = false
	_spawn_explosion_effects()
	_apply_radius_damage()
	_spawn_debris(7)
	if audio_clip:
		_play_sfx(audio_clip)
	ScreenShake.shake(0.3, 1.0)
	await get_tree().create_timer(respawn_time).timeout
	_reset_object()


func _spawn_explosion_effects() -> void:
	var parent: Node = get_parent()
	if particle_prefab:
		var particles: Node3D = particle_prefab.instantiate() as Node3D
		parent.add_child(particles)
		particles.global_position = global_position
		if particles is GPUParticles3D:
			(particles as GPUParticles3D).emitting = true
	else:
		var fire: FireEffect = FireEffect.new()
		parent.add_child(fire)
		fire.global_position = global_position
		fire.initialize(100.0, Vector3.UP)
		var shockwave: Shockwave = Shockwave.new()
		parent.add_child(shockwave)
		shockwave.global_position = global_position
		shockwave.initialize(Vector3.UP)


func _apply_radius_damage() -> void:
	for body in get_tree().get_nodes_in_group("fighter") + get_tree().get_nodes_in_group("enemy"):
		if body is Node3D and (body as Node3D).global_position.distance_to(global_position) < explosion_radius:
			var target: Node3D = body as Node3D
			var distance: float = target.global_position.distance_to(global_position)
			var damage: float = explosion_damage * (1.0 - distance / explosion_radius)
			if body.has_method("take_damage"):
				body.call("take_damage", damage)
			if body.has_method("apply_knockback"):
				body.call("apply_knockback", (target.global_position - global_position).normalized() * 8.0 + Vector3.UP * 4.0)


func _spawn_debris(count: int) -> void:
	var parent: Node = get_parent()
	for i in range(count):
		var debris: RigidBody3D
		if debris_prefab:
			debris = debris_prefab.instantiate() as RigidBody3D
		else:
			debris = _create_default_debris()
		if debris == null:
			continue
		parent.add_child(debris)
		debris.global_position = global_position + Vector3(randf_range(-1, 1), randf_range(0, 1), randf_range(-1, 1))
		debris.apply_impulse(Vector3(randf_range(-5, 5), randf_range(2, 8), randf_range(-5, 5)))
		_free_debris_later(debris, 3.0)


func _create_default_debris() -> RigidBody3D:
	var body: RigidBody3D = RigidBody3D.new()
	var mesh: MeshInstance3D = MeshInstance3D.new()
	var box_mesh: BoxMesh = BoxMesh.new()
	box_mesh.size = Vector3(0.25, 0.18, 0.25)
	mesh.mesh = box_mesh
	body.add_child(mesh)
	var shape: CollisionShape3D = CollisionShape3D.new()
	var box: BoxShape3D = BoxShape3D.new()
	box.size = box_mesh.size
	shape.shape = box
	body.add_child(shape)
	return body


func _free_debris_later(debris: Node, delay: float) -> void:
	await get_tree().create_timer(delay).timeout
	if is_instance_valid(debris):
		debris.queue_free()


func _trip_enemy() -> void:
	_spawn_cloth_snap()
	for body in get_tree().get_nodes_in_group("fighter") + get_tree().get_nodes_in_group("enemy"):
		if body is Node3D and (body as Node3D).global_position.distance_to(global_position) < 3.0:
			if body.has_method("stun"):
				body.call("stun", 0.3)
			if body.has_method("take_damage"):
				body.call("take_damage", 5.0)


func _collapse() -> void:
	if not _is_active:
		return
	_is_active = false
	var tween: Tween = create_tween()
	tween.tween_property(self, "rotation:z", rotation.z + deg_to_rad(45.0), 0.5)
	tween.parallel().tween_property(self, "position:y", position.y - 1.0, 0.5)
	_spawn_debris(3)
	ScreenShake.shake(0.18, 0.45)
	await get_tree().create_timer(respawn_time).timeout
	_reset_object()


func _flicker() -> void:
	var mesh: MeshInstance3D = get_node_or_null("MeshInstance3D") as MeshInstance3D
	var material: Material = mesh.material_override if mesh else null
	if material is ShaderMaterial:
		for i in range(7):
			(material as ShaderMaterial).set_shader_parameter("emission_intensity", randf_range(0.0, 2.4))
			await get_tree().create_timer(0.08).timeout
		(material as ShaderMaterial).set_shader_parameter("emission_intensity", 1.0)
	elif mesh:
		for i in range(7):
			mesh.visible = not mesh.visible
			await get_tree().create_timer(0.08).timeout
		mesh.visible = true


func _flicker_warning() -> void:
	_flicker()
	var sparks: HitSpark = HitSpark.new()
	get_parent().add_child(sparks)
	sparks.global_position = global_position + Vector3.UP
	sparks.initialize(20.0, Vector3.UP, false)


func _spawn_cloth_snap() -> void:
	var shockwave: Shockwave = Shockwave.new()
	get_parent().add_child(shockwave)
	shockwave.global_position = global_position
	shockwave.wave_color = Color(0.9, 0.9, 1.0, 0.7)
	shockwave.initialize(Vector3.UP)


func _reset_object() -> void:
	is_destroyed = false
	_is_active = true
	_hit_count = 0
	visible = true
	global_position = _original_position
	rotation = _original_rotation
	if _hit_area:
		_hit_area.monitoring = false
	if _rigidbody:
		_rigidbody.freeze = true
		_rigidbody.linear_velocity = Vector3.ZERO
		_rigidbody.angular_velocity = Vector3.ZERO


func _on_hit_object(body: Node) -> void:
	if not body or body == self:
		return
	var damage: float = throw_damage if object_type == ObjectType.BEER_CRATE else impact_damage
	if body.has_method("take_damage"):
		body.call("take_damage", damage)
	if body.has_method("apply_knockback"):
		body.call("apply_knockback", Vector3.UP * 5.0)


func _play_sfx(stream: AudioStream) -> void:
	var player: AudioStreamPlayer3D = AudioStreamPlayer3D.new()
	player.stream = stream
	player.global_position = global_position
	get_tree().root.add_child(player)
	player.play()
	await player.finished
	if is_instance_valid(player):
		player.queue_free()
