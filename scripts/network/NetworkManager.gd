extends Node
class_name NetworkManager

## WebSocket client for lightweight multiplayer/event relay.

signal connected()
signal disconnected(code: int, reason: String)
signal connection_failed(error: String)
signal message_received(message: String)
signal json_received(data: Dictionary)

@export var default_url: String = ""
@export var auto_reconnect: bool = false
@export var reconnect_delay: float = 3.0

var _websocket: WebSocketPeer = WebSocketPeer.new()
var _url: String = ""
var _was_connecting_or_open: bool = false
var _reconnect_timer: float = 0.0


func _ready() -> void:
	# Android: Produktions-Builds setzen network/relay/url in project.godot
	# (empfohlen wss://), sonst lokaler Dev-Server.
	if default_url.is_empty():
		default_url = String(ProjectSettings.get_setting(
			"network/relay/url", "ws://localhost:8080/kombat"))


func connect_to_server(url: String = "") -> void:
	_url = url if not url.strip_edges().is_empty() else default_url
	_websocket = WebSocketPeer.new()
	var err: Error = _websocket.connect_to_url(_url)
	if err != OK:
		connection_failed.emit("WebSocket connection failed: " + error_string(err))
		return
	_was_connecting_or_open = true


func disconnect_from_server(code: int = 1000, reason: String = "Normal closure") -> void:
	auto_reconnect = false
	_websocket.close(code, reason)
	disconnected.emit(code, reason)


func send_message(message: String) -> bool:
	if _websocket.get_ready_state() != WebSocketPeer.STATE_OPEN:
		return false
	return _websocket.send_text(message) == OK


func send_json(data: Dictionary) -> bool:
	return send_message(JSON.stringify(data))


func is_connected_to_server() -> bool:
	return _websocket.get_ready_state() == WebSocketPeer.STATE_OPEN


func _process(delta: float) -> void:
	var state_before: int = _websocket.get_ready_state()
	_websocket.poll()
	var state_after: int = _websocket.get_ready_state()

	if state_after == WebSocketPeer.STATE_OPEN:
		if state_before != WebSocketPeer.STATE_OPEN:
			connected.emit()
		_drain_packets()
	elif state_after == WebSocketPeer.STATE_CLOSED and _was_connecting_or_open:
		_was_connecting_or_open = false
		var code: int = _websocket.get_close_code()
		var reason: String = _websocket.get_close_reason()
		disconnected.emit(code, reason)
		if auto_reconnect:
			_reconnect_timer = reconnect_delay

	if auto_reconnect and _reconnect_timer > 0.0:
		_reconnect_timer -= delta
		if _reconnect_timer <= 0.0 and not _url.is_empty():
			connect_to_server(_url)


func _drain_packets() -> void:
	while _websocket.get_available_packet_count() > 0:
		var message: String = _websocket.get_packet().get_string_from_utf8()
		message_received.emit(message)
		var parsed: Variant = JSON.parse_string(message)
		if parsed is Dictionary:
			json_received.emit(parsed as Dictionary)
