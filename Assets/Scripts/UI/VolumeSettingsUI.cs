
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class VolumeSettingsUI : MonoBehaviour
{
    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer audioMixer;

    [Header("UI")]
    [SerializeField] private Slider volumeSlider;

    private const string VolumeParameter = "MainAudioVolume";
    private const string SaveKey = "MainAudioVolume";

    private void Start()
    {
        float savedVolume = PlayerPrefs.GetFloat(SaveKey, 1f);

        volumeSlider.SetValueWithoutNotify(savedVolume);
        SetMasterVolume(savedVolume);

        volumeSlider.onValueChanged.AddListener(SetMasterVolume);
    }

    public void SetMasterVolume(float sliderValue)
    {
        float volume = Mathf.Log10(
            Mathf.Max(sliderValue, 0.0001f)
        ) * 20f;

        audioMixer.SetFloat(VolumeParameter, volume);

        PlayerPrefs.SetFloat(SaveKey, sliderValue);
    }

    private void OnDestroy()
    {
        volumeSlider.onValueChanged.RemoveListener(SetMasterVolume);
    }
}
