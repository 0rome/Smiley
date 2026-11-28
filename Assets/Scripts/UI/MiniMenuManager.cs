using UnityEngine;
using Zenject;

public class MiniMenuManager : MonoBehaviour
{
    [SerializeField] private GameObject miniMenuCanvasObject;

    [Inject] private PlayerStateSwitcher playerStateSwitcher;

    private bool isActive;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleMenu();
        }
    }

    public void ToggleMenu()
    {
        isActive = !isActive;

        miniMenuCanvasObject.SetActive(isActive);

        if (isActive)
        {
            playerStateSwitcher.DeactivatePlayerControl(this);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            playerStateSwitcher.ActivatePlayerControl(this);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
    
}