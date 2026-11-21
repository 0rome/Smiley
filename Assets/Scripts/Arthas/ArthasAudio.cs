using Cysharp.Threading.Tasks;
using DG.Tweening;
using SteamAudio;
using UnityEngine;

public class ArthasAudio : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] randomClips;
    [SerializeField] private AudioClip[] playerDetectClips;

    [SerializeField] private ArthasController arthasController;

    private void Start()
    {
        RandomSound().Forget();
    }

    private async UniTaskVoid RandomSound()
    {
        while (true)
        {
            await UniTask.WaitForSeconds(10);

            if (arthasController.CurrentState != ArthasState.Patrol)
                continue;

            audioSource.clip = randomClips[Random.Range(0, randomClips.Length)];
            audioSource.Play();
        }
    }
    public void DetectPlayerSound()
    {
        audioSource.clip = playerDetectClips[Random.Range(0, playerDetectClips.Length)];
        audioSource.Play();
    }
}
