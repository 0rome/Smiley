
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public sealed class PlayerStateSwitcher : MonoBehaviour
{
    [Header("Player components")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerInteraction playerInteractions;
    [SerializeField] private PlayerLook playerLook;
    [SerializeField] private PlayerFootsteps playerFootsteps;
    [SerializeField] private NightVision nightVision;
    // [SerializeField] private HeadMotionController headMotionController;

    [Header("Other player components")]
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private CustomPassVolume customPassVolume;

    private readonly HashSet<object> controlBlockers = new();

    private bool isActiveControll = true;

    public bool IsActiveControll => isActiveControll;

    public bool CanControlPlayer => controlBlockers.Count == 0;

    private void Awake()
    {
        ApplyPlayerState();
    }

    public void DeactivatePlayerControl(object blocker)
    {
        if (blocker == null)
            return;

        controlBlockers.Add(blocker);

        ApplyPlayerState();
    }

    public void ActivatePlayerControl(object blocker)
    {
        if (blocker == null)
            return;

        controlBlockers.Remove(blocker);

        ApplyPlayerState();
    }

    private void ApplyPlayerState()
    {
        bool canControl = CanControlPlayer;

        if (isActiveControll == canControl)
            return;

        isActiveControll = canControl;

        customPassVolume.customPasses[5].enabled = canControl;

        characterController.enabled = canControl;

        if (!canControl)
            playerLook.ResetLook();

        playerController.enabled = canControl;
        playerInteractions.enabled = canControl;
        playerLook.enabled = canControl;
        nightVision.enabled = canControl;
        playerFootsteps.enabled = canControl;

        // headMotionController.enabled = canControl;
    }
}
