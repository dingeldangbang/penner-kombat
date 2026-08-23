extends Node
class_name BeatSync

## Synchronizes visual pulses with music beat using an audio spectrum analyzer.

signal beat(strength: float)

@export var audio_player: AudioStreamPlayer
@export var visual_node: Node
@export var bpm: float = 140.0
@export var bass_threshold: float = 0.22
@export var bus_name: String = "Master"

var _analyzer_effect: AudioEffectSpectrumAnalyzer
var _analyzer_instance: AudioEffectSpectrumAnalyzerInstance
var _beat_interval: float = 0.0
var _last_beat_msec: int = 0
var _is_initialized: bool = false
var _bus_idx: int = -1
var _effect_idx: int = -1


func _ready() -> void:
	_initialize_beat_detection()


func _exit_tree() -> void:
	if _bus_idx >= 0 and _effect_idx >= 0 and _effect_idx < AudioServer.get_bus_effect_count(_bus_idx):
		AudioServer.remove_bus_effect(_bus_idx, _effect_idx)


func _initialize_beat_detection() -> void:
	_bus_idx = AudioServer.get_bus_index(bus_name)
	if _bus_idx < 0:
		_bus_idx = AudioServer.get_bus_index("Master")
	_analyzer_effect = AudioEffectSpectrumAnalyzer.new()
	_analyzer_effect.buffer_length = 0.2
	AudioServer.add_bus_effect(_bus_idx, _analyzer_effect)
	_effect_idx = AudioServer.get_bus_effect_count(_bus_idx) - 1
	_analyzer_instance = AudioServer.get_bus_effect_instance(_bus_idx, _effect_idx) as AudioEffectSpectrumAnalyzerInstance
	_beat_interval = 60.0 / maxf(bpm, 1.0)
	_is_initialized = _analyzer_instance != null


func _process(_delta: float) -> void:
	if not _is_initialized:
		return
	var magnitude: Vector2 = _analyzer_instance.get_magnitude_for_frequency_range(20.0, 140.0, AudioEffectSpectrumAnalyzerInstance.MAGNITUDE_AVERAGE)
	var bass: float = (magnitude.x + magnitude.y) * 0.5
	var now: int = Time.get_ticks_msec()
	if bass > bass_threshold and float(now - _last_beat_msec) > _beat_interval * 1000.0 * 0.55:
		_on_beat(bass)
		_last_beat_msec = now


func _on_beat(strength: float) -> void:
	beat.emit(strength)
	if visual_node:
		var tween: Tween = visual_node.create_tween()
		var base_scale: Variant = visual_node.get("scale")
		if base_scale is Vector2:
			tween.tween_property(visual_node, "scale", (base_scale as Vector2) * 1.1, 0.05)
			tween.tween_property(visual_node, "scale", base_scale, 0.1)
		elif base_scale is Vector3:
			tween.tween_property(visual_node, "scale", (base_scale as Vector3) * 1.1, 0.05)
			tween.tween_property(visual_node, "scale", base_scale, 0.1)
		if visual_node is CanvasItem:
			var item: CanvasItem = visual_node as CanvasItem
			tween.parallel().tween_property(item, "modulate", Color(2, 2, 2, 1), 0.05)
			tween.parallel().tween_property(item, "modulate", Color.WHITE, 0.1)


func set_bpm(new_bpm: float) -> void:
	bpm = maxf(new_bpm, 1.0)
	_beat_interval = 60.0 / bpm
