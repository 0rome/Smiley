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
    //[SerializeField] private HeadMotionController headMotionController;

    [Header("Other player components")]
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private CustomPassVolume customPassVolume;

    private bool isActiveControll = true;
    
    public bool IsActiveControll => isActiveControll;

    public void DeactivatePlayerControl()
    {
        customPassVolume.customPasses[5].enabled = false;

        isActiveControll = false;

        playerController.enabled = false;
        playerInteractions.enabled = false;
        playerLook.enabled = false;
        nightVision.enabled = false;
        playerFootsteps.enabled = false;
        //headMotionController.enabled = false;
    }
    public void ActivatePlayerControl()
    {
        customPassVolume.customPasses[5].enabled = true;

        isActiveControll = true;

        playerController.enabled = true;
        playerInteractions.enabled = true;
        playerLook.enabled = true;
        nightVision.enabled = true;
        playerFootsteps.enabled= true;
        //headMotionController.enabled = true;
    }
    
}
