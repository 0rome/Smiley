
using UnityEngine;

public class HeadMotionController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController movement;
    [SerializeField] private PlayerLook playerLook;

    [Header("Walking")]
    [SerializeField] private float walkStrideLength = 2.2f;
    [SerializeField] private float walkBob = 0.035f;
    [SerializeField] private float walkSideSway = 0.025f;
    [SerializeField] private float walkForwardSway = 0.018f;
    [SerializeField] private float walkStepImpact = 0.025f;

    [Header("Running")]
    [SerializeField] private float runStrideLength = 3.2f;
    [SerializeField] private float runBob = 0.065f;
    [SerializeField] private float runSideSway = 0.04f;
    [SerializeField] private float runForwardSway = 0.035f;
    [SerializeField] private float runStepImpact = 0.045f;

    [Header("Body Rotation")]
    [SerializeField] private float walkRoll = 1.8f;
    [SerializeField] private float runRoll = 3.2f;
    [SerializeField] private float walkPitch = 1f;
    [SerializeField] private float runPitch = 2f;

    [Header("Handheld Movement")]
    [SerializeField] private float idleNoisePosition = 0.0015f;
    [SerializeField] private float walkNoisePosition = 0.006f;
    [SerializeField] private float runNoisePosition = 0.012f;

    [SerializeField] private float idleNoiseRotation = 0.12f;
    [SerializeField] private float walkNoiseRotation = 0.3f;
    [SerializeField] private float runNoiseRotation = 0.7f;

    [SerializeField] private float noiseFrequency = 2.5f;

    [Header("Inertia")]
    [SerializeField] private float accelerationPosition = 0.003f;
    [SerializeField] private float accelerationRotation = 0.25f;
    [SerializeField] private float inertiaSmooth = 0.12f;

    [Header("Look")]
    [SerializeField] private float lookRoll = 1f;
    [SerializeField] private float maxLookRoll = 5f;

    [Header("Step Impact")]
    [SerializeField] private float stepRecovery = 0.12f;

    [Header("Breathing")]
    [SerializeField] private float breathingFrequency = 0.22f;
    [SerializeField] private float breathingAmplitude = 0.002f;

    [Header("Landing")]
    [SerializeField] private float landingStrength = 0.04f;
    [SerializeField] private float landingRecovery = 0.18f;

    [Header("Smoothing")]
    [SerializeField] private float positionSmooth = 0.07f;
    [SerializeField] private float rotationSmooth = 8f;
    [SerializeField] private float movementBlendSpeed = 5f;

    private Vector3 defaultPosition;
    private Quaternion defaultRotation;

    private Vector3 positionVelocity;
    private Vector3 inertiaOffset;
    private Vector3 inertiaVelocity;

    private float gaitPhase;
    private float movementBlend;
    private float runBlend;
    private float breathingPhase;

    private float stepOffset;
    private float stepVelocity;

    private float landingOffset;
    private float landingVelocity;

    private float currentRoll;
    private float rollVelocity;

    private void Start()
    {
        defaultPosition = transform.localPosition;
        defaultRotation = transform.localRotation;
    }

    private void LateUpdate()
    {
        if (movement == null)
            return;

        float dt = Time.deltaTime;

        if (dt <= 0f)
            return;

        UpdateMovementBlend(dt);
        UpdateGait(dt);
        UpdateStepImpact(dt);
        UpdateLanding(dt);
        UpdateInertia(dt);
        UpdateCamera(dt);
    }

    private void UpdateMovementBlend(float dt)
    {
        float target = Mathf.InverseLerp(
            0.1f,
            movement.IsRunning ? 7f : 4f,
            movement.CurrentSpeed
        );

        if (!movement.IsGrounded)
            target *= 0.5f;

        movementBlend = Mathf.MoveTowards(
            movementBlend,
            target,
            dt * movementBlendSpeed
        );
    }

    private void UpdateGait(float dt)
    {
        float speed = movement.CurrentSpeed;

        if (!movement.IsGrounded || speed < 0.1f)
            return;

        float strideLength = movement.IsRunning
            ? runStrideLength
            : walkStrideLength;

        float previousPhase = gaitPhase;

        gaitPhase +=
            speed / strideLength * Mathf.PI * 2f * dt;

        if (previousPhase < Mathf.PI && gaitPhase >= Mathf.PI)
            TriggerStep();

        if (gaitPhase >= Mathf.PI * 2f)
        {
            gaitPhase -= Mathf.PI * 2f;
            TriggerStep();
        }
    }

    private void TriggerStep()
    {
        float impact = movement.IsRunning
            ? runStepImpact
            : walkStepImpact;

        stepOffset = -impact;
        stepVelocity = 0f;
    }

    private void UpdateStepImpact(float dt)
    {
        stepOffset = Mathf.SmoothDamp(
            stepOffset,
            0f,
            ref stepVelocity,
            stepRecovery,
            Mathf.Infinity,
            dt
        );
    }

    private void UpdateLanding(float dt)
    {
        if (movement.LandingSpeed > 0f)
        {
            float impact = Mathf.Clamp01(
                movement.LandingSpeed / 12f
            );

            landingVelocity -= landingStrength * impact * 20f;
        }

        landingOffset = Mathf.SmoothDamp(
            landingOffset,
            0f,
            ref landingVelocity,
            landingRecovery,
            Mathf.Infinity,
            dt
        );
    }

    private void UpdateInertia(float dt)
    {
        Vector3 localAcceleration =
            movement.transform.InverseTransformDirection(
                movement.PlanarAcceleration
            );

        Vector3 target = new Vector3(
            -localAcceleration.x * accelerationPosition,
            0f,
            -localAcceleration.z * accelerationPosition
        );

        target = Vector3.ClampMagnitude(target, 0.04f);

        inertiaOffset = Vector3.SmoothDamp(
            inertiaOffset,
            target,
            ref inertiaVelocity,
            inertiaSmooth,
            Mathf.Infinity,
            dt
        );
    }

    private void UpdateCamera(float dt)
    {
        float targetRunBlend = movement.IsRunning ? 1f : 0f;

        runBlend = Mathf.MoveTowards(
            runBlend,
            targetRunBlend,
            dt * 4f
        );

        float bob = Mathf.Lerp(walkBob, runBob, runBlend);
        float side = Mathf.Lerp(walkSideSway, runSideSway, runBlend);
        float forward = Mathf.Lerp(walkForwardSway, runForwardSway, runBlend);

        float roll = Mathf.Lerp(walkRoll, runRoll, runBlend);
        float pitch = Mathf.Lerp(walkPitch, runPitch, runBlend);

        // Step movement

        float vertical =
            Mathf.Sin(gaitPhase * 2f) * bob * movementBlend;

        float lateral =
            Mathf.Sin(gaitPhase) * side * movementBlend;

        float forwardMovement =
            Mathf.Cos(gaitPhase * 2f) * forward * movementBlend;

        // Breathing

        breathingPhase += dt * breathingFrequency * Mathf.PI * 2f;

        float breathing =
            Mathf.Sin(breathingPhase) *
            breathingAmplitude *
            (1f - movementBlend);

        // Handheld noise

        float noisePosition = Mathf.Lerp(
            idleNoisePosition,
            Mathf.Lerp(walkNoisePosition, runNoisePosition, runBlend),
            movementBlend
        );

        float noiseRotation = Mathf.Lerp(
            idleNoiseRotation,
            Mathf.Lerp(walkNoiseRotation, runNoiseRotation, runBlend),
            movementBlend
        );

        float time = Time.time * noiseFrequency;

        Vector3 noisePositionOffset = new Vector3(
            Noise(time, 12f),
            Noise(time, 25f),
            Noise(time, 47f)
        ) * noisePosition;

        Vector3 noiseRotationOffset = new Vector3(
            Noise(time, 61f),
            Noise(time, 73f),
            Noise(time, 89f)
        ) * noiseRotation;

        // Crouching

        float crouchMultiplier = movement.IsCrouching ? 0.65f : 1f;

        // Position

        Vector3 targetPosition = defaultPosition;

        targetPosition += new Vector3(
            lateral,
            vertical + breathing +
            stepOffset * crouchMultiplier +
            landingOffset,
            forwardMovement
        );

        targetPosition += inertiaOffset;
        targetPosition += noisePositionOffset;

        transform.localPosition = Vector3.SmoothDamp(
            transform.localPosition,
            targetPosition,
            ref positionVelocity,
            positionSmooth,
            Mathf.Infinity,
            dt
        );

        // Rotation

        Vector3 localAcceleration =
            movement.transform.InverseTransformDirection(
                movement.PlanarAcceleration
            );

        float bodyRoll =
            Mathf.Sin(gaitPhase) * roll * movementBlend;

        float bodyPitch =
            Mathf.Cos(gaitPhase * 2f) * pitch * movementBlend;

        float accelerationRoll =
            localAcceleration.x * accelerationRotation;

        float accelerationPitch =
            -localAcceleration.z * accelerationRotation;

        float mouseX = playerLook != null
            ? playerLook.LookDelta.x
            : 0f;

        float targetRoll =
            -mouseX * lookRoll +
            bodyRoll +
            accelerationRoll +
            noiseRotationOffset.z;

        targetRoll = Mathf.Clamp(
            targetRoll,
            -maxLookRoll,
            maxLookRoll
        );

        currentRoll = targetRoll;

        Quaternion targetRotation =
            defaultRotation *
            Quaternion.Euler(
                bodyPitch + accelerationPitch + noiseRotationOffset.x,
                noiseRotationOffset.y,
                currentRoll
            );

        transform.localRotation = Quaternion.Slerp(
            transform.localRotation,
            targetRotation,
            1f - Mathf.Exp(-rotationSmooth * dt)
        );
    }

    private float Noise(float time, float seed)
    {
        return (Mathf.PerlinNoise(time, seed) - 0.5f) * 2f;
    }
}
