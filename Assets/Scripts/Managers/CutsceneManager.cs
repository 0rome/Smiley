
using Unity.DemoTeam.Hair;
using UnityEngine;
using UnityEngine.Playables;
using Zenject;

public class CutsceneManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayableDirector playableDirector;

    [Header("Cutscenes")]
    [SerializeField] private PlayableAsset[] cutscenes;

    public void PlayCutscene(string cutsceneName)
    {
        if (playableDirector == null)
        {
            Debug.LogError("PlayableDirector is not assigned!", this);
            return;
        }

        foreach (PlayableAsset cutscene in cutscenes)
        {
            if (cutscene == null)
                continue;

            if (cutscene.name == cutsceneName)
            {
                playableDirector.Stop();
                playableDirector.playableAsset = cutscene;
                playableDirector.Play();
                return;
            }
        }

        Debug.LogWarning($"Cutscene '{cutsceneName}' not found!", this);
    }

    public void StopCutscene()
    {
        if (playableDirector != null)
            playableDirector.Stop();
    }
}
