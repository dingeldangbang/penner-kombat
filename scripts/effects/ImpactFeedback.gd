extends Node
class_name ImpactFeedback

## Treffer-Feedback-Direktor (MKX-"Game Feel"):
##  - HitSpark (GPUParticles3D) + Shockwave-Ring am Einschlagpunkt
##  - Kamera-Shake und Bildschirm-Flash über ScreenShake/PostProcessing
##  - Hit-Stop bei schweren Treffern (2–4 Frames Zeitstillstand)
##  - Optionale Charakter-Flash-Blitz (CharacterShaderBinder)

var _post_processing: PostProcessing
var _combat_camera: KombatCamera
var _hitstop_depth: int = 0


func initialize(post: PostProcessing = null, combat_camera: KombatCamera = null) -> void:
	_post_processing = post
	_combat_camera = combat_camera


func spawn_hit(at_position: Vector3, damage: float, toward_camera: bool = true) -> void:
	if not is_inside_tree():
		return
	if damage <= 0.0:
		return

	var spark: HitSpark = HitSpark.new()
	spark.name = "HitSpark"
	add_child(spark)
	spark.global_position = at_position + Vector3.UP * 1.05
	var direction: Vector3 = Vector3(0, 0.25, 1.0) if toward_camera else Vector3(0, 0.25, -1.0)
	spark.initialize(damage, direction, damage >= 24.0)

	var wave: Shockwave = Shockwave.new()
	wave.name = "HitWave"
	add_child(wave)
	wave.global_position = at_position + Vector3.UP * 0.85
	wave.initialize(direction)
	wave.scale = Vector3.ONE * clampf(0.6 + damage / 60.0, 0.6, 1.8)

	var shake: float = clampf(0.22 + damage / 30.0, 0.22, 0.95)
	if _combat_camera:
		_combat_camera.add_trauma(shake)
	else:
		ScreenShake.shake(clampf(0.1 + damage / 40.0, 0.1, 0.4), shake * 0.5)

	if _post_processing:
		_post_processing.apply_hit_flash(clampf(0.25 + damage / 80.0, 0.25, 0.9))

	if _combat_camera:
		_combat_camera.punch_zoom(clampf(damage / 35.0, 0.4, 1.6))

	if damage >= 14.0:
		_hitstop()


func _hitstop(duration: float = 0.045) -> void:
	# Verschachtelte Hit-Stops (Combo!) nicht doppelt ausführen.
	if _hitstop_depth > 0:
		_hitstop_depth += 1
		return
	_hitstop_depth = 1
	var previous: float = Engine.time_scale
	Engine.time_scale = 0.02
	await get_tree().create_timer(duration, true, false, true).timeout
	if _hitstop_depth > 1:
		_hitstop_depth -= 1
		return
	_hitstop_depth = 0
	# Nicht auf 1.0 zurücksetzen, wenn parallel ein Slow-Mo läuft.
	if Engine.time_scale < 0.05:
		Engine.time_scale = previous
