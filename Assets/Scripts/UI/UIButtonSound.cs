using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Автоматически добавляет компонент Button, если его нет
public class UIButtonSound : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    [Header("Настройки звука")]
    [Tooltip("Звук при наведении курсора")]
    [SerializeField] private AudioClip hoverSound;

    [Tooltip("Звук при клике")]
    [SerializeField] private AudioClip clickSound;

    [Header("AudioSource")]
    [SerializeField] private AudioSource audioSource;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (hoverSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(hoverSound);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (clickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(clickSound);
        }
    }
}