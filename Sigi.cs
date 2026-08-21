using System.Collections;
using UnityEngine;

namespace PennerKombat
{
    /// <summary>
    /// Sigi — Zoner/Debuff. Hackt den Gegner und invertiert dessen Eingaben.
    /// Special1 = Laptop (↓↘→+□) · Special2 = Root-Zugriff (←↙↓↘→+○)
    /// </summary>
    public class Sigi : FighterController
    {
        [Header("Sigi")]
        public GameObject laptopPrefab;
        public float rootAccessDuration = 6f;
        public float rootAccessCooldown = 20f;

        private float rootAccessTimer;

        protected override void Awake()
        {
            base.Awake();
            moves.AddRange(MoveCatalog.Get(GameConstants.CharSigi));
        }

        public override void ExecuteMove(MoveData move)
        {
            if (move == null) return;
            switch (move.id)
            {
                case "laptop": Special1(); break;
                case "root": if (!IsOnCooldown("root")) { Special2(); SetCooldown("root", move.cooldown); } break;
                case "firewall": if (!IsOnCooldown("firewall")) { Firewall(); SetCooldown("firewall", move.cooldown); } break;
                case "sql": if (!IsOnCooldown("sql")) { SQLInjection(); SetCooldown("sql", 4f); } break;
                case "ddos": if (!IsOnCooldown("ddos")) { DDoS(); SetCooldown("ddos", move.cooldown); } break;
            }
        }

        // Firewall: Block-Buff für 5s
        void Firewall() => StartCoroutine(FirewallBuff());

        System.Collections.IEnumerator FirewallBuff()
        {
            blockReductionBonus = 0.9f;
            yield return new WaitForSeconds(5f);
            blockReductionBonus = 0f;
        }

        // SQL-Injection: Gegner-Eingaben invertieren (2s)
        void SQLInjection()
        {
            Collider[] hits = Physics.OverlapSphere(attackPoint.position, attackRange + 1f, enemyLayer);
            foreach (var h in hits)
            {
                var e = h.GetComponent<FighterController>();
                if (e != null && e != this)
                {
                    e.TakeDamage(12f, transform.forward, this);
                    e.InvertInputs(2f);
                }
            }
        }

        // DDoS: Gegner-Eingabeverzögerung (+4 Frames) für 3s
        void DDoS()
        {
            Collider[] hits = Physics.OverlapSphere(attackPoint.position, attackRange + 2f, enemyLayer);
            foreach (var h in hits)
            {
                var e = h.GetComponent<FighterController>();
                if (e != null && e != this)
                {
                    var fass = e.gameObject.GetComponent<Fassungslosigkeit>();
                    if (fass == null) fass = e.gameObject.AddComponent<Fassungslosigkeit>();
                    fass.Apply(4f, 3f);
                }
            }
        }

        protected override void Update()
        {
            base.Update();
            if (rootAccessTimer > 0f) rootAccessTimer -= Time.deltaTime;
        }

        protected override void HandleSpecialInput()
        {
            var input = FighterInput.Instance;
            if (input == null) return;
            if (input.GetSpecial1(playerIndex) && !isBlocking) Special1();
            if (input.GetSpecial2(playerIndex) && !isBlocking && rootAccessTimer <= 0f) Special2();
        }

        public override void Special1()
        {
            if (laptopPrefab != null)
                Instantiate(laptopPrefab, attackPoint.position, Quaternion.identity);
        }

        public override void Special2()
        {
            rootAccessTimer = rootAccessCooldown;
            Collider[] hits = Physics.OverlapSphere(attackPoint.position, attackRange * 2f, enemyLayer);
            foreach (var h in hits)
            {
                var e = h.GetComponent<FighterController>();
                if (e != null && e != this)
                    StartCoroutine(Hack(e));
            }
        }

        IEnumerator Hack(FighterController enemy)
        {
            var hack = enemy.gameObject.GetComponent<InputHack>();
            if (hack == null) hack = enemy.gameObject.AddComponent<InputHack>();
            hack.Enable(rootAccessDuration);
            yield return new WaitForSeconds(rootAccessDuration);
            hack.Disable();
        }

        public void PurgeBuffs()
        {
            rootAccessTimer = 0f;
        }
    }

    /// <summary>Debuff: invertiert die Bewegungsrichtung des Gegners.</summary>
    public class InputHack : MonoBehaviour
    {
        public bool Active { get; private set; }
        private float remaining;

        public void Enable(float duration) { Active = true; remaining = duration; }
        public void Disable() { Active = false; }

        void Update()
        {
            if (!Active) return;
            remaining -= Time.deltaTime;
            if (remaining <= 0f) Active = false;
        }

        // Wird vom LuckyBag broadcastet (Eingabe-Invertierung über Mojo Bob)
        public void InvertInputs(float duration)
        {
            if (this == null) return;
            Enable(duration);
        }
    }
}
