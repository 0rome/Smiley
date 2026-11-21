using UnityEngine;
using Zenject;

public class NightVisionItem : InteractableBase
{
    [SerializeField] private NightVision nightVision;
    [SerializeField] private AudioSource playerAudioSource;
    [SerializeField] private AudioClip takeItemClip;

    [Inject] private ScreenClueManager screenClueManager;
    [Inject] private InputSettings inputSettings;
    [Inject] private PlotActionManager plotActionManager;

    public override void Interact()
    {
        nightVision.UnlockNightVision();
        screenClueManager.ShowClue("Press " + inputSettings.nightVision + " to activate night vision");
        playerAudioSource.PlayOneShot(takeItemClip);
        plotActionManager.CompleteAction("NightVisionGlassesTaken");
        
        gameObject.SetActive(false);
    }
}
