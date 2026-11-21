using DG.Tweening;
using UnityEngine;

public class Lever : InteractableBase
{
    [Header("Settings")]
    [SerializeField] private InteractableBase[] objectsToInteract;
    [SerializeField] private float leverDuration = 0.3f;
    [SerializeField] private float loweredAngle = -50f;
    [SerializeField] private float raisedAngle = -0f;
    [SerializeField] private bool isLowered = false;
    [SerializeField] private MeshRenderer signMeshRenderer;

    [Header("Audio")]
    [SerializeField] private AudioSource mainAudioSource;
    [SerializeField] private AudioClip[] audioClips;

    [Header("Materials")]
    [SerializeField] private Material greenEmission; // когда поднят (raised)
    [SerializeField] private Material redEmission;   // когда опущен (lowered)

    private Tween _rotateTween;

    private void Start()
    {
        // синхронизируем цвет с начальным состоянием рычага
        UpdateSignColor();
    }

    public override void Interact()
    {
        isLowered = !isLowered;

        float targetZ = isLowered ? loweredAngle : raisedAngle;
        Vector3 targetRot = new Vector3(targetZ, 0f, 90f);

        _rotateTween?.Kill();
        _rotateTween = transform
            .DOLocalRotate(targetRot, leverDuration)
            .SetEase(Ease.OutQuad);

        UpdateSignColor();

        if (objectsToInteract != null)
        {
            foreach (var item in objectsToInteract)
                if (item != null) item.Interact();
        }

        if (audioClips == null || mainAudioSource == null) return;
        if (audioClips.Length == 0) return;

        mainAudioSource.pitch = Random.Range(0.7f, 1f);
        int randSound = Random.Range(0, audioClips.Length);
        mainAudioSource.PlayOneShot(audioClips[randSound]);
    }

    private void UpdateSignColor()
    {
        if (signMeshRenderer == null) return;

        Material targetMat = isLowered ? redEmission : greenEmission;
        if (targetMat != null)
            signMeshRenderer.material = targetMat;
    }
}