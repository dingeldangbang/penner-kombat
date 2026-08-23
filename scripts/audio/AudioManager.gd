extends Node
class_name AudioManager

## Lightweight positional SFX manager. Add one instance to a scene or rely on static no-op safety.

static var instance: AudioManager

@export var max_concurrent_sfx: int = 16
var _active_players: Array[AudioStreamPlayer3D] = []


func _ready() -> void:
	instance = self


static func play_sfx_at_position(stream: AudioStream, world_position: Vector3, volume_db: float = 0.0, pitch_scale: float = 1.0) -> void:
	if instance:
		instance._play_sfx_at_position(stream, world_position, volume_db, pitch_scale)


func _play_sfx_at_position(stream: AudioStream, world_position: Vector3, volume_db: float, pitch_scale: float) -> void:
	if stream == null:
		return
	_cleanup_players()
	if _active_players.size() >= max_concurrent_sfx:
		var oldest: AudioStreamPlayer3D = _active_players.pop_front()
		if is_instance_valid(oldest):
			oldest.queue_free()
	var player: AudioStreamPlayer3D = AudioStreamPlayer3D.new()
	player.stream = stream
	player.volume_db = volume_db
	player.pitch_scale = pitch_scale
	get_tree().root.add_child(player)
	player.global_position = world_position
	_active_players.append(player)
	player.finished.connect(_on_player_finished.bind(player))
	player.play()


func _on_player_finished(player: AudioStreamPlayer3D) -> void:
	_active_players.erase(player)
	if is_instance_valid(player):
		player.queue_free()


func _cleanup_players() -> void:
	for i in range(_active_players.size() - 1, -1, -1):
		if not is_instance_valid(_active_players[i]):
			_active_players.remove_at(i)
