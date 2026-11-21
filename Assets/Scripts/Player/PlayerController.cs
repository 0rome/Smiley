
using UnityEngine;
using Zenject;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterController controller;

    [Header("Speed")]
    [SerializeField] private float walkSpeed = 4f;
    [SerializeField] private float sprintSpeed = 7f;
    [SerializeField] private float crouchSpeed = 2f;

    [Header("Acceleration")]
    [SerializeField] private float acceleration = 12f;

    [Header("Gravity")]
    [SerializeField] private float gravity = -25f;

    [Header("Crouch")]
    [SerializeField] private float standingHeight = 2f;
    [SerializeField] private float crouchHeight = 0.9f;
    [SerializeField] private float crouchSmooth = 12f;

    [Inject] private InputSettings inputSettings;

    private Vector3 verticalVelocity;
    private Vector3 previousPlanarVelocity;

    private float standingCenterY;
    private float crouchCenterY;
    private float previousVerticalVelocity;

    private bool wasGrounded;

    public bool IsRunning { get; private set; }
    public bool IsMoving { get; private set; }
    public bool IsCrouching { get; private set; }
    public bool IsGrounded { get; private set; }

    public Vector3 PlanarVelocity { get; private set; }
    public Vector3 PlanarAcceleration { get; private set; }
    public Vector3 MoveDirection { get; private set; }

    public float VerticalVelocity { get; private set; }
    public float LandingSpeed { get; private set; }

    public float CurrentSpeed => PlanarVelocity.magnitude;

    private void Reset()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Awake()
    {
        if (controller == null)
            controller = GetComponent<CharacterController>();

        standingCenterY = controller.center.y;
        crouchCenterY = crouchHeight * 0.5f;
    }

    private void Update()
    {
        LandingSpeed = 0f;

        Move();
        Crouch();
        UpdateMovementData();
    }

    private void Move()
    {
        float x = 0f;
        float z = 0f;

        if (Input.GetKey(inputSettings.forward)) z += 1f;
        if (Input.GetKey(inputSettings.backward)) z -= 1f;
        if (Input.GetKey(inputSettings.right)) x += 1f;
        if (Input.GetKey(inputSettings.left)) x -= 1f;

        Vector3 input = new Vector3(x, 0f, z).normalized;

        MoveDirection = transform.TransformDirection(input);

        IsMoving = input.sqrMagnitude > 0.01f;

        IsRunning =
            Input.GetKey(inputSettings.sprint) &&
            !IsCrouching &&
            z > 0f &&
            IsMoving;

        float speed = IsRunning
            ? sprintSpeed
            : IsCrouching
                ? crouchSpeed
                : walkSpeed;

        if (controller.isGrounded && verticalVelocity.y < 0f)
            verticalVelocity.y = -2f;

        verticalVelocity.y += gravity * Time.deltaTime;

        Vector3 finalMovement =
            MoveDirection * speed +
            Vector3.up * verticalVelocity.y;

        // IMPORTANT: only one Move call per frame
        controller.Move(finalMovement * Time.deltaTime);
    }

    private void UpdateMovementData()
    {
        float dt = Mathf.Max(Time.deltaTime, 0.0001f);

        Vector3 actualVelocity = controller.velocity;

        PlanarVelocity = new Vector3(
            actualVelocity.x,
            0f,
            actualVelocity.z
        );

        Vector3 rawAcceleration =
            (PlanarVelocity - previousPlanarVelocity) / dt;

        PlanarAcceleration = Vector3.Lerp(
            PlanarAcceleration,
            rawAcceleration,
            1f - Mathf.Exp(-acceleration * dt)
        );

        IsGrounded = controller.isGrounded;

        if (IsGrounded && !wasGrounded && previousVerticalVelocity < -1f)
        {
            LandingSpeed = Mathf.Abs(previousVerticalVelocity);
        }

        VerticalVelocity = actualVelocity.y;

        previousVerticalVelocity = actualVelocity.y;
        previousPlanarVelocity = PlanarVelocity;

        wasGrounded = IsGrounded;
    }

    private void Crouch()
    {
        if (Input.GetKey(KeyCode.LeftControl))
        {
            IsCrouching = true;
        }
        else if (CanStand())
        {
            IsCrouching = false;
        }

        float targetHeight = IsCrouching
            ? crouchHeight
            : standingHeight;

        float targetCenterY = IsCrouching
            ? crouchCenterY
            : standingCenterY;

        controller.height = Mathf.Lerp(
            controller.height,
            targetHeight,
            Time.deltaTime * crouchSmooth
        );

        Vector3 center = controller.center;

        center.y = Mathf.Lerp(
            center.y,
            targetCenterY,
            Time.deltaTime * crouchSmooth
        );

        controller.center = center;
    }

    private bool CanStand()
    {
        float radius = controller.radius * 0.9f;

        Vector3 bottom =
            transform.position + Vector3.up * radius;

        Vector3 top =
            transform.position + Vector3.up * (standingHeight - radius);

        return !Physics.CheckCapsule(
            bottom,
            top,
            radius,
            ~LayerMask.GetMask("Player")
        );
    }
}
