using UnityEngine;

namespace PennerKombat
{
    public enum CrowdMood { Ruhig, Interessiert, Wild, Ekstase, Buhrufe, Entsetzt }

    /// <summary>
    /// Reagierende Zuschauer am Arenarand (docs/EXTRAS.md §9).
    /// Die Menge wippt im Beat, jubelt bei Combos, buht bei Blockorgien und
    /// rastet bei Fatalities aus. Figuren sind Kapseln — es geht um die
    /// Bewegung und den Sound, nicht um Details.
    /// </summary>
    public class CrowdReactions : MonoBehaviour
    {
        public static CrowdReactions Instance { get; private set; }

        [Header("Menge")]
        public int crowdSize = 24;
        public float ringRadius = 13f;
        public bool bobToBeat = true;

        [Header("Reaktion")]
        public CrowdMood mood = CrowdMood.Ruhig;
        public float moodDecay = 4f;

        private readonly Transform[] members = new Transform[64];
        private readonly float[] phase = new float[64];
        private int count;
        private float moodTimer;
        private float energy;

        public static CrowdReactions Ensure()
        {
            if (Instance == null)
            {
                var go = new GameObject("~Crowd");
                Instance = go.AddComponent<CrowdReactions>();
            }
            return Instance;
        }

        void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(this); return; }
        }

        void Start()
        {
            Build();
            if (ComboSystem.Instance != null)
                ComboSystem.Instance.OnComboChanged += HandleCombo;
            if (MusicSync.Instance != null)
                MusicSync.Instance.OnBar += HandleBar;
        }

        void OnDestroy()
        {
            if (ComboSystem.Instance != null)
                ComboSystem.Instance.OnComboChanged -= HandleCombo;
            if (MusicSync.Instance != null)
                MusicSync.Instance.OnBar -= HandleBar;
        }

        void Build()
        {
            count = Mathf.Min(crowdSize, members.Length);
            for (int i = 0; i < count; i++)
            {
                float a = (360f / count) * i * Mathf.Deg2Rad;
                Vector3 pos = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * (ringRadius + Random.Range(-0.8f, 0.8f));

                var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                Destroy(go.GetComponent<Collider>());
                go.name = "PK_Zuschauer";
                go.transform.SetParent(transform, false);
                go.transform.position = new Vector3(pos.x, 0.9f, pos.z);
                go.transform.localScale = new Vector3(0.45f, Random.Range(0.75f, 0.95f), 0.45f);
                go.transform.LookAt(new Vector3(0f, go.transform.position.y, 0f));

                var mr = go.GetComponent<MeshRenderer>();
                mr.material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                mr.material.color = Color.Lerp(PennerPalette.NightBlue, PennerPalette.Earth, Random.value) * 0.9f;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                members[i] = go.transform;
                phase[i] = Random.Range(0f, Mathf.PI * 2f);
            }
        }

        void Update()
        {
            if (moodTimer > 0f)
            {
                moodTimer -= Time.deltaTime;
                if (moodTimer <= 0f) mood = CrowdMood.Ruhig;
            }
            energy = Mathf.Lerp(energy, EnergyFor(mood), 3f * Time.deltaTime);

            float bpm = MusicSync.Instance != null ? MusicSync.Instance.CurrentBpm : 140f;
            float speed = bobToBeat ? bpm / 60f * Mathf.PI : 3f;

            for (int i = 0; i < count; i++)
            {
                var m = members[i];
                if (m == null) continue;
                float hop = Mathf.Abs(Mathf.Sin(Time.time * speed + phase[i])) * (0.05f + energy * 0.5f);
                Vector3 p = m.position;
                p.y = 0.9f + hop;
                m.position = p;
            }
        }

        void HandleCombo(FighterController fighter, int combo)
        {
            if (combo >= 15) React(CrowdMood.Ekstase);
            else if (combo >= 8) React(CrowdMood.Wild);
            else if (combo >= 4) React(CrowdMood.Interessiert);
        }

        void HandleBar(int bar)
        {
            // Auf jeden vierten Takt ein kleiner gemeinsamer Sprung
            if (energy > 0.4f && bar % 8 == 0)
                for (int i = 0; i < count; i++)
                    if (members[i] != null) phase[i] = 0f;
        }

        /// <summary>Stimmung setzen (auch von Fatality/Blackout aufrufbar).</summary>
        public void React(CrowdMood newMood, float duration = 4f)
        {
            // Nur „lautere" Stimmung überschreibt eine laufende
            if ((int)newMood >= (int)mood || moodTimer <= 0f)
            {
                mood = newMood;
                moodTimer = duration > 0f ? duration : moodDecay;
            }

            switch (newMood)
            {
                case CrowdMood.Ekstase:
                    AudioManager.Instance?.PlayRandomUI();
                    CameraShake.Shake(3f, 0.12f);
                    break;
                case CrowdMood.Entsetzt:
                    ScreenEffects.FlashColor(PennerPalette.BloodRed, 0.15f, 0.2f);
                    break;
            }
        }

        static float EnergyFor(CrowdMood m)
        {
            switch (m)
            {
                case CrowdMood.Interessiert: return 0.35f;
                case CrowdMood.Wild:         return 0.7f;
                case CrowdMood.Ekstase:      return 1f;
                case CrowdMood.Buhrufe:      return 0.25f;
                case CrowdMood.Entsetzt:     return 0.15f;
                default:                     return 0.1f;
            }
        }
    }
}
