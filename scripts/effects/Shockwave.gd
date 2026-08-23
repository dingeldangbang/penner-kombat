extends Sprite3D
class_name Shockwave

## Camera-facing 2D ring animation for impact shockwaves.

@export var start_size: float = 0.5
@export var end_size: float = 4.0
@export var duration: float = 0.2
@export var wave_color: Color = Color(1.0, 1.0, 1.0, 1.0)

var _timer: float = 0.0
var _started: bool = false


func initialize(_direction: Vector3 = Vector3.UP) -> void:
	texture = _create_ring_texture()
	billboard = BaseMaterial3D.BILLBOARD_ENABLED
	modulate = wave_color
	scale = Vector3.ONE * start_size
	_started = true


func _process(delta: float) -> void:
	if not _started:
		return
	_timer += delta
	var progress: float = _timer / maxf(duration, 0.001)
	if progress >= 1.0:
		queue_free()
		return
	var current_size: float = lerpf(start_size, end_size, progress)
	scale = Vector3.ONE * current_size
	modulate.a = 1.0 - progress


func _create_ring_texture() -> Texture2D:
	var size_px: int = 128
	var image: Image = Image.create(size_px, size_px, false, Image.FORMAT_RGBA8)
	var center: Vector2 = Vector2(size_px, size_px) * 0.5
	var radius: float = size_px * 0.5 - 4.0
	var thickness: float = 5.0
	for y in range(size_px):
		for x in range(size_px):
			var dist: float = Vector2(x, y).distance_to(center)
			var alpha: float = 0.0
			if dist >= radius - thickness and dist <= radius:
				alpha = 1.0 - abs(dist - (radius - thickness * 0.5)) / (thickness * 0.5)
			image.set_pixel(x, y, Color(1, 1, 1, clampf(alpha, 0.0, 1.0)))
	return ImageTexture.create_from_image(image)
