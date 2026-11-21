using System.Collections.Generic;
using UnityEngine;

public class PlotActionManager : MonoBehaviour
{
    private Dictionary<string, bool> PlotActions = new Dictionary<string, bool>();

    private void Awake()
    {
        PlotActions.Add("NightVisionGlassesTaken", false);
    }

    public void CompleteAction(string actionName)
    {
        if (!PlotActions.ContainsKey(actionName))
        {
            Debug.LogWarning($"Plot action '{actionName}' doesn't exist!");
            return;
        }

        PlotActions[actionName] = true;
    }

    public bool IsActionCompleted(string actionName)
    {
        if (!PlotActions.ContainsKey(actionName))
        {
            Debug.LogWarning($"Plot action '{actionName}' doesn't exist!");
            return false;
        }

        return PlotActions[actionName];
    }
}