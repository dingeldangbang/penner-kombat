using System.Collections;
using UnityEngine;

namespace PennerKombat
{
    /// <summary>
    /// TetraPak — DoT/Buff. Fusel-Atem verbrennt (LeBindes Schlüppa-Fett!),
    /// Leergut als Zonenkontrolle, Zweiter Wind als Comeback-Buff.
    /// Special1 = Fusel-Atem (↓↘→+□) · Special2 = Leergut (←↙↓+○)
    /// </summary>
    public class TetraPak : FighterController
    {
        [Header("TetraPak")]
        public GameObject fuselAtemPrefab;
        public GameObject leergutPrefab;
        public float zweiterWindThreshold = 20f;
        public float zweiterWindBonus = 20f;
        public float burnDamage = 3f;
        public float burnTicks = 5f;

        private bool zweiterWindActive;
        [HideInInspector] public int drunkenness;

        protected override void Awake()
        {
            base.Awake();
            moves.AddRange(MoveCatalog.Get(GameConstants.CharTetraPak));
        }

        public override void ExecuteMove(MoveData move)
        {
            if (move == null) return;
            switch (move.id)
            {
                case "fusel": Special1(); break;
                case "leergut": if (!IsOnCooldown("leergut")) { Special2(); SetCooldown("leergut", move.cooldown); } break;
                case "pfandflasche": Pfandflasche(); break;
                case "kater": if (!IsOnCooldown("kater")) { Kater(); SetCooldown("kater", move.cooldown); } break;
                case "trinken": if (!IsOnCooldown("trinken")) { Trinken(); SetCooldown("trinken", move.cooldown); } break;
            }
        }

        // Pfandflasche: Projektil
        void Pfandflasche()
        {
            Collider[] hits = Physics.OverlapSphere(attackPoint.position, attackRange + 1.5f, enemyLayer);
            foreach (var h in hits)
            {
                var e = h.GetComponent<FighterController>();
                if (e != null && e != this) e.TakeDamage(15f, transform.forward, this);
            }
        }

        // Kater: Debuff — Gegner langsamer für 4s
        void Kater()
        {
            Collider[] hits = Physics.OverlapSphere(attackPoint.position, attackRange + 1f, enemyLayer);
            foreach (var h in hits)
            {
                var e = h.GetComponent<FighterController>();
                if (e != null && e != this)
                {
                    var slow = e.gameObject.GetComponent<SlowDebuff>();
                    if (slow == null) slow = e.gameObject.AddComponent<SlowDebuff>();
                    slow.Apply(4f, 0.6f);
                }
            }
        }

        // Trinken: +3% HP, erhöht Betrunkenheit (Später: +Schaden, -Genauigkeit)
        void Trinken()
        {
            HealSelf(3f);
            drunkenness = Mathf.Min(drunkenness + 1, 5);
        }

        protected override void Update()
        {
            base.Update();

            if (!zweiterWindActive && currentHP <= zweiterWindThreshold && currentHP > 0f)
                ZweiterWind();
        }

        protected override void HandleSpecialInput()
        {
            var input = FighterInput.Instance;
            if (input == null) return;
            if (input.GetSpecial1(playerIndex) && !isBlocking) Special1();
            if (input.GetSpecial2(playerIndex) && !isBlocking) Special2();
        }

        public override void Special1() // Fusel-Atem: Brand-DoT
        {
            Collider[] hits = Physics.OverlapSphere(attackPoint.position, attackRange + 1f, enemyLayer);
            foreach (var h in hits)
            {
                var e = h.GetComponent<FighterController>();
                if (e == null || e == this) continue;
                e.TakeDamage(6f, transform.forward, this);
                SignatureFx.TetraPak_FuselAtem(this, e);
                PlayHitFeedback(e, 6f, HitTier.Special);
                StartCoroutine(Burn(e));
                // LeBindes Fett brennt weg
                if (e is LeBinde lb) lb.BurnOffGrease();
            }
        }

        IEnumerator Burn(FighterController target)
        {
            for (int i = 0; i < burnTicks; i++)
            {
                yield return new WaitForSeconds(0.6f);
                if (target != null && target.gameObject.activeSelf)
                    target.TakeDamage(burnDamage, Vector3.zero, this);
            }
        }

        public override void Special2() // Leergut: Zonenkontrolle
        {
            if (leergutPrefab != null)
                Instantiate(leergutPrefab, attackPoint.position, Quaternion.identity);
        }

        void ZweiterWind()
        {
            zweiterWindActive = true;
            currentHP = Mathf.Min(currentHP + zweiterWindBonus, maxHP);
            SignatureFx.TetraPak_ZweiterWind(this);
            StartCoroutine(ZweiterWindBuff());
        }

        IEnumerator ZweiterWindBuff()
        {
            lightDamage *= 1.2f;
            heavyDamage *= 1.2f;
            yield return new WaitForSeconds(8f);
            lightDamage /= 1.2f;
            heavyDamage /= 1.2f;
            zweiterWindActive = false;
        }

        public void PurgeBuffs()
        {
            zweiterWindActive = false;
            StopAllCoroutines();
            lightDamage = 9f;
            heavyDamage = 15f;
            drunkenness = 0;
        }
    }

    /// <summary>Debuff: verlangsamt einen Kämpfer für eine Dauer (TetraPaks "Kater").</summary>
    public class SlowDebuff : MonoBehaviour
    {
        public float remaining;
        public float speedScale = 1f;

        public void Apply(float duration, float scale)
        {
            speedScale = scale;
            remaining = duration;
        }

        void Update()
        {
            if (remaining <= 0f) return;
            remaining -= Time.deltaTime;
            if (remaining <= 0f) speedScale = 1f;
        }

        public bool IsActive => remaining > 0f;
    }
}
