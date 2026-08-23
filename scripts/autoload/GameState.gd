extends Node

## Persistenter Spielstand: gewaehlter Kaempfer, Arcade-Fortschritt, Optionen,
## Statistik. Wird als Autoload geladen und speichert nach user://savegame.cfg.
##
## Bewusst getrennt von GlobalData: GlobalData ist die fluechtige Uebergabe
## zwischen Editor und Arena (haelt sogar Node-Referenzen). Hier liegt nur, was
## einen Neustart der App ueberleben soll.

const SAVE_PATH: String = "user://savegame.cfg"

signal progress_changed()

# --- laufender Kampf (wird von den Menues gesetzt, von der Arena gelesen) ---
var selected_fighter: String = "kalle"
var opponent_fighter: String = "doktor"
var use_custom_player: bool = false     # true = Figur aus dem Editor benutzen
var mode: String = "versus"             # "versus" | "arcade" | "training"
var difficulty: int = 1                 # 0 leicht, 1 normal, 2 schwer, 3 sehr schwer, 4 boss
var rounds_to_win: int = 2
var round_seconds: float = 99.0

# --- Arcade-Fortschritt ---
var arcade_stage: int = 0
var arcade_ladder: Array[String] = []

# --- Optionen ---
var sfx_enabled: bool = true
var touch_scale: float = 1.0

# --- Statistik ---
var stats: Dictionary = {
	"matches": 0,
	"wins": 0,
	"losses": 0,
	"arcade_cleared": 0,
	"best_combo": 0,
}

var unlocked: Array[String] = []


func _ready() -> void:
	load_game()


# ------------------------------------------------------------------ Speichern

func save_game() -> void:
	var cfg: ConfigFile = ConfigFile.new()
	cfg.set_value("game", "selected_fighter", selected_fighter)
	cfg.set_value("game", "difficulty", difficulty)
	cfg.set_value("game", "rounds_to_win", rounds_to_win)
	cfg.set_value("game", "round_seconds", round_seconds)
	cfg.set_value("arcade", "stage", arcade_stage)
	cfg.set_value("arcade", "ladder", arcade_ladder)
	cfg.set_value("options", "sfx_enabled", sfx_enabled)
	cfg.set_value("options", "touch_scale", touch_scale)
	cfg.set_value("stats", "data", stats)
	cfg.set_value("stats", "unlocked", unlocked)
	var err: Error = cfg.save(SAVE_PATH)
	if err != OK:
		push_warning("Spielstand konnte nicht gespeichert werden: %s" % error_string(err))


func load_game() -> void:
	var cfg: ConfigFile = ConfigFile.new()
	if cfg.load(SAVE_PATH) != OK:
		# Erster Start: Standardwerte behalten, nichts ist kaputt.
		return
	selected_fighter = str(cfg.get_value("game", "selected_fighter", selected_fighter))
	difficulty = int(cfg.get_value("game", "difficulty", difficulty))
	rounds_to_win = int(cfg.get_value("game", "rounds_to_win", rounds_to_win))
	round_seconds = float(cfg.get_value("game", "round_seconds", round_seconds))
	arcade_stage = int(cfg.get_value("arcade", "stage", arcade_stage))

	var ladder: Variant = cfg.get_value("arcade", "ladder", [])
	arcade_ladder.clear()
	if ladder is Array:
		for entry in (ladder as Array):
			arcade_ladder.append(str(entry))

	sfx_enabled = bool(cfg.get_value("options", "sfx_enabled", sfx_enabled))
	touch_scale = float(cfg.get_value("options", "touch_scale", touch_scale))

	var loaded_stats: Variant = cfg.get_value("stats", "data", {})
	if loaded_stats is Dictionary:
		for k in (loaded_stats as Dictionary):
			stats[k] = (loaded_stats as Dictionary)[k]

	var loaded_unlocked: Variant = cfg.get_value("stats", "unlocked", [])
	unlocked.clear()
	if loaded_unlocked is Array:
		for entry in (loaded_unlocked as Array):
			unlocked.append(str(entry))


# ------------------------------------------------------------------- Arcade

func start_arcade() -> void:
	mode = "arcade"
	arcade_stage = 0
	arcade_ladder.clear()
	# Alle ausser dem eigenen Kaempfer, Boss zuletzt.
	for id in FighterRoster.ids():
		if id != selected_fighter and id != "koenig":
			arcade_ladder.append(id)
	arcade_ladder.shuffle()
	# Wer selbst den Boss spielt, kann nicht gegen sich antreten -- dann
	# uebernimmt der staerkste verbleibende Gegner das Finale.
	if selected_fighter != "koenig":
		arcade_ladder.append("koenig")
	elif not arcade_ladder.is_empty():
		var finale: String = arcade_ladder.pop_back()
		arcade_ladder.append(finale)
	opponent_fighter = arcade_ladder[0] if not arcade_ladder.is_empty() else "doktor"
	save_game()
	progress_changed.emit()


func arcade_current_opponent() -> String:
	if arcade_stage < arcade_ladder.size():
		return arcade_ladder[arcade_stage]
	return "koenig"


func arcade_advance() -> bool:
	"""Naechster Gegner. Gibt false zurueck, wenn die Leiter durch ist."""
	arcade_stage += 1
	if arcade_stage >= arcade_ladder.size():
		stats["arcade_cleared"] = int(stats.get("arcade_cleared", 0)) + 1
		if not unlocked.has("arcade_champion"):
			unlocked.append("arcade_champion")
		save_game()
		progress_changed.emit()
		return false
	opponent_fighter = arcade_ladder[arcade_stage]
	save_game()
	progress_changed.emit()
	return true


func arcade_total() -> int:
	return arcade_ladder.size()


# -------------------------------------------------------------------- Stats

func record_match(won: bool, best_combo: int) -> void:
	stats["matches"] = int(stats.get("matches", 0)) + 1
	if won:
		stats["wins"] = int(stats.get("wins", 0)) + 1
	else:
		stats["losses"] = int(stats.get("losses", 0)) + 1
	if best_combo > int(stats.get("best_combo", 0)):
		stats["best_combo"] = best_combo
	save_game()
	progress_changed.emit()


func difficulty_name() -> String:
	match difficulty:
		0: return "Leicht"
		1: return "Normal"
		2: return "Schwer"
		3: return "Sehr schwer"
		_: return "Boss"
