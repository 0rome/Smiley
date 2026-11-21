using UnityEngine;
using UnityEngine.AI;
using Zenject;

public class SmileManChasing : MonoBehaviour
{
    [SerializeField] private float chaseSpeed = 3.5f;

    [Header("Player Catching")]
    [SerializeField] private float catchDistance = 1.2f;

    private NavMeshAgent agent;
    private Animator animator;

    [Inject] private SmileManController smileManController;
    [Inject] private PlayerController playerController;

    private SmileManPatrolling smileManPatrolling;

    private static readonly int IsRunningHash = Animator.StringToHash("isRunning");

    private void Start()
    {
        smileManPatrolling = smileManController.GetComponent<SmileManPatrolling>();
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
    }

    public void StartChasingPlayer()
    {
        agent.speed = chaseSpeed;
        agent.isStopped = false;
        animator.SetBool(IsRunningHash, true);

        smileManController.smileManState = SmileManState.ChasingPlayer;
    }
    public void StartChasingPoint()
    {
        agent.speed = chaseSpeed;
        agent.isStopped = false;
        animator.SetBool(IsRunningHash, true);

        smileManController.smileManState = SmileManState.ChasingPoint;
    }

    public void StopChasing()
    {
        // Останавливаем агента физически, чтобы он не скользил и не пытался куда-то идти
        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        // Выключаем анимацию бега
        animator.SetBool(IsRunningHash, false);

        smileManPatrolling.FindClosestToPlayerPoint();

        Debug.Log("Search closest to player point");
    }

    public void UpdateDestination(Vector3 targetPosition)
    {
        if (agent.enabled)
        {
            agent.isStopped = false; // Обязательно возобновляем движение, если оно было остановлено
            agent.SetDestination(targetPosition);
        }
    }

    public bool HasReachedTarget()
    {
        if (!agent.pathPending)
        {
            if (agent.remainingDistance <= agent.stoppingDistance)
            {
                return true;
            }
            if (agent.pathStatus == NavMeshPathStatus.PathPartial)
            {
                return true;
            }
        }
        return false;
    }

    public bool HasCaughtPlayer()
    {
        if (playerController == null)
            return false;

        float sqrDistance = (transform.position - playerController.transform.position).sqrMagnitude;

        return sqrDistance <= catchDistance * catchDistance;
    }
}