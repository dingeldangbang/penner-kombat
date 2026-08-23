extends Node
class_name KryptaManager

## Unlock shop/krypta manager using Pfand-Münzen and persistent unlock state.

signal coins_changed(coins: int)
signal item_unlocked(item_id: String)
signal purchase_failed(item_id: String, reason: String)

@export var save_path: String = "user://krypta.json"

var coins: int = 0
var items: Dictionary = {
	"skin_le_binde_gold": {"name": "Goldene Binde", "cost": 50, "unlocked": false},
	"skin_mojo_bob_neon": {"name": "Neon Bob", "cost": 50, "unlocked": false},
	"arena_blauer_eimer": {"name": "Zum Blauen Eimer", "cost": 80, "unlocked": false},
	"fatality_xray": {"name": "Röntgen-Fatality", "cost": 120, "unlocked": false}
}


func _ready() -> void:
	load_state()


func add_coins(amount: int) -> void:
	coins = max(coins + amount, 0)
	coins_changed.emit(coins)
	save_state()


func purchase(item_id: String) -> bool:
	if not items.has(item_id):
		purchase_failed.emit(item_id, "Unknown item")
		return false
	var item: Dictionary = items[item_id]
	if bool(item.get("unlocked", false)):
		purchase_failed.emit(item_id, "Already unlocked")
		return false
	var cost: int = int(item.get("cost", 0))
	if coins < cost:
		purchase_failed.emit(item_id, "Not enough Pfand-Münzen")
		return false
	coins -= cost
	item["unlocked"] = true
	items[item_id] = item
	coins_changed.emit(coins)
	item_unlocked.emit(item_id)
	save_state()
	return true


func is_unlocked(item_id: String) -> bool:
	return items.has(item_id) and bool(items[item_id].get("unlocked", false))


func save_state() -> void:
	var file: FileAccess = FileAccess.open(save_path, FileAccess.WRITE)
	if file:
		file.store_string(JSON.stringify({"coins": coins, "items": items}, "\t"))


func load_state() -> void:
	if not FileAccess.file_exists(save_path):
		return
	var parsed: Variant = JSON.parse_string(FileAccess.get_file_as_string(save_path))
	if parsed is Dictionary:
		coins = int((parsed as Dictionary).get("coins", coins))
		items = (parsed as Dictionary).get("items", items)
