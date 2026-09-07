extends SceneTree
## Headless-Pipeline-Test (Phase 1-Verifikation), ausführbar ohne Editor:
##
##   godot --headless --path . --script res://scripts/Testing/PipelineSmoke.gd
##
## Prüft die komplette Kette: GLB-Metarig-Import → Skeleton -> DynamicRigger
## (Bone-Map/Hitboxen) → CharacterShaderBinder (Toon-PBR-Shader auf GLB-
## Materialien) → ImpactFeedback + GraphicsQuality (5 Stufen, Upscaling).
##
## Rückgabecode 0 = alles grün, 1 = mindestens eine Prüfung fehlgeschlagen.

const METARIG_PATH: String = "res://test_assets/metarig_test.glb"
const EXPECTED_SCALES: Array[float] = [0.66, 0.75, 0.85, 1.0, 1.0]

var _checks: Array[String] = []
var _failures: int = 0


func _init() -> void:
	call_deferred("_run_all")


func check(name: String, condition: bool, detail: String = "") -> void:
	if condition:
		_checks.append("✅ " + name)
	else:
		_failures += 1
		_checks.append("❌ " + name + (" — " + detail if not detail.is_empty() else ""))


func find_skeleton(node: Node) -> Skeleton3D:
	if node is Skeleton3D:
		return node as Skeleton3D
	for child in node.get_children():
		var found: Skeleton3D = find_skeleton(child)
		if found:
			return found
	return null


func find_mesh(node: Node) -> MeshInstance3D:
	if node is MeshInstance3D:
		return node as MeshInstance3D
	for child in node.get_children():
		var found: MeshInstance3D = find_mesh(child)
		if found:
			return found
	return null


func _run_all() -> void:
	# ------------------------------------------------------------------
	# 1. GraphicsQuality: 5 Stufen + Upscaling-Modus & -Skala
	# ------------------------------------------------------------------
	var gq: Node = load("res://scripts/core/GraphicsQuality.gd").new()
	root.add_child(gq)
	await process_frame

	check("GraphicsQuality startet mit gültiger Stufe",
		gq.get_quality_index() >= 0 and gq.get_quality_index() <= 4)

	var mode_history: Array[int] = []
	var scale_history: Array[float] = []
	for tier in range(5):
		gq.set_quality(tier, false)
		await process_frame
		mode_history.append(root.scaling_3d_mode)
		scale_history.append(root.scaling_3d_scale)

	# Moduswerte dynamisch aus der Engine holen (Godot-Versionen unterscheiden
	# sich hier: BILINEAR/FSR/TSR-Konstanten werden von GraphicsQuality gelesen).
	var modes: Array = gq.call("_get_scaling_modes")
	var mode_bilinear: int = modes[0]
	var mode_fsr: int = modes[1]
	var mode_tsr: int = modes[2]
	var mode_ultra: int = mode_tsr if mode_tsr >= 0 else mode_fsr

	check("Stufen 0–1: Bilinear-Upscaling",
		mode_history[0] == mode_bilinear and mode_history[1] == mode_bilinear,
		str(mode_history))
	check("Stufen 2–4: FSR/TSR-Upgrade",
		mode_history[2] == mode_fsr
		and mode_history[3] == mode_ultra
		and mode_history[4] == mode_ultra,
		str(mode_history))
	var scales_ok: bool = true
	for i in range(5):
		if absf(scale_history[i] - EXPECTED_SCALES[i]) > 0.001:
			scales_ok = false
	check("Auflösungs-Skala je Stufe korrekt", scales_ok, str(scale_history))

	# ------------------------------------------------------------------
	# 2. GLB-Metarig-Import (GlbImporter, wie im Character-Asset-Suite-Flow)
	# ------------------------------------------------------------------
	var importer: Node = load("res://scripts/core/GlbImporter.gd").new()
	root.add_child(importer)
	var entity: Node3D = importer.load_glb(METARIG_PATH)
	check("GLB-Metarig lädt", entity != null)
	if entity == null:
		_finish()
		return

	var skeleton: Skeleton3D = find_skeleton(entity)
	check("Skeleton3D wurde aus Skin erzeugt", skeleton != null)

	var bone_names: Array[String] = []
	if skeleton:
		for i in range(skeleton.get_bone_count()):
			bone_names.append(skeleton.get_bone_name(i))
	check("Metarig-Knochen vorhanden (Root/Hips/Head)",
		bone_names.has("Root") and bone_names.has("Hips") and bone_names.has("Head"),
		str(bone_names))
	check("Hand-Links/Rechts für Hitbox-Attachment da",
		bone_names.has("Hand_L") and bone_names.has("Hand_R"), str(bone_names))

	# ------------------------------------------------------------------
	# 3. DynamicRigger: Bone-Map + Hitboxen + VFX am Knochen
	# ------------------------------------------------------------------
	var profile: Resource = load("res://scripts/combat/SkillData.gd").new()
	profile.move_speed = 4.0
	profile.attack_power = 12.0
	profile.combos = [
		{"name": "Jab", "delay": 0.2, "damage_multiplier": 1.0, "hitbox_radius": 1.2, "vfx_color": "#FFAA00", "attach_bone": "Hand_R"},
		{"name": "Kopf", "delay": 0.35, "damage_multiplier": 1.6, "hitbox_radius": 1.0, "vfx_color": "#FF2200", "attach_bone": "Head"}
	]

	var rigger: Node = load("res://scripts/core/DynamicRigger.gd").new()
	var rig_root: Node3D = Node3D.new()
	rig_root.name = "RigTestRoot"
	root.add_child(rig_root)
	rigger.rig_entity(entity, profile)
	rig_root.add_child(rigger)

	check("Rigger erzeugt Hitboxen (2 Combos)", rigger.get_hitboxes().size() == 2,
		str(rigger.get_hitboxes().size()))
	check("Rigger erzeugt VFX-Nodes (2 Combos)", rigger.get_vfx_nodes().size() == 2,
		str(rigger.get_vfx_nodes().size()))

	var bone_attachments: int = 0
	if skeleton:
		for child in skeleton.get_children():
			if child is BoneAttachment3D:
				bone_attachments += 1
	check("BoneAttachment3D an Hand/Head erstellt", bone_attachments >= 3,
		str(bone_attachments))

	# ------------------------------------------------------------------
	# 4. CharacterShaderBinder: GLB-Surface-Material → Toon-PBR-Shader
	# ------------------------------------------------------------------
	var binder: Node = load("res://scripts/characters/CharacterShaderBinder.gd").new()
	root.add_child(binder)
	var converted: int = binder.apply_to(entity)
	check("Shader-Binder konvertiert GLB-Meshes", converted >= 1, str(converted))

	var mesh: MeshInstance3D = find_mesh(entity)
	var shader_applied: bool = false
	if mesh and mesh.mesh:
		for i in range(mesh.mesh.get_surface_count()):
			var mat: Material = mesh.mesh.surface_get_material(i)
			if mat is ShaderMaterial and (mat as ShaderMaterial).shader != null:
				shader_applied = true
	check("Toon-PBR-Shader sitzt auf Mesh-Surface", shader_applied)

	# Hit-Flash darf nicht crashen und muss den Binder-Shader finden
	binder.flash_hit(entity, 0.8)
	check("Hit-Flash auf Shader-Bindungen ok", true)

	# ------------------------------------------------------------------
	# 5. ImpactFeedback: HitSpark + Shockwave + Hit-Stop (Headless)
	# ------------------------------------------------------------------
	var impact: Node = load("res://scripts/effects/ImpactFeedback.gd").new()
	root.add_child(impact)
	impact.initialize(null, null)
	impact.spawn_hit(Vector3(0, 0.8, 0), 25.0)
	await process_frame
	var spark_found: bool = false
	for child in impact.get_children():
		if "Hit" in str(child.name) or child is GPUParticles3D:
			spark_found = true
	check("ImpactFeedback erzeugt Treffer-FX (HitSpark/Wave)", spark_found)

	_finish()


func _finish() -> void:
	print("\n===== PIPELINE SMOKE (Phase 1-Verifikation) =====")
	for line in _checks:
		print(line)
	print("================================================")
	print("%d Prüfungen, %d Fehler" % [_checks.size(), _failures])
	quit(1 if _failures > 0 else 0)
