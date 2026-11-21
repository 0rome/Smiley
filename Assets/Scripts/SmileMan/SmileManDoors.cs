using UnityEngine;
using UnityEngine.Animations.Rigging;

public class SmileManDoors : MonoBehaviour
{
    [Header("IK Settings")]
    [SerializeField] private Transform RightHandTarget;
    [SerializeField] private TwoBoneIKConstraint RightHandIk;
    [SerializeField] private float ikSpeed = 5f;

    [Header("Detection Settings")]
    [SerializeField] private float detectionRadius = 2.5f;
    [SerializeField] private float interactDistance = 1.2f;
    [SerializeField] private LayerMask doorLayer;

    private Door currentDoor;

    private void Update()
    {
        CheckForDoor();
        HandleIKAndInteraction();
    }

    private void CheckForDoor()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius, doorLayer);

        Door closestDoor = null;
        float minDistance = float.MaxValue;

        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<Door>(out var door))
            {
                // Взаимодействуем только с ЗАКРЫТЫМИ дверями
                if (door.IsOpened) continue;

                float dist = Vector3.Distance(transform.position, door.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    closestDoor = door;
                }
            }
        }

        currentDoor = closestDoor;
    }

    private void HandleIKAndInteraction()
    {
        if (currentDoor != null && !currentDoor.IsOpened)
        {
            float distanceToDoor = Vector3.Distance(transform.position, currentDoor.transform.position);

            if (currentDoor.HandleTransform != null)
            {
                RightHandTarget.position = currentDoor.HandleTransform.position;
                RightHandTarget.rotation = currentDoor.HandleTransform.rotation;
            }

            // Натягиваем руку к ручке
            RightHandIk.weight = Mathf.MoveTowards(RightHandIk.weight, 1f, Time.deltaTime * ikSpeed);

            // Нажимаем ручку / открываем дверь
            if (distanceToDoor <= interactDistance)
            {
                currentDoor.Interact();
                currentDoor = null; // Сбрасываем текущую дверь сразу после нажатия
            }
        }
        else
        {
            // Плавный возврат руки в обычное положение
            RightHandIk.weight = Mathf.MoveTowards(RightHandIk.weight, 0f, Time.deltaTime * ikSpeed);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, interactDistance);
    }
}