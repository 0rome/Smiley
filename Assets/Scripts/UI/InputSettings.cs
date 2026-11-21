using UnityEngine;

[CreateAssetMenu(fileName = "InputSettings", menuName = "Game/Input Settings")]
public class InputSettings : ScriptableObject
{
    [Header("Movement Keys")]
    public KeyCode forward = KeyCode.W;
    public KeyCode backward = KeyCode.S;
    public KeyCode left = KeyCode.A;
    public KeyCode right = KeyCode.D;
    public KeyCode sprint = KeyCode.LeftShift;
    public KeyCode crouch = KeyCode.LeftControl;

    [Header("Action Keys")]
    public KeyCode interact = KeyCode.E;
    public KeyCode nightVision = KeyCode.F;

    [Header("Mouse Settings")]
    public float mouseSensitivity = 1.0f;
}