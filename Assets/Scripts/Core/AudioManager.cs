using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace PennerKombat
{
    /// <summary>
    /// Zentrales Audio-Management: Musik, SFX, Voice und Ambient über
    /// dedizierte AudioSources und (optional) einen Mixer.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance;

        [Header("Fallback")]
        [Tooltip("Leere SFX-Listen mit synthetisierten Klängen füllen, solange keine Aufnahmen da sind.")]
        public bool useProceduralFallback = true;

        [Header("Mixer")]
        public AudioMixerGroup musicGroup;
        public AudioMixerGroup sfxGroup;
        public AudioMixerGroup voiceGroup;

        [Header("Music")]
        public List<AudioClip> battleTracks;      // 28 Basis-Tracks (Index 0..27)
        public List<AudioClip> characterThemes;   // Index-Mapping siehe GetCharacterThemeIndex (0..8) + 29..31
        public AudioClip[] victoryTracks;
        public AudioClip[] menuTracks;
        public AudioClip[] storyTracks;

        [Header("SFX")]
        public AudioClip[] hitSounds;
        public AudioClip[] blockSounds;
        public AudioClip[] hurtSounds;
        public AudioClip[] specialSounds;
        public AudioClip[] uiSounds;

        private AudioSource musicSource;
        private AudioSource sfxSource;
        private AudioSource voiceSource;
        private AudioSource ambientSource;

        void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(this); return; }
            DontDestroyOnLoad(gameObject);

            musicSource = CreateSource("Music", musicGroup, 0.7f, true);
            sfxSource = CreateSource("SFX", sfxGroup, 0.9f, false);
            voiceSource = CreateSource("Voice", voiceGroup, 0.9f, false);
            ambientSource = CreateSource("Ambient", sfxGroup, 0.35f, true);

            EnsureFallbackSfx();
        }

        /// <summary>
        /// Ohne echte Aufnahmen bliebe das Spiel stumm. Deshalb werden leere
        /// SFX-Listen mit synthetisierten Klängen gefüllt (Core/ProceduralAudio.cs).
        /// Sobald eigene Clips zugewiesen sind, passiert hier nichts.
        /// </summary>
        void EnsureFallbackSfx()
        {
            if (!useProceduralFallback) return;

            if (hitSounds == null || hitSounds.Length == 0)
                hitSounds = new[] { ProceduralAudio.Hit(1f), ProceduralAudio.Hit(1.15f), ProceduralAudio.HeavyHit() };
            if (blockSounds == null || blockSounds.Length == 0)
                blockSounds = new[] { ProceduralAudio.Block() };
            if (hurtSounds == null || hurtSounds.Length == 0)
                hurtSounds = new[] { ProceduralAudio.Hurt() };
            if (uiSounds == null || uiSounds.Length == 0)
                uiSounds = new[] { ProceduralAudio.Click() };
            if (specialSounds == null || specialSounds.Length == 0)
                specialSounds = new[] { ProceduralAudio.Kick808() };
            if (victoryTracks == null || victoryTracks.Length == 0)
                victoryTracks = new[] { ProceduralAudio.Victory() };
        }

        AudioSource CreateSource(string name, AudioMixerGroup grp, float vol, bool loop)
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = loop;
            src.volume = vol;
            src.outputAudioMixerGroup = grp;
            return src;
        }

        // ===== Musik (public – StoryManager nutzt diese) =====
        public void PlayMusic(AudioClip clip)
        {
            if (clip == null || musicSource == null) return;
            musicSource.clip = clip;
            musicSource.Play();
        }

        public void PlayBattleMusic(int index = -1)
        {
            AudioClip clip = battleTracks != null && battleTracks.Count > 0
                ? battleTracks[Mathf.Clamp(index >= 0 ? index : Random.Range(0, battleTracks.Count), 0, battleTracks.Count - 1)]
                : null;
            PlayMusic(clip);
        }

        public void PlayCharacterTheme(string characterId)
        {
            int idx = GetCharacterThemeIndex(characterId);
            if (idx >= 0 && characterThemes != null && idx < characterThemes.Count)
                PlayMusic(characterThemes[idx]);
            else
                PlayBattleMusic();
        }

        /// <summary>Track 29-31 (die drei neuen Themes) über die characterThemes-Liste erreichbar.</summary>
        public int GetCharacterThemeIndex(string id)
        {
            switch (id)
            {
                case GameConstants.CharLeBinde: return 29; // Le Bindes Mops-Walzer
                case GameConstants.CharMell: return 30;    // Mells Puls
                case GameConstants.CharMojoBob: return 31; // Mojo Dingeldang Dub
                case GameConstants.CharDieter: return 3;
                case GameConstants.CharUschi: return 4;
                case GameConstants.CharTetraPak: return 5;
                case GameConstants.CharSigi: return 6;
                case GameConstants.CharRolf: return 7;
                case GameConstants.CharKalle: return 8;
                default: return -1;
            }
        }

        public void PlayVictoryMusic()
        {
            if (victoryTracks != null && victoryTracks.Length > 0)
                PlayMusic(victoryTracks[Random.Range(0, victoryTracks.Length)]);
        }

        public void PlayMenuMusic()
        {
            if (menuTracks != null && menuTracks.Length > 0)
                PlayMusic(menuTracks[Random.Range(0, menuTracks.Length)]);
        }

        public void PlayStoryMusic()
        {
            if (storyTracks != null && storyTracks.Length > 0)
                PlayMusic(storyTracks[Random.Range(0, storyTracks.Length)]);
        }

        // ===== SFX / Voice / Ambient =====
        public void PlaySFX(AudioClip clip, float volume = 1f)
        {
            if (clip != null && sfxSource != null) sfxSource.PlayOneShot(clip, volume);
        }

        public void PlayRandomHit() => PlayRandomFrom(hitSounds);
        public void PlayRandomBlock() => PlayRandomFrom(blockSounds);
        public void PlayRandomHurt() => PlayRandomFrom(hurtSounds);
        public void PlayRandomSpecial() => PlayRandomFrom(specialSounds);
        public void PlayRandomUI() => PlayRandomFrom(uiSounds);

        void PlayRandomFrom(AudioClip[] arr)
        {
            if (arr != null && arr.Length > 0)
                PlaySFX(arr[Random.Range(0, arr.Length)]);
        }

        public void PlayVoice(AudioClip clip)
        {
            if (clip != null && voiceSource != null) voiceSource.PlayOneShot(clip);
        }

        public void PlayAmbient(AudioClip clip)
        {
            if (clip != null && ambientSource != null) { ambientSource.clip = clip; ambientSource.Play(); }
        }

        public void StopAmbient() => ambientSource?.Stop();

        // ===== Lautstärke =====
        public void SetMusicVolume(float v) => musicSource.volume = Mathf.Clamp01(v);
        public void SetSFXVolume(float v) => sfxSource.volume = Mathf.Clamp01(v);
        public void SetVoiceVolume(float v) => voiceSource.volume = Mathf.Clamp01(v);
        public void SetMasterVolume(float v) => AudioListener.volume = Mathf.Clamp01(v);

        public void FadeOutMusic(float duration = 1f) => StartCoroutine(FadeMusic(duration, 0f));
        public void FadeInMusic(float duration = 1f) => StartCoroutine(FadeMusic(duration, 0.7f));

        IEnumerator FadeMusic(float duration, float target)
        {
            float start = musicSource.volume, elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                musicSource.volume = Mathf.Lerp(start, target, elapsed / duration);
                yield return null;
            }
            musicSource.volume = target;
        }
    }
}
