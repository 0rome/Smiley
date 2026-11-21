
using System;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public sealed class ProceduralWalker : MonoBehaviour
{
    [Serializable]
    private class GaitSettings
    {
        [Header("Step")]
        [Min(0.05f)] public float stepTriggerDistance = 0.35f;
        [Min(0.05f)] public float maxStepDistance = 0.7f;
        [Min(0.05f)] public float stepDuration = 0.32f;
        [Min(0f)] public float stepHeight = 0.12f;

        [Header("Foot Placement")]
        [Min(0f)] public float leadTimeFactor = 0.15f;
        [Min(0f)] public float maxLandingLead = 0.2f;
    }

    [Header("IK Targets (Two Bone IK)")]
    [SerializeField] private Transform leftTarget;
    [SerializeField] private Transform rightTarget;

    [Header("Foot Home Positions")]
    [SerializeField] private Transform leftHome;
    [SerializeField] private Transform rightHome;

    [Header("Gait Settings")]
    [SerializeField]
    private GaitSettings walk = new GaitSettings
    {
        stepTriggerDistance = 0.35f,
        maxStepDistance = 0.65f,
        stepDuration = 0.32f,
        stepHeight = 0.12f,
        leadTimeFactor = 0.15f,
        maxLandingLead = 0.18f
    };

    [SerializeField]
    private GaitSettings run = new GaitSettings
    {
        stepTriggerDistance = 0.48f,
        maxStepDistance = 0.95f,
        stepDuration = 0.23f,
        stepHeight = 0.24f,
        leadTimeFactor = 0.2f,
        maxLandingLead = 0.35f
    };

    [Header("Movement Detection")]
    [SerializeField] private float minMovementSpeed = 0.08f;
    [SerializeField] private float velocitySmoothing = 12f;
    [SerializeField] private float headingSmoothRate = 12f;

    [Header("Step Protection")]
    [SerializeField] private float minStepInterval = 0.08f;
    [SerializeField] private bool alternateFeet = true;

    [Header("Foot Orientation")]
    [SerializeField] private bool useMovementDirection = true;
    [SerializeField] private Vector3 leftFootEulerOffset = new Vector3(-60f, 0f, 180f);
    [SerializeField] private Vector3 rightFootEulerOffset = new Vector3(-60f, 0f, 180f);
    [SerializeField] private float maxGroundTilt = 45f;

    [Header("Ground Detection")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float raycastHeight = 1f;
    [SerializeField] private float footOffset = 0.05f;

    [Header("Step Trajectory")]
    [SerializeField]
    private AnimationCurve liftCurve = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(0.5f, 1f),
        new Keyframe(1f, 0f)
    );

    public static event Action OnStepEvent;

    private NavMeshAgent agent;
    private SmileManController smileManController;

    private Vector3 bodyVelocity;
    private Vector3 smoothedHeading;

    private Transform activeFoot;
    private Vector3 stepStartPosition;
    private Vector3 stepTargetPosition;

    private Quaternion stepStartRotation;
    private Quaternion stepTargetRotation;

    private GaitSettings activeGait;

    private float stepTimer;
    private float lastStepTime = -1000f;

    private bool isStepping;
    private bool activeStepIsLeft;
    private bool nextStepIsLeft = true;

    private bool initialized;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        smileManController = GetComponent<SmileManController>();

        if (leftTarget == null || rightTarget == null ||
            leftHome == null || rightHome == null)
        {
            Debug.LogError("ProceduralWalker: IK references are missing!", this);
            enabled = false;
            return;
        }

        // IK targets must remain in world space.
        leftTarget.SetParent(null, true);
        rightTarget.SetParent(null, true);

        bodyVelocity = Vector3.zero;

        smoothedHeading = transform.forward;
        smoothedHeading.y = 0f;

        if (smoothedHeading.sqrMagnitude < 0.0001f)
            smoothedHeading = Vector3.forward;

        smoothedHeading.Normalize();

        SnapFootToGround(leftTarget, leftHome, true);
        SnapFootToGround(rightTarget, rightHome, false);

        initialized = true;
    }

    private void Update()
    {
        if (!initialized)
            return;

        UpdateMovementData();

        // An ongoing step must always finish,
        // even if the NavMeshAgent has stopped.
        if (isStepping)
        {
            UpdateStep();
            return;
        }

        if (!CanStartWalking())
            return;

        activeGait = GetCurrentGait();

        TryStartStep();
    }

    // --------------------------------------------------
    // MOVEMENT
    // --------------------------------------------------

    private void UpdateMovementData()
    {
        Vector3 rawVelocity = Vector3.zero;

        if (agent.enabled && agent.isOnNavMesh && !agent.isStopped)
        {
            rawVelocity = agent.velocity;
        }

        rawVelocity.y = 0f;

        float velocityT = 1f - Mathf.Exp(
            -velocitySmoothing * Time.deltaTime
        );

        bodyVelocity = Vector3.Lerp(
            bodyVelocity,
            rawVelocity,
            velocityT
        );

        UpdateHeading();
    }

    private void UpdateHeading()
    {
        Vector3 desiredDirection = Vector3.zero;

        if (useMovementDirection)
        {
            Vector3 desiredVelocity = agent.desiredVelocity;
            desiredVelocity.y = 0f;

            if (desiredVelocity.sqrMagnitude > 0.01f)
                desiredDirection = desiredVelocity;
            else
                desiredDirection = bodyVelocity;
        }
        else
        {
            desiredDirection = transform.forward;
        }

        desiredDirection.y = 0f;

        if (desiredDirection.sqrMagnitude < 0.0001f)
            desiredDirection = transform.forward;

        desiredDirection.Normalize();

        float t = 1f - Mathf.Exp(
            -headingSmoothRate * Time.deltaTime
        );

        Vector3 result = Vector3.Lerp(
            smoothedHeading,
            desiredDirection,
            t
        );

        if (result.sqrMagnitude > 0.0001f)
            smoothedHeading = result.normalized;
    }

    private bool CanStartWalking()
    {
        if (!agent.enabled || !agent.isOnNavMesh || agent.isStopped)
            return false;

        if (bodyVelocity.magnitude < minMovementSpeed)
            return false;

        if (smileManController != null &&
            smileManController.smileManState == SmileManState.LookingAround)
            return false;

        return true;
    }

    private GaitSettings GetCurrentGait()
    {
        if (smileManController == null)
            return walk;

        switch (smileManController.smileManState)
        {
            case SmileManState.ChasingPlayer:
            case SmileManState.ChasingPoint:
                return run;

            default:
                return walk;
        }
    }

    // --------------------------------------------------
    // STEP SELECTION
    // --------------------------------------------------

    private void TryStartStep()
    {
        if (Time.time - lastStepTime < minStepInterval)
            return;

        GetGroundInfo(leftHome.position,
            out Vector3 leftGround, out _);

        GetGroundInfo(rightHome.position,
            out Vector3 rightGround, out _);

        float leftDistance = GetHorizontalDistance(
            leftTarget.position,
            leftGround
        );

        float rightDistance = GetHorizontalDistance(
            rightTarget.position,
            rightGround
        );

        bool leftNeedsStep =
            leftDistance > activeGait.stepTriggerDistance;

        bool rightNeedsStep =
            rightDistance > activeGait.stepTriggerDistance;

        if (!leftNeedsStep && !rightNeedsStep)
            return;

        bool stepLeft;

        if (leftNeedsStep && rightNeedsStep)
        {
            if (alternateFeet)
            {
                stepLeft = nextStepIsLeft;
            }
            else
            {
                stepLeft = leftDistance >= rightDistance;
            }
        }
        else
        {
            stepLeft = leftNeedsStep;
        }

        StartStep(stepLeft);
    }


    private void StartStep(bool isLeft)
    {
        activeStepIsLeft = isLeft;

        activeFoot = isLeft ? leftTarget : rightTarget;
        Transform home = isLeft ? leftHome : rightHome;

        lastStepTime = Time.time;
        nextStepIsLeft = !isLeft;

        // Время переноса стопы.
        float duration = activeGait.stepDuration;

        // Реальная скорость NavMeshAgent.
        Vector3 velocity = agent.velocity;
        velocity.y = 0f;

        // Предсказываем положение Home к моменту
        // завершения переноса стопы.
        Vector3 predictedPosition =
            home.position + velocity * duration;

        // Дополнительный вынос стопы вперёд.
        float speed = velocity.magnitude;

        float extraLead = Mathf.Clamp(
            speed * 0.06f,
            0.05f,
            0.25f
        );

        Vector3 direction = velocity.sqrMagnitude > 0.01f
            ? velocity.normalized
            : smoothedHeading;

        predictedPosition += direction * extraLead;

        GetGroundInfo(
            predictedPosition,
            out Vector3 targetPosition,
            out Vector3 groundNormal
        );

        stepStartPosition = activeFoot.position;
        stepTargetPosition = targetPosition;

        stepStartRotation = activeFoot.rotation;

        stepTargetRotation = GetFootRotation(
            home,
            groundNormal,
            isLeft
        );

        stepTimer = 0f;
        isStepping = true;
    }


    // --------------------------------------------------
    // STEP ANIMATION
    // --------------------------------------------------

    private void UpdateStep()
    {
        stepTimer += Time.deltaTime;

        float progress = Mathf.Clamp01(
            stepTimer / activeGait.stepDuration
        );

        // Smooth horizontal movement.
        float easedProgress = Mathf.SmoothStep(
            0f, 1f, progress
        );

        Vector3 currentPosition = Vector3.Lerp(
            stepStartPosition,
            stepTargetPosition,
            easedProgress
        );

        // Natural foot lifting trajectory.
        float lift = liftCurve.Evaluate(progress) *
                     activeGait.stepHeight;

        currentPosition.y += lift;

        activeFoot.position = currentPosition;

        activeFoot.rotation = Quaternion.Slerp(
            stepStartRotation,
            stepTargetRotation,
            easedProgress
        );

        if (progress >= 1f)
            FinishStep();
    }

    private void FinishStep()
    {
        activeFoot.position = stepTargetPosition;
        activeFoot.rotation = stepTargetRotation;

        isStepping = false;
        activeFoot = null;

        OnStepEvent?.Invoke();
    }

    // --------------------------------------------------
    // GROUND DETECTION
    // --------------------------------------------------

    private void SnapFootToGround(
        Transform foot,
        Transform home,
        bool isLeft)
    {
        GetGroundInfo(
            home.position,
            out Vector3 groundPosition,
            out Vector3 groundNormal
        );

        foot.position = groundPosition;

        foot.rotation = GetFootRotation(
            home,
            groundNormal,
            isLeft
        );
    }

    private void GetGroundInfo(
        Vector3 startPosition,
        out Vector3 groundPosition,
        out Vector3 groundNormal)
    {
        Vector3 rayOrigin =
            startPosition + Vector3.up * raycastHeight;

        if (Physics.Raycast(
            rayOrigin,
            Vector3.down,
            out RaycastHit hit,
            raycastHeight * 2f,
            groundLayer,
            QueryTriggerInteraction.Ignore))
        {
            groundPosition =
                hit.point + hit.normal * footOffset;

            groundNormal = hit.normal;
        }
        else
        {
            groundPosition = startPosition;
            groundNormal = Vector3.up;
        }
    }

    // --------------------------------------------------
    // FOOT ROTATION
    // --------------------------------------------------

    private Quaternion GetFootRotation(
        Transform home,
        Vector3 groundNormal,
        bool isLeft)
    {
        if (groundNormal.sqrMagnitude < 0.000001f)
            groundNormal = Vector3.up;

        groundNormal.Normalize();

        if (Vector3.Angle(Vector3.up, groundNormal) > maxGroundTilt)
            groundNormal = Vector3.up;

        Vector3 forward = Vector3.ProjectOnPlane(
            smoothedHeading,
            groundNormal
        );

        if (forward.sqrMagnitude < 0.000001f)
        {
            forward = Vector3.ProjectOnPlane(
                home.forward,
                groundNormal
            );
        }

        if (forward.sqrMagnitude < 0.000001f)
            forward = transform.forward;

        forward.Normalize();

        Quaternion baseRotation = Quaternion.LookRotation(
            forward,
            groundNormal
        );

        Vector3 offset = isLeft
            ? leftFootEulerOffset
            : rightFootEulerOffset;

        return baseRotation * Quaternion.Euler(offset);
    }

    private float GetHorizontalDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;

        return Vector3.Distance(a, b);
    }

    // --------------------------------------------------
    // DEBUG
    // --------------------------------------------------

    private void OnDrawGizmosSelected()
    {
        if (leftHome != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(leftHome.position, 0.04f);
        }

        if (rightHome != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(rightHome.position, 0.04f);
        }

        if (leftTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(leftTarget.position, 0.04f);
        }

        if (rightTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(rightTarget.position, 0.04f);
        }

        if (isStepping && activeFoot != null)
        {
            Gizmos.color = Color.yellow;

            Gizmos.DrawLine(
                activeFoot.position,
                stepTargetPosition
            );

            Gizmos.DrawSphere(stepTargetPosition, 0.06f);
        }
    }
}
