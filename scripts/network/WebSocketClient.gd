extends Node
class_name WebSocketClient

## Small WebSocketPeer wrapper for optional multiplayer features.

signal connected()
signal disconnected(code: int, reason: String)
signal connection_failed(error: String)
signal text_received(text: String)
signal json_received(data: Dictionary)

@export var url: String = ""
@export var auto_connect: bool = false

var peer: WebSocketPeer = WebSocketPeer.new()
var _was_active: bool = false


func _ready() -> void:
	# Android: Produktions-Builds setzen network/relay/url in project.godot
	# (empfohlen wss://), sonst lokaler Dev-Server.
	if url.is_empty():
		url = String(ProjectSettings.get_setting(
			"network/relay/url", "ws://localhost:8080/kombat"))
	if auto_connect:
		connect_to_url(url)


func connect_to_url(new_url: String = "") -> void:
	if not new_url.is_empty():
		url = new_url
	peer = WebSocketPeer.new()
	var err: Error = peer.connect_to_url(url)
	if err != OK:
		connection_failed.emit(error_string(err))
		return
	_was_active = true


func close(code: int = 1000, reason: String = "normal") -> void:
	peer.close(code, reason)


func send_text(text: String) -> bool:
	return peer.get_ready_state() == WebSocketPeer.STATE_OPEN and peer.send_text(text) == OK


func send_json(data: Dictionary) -> bool:
	return send_text(JSON.stringify(data))


func _process(_delta: float) -> void:
	var before: int = peer.get_ready_state()
	peer.poll()
	var after: int = peer.get_ready_state()
	if after == WebSocketPeer.STATE_OPEN and before != WebSocketPeer.STATE_OPEN:
		connected.emit()
	if after == WebSocketPeer.STATE_OPEN:
		while peer.get_available_packet_count() > 0:
			var text: String = peer.get_packet().get_string_from_utf8()
			text_received.emit(text)
			var parsed: Variant = JSON.parse_string(text)
			if parsed is Dictionary:
				json_received.emit(parsed as Dictionary)
	elif after == WebSocketPeer.STATE_CLOSED and _was_active:
		_was_active = false
		disconnected.emit(peer.get_close_code(), peer.get_close_reason())
