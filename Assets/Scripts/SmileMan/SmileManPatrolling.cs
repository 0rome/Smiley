using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;
using Zenject;

public class SmileManPatrolling : MonoBehaviour
{
    [SerializeField] private Transform[] points;

    private NavMeshAgent agent;
    private Animator animator;
    [Inject] private SmileManController smileManController;
    [Inject] private PlayerController playerController;
    private Vector3 playerPos;
    private bool isLookingAround;

    private static readonly int IsLookingAroundHash = Animator.StringToHash("isLookingAround");

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        if (points != null && points.Length > 0)
        {
            ResumePatrol();
        }
        else
        {
            Debug.LogWarning("Массив точек пуст!", this);
        }
    }

    private void Update()
    {
        if (smileManController.smileManState != SmileManState.Patrolling) return;

        CheckPointDistance();
    }

    public void FindClosestToPlayerPoint()
    {
        if (points == null || points.Length == 0) return;

        // Проверяем, существует ли игрок
        if (playerController == null || !playerController.gameObject.activeInHierarchy)
        {
            ResumePatrol();
            return;
        }

        // Обновляем позицию игрока прямо перед поиском
        playerPos = playerController.transform.position;

        Transform closest = null;
        float minDistance = float.MaxValue;

        foreach (var item in points)
        {
            if (item == null) continue;

            float distance = Vector3.Distance(playerPos, item.transform.position);

            if (distance < minDistance)
            {
                minDistance = distance;
                closest = item;
            }
        }

        if (closest == null)
        {
            ResumePatrol();
            return;
        }

        agent.speed = 1f;
        agent.isStopped = false;
        isLookingAround = false;
        animator.SetBool(IsLookingAroundHash, false);

        smileManController.smileManState = SmileManState.Patrolling;

        agent.SetDestination(closest.position);
    }

    private void CheckPointDistance()
    {
        if (smileManController.smileManState != SmileManState.Patrolling) return;
        if (points == null || points.Length == 0 || isLookingAround) return;

        // Проверяем, действительно ли агент дошёл до Конечной цели, а не застрял на тупиковом обрыве пути
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
            {
                // Если путь частичный (заблокирован физически), пробуем другую точку вместо зависания в осмотре
                if (agent.pathStatus == NavMeshPathStatus.PathPartial)
                {
                    ResumePatrol();
                    return;
                }

                LookAroundAsync().Forget();
            }
        }
    }

    public void ResumePatrol()
    {
        agent.speed = 1f;
        agent.isStopped = false;
        isLookingAround = false;
        animator.SetBool(IsLookingAroundHash, false);

        smileManController.smileManState = SmileManState.Patrolling;

        MoveToRandomPoint();
    }

    private async UniTaskVoid LookAroundAsync()
    {
        smileManController.smileManState = SmileManState.LookingAround;

        isLookingAround = true;
        agent.isStopped = true;

        animator.SetBool(IsLookingAroundHash, true);

        var ct = this.GetCancellationTokenOnDestroy();

        bool isCanceled = await UniTask.WaitUntil(() => !isLookingAround, cancellationToken: ct)
                                       .SuppressCancellationThrow();

        if (isCanceled) return;

        animator.SetBool(IsLookingAroundHash, false);

        agent.isStopped = false;
        ResumePatrol();
    }

    public void StopLookingAround()
    {
        isLookingAround = false;
    }

    private void MoveToRandomPoint()
    {
        if (points == null || points.Length == 0) return;

        // Перебираем точки в случайном порядке и ищем полностью доступную
        int startIndex = Random.Range(0, points.Length);
        NavMeshPath path = new NavMeshPath();

        for (int i = 0; i < points.Length; i++)
        {
            int index = (startIndex + i) % points.Length;
            Vector3 targetPos = points[index].position;

            if (agent.CalculatePath(targetPos, path))
            {
                // Если путь валиден (при настроенном NavMesh через двери pathStatus будет PathComplete)
                if (path.status == NavMeshPathStatus.PathComplete)
                {
                    agent.SetPath(path);
                    return;
                }
            }
        }

        // Запасной вариант, если пути с ошибками
        int fallbackIndex = Random.Range(0, points.Length);
        agent.SetDestination(points[fallbackIndex].position);
    }
}