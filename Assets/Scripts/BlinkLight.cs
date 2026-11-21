using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class BlinkLightUniTask : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private GameObject lightObject;
    [SerializeField] private MeshRenderer lampRenderer;
    [SerializeField] private Material emissionMat;
    [SerializeField] private Material darkMat;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip flickerSound; // Короткий звук щелчка или треска
    [SerializeField, Range(0f, 1f)] private float soundVolume = 0.8f;

    [Header("Timer")]
    [Tooltip("Как часто проверяем шанс мигания (в секундах)")]
    [SerializeField] private float checkInterval = 120f;

    [Tooltip("Blink chance (0-100)")]
    [SerializeField, Range(0, 100)] private int blinkChance = 10;

    private void Start()
    {
        TimerLoopAsync(this.GetCancellationTokenOnDestroy()).Forget();
    }

    private async UniTaskVoid TimerLoopAsync(CancellationToken token)
    {
        // Цикл работает, пока объект живет на сцене
        while (!token.IsCancellationRequested)
        {
            // Ждем указанный интервал (120 сек)
            await UniTask.Delay(TimeSpan.FromSeconds(checkInterval), cancellationToken: token);

            // ПРАВИЛЬНОЕ МЕСТО ДЛЯ ПРОВЕРКИ:
            // Если света нет или он выключен — пропускаем мигание и идем на следующий круг ожидания
            if (lightObject == null || !lightObject.activeSelf)
            {
                continue; // continue перебрасывает нас обратно в начало цикла (к ожиданию)
            }

            // Проверяем шанс (10%)
            if (UnityEngine.Random.Range(0f, 100f) <= blinkChance)
            {
                await BlinkAsync(token);
            }
        }
    }

    private async UniTask BlinkAsync(CancellationToken token)
    {
        int flickers = UnityEngine.Random.Range(2, 6);

        for (int i = 0; i < flickers; i++)
        {
            // Выключаем свет и издаем звук щелчка
            SetLightState(false);
            PlayFlickerSound();
            await UniTask.Delay(TimeSpan.FromSeconds(UnityEngine.Random.Range(0.05f, 0.2f)), cancellationToken: token);

            // Включаем свет и издаем звук щелчка
            SetLightState(true);
            PlayFlickerSound();
            await UniTask.Delay(TimeSpan.FromSeconds(UnityEngine.Random.Range(0.05f, 0.15f)), cancellationToken: token);
        }

        // В конце убеждаемся, что свет включен
        SetLightState(true);
    }

    private void SetLightState(bool isOn)
    {
        if (lightObject != null) lightObject.SetActive(isOn);
        if (lampRenderer != null) lampRenderer.material = isOn ? emissionMat : darkMat;
    }

    private void PlayFlickerSound()
    {
        if (audioSource != null && flickerSound != null)
        {
            // Важная фишка для реализма: немного меняем pitch (высоту тона)
            // Каждый щелчок будет звучать немного иначе
            audioSource.pitch = UnityEngine.Random.Range(0.85f, 1.15f);

            // PlayOneShot позволяет накладывать звуки друг на друга без прерывания
            audioSource.PlayOneShot(flickerSound, soundVolume);
        }
    }
}