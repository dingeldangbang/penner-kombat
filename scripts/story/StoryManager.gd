extends Node
class_name StoryManager

## Story progression manager with eight playable chapter slots and ending resolution.

signal chapter_started(chapter_id: String)
signal chapter_completed(chapter_id: String)
signal ending_unlocked(ending_id: String)

@export var save_path: String = "user://story_progress.json"

var chapters: Array[Dictionary] = [
	{"id": "kapitel_1", "title": "Der Blaue Eimer", "unlocked": true, "completed": false},
	{"id": "kapitel_2", "title": "Pfandkrieg", "unlocked": false, "completed": false},
	{"id": "kapitel_3", "title": "Unter der Brücke", "unlocked": false, "completed": false},
	{"id": "kapitel_4", "title": "Mops-Kommando", "unlocked": false, "completed": false},
	{"id": "kapitel_5", "title": "Fusel-Atem", "unlocked": false, "completed": false},
	{"id": "kapitel_6", "title": "Atzenalarm", "unlocked": false, "completed": false},
	{"id": "kapitel_7", "title": "Mercedes 190e", "unlocked": false, "completed": false},
	{"id": "kapitel_8", "title": "König der Platte", "unlocked": false, "completed": false}
]

var endings: Dictionary = {
	"good": false,
	"bad": false,
	"secret": false,
	"chaos": false
}
var current_chapter: String = ""


func _ready() -> void:
	load_progress()


func start_chapter(chapter_id: String) -> bool:
	var chapter: Dictionary = _get_chapter(chapter_id)
	if chapter.is_empty() or not bool(chapter.get("unlocked", false)):
		return false
	current_chapter = chapter_id
	chapter_started.emit(chapter_id)
	return true


func complete_chapter(chapter_id: String) -> void:
	for i in range(chapters.size()):
		if chapters[i].get("id", "") == chapter_id:
			chapters[i]["completed"] = true
			if i + 1 < chapters.size():
				chapters[i + 1]["unlocked"] = true
			chapter_completed.emit(chapter_id)
			break
	_resolve_endings()
	save_progress()


func _resolve_endings() -> void:
	var completed_count: int = 0
	for chapter in chapters:
		if bool(chapter.get("completed", false)):
			completed_count += 1
	if completed_count >= 8 and not endings["good"]:
		endings["good"] = true
		ending_unlocked.emit("good")
	if int(GlobalData.player_stats.get("losses", 0)) >= 5 and not endings["bad"]:
		endings["bad"] = true
		ending_unlocked.emit("bad")
	if int(GlobalData.player_stats.get("total_combos", 0)) >= 100 and not endings["secret"]:
		endings["secret"] = true
		ending_unlocked.emit("secret")


func _get_chapter(chapter_id: String) -> Dictionary:
	for chapter in chapters:
		if chapter.get("id", "") == chapter_id:
			return chapter
	return {}


func save_progress() -> void:
	var file: FileAccess = FileAccess.open(save_path, FileAccess.WRITE)
	if file:
		file.store_string(JSON.stringify({"chapters": chapters, "endings": endings}, "\t"))


func load_progress() -> void:
	if not FileAccess.file_exists(save_path):
		return
	var parsed: Variant = JSON.parse_string(FileAccess.get_file_as_string(save_path))
	if parsed is Dictionary:
		chapters = (parsed as Dictionary).get("chapters", chapters)
		endings = (parsed as Dictionary).get("endings", endings)
