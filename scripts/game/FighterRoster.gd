extends RefCounted
class_name FighterRoster

## Fest eingebaute Kaempfer-Riege.
##
## Warum das noetig ist: der Editor kann Figuren aus GLB-Dateien bauen, aber auf
## einem frisch installierten Handy gibt es keine GLB-Datei -- und ohne geladenes
## Asset liess sich das Spiel gar nicht starten. Diese Riege wird komplett aus
## Godot-Primitiven zusammengesetzt, braucht also keine Asset-Dateien und
## funktioniert sofort nach der Installation.
##
## Die Koerperteile heissen bewusst "Head", "Hand_R", "Hand_L" und "Hips":
## DynamicRigger._classify_name() erkennt genau diese Namen und haengt Hitboxen
## und VFX daran auf. Ein namenloser Klumpen wuerde nur auf "Root" zurueckfallen
## und alle Treffer kaemen aus der Huefte.


class Fighter extends RefCounted:
	var id: String = ""
	var display_name: String = ""
	var tagline: String = ""
	var skin: Color = Color(0.8, 0.65, 0.5)
	var cloth: Color = Color(0.3, 0.3, 0.35)
	var accent: Color = Color(0.9, 0.2, 0.2)
	var move_speed: float = 4.0
	var attack_power: float = 15.0
	var max_health: float = 100.0
	var build: float = 1.0        # Koerperfuelle: 0.85 schmal .. 1.25 breit
	var height: float = 1.0       # Groessenfaktor
	var combos: Array[Dictionary] = []

	func to_skill_data() -> SkillData:
		var data: SkillData = SkillData.new()
		data.move_speed = move_speed
		data.attack_power = attack_power
		data.combos = combos.duplicate(true)
		return data


static func _combo(name: String, delay: float, mult: float, radius: float,
		color: String, bone: String) -> Dictionary:
	return {
		"name": name,
		"delay": delay,
		"damage_multiplier": mult,
		"hitbox_radius": radius,
		"vfx_color": color,
		"attach_bone": bone,
	}


## Alle waehlbaren Kaempfer, in Anzeigereihenfolge.
static func all() -> Array:
	var list: Array = []

	var kalle: Fighter = Fighter.new()
	kalle.id = "kalle"
	kalle.display_name = "Kalle Kanister"
	kalle.tagline = "Langsam, aber jeder Treffer sitzt."
	kalle.skin = Color(0.78, 0.60, 0.45)
	kalle.cloth = Color(0.24, 0.28, 0.20)
	kalle.accent = Color(0.95, 0.55, 0.10)
	kalle.move_speed = 3.2
	kalle.attack_power = 21.0
	kalle.max_health = 130.0
	kalle.build = 1.25
	kalle.height = 1.06
	kalle.combos = [
		_combo("Kanisterschwinger", 0.34, 1.30, 1.9, "#FF8C1A", "Hand_R"),
		_combo("Schulterramme", 0.42, 1.65, 2.1, "#FFB347", "Hips"),
		_combo("Blechhaken", 0.30, 1.15, 1.7, "#FF6600", "Hand_L"),
	]
	list.append(kalle)

	var sonja: Fighter = Fighter.new()
	sonja.id = "sonja"
	sonja.display_name = "Schnelle Sonja"
	sonja.tagline = "Drei Schlaege, bevor du blinzelst."
	sonja.skin = Color(0.85, 0.68, 0.55)
	sonja.cloth = Color(0.55, 0.14, 0.30)
	sonja.accent = Color(0.20, 0.85, 0.95)
	sonja.move_speed = 6.4
	sonja.attack_power = 10.5
	sonja.max_health = 82.0
	sonja.build = 0.86
	sonja.height = 0.96
	sonja.combos = [
		_combo("Jab", 0.13, 0.95, 1.3, "#33E1FF", "Hand_R"),
		_combo("Konter", 0.15, 1.05, 1.4, "#66ECFF", "Hand_L"),
		_combo("Wirbeltritt", 0.26, 1.55, 1.8, "#00BFFF", "Hips"),
	]
	list.append(sonja)

	var doktor: Fighter = Fighter.new()
	doktor.id = "doktor"
	doktor.display_name = "Doktor Dosenbier"
	doktor.tagline = "Ausgewogen. Meistens."
	doktor.skin = Color(0.80, 0.63, 0.48)
	doktor.cloth = Color(0.30, 0.32, 0.42)
	doktor.accent = Color(0.45, 0.90, 0.35)
	doktor.move_speed = 4.5
	doktor.attack_power = 15.0
	doktor.max_health = 100.0
	doktor.build = 1.0
	doktor.height = 1.0
	doktor.combos = [
		_combo("Dosenklatsche", 0.20, 1.10, 1.6, "#72E645", "Hand_R"),
		_combo("Kopfnuss", 0.28, 1.40, 1.5, "#A6F573", "Head"),
		_combo("Pfandaufschlag", 0.34, 1.70, 1.9, "#4CD137", "Hand_L"),
	]
	list.append(doktor)

	var rudi: Fighter = Fighter.new()
	rudi.id = "rudi"
	rudi.display_name = "Ratten-Rudi"
	rudi.tagline = "Glaskinn, Vorschlaghammer."
	rudi.skin = Color(0.72, 0.58, 0.48)
	rudi.cloth = Color(0.34, 0.24, 0.16)
	rudi.accent = Color(0.85, 0.85, 0.20)
	rudi.move_speed = 5.2
	rudi.attack_power = 23.0
	rudi.max_health = 70.0
	rudi.build = 0.90
	rudi.height = 0.93
	rudi.combos = [
		_combo("Rattenbiss", 0.18, 1.20, 1.4, "#E8E020", "Head"),
		_combo("Kanalhaken", 0.24, 1.75, 1.7, "#FFF176", "Hand_R"),
	]
	list.append(rudi)

	var olga: Fighter = Fighter.new()
	olga.id = "olga"
	olga.display_name = "Oma Olga"
	olga.tagline = "Die Handtasche ist voller Kleingeld."
	olga.skin = Color(0.86, 0.72, 0.62)
	olga.cloth = Color(0.48, 0.26, 0.52)
	olga.accent = Color(0.98, 0.75, 0.85)
	olga.move_speed = 3.8
	olga.attack_power = 17.5
	olga.max_health = 112.0
	olga.build = 1.08
	olga.height = 0.90
	olga.combos = [
		_combo("Handtaschenhieb", 0.26, 1.35, 1.8, "#FFB6D5", "Hand_R"),
		_combo("Gehstock-Fege", 0.32, 1.50, 2.0, "#F48FB1", "Hips"),
		_combo("Ohrfeige", 0.16, 0.90, 1.3, "#FFD1E3", "Hand_L"),
	]
	list.append(olga)

	var koenig: Fighter = Fighter.new()
	koenig.id = "koenig"
	koenig.display_name = "Der Grubenkoenig"
	koenig.tagline = "Boss der Unterfuehrung."
	koenig.skin = Color(0.62, 0.50, 0.44)
	koenig.cloth = Color(0.14, 0.12, 0.16)
	koenig.accent = Color(0.85, 0.10, 0.12)
	koenig.move_speed = 3.6
	koenig.attack_power = 24.0
	koenig.max_health = 150.0
	koenig.build = 1.32
	koenig.height = 1.14
	koenig.combos = [
		_combo("Thronschlag", 0.36, 1.60, 2.2, "#FF1E1E", "Hand_R"),
		_combo("Kronenstoss", 0.30, 1.45, 1.8, "#FF5555", "Head"),
		_combo("Grubenbeben", 0.48, 2.10, 2.6, "#B00000", "Hips"),
	]
	list.append(koenig)

	return list


static func by_id(id: String) -> Fighter:
	for f in all():
		if (f as Fighter).id == id:
			return f as Fighter
	var fallback: Array = all()
	return fallback[0] as Fighter


static func ids() -> Array[String]:
	var out: Array[String] = []
	for f in all():
		out.append((f as Fighter).id)
	return out


# ------------------------------------------------------------------ Koerperbau

static func _mat(color: Color, emission: float = 0.0) -> StandardMaterial3D:
	var m: StandardMaterial3D = StandardMaterial3D.new()
	m.albedo_color = color
	m.roughness = 0.75
	if emission > 0.0:
		m.emission_enabled = true
		m.emission = color * emission
	return m


static func _part(parent: Node3D, node_name: String, mesh: Mesh,
		pos: Vector3, mat: StandardMaterial3D) -> MeshInstance3D:
	var mi: MeshInstance3D = MeshInstance3D.new()
	mi.name = node_name
	mi.mesh = mesh
	mi.position = pos
	mi.material_override = mat
	parent.add_child(mi)
	return mi


## Baut die Figur aus Primitiven. Ergebnis ist ein Node3D, das genauso in
## CombatController gehaengt werden kann wie ein importiertes GLB.
static func build_body(fighter: Fighter) -> Node3D:
	var root: Node3D = Node3D.new()
	root.name = "PennerBody"

	var b: float = fighter.build
	var h: float = fighter.height
	var skin: StandardMaterial3D = _mat(fighter.skin)
	var cloth: StandardMaterial3D = _mat(fighter.cloth)
	var accent: StandardMaterial3D = _mat(fighter.accent, 0.35)

	# Huefte -> wird von DynamicRigger als "Root" klassifiziert.
	var hips_mesh: SphereMesh = SphereMesh.new()
	hips_mesh.radius = 0.26 * b
	hips_mesh.height = 0.42 * b
	var hips: MeshInstance3D = _part(root, "Hips", hips_mesh,
		Vector3(0, 0.42 * h, 0), cloth)

	# Rumpf
	var torso_mesh: CapsuleMesh = CapsuleMesh.new()
	torso_mesh.radius = 0.30 * b
	torso_mesh.height = 0.86 * h
	_part(root, "Torso", torso_mesh, Vector3(0, 0.86 * h, 0), cloth)

	# Mantel-Akzent (Schulterstueck), rein optisch
	var coat_mesh: CylinderMesh = CylinderMesh.new()
	coat_mesh.top_radius = 0.34 * b
	coat_mesh.bottom_radius = 0.30 * b
	coat_mesh.height = 0.16 * h
	_part(root, "Coat", coat_mesh, Vector3(0, 1.16 * h, 0), accent)

	# Kopf -> "Head"
	var head_mesh: SphereMesh = SphereMesh.new()
	head_mesh.radius = 0.20 * (0.5 + b * 0.5)
	head_mesh.height = 0.40 * (0.5 + b * 0.5)
	_part(root, "Head", head_mesh, Vector3(0, 1.46 * h, 0), skin)

	# Muetze
	var cap_mesh: CylinderMesh = CylinderMesh.new()
	cap_mesh.top_radius = 0.19
	cap_mesh.bottom_radius = 0.21
	cap_mesh.height = 0.12
	_part(root, "Cap", cap_mesh, Vector3(0, 1.62 * h, 0), accent)

	# Arme + Haende -> "Hand_R" / "Hand_L"
	var arm_mesh: CapsuleMesh = CapsuleMesh.new()
	arm_mesh.radius = 0.09 * b
	arm_mesh.height = 0.56 * h
	var hand_mesh: SphereMesh = SphereMesh.new()
	hand_mesh.radius = 0.115 * b
	hand_mesh.height = 0.23 * b

	var x: float = 0.40 * b
	_part(root, "Arm_R", arm_mesh, Vector3(x, 0.98 * h, 0), cloth)
	_part(root, "Hand_R", hand_mesh, Vector3(x, 0.66 * h, 0), skin)
	_part(root, "Arm_L", arm_mesh, Vector3(-x, 0.98 * h, 0), cloth)
	_part(root, "Hand_L", hand_mesh, Vector3(-x, 0.66 * h, 0), skin)

	# Beine
	var leg_mesh: CapsuleMesh = CapsuleMesh.new()
	leg_mesh.radius = 0.115 * b
	leg_mesh.height = 0.62 * h
	_part(root, "Leg_R", leg_mesh, Vector3(0.15 * b, 0.10 * h, 0), cloth)
	_part(root, "Leg_L", leg_mesh, Vector3(-0.15 * b, 0.10 * h, 0), cloth)

	# Blickrichtung sichtbar machen: kleine Nase am Kopf.
	var nose_mesh: BoxMesh = BoxMesh.new()
	nose_mesh.size = Vector3(0.07, 0.06, 0.12)
	_part(root, "Nose", nose_mesh, Vector3(0, 1.44 * h, -0.20), skin)

	hips.visible = true
	return root


## Kleines Vorschaumodell fuer die Charakterauswahl (gleicher Koerper, aber
## ohne Kollision/Logik -- die Auswahl zeigt es in einem SubViewport).
static func build_preview(fighter: Fighter) -> Node3D:
	return build_body(fighter)
