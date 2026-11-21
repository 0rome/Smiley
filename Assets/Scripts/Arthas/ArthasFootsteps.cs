using UnityEngine;

public class ArthasFootsteps : MonoBehaviour
{
    [Header("Базовые настройки")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private float minPitch = 0.9f;
    [SerializeField] private float maxPitch = 1.1f;

    [Header("Настройки луча")]
    [Tooltip("Точка, откуда пускаем луч")]
    [SerializeField] private Transform feetTransform;
    [Tooltip("ВСЕ слои, в которые должен упираться луч")]
    [SerializeField] private LayerMask raycastMask;
    [SerializeField] private float rayDistance = 0.5f;

    [Header("Маски конкретных поверхностей")]
    [Tooltip("Слой(и) травы")]
    [SerializeField] private LayerMask grassMask;
    [Tooltip("Слой(и) камня")]
    [SerializeField] private LayerMask stoneMask;
    [Tooltip("Слой(и) дерева")]
    [SerializeField] private LayerMask woodMask;
    [Tooltip("Слой(и) грязи")]
    [SerializeField] private LayerMask mudMask;
    [Tooltip("Слой(и) воды")]
    [SerializeField] private LayerMask waterMask;

    [Header("Звуки поверхностей")]
    [SerializeField] private AudioClip[] defaultSteps;
    [SerializeField] private AudioClip[] mudSteps;
    [SerializeField] private AudioClip[] waterSteps;
    [SerializeField] private AudioClip[] grassSteps;
    [SerializeField] private AudioClip[] stoneSteps;
    [SerializeField] private AudioClip[] woodSteps;

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (feetTransform == null)
            feetTransform = transform;
    }

    public void PlayFootstep()
    {
        AudioClip[] clipsToPlay = defaultSteps;

        Vector3 rayOrigin = feetTransform.position + Vector3.up * 0.1f;
        Debug.DrawRay(rayOrigin, Vector3.down * rayDistance, Color.red, 1f);

        // Пускаем луч по ВСЕМ выбранным поверхностям
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, rayDistance, raycastMask))
        {
            int hitLayer = hit.collider.gameObject.layer;

            // Проверяем, к какому именно типу относится слой
            if (ContainsLayer(grassMask, hitLayer))
            {
                if (grassSteps.Length > 0) clipsToPlay = grassSteps;
            }
            else if (ContainsLayer(stoneMask, hitLayer))
            {
                if (stoneSteps.Length > 0) clipsToPlay = stoneSteps;
            }
            else if (ContainsLayer(woodMask, hitLayer))
            {
                if (woodSteps.Length > 0) clipsToPlay = woodSteps;
            }
            else if (ContainsLayer(mudMask, hitLayer))
            {
                if (mudSteps.Length > 0) clipsToPlay = mudSteps;
            }
            else if (ContainsLayer(waterMask, hitLayer))
            {
                if (waterSteps.Length > 0) clipsToPlay = waterSteps;
            }
            // Если слой есть в raycastMask, но для него нет своей маски (например, обычный слой Default),
            // то скрипт просто сыграет звуки из defaultSteps
        }

        if (clipsToPlay == null || clipsToPlay.Length == 0) return;

        int randomIndex = Random.Range(0, clipsToPlay.Length);
        audioSource.pitch = Random.Range(minPitch, maxPitch);
        audioSource.PlayOneShot(clipsToPlay[randomIndex]);
    }

    private bool ContainsLayer(LayerMask mask, int layer)
    {
        return (mask.value & (1 << layer)) != 0;
    }
}