
using UnityEngine;
using Zenject;

public class PlayerLook : MonoBehaviour
{
    [SerializeField] private Transform player;

    [Header("Sensitivity")]
    [SerializeField] private float sensitivity = 1f;
    [SerializeField] private float smooth = 12f;

    [Inject] private InputSettings inputSettings;

    private float xRotation;

    private Vector2 currentLook;
    private Vector2 currentVelocity;

    public Vector2 LookDelta => currentLook;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        sensitivity = inputSettings.mouseSensitivity;
    }

    private void Update()
    {
        Vector2 target = new Vector2(
            Input.GetAxisRaw("Mouse X"),
            Input.GetAxisRaw("Mouse Y")
        ) * sensitivity;

        currentLook = Vector2.SmoothDamp(
            currentLook,
            target,
            ref currentVelocity,
            1f / smooth
        );

        xRotation -= currentLook.y;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        player.Rotate(Vector3.up * currentLook.x);
    }
}
