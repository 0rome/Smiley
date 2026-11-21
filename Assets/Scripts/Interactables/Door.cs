using DG.Tweening;
using UnityEngine;
using UnityEngine.AI;
using Zenject;

public class Door : InteractableBase
{
    [Header("Settings")]
    [SerializeField] private Vector3 openedRotation;
    [SerializeField] private float doorSpeed = 2f;
    [SerializeField] private Transform transformToRotate;
    [SerializeField] private Transform handleTransform;
    [SerializeField] private bool canInterrupt;
    [SerializeField] private bool isLocked;

    [Header("Lock")]
    [SerializeField] private bool requiresKey;
    [SerializeField] private string keyName = "defaultKey";
    [SerializeField] private AudioClip openDoorWithKeyClip;
    [SerializeField] private AudioClip lockedDoorClip;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] openClips;
    [SerializeField] private AudioClip[] closeClips;

    [Inject] private PlayerInventory playerInventory;
    [Inject] private ScreenClueManager screenClueManager;

    private NavMeshObstacle navMeshObstacle;
    private Collider doorCollider;
    private bool isOpened;
    private Tween currentTween;

    public Transform HandleTransform => handleTransform;
    public bool IsOpened => isOpened;
    public bool IsLocked => isLocked;

    private void Start()
    {
        TryGetComponent(out navMeshObstacle);
        TryGetComponent(out doorCollider);

        if (transformToRotate == null)
        {
            transformToRotate = transform;
        }

        // Применяем стартовое состояние замка к NavMeshObstacle
        if (navMeshObstacle != null)
        {
            navMeshObstacle.enabled = !isOpened;
            navMeshObstacle.carving = true;
        }
    }

    public override void Interact()
    {
        // Запертая дверь: пытаемся разблокировать ключом
        if (isLocked)
        {
            TryUnlock();
            return;
        }

        // Нельзя прерывать анимацию, если это запрещено
        if (currentTween != null && currentTween.IsActive() && currentTween.IsPlaying() && !canInterrupt)
        {
            return;
        }

        // Прерываем текущую анимацию, если разрешено
        if (currentTween != null && currentTween.IsActive() && currentTween.IsPlaying())
        {
            currentTween.Kill();
        }

        bool willBeOpened = !isOpened;
        MakeSound(willBeOpened);

        if (willBeOpened)
        {
            // Отключаем NavMeshObstacle сразу в начале открытия,
            // чтобы AI сразу строил путь через проём
            if (navMeshObstacle != null)
            {
                navMeshObstacle.enabled = false;
            }

            currentTween = transformToRotate.DOLocalRotate(openedRotation, doorSpeed)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    isOpened = true;
                });
        }
        else
        {
            currentTween = transformToRotate.DOLocalRotate(Vector3.zero, doorSpeed)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    isOpened = false;
                    // Включаем Obstacle только после полного закрытия двери
                    if (navMeshObstacle != null)
                    {
                        navMeshObstacle.enabled = true;
                        navMeshObstacle.carving = true;
                    }
                });
        }

        isOpened = willBeOpened;
    }

    /// <summary>
    /// Публичный сеттер состояния замка (для внешних систем, например триггеров).
    /// </summary>
    public void DoorIsLocked(bool locked)
    {
        isLocked = locked;
    }

    private void TryUnlock()
    {
        if (!requiresKey)
        {
            // Заперта без ключа — просто сообщаем и играем звук
            screenClueManager?.ShowClue("The door is locked");
            if (lockedDoorClip != null && audioSource != null)
            {
                audioSource.PlayOneShot(lockedDoorClip);
            }
            return;
        }

        var heldItem = playerInventory != null ? playerInventory.ItemInHand : null;
        var key = heldItem != null ? heldItem.GetComponent<Key>() : null;

        if (key != null && key.KeyName == keyName)
        {
            isLocked = false;

            if (openDoorWithKeyClip != null && audioSource != null)
            {
                audioSource.PlayOneShot(openDoorWithKeyClip);
            }

            // Сразу открываем после разблокировки
            Interact();
        }
        else
        {
            screenClueManager?.ShowClue("Need outside door key");
            if (lockedDoorClip != null && audioSource != null)
            {
                audioSource.PlayOneShot(lockedDoorClip);
            }
        }
    }

    private void MakeSound(bool opening)
    {
        if (audioSource == null) return;

        AudioClip[] targetClips = opening ? openClips : closeClips;
        if (targetClips == null || targetClips.Length == 0) return;

        audioSource.pitch = Random.Range(0.65f, 1.05f);
        audioSource.clip = targetClips[Random.Range(0, targetClips.Length)];
        audioSource.Play();
    }
}