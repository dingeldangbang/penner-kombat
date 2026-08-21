using UnityEngine;

namespace PennerKombat
{
    /// <summary>
    /// Uschi — Healer/Zoner. Küchen-Charakter.
    /// Special1 = Handtasche (↓↘→+□) · Special2 = Kochlöffel · Gurke (heilt).
    /// </summary>
    public class Uschi : FighterController
    {
        [Header("Uschi")]
        public GameObject handtaschePrefab;
        public GameObject kochloeffelPrefab;
        public float gurkeHeal = 8f;
        public float gurkeCooldown = 12f;
        public AudioClip gurkeSound;

        private int handtascheCharges = 3;
        private float gurkeTimer;

        protected override void Awake()
        {
            base.Awake();
            moves.AddRange(MoveCatalog.Get(GameConstants.CharUschi));
        }

        public override void ExecuteMove(MoveData move)
        {
            if (move == null) return;
            switch (move.id)
            {
                case "handtasche": if (handtascheCharges > 0) Special1(); break;
                case "gurke": if (!IsOnCooldown("gurke")) { Gurke(); SetCooldown("gurke", move.cooldown); } break;
                case "kochloeffel": Special2(); break;
                case "topfdeckel": if (!IsOnCooldown("topfdeckel")) { Topfdeckel(); SetCooldown("topfdeckel", move.cooldown); } break;
                case "suppe": if (!IsOnCooldown("suppe")) { Suppe(); SetCooldown("suppe", move.cooldown); } break;
            }
        }

        // Topfdeckel: Block-Buff (+50% extra Reduktion) für 8s
        void Topfdeckel() => StartCoroutine(TopfdeckelBuff());

        System.Collections.IEnumerator TopfdeckelBuff()
        {
            blockReductionBonus = 0.5f;
            SignatureFx.Uschi_Topfdeckel(this);
            yield return new WaitForSeconds(8f);
            blockReductionBonus = 0f;
        }

        // Suppe: Heal (+15% HP). Im 1v1 heilt sie nur Uschi selbst;
        // im Team-Modus kann sie später auch Verbündete heilen.
        void Suppe()
        {
            HealSelf(15f);
        }

        protected override void Update()
        {
            base.Update();
            if (gurkeTimer > 0f) gurkeTimer -= Time.deltaTime;
        }

        protected override void HandleSpecialInput()
        {
            var input = FighterInput.Instance;
            if (input == null) return;
            if (input.GetSpecial1(playerIndex) && !isBlocking && handtascheCharges > 0) Special1();
            if (input.GetSpecial2(playerIndex) && !isBlocking) Special2();
            if (UnityEngine.InputSystem.Keyboard.current != null && playerIndex == 0
                && UnityEngine.InputSystem.Keyboard.current[UnityEngine.InputSystem.Key.Alpha1].wasPressedThisFrame
                && gurkeTimer <= 0f)
                Gurke();
        }

        public override void Special1()
        {
            handtascheCharges--;
            if (handtaschePrefab != null)
                Instantiate(handtaschePrefab, attackPoint.position, transform.rotation);
            Collider[] hits = Physics.OverlapSphere(attackPoint.position, attackRange, enemyLayer);
            foreach (var h in hits)
            {
                var e = h.GetComponent<FighterController>();
                if (e != null && e != this)
                {
                    e.TakeDamage(8f, transform.forward, this);
                    PlayHitFeedback(e, 8f, HitTier.Special);
                }
            }
        }

        public override void Special2()
        {
            if (kochloeffelPrefab != null)
                Instantiate(kochloeffelPrefab, attackPoint.position, transform.rotation);
        }

        public void Gurke()
        {
            gurkeTimer = gurkeCooldown;
            currentHP = Mathf.Min(currentHP + gurkeHeal, maxHP);
            SignatureFx.Uschi_Heal(this, gurkeHeal, false);
            if (gurkeSound != null) AudioSource.PlayClipAtPoint(gurkeSound, transform.position);
        }
    }
}
