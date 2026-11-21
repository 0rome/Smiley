using Cysharp.Threading.Tasks;
using UnityEngine;
using Zenject;
using System.Threading;

public class MusicAndJumpscares : MonoBehaviour
{
    [SerializeField] private AudioSource mainAudioSource;
    [SerializeField] private AudioClip[] chasingMusic;

    [Header("Fade Settings")]
    [SerializeField] private float fadeDuration = 1f;

    [Inject] private SmileManController smileManController;

    private float mainAudioSourceVolume;
    private SmileManState _previousState;
    private CancellationTokenSource _fadeCts;

    private void Start()
    {
        mainAudioSourceVolume = mainAudioSource.volume;
    }

    private void Update() => CheckStateChanges();

    private void CheckStateChanges()
    {
        var currentState = smileManController.smileManState;
        if (currentState == _previousState) return;

        _previousState = currentState;

        switch (currentState)
        {
            case SmileManState.ChasingPlayer:
                _fadeCts?.Cancel();
                _fadeCts = null;
                mainAudioSource.volume = mainAudioSourceVolume;

                if (!mainAudioSource.isPlaying)
                {
                    PlayRandomClip(chasingMusic);
                }
                break;

            default:
                if (mainAudioSource.isPlaying && _fadeCts == null)
                {
                    _fadeCts = new CancellationTokenSource();
                    FadeOutAsync(fadeDuration, _fadeCts.Token).Forget();
                }
                break;
        }
    }

    private void PlayRandomClip(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0) return;
        mainAudioSource.clip = clips[Random.Range(0, clips.Length)];
        mainAudioSource.Play();
    }

    private async UniTask FadeOutAsync(float duration, CancellationToken token)
    {
        float startVolume = mainAudioSource.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            mainAudioSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: token);
        }

        mainAudioSource.volume = 0f;
        mainAudioSource.Stop();
        _fadeCts = null;
    }

    private void OnDestroy() => _fadeCts?.Cancel();
}