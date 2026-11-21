using Cysharp.Threading.Tasks;
using UnityEngine;
using Zenject;

public class NightVisionTakenPlotAction : MonoBehaviour
{
    [SerializeField] private Lamp[] allLamps;

    [Inject] private PlotActionManager plotActionManager;

    private void Update()
    {
        if (plotActionManager.IsActionCompleted("NightVisionGlassesTaken"))
        {
            TurnOffLights().Forget();
            enabled = false;
        }
    }

    private async UniTask TurnOffLights()
    {
        await UniTask.WaitForSeconds(2f);

        foreach (Lamp lamp in allLamps)
        {
            lamp.OffLight();
        }
    }
}
