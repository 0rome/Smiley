using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;

public class RandomWhisper : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private Transform[] pointsTransform;
    [SerializeField] private AudioClip[] whisperAudioClips;


    private void Start()
    {
        PlayWhisper().Forget();
    }

    private void PlayWhisperSound()
    {
        audioSource.transform.position = pointsTransform[Random.Range(0,pointsTransform.Length)].position;

        audioSource.PlayOneShot(whisperAudioClips[Random.Range(0,whisperAudioClips.Length)]);
    }
    private async UniTask PlayWhisper()
    {
        while(true)
        {
            await UniTask.WaitForSeconds(180f);
            PlayWhisperSound();
        }
    }
}
