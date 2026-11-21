using UnityEngine;


public enum SmileManState
{
    Patrolling,
    LookingAround,
    ChasingPlayer,
    ChasingPoint
}

public sealed class SmileManController : MonoBehaviour
{
    public SmileManState smileManState;

    [Header("Components")]
    [SerializeField] private SmileManPatrolling smileManPatrolling;
    [SerializeField] private SmileManChasing smileManChasing;
    [SerializeField] private SmileManVision smileManVision;
    [SerializeField] private SmileManDoors smileManDoors;
    [SerializeField] private SmilemanFootstepsAudio smilemanFootstepsAudio;
    [SerializeField] private SmileManAudio smileManAudio;

    [Header("Other components")]
    [SerializeField] private CapsuleCollider capsuleCollider;
    

    public void ActivateSmileMan()
    {
        smileManPatrolling.enabled = true;
        smileManChasing.enabled = true;
        smileManVision.enabled = true;
        smileManDoors.enabled = true;
        smilemanFootstepsAudio.enabled = true;
        smileManAudio.enabled = true;

        capsuleCollider.enabled = true;
    }
    public void DeactivateSmileMan()
    {
        smileManPatrolling.enabled = false;
        smileManChasing.enabled = false;
        smileManVision.enabled = false;
        smileManDoors.enabled = false;
        smilemanFootstepsAudio.enabled = false;
        smileManAudio.enabled = false;

        capsuleCollider.enabled = false;
    }
}
