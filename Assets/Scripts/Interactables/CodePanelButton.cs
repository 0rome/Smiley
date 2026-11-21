using DG.Tweening;
using UnityEngine;

public class CodePanelButton : MonoBehaviour
{
    public enum ButtonType
    {
        Number,
        Enter,
        Delete
    }

    [Header("Settings")]
    [SerializeField] private string currentValue = "1";
    [SerializeField] private CodePanel codePanel;
    [SerializeField] private ButtonType buttonType;

    [Header("Materials")]
    [SerializeField] private Material greenEmissionMaterial;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip clickSound;

    [Header("Press Animation")]
    [SerializeField] private float pressDepth = 0.004f;
    [SerializeField] private float pressDuration = 0.1f;

    private Material mainMaterial;
    private MeshRenderer meshRenderer;
    private Vector3 baseLocalPosition;
    private Tween pressTween;

    private void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
            mainMaterial = meshRenderer.sharedMaterial;

        // Запоминаем исходную позицию ОДИН раз — от неё всегда считаем
        baseLocalPosition = transform.localPosition;
    }

    private void OnMouseDown()
    {
        if (codePanel == null || !codePanel.IsActive) return;

        PlayPressAnimation();

        if (audioSource != null && clickSound != null)
            audioSource.PlayOneShot(clickSound);

        switch (buttonType)
        {
            case ButtonType.Number:
                codePanel.EnterNumber(currentValue);
                break;
            case ButtonType.Enter:
                codePanel.EnterAllCode();
                break;
            case ButtonType.Delete:
                codePanel.DeleteAllCode();
                break;
        }
    }

    private void PlayPressAnimation()
    {
        // Убиваем предыдущий твин и ПРИНУДИТЕЛЬНО возвращаем кнопку в базу,
        // чтобы следующий твин стартовал из известной точки.
        pressTween?.Kill();
        transform.localPosition = baseLocalPosition;

        Vector3 pressedPos = baseLocalPosition + new Vector3(0f, 0f, -pressDepth);

        // Абсолютные перемещения (DOLocalMove), а не относительные (DOBlendableLocalMoveBy)
        pressTween = transform
            .DOLocalMove(pressedPos, pressDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                transform
                    .DOLocalMove(baseLocalPosition, pressDuration)
                    .SetEase(Ease.InQuad)
                    .OnComplete(() => transform.localPosition = baseLocalPosition);
            });
    }

    private void OnMouseEnter()
    {
        if (meshRenderer != null && greenEmissionMaterial != null)
            meshRenderer.sharedMaterial = greenEmissionMaterial;
    }

    private void OnMouseExit()
    {
        if (meshRenderer != null && mainMaterial != null)
            meshRenderer.sharedMaterial = mainMaterial;
    }

    private void OnDisable()
    {
        // При отключении объекта возвращаем кнопку на место и убиваем твин
        pressTween?.Kill();
        transform.localPosition = baseLocalPosition;
    }

    private void OnDestroy()
    {
        pressTween?.Kill();
    }
}