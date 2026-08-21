using System.Collections;
using UnityEngine;

namespace PennerKombat
{
    /// <summary>
    /// Mell — "Amphe-Pampe". Rushdown / Mix-up / Glass Cannon.
    /// Sonderfunktion: "Der Puls" (Ressourcen-System 60–220 bpm).
    /// Je höher der Puls, desto schneller — aber es kostet HP und endet im
    /// Blackout bei 220.
    ///
    /// Spezialbewegungen:
    ///  Special1 = Pampe (↓↙←+□)   · Special2 = Doppelschicht (→→+□)
    ///  3         = Erste Hilfe (↓↓+△) · 4 = Defi (←↙↓↘→+○)
    ///  5         = Sechzehn Stunden (Puls ≥ 160)
    /// </summary>
    public class Mell : FighterController
    {
        [Header("Mell — Puls-System")]
        public float pulse = 60f;
        public float maxPulse = 220f;
        public float pulseRisePerHit = 8f;
        public float pulseDecayPerSecond = 6f;
        public float healAmount = 12f;
        public float pulseHealIncrease = 40f;

        [Header("Mell — Specials")]
        public GameObject sludgeProjectile;
        public float defiRange = 1.5f;
        public AudioClip blackoutSound;

        public float Pulse => pulse;
        public bool IsBlackout { get; private set; }
        public bool IsCritical => pulse >= 200f;

        private float hpDrainPerSecond;

        protected override void Awake()
        {
            base.Awake();
            moves.AddRange(MoveCatalog.Get(GameConstants.CharMell));
        }

        public override void ExecuteMove(MoveData move)
        {
            if (move == null) return;
            switch (move.id)
            {
                case "pampe": if (!IsOnCooldown("pampe")) { Special1(); SetCooldown("pampe", 1.5f); } break;
                case "doppelschicht": Special2(); break;
                case "erste_hilfe": if (!IsOnCooldown("erste_hilfe")) { Heal(); SetCooldown("erste_hilfe", move.cooldown); } break;
                case "defi": if (!IsOnCooldown("defi")) { Defi(); SetCooldown("defi", 2f); } break;
                case "sechzehn": SechzehnStunden(); break;
            }
        }

        protected override void Update()
        {
            base.Update();
            if (IsBlackout) return;

            // Puls regulieren
            if (comboCount > 0 || isAttacking)
                pulse += pulseRisePerHit * Time.deltaTime;
            else
                pulse -= pulseDecayPerSecond * Time.deltaTime;
            pulse = Mathf.Clamp(pulse, 60f, maxPulse);

            // HP-Drain in hohen Puls-Zonen (Design: 100-160 → 0,4%/s · 160-200 → 1,2%/s · 200+ → 3%/s)
            if (pulse >= 200f) hpDrainPerSecond = 3f;
            else if (pulse >= 160f) hpDrainPerSecond = 1.2f;
            else if (pulse >= 100f) hpDrainPerSecond = 0.4f;
            else hpDrainPerSecond = 0f;

            if (hpDrainPerSecond > 0f)
            {
                currentHP -= hpDrainPerSecond * Time.deltaTime;
                if (currentHP < 0f) currentHP = 0f;
                if (currentHP <= 0f) { Die(); return; }
            }

            // Speed-Bonus
            float speedMul = 1f;
            if (pulse >= 200f) speedMul = 1.5f;
            else if (pulse >= 160f) speedMul = 1.35f;
            else if (pulse >= 100f) speedMul = 1.15f;
            moveSpeed = base.moveSpeed * speedMul;

            // Blackout
            if (pulse >= maxPulse) StartCoroutine(Blackout());
        }

        IEnumerator Blackout()
        {
            if (IsBlackout) yield break;
            IsBlackout = true;
            if (blackoutSound != null) AudioSource.PlayClipAtPoint(blackoutSound, transform.position);
            // Blackout-Präsentation: Schwarzbild + elektrisches Flackern (3 s)
            if (!isAI) ScreenEffects.Blackout(3f);
            VFXManager.Instance?.PlayElectro(transform.position + Vector3.up, 6);
            yield return new WaitForSeconds(3f);
            pulse = 60f;
            IsBlackout = false;
        }

        protected override void HandleSpecialInput()
        {
            var input = FighterInput.Instance;
            if (input == null) return;

            if (input.GetSpecial1(playerIndex) && !isBlocking) Special1();
            if (input.GetSpecial2(playerIndex) && !isBlocking) Special2();

            if (UnityEngine.InputSystem.Keyboard.current != null && playerIndex == 0)
            {
                if (UnityEngine.InputSystem.Keyboard.current[UnityEngine.InputSystem.Key.Alpha1].wasPressedThisFrame) Heal();
                if (UnityEngine.InputSystem.Keyboard.current[UnityEngine.InputSystem.Key.Alpha2].wasPressedThisFrame) Defi();
                if (UnityEngine.InputSystem.Keyboard.current[UnityEngine.InputSystem.Key.Alpha3].wasPressedThisFrame) SechzehnStunden();
            }
        }

        public override void Special1() // Pampe
        {
            if (sludgeProjectile != null)
            {
                var go = Instantiate(sludgeProjectile, attackPoint.position, transform.rotation);
                var proj = go.GetComponent<SludgeProjectile>();
                if (proj != null) proj.Initialize(transform.forward, this);
            }
            pulse += 15f;
        }

        public override void Special2() // Doppelschicht
        {
            Collider[] hits = Physics.OverlapSphere(attackPoint.position, attackRange, enemyLayer);
            foreach (var hit in hits)
            {
                var enemy = hit.GetComponent<FighterController>();
                if (enemy != null && enemy != this)
                    enemy.TakeDamage(9f, transform.forward, this);
            }
            pulse += 20f;
        }

        public void Heal() // Erste Hilfe: +12% HP, +40 bpm
        {
            if (pulse >= 200f || currentHP >= maxHP) return;
            currentHP = Mathf.Min(currentHP + healAmount, maxHP);
            pulse += pulseHealIncrease;
            pulse = Mathf.Clamp(pulse, 60f, maxPulse);
        }

        public void Defi() // Buff-Purge
        {
            Collider[] hits = Physics.OverlapSphere(attackPoint.position, defiRange, enemyLayer);
            foreach (var hit in hits)
            {
                var enemy = hit.GetComponent<FighterController>();
                if (enemy != null && enemy != this)
                {
                    enemy.BroadcastMessage("PurgeBuffs", SendMessageOptions.DontRequireReceiver);
                    enemy.TakeDamage(8f, transform.forward, this);
                    // Defi: blaue Arcs + Überbelichtung des Gegners
                    VFXManager.Instance?.PlayElectro(enemy.transform.position + Vector3.up * 1.1f);
                    PlayHitFeedback(enemy, 8f, HitTier.Special);
                }
            }
            pulse += 20f;
        }

        public void SechzehnStunden() // nur bei Puls ≥ 160
        {
            if (pulse < 160f || IsBlackout) return;
            StartCoroutine(SechzehnStundenCoroutine());
        }

        IEnumerator SechzehnStundenCoroutine()
        {
            float angleStep = 360f / 11f;
            float interval = 0.5f / 11f;
            for (int i = 0; i < 11; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                Collider[] hits = Physics.OverlapSphere(attackPoint.position, attackRange, enemyLayer);
                foreach (var hit in hits)
                {
                    var enemy = hit.GetComponent<FighterController>();
                    if (enemy != null && enemy != this)
                        enemy.TakeDamage(3.5f, dir, this);
                }
                yield return new WaitForSeconds(interval);
            }
            pulse = Mathf.Max(60f, pulse - 60f);
        }

        // Puls-Interaktion mit Schaden: Treffer senkt Puls (Adrenalin-Schock)
        protected override void OnReceivedHit(FighterController attacker, float damage)
        {
            base.OnReceivedHit(attacker, damage);
            if (IsBlackout) return;
            pulse = Mathf.Max(60f, pulse - 10f);
        }

        protected override void OnBlocked(FighterController attacker)
        {
            base.OnBlocked(attacker);
            if (IsBlackout) return;
            pulse = Mathf.Max(60f, pulse - 8f);
        }

        public override void ResetForRound()
        {
            base.ResetForRound();
            pulse = 60f;
            IsBlackout = false;
            hpDrainPerSecond = 0f;
        }

        // ---- Fatality "Schichtenende" + Trophäen 53 & 56 ----
        public override void PerformFatality(FighterController target, string fatalityId)
        {
            base.PerformFatality(target, fatalityId);
            lastFatalityDone = true;
            // Platin-Trophäe 56 braucht Online + Rematch — wird von FatalitySystem ergänzt
        }

        public bool lastFatalityDone;
    }
}
