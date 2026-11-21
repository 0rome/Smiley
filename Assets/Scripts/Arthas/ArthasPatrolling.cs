using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;
using System.Threading; // Нужно для CancellationTokenSource

public class ArthasPatrolling : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Transform[] pointsTransform;
    [SerializeField] private float walkSpeed = 3f;

    private ArthasController controller;
    private NavMeshAgent agent;
    private Animator animator;

    private bool walkingToPoint;
    private bool lookingAround;
    private Transform currentPointTransform;

    private CancellationTokenSource lookAroundCTS;

    public void Init(ArthasController controller, NavMeshAgent agent, Animator animator)
    {
        this.controller = controller;
        this.agent = agent;
        this.animator = animator;
    }

    private void Update()
    {
        if (controller.CurrentState == ArthasState.Patrol)
        {
            CheckDistance();
        }
    }

    private void CheckDistance()
    {
        if (currentPointTransform == null || lookingAround || !walkingToPoint) return;

        // Проверяем, дошли ли мы до точки
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            walkingToPoint = false;
            LookAround().Forget();
        }
    }

    public void StopPatrol()
    {
        // Отменяем ожидание на точке, если нас прервали (например, увидели игрока)
        lookAroundCTS?.Cancel();
        walkingToPoint = false;
        lookingAround = false;
    }

    public void ResumePatrol()
    {
        controller.ChangeState(ArthasState.Patrol);
        lookingAround = false;
        GoToNextPatrolPoint();
    }

    private void GoToNextPatrolPoint()
    {
        if (pointsTransform.Length == 0) return;

        agent.speed = walkSpeed;
        agent.isStopped = false;

        animator.SetBool("isWalking", true);
        animator.SetBool("isLookingAround", false);
        animator.SetBool("isRunning", false);

        int randomPoint = Random.Range(0, pointsTransform.Length);
        currentPointTransform = pointsTransform[randomPoint];

        agent.SetDestination(currentPointTransform.position);
        walkingToPoint = true;
    }

    protected async UniTask LookAround()
    {
        lookAroundCTS?.Cancel();
        lookAroundCTS = new CancellationTokenSource();
        var token = lookAroundCTS.Token;

        lookingAround = true;

        animator.SetBool("isLookingAround", true);
        animator.SetBool("isWalking", false);
        animator.SetBool("isRunning", false);
        agent.isStopped = true;

        try
        {
            await UniTask.WaitForSeconds(4f, cancellationToken: token);

            // Если дошли сюда, значит нас не прервали
            animator.SetBool("isLookingAround", false);
            animator.SetBool("isWalking", true);
            agent.isStopped = false;

            lookingAround = false;
            GoToNextPatrolPoint();
        }
        catch (System.OperationCanceledException)
        {
            // Задача была отменена из-за начала погони, ничего не делаем
        }
    }
}