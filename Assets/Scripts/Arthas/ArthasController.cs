using UnityEngine;
using UnityEngine.AI;
using Zenject;

public class ArthasController : MonoBehaviour
{
    [Header("Components")]
    public Animator animator { get; private set; }
    public NavMeshAgent agent { get; private set; }

    [Header("Modules")]
    [SerializeField] private ArthasPatrolling arthasPatrolling;
    [SerializeField] private ArthasChasing arthasChasing;
    [SerializeField] private ArthasVision arthasVision;
    [SerializeField] private ArthasAudio arthasAudio;

    public ArthasState CurrentState { get; private set; } = ArthasState.Patrol;
    protected Transform playerTransform;

    [Inject] protected PlayerController playerController;

    protected virtual void Awake()
    {
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();

        if (arthasPatrolling == null) arthasPatrolling = GetComponent<ArthasPatrolling>();
        if (arthasChasing == null) arthasChasing = GetComponent<ArthasChasing>();
        if (arthasVision == null) arthasVision = GetComponent<ArthasVision>();
        if (arthasAudio == null) arthasAudio = GetComponent<ArthasAudio>();
    }

    protected virtual void Start()
    {
        playerTransform = playerController.transform;

        arthasPatrolling.Init(this, agent, animator);
        arthasChasing.Init(this, agent, animator, playerTransform, arthasPatrolling, arthasAudio);
        arthasVision.Init(playerTransform, arthasChasing); // Инициализируем зрение

        arthasPatrolling.ResumePatrol();
    }

    private void Update()
    {
        if (CurrentState == ArthasState.Chase)
        {
            arthasChasing.OnUpdateChase();
        }
    }

    public void ChangeState(ArthasState newState)
    {
        CurrentState = newState;
    }
}