using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PennerKombat
{
    /// <summary>
    /// Abstrakte Basisklasse für jeden Kämpfer (Spieler UND KI).
    /// Kümmert sich um: Bewegung, Block, Angriffe/Hitboxen, Combo,
    /// Schaden/Knockback, Stun, Fatal-Blow-Leiste und Sterben.
    /// </summary>
    /// <remarks>
    /// WICHTIG für Unterklassen: Zugriff über geschützte Mitglieder
    /// (rb, anim, stunTimer, StartAttack, Jump …) und Überschreiben über
    /// virtual. NIEMALS private Felder der Basis anfassen.
    /// </remarks>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Animator))]
    public abstract class FighterController : MonoBehaviour
    {
        [Header("Status")]
        public string fighterId = GameConstants.CharLeBinde;
        public string displayName = "Fighter";
        public float maxHP = 100f;
        [HideInInspector] public float currentHP;
        public float moveSpeed = 5f;
        public float attackRange = 2.5f;
        public float lightDamage = 8f;
        public float heavyDamage = 15f;
        public float lightCooldown = 0.4f;
        public float heavyCooldown = 0.8f;
        public float comboWindow = GameConstants.ComboWindow;
        public bool isBlocking;
        public bool isGrounded = true;
        [HideInInspector] public int comboCount;
        [HideInInspector] public float blockReductionBonus;   // z.B. Uschis Topfdeckel, Sigis Firewall

        // Eingabe-Steuerung
        public bool isAI;              // true => wird von AIController gesteuert
        public int playerIndex = 0;    // 0 = Spieler 1, 1 = Spieler 2

        [Header("Hitboxes")]
        public Transform attackPoint;
        public Vector3 attackBoxSize = new Vector3(1.5f, 1.5f, 2f);
        public LayerMask enemyLayer;

        [Header("References")]
        public GameObject hitEffectPrefab;
        public GameObject blockEffectPrefab;
        public AudioClip hitSound;
        public AudioClip blockSound;
        public AudioClip hurtSound;
        public AudioClip fatalBlowSound;

        [Header("Physics")]
        public float gravity = GameConstants.DefaultGravity;
        public float jumpForce = GameConstants.DefaultJumpForce;
        public float knockbackForce = GameConstants.DefaultKnockback;
        public float knockbackUp = GameConstants.DefaultKnockbackUp;

        // Geschützte / interne Felder (für Unterklassen)
        protected Rigidbody rb;
        protected Animator anim;
        protected float attackTimer;
        protected float stunTimer;
        protected bool isAttacking;
        protected bool isHeavy;
        protected float lastHitTime;
        protected HashSet<FighterController> hitThisAttack = new HashSet<FighterController>();

        // Fatal Blow (X-Ray) Leiste
        [HideInInspector] public float fatalBlowMeter;
        [HideInInspector] public bool fatalBlowReady;

        // Moveset / Command-Input
        [HideInInspector] public List<MoveData> moves = new List<MoveData>();
        [HideInInspector] public CommandInput commandInput;

        // Events (für GameManager / UI / Trophäen / Netzwerk)
        public event System.Action<FighterController> OnDeath;
        public event System.Action<FighterController, FighterController, float> OnDamageDealt;   // attacker, target, amount
        public event System.Action<FighterController, FighterController, float> OnDamaged;       // target, attacker, amount
        public event System.Action<FighterController> OnFatalBlowReady;

        protected virtual void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            anim = GetComponent<Animator>();
            currentHP = maxHP;

            commandInput = GetComponent<CommandInput>();
            if (commandInput == null) commandInput = gameObject.AddComponent<CommandInput>();

            // Visuelle Signatur (Aura, Trail, Zustands-FX) — docs/VISUALS.md §2
            if (GetComponent<CharacterVisuals>() == null) gameObject.AddComponent<CharacterVisuals>();
            if (GetComponent<CharacterShaderBinder>() == null) gameObject.AddComponent<CharacterShaderBinder>();
        }

        private readonly Dictionary<string, float> moveCooldowns = new Dictionary<string, float>();

        protected void TickMoveCooldowns()
        {
            if (moveCooldowns.Count == 0) return;
            var keys = new List<string>(moveCooldowns.Keys);
            foreach (var k in keys)
            {
                moveCooldowns[k] -= Time.deltaTime;
                if (moveCooldowns[k] <= 0f) moveCooldowns.Remove(k);
            }
        }

        protected bool IsOnCooldown(string id) => moveCooldowns.TryGetValue(id, out float t) && t > 0f;
        protected void SetCooldown(string id, float seconds) => moveCooldowns[id] = seconds;

        protected virtual void Update()
        {
            TickMoveCooldowns();
            if (attackTimer > 0) attackTimer -= Time.deltaTime;
            if (stunTimer > 0)
            {
                stunTimer -= Time.deltaTime;
                return;
            }

            // Eingabe holen (nur wenn NICHT KI-gesteuert)
            Vector3 moveDir = Vector3.zero;
            if (!isAI && FighterInput.Instance != null)
            {
                moveDir = FighterInput.Instance.GetMoveDirection(playerIndex);
                isBlocking = FighterInput.Instance.GetBlock(playerIndex);

                if (FighterInput.Instance.GetJump(playerIndex) && isGrounded)
                    Jump();
                if (FighterInput.Instance.GetLightAttack(playerIndex) && attackTimer <= 0 && !isBlocking)
                    StartAttack(false);
                if (FighterInput.Instance.GetHeavyAttack(playerIndex) && attackTimer <= 0 && !isBlocking)
                    StartAttack(true);

                HandleSpecialInput();
            }

            // Spezialeingaben über das Command-System
            if (!isAI && commandInput != null && !isBlocking && !isAttacking && stunTimer <= 0f)
            {
                MoveData move = commandInput.ProcessInput();
                if (move != null) ExecuteMove(move);
            }

            Move(moveDir);
            if (anim != null) anim.SetBool("Block", isBlocking);
        }

        /// <summary>Hook für Charakter-Spezialeingaben. Wird in Unterklassen überschrieben.</summary>
        protected virtual void HandleSpecialInput() { }

        /// <summary>Führt eine erkannte Spezialbewegung aus. Wird in Unterklassen überschrieben.</summary>
        public virtual void ExecuteMove(MoveData move) { }

        /// <summary>Findet den Gegner (für Command-Input, Projektile, Buffs).</summary>
        public FighterController GetEnemy()
        {
            var all = FindObjectsOfType<FighterController>();
            foreach (var f in all)
                if (f != this && f.gameObject.activeSelf) return f;
            return null;
        }

        protected virtual void Move(Vector3 dir)
        {
            // Sigis Root-Zugriff invertiert die Bewegung
            if (GetComponent<InputHack>() is { } hack && hack.Active)
                dir = -dir;
            if (isBlocking) dir *= GameConstants.BlockMoveScale;
            // TetraPaks "Kater" verlangsamt
            if (GetComponent<SlowDebuff>() is { } slow && slow.IsActive)
                dir *= slow.speedScale;
            rb.velocity = new Vector3(dir.x * moveSpeed, rb.velocity.y, dir.z * moveSpeed);
            if (dir.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(dir);
                if (anim != null) anim.SetBool("Walk", true);
            }
            else if (anim != null) anim.SetBool("Walk", false);
        }

        /// <summary>Empfänger für Mells "Pampe"-Sludge (broadcastet vom Projektil).</summary>
        public void ApplySludge(float duration)
        {
            var stick = GetComponent<SludgeStick>();
            if (stick == null) stick = gameObject.AddComponent<SludgeStick>();
            stick.Apply(duration);
        }

        /// <summary>Empfänger für Eingabe-Invertierung (Mojo Bobs Beutelchen / Sigis Hack).</summary>
        public void InvertInputs(float duration)
        {
            var hack = GetComponent<InputHack>();
            if (hack == null) hack = gameObject.AddComponent<InputHack>();
            hack.Enable(duration);
        }

        /// <summary>Buffs zurücksetzen (wird von Mells Defi broadcastet). Unterklassen überschreiben.</summary>
        public virtual void PurgeBuffs() { }

        protected bool IsStuck() => GetComponent<SludgeStick>() is { } s && s.IsStuck;

        /// <summary>Öffentlicher Sprung (KI darf ihn ebenfalls aufrufen).</summary>
        public virtual void Jump()
        {
            if (!isGrounded || IsStuck()) return;
            rb.velocity = new Vector3(rb.velocity.x, jumpForce, rb.velocity.z);
            isGrounded = false;
            if (anim != null) anim.SetTrigger("Jump");
        }

        /// <summary>Öffentlicher Angriffsstart (KI- und Trigger-API).</summary>
        public virtual void StartAttack(bool heavy)
        {
            if (isAttacking || isBlocking) return;
            isAttacking = true;
            isHeavy = heavy;
            hitThisAttack.Clear();
            attackTimer = heavy ? heavyCooldown : lightCooldown;
            if (anim != null) anim.SetTrigger(heavy ? "HeavyAttack" : "LightAttack");
            Invoke(nameof(EnableHitbox), 0.12f);
            Invoke(nameof(DisableHitbox), 0.35f);
        }

        // --- Hitboxen ---
        protected virtual void EnableHitbox()
        {
            if (!isAttacking) return;
            Collider[] hits = Physics.OverlapBox(
                attackPoint != null ? attackPoint.position : transform.position,
                attackBoxSize / 2f,
                attackPoint != null ? attackPoint.rotation : transform.rotation,
                enemyLayer);

            foreach (Collider c in hits)
            {
                FighterController enemy = c.GetComponent<FighterController>();
                if (enemy == null || enemy == this || hitThisAttack.Contains(enemy)) continue;
                hitThisAttack.Add(enemy);

                float dmg = isHeavy ? heavyDamage : lightDamage;
                dmg = ResolveDamage(enemy, dmg);
                enemy.TakeDamage(dmg, transform.forward, this);

                // Combo
                if (Time.time - lastHitTime < comboWindow) comboCount++;
                else comboCount = 1;
                lastHitTime = Time.time;

                OnDamageDealt?.Invoke(this, enemy, dmg);
                OnDealtHit(enemy, dmg);

                // --- Visuelles Trefferfeedback (docs/VISUALS.md §4) ---
                Vector3 impact = c.ClosestPoint(attackPoint != null ? attackPoint.position : transform.position);
                PlayHitFeedback(enemy, impact, dmg, isHeavy ? HitTier.Heavy : HitTier.Light);

                if (hitEffectPrefab != null)
                    Instantiate(hitEffectPrefab, c.ClosestPoint(transform.position), Quaternion.identity);
                if (hitSound != null) AudioSource.PlayClipAtPoint(hitSound, transform.position);
            }
        }

        /// <summary>
        /// Spielt Partikel, Screen-Shake, Hitstop und Combo-Eskalation für einen
        /// gelandeten Treffer. Unterklassen rufen das für Spezials/EX/Krits mit
        /// der passenden <see cref="HitTier"/> auf.
        /// </summary>
        public void PlayHitFeedback(FighterController target, Vector3 impactPoint, float damage, HitTier tier)
        {
            Vector3 dir = target != null
                ? (target.transform.position - transform.position).normalized
                : transform.forward;

            VFXManager.Instance?.PlayHit(impactPoint, dir, damage, tier, fighterId);
            ComboSystem.Instance?.RegisterHit(this, target, damage, tier);
            target?.GetComponent<CharacterShaderBinder>()?.HitFlash(
                tier >= HitTier.Ex ? 1f : tier == HitTier.Special ? 0.7f : 0.45f);
        }

        /// <summary>Kurzform: Trefferfeedback auf Höhe der Brust des Ziels.</summary>
        public void PlayHitFeedback(FighterController target, float damage, HitTier tier)
        {
            Vector3 p = target != null ? target.transform.position + Vector3.up * 1.1f
                                       : transform.position + transform.forward;
            PlayHitFeedback(target, p, damage, tier);
        }

        /// <summary>Krit-/Buff-Anpassung des Schadens. Wird in Unterklassen (MojoBob) überschrieben.</summary>
        protected virtual float ResolveDamage(FighterController target, float baseDamage) => baseDamage;

        /// <summary>Hook nach gelandetem Treffer (Mell-Puls, LeBinde-Buff, MojoBob).</summary>
        protected virtual void OnDealtHit(FighterController target, float damage) { }

        protected virtual void DisableHitbox() => isAttacking = false;

        // --- Schaden ---
        public virtual void TakeDamage(float damage, Vector3 knockbackDir, FighterController attacker)
        {
            if (isBlocking)
            {
                damage *= Mathf.Max(0.05f, GameConstants.BlockDamageReduction - blockReductionBonus);
                if (blockEffectPrefab != null)
                    Instantiate(blockEffectPrefab, transform.position + Vector3.up, Quaternion.identity);
                if (blockSound != null) AudioSource.PlayClipAtPoint(blockSound, transform.position);
                VFXManager.Instance?.PlayBlock(transform.position + Vector3.up * 1.1f, knockbackDir);
                StartCoroutine(BlockStun(0.15f));
                OnBlocked(attacker);
                return;
            }

            currentHP -= damage;
            if (currentHP < 0) currentHP = 0;
            if (hurtSound != null) AudioSource.PlayClipAtPoint(hurtSound, transform.position);

            Vector3 kb = knockbackDir.normalized * knockbackForce + Vector3.up * knockbackUp;
            rb.AddForce(kb, ForceMode.Impulse);

            stunTimer = 0.2f;
            if (anim != null) anim.SetTrigger("HitReact");

            // Fatal-Blow-Leiste füllt sich beim KASSIEREN von Schaden
            AddFatalBlow(GameConstants.FatalBlowChargePerHitTaken);

            OnDamaged?.Invoke(this, attacker, damage);
            OnReceivedHit(attacker, damage);

            if (currentHP <= 0) Die();
        }

        /// <summary>Hook wenn Schaden erhalten (MojoBob +1 Mojo, Mell Puls).</summary>
        protected virtual void OnReceivedHit(FighterController attacker, float damage) { }

        /// <summary>Hook wenn ein Angriff geblockt wurde.</summary>
        protected virtual void OnBlocked(FighterController attacker) { }

        protected virtual IEnumerator BlockStun(float duration)
        {
            yield return new WaitForSeconds(duration);
        }

        /// <summary>Schadensreduktion / Abrutsch-Sonderfälle (LeBinde-Schlüppa) überschreiben hier.</summary>
        public virtual float ModifyIncomingDamage(float incoming)
        {
            return incoming;
        }

        // --- Fatal Blow (X-Ray) ---
        protected void AddFatalBlow(float amount)
        {
            fatalBlowMeter = Mathf.Clamp(fatalBlowMeter + amount, 0f, GameConstants.FatalBlowMeterMax);
            if (!fatalBlowReady && fatalBlowMeter >= GameConstants.FatalBlowMeterMax)
            {
                fatalBlowReady = true;
                OnFatalBlowReady?.Invoke(this);
            }
        }

        public void ResetFatalBlow()
        {
            fatalBlowMeter = 0f;
            fatalBlowReady = false;
        }

        /// <summary>Heilt den Kämpfer (für Supports wie Uschi).</summary>
        public virtual void HealSelf(float amount)
        {
            currentHP = Mathf.Min(currentHP + amount, maxHP);
        }

        /// <summary>Wird vom FatalBlowSystem aufgerufen, wenn der Spieler den X-Ray aktiviert.</summary>
        public virtual bool ExecuteFatalBlow(FighterController target)
        {
            if (!fatalBlowReady || target == null) return false;
            if (fatalBlowSound != null) AudioSource.PlayClipAtPoint(fatalBlowSound, transform.position);
            // X-Ray-Präsentation: Zeitlupe 0,5x, harter Shake, Röntgen-Blitz
            CameraShake.SlowMotion(0.5f, 0.8f);
            SaveSystem.Instance?.RecordFatalBlow();
            PlayHitFeedback(target, target.transform.position + Vector3.up * 1.1f,
                            GameConstants.FatalBlowDamageMax, HitTier.FatalBlow);
            ScreenEffects.FlashColor(Color.white, 0.6f, 0.25f);
            ResetFatalBlow();
            return true;
        }

        /// <summary>
        /// Führt eine Fatality aus. Basis liefert den Todesschaden; Unterklassen
        /// können zusätzliche Effekte/Trophäen auslösen.
        /// </summary>
        public virtual void PerformFatality(FighterController target, string fatalityId)
        {
            if (target == null) return;
            // Fatality-Präsentation: 0,3x Zeitlupe, Blutfontäne, Linsen-Splatter
            CameraShake.SlowMotion(0.3f, 1.2f);
            SaveSystem.Instance?.RecordFatality();
            ScreenEffects.SetState(ScreenState.Fatality);
            VFXManager.Instance?.PlayHit(target.transform.position + Vector3.up * 1.1f,
                                         transform.forward, 100f, HitTier.Fatality, fighterId);
            target.TakeDamage(999f, transform.forward, this);
        }

        // --- Tod / Runde ---
        public virtual void Die()
        {
            if (anim != null) anim.SetTrigger("Death");
            VFXManager.Instance?.PlayHit(transform.position + Vector3.up, Vector3.up, 30f,
                                         HitTier.Heavy, fighterId);
            ComboSystem.Instance?.ResetAll();
            OnDeath?.Invoke(this);
            gameObject.SetActive(false);
        }

        public virtual void ResetForRound()
        {
            currentHP = maxHP;
            comboCount = 0;
            isBlocking = false;
            isAttacking = false;
            stunTimer = 0f;
            attackTimer = 0f;
            hitThisAttack.Clear();
            ResetFatalBlow();
            gameObject.SetActive(true);
        }

        // --- Spezialbewegungen (Hook für Unterklassen) ---
        public virtual void Special1() { }
        public virtual void Special2() { }

        // --- Movement-Helfer für KI ---
        public void MoveTowards(Vector3 dir, float speedScale = 1f)
        {
            Vector3 targetVel = new Vector3(dir.x, rb.velocity.y, dir.z) * (moveSpeed * speedScale);
            rb.velocity = new Vector3(targetVel.x, rb.velocity.y, targetVel.z);
            if (dir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(dir);
        }

        public void ApplyImpulse(Vector3 impulse) => rb.AddForce(impulse, ForceMode.Impulse);

        /// <summary>Versetzt den Gegner in einen Stun (für Grabs/Abrufe über andere Klassen).</summary>
        public void ApplyGrabStun(float seconds) => stunTimer = Mathf.Max(stunTimer, seconds);

        protected virtual void OnCollisionEnter(Collision col)
        {
            if (col.gameObject.CompareTag(GameConstants.TagGround))
                isGrounded = true;
        }
    }
}
