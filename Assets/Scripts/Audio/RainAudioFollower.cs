
using UnityEngine;

public class RainAudioFollower : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private Collider soundBounds;

    [Header("3D / 2D Transition")]
    [SerializeField] private float full3DDistance = 15f;
    [SerializeField] private float full2DDistance = 3f;
    [SerializeField] private float transitionSpeed = 3f;

    [Header("Position")]
    [SerializeField] private float followSpeed = 10f;

    private void LateUpdate()
    {
        if (!player || !audioSource || !soundBounds)
            return;

        // Get closest point on or inside collider.
        Vector3 targetPosition = soundBounds.ClosestPoint(player.position);

        // Smoothly follow the target.
        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            followSpeed * Time.deltaTime
        );

        // Calculate distance to the player.
        float distance = Vector3.Distance(
            player.position,
            transform.position
        );

        // 1 = 3D, 0 = 2D.
        float targetBlend = Mathf.InverseLerp(
            full2DDistance,
            full3DDistance,
            distance
        );

        audioSource.spatialBlend = Mathf.MoveTowards(
            audioSource.spatialBlend,
            targetBlend,
            transitionSpeed * Time.deltaTime
        );
    }
}
