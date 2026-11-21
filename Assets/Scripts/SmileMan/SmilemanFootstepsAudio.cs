using UnityEngine;

public class SmilemanFootstepsAudio : MonoBehaviour
{
    [System.Serializable]
    public struct SurfaceAudio
    {
        public LayerMask layerMask;     // Слой (например, Ground/Wood, Ground/Stone)
        public AudioClip[] audioClips;  // Набор звуков для этого слоя (для разнообразия)
    }

    [Header("Настройки")]
    [SerializeField] private SurfaceAudio[] surfaceAudios; // Список поверхностей и их звуков
    [SerializeField] private AudioClip[] defaultAudioClips; // Звуки по умолчанию, если слой не найден
    [SerializeField] private AudioSource audioSource;       // Источник звука
    [SerializeField] private float raycastDistance = 1.2f;  // Длина луча вниз
    [SerializeField] private LayerMask groundLayer;         // Все слои полов, которые нужно проверять

    private void Awake()
    {
        // Если AudioSource не назначен, попытаемся взять его с этого же объекта
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }
    private void OnEnable()
    {
        ProceduralWalker.OnStepEvent += PlayFootstepAudio;
    }
    private void OnDisable()
    {
        ProceduralWalker.OnStepEvent -= PlayFootstepAudio;
    }

    /// <summary>
    /// Вызывайте этот метод из анимации (Animation Event) или скрипта движения, когда шаг должен прозвучать.
    /// </summary>
    public void PlayFootstepAudio()
    {
        if (audioSource == null) return;

        AudioClip clipToPlay = GetFootstepAudioBySurface();

        if (clipToPlay != null)
        {
            // Используем PlayOneShot, чтобы звуки шагов могли накладываться друг на друга, если нужно
            audioSource.PlayOneShot(clipToPlay);
        }
    }

    private AudioClip GetFootstepAudioBySurface()
    {
        RaycastHit hit;
        Vector3 rayOrigin = transform.position + Vector3.up * 0.1f; // Чуть выше ног, чтобы луч не уходил сквозь землю

        // Пускаем луч вниз
        if (Physics.Raycast(rayOrigin, Vector3.down, out hit, raycastDistance, groundLayer))
        {
            int hitLayer = hit.collider.gameObject.layer;

            // Ищем подходящий слой в массиве настроек
            foreach (var surface in surfaceAudios)
            {
                // Проверяем, входит ли слой объекта в LayerMask структуры
                if ((surface.layerMask.value & (1 << hitLayer)) > 0)
                {
                    if (surface.audioClips != null && surface.audioClips.Length > 0)
                    {
                        // Возвращаем случайный звук из массива для этого слоя (чтобы шаги не звучали монотонно)
                        int randomIndex = Random.Range(0, surface.audioClips.Length);
                        return surface.audioClips[randomIndex];
                    }
                }
            }
        }

        // Если слой не найден в настройках, но дефолтные звуки заданы — возвращаем их
        if (defaultAudioClips != null && defaultAudioClips.Length > 0)
        {
            return defaultAudioClips[Random.Range(0, defaultAudioClips.Length)];
        }

        return null;
    }

    // Опционально: визуализация луча в редакторе Unity для отладки
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 rayOrigin = transform.position + Vector3.up * 0.1f;
        Gizmos.DrawLine(rayOrigin, rayOrigin + Vector3.down * raycastDistance);
    }
}