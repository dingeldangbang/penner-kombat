extends Node
class_name AiService

## OpenAI API integration for combat profile generation.
## Requests strict JSON output compatible with SkillData.

signal profile_received(profile_data: Dictionary)
signal request_failed(error: String)

const API_URL: String = "https://api.openai.com/v1/chat/completions"
const DEFAULT_MODEL: String = "gpt-4o-mini"

var _http_request: HTTPRequest
var _pending_key: String = ""
var _pending_prompt: String = ""
var _pending_nodes: Array[String] = []


func _ready() -> void:
	_ensure_http_request()


func _ensure_http_request() -> void:
	if _http_request != null:
		return
	_http_request = HTTPRequest.new()
	_http_request.name = "OpenAIRequest"
	add_child(_http_request)
	_http_request.request_completed.connect(_on_request_completed)


## Request an entity profile from OpenAI.
func request_entity_profile(node_names: Array[String], user_prompt: String, api_key: String) -> void:
	_ensure_http_request()

	if api_key.strip_edges().is_empty():
		request_failed.emit("API key is empty")
		return
	if user_prompt.strip_edges().is_empty():
		request_failed.emit("Prompt is empty")
		return

	_pending_key = api_key
	_pending_nodes = node_names.duplicate()
	_pending_prompt = user_prompt

	var payload: Dictionary = _build_payload(node_names, user_prompt)
	var json_payload: String = JSON.stringify(payload)
	var headers: PackedStringArray = PackedStringArray([
		"Content-Type: application/json",
		"Authorization: Bearer " + api_key
	])

	var err: Error = _http_request.request(API_URL, headers, HTTPClient.METHOD_POST, json_payload)
	if err != OK:
		request_failed.emit("HTTP request failed: " + error_string(err))


func _build_payload(node_names: Array[String], prompt: String) -> Dictionary:
	var system_prompt: String = "You are a game design AI. Generate balanced combat profiles for a 3D fighting game. Return only valid JSON."
	var user_content: String = "Node names in scene: %s\nUser request: %s" % [str(node_names), prompt]
	var schema_description: String = """Generate JSON matching this structure:
{
  "move_speed": 5.0,
  "attack_power": 15.0,
  "combos": [
    {
      "name": "Combo1",
      "delay": 0.2,
      "damage_multiplier": 1.2,
      "hitbox_radius": 1.5,
      "vfx_color": "#FF0000",
      "attach_bone": "Hand_R"
    }
  ]
}
Use attach_bone values from Root, Head, Hand_R, Hand_L when possible.
Ranges: move_speed 3.0-8.0, attack_power 5.0-30.0, delay 0.1-0.5, damage_multiplier 0.8-2.5, hitbox_radius 0.5-3.0."""

	return {
		"model": DEFAULT_MODEL,
		"messages": [
			{"role": "system", "content": system_prompt},
			{"role": "user", "content": user_content + "\n\n" + schema_description}
		],
		"response_format": {"type": "json_object"},
		"temperature": 0.7,
		"max_tokens": 1000
	}


func _on_request_completed(_result: int, response_code: int, _headers: PackedStringArray, body: PackedByteArray) -> void:
	if response_code != 200:
		var error_text: String = "HTTP " + str(response_code)
		if body.size() > 0:
			error_text += ": " + body.get_string_from_utf8()
		request_failed.emit(error_text)
		return

	var parsed: Variant = JSON.parse_string(body.get_string_from_utf8())
	if not parsed is Dictionary:
		request_failed.emit("Invalid response from API")
		return

	var data: Dictionary = parsed as Dictionary
	if not data.has("choices") or not data["choices"] is Array or (data["choices"] as Array).is_empty():
		request_failed.emit("Invalid response from API: missing choices")
		return

	var choice: Variant = (data["choices"] as Array)[0]
	if not choice is Dictionary or not (choice as Dictionary).has("message"):
		request_failed.emit("Invalid response from API: missing message")
		return

	var message_variant: Variant = (choice as Dictionary)["message"]
	if not message_variant is Dictionary:
		request_failed.emit("Invalid response from API: malformed message")
		return
	var message: Dictionary = message_variant as Dictionary
	var content: String = str(message.get("content", ""))
	var profile_variant: Variant = JSON.parse_string(content)
	if not profile_variant is Dictionary:
		request_failed.emit("Failed to parse profile JSON")
		return

	var profile: Dictionary = profile_variant as Dictionary
	if not profile.has("move_speed") or not profile.has("attack_power") or not profile.has("combos"):
		request_failed.emit("Invalid profile structure: missing required fields")
		return

	profile_received.emit(profile)


func test_connection(api_key: String) -> void:
	request_entity_profile(["TestNode"], "Generate a simple test profile", api_key)


func cancel_request() -> void:
	if _http_request:
		_http_request.cancel_request()
