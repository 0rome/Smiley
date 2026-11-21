using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public class GraphicsSettings : MonoBehaviour
{
    [Header("UI Elements")]
    public TMP_Dropdown qualityDropdown;
    public TMP_Dropdown resolutionDropdown;
    public TMP_Dropdown screenModeDropdown;
    public Toggle vSyncToggle;

    [Header("Upscaler UI")]
    public TMP_Dropdown upscalerTypeDropdown;    // Native, DLSS, FSR
    public TMP_Dropdown upscalerQualityDropdown; // Quality, Balanced, Performance

    [Header("Anti-Aliasing UI (only for Native)")]
    public TMP_Dropdown antiAliasingDropdown;    // None, FXAA, TAA, SMAA

    [Header("Player prefab camera")]
    [SerializeField] private HDAdditionalCameraData cameraData;

    private Resolution[] resolutions;

    void Start()
    {
        // Находим камеру и получаем её HDRP-компонент
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            cameraData = mainCam.GetComponent<HDAdditionalCameraData>();
        }
        else
        {
            Debug.LogError("Main Camera not found! Graphics settings won't work properly.");
        }

        InitResolutionDropdown();
        InitQualityDropdown();
        InitScreenModeDropdown();
        InitUpscalerDropdowns();
        InitAntiAliasingDropdown();

        LoadSettings();
    }

    #region Инициализация UI

    private void InitResolutionDropdown()
    {
        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();
        int currentResolutionIndex = 0;

        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = resolutions[i].width + " x " + resolutions[i].height + " @ " + resolutions[i].refreshRateRatio + "Hz";
            options.Add(option);

            if (resolutions[i].width == Screen.width &&
                resolutions[i].height == Screen.height)
            {
                currentResolutionIndex = i;
            }
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();
        resolutionDropdown.onValueChanged.AddListener(SetResolution);
    }

    private void InitQualityDropdown()
    {
        qualityDropdown.ClearOptions();
        qualityDropdown.AddOptions(new List<string>
        {
            "High",
            "Medium",
            "Low"
        });

        qualityDropdown.onValueChanged.AddListener(SetQuality);
    }

    private void InitScreenModeDropdown()
    {
        screenModeDropdown.ClearOptions();
        screenModeDropdown.AddOptions(new List<string>
        {
            "Exclusive Fullscreen",
            "Fullscreen Window",
            "Windowed",
            "Maximized Window"
        });

        screenModeDropdown.onValueChanged.AddListener(SetScreenMode);
        vSyncToggle.onValueChanged.AddListener(SetVSync);
    }

    private void InitUpscalerDropdowns()
    {
        upscalerTypeDropdown.ClearOptions();
        upscalerTypeDropdown.AddOptions(new List<string> { "Native (No Upscaler)", "DLSS", "FSR" });
        upscalerTypeDropdown.onValueChanged.AddListener(SetUpscalerType);

        upscalerQualityDropdown.ClearOptions();
        upscalerQualityDropdown.AddOptions(new List<string> { "Quality", "Balanced", "Performance" });
        upscalerQualityDropdown.onValueChanged.AddListener(SetUpscalerQuality);
    }

    private void InitAntiAliasingDropdown()
    {
        antiAliasingDropdown.ClearOptions();
        antiAliasingDropdown.AddOptions(new List<string> { "None (Off)", "FXAA", "TAA", "SMAA" });
        antiAliasingDropdown.onValueChanged.AddListener(SetAntiAliasing);
    }

    #endregion

    #region Применение настроек

    public void SetQuality(int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex, true);
        ApplyStreamingSettings(qualityIndex);

        PlayerPrefs.SetInt("QualityLevel", qualityIndex);
        PlayerPrefs.Save();
    }

    private void ApplyStreamingSettings(int qualityIndex)
    {
        QualitySettings.streamingMipmapsActive = true;

        switch (qualityIndex)
        {
            case 0: // High
                QualitySettings.streamingMipmapsMemoryBudget = 1024;
                QualitySettings.streamingMipmapsMaxLevelReduction = 1;
                break;

            case 1: // Medium
                QualitySettings.streamingMipmapsMemoryBudget = 512;
                QualitySettings.streamingMipmapsMaxLevelReduction = 2;
                break;

            case 2: // Low
                QualitySettings.streamingMipmapsMemoryBudget = 256;
                QualitySettings.streamingMipmapsMaxLevelReduction = 3;
                break;
        }
    }

    public void SetResolution(int resolutionIndex)
    {
        Resolution res = resolutions[resolutionIndex];
        Screen.SetResolution(res.width, res.height, Screen.fullScreenMode, res.refreshRateRatio);

        PlayerPrefs.SetInt("ResolutionIndex", resolutionIndex);
        PlayerPrefs.Save();
    }

    public void SetScreenMode(int modeIndex)
    {
        FullScreenMode mode = FullScreenMode.ExclusiveFullScreen;
        switch (modeIndex)
        {
            case 0: mode = FullScreenMode.ExclusiveFullScreen; break;
            case 1: mode = FullScreenMode.FullScreenWindow; break;
            case 2: mode = FullScreenMode.Windowed; break;
            case 3: mode = FullScreenMode.MaximizedWindow; break;
        }

        Screen.fullScreenMode = mode;

        if (mode == FullScreenMode.ExclusiveFullScreen || mode == FullScreenMode.FullScreenWindow)
        {
            SetResolution(resolutionDropdown.value);
        }

        PlayerPrefs.SetInt("ScreenMode", modeIndex);
        PlayerPrefs.Save();
    }

    public void SetVSync(bool isVSyncOn)
    {
        QualitySettings.vSyncCount = isVSyncOn ? 1 : 0;
        PlayerPrefs.SetInt("VSync", isVSyncOn ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void SetUpscalerType(int typeIndex)
    {
        if (cameraData == null) return;

        // Сбрасываем кастомные настройки апскейлеров
        cameraData.deepLearningSuperSamplingUseCustomQualitySettings = false;
        cameraData.fidelityFX2SuperResolutionUseCustomQualitySettings = false;

        switch (typeIndex)
        {
            case 0: // Native
                // Ничего не включаем
                break;
            case 1: // DLSS
                cameraData.deepLearningSuperSamplingUseCustomQualitySettings = true;
                break;
            case 2: // FSR
                cameraData.fidelityFX2SuperResolutionUseCustomQualitySettings = true;
                break;
        }

        // Активируем/деактивируем выпадающий список сглаживания
        // Сглаживание имеет смысл настраивать только в Native режиме
        bool isNative = (typeIndex == 0);
        antiAliasingDropdown.interactable = isNative;

        // Если переключились на Native, применяем сохранённое сглаживание
        if (isNative)
        {
            SetAntiAliasing(antiAliasingDropdown.value);
        }

        // Принудительно обновляем качество апскейлера
        SetUpscalerQuality(upscalerQualityDropdown.value);

        PlayerPrefs.SetInt("UpscalerType", typeIndex);
        PlayerPrefs.Save();
    }

    public void SetUpscalerQuality(int qualityIndex)
    {
        if (cameraData == null) return;

        // 0 = Quality, 1 = Balanced, 2 = Performance
        cameraData.deepLearningSuperSamplingQuality = (uint)qualityIndex;
        cameraData.fidelityFX2SuperResolutionQuality = (uint)qualityIndex;

        PlayerPrefs.SetInt("UpscalerQuality", qualityIndex);
        PlayerPrefs.Save();
    }

    public void SetAntiAliasing(int aaIndex)
    {
        if (cameraData == null) return;

        HDAdditionalCameraData.AntialiasingMode mode = HDAdditionalCameraData.AntialiasingMode.None;
        switch (aaIndex)
        {
            case 0: mode = HDAdditionalCameraData.AntialiasingMode.None; break;
            case 1: mode = HDAdditionalCameraData.AntialiasingMode.FastApproximateAntialiasing; break; // FXAA
            case 2: mode = HDAdditionalCameraData.AntialiasingMode.TemporalAntialiasing; break; // TAA
            case 3: mode = HDAdditionalCameraData.AntialiasingMode.SubpixelMorphologicalAntiAliasing; break; // SMAA
        }

        cameraData.antialiasing = mode;

        PlayerPrefs.SetInt("AntiAliasing", aaIndex);
        PlayerPrefs.Save();
    }

    #endregion

    #region Загрузка сохраненных настроек

    private void LoadSettings()
    {
        // 1. Качество
        int savedQuality = PlayerPrefs.GetInt("QualityLevel", 2);
        qualityDropdown.value = savedQuality;
        qualityDropdown.RefreshShownValue();
        SetQuality(savedQuality);

        // 2. Разрешение
        int savedRes = PlayerPrefs.GetInt("ResolutionIndex", resolutions.Length - 1);
        resolutionDropdown.value = savedRes;
        resolutionDropdown.RefreshShownValue();
        SetResolution(savedRes);

        // 3. Режим экрана
        int savedMode = PlayerPrefs.GetInt("ScreenMode", 0);
        screenModeDropdown.value = savedMode;
        screenModeDropdown.RefreshShownValue();
        SetScreenMode(savedMode);

        // 4. VSync
        bool isVSync = PlayerPrefs.GetInt("VSync", 0) == 1;
        vSyncToggle.isOn = isVSync;
        SetVSync(isVSync);

        // 5. Апскейлер
        int defaultUpscalerType = 0; // Native по умолчанию
        if (cameraData != null)
        {
            if (cameraData.deepLearningSuperSamplingUseCustomQualitySettings)
                defaultUpscalerType = 1; // DLSS
            else if (cameraData.fidelityFX2SuperResolutionUseCustomQualitySettings)
                defaultUpscalerType = 2; // FSR
        }

        int savedUpscalerType = PlayerPrefs.GetInt("UpscalerType", defaultUpscalerType);
        upscalerTypeDropdown.value = savedUpscalerType;
        upscalerTypeDropdown.RefreshShownValue();

        // 6. Сглаживание (загружаем до SetUpscalerType, чтобы при Native применилось корректно)
        int savedAA = PlayerPrefs.GetInt("AntiAliasing", 2); // По умолчанию TAA
        antiAliasingDropdown.value = savedAA;
        antiAliasingDropdown.RefreshShownValue();

        // Вызываем SetUpscalerType в конце, так как он управляет доступностью AA и применяет настройки
        SetUpscalerType(savedUpscalerType);
    }

    #endregion
}