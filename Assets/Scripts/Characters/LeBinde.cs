using System.Collections;
using UnityEngine;

namespace PennerKombat
{
    /// <summary>
    /// Le Binde — "Der Schlachter vom Blauen Eimer".
    /// Grappler / Zoner-Killer / Panic-Button. Superschwer (immun gegen
    /// kleine Launcher). Sonderfunktion: Schmier-Schlüppa (Fett-Rüstung).
    ///
    /// Spezialbewegungen:
    ///  Special1 = Flaschenhals (↓↘→+□)   · Special2 = Mops-Kommando (↓↘→+○)
    ///  3         = Der große Schwung (←→+△)
    ///  4         = REIF! (↓↓+○)           ·  5 = Aus der Pfanne (→→+△)
    /// </summary>
    public class LeBinde : FighterController
    {
        [Header("Le Binde Specials")]
        public float flaschenHalsDamage = 11f;
        public float flaschenHalsCooldown = 5f;
        public GameObject bottleProjectile;
        public float mopsCooldown = 22f;
        public GameObject mopsPrefab;
        public float bigSwingDamage = 16f;
        public float reifBuffMultiplier = 1.8f;
        public float panDamage = 14f;

        [Header("Schmier-Schlüppa (Fett-Rüstung)")]
        public float greaseReduction = 0.6f;
        public float grabSlipChance = 0.45f;
        public int greaseChargesMax = 3;
        public float greaseBurnDuration = 8f;   // nach 3 Feuertreffern wirkungslos

        private float flaschenTimer;
        private float mopsTimer;
        private int greaseCharges;
        private bool greaseActive = true;
        private int fireHitsTaken;
        private float greaseBurnTimer;
        private float critChance;
        private float critTimer;
        private float reifTimer;

        protected override void Awake()
        {
            base.Awake();
            greaseCharges = greaseChargesMax;
            moves.AddRange(MoveCatalog.Get(GameConstants.CharLeBinde));
        }

        public override void ExecuteMove(MoveData move)
        {
            if (move == null) return;
            switch (move.id)
            {
                case "flaschenhals": Flaschenhals(false); break;
                case "flaschenhals_ex": Flaschenhals(true); break;
                case "grosser_schwung": GroesserSchwung(); break; // Alias für GroßerSchwung
                case "reif": Reif(); break;
                case "mops": if (!IsOnCooldown("mops")) { MopsKommando(); SetCooldown("mops", move.cooldown); } break;
                case "pfanne": AusDerPfanne(); break;
            }
        }

        protected override void Update()
        {
            base.Update();
            if (flaschenTimer > 0) flaschenTimer -= Time.deltaTime;
            if (mopsTimer > 0) mopsTimer -= Time.deltaTime;
            if (critTimer > 0) { critTimer -= Time.deltaTime; if (critTimer <= 0f) critChance = 0f; }
            if (reifTimer > 0) { reifTimer -= Time.deltaTime; if (reifTimer <= 0f) ReifExpire(); }

            // Fett brennt ab -> nach Burn-Dauer wieder aktiv
            if (!greaseActive)
            {
                greaseBurnTimer -= Time.deltaTime;
                if (greaseBurnTimer <= 0f) { greaseActive = true; fireHitsTaken = 0; }
            }
        }

        protected override void HandleSpecialInput()
        {
            var input = FighterInput.Instance;
            if (input == null) return;

            if (input.GetSpecial1(playerIndex) && flaschenTimer <= 0 && !isBlocking) Special1();
            if (input.GetSpecial2(playerIndex) && mopsTimer <= 0 && !isBlocking) Special2();

            // Zusatzspezials über Keyboard (Demo-Binding)
            if (Keyboard.current != null && playerIndex == 0)
            {
                if (UnityEngine.InputSystem.Keyboard.current[UnityEngine.InputSystem.Key.Alpha1].wasPressedThisFrame && !isBlocking)
                    GroesserSchwung();
                if (UnityEngine.InputSystem.Keyboard.current[UnityEngine.InputSystem.Key.Alpha2].wasPressedThisFrame && !isBlocking)
                    Reif();
                if (UnityEngine.InputSystem.Keyboard.current[UnityEngine.InputSystem.Key.Alpha3].wasPressedThisFrame && !isBlocking)
                    AusDerPfanne();
            }
        }

        public override void Special1() => Flaschenhals(false);
        public override void Special2() => MopsKommando();

        // ---- Special 1: Flaschenhals ----
        public void Flaschenhals(bool exVersion)
        {
            flaschenTimer = flaschenHalsCooldown;
            if (bottleProjectile != null)
            {
                var go = Instantiate(bottleProjectile, attackPoint.position, transform.rotation);
                var bottle = go.GetComponent<BottleProjectile>();
                if (bottle != null) bottle.Initialize(transform.forward, this, exVersion);
            }
        }

        // ---- Special 2: Mops-Kommando ----
        public void MopsKommando()
        {
            mopsTimer = mopsCooldown;
            if (mopsPrefab != null)
            {
                var mops = Instantiate(mopsPrefab, transform.position + transform.forward * 2f, Quaternion.identity);
                var ctrl = mops.GetComponent<MopsController>();
                if (ctrl != null) ctrl.Initialize(this, GetEnemy());
            }
            // Herta einsetzen (Trophäe)
            Herta herta = FindObjectOfType<Herta>();
            if (herta != null) herta.RegisterUse();
        }

        // ---- Special 3: Der große Schwung ----
        public void GroesserSchwung()
        {
            // Armor Frames 8-20 vereinfacht: hitzt während des Schwungs durch
            Collider[] hits = Physics.OverlapBox(attackPoint.position, attackBoxSize, attackPoint.rotation, enemyLayer);
            foreach (var c in hits)
            {
                var enemy = c.GetComponent<FighterController>();
                if (enemy != null && enemy != this)
                    enemy.TakeDamage(bigSwingDamage, transform.forward, this);
            }
        }

        // ---- Special 4: REIF! ----
        public void Reif()
        {
            // Buff: nächster Treffer +80% Schaden
            reifTimer = 6f;
        }
        void ReifExpire()
        {
            reifTimer = 0f;
        }

        // ---- Special 5: Aus der Pfanne (Anti-Air) ----
        public void AusDerPfanne()
        {
            Collider[] hits = Physics.OverlapBox(attackPoint.position, attackBoxSize, attackPoint.rotation, enemyLayer);
            foreach (var c in hits)
            {
                var enemy = c.GetComponent<FighterController>();
                if (enemy != null && enemy != this)
                    enemy.TakeDamage(panDamage, transform.forward, this);
            }
        }

        // ---- Schmier-Schlüppa ----
        public override float ModifyIncomingDamage(float incoming)
        {
            // Block-Reduktion übernimmt base.TakeDamage — hier nur das Fett.
            if (greaseActive) incoming *= (1f - greaseReduction);
            return Mathf.Max(0f, incoming);
        }

        public override void TakeDamage(float damage, Vector3 knockbackDir, FighterController attacker)
        {
            // Abrutsch bei Würfen/Grabs (vereinfacht): Gegner rutscht ab
            if (greaseActive && attacker != null && Random.value < grabSlipChance)
                attacker.ApplyGrabStun(0.2f);

            damage = ModifyIncomingDamage(damage);
            base.TakeDamage(damage, knockbackDir, attacker);
        }

        /// <summary>Feuertreffer (z.B. TetraPaks Fusel-Atem) brennen das Fett weg.</summary>
        public void BurnOffGrease()
        {
            fireHitsTaken++;
            if (fireHitsTaken >= 3 && greaseActive)
            {
                greaseActive = false;
                greaseBurnTimer = greaseBurnDuration;
            }
        }

        // ---- Mops-Krit-Buff ----
        public void ApplyMopsCritBonus(float chance, float duration)
        {
            critChance = chance;
            critTimer = duration;
        }

        protected override float ResolveDamage(FighterController target, float baseDamage)
        {
            float dmg = baseDamage;
            if (critChance > 0f && Random.value < critChance) dmg *= 1.5f;
            if (reifTimer > 0f) dmg *= reifBuffMultiplier;
            return dmg;
        }

        FighterController GetEnemy()
        {
            var all = FindObjectsOfType<FighterController>();
            foreach (var f in all) if (f != this) return f;
            return null;
        }

        public override void PurgeBuffs()
        {
            base.PurgeBuffs();
            reifTimer = 0f;
            critChance = 0f;
            critTimer = 0f;
        }

        // ---- Fatal Blow: Betriebsunfall ----
        public override bool ExecuteFatalBlow(FighterController target)
        {
            if (!base.ExecuteFatalBlow(target)) return false;
            if (target != null) target.TakeDamage(34f, transform.forward, this);
            return true;
        }

        // ---- Fatalities (2) + Trophäe 51 "Guten Appetit" ----
        public override void PerformFatality(FighterController target, string fatalityId)
        {
            base.PerformFatality(target, fatalityId);
            leBindeFatalitiesDone = Mathf.Min(leBindeFatalitiesDone + 1, 2);
            if (leBindeFatalitiesDone >= 2 && TrophyManager.Instance != null)
                TrophyManager.Instance.Unlock(TrophyManager.TROPHY_GUTEN_APPETIT);
        }

        public int leBindeFatalitiesDone;
    }
}
