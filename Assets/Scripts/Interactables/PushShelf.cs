using UnityEngine;
using DG.Tweening;

public class PushShelf : InteractableBase
{
    [Header("Settings")]
    [SerializeField] private Vector3 pushOffset = new Vector3(20f, 0f, 0f);
    [SerializeField] private float moveDuration = 0.5f;
    [SerializeField] private Ease moveEase = Ease.InOutQuad;

    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] openClips;
    [SerializeField] private AudioClip[] closeClips;

    private Vector3 startPosition;
    private bool isExtended = false;
    private bool isAnimating = false;

    private void Start()
    {
        startPosition = transform.localPosition;
    }

    public override void Interact()
    {
        if (isAnimating) return;

        isAnimating = true;

        // Воспроизводим звук открытия или закрытия
        PlaySound(isExtended ? closeClips : openClips);

        Vector3 targetPosition = isExtended ? startPosition : startPosition + pushOffset;

        transform.DOLocalMove(targetPosition, moveDuration)
            .SetEase(moveEase)
            .OnComplete(() =>
            {
                isAnimating = false;
                isExtended = !isExtended;
            });
    }

    private void PlaySound(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0 || audioSource == null) return;

        AudioClip clipToPlay = clips[Random.Range(0, clips.Length)];
        if (clipToPlay != null)
        {
            audioSource.PlayOneShot(clipToPlay);
        }
    }

    private void OnDestroy()
    {
        DOTween.Kill(transform);
    }
}