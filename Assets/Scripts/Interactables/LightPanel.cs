using DG.Tweening;
using UnityEngine;

public class LightPanel : InteractableBase
{
    [Header("Audio settings")]
    [SerializeField] private GameObject[] toggles;

    [Header("Audio settings")]
    [SerializeField] private AudioSource mainAudioSource;
    [SerializeField] private AudioClip toggleClip;

    private bool isActive = true;

    public override void Interact()
    {
        float zRot = isActive ? 45 : -45;

        foreach (var item in toggles)
        {
            item.transform.DOLocalRotate(new Vector3(0,90,zRot),1f);
        }

        isActive = !isActive;

        mainAudioSource.PlayOneShot(toggleClip);
    }

}
