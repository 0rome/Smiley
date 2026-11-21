using UnityEngine;

public class FloorCreaking : NoiseSoundEvent
{
    [SerializeField] private AudioSource mainAudioSource;
    [SerializeField] private AudioClip[] audioClips;

    public void MakeSound()
    {
        MakeSoundEvent();

        int randomClip = Random.Range(0, audioClips.Length);

        mainAudioSource.PlayOneShot(audioClips[randomClip]);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            MakeSound();
        }
    }
}
