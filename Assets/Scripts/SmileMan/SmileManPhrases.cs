
using Cysharp.Threading.Tasks;
using UnityEngine;

public class SmileManPhrases : MonoBehaviour
{
    [Header("Audio settings")]
    [SerializeField] private AudioSource mainAudioSource;
    [SerializeField] private AudioClip[] audioClips;
    [SerializeField] private float timeBetweenSounds = 10f;

    private void Start()
    {
        MakeVoiceSound().Forget();
    }
    private async UniTask MakeVoiceSound()
    {
        while (true)
        {
            await UniTask.WaitForSeconds(timeBetweenSounds);

            AudioClip rand = audioClips[Random.Range(0, audioClips.Length)];

            mainAudioSource.PlayOneShot(rand);
        }
    }
}
