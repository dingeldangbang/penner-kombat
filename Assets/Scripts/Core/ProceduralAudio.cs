using UnityEngine;

namespace PennerKombat
{
    /// <summary>
    /// Erzeugt Sound-Effekte komplett aus Code — damit es knallt, bevor echte
    /// Aufnahmen da sind. Alles synthetisiert: Rauschen, Sinus, Hüllkurven.
    /// Der <see cref="AudioManager"/> füllt damit leere SFX-Listen auf; sobald
    /// echte Clips zugewiesen sind, werden diese hier nicht mehr benutzt.
    ///
    /// Bewusst grob und dreckig gehalten — passt zum Hinterhof.
    /// </summary>
    public static class ProceduralAudio
    {
        const int SampleRate = 44100;

        /// <summary>Dumpfer Treffer: tiefer Sinus-Abfall plus Rauschtransient.</summary>
        public static AudioClip Hit(float pitch = 1f)
        {
            return Build("pk_hit", 0.18f, (t, dur) =>
            {
                float env = Mathf.Exp(-22f * t);
                float body = Mathf.Sin(2f * Mathf.PI * (150f * pitch) * t * Mathf.Exp(-3f * t));
                float crack = (Random.value * 2f - 1f) * Mathf.Exp(-60f * t);
                return (body * 0.8f + crack * 0.5f) * env;
            });
        }

        /// <summary>Schwerer Treffer: tiefer, länger, mehr Wumms.</summary>
        public static AudioClip HeavyHit()
        {
            return Build("pk_hit_heavy", 0.3f, (t, dur) =>
            {
                float env = Mathf.Exp(-12f * t);
                float body = Mathf.Sin(2f * Mathf.PI * 90f * t * Mathf.Exp(-2.2f * t));
                float crack = (Random.value * 2f - 1f) * Mathf.Exp(-40f * t);
                return (body * 0.9f + crack * 0.45f) * env;
            });
        }

        /// <summary>Block: metallisches Klacken.</summary>
        public static AudioClip Block()
        {
            return Build("pk_block", 0.14f, (t, dur) =>
            {
                float env = Mathf.Exp(-30f * t);
                float metal = Mathf.Sin(2f * Mathf.PI * 900f * t) * 0.5f
                            + Mathf.Sin(2f * Mathf.PI * 1370f * t) * 0.3f;
                float noise = (Random.value * 2f - 1f) * Mathf.Exp(-90f * t) * 0.4f;
                return (metal + noise) * env;
            });
        }

        /// <summary>Getroffen werden: kurzer, gepresster Laut.</summary>
        public static AudioClip Hurt()
        {
            return Build("pk_hurt", 0.22f, (t, dur) =>
            {
                float env = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / dur)) * Mathf.Exp(-6f * t);
                float tone = Mathf.Sin(2f * Mathf.PI * (220f - 90f * t) * t);
                float rasp = (Random.value * 2f - 1f) * 0.25f;
                return (tone * 0.7f + rasp) * env;
            });
        }

        /// <summary>UI-Klick.</summary>
        public static AudioClip Click()
        {
            return Build("pk_ui", 0.06f, (t, dur) =>
            {
                float env = Mathf.Exp(-70f * t);
                return Mathf.Sin(2f * Mathf.PI * 640f * t) * env * 0.6f;
            });
        }

        /// <summary>808-artiger Kick — Grundlage für den Beat-Sync-Platzhalter.</summary>
        public static AudioClip Kick808()
        {
            return Build("pk_808", 0.45f, (t, dur) =>
            {
                float freq = 55f + 90f * Mathf.Exp(-28f * t);
                float env = Mathf.Exp(-5.5f * t);
                float click = (Random.value * 2f - 1f) * Mathf.Exp(-180f * t) * 0.3f;
                return (Mathf.Sin(2f * Mathf.PI * freq * t) + click) * env * 0.9f;
            });
        }

        /// <summary>Sieg: drei aufsteigende Töne, absichtlich schäbig.</summary>
        public static AudioClip Victory()
        {
            float[] notes = { 196f, 261.6f, 392f };
            return Build("pk_victory", 0.75f, (t, dur) =>
            {
                int step = Mathf.Clamp(Mathf.FloorToInt(t / (dur / 3f)), 0, 2);
                float local = t - step * (dur / 3f);
                float env = Mathf.Exp(-6f * local);
                float square = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * notes[step] * t));
                return square * env * 0.35f;
            });
        }

        // ------------------------------------------------------------------

        delegate float Sample(float time, float duration);

        static AudioClip Build(string name, float duration, Sample fn)
        {
            int count = Mathf.Max(1, Mathf.RoundToInt(duration * SampleRate));
            var data = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                data[i] = Mathf.Clamp(fn(t, duration), -1f, 1f);
            }

            var clip = AudioClip.Create(name, count, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
