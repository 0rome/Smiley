
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class PlayerInteractionUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform prompt;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Canvas canvas;
    [SerializeField] private Slider holdSlider;
    [SerializeField] private TextMeshProUGUI InteractLetterTMP;
    [SerializeField] private TextMeshProUGUI objectNameTMP;


    [Header("Settings")]
    [SerializeField] private Vector2 screenOffset = new Vector2(0, 35);
    [SerializeField] private float fadeSpeed = 10f;
    [SerializeField] private float moveSpeed = 15f;

    
    [Inject] private InputSettings inputSettings;

    private Vector3 targetPosition;
    private Vector2 currentPosition;

    private bool isVisible;
    private bool hasPosition;


    private void Awake()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        holdSlider.value = 0f;
        holdSlider.gameObject.SetActive(false);
    }

    private void Update()
    {
        InteractLetterTMP.text = inputSettings.interact.ToString();

        float targetAlpha = isVisible ? 1f : 0f;

        canvasGroup.alpha = Mathf.MoveTowards(
            canvasGroup.alpha,
            targetAlpha,
            fadeSpeed * Time.deltaTime
        );

        if (!isVisible)
            return;

        Vector3 screenPosition =
            playerCamera.WorldToScreenPoint(targetPosition);

        if (screenPosition.z <= 0)
            return;

        RectTransform canvasRect =
            canvas.transform as RectTransform;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPosition,
            canvas.worldCamera,
            out Vector2 localPoint))
        {
            Vector2 targetUIPosition = localPoint + screenOffset;

            if (!hasPosition)
            {
                currentPosition = targetUIPosition;
                hasPosition = true;
            }
            else
            {
                currentPosition = Vector2.Lerp(
                    currentPosition,
                    targetUIPosition,
                    moveSpeed * Time.deltaTime
                );
            }

            prompt.anchoredPosition = currentPosition;
        }
    }

    public void Show(Vector3 worldPosition, string objectName)
    {
        targetPosition = worldPosition;

        InteractLetterTMP.text = inputSettings.interact.ToString();

        // Здесь назначь отдельный TMP для названия предмета.
        objectNameTMP.text = objectName;

        if (!isVisible)
        {
            hasPosition = false;
            prompt.gameObject.SetActive(true);
        }

        isVisible = true;
    }

    public void Hide()
    {
        isVisible = false;
        hasPosition = false;

        SetHolding(false);
    }

    public void SetHolding(bool holding)
    {
        holdSlider.gameObject.SetActive(holding);

        if (!holding)
            holdSlider.value = 0f;
    }

    public void SetHoldProgress(float progress)
    {
        holdSlider.value = Mathf.Clamp01(progress);
    }
}
