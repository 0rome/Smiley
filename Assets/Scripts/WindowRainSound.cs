using UnityEngine;

public class WindowRainSound : MonoBehaviour
{
    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        if (audioSource.clip != null)
        {
            // Устанавливаем случайную точку воспроизведения (в секундах)
            audioSource.time = Random.Range(0f, audioSource.clip.length);
            audioSource.Play();
        }
    }
}
