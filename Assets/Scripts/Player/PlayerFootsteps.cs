using UnityEngine;

public class PlayerFootsteps : MonoBehaviour
{
    [System.Serializable]
    public class SurfaceAudio
    {
        public LayerMask layer;
        public AudioClip[] clips;
    }

    [Header("References")]
    [SerializeField] private AudioSource audioSource;
    private PlayerController player;
    private CharacterController controller;

    [Header("Footsteps")]
    [SerializeField] private SurfaceAudio[] surfaces;

    [Header("Timing")]
    [SerializeField] private float walkStepTime = 0.55f;
    [SerializeField] private float runStepTime = 0.35f;
    [SerializeField] private float crouchStepTime = 0.75f;

    [Tooltip("Задержка первого шага (0 = мгновенный первый шаг, 0.1 = микропауза)")]
    [Range(0f, 0.5f)]
    [SerializeField] private float firstStepDelayMultiplier = 0.05f;

    [Header("Volume & Pitch Realism")]
    [SerializeField] private float walkVolume = 0.6f;
    [SerializeField] private float runVolume = 0.9f;
    [SerializeField] private float crouchVolume = 0.3f;
    [SerializeField] private float minPitch = 0.93f;
    [SerializeField] private float maxPitch = 1.07f;

    [Header("Raycast")]
    [SerializeField] private float rayDistance = 2f;

    private float timer;
    private bool wasMoving;
    private int lastClipIndex = -1;

    private void Start()
    {
        player = GetComponent<PlayerController>();
        controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        // Если не на земле или не двигаемся
        if (!controller.isGrounded || !player.IsMoving)
        {
            wasMoving = false;
            timer = 0f;
            return;
        }

        // Настройка интервала и громкости под режим движения
        float interval = walkStepTime;
        float currentVolume = walkVolume;

        if (player.IsRunning)
        {
            interval = runStepTime;
            currentVolume = runVolume;
        }
        else if (player.IsCrouching)
        {
            interval = crouchStepTime;
            currentVolume = crouchVolume;
        }

        // Если только начали движение — подготавливаем таймер к быстрому первому шагу
        if (!wasMoving)
        {
            wasMoving = true;
            timer = interval * (1f - firstStepDelayMultiplier);
        }

        timer += Time.deltaTime;

        if (timer >= interval)
        {
            PlayFootstep(currentVolume);
            timer = 0f;
        }
    }

    private void PlayFootstep(float volume)
    {
        if (Physics.Raycast(transform.position + Vector3.up * 0.2f,
                Vector3.down,
                out RaycastHit hit,
                rayDistance))
        {
            int hitLayer = hit.collider.gameObject.layer;

            foreach (var surface in surfaces)
            {
                if (((1 << hitLayer) & surface.layer) != 0)
                {
                    if (surface.clips == null || surface.clips.Length == 0)
                        return;

                    // Выбор случайного клипа без повторения подряд
                    int clipIndex = GetRandomClipIndex(surface.clips.Length);
                    AudioClip clip = surface.clips[clipIndex];

                    // Небольшой случайный разброс высоты тона и громкости
                    audioSource.pitch = Random.Range(minPitch, maxPitch);
                    float volumeVariation = Random.Range(0.92f, 1.08f);

                    audioSource.PlayOneShot(clip, volume * volumeVariation);
                    return;
                }
            }
        }
    }

    private int GetRandomClipIndex(int length)
    {
        if (length <= 1) return 0;

        int index = Random.Range(0, length);
        if (index == lastClipIndex)
        {
            index = (index + 1) % length;
        }
        lastClipIndex = index;
        return index;
    }
}