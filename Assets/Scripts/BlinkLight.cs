
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class BlinkLightUniTask : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Light targetLight;
    [SerializeField] private MeshRenderer lampRenderer;
    [SerializeField] private int emissionMaterialIndex = 0;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip flickerSound;
    [SerializeField, Range(0f, 1f)]
    private float soundVolume = 0.8f;

    [Header("Timer")]
    [SerializeField] private float checkInterval = 120f;
    [SerializeField, Range(0, 100)]
    private int blinkChance = 10;

    [Header("Flicker Settings")]
    [Tooltip("Минимальная яркость относительно исходной")]
    [SerializeField, Range(0f, 1f)]
    private float minIntensityMultiplier = 0.02f;

    [Tooltip("Максимальная яркость относительно исходной")]
    [SerializeField, Range(1f, 2f)]
    private float maxIntensityMultiplier = 1.2f;

    [Tooltip("Вероятность почти полного отключения")]
    [SerializeField, Range(0f, 1f)]
    private float blackoutChance = 0.15f;

    [Tooltip("Скорость изменения яркости")]
    [SerializeField] private float flickerSpeed = 0.04f;

    [Tooltip("Плавность изменения яркости")]
    [SerializeField, Range(0.01f, 1f)]
    private float intensityVariation = 0.5f;

    [Header("Restore")]
    [SerializeField] private float restoreDuration = 0.3f;

    // Original values
    private float originalIntensity;
    private Color originalEmissionColor;

    // Runtime material
    private Material runtimeMaterial;

    private CancellationToken destroyToken;

    private void Awake()
    {
        destroyToken = this.GetCancellationTokenOnDestroy();

        if (targetLight != null)
        {
            originalIntensity = targetLight.intensity;
        }

        if (lampRenderer != null)
        {
            Material[] materials = lampRenderer.materials;

            if (emissionMaterialIndex >= 0 &&
                emissionMaterialIndex < materials.Length)
            {
                runtimeMaterial = materials[emissionMaterialIndex];

                if (runtimeMaterial.HasProperty("_EmissiveColor"))
                {
                    originalEmissionColor =
                        runtimeMaterial.GetColor("_EmissiveColor");
                }
            }
        }
    }

    private void Start()
    {
        TimerLoopAsync(destroyToken).Forget();
    }

    private async UniTaskVoid TimerLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            await UniTask.Delay(
                TimeSpan.FromSeconds(checkInterval),
                cancellationToken: token
            );

            if (targetLight == null || !targetLight.enabled)
                continue;

            if (UnityEngine.Random.Range(0, 100) < blinkChance)
            {
                await BlinkAsync(token);
            }
        }
    }

    private async UniTask BlinkAsync(CancellationToken token)
    {
        float duration = flickerSound != null
            ? flickerSound.length
            : 2f;

        float elapsed = 0f;

        // Play flicker sound once.
        if (audioSource != null && flickerSound != null)
        {
            audioSource.PlayOneShot(flickerSound, soundVolume);
        }

        while (elapsed < duration)
        {
            token.ThrowIfCancellationRequested();

            // Random intensity based on original intensity.
            float randomIntensity = originalIntensity *
                UnityEngine.Random.Range(
                    minIntensityMultiplier,
                    maxIntensityMultiplier
                );

            // Occasional almost complete blackout.
            if (UnityEngine.Random.value < blackoutChance)
            {
                randomIntensity = originalIntensity * 0.01f;
            }

            // Change light intensity.
            targetLight.intensity = Mathf.Lerp(
                targetLight.intensity,
                randomIntensity,
                intensityVariation
            );

            // Update emission.
            float emissionMultiplier = originalIntensity > 0f
                ? targetLight.intensity / originalIntensity
                : 0f;

            UpdateEmission(emissionMultiplier);

            elapsed += flickerSpeed;

            await UniTask.Delay(
                TimeSpan.FromSeconds(flickerSpeed),
                cancellationToken: token
            );
        }

        await RestoreLightAsync(token);
    }

    private async UniTask RestoreLightAsync(CancellationToken token)
    {
        float startIntensity = targetLight.intensity;
        float elapsed = 0f;

        while (elapsed < restoreDuration)
        {
            token.ThrowIfCancellationRequested();

            float t = restoreDuration > 0f
                ? elapsed / restoreDuration
                : 1f;

            // Smooth recovery.
            t = Mathf.SmoothStep(0f, 1f, t);

            targetLight.intensity = Mathf.Lerp(
                startIntensity,
                originalIntensity,
                t
            );

            float emissionMultiplier = originalIntensity > 0f
                ? targetLight.intensity / originalIntensity
                : 0f;

            UpdateEmission(emissionMultiplier);

            elapsed += Time.deltaTime;

            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }

        targetLight.intensity = originalIntensity;
        UpdateEmission(1f);
    }

    private void UpdateEmission(float multiplier)
    {
        if (runtimeMaterial == null)
            return;

        runtimeMaterial.SetColor(
            "_EmissiveColor",
            originalEmissionColor * multiplier
        );
    }

    private void OnDestroy()
    {
        if (runtimeMaterial != null)
        {
            Destroy(runtimeMaterial);
        }
    }
}
