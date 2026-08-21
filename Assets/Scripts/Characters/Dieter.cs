using System.Collections;
using UnityEngine;

namespace PennerKombat
{
    /// <summary>
    /// Dieter — Brawler/Kanalarbeiter. Solider Schlagetot mit Buff.
    /// Special1 = Schraubenschlüssel (↓↘→+□) · Special2 = Abflussreiniger-Buff (→↘↓↙←+○)
    /// </summary>
    public class Dieter : FighterController
    {
        [Header("Dieter")]
        public GameObject schraubenschluesselPrefab;
        public float abflussCooldown = 8f;
        public float buffMultiplier = 1.3f;
        public float buffDuration = 10f;

        private float abflussTimer;

        protected override void Awake()
        {
            base.Awake();
            moves.AddRange(MoveCatalog.Get(GameConstants.CharDieter));
        }

        public override void ExecuteMove(MoveData move)
        {
            if (move == null) return;
            switch (move.id)
            {
                case "schraubenschluessel": Special1(); break;
                case "abfluss": if (!IsOnCooldown("abfluss")) { Special2(); SetCooldown("abfluss", move.cooldown); } break;
                case "rohrbruch": if (!IsOnCooldown("rohrbruch")) { Rohrbruch(); SetCooldown("rohrbruch", move.cooldown); } break;
                case "kanalisation": if (!IsOnCooldown("kanalisation")) { Kanalisation(); SetCooldown("kanalisation", move.cooldown); } break;
                case "wasserrohr": Wasserrohr(); break;
            }
        }

        // Rohrbruch: Zonenkontrolle — Gegner in der Nähe rutscht (Sludge/Stun)
        void Rohrbruch()
        {
            Collider[] hits = Physics.OverlapSphere(attackPoint.position, attackRange + 1f, enemyLayer);
            foreach (var h in hits)
            {
                var e = h.GetComponent<FighterController>();
                if (e != null && e != this) e.ApplyGrabStun(0.3f);
            }
        }

        // Kanalisation: Grab — Gegner kurz verschwinden lassen (vereinfacht: Stun + Schaden)
        void Kanalisation()
        {
            Collider[] hits = Physics.OverlapSphere(attackPoint.position, attackRange, enemyLayer);
            foreach (var h in hits)
            {
                var e = h.GetComponent<FighterController>();
                if (e != null && e != this)
                {
                    e.TakeDamage(12f, transform.forward, this);
                    e.ApplyGrabStun(0.5f);
                    break;
                }
            }
        }

        // Wasserrohr: Anti-Air
        void Wasserrohr()
        {
            Collider[] hits = Physics.OverlapSphere(attackPoint.position, attackRange, enemyLayer);
            foreach (var h in hits)
            {
                var e = h.GetComponent<FighterController>();
                if (e != null && e != this) e.TakeDamage(18f, Vector3.up, this);
            }
        }

        protected override void Update()
        {
            base.Update();
            if (abflussTimer > 0f) abflussTimer -= Time.deltaTime;
        }

        protected override void HandleSpecialInput()
        {
            var input = FighterInput.Instance;
            if (input == null) return;
            if (input.GetSpecial1(playerIndex) && !isBlocking) Special1();
            if (input.GetSpecial2(playerIndex) && !isBlocking && abflussTimer <= 0f) Special2();
        }

        public override void Special1()
        {
            if (schraubenschluesselPrefab != null)
                Instantiate(schraubenschluesselPrefab, attackPoint.position, transform.rotation);
            Collider[] hits = Physics.OverlapSphere(attackPoint.position, attackRange, enemyLayer);
            foreach (var h in hits)
            {
                var e = h.GetComponent<FighterController>();
                if (e != null && e != this) e.TakeDamage(14f, transform.forward, this);
            }
        }

        public override void Special2()
        {
            abflussTimer = abflussCooldown;
            StartCoroutine(AbflussBuff());
        }

        IEnumerator AbflussBuff()
        {
            float oldL = lightDamage, oldH = heavyDamage;
            lightDamage *= buffMultiplier;
            heavyDamage *= buffMultiplier;
            yield return new WaitForSeconds(buffDuration);
            lightDamage = oldL;
            heavyDamage = oldH;
        }

        public void PurgeBuffs()
        {
            lightDamage = 10f;
            heavyDamage = 16f;
            StopAllCoroutines();
        }
    }
}
