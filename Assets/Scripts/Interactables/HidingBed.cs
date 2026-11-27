using UnityEngine;
using Zenject;

public class HidingBed : InteractableBase, IHideble
{
    [SerializeField] private Transform cameraPivot;

    [Inject] private PlayerStateSwitcher playerStateSwitcher;
    [Inject] private ScreenClueManager screenClueManager;

    private bool isPlayerInside;
    private Vector3 playerPreviousPosition;


    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && isPlayerInside)
        {
            HidePlayer();
        }
    }

    public override void Interact()
    {
        HidePlayer();
    }
    public void HidePlayer()
    {
        if (!isPlayerInside)
        {
            playerPreviousPosition = playerStateSwitcher.transform.position;

            playerStateSwitcher.DeactivatePlayerControl();

            playerStateSwitcher.transform.position = cameraPivot.position;
            playerStateSwitcher.transform.rotation = cameraPivot.rotation;

            screenClueManager.ShowClue("Press Space to exit the bed");

            isPlayerInside = true;
        }
        else
        {
            playerStateSwitcher.transform.position = playerPreviousPosition;

            playerStateSwitcher.ActivatePlayerControl();

            isPlayerInside = false;
        }
    }
}
