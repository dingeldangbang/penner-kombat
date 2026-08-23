extends Node3D
class_name ElectricEffect

## Cyan/white lightning arcs with a short overexposed flash.

@export var arc_count: int = 5
@export var duration: float = 0.35
@export var arc_color: Color = Color(0.0, 0.8, 1.0, 1.0)

var _timer: float = 0.0
var _lines: Array[MeshInstance3D] = []


func initialize(radius: float = 1.5) -> void:
	_clear_lines()
	for i in range(arc_count):
		var end: Vector3 = Vector3(randf_range(-radius, radius), randf_range(0.2, radius), randf_range(-radius, radius))
		_lines.append(_create_arc(Vector3.ZERO, end))
	_flash_screen()


func _process(delta: float) -> void:
	_timer += delta
	if _timer >= duration:
		queue_free()
		return
	var alpha: float = 1.0 - _timer / duration
	for line in _lines:
		if line.material_override is StandardMaterial3D:
			(line.material_override as StandardMaterial3D).albedo_color.a = alpha


func _create_arc(start: Vector3, end: Vector3) -> MeshInstance3D:
	var mesh_instance: MeshInstance3D = MeshInstance3D.new()
	var immediate: ImmediateMesh = ImmediateMesh.new()
	var mat: StandardMaterial3D = StandardMaterial3D.new()
	mat.albedo_color = arc_color
	mat.emission_enabled = true
	mat.emission = arc_color
	mat.emission_energy_multiplier = 4.0
	mat.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	mat.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	immediate.surface_begin(Mesh.PRIMITIVE_LINE_STRIP, mat)
	var segments: int = 6
	for i in range(segments + 1):
		var t: float = float(i) / float(segments)
		var jitter: Vector3 = Vector3(randf_range(-0.12, 0.12), randf_range(-0.12, 0.12), randf_range(-0.12, 0.12))
		immediate.surface_add_vertex(start.lerp(end, t) + jitter)
	immediate.surface_end()
	mesh_instance.mesh = immediate
	mesh_instance.material_override = mat
	add_child(mesh_instance)
	return mesh_instance


func _flash_screen() -> void:
	var canvas: CanvasLayer = CanvasLayer.new()
	canvas.layer = 100
	var flash: ColorRect = ColorRect.new()
	flash.color = Color(0.75, 0.95, 1.0, 0.45)
	flash.set_anchors_preset(Control.PRESET_FULL_RECT)
	canvas.add_child(flash)
	get_tree().root.add_child(canvas)
	var tween: Tween = create_tween()
	tween.tween_property(flash, "color:a", 0.0, 0.12)
	await tween.finished
	if is_instance_valid(canvas):
		canvas.queue_free()


func _clear_lines() -> void:
	for line in _lines:
		if is_instance_valid(line):
			line.queue_free()
	_lines.clear()
