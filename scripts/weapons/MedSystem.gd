extends Node
class_name MedSystem

## Healing/medication charge system with cooldown and overdose guard.

signal charge_used(remaining: int)
signal charges_changed(current: int, max_charges: int)
signal overdose_triggered()

@export var owner_fighter: CombatController
@export var max_charges: int = 3
@export var heal_amount: float = 25.0
@export var cooldown: float = 1.2
@export var overdose_threshold: int = 3
@export var overdose_window: float = 8.0

var charges: int = 3
var _cooldown_timer: float = 0.0
var _recent_uses: Array[float] = []


func _ready() -> void:
	charges = clampi(charges, 0, max_charges)


func _process(delta: float) -> void:
	_cooldown_timer = maxf(_cooldown_timer - delta, 0.0)


func use_charge() -> bool:
	if charges <= 0 or _cooldown_timer > 0.0:
		return false
	charges -= 1
	_cooldown_timer = cooldown
	if owner_fighter:
		owner_fighter.heal(heal_amount)
	_register_use()
	charge_used.emit(charges)
	charges_changed.emit(charges, max_charges)
	return true


func add_charges(amount: int) -> void:
	charges = clampi(charges + amount, 0, max_charges)
	charges_changed.emit(charges, max_charges)


func _register_use() -> void:
	var now: float = Time.get_ticks_msec() / 1000.0
	_recent_uses.append(now)
	_recent_uses = _recent_uses.filter(func(t: float) -> bool: return now - t <= overdose_window)
	if _recent_uses.size() >= overdose_threshold:
		overdose_triggered.emit()
		if owner_fighter:
			owner_fighter.take_damage(heal_amount * 0.5)
		_recent_uses.clear()
