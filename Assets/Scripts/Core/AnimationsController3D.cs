using UnityEngine;

namespace PennerKombat
{
    /// <summary>
    /// (8) Animator-Brücke für 3D-Charaktere — docs/3D.md §6.2.
    /// Setzt die Mecanim-Parameter aus dem Zustand des Kämpfers, ohne dass der
    /// Kampfcode Animator-Details kennen muss. Fehlt ein Parameter im
    /// Controller, wird er stillschweigend übersprungen (kein Log-Spam).
    ///
    /// Erwartete Parameter (siehe docs/3D.md):
    /// `MoveSpeed` (float), `Direction` (float, -1…1), `IsGrounded` (bool),
    /// `Block` (bool), `Walk` (bool), `ComboCount` (int),
    /// Trigger: `LightAttack`, `HeavyAttack`, `Jump`, `Roll`, `HitReact`, `Death`, `Special`.
    /// </summary>
    [RequireComponent(typeof(FighterController))]
    public class AnimationsController3D : MonoBehaviour
    {
        [Header("Root Motion")]
        public bool useRootMotion = false;

        private FighterController fighter;
        private Animator anim;
        private Rigidbody rb;

        static readonly int PMoveSpeed = Animator.StringToHash("MoveSpeed");
        static readonly int PDirection = Animator.StringToHash("Direction");
        static readonly int PGrounded  = Animator.StringToHash("IsGrounded");
        static readonly int PCombo     = Animator.StringToHash("ComboCount");
        static readonly int PSpecial   = Animator.StringToHash("SpecialIndex");

        void Awake()
        {
            fighter = GetComponent<FighterController>();
            anim = GetComponent<Animator>();
            rb = GetComponent<Rigidbody>();
            if (anim != null) anim.applyRootMotion = useRootMotion;
        }

        void Update()
        {
            if (anim == null || rb == null) return;

            Vector3 flat = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
            float speed = flat.magnitude;

            SetFloat(PMoveSpeed, speed);
            SetBool(PGrounded, fighter.isGrounded);
            SetInt(PCombo, fighter.comboCount);

            // Richtung relativ zur Blickrichtung: -1 = links, 0 = vorwärts, 1 = rechts
            if (speed > 0.1f)
            {
                float signed = Vector3.SignedAngle(transform.forward, flat.normalized, Vector3.up);
                SetFloat(PDirection, Mathf.Clamp(signed / 90f, -1f, 1f));
            }
            else SetFloat(PDirection, 0f);
        }

        // --- Trigger-API für Kampf und Specials ---
        public void PlaySpecial(int index)
        {
            if (anim == null) return;
            SetInt(PSpecial, index);
            Trigger("Special");
        }

        public void Trigger(string name)
        {
            if (anim == null || !HasParameter(name, AnimatorControllerParameterType.Trigger)) return;
            anim.SetTrigger(name);
        }

        // --- sichere Setter ---
        void SetFloat(int hash, float value)
        {
            if (HasParameter(hash, AnimatorControllerParameterType.Float)) anim.SetFloat(hash, value);
        }

        void SetBool(int hash, bool value)
        {
            if (HasParameter(hash, AnimatorControllerParameterType.Bool)) anim.SetBool(hash, value);
        }

        void SetInt(int hash, int value)
        {
            if (HasParameter(hash, AnimatorControllerParameterType.Int)) anim.SetInteger(hash, value);
        }

        bool HasParameter(int hash, AnimatorControllerParameterType type)
        {
            if (anim == null || anim.runtimeAnimatorController == null) return false;
            foreach (var p in anim.parameters)
                if (p.nameHash == hash && p.type == type) return true;
            return false;
        }

        bool HasParameter(string name, AnimatorControllerParameterType type)
            => HasParameter(Animator.StringToHash(name), type);
    }
}
