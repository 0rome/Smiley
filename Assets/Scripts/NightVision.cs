using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using Zenject;

public class NightVision : MonoBehaviour
{
    [SerializeField] private CustomPassVolume customPassVolume;
    [SerializeField] private Volume effectsVolume;
    [SerializeField] private GameObject lightObject;

    [Header("Zoom Settings")]
    [SerializeField] private float zoomSpeed = 2f;
    [SerializeField] private float minZoom = 20f;
    [SerializeField] private float maxZoom = 80f;
    [SerializeField] private float defaultZoom = 60f;

    [Inject] private InputSettings inputSettings;

    private SoundPlayer soundPlayer;
    private Camera playerCamera;

    private bool isActive;
    private bool isUnlocked;

    private void Start()
    {
        soundPlayer = GetComponent<SoundPlayer>();
        playerCamera = Camera.main;
    }

    private void Update()
    {
        if (isUnlocked == false) return;

        // Переключение ночного видения
        if (Input.GetKeyDown(inputSettings.nightVision) && lightObject != null)
        {
            if (!isActive)
            {
                customPassVolume.customPasses[2].enabled = true;
                lightObject.SetActive(true);
                isActive = true;
                soundPlayer.PlayerSound(0);

                if (effectsVolume.profile.TryGet<Bloom>(out var bloom))
                {
                    bloom.dirtTexture.overrideState = true;
                }
            }
            else
            {
                customPassVolume.customPasses[2].enabled = false;
                lightObject.SetActive(false);
                isActive = false;
                soundPlayer.PlayerSound(1);

                if (effectsVolume.profile.TryGet<Bloom>(out var bloom))
                {
                    bloom.dirtTexture.overrideState = false;
                }

                // Сброс зума при выключении
                if (playerCamera != null)
                    playerCamera.fieldOfView = defaultZoom;
            }
        }

        // Зум только при активном ночном видении
        if (isActive && playerCamera != null)
        {
            HandleZoom();
        }
    }

    private void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        float newFOV = playerCamera.fieldOfView;

        if (Input.GetKey(KeyCode.Z))
            newFOV -= zoomSpeed * Time.deltaTime * 10f;

        if (Input.GetKey(KeyCode.X))
            newFOV += zoomSpeed * Time.deltaTime * 10f;

        // Опционально: зум колёсиком мыши
        if (Mathf.Abs(scroll) > 0.01f)
            newFOV -= scroll * zoomSpeed * 10f;

        playerCamera.fieldOfView = Mathf.Clamp(newFOV, minZoom, maxZoom);
    }

    public void UnlockNightVision()
    {
        isUnlocked = true;
    }
}