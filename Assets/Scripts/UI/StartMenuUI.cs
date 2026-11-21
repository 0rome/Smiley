using UnityEngine;

public class StartMenuUI : MonoBehaviour
{
    [SerializeField] private LoadingScreen loadingScreen;

    public void StartButton()
    {
        loadingScreen.StartLoading();
    }
    public void ExitButton()
    {
        Application.Quit();
    }
}
