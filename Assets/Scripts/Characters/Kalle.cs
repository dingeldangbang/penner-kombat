using System.Collections;
using UnityEngine;

namespace PennerKombat
{
    /// <summary>
    /// Kalle — Grappler/Klempner. Greifattacken, verbrennt mit Feuerzeuggas
    /// und kann Mojo Bobs Münze fangen UND zurückwerfen.
    /// Special1 = Arbeitshandschuh (↓↘→+□) · Special2 = Feuerzeuggas (←↙↓+○)
    /// </summary>
    public class Kalle : FighterController
    {
        [Header("Kalle")]
        public GameObject arbeitshandschuhPrefab;
        public float grabRange = 3f;
        public float grabDamage = 16f;
        public float gasDamage = 12f;

        protected override void Awake()
        {
            base.Awake();
            moves.AddRange(MoveCatalog.Get(GameConstants.CharKalle));
        }

        public override void ExecuteMove(MoveData move)
        {
            if (move == null) return;
            switch (move.id)
            {
                case "handschuh": Special1(); break;
                case "feuerzeuggas": if (!IsOnCooldown("feuerzeuggas")) { Special2(); SetCooldown("feuerzeuggas", 4f); } break;
                case "rohrzange": Rohrzange(); break;
                case "ventil": if (!IsOnCooldown("ventil")) { Ventil(); SetCooldown("ventil", move.cooldown); } break;
                case "klempner": if (!IsOnCooldown("klempner")) { Klempner(); SetCooldown("klempner", move.cooldown); } break;
            }
        }

        // Rohrzange: Anti-Air
        void Rohrzange()
        {
            Collider[] hits = Physics.OverlapSphere(attackPoint.position, attackRange, enemyLayer);
            foreach (var h in hits)
            {
                var e = h.GetComponent<FighterController>();
                if (e != null && e != this) e.TakeDamage(16f, Vector3.up, this);
            }
        }

        // Ventil: Buff +10% Schaden für 8s
        void Ventil() => StartCoroutine(VentilBuff());

        System.Collections.IEnumerator VentilBuff()
        {
            lightDamage *= 1.1f; heavyDamage *= 1.1f;
            yield return new WaitForSeconds(8f);
            lightDamage /= 1.1f; heavyDamage /= 1.1f;
        }

        // Klempner: Heilen +10% HP, 10s Cooldown
        void Klempner() => HealSelf(10f);

        public void CatchCoin() // Mojo Bobs "Fünfzig Cent": fangen und zurückwerfen
        {
            // Kalle verliert KEINE Frames und gibt keine Mojo — er wirft zurück.
            var enemy = GetEnemy();
            if (enemy is MojoBob bob) bob.AddMojo(0); // Bob bekommt kein Mojo
        }

        protected override void HandleSpecialInput()
        {
            var input = FighterInput.Instance;
            if (input == null) return;
            if (input.GetSpecial1(playerIndex) && !isBlocking) Special1();
            if (input.GetSpecial2(playerIndex) && !isBlocking) Special2();
        }

        public override void Special1() // Arbeitshandschuh: Greifattacke
        {
            Collider[] hits = Physics.OverlapSphere(attackPoint.position, grabRange, enemyLayer);
            foreach (var h in hits)
            {
                var e = h.GetComponent<FighterController>();
                if (e != null && e != this)
                {
                    e.TakeDamage(grabDamage, transform.forward, this);
                    SignatureFx.Kalle_Zug(this, e);
                    break;
                }
            }
        }

        public override void Special2() // Feuerzeuggas: Brand-DoT
        {
            Collider[] hits = Physics.OverlapSphere(attackPoint.position, attackRange, enemyLayer);
            foreach (var h in hits)
            {
                var e = h.GetComponent<FighterController>();
                if (e != null && e != this)
                {
                    e.TakeDamage(gasDamage, transform.forward, this);
                    VFXManager.Instance?.PlayFire(e.transform.position + Vector3.up, transform.forward, 18);
                    PlayHitFeedback(e, gasDamage, HitTier.Special);
                    if (e is LeBinde lb) lb.BurnOffGrease();
                }
            }
        }

        FighterController GetEnemy()
        {
            var all = FindObjectsOfType<FighterController>();
            foreach (var f in all) if (f != this) return f;
            return null;
        }
    }
}
