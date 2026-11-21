using UnityEngine;

public class SoundPlayer : MonoBehaviour
{
    [Header("AudioSource")]
    [SerializeField] private AudioSource m_AudioSource;

    [Header("AudioClips")]
    [SerializeField] private AudioClip[] audioClips;

    public void PlayerSound(int audioNumber)
    {
        m_AudioSource.PlayOneShot(audioClips[audioNumber]);
    }
}
