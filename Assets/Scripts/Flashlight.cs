using UnityEngine;

public class Flashlight : MonoBehaviour
{
    [SerializeField] private PlayerLook playerLook;

    [Header("Settings")]
    [SerializeField] private GameObject lightObj;
    [SerializeField] private float amount = 3f;
    [SerializeField] private float smooth = 8f;

    private bool isActive = false;

    private Quaternion startRotation;

    private void Update()
    {
        InputFlashlight();
    }

    private void Start()
    {
        startRotation = transform.localRotation;
    }

    private void LateUpdate()
    {
        Vector2 look = playerLook.LookDelta;

        Quaternion target =
            startRotation *
            Quaternion.Euler(
                look.y * amount,
                -look.x * amount,
                0);

        transform.localRotation = Quaternion.Slerp(
            transform.localRotation,
            target,
            smooth * Time.deltaTime);
    }

    private void InputFlashlight()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            isActive = !isActive;              
            lightObj.SetActive(isActive);      
        }
    }
}