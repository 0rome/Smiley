using UnityEngine;

public class ArthasVision : MonoBehaviour
{
    [Header("Vision")]
    [SerializeField] private Transform eyesTransform;
    [SerializeField] private float forwardViewDistance = 30f;
    [SerializeField] private float closeViewDistance = 5f;
    [SerializeField] private float viewAngle = 90f;

    [Tooltip("Если луч летит над головой игрока, поставь Y = 0 или 0.5")]
    [SerializeField] private Vector3 targetOffset = new Vector3(0, 1f, 0);

    [Header("Memory")]
    [SerializeField] private float memoryTime = 3f;
    private float timeSinceLostSight;

    [Header("Layers")]
    [Tooltip("ОБЯЗАТЕЛЬНО выдели тут слой твоего Игрока и слой Стен (Default)")]
    [SerializeField] private LayerMask rayMask;

    private Transform playerTransform;
    private ArthasChasing arthasChasing;

    public bool isSeeingPlayer { get; private set; }
    protected bool isHuntingPlayer;

    public void Init(Transform player, ArthasChasing chasing)
    {
        playerTransform = player;
        arthasChasing = chasing;
    }

    private void Update()
    {
        CheckPlayer();
    }

    private void CheckPlayer()
    {
        if (playerTransform == null) return;

        Vector3 playerBodyPos = playerTransform.position + targetOffset;
        Vector3 direction = playerBodyPos - eyesTransform.position;
        float distance = direction.magnitude;

        isSeeingPlayer = false;

        if (distance <= forwardViewDistance)
        {
            direction.Normalize();

            // Проверка угла или очень близкой дистанции (спиной)
            if (distance <= closeViewDistance || Vector3.Angle(eyesTransform.forward, direction) <= viewAngle * 0.5f)
            {
                if (Physics.Raycast(eyesTransform.position, direction, out RaycastHit hit, distance, rayMask))
                {
                    // Если врезались в игрока или его дочерние объекты
                    if (hit.transform == playerTransform || hit.transform.IsChildOf(playerTransform))
                    {
                        isSeeingPlayer = true;
                        Debug.DrawLine(eyesTransform.position, hit.point, Color.green); // ВИЖУ!
                    }
                    else
                    {
                        Debug.DrawLine(eyesTransform.position, hit.point, Color.red); // Врезался в стену/пол
                        // Убери слеши в строке ниже, чтобы понять, во что врезается луч:
                        // Debug.Log("Луч зрения уперся в: " + hit.transform.name);
                    }
                }
                else
                {
                    Debug.DrawRay(eyesTransform.position, direction * distance, Color.yellow); // Луч не достал
                }
            }
        }

        // Логика обнаружения
        if (isSeeingPlayer)
        {
            timeSinceLostSight = 0f;
            if (!isHuntingPlayer) FindPlayer();

            arthasChasing.UpdateLastKnownPosition(playerTransform.position);
        }
        else if (isHuntingPlayer)
        {
            timeSinceLostSight += Time.deltaTime;
            if (timeSinceLostSight >= memoryTime)
            {
                LosePlayer();
            }
        }
    }

    private void FindPlayer()
    {
        isHuntingPlayer = true;
        arthasChasing.StartChasing();
    }

    private void LosePlayer()
    {
        isHuntingPlayer = false;
        arthasChasing.StopChasing();
    }
}