using UnityEngine;
using UnityEngine.AI;

public class ArthasChasing : MonoBehaviour
{
    [SerializeField] private float runSpeed = 6f;

    private ArthasController controller;
    private NavMeshAgent agent;
    private Animator animator;
    private Transform playerTransform;
    private ArthasPatrolling arthasPatrolling;
    private ArthasAudio arthasAudio;

    private Vector3 lastKnownPosition;

    public void Init(ArthasController controller, NavMeshAgent agent, Animator animator, Transform playerTransform, ArthasPatrolling patrolling, ArthasAudio arthasAudio)
    {
        this.controller = controller;
        this.agent = agent;
        this.animator = animator;
        this.playerTransform = playerTransform;
        this.arthasPatrolling = patrolling;
        this.arthasAudio = arthasAudio;
    }

    public void StartChasing()
    {
        controller.ChangeState(ArthasState.Chase);
        arthasPatrolling.StopPatrol(); // Обязательно прерываем патруль

        arthasAudio.DetectPlayerSound();

        agent.speed = runSpeed;
        agent.isStopped = false;

        animator.SetBool("isWalking", false);
        animator.SetBool("isLookingAround", false);
        animator.SetBool("isRunning", true);

        UpdateLastKnownPosition(playerTransform.position);
    }

    public void UpdateLastKnownPosition(Vector3 pos)
    {
        lastKnownPosition = pos;
    }

    public void OnUpdateChase()
    {
        // Постоянно обновляем цель в навигации
        agent.SetDestination(lastKnownPosition);

        // Проверяем дистанцию (добавил +0.5f для мягкой остановки)
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.5f)
        {
            animator.SetBool("isRunning", false);
            animator.SetBool("isWalking", true);
        }
        else
        {
            animator.SetBool("isRunning", true);
            animator.SetBool("isWalking", false);
        }
    }

    public void StopChasing()
    {
        animator.SetBool("isRunning", false);
        arthasPatrolling.ResumePatrol();
    }
}