using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PennerKombat
{
    /// <summary>
    /// Optionsmenü: Audio, Bild und die Gewalt-/Effektregler der VFX-Schicht.
    /// Alle Werte landen im <see cref="SaveSystem"/> und werden beim Start
    /// über <c>SaveSystem.ApplyOptions()</c> wieder angewendet.
    ///
    /// Alle UI-Referenzen sind optional — nicht zugewiesene Felder werden
    /// übersprungen, das Menü funktioniert also auch als Teilausbau.
    /// </summary>
    public class OptionsMenu : MonoBehaviour
    {
        [Header("Audio")]
        public Slider masterSlider;
        public Slider musicSlider;
        public Slider sfxSlider;

        [Header("Bild")]
        public Toggle fullscreenToggle;
        public TMP_Dropdown resolutionDropdown;
        public TMP_Dropdown qualityDropdown;
        public Toggle vsyncToggle;

        [Header("Effekte (docs/VISUALS.md)")]
        [Tooltip("Partikelmenge global: 0 = aus, 1 = Standard, 2 = doppelt.")]
        public Slider vfxIntensitySlider;
        [Tooltip("Blut und Gore-Effekte abschalten (Streams, Jugendschutz).")]
        public Toggle goreToggle;

        [Header("Buttons")]
        public Button applyButton;
        public Button resetButton;
        public Button backButton;
        public string backScene = "MainMenu";

        private Resolution[] resolutions;

        void Start()
        {
            SaveSystem.Ensure();
            BuildResolutionDropdown();
            BuildQualityDropdown();
            LoadIntoUI();

            applyButton?.onClick.AddListener(Apply);
            resetButton?.onClick.AddListener(ResetToDefaults);
            backButton?.onClick.AddListener(Back);

            // Live-Vorschau für die Regler
            masterSlider?.onValueChanged.AddListener(v => AudioManager.Instance?.SetMasterVolume(v));
            musicSlider?.onValueChanged.AddListener(v => AudioManager.Instance?.SetMusicVolume(v));
            sfxSlider?.onValueChanged.AddListener(v => AudioManager.Instance?.SetSFXVolume(v));
            vfxIntensitySlider?.onValueChanged.AddListener(v =>
            {
                if (VFXManager.Instance != null) VFXManager.Instance.intensity = v;
            });
            goreToggle?.onValueChanged.AddListener(v =>
            {
                if (VFXManager.Instance != null) VFXManager.Instance.gore = v;
            });
        }

        void BuildResolutionDropdown()
        {
            if (resolutionDropdown == null) return;

            resolutions = Screen.resolutions;
            var options = new List<string>();
            int current = 0;
            for (int i = 0; i < resolutions.Length; i++)
            {
                options.Add($"{resolutions[i].width} × {resolutions[i].height}");
                if (resolutions[i].width == Screen.width && resolutions[i].height == Screen.height)
                    current = i;
            }
            resolutionDropdown.ClearOptions();
            resolutionDropdown.AddOptions(options);
            resolutionDropdown.value = PlayerPrefs.GetInt("pk_resolution", current);
            resolutionDropdown.RefreshShownValue();
        }

        void BuildQualityDropdown()
        {
            if (qualityDropdown == null) return;
            qualityDropdown.ClearOptions();
            qualityDropdown.AddOptions(new List<string>(QualitySettings.names));
            qualityDropdown.value = QualitySettings.GetQualityLevel();
            qualityDropdown.RefreshShownValue();
        }

        void LoadIntoUI()
        {
            var d = SaveSystem.Instance.Data;
            if (masterSlider != null) masterSlider.value = d.masterVolume;
            if (musicSlider != null) musicSlider.value = d.musicVolume;
            if (sfxSlider != null) sfxSlider.value = d.sfxVolume;
            if (vfxIntensitySlider != null) { vfxIntensitySlider.minValue = 0f; vfxIntensitySlider.maxValue = 2f; vfxIntensitySlider.value = d.vfxIntensity; }
            if (goreToggle != null) goreToggle.isOn = d.gore;
            if (fullscreenToggle != null) fullscreenToggle.isOn = d.fullscreen;
            if (vsyncToggle != null) vsyncToggle.isOn = d.vsync;
            if (qualityDropdown != null) qualityDropdown.value = Mathf.Clamp(d.qualityLevel, 0, QualitySettings.names.Length - 1);
        }

        public void Apply()
        {
            var d = SaveSystem.Instance.Data;
            if (masterSlider != null) d.masterVolume = masterSlider.value;
            if (musicSlider != null) d.musicVolume = musicSlider.value;
            if (sfxSlider != null) d.sfxVolume = sfxSlider.value;
            if (vfxIntensitySlider != null) d.vfxIntensity = vfxIntensitySlider.value;
            if (goreToggle != null) d.gore = goreToggle.isOn;
            if (fullscreenToggle != null) d.fullscreen = fullscreenToggle.isOn;
            if (vsyncToggle != null) d.vsync = vsyncToggle.isOn;
            if (qualityDropdown != null) d.qualityLevel = qualityDropdown.value;

#if !UNITY_ANDROID && !UNITY_IOS
            if (resolutionDropdown != null && resolutions != null &&
                resolutionDropdown.value >= 0 && resolutionDropdown.value < resolutions.Length)
            {
                var r = resolutions[resolutionDropdown.value];
                Screen.SetResolution(r.width, r.height, d.fullscreen);
                PlayerPrefs.SetInt("pk_resolution", resolutionDropdown.value);
            }
#endif
            SaveSystem.Instance.ApplyOptions();
            SaveSystem.Instance.Save();
            AudioManager.Instance?.PlayRandomUI();
        }

        public void ResetToDefaults()
        {
            var d = SaveSystem.Instance.Data;
            d.masterVolume = 0.8f;
            d.musicVolume = 0.7f;
            d.sfxVolume = 0.8f;
            d.vfxIntensity = 1f;
            d.gore = true;
            d.fullscreen = true;
            d.vsync = true;
            d.qualityLevel = Mathf.Clamp(2, 0, QualitySettings.names.Length - 1);
            LoadIntoUI();
            SaveSystem.Instance.ApplyOptions();
            SaveSystem.Instance.Save();
        }

        public void Back()
        {
            Apply();
            if (!string.IsNullOrEmpty(backScene))
                UnityEngine.SceneManagement.SceneManager.LoadScene(backScene);
        }
    }
}
