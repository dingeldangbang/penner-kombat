using UnityEngine;

namespace PennerKombat
{
    /// <summary>
    /// Rolf — Summoner. Beschwört Ratten und peitscht mit dem Rattenschwanz.
    /// Special1 = Ratten (↓↘→+□) · Special2 = Rattenschwanz (←↙↓+○)
    /// </summary>
    public class Rolf : FighterController
    {
        [Header("Rolf")]
        public GameObject ratPrefab;
        public GameObject rattenschwanzPrefab;
        public int ratCount = 5;
        public float ratCooldown = 10f;
        public float tailDamage = 10f;

        private float ratTimer;

        protected override void Awake()
        {
            base.Awake();
            moves.AddRange(MoveCatalog.Get(GameConstants.CharRolf));
        }

        public override void ExecuteMove(MoveData move)
        {
            if (move == null) return;
            switch (move.id)
            {
                case "ratten": if (!IsOnCooldown("ratten")) { Special1(); SetCooldown("ratten", move.cooldown); } break;
                case "rattenschwanz": Special2(); break;
                case "rattenkoenig": if (!IsOnCooldown("rattenkoenig")) { Rattenkoenig(); SetCooldown("rattenkoenig", move.cooldown); } break;
                case "ratengift": if (!IsOnCooldown("ratengift")) { Ratengift(); SetCooldown("ratengift", move.cooldown); } break;
                case "kanalisation": Kanalisation(); break;
            }
        }

        // Rattenkönig: Beschwörung, die Schaden anrichtet (20%)
        void Rattenkoenig()
        {
            var enemy = GetEnemy();
            if (enemy != null)
            {
                for (int i = 0; i < 3; i++)
                    if (ratPrefab != null)
                    {
                        var rat = Instantiate(ratPrefab, transform.position + Random.insideUnitSphere * 1.5f, Quaternion.identity);
                        var ctrl = rat.GetComponent<RatController>();
                        if (ctrl != null) ctrl.Initialize(enemy);
                    }
            }
        }

        // Ratengift: Gift-Zone (DoT 3%/s für 6s)
        void Ratengift()
        {
            Collider[] hits = Physics.OverlapSphere(attackPoint.position, attackRange + 1f, enemyLayer);
            foreach (var h in hits)
            {
                var e = h.GetComponent<FighterController>();
                if (e != null && e != this) StartCoroutine(Poison(e, 6f));
            }
        }

        System.Collections.IEnumerator Poison(FighterController target, float duration)
        {
            float t = 0f;
            while (t < duration && target != null && target.gameObject.activeSelf)
            {
                t += 0.5f;
                target.TakeDamage(1.5f, Vector3.zero, this); // ~3%/s über 6s
                yield return new WaitForSeconds(0.5f);
            }
        }

        // Kanalisation: Grab
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

        protected override void Update()
        {
            base.Update();
            if (ratTimer > 0f) ratTimer -= Time.deltaTime;
        }

        protected override void HandleSpecialInput()
        {
            var input = FighterInput.Instance;
            if (input == null) return;
            if (input.GetSpecial1(playerIndex) && !isBlocking && ratTimer <= 0f) Special1();
            if (input.GetSpecial2(playerIndex) && !isBlocking) Special2();
        }

        public override void Special1()
        {
            ratTimer = ratCooldown;
            var enemy = GetEnemy();
            for (int i = 0; i < ratCount; i++)
            {
                if (ratPrefab != null)
                {
                    Vector3 offset = new Vector3(Random.Range(-2f, 2f), 0, Random.Range(-2f, 2f));
                    var rat = Instantiate(ratPrefab, transform.position + offset, Quaternion.identity);
                    var ctrl = rat.GetComponent<RatController>();
                    if (ctrl != null) ctrl.Initialize(enemy);
                }
            }
        }

        public override void Special2()
        {
            if (rattenschwanzPrefab != null)
                Instantiate(rattenschwanzPrefab, attackPoint.position, transform.rotation);
            Collider[] hits = Physics.OverlapSphere(attackPoint.position, attackRange, enemyLayer);
            foreach (var h in hits)
            {
                var e = h.GetComponent<FighterController>();
                if (e != null && e != this) e.TakeDamage(tailDamage, transform.forward, this);
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
