using System.Collections;
using UnityEngine;

namespace PennerKombat
{
    /// <summary>
    /// Dingeling Mojo Bob — "Der Glücksbringer". Krit-Charakter / RNG.
    /// Sonderfunktion: "Das Mojo" — echte kritische Treffer; Mojo-Punkte
    /// sammelt er aus verlorenen Situationen und kann sie verWETTEN.
    ///
    /// Spezialbewegungen:
    ///  Special1 = Riesenschwanz (↓↘→+□)   · Special2 = Beutelchen (←↙↓+○)
    ///  3         = Löffelsturm (→↘↓↙←+□)   · 4 = Fünfzig Cent (↓↓+□)
    ///  5         = Dingeneldang! (7 Mojo)
    /// </summary>
    public class MojoBob : FighterController
    {
        [Header("Mojo-System")]
        public int maxMojo = 7;
        public float baseCritChance = 0.15f;
        public float critMultiplier = 2.4f;
        public float critPerMojo = 0.10f;
        public float gambleDuration = 5f;

        [Header("Specials")]
        public GameObject tailPrefab;      // Riesenschwanz (visuell)
        public GameObject bagPrefab;       // Beutelchen
        public GameObject spoonPrefab;     // Löffelsturm
        public GameObject coinPrefab;      // Fünfzig Cent
        public int spoonCount = 17;
        public float tailRangeFactor = 2.7f;

        [Header("Audio")]
        public AudioClip dingeneldangSound;
        public AudioClip critSound;
        public AudioClip loseSound;

        public int MojoPoints => mojoPoints;
        public float CritChance => currentCritChance;

        private int mojoPoints;
        private float currentCritChance = 0.15f;
        private bool isGambling;
        private float gambleTimer;
        private bool isSuperMode;
        private float superModeTimer;
        private bool isLockedOut;
        private float lockoutTimer;
        private bool gambleHitCrit;

        protected override void Awake()
        {
            base.Awake();
            moves.AddRange(MoveCatalog.Get(GameConstants.CharMojoBob));
        }

        public override void ExecuteMove(MoveData move)
        {
            if (move == null) return;
            switch (move.id)
            {
                case "schwanz": Special1(); break;
                case "schwanz_ex": ExTailSwing(); break;
                case "beutelchen": if (!IsOnCooldown("beutelchen")) { Special2(); SetCooldown("beutelchen", 3f); } break;
                case "loeffelsturm": if (!IsOnCooldown("loeffelsturm")) { Loeffelsturm(); SetCooldown("loeffelsturm", move.cooldown); } break;
                case "fuenfzig": FuenfzigCent(); break;
                case "dingeneldang": if (mojoPoints >= move.mojoCost) Dingeneldang(); break;
            }
        }

        IEnumerator ExTailSwing()
        {
            for (int i = 0; i < 3; i++)
            {
                yield return new WaitForSeconds(0.12f);
                float range = attackRange * tailRangeFactor;
                Collider[] hits = Physics.OverlapSphere(attackPoint.position, range, enemyLayer);
                foreach (var hit in hits)
                {
                    var enemy = hit.GetComponent<FighterController>();
                    if (enemy == null || enemy == this) continue;
                    bool isCrit = Random.value < currentCritChance;
                    float dmg = isCrit ? 12f * critMultiplier : 12f;
                    enemy.TakeDamage(dmg, transform.forward, this);
                    if (isCrit) OnCritLanded(enemy);
                }
            }
        }

        protected override void Update()
        {
            base.Update();

            if (isGambling)
            {
                gambleTimer -= Time.deltaTime;
                if (gambleTimer <= 0f) EndGamble(gambleHitCrit);
            }
            if (isSuperMode)
            {
                superModeTimer -= Time.deltaTime;
                if (superModeTimer <= 0f) EndSuperMode();
            }
            if (isLockedOut)
            {
                lockoutTimer -= Time.deltaTime;
                if (lockoutTimer <= 0f) isLockedOut = false;
            }
        }

        protected override void HandleSpecialInput()
        {
            var input = FighterInput.Instance;
            if (input == null) return;

            if (input.GetSpecial1(playerIndex) && !isBlocking) Special1();
            if (input.GetSpecial2(playerIndex) && !isBlocking) Special2();

            if (UnityEngine.InputSystem.Keyboard.current != null && playerIndex == 0)
            {
                if (UnityEngine.InputSystem.Keyboard.current[UnityEngine.InputSystem.Key.Alpha1].wasPressedThisFrame) Loeffelsturm();
                if (UnityEngine.InputSystem.Keyboard.current[UnityEngine.InputSystem.Key.Alpha2].wasPressedThisFrame) FuenfzigCent();
                if (UnityEngine.InputSystem.Keyboard.current[UnityEngine.InputSystem.Key.Alpha3].wasPressedThisFrame) Dingeneldang();
                if (UnityEngine.InputSystem.Keyboard.current[UnityEngine.InputSystem.Key.Alpha4].wasPressedThisFrame) TryGamble(3);
            }
        }

        // ---- Öffentliche Mojo-API (für Projektile) ----
        public void AddMojo(int amount)
        {
            if (isLockedOut || isSuperMode) return;
            mojoPoints = Mathf.Clamp(mojoPoints + amount, 0, maxMojo);
        }

        public void RecordCrit(bool hit)
        {
            if (isGambling && hit) gambleHitCrit = true;
        }

        // ---- Special 1: Riesenschwanz ----
        public override void Special1()
        {
            StartCoroutine(TailSwing());
        }

        IEnumerator TailSwing()
        {
            yield return new WaitForSeconds(0.1f);
            float range = attackRange * tailRangeFactor;
            Collider[] hits = Physics.OverlapSphere(attackPoint.position, range, enemyLayer);
            foreach (var hit in hits)
            {
                var enemy = hit.GetComponent<FighterController>();
                if (enemy == null || enemy == this) continue;
                bool isCrit = Random.value < currentCritChance;
                float dmg = isCrit ? 12f * critMultiplier : 12f;
                enemy.TakeDamage(dmg, transform.forward, this);
                if (isCrit) OnCritLanded(enemy);
            }
        }

        // ---- Special 2: Beutelchen ----
        public override void Special2()
        {
            if (bagPrefab != null)
            {
                var go = Instantiate(bagPrefab, attackPoint.position, transform.rotation);
                var bag = go.GetComponent<LuckyBag>();
                if (bag != null) bag.Initialize(transform.forward, this);
            }
        }

        // ---- Special 3: Löffelsturm ----
        public void Loeffelsturm()
        {
            StartCoroutine(SpoonStorm());
        }

        IEnumerator SpoonStorm()
        {
            int crits = 0;
            for (int i = 0; i < spoonCount; i++)
            {
                bool isCrit = Random.value < currentCritChance;
                if (isCrit) crits++;
                if (spoonPrefab != null)
                {
                    var go = Instantiate(spoonPrefab, attackPoint.position, Quaternion.identity);
                    var spoon = go.GetComponent<SpoonProjectile>();
                    if (spoon != null)
                    {
                        Vector3 dir = Quaternion.Euler(0, Random.Range(-15f, 15f), 0) * transform.forward;
                        spoon.Initialize(dir, this, isCrit);
                    }
                }
                yield return new WaitForSeconds(0.03f);
            }
            AddMojo(crits);
        }

        // ---- Special 4: Fünfzig Cent ----
        public void FuenfzigCent()
        {
            if (coinPrefab != null)
            {
                var go = Instantiate(coinPrefab, attackPoint.position, transform.rotation);
                var coin = go.GetComponent<CoinProjectile>();
                if (coin != null) coin.Initialize(transform.forward, this);
            }
        }

        // ---- Special 5: Dingeneldang! ----
        public void Dingeneldang()
        {
            if (mojoPoints < 7 || isLockedOut || isSuperMode) return;
            mojoPoints = 0;
            isSuperMode = true;
            superModeTimer = 8f;
            currentCritChance = 1f;
            if (dingeneldangSound != null) AudioSource.PlayClipAtPoint(dingeneldangSound, transform.position);
        }

        void EndSuperMode()
        {
            isSuperMode = false;
            currentCritChance = baseCritChance;
            isLockedOut = true;
            lockoutTimer = 15f;
        }

        // ---- Wette ----
        public void TryGamble(int points)
        {
            if (mojoPoints < points || isGambling || isLockedOut) return;
            mojoPoints -= points;
            currentCritChance = Mathf.Clamp(baseCritChance + points * critPerMojo, 0f, 0.85f);
            isGambling = true;
            gambleHitCrit = false;
            gambleTimer = gambleDuration;
        }

        void EndGamble(bool hitCrit)
        {
            isGambling = false;
            if (!hitCrit)
            {
                // Das Glück nimmt es persönlich: Mojo verloren + 10% HP
                currentHP -= maxHP * 0.1f;
                if (currentHP < 0f) currentHP = 0f;
                if (loseSound != null) AudioSource.PlayClipAtPoint(loseSound, transform.position);
                if (currentHP <= 0f) Die();
            }
            currentCritChance = baseCritChance;
        }

        // ---- Krit-Auflösung ----
        protected override float ResolveDamage(FighterController target, float baseDamage)
        {
            return baseDamage; // Krit wird in Spezial-/Trefferpfad aufgelöst
        }

        void OnCritLanded(FighterController target)
        {
            if (critSound != null) AudioSource.PlayClipAtPoint(critSound, transform.position);
            RecordCrit(true);

            // Trophäe: 7 Krit-Treffer in einer Combo
            if (TrophyManager.Instance != null && comboCount >= 7)
                TrophyManager.Instance.UpdateProgress(TrophyManager.TROPHY_DINGENELDANG, 1f);
        }

        protected override void OnReceivedHit(FighterController attacker, float damage)
        {
            base.OnReceivedHit(attacker, damage);
            AddMojo(1); // +1 Mojo beim Treffer kassieren
        }

        protected override void OnDealtHit(FighterController target, float damage)
        {
            base.OnDealtHit(target, damage);
            // Kombo-Krit-Tracking über comboCount (gesetzt in Basis)
        }

        public override void ResetForRound()
        {
            base.ResetForRound();
            mojoPoints = 0;
            currentCritChance = baseCritChance;
            isGambling = isSuperMode = isLockedOut = false;
        }

        // ---- Fatal Blow: Hausgewinn ----
        public override bool ExecuteFatalBlow(FighterController target)
        {
            if (!base.ExecuteFatalBlow(target)) return false;
            if (target != null)
            {
                float dmg = Random.value < 0.5f ? GameConstants.FatalBlowDamageMin : GameConstants.FatalBlowDamageMax;
                target.TakeDamage(dmg, transform.forward, this);
            }
            return true;
        }

        // ---- Fatality "Alles oder Nichts" + Trophäe 54 "Der ehrliche Betrug" ----
        public override void PerformFatality(FighterController target, string fatalityId)
        {
            // Gegner wählt 1 von 3 Kronkorken; Option 3 = Überleben (1/3)
            int choice = Random.Range(0, 3);
            if (choice == 2)
            {
                // Gegner überlebt — Runde zählt trotzdem als Fatality (Trophäe)
                if (TrophyManager.Instance != null)
                    TrophyManager.Instance.Unlock(TrophyManager.TROPHY_EHRLICHER_BETRUG);
                if (target != null) target.HealSelf(target.maxHP); // überlebt
            }
            else
            {
                base.PerformFatality(target, fatalityId);
            }
        }
    }
}
