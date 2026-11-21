using TMPro;
using UnityEngine;
using Zenject;

public class CodePanel : InteractableBase
{
    [Header("Settings")]
    [SerializeField] private string mainCode = "9316";
    [SerializeField] private TextMeshPro codeTMP;
    [SerializeField] private Door doorToUnlock;

    [Header("Transforms")]
    [SerializeField] private Transform playerHolderTransform;
    [SerializeField] private Transform cameraTransform;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip rightClip;
    [SerializeField] private AudioClip wrongClip;

    [Inject] private PlayerController playerController;
    private PlayerStateSwitcher playerStateSwitcher;

    private bool playerInPanel;
    public bool IsActive => playerInPanel;

    private void Start()
    {
        playerStateSwitcher = playerController.GetComponent<PlayerStateSwitcher>();
        codeTMP.text = string.Empty;
    }

    public void EnterNumber(string number)
    {
        if (codeTMP.text.Length >= mainCode.Length) return;
        codeTMP.text += number;
    }

    public void DeleteAllCode()
    {
        codeTMP.text = string.Empty;
    }

    public void EnterAllCode()
    {
        if (codeTMP.text == mainCode)
        {
            Debug.Log("Код верный");

            if (doorToUnlock.IsOpened) return;

            if (audioSource != null && rightClip != null)
                audioSource.PlayOneShot(rightClip);

            doorToUnlock.Interact();

            // тут можно вызвать событие открытия двери
            DeleteAllCode();
            ExitPanel();
        }
        else
        {
            Debug.Log("Код неверный");
            if (audioSource != null && wrongClip != null)
                audioSource.PlayOneShot(wrongClip);

            DeleteAllCode();
        }
    }

    public override void Interact()
    {
        if (playerInPanel) return;

        playerController.transform.position = playerHolderTransform.position;
        playerController.transform.rotation = playerHolderTransform.rotation;

        // Камера — дочерний объект игрока: выставляем локальную ротацию,
        // чтобы она смотрела туда же, куда смотрит холдер.
        if (cameraTransform != null)
            cameraTransform.localRotation = Quaternion.identity;

        playerStateSwitcher.DeactivatePlayerControl();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        playerInPanel = true;
    }

    private void ExitPanel()
    {
        if (!playerInPanel) return;

        playerStateSwitcher.ActivatePlayerControl();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        playerInPanel = false;

        DeleteAllCode();
    }

    private void Update()
    {
        if (playerInPanel && Input.GetKeyDown(KeyCode.Escape))
            ExitPanel();
    }

    private void OnDisable()
    {
        // Страховка: если объект выключили/уничтожили, пока игрок в панели —
        // возвращаем управление.
        if (playerInPanel)
            ExitPanel();
    }
}