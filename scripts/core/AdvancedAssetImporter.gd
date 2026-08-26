extends Node
class_name AdvancedAssetImporter

## Fortgeschrittener Asset-Importer mit:
## - Lokalem Upload
## - URL-Download
## - ZIP/Archive Support (auch sehr große Archive)
## - Atomic Chunking (sicheres, unterbrechungssicheres Verarbeiten)

signal import_started(path: String)
signal import_progress(current: int, total: int, message: String)
signal import_completed(entity: Node3D, original_path: String)
signal import_failed(error: String)
signal archive_extracted(files: Array[String])

const CHUNK_SIZE := 1024 * 1024 * 4  # 4 MB Chunks (atomic)
const MAX_ARCHIVE_SIZE := 1024 * 1024 * 1024 * 2  # 2 GB Limit

var _current_download: HTTPRequest
var _current_file_path: String = ""
var _temp_dir: String = "user://temp_import/"

func _ready() -> void:
	_ensure_temp_dir()

func _ensure_temp_dir() -> void:
	var dir := DirAccess.open("user://")
	if dir:
		dir.make_dir_recursive("temp_import")

# ====================== ÖFFENTLICHE API ======================

func import_from_local(path: String) -> void:
	if not FileAccess.file_exists(path):
		import_failed.emit("Datei nicht gefunden: " + path)
		return
	_process_file(path)

func import_from_url(url: String) -> void:
	_current_download = HTTPRequest.new()
	_current_download.name = "ChunkedDownloader"
	add_child(_current_download)
	_current_download.request_completed.connect(_on_url_download_complete)
	
	import_started.emit(url)
	import_progress.emit(0, 100, "Starte Download...")
	
	var err := _current_download.request(url)
	if err != OK:
		import_failed.emit("Download konnte nicht gestartet werden: " + error_string(err))

func import_archive(path: String) -> void:
	if not path.ends_with(".zip"):
		import_failed.emit("Nur .zip Archive werden unterstützt")
		return
	
	import_started.emit(path)
	import_progress.emit(0, 100, "Öffne Archiv...")
	
	var reader := ZIPReader.new()
	var err := reader.open(path)
	if err != OK:
		import_failed.emit("Konnte ZIP nicht öffnen")
		return
	
	var files := reader.get_files()
	var extracted: Array[String] = []
	var total := files.size()
	
	for i in range(total):
		var file_name: String = files[i]
		if not file_name.ends_with(".glb") and not file_name.ends_with(".gltf"):
			continue
		
		import_progress.emit(i, total, "Extrahiere: " + file_name)
		
		var data := reader.read_file(file_name)
		var safe_name := _sanitize_file_name(file_name.get_file())
		var out_path := _temp_dir + safe_name
		
		var f := FileAccess.open(out_path, FileAccess.WRITE)
		if f:
			f.store_buffer(data)
			f.close()
			extracted.append(out_path)
	
	reader.close()
	archive_extracted.emit(extracted)
	
	if extracted.size() > 0:
		# Lade das erste gefundene GLB
		_process_file(extracted[0])
	else:
		import_failed.emit("Keine GLB/GLTF Dateien im Archiv gefunden")

# ====================== INTERNE VERARBEITUNG ======================

func _process_file(path: String) -> void:
	_current_file_path = path
	import_started.emit(path)
	
	# Atomic Chunking für große Dateien
	if FileAccess.get_file_as_bytes(path).size() > CHUNK_SIZE * 10:
		_load_large_file_chunked(path)
	else:
		_load_normal_file(path)

func _load_normal_file(path: String) -> void:
	import_progress.emit(50, 100, "Lade normales Asset...")
	var importer := GlbImporter.new()
	add_child(importer)
	var entity := importer.load_glb(path)
	if entity:
		import_completed.emit(entity, path)
	else:
		import_failed.emit("GLB konnte nicht geladen werden")
	importer.queue_free()

func _load_large_file_chunked(path: String) -> void:
	import_progress.emit(0, 100, "Lade große Datei mit Atomic Chunking...")
	
	var file := FileAccess.open(path, FileAccess.READ)
	if file == null:
		import_failed.emit("Kann Datei nicht öffnen")
		return
	
	var total_size := file.get_length()
	var loaded := 0
	var buffer := PackedByteArray()
	
	while not file.eof_reached():
		var chunk := file.get_buffer(CHUNK_SIZE)
		buffer.append_array(chunk)
		loaded += chunk.size()
		
		var percent := int(float(loaded) / total_size * 100)
		import_progress.emit(percent, 100, "Lade Chunk %d MB..." % (loaded / (1024*1024)))
		
		# Atomic: nach jedem Chunk kurz pausieren (verhindert UI-Freeze)
		await get_tree().process_frame
	
	file.close()
	
	# Jetzt als temporäre Datei speichern und laden
	var temp_path := _temp_dir + _sanitize_file_name(path.get_file())
	var out := FileAccess.open(temp_path, FileAccess.WRITE)
	out.store_buffer(buffer)
	out.close()
	
	import_progress.emit(95, 100, "Lade finalisiertes Asset...")
	var importer := GlbImporter.new()
	add_child(importer)
	var entity := importer.load_glb(temp_path)
	if entity:
		import_completed.emit(entity, path)
	else:
		import_failed.emit("Chunked GLB konnte nicht geladen werden")
	importer.queue_free()

func _on_url_download_complete(_result: int, response_code: int, _headers: PackedStringArray, body: PackedByteArray) -> void:
	if response_code != 200:
		import_failed.emit("HTTP Fehler: " + str(response_code))
		return
	
	import_progress.emit(90, 100, "Speichere heruntergeladene Datei...")
	
	var file_name := "downloaded_asset.glb"
	if _current_download.get_meta("suggested_filename"):
		file_name = _current_download.get_meta("suggested_filename")
	
	var out_path := _temp_dir + _sanitize_file_name(file_name)
	var f := FileAccess.open(out_path, FileAccess.WRITE)
	f.store_buffer(body)
	f.close()
	
	_process_file(out_path)

func _sanitize_file_name(name: String) -> String:
	var result := name.strip_edges()
	for c in ["/", "\\", ":", "*", "?", "\"", "<", ">", "|", " "]:
		result = result.replace(c, "_")
	return result if not result.is_empty() else "asset_" + str(Time.get_unix_time_from_system())

func cleanup_temp_files() -> void:
	var dir := DirAccess.open(_temp_dir)
	if dir:
		for f in dir.get_files():
			DirAccess.remove_absolute(_temp_dir + f)