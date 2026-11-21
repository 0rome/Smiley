using UnityEngine;
using Zenject;

public class SmileManVision : MonoBehaviour
{
    [Header("Настройки обзора")]
    [SerializeField] private float viewDistance = 12f;
    [SerializeField, Range(0, 360)] private float viewAngle = 90f;
    [SerializeField, Range(0, 180)] private float maxVerticalAngle = 45f;
    [SerializeField] private float closeRadius = 2.5f;
    [SerializeField] private float eyeHeight = 1.5f;
    [SerializeField] private float playerCenterOffset = 1.0f;
    [SerializeField] private LayerMask obstacleLayer;

    [Header("Потеря из вида")]
    [SerializeField] private float loseSightDelay = 2.5f;

    [Inject] private PlayerController playerController;
    [Inject] private SmileManController smileManController;
    [Inject] private CutsceneManager cutsceneManager;

    private SmileManChasing chasingScript;
    private SmileManPatrolling patrollingScript;

    private float loseSightTimer;
    private Vector3 lastSeenPosition;
    private bool canSeePlayer;

    private void Start()
    {
        chasingScript = GetComponent<SmileManChasing>();
        patrollingScript = GetComponent<SmileManPatrolling>();
    }
    private void Update()
    {
        UpdateVisionAndTracking();
    }

    private void UpdateVisionAndTracking()
    {
        if (!IsPlayerValid())
        {
            if (smileManController.smileManState == SmileManState.ChasingPlayer)
            {
                StopChasing();
            }
            return;
        }

        canSeePlayer = CheckPlayerInVision();
        HandleTargetTracking();
    }

    private bool IsPlayerValid()
    {
        return playerController != null && playerController.gameObject.activeInHierarchy;
    }

    // --- ЛОГИКА ЗРЕНИЯ ---

    private bool CheckPlayerInVision()
    {
        Vector3 eyePosition = transform.position + Vector3.up * eyeHeight;
        Vector3 playerTargetPos = playerController.transform.position + Vector3.up * playerCenterOffset;

        Vector3 dirToTarget = (playerTargetPos - eyePosition).normalized;
        float distanceToTarget = Vector3.Distance(eyePosition, playerTargetPos);

        // 1. Ближний радиус (360 градусов)
        if (distanceToTarget <= closeRadius)
        {
            return HasLineOfSight(eyePosition, dirToTarget, distanceToTarget);
        }

        // 2. Основной конус обзора
        if (distanceToTarget <= viewDistance)
        {
            return IsInViewCone(dirToTarget) && HasLineOfSight(eyePosition, dirToTarget, distanceToTarget);
        }

        return false;
    }

    private bool IsInViewCone(Vector3 dirToTarget)
    {
        Vector3 forwardXZ = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        Vector3 dirXZ = Vector3.ProjectOnPlane(dirToTarget, Vector3.up).normalized;
        float horizontalAngle = Vector3.Angle(forwardXZ, dirXZ);

        Vector3 targetYZ = Vector3.ProjectOnPlane(dirToTarget, transform.right).normalized;
        float verticalAngle = Vector3.Angle(transform.forward, targetYZ);

        return horizontalAngle <= viewAngle / 2f && verticalAngle <= maxVerticalAngle;
    }

    private bool HasLineOfSight(Vector3 eyePosition, Vector3 direction, float distance)
    {
        return !Physics.Raycast(eyePosition, direction, distance, obstacleLayer);
    }

    // --- ЛОГИКА ТРЕКИНГА И ПОГОНИ ---

    private void HandleTargetTracking()
    {
        if (smileManController.smileManState == SmileManState.ChasingPlayer &&
            chasingScript.HasCaughtPlayer())
        {
            cutsceneManager.PlayCutscene("SmileManCoughtPlayer_Cutscene");
            return;
        }

        if (canSeePlayer)
        {
            // Видим игрока: обновляем точку и сбрасываем таймер потери из вида
            lastSeenPosition = playerController.transform.position;
            loseSightTimer = loseSightDelay;

            if (smileManController.smileManState != SmileManState.ChasingPlayer)
            {
                chasingScript.StartChasingPlayer();
            }

            chasingScript.UpdateDestination(lastSeenPosition);
        }
        else if (smileManController.smileManState == SmileManState.ChasingPlayer)
        {
            // Игрока не видно, но работает таймер задержки памяти
            if (loseSightTimer > 0f)
            {
                loseSightTimer -= Time.deltaTime;

                // ВАЖНО: пока таймер активен, обновляем позицию, чтобы враг 
                // преследовал игрока за углом или вокруг препятствий (столов)
                lastSeenPosition = playerController.transform.position;

                chasingScript.UpdateDestination(lastSeenPosition);
            }
            else
            {
                // Таймер истек, проверяем, добежал ли агент до точки
                if (!chasingScript.HasReachedTarget())
                {
                    chasingScript.UpdateDestination(lastSeenPosition);
                }
                else
                {
                    // Добежал до точки и время вышло — прекращаем погоню
                    StopChasing();
                }
            }
        }
        else if (smileManController.smileManState == SmileManState.ChasingPoint)
        {
            // Движение к точке звука
            if (!chasingScript.HasReachedTarget())
            {
                chasingScript.UpdateDestination(lastSeenPosition);
            }
            else
            {
                StopChasing();
            }
        }
    }

    // Публичный метод для внешнего вызова (например, от звуковых триггеров)
    public void HearSound(Vector3 soundPosition)
    {
        if (smileManController.smileManState != SmileManState.ChasingPoint)
        {
            chasingScript.StartChasingPoint();
        }

        lastSeenPosition = soundPosition;
        loseSightTimer = loseSightDelay;
        chasingScript.UpdateDestination(lastSeenPosition);
    }

    private void StopChasing()
    {
        chasingScript.StopChasing();
        patrollingScript.ResumePatrol(); // Само вернет SmileManState.Patrolling
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, closeRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewDistance);

        Vector3 viewAngleA = DirectionFromAngle(-viewAngle / 2f, false);
        Vector3 viewAngleB = DirectionFromAngle(viewAngle / 2f, false);

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + viewAngleA * viewDistance);
        Gizmos.DrawLine(transform.position, transform.position + viewAngleB * viewDistance);
    }

    private Vector3 DirectionFromAngle(float angleInDegrees, bool angleIsGlobal)
    {
        if (!angleIsGlobal)
        {
            angleInDegrees += transform.eulerAngles.y;
        }
        return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
    }
#endif
}