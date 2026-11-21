using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Threading;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using Zenject;

public class Ladder : InteractableBase
{
    [Header("Points (Set in Inspector as child objects)")]
    [SerializeField] private Transform topPoint;
    [SerializeField] private Transform bottomPoint;
    [SerializeField] private Transform exitPoint;

    [Header("Settings")]
    [SerializeField] private float climbDuration = 3f;
    [SerializeField] private CustomPassVolume customPassVolume;

    [Header("Audio")]
    [SerializeField] private AudioSource mainAudioSource;
    [SerializeField] private AudioClip climbUpSound;
    [SerializeField] private AudioClip climbDownSound;

    [Header("Camera Shake")]
    [Tooltip("Камера для тряски. Если не указана, будет использована Camera.main")]
    [SerializeField] private Camera targetCamera;
    [Tooltip("Сила тряски позиции")]
    [SerializeField] private float shakeStrength = 0.15f;
    [Tooltip("Количество колебаний в секунду")]
    [SerializeField] private int shakeVibrato = 15;
    [Tooltip("Случайность направления тряски (0-180)")]
    [SerializeField] private float shakeRandomness = 90f;

    [Inject] private PlayerController playerController;

    private Transform playerTransform;
    private PlayerStateSwitcher playerStateSwitcher;
    private BoxCollider boxCollider;

    private Tween _currentTween;
    private Tween _cameraShakeTween;
    private bool _isClimbing;
    private CancellationTokenSource _cts;

    private void Start()
    {
        playerTransform = playerController.transform;
        playerStateSwitcher = playerController.GetComponent<PlayerStateSwitcher>();
        boxCollider = GetComponent<BoxCollider>();

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        // Защита от забытых точек в инспекторе
        if (exitPoint == null) Debug.LogError("Ladder: exitPoint is not assigned!", this);
    }

    public override void Interact()
    {
        UseLadder().Forget();
    }

    private async UniTask UseLadder()
    {
        if (_isClimbing) return;
        _isClimbing = true;

        _cts?.Cancel();
        _cts = new CancellationTokenSource();

        // Определяем направление заранее, чтобы использовать эту логику и в catch
        bool isGoingUp = Vector3.Distance(playerTransform.position, bottomPoint.position) <
                         Vector3.Distance(playerTransform.position, topPoint.position);

        float currentShakeStrength = isGoingUp ? shakeStrength : shakeStrength * 0.7f;

        try
        {
            playerStateSwitcher.DeactivatePlayerControl();
            boxCollider.enabled = false;
            customPassVolume.customPasses[5].enabled = false;

            if (isGoingUp)
            {
                mainAudioSource.PlayOneShot(climbUpSound);
                playerTransform.position = bottomPoint.position;
                playerTransform.rotation = bottomPoint.rotation;

                StartCameraShake(climbDuration, currentShakeStrength);

                _currentTween = playerTransform.DOMoveY(topPoint.position.y, climbDuration).SetEase(Ease.InOutSine);
                await _currentTween.AsyncWaitForCompletion();

                // ГАРАНТИРОВАННЫЙ ТЕЛЕПОРТ НАВЕРХ
                playerTransform.position = exitPoint.position;
                playerTransform.rotation = exitPoint.rotation;
            }
            else
            {
                mainAudioSource.PlayOneShot(climbDownSound);
                playerTransform.position = topPoint.position;
                playerTransform.rotation = topPoint.rotation;

                StartCameraShake(climbDuration, currentShakeStrength);

                _currentTween = playerTransform.DOMoveY(bottomPoint.position.y, climbDuration).SetEase(Ease.InOutSine);
                await _currentTween.AsyncWaitForCompletion();

                // Если после спуска тоже нужна точка выхода, раскомментируйте и настройте:
                // playerTransform.position = bottomExitPoint.position;
                // playerTransform.rotation = bottomExitPoint.rotation;
            }
        }
        catch (System.Exception e)
        {
            // ЕСЛИ АНИМАЦИЯ ПРЕРВАЛАСЬ (например, из-за деактивации объекта), 
            // мы всё равно телепортируем игрока в безопасное место, чтобы он не упал!
            Debug.LogWarning($"Ladder climb interrupted: {e.Message}. Forcing safe position.");

            if (isGoingUp && exitPoint != null)
            {
                playerTransform.position = exitPoint.position;
                playerTransform.rotation = exitPoint.rotation;
            }
        }
        finally
        {
            _isClimbing = false;
            _currentTween?.Kill();
            _cameraShakeTween?.Kill();

            if (targetCamera != null)
            {
                // DOKill() с fadeOut=true (по умолчанию) сам плавно вернёт камеру в исходное положение.
                // Жесткий сброс в Vector3.zero убран, так как он мог ломать позицию камеры, 
                // если она является дочерним объектом игрока.
                targetCamera.transform.DOKill();
            }

            // Восстанавливаем состояние только в самом конце, после всех телепортаций
            customPassVolume.customPasses[5].enabled = true;
            boxCollider.enabled = true;
            playerStateSwitcher.ActivatePlayerControl();

            _cts?.Dispose();
            _cts = null;
        }
    }

    private void StartCameraShake(float duration, float strength)
    {
        if (targetCamera == null) return;

        Transform camTransform = targetCamera.transform;

        _cameraShakeTween = camTransform.DOShakePosition(
            duration: duration,
            strength: strength,
            vibrato: shakeVibrato,
            randomness: shakeRandomness,
            fadeOut: true
        );

        camTransform.DOShakeRotation(
            duration: duration,
            strength: new Vector3(1.5f, 0.5f, 1f),
            vibrato: shakeVibrato,
            randomness: shakeRandomness,
            fadeOut: true
        );
    }

    private void OnDestroy()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _currentTween?.Kill();
        _cameraShakeTween?.Kill();

        if (targetCamera != null)
        {
            targetCamera.transform.DOKill();
        }
    }
}