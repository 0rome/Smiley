
using UnityEngine;
using Zenject;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float interactRange = 3f;
    [SerializeField] private float sphereRadius = 0.3f;
    [SerializeField] private LayerMask interactableLayer;

    [Header("Hold Settings")]
    [SerializeField] private float holdDuration = 1f;

    [Header("UI")]
    [SerializeField] private PlayerInteractionUI interactionUI;

    [Header("Debug")]
    [SerializeField] private bool showGizmos = true;

    [Inject] private InputSettings inputSettings;

    private IInteractable currentInteractable;

    private float holdTimer;
    private bool interactionCompleted;

    private Vector3 debugOrigin;
    private Vector3 debugDirection;
    private bool debugHit;

    private void Update()
    {
        CheckForInteractable();
        HandleInteractionInput();
    }


    private void CheckForInteractable()
    {
        Ray ray = new Ray(
            playerCamera.transform.position,
            playerCamera.transform.forward
        );

        debugOrigin = ray.origin;
        debugDirection = ray.direction;

        IInteractable hitInteractable = null;
        Vector3 hitPosition = Vector3.zero;

        RaycastHit[] hits = Physics.SphereCastAll(
            ray,
            sphereRadius,
            interactRange,
            Physics.AllLayers,
            QueryTriggerInteraction.Collide
        );

        System.Array.Sort(hits, (a, b) =>
            a.distance.CompareTo(b.distance));

        debugHit = false;

        foreach (RaycastHit hit in hits)
        {
            IInteractable interactable =
                hit.collider.GetComponentInParent<IInteractable>();

            if (interactable == null)
                continue;

            hitInteractable = interactable;
            hitPosition = hit.collider.bounds.center;
            debugHit = true;

            break;
        }

        if (hitInteractable != currentInteractable)
        {
            currentInteractable?.OnLookExit();

            currentInteractable = hitInteractable;

            currentInteractable?.OnLookEnter();

            ResetHold();
        }

        if (currentInteractable != null)
        {
            interactionUI.Show(
                hitPosition,
                currentInteractable.InteractableName
            );
        }
        else
        {
            interactionUI.Hide();
        }
    }



    private void HandleInteractionInput()
    {
        if (currentInteractable == null)
            return;

        if (Input.GetKey(inputSettings.interact))
        {
            if (interactionCompleted)
                return;

            holdTimer += Time.deltaTime;

            interactionUI.SetHolding(true);

            float progress = Mathf.Clamp01(
                holdTimer / Mathf.Max(holdDuration, 0.01f)
            );

            interactionUI.SetHoldProgress(progress);

            if (holdTimer >= holdDuration)
            {
                currentInteractable.Interact();

                interactionCompleted = true;
                interactionUI.SetHolding(false);
            }
        }
        else
        {
            // Отпустили клавишу — разрешаем повторное взаимодействие.
            ResetHold();
        }
    }


    private void ResetHold()
    {
        holdTimer = 0f;
        interactionCompleted = false;

        if (interactionUI != null)
            interactionUI.SetHolding(false);
    }

    private void OnDisable()
    {
        currentInteractable?.OnLookExit();
        currentInteractable = null;

        ResetHold();

        if (interactionUI != null)
            interactionUI.Hide();
    }

    private void OnDrawGizmosSelected()
    {
        if (!showGizmos || playerCamera == null)
            return;

        Vector3 origin = Application.isPlaying
            ? debugOrigin
            : playerCamera.transform.position;

        Vector3 direction = Application.isPlaying
            ? debugDirection
            : playerCamera.transform.forward;

        Vector3 end = origin + direction * interactRange;

        Gizmos.color = Application.isPlaying && debugHit
            ? Color.green
            : Color.yellow;

        Gizmos.DrawWireSphere(origin, sphereRadius);
        Gizmos.DrawWireSphere(end, sphereRadius);

        Vector3 right = playerCamera.transform.right * sphereRadius;
        Vector3 up = playerCamera.transform.up * sphereRadius;

        Gizmos.DrawLine(origin + right, end + right);
        Gizmos.DrawLine(origin - right, end - right);
        Gizmos.DrawLine(origin + up, end + up);
        Gizmos.DrawLine(origin - up, end - up);

        Gizmos.DrawLine(origin, end);
    }
}
