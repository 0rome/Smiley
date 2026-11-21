using UnityEngine;
using Zenject;

public class SmileManAudio : MonoBehaviour
{
    [SerializeField] private AudioSource mainAudioSource;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip[] lookAroundClips;
    [SerializeField] private AudioClip[] walkingBreathClips;
    [SerializeField] private AudioClip[] runningBreathClips;
    [SerializeField] private AudioClip[] chargeClips;

    [Inject] private SmileManController smileManController;

    private SmileManState _previousState;

    private void Update()
    {
        CheckStateChanges();
    }

    private void CheckStateChanges()
    {
        var currentState = smileManController.smileManState;

        // Если состояние не изменилось, ничего не делаем
        if (currentState == _previousState) return;

        _previousState = currentState;

        // Распределяем звуки в зависимости от состояния
        switch (currentState)
        {
            case SmileManState.Patrolling:
                PlayRandomClip(walkingBreathClips, isLoop: true);
                break;
            case SmileManState.ChasingPlayer: // Или состояние бега
                PlayRandomChargeClip();
                PlayRandomClip(runningBreathClips, isLoop: true);
                break;
            case SmileManState.LookingAround:
                PlayRandomClip(lookAroundClips, isLoop: false);
                break;
            default:
                mainAudioSource.Stop(); // Если состояние нейтральное
                break;
        }
    }

    private void PlayRandomClip(AudioClip[] clips, bool isLoop)
    {
        if (clips == null || clips.Length == 0) return;

        mainAudioSource.clip = clips[Random.Range(0, clips.Length)];
        mainAudioSource.loop = isLoop;
        mainAudioSource.Play();
    }
    private void PlayRandomChargeClip()
    {
        if (chargeClips == null || chargeClips.Length == 0)
            return;

        AudioClip clip = chargeClips[Random.Range(0, chargeClips.Length)];

        mainAudioSource.PlayOneShot(clip);
    }
}