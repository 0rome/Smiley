using DG.Tweening;
using UnityEngine;
using UnityEngine.AI;

public class DoorShelf : InteractableBase
{
    [Header("Settings")]
    [SerializeField] private Vector3 openedRotation;
    [SerializeField] private float doorSpeed = 2f;
    [SerializeField] private Transform transformToRotate;
    [SerializeField] private bool canInterrupt; // Можно ли прерывать движение

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] openClips;
    [SerializeField] private AudioClip[] closeClips;
    private bool isOpened;
    private Tween currentTween; // Ссылка на текущую анимацию DOTween

    private void Start()
    {
        if (transformToRotate == null)
        {
            transformToRotate = transform;
        }
    }

    public override void Interact()
    {
        // Проверяем: если дверь уже движется и прерывание запрещено, ничего не делаем
        if (currentTween != null && currentTween.IsActive() && currentTween.IsPlaying() && !canInterrupt)
        {
            return;
        }

        // Если дверь двигалась и прерывание разрешено — останавливаем старую анимацию
        if (currentTween != null && currentTween.IsActive() && currentTween.IsPlaying())
        {
            currentTween.Kill();
        }

        // Определяем целевое состояние перед изменением (будет ли дверь открываться или закрываться)
        bool willBeOpened = !isOpened;

        // Воспроизведение звука (передаем то, куда дверь СЕЙЧАС начнет движение)
        MakeSound(willBeOpened);

        // Запуск нового твина
        if (willBeOpened)
        {
            currentTween = transformToRotate.DOLocalRotate(openedRotation, doorSpeed)
                .SetEase(Ease.OutQuad)
                .OnComplete(() => isOpened = true);
        }
        else
        {
            currentTween = transformToRotate.DOLocalRotate(Vector3.zero, doorSpeed)
                .SetEase(Ease.OutQuad)
                .OnComplete(() => isOpened = false);
        }

        // Обновляем состояние
        isOpened = willBeOpened;
    }

    private void MakeSound(bool opening)
    {
        if (audioSource == null) return;

        AudioClip[] targetClips = opening ? openClips : closeClips;

        // Если массив для текущего действия пустой, ничего не воспроизводим
        if (targetClips == null || targetClips.Length == 0) return;

        audioSource.pitch = Random.Range(0.65f, 1.05f);
        audioSource.clip = targetClips[Random.Range(0, targetClips.Length)];
        audioSource.Play();
    }
}
